#pragma warning disable xUnit1030

// <copyright file="PipelinePersistencePerformanceTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.IntegrationTests.Performance;

using System.Diagnostics;
using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

using Hexalith.Memories.Contracts.V1;
using Hexalith.Memories.IntegrationTests.Fixtures;

using Shouldly;

using Xunit;

using MemoriesCase = Hexalith.Memories.Contracts.V1.Case;

/// <summary>
/// Performance-oriented integration benchmarks for Story 6.4.
/// Measures warm restart time and sustained inline-ingestion throughput with fake embeddings.
/// </summary>
[Collection("AspireIngestionPipeline")]
[Trait("Category", "Integration")]
[Trait("Category", "IntegrationSlow")]
[Trait("Category", "Performance")]
public sealed class PipelinePersistencePerformanceTests
{
    private const int LargePayloadBytes = 256 * 1024;
    private const int LargePayloadUnitCount = 8;
    private const double LargePayloadUnitsPerMinuteTarget = 10.0;
    private const int SmallPayloadBytes = 8 * 1024;
    private const int SmallPayloadUnitCount = 24;
    private const double SmallPayloadUnitsPerMinuteTarget = 100.0;
    private static readonly TimeSpan DefaultTimeout = TimeSpan.FromMinutes(5);
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
    };
    private static readonly TimeSpan WarmRestartTarget = TimeSpan.FromSeconds(60);

    private readonly AspireIngestionPipelineFixture _fixture;
    private readonly ITestOutputHelper _output;

    /// <summary>Initializes a new instance of the <see cref="PipelinePersistencePerformanceTests"/> class.</summary>
    /// <param name="fixture">Shared Aspire ingestion topology fixture.</param>
    /// <param name="output">xUnit test output sink.</param>
    public PipelinePersistencePerformanceTests(AspireIngestionPipelineFixture fixture, ITestOutputHelper output)
    {
        _fixture = fixture;
        _output = output;
    }

    [Fact]
    public async Task RunPipelinePersistenceBenchmarks_ShouldMeetWarmRestartAndThroughputTargets()
    {
        PipelineThroughputMeasurement smallPayload = await MeasureUrlThroughputAsync(
            "small-payload",
            SmallPayloadBytes,
            SmallPayloadUnitCount,
            SmallPayloadUnitsPerMinuteTarget).ConfigureAwait(false);

        PipelineThroughputMeasurement largePayload = await MeasureUrlThroughputAsync(
            "large-payload",
            LargePayloadBytes,
            LargePayloadUnitCount,
            LargePayloadUnitsPerMinuteTarget).ConfigureAwait(false);

        // Measure warm restart last so throughput runs against the already-stable initial topology,
        // while the restart metric still excludes first-time image pull latency.
        TimeSpan warmRestartDuration = await _fixture.RestartTopologyAsync().ConfigureAwait(false);

        PipelinePersistencePerformanceResult result = new()
        {
            RunTimestamp = DateTimeOffset.UtcNow,
            WarmRestartSeconds = warmRestartDuration.TotalSeconds,
            WarmRestartTargetSeconds = WarmRestartTarget.TotalSeconds,
            WarmRestartWithinTarget = warmRestartDuration <= WarmRestartTarget,
            SmallPayload = smallPayload,
            LargePayload = largePayload,
        };

        string outputPath = GetBenchmarkResultsOutputPath();
        await WriteBenchmarkResultsArtifactAsync(result, outputPath).ConfigureAwait(false);

        _output.WriteLine(FormatConsoleReport(result, outputPath));

        File.Exists(outputPath).ShouldBeTrue("pipeline-persistence-performance.json should be written");
        warmRestartDuration.TotalSeconds.ShouldBeLessThanOrEqualTo(WarmRestartTarget.TotalSeconds);
        smallPayload.UnitsPerMinute.ShouldBeGreaterThan(SmallPayloadUnitsPerMinuteTarget);
        largePayload.UnitsPerMinute.ShouldBeGreaterThan(LargePayloadUnitsPerMinuteTarget);
    }

    private static string GetBenchmarkResultsOutputPath()
        => Path.Combine(AppContext.BaseDirectory, "pipeline-persistence-performance.json");

    private static string FormatConsoleReport(PipelinePersistencePerformanceResult result, string outputPath)
    {
        StringBuilder sb = new();
        sb.AppendLine("╔══════════════════════════════════════════════════════════════════════════════════════════════╗");
        sb.AppendLine("║                  PIPELINE PERSISTENCE PERFORMANCE — Story 6.4 Benchmarks                  ║");
        sb.AppendLine("╚══════════════════════════════════════════════════════════════════════════════════════════════╝");
        sb.AppendLine();
        sb.AppendLine(string.Format(
            CultureInfo.InvariantCulture,
            "Warm restart: {0:F2}s (target ≤ {1:F0}s) => {2}",
            result.WarmRestartSeconds,
            result.WarmRestartTargetSeconds,
            result.WarmRestartWithinTarget ? "PASS" : "FAIL"));
        sb.AppendLine();
        sb.AppendLine("Throughput scenarios:");
        sb.AppendLine(FormatMeasurement(result.SmallPayload));
        sb.AppendLine(FormatMeasurement(result.LargePayload));
        sb.AppendLine();
        sb.AppendLine($"Artifact: {outputPath}");
        return sb.ToString();
    }

    private static string FormatMeasurement(PipelineThroughputMeasurement measurement)
        => string.Format(
            CultureInfo.InvariantCulture,
            "  - {0}: {1} units × {2:N0} bytes in {3:F2}s => {4:F1} units/min (target > {5:F0}, {6})",
            measurement.Scenario,
            measurement.UnitCount,
            measurement.PayloadSizeBytes,
            measurement.ElapsedSeconds,
            measurement.UnitsPerMinute,
            measurement.TargetUnitsPerMinute,
            measurement.MeetsTarget ? "PASS" : "FAIL");

    private static async Task WriteBenchmarkResultsArtifactAsync(
        PipelinePersistencePerformanceResult result,
        string outputPath)
    {
        string json = JsonSerializer.Serialize(result, JsonOptions);
        await File.WriteAllTextAsync(outputPath, json).ConfigureAwait(false);
    }

    private async Task<PipelineThroughputMeasurement> MeasureUrlThroughputAsync(
        string scenario,
        int payloadSizeBytes,
        int unitCount,
        double targetUnitsPerMinute)
    {
        string tenantId = $"tenant-{scenario}-{Guid.NewGuid():N}";
        byte[] payloadBytes = CreatePayloadBytes(payloadSizeBytes, scenario, 0);

        await EnsureTenantActiveAsync(tenantId).ConfigureAwait(false);
        await RaiseRateLimitBudgetAsync(tenantId, unitCount).ConfigureAwait(false);

        await using ScriptedHttpServer server = await ScriptedHttpServer.StartAsync((_, _) =>
            new ValueTask<ScriptedHttpResponse>(
                new ScriptedHttpResponse(
                    HttpStatusCode.OK,
                    payloadBytes,
                    "text/plain; charset=utf-8",
                    new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)))).ConfigureAwait(false);

        string warmupCaseId = await CreateCaseAsync(tenantId, $"{scenario} warmup").ConfigureAwait(false);
        await WarmUpScenarioAsync(tenantId, warmupCaseId, server, scenario).ConfigureAwait(false);

        string measuredCaseId = await CreateCaseAsync(tenantId, $"{scenario} benchmark").ConfigureAwait(false);

        Stopwatch stopwatch = Stopwatch.StartNew();

        Task<string>[] submissions = Enumerable.Range(0, unitCount)
            .Select(index => PostUrlIngestionAsync(
                tenantId,
                measuredCaseId,
                server.GetUri(BuildRelativePath(scenario, index)).ToString()))
            .ToArray();

        _ = await Task.WhenAll(submissions).ConfigureAwait(false);

        CaseStatusDetail finalStatus = await WaitForCaseStatusAsync(
            tenantId,
            measuredCaseId,
            status => status.IndexedCount == unitCount &&
                status.FailedCount == 0 &&
                status.QueuedCount == 0 &&
                status.ExtractingCount == 0 &&
                status.EmbeddingCount == 0 &&
                status.IndexingCount == 0,
            DefaultTimeout).ConfigureAwait(false);

        stopwatch.Stop();

        finalStatus.IndexedCount.ShouldBe(unitCount);
        finalStatus.FailedCount.ShouldBe(0);

        // Guard against a degenerate "instant success" (e.g. all units short-circuited through a dedup hit)
        // which would divide by ~0 and make unitsPerMinute = +Infinity, silently passing any target check.
        stopwatch.Elapsed.ShouldBeGreaterThan(
            TimeSpan.FromMilliseconds(1),
            "throughput measurement requires a non-trivial elapsed window");

        double unitsPerMinute = unitCount / stopwatch.Elapsed.TotalMinutes;
        return new PipelineThroughputMeasurement
        {
            Scenario = scenario,
            PayloadSizeBytes = payloadSizeBytes,
            UnitCount = unitCount,
            ElapsedSeconds = stopwatch.Elapsed.TotalSeconds,
            UnitsPerMinute = unitsPerMinute,
            TargetUnitsPerMinute = targetUnitsPerMinute,
            MeetsTarget = unitsPerMinute > targetUnitsPerMinute,
        };
    }

    private async Task WarmUpScenarioAsync(
        string tenantId,
        string caseId,
        ScriptedHttpServer server,
        string scenario)
    {
        _ = await PostUrlIngestionAsync(
                tenantId,
                caseId,
                server.GetUri(BuildRelativePath($"{scenario}-warmup", 0)).ToString())
            .ConfigureAwait(false);

        CaseStatusDetail status = await WaitForCaseStatusAsync(
            tenantId,
            caseId,
            candidate => candidate.IndexedCount == 1 &&
                candidate.FailedCount == 0 &&
                candidate.QueuedCount == 0 &&
                candidate.ExtractingCount == 0 &&
                candidate.EmbeddingCount == 0 &&
                candidate.IndexingCount == 0,
            DefaultTimeout).ConfigureAwait(false);

        status.IndexedCount.ShouldBe(1);
    }

    private async Task RaiseRateLimitBudgetAsync(string tenantId, int unitCount)
    {
        using HttpResponseMessage getResponse = await _fixture.MemoriesClient
            .GetAsync($"/api/tenants/{tenantId}/embedding-config")
            .ConfigureAwait(false);

        getResponse.StatusCode.ShouldBe(HttpStatusCode.OK);

        TenantEmbeddingConfig? currentConfig = await getResponse.Content
            .ReadFromJsonAsync<TenantEmbeddingConfig>(MemoriesJsonContext.Options)
            .ConfigureAwait(false);

        currentConfig.ShouldNotBeNull();

        int desiredRateLimit = Math.Max(currentConfig.RateLimitPerMinute, Math.Max(600, unitCount * 20));
        using HttpResponseMessage putResponse = await _fixture.MemoriesClient
            .PutAsJsonAsync(
                $"/api/tenants/{tenantId}/embedding-config",
                currentConfig with { RateLimitPerMinute = desiredRateLimit },
                MemoriesJsonContext.Options)
            .ConfigureAwait(false);

        putResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    private async Task EnsureTenantActiveAsync(string tenantId)
    {
        using HttpResponseMessage provisionResponse = await _fixture.MemoriesClient
            .PostAsJsonAsync(
                "/api/tenants",
                new TenantProvisioningInput(tenantId, $"Tenant {tenantId}"),
                MemoriesJsonContext.Options)
            .ConfigureAwait(false);

        provisionResponse.StatusCode.ShouldBe(HttpStatusCode.Accepted);

        DateTimeOffset deadline = DateTimeOffset.UtcNow.Add(DefaultTimeout);
        while (DateTimeOffset.UtcNow < deadline)
        {
            using HttpResponseMessage tenantResponse = await _fixture.MemoriesClient
                .GetAsync($"/api/tenants/{tenantId}")
                .ConfigureAwait(false);

            if (tenantResponse.StatusCode == HttpStatusCode.OK)
            {
                TenantInfo? tenant = await tenantResponse.Content
                    .ReadFromJsonAsync<TenantInfo>(MemoriesJsonContext.Options)
                    .ConfigureAwait(false);

                if (tenant?.Status == TenantStatus.Active)
                {
                    return;
                }
            }

            await Task.Delay(TimeSpan.FromSeconds(2)).ConfigureAwait(false);
        }

        throw new TimeoutException($"Tenant '{tenantId}' did not become active within {DefaultTimeout}.");
    }

    private async Task<string> CreateCaseAsync(string tenantId, string scenarioLabel)
    {
        using HttpResponseMessage response = await _fixture.MemoriesClient
            .PostAsJsonAsync(
                $"/api/tenants/{tenantId}/cases",
                new CreateCaseInput(tenantId, $"{scenarioLabel} {Guid.NewGuid():N}", "Story 6.4 performance benchmark"),
                MemoriesJsonContext.Options)
            .ConfigureAwait(false);

        response.StatusCode.ShouldBe(HttpStatusCode.Created);

        MemoriesCase? created = await response.Content
            .ReadFromJsonAsync<MemoriesCase>(MemoriesJsonContext.Options)
            .ConfigureAwait(false);

        created.ShouldNotBeNull();
        created.Id.ShouldNotBeNullOrWhiteSpace();
        return created.Id;
    }

    private async Task<string> PostUrlIngestionAsync(string tenantId, string caseId, string sourceUri)
    {
        using HttpResponseMessage response = await _fixture.MemoriesClient
            .PostAsJsonAsync(
                "/api/ingest/url",
                new UrlIngestionRequest
                {
                    TenantId = tenantId,
                    CaseId = caseId,
                    Url = sourceUri,
                    IngestedBy = "performance@test.local",
                },
                MemoriesJsonContext.Options)
            .ConfigureAwait(false);

        response.StatusCode.ShouldBe(HttpStatusCode.Accepted);

        UrlIngestionResponse? accepted = await response.Content
            .ReadFromJsonAsync<UrlIngestionResponse>(MemoriesJsonContext.Options)
            .ConfigureAwait(false);

        accepted.ShouldNotBeNull();
        string instanceId = accepted.InstanceId;
        instanceId.ShouldNotBeNullOrWhiteSpace();
        return instanceId;
    }

    private async Task<CaseStatusDetail> WaitForCaseStatusAsync(
        string tenantId,
        string caseId,
        Func<CaseStatusDetail, bool> predicate,
        TimeSpan timeout)
    {
        DateTimeOffset deadline = DateTimeOffset.UtcNow.Add(timeout);
        while (DateTimeOffset.UtcNow < deadline)
        {
            using HttpResponseMessage response = await _fixture.MemoriesClient
                .GetAsync($"/api/tenants/{tenantId}/cases/{caseId}/status")
                .ConfigureAwait(false);

            if (response.StatusCode == HttpStatusCode.OK)
            {
                CaseStatusDetail? status = await response.Content
                    .ReadFromJsonAsync<CaseStatusDetail>(MemoriesJsonContext.Options)
                    .ConfigureAwait(false);

                if (status is not null && predicate(status))
                {
                    return status;
                }
            }

            await Task.Delay(TimeSpan.FromSeconds(1)).ConfigureAwait(false);
        }

        throw new TimeoutException($"Case status for '{tenantId}/{caseId}' did not satisfy the predicate within {timeout}.");
    }

    private static string BuildRelativePath(string scenario, int index)
        => $"/{scenario}/{index:D4}.txt";

    private static byte[] CreatePayloadBytes(int payloadSizeBytes, string scenario, int index)
    {
        string header = $"Story 6.4 {scenario} payload {index:D4}\n";
        byte[] headerBytes = Encoding.UTF8.GetBytes(header);

        if (headerBytes.Length >= payloadSizeBytes)
        {
            return headerBytes[..payloadSizeBytes];
        }

        byte[] buffer = new byte[payloadSizeBytes];
        headerBytes.CopyTo(buffer, 0);
        for (int i = headerBytes.Length; i < buffer.Length; i++)
        {
            buffer[i] = (byte)('a' + (i % 26));
        }

        return buffer;
    }

    private sealed record PipelinePersistencePerformanceResult
    {
        public required DateTimeOffset RunTimestamp { get; init; }

        public required double WarmRestartSeconds { get; init; }

        public required double WarmRestartTargetSeconds { get; init; }

        public required bool WarmRestartWithinTarget { get; init; }

        public required PipelineThroughputMeasurement SmallPayload { get; init; }

        public required PipelineThroughputMeasurement LargePayload { get; init; }
    }

    private sealed record PipelineThroughputMeasurement
    {
        public required string Scenario { get; init; }

        public required int PayloadSizeBytes { get; init; }

        public required int UnitCount { get; init; }

        public required double ElapsedSeconds { get; init; }

        public required double UnitsPerMinute { get; init; }

        public required double TargetUnitsPerMinute { get; init; }

        public required bool MeetsTarget { get; init; }
    }
}

#pragma warning restore xUnit1030
