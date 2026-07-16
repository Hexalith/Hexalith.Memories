// <copyright file="PipelinePersistenceIntegrationTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

#pragma warning disable xUnit1030

namespace Hexalith.Memories.IntegrationTests.Ingestion;

using System.Net;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

using Hexalith.Memories.Contracts.V1;
using Hexalith.Memories.IntegrationTests.Fixtures;
using Hexalith.Memories.Server.Infrastructure;

using NFalkorDB;

using Shouldly;

using StackExchange.Redis;

using MemoriesCase = Hexalith.Memories.Contracts.V1.Case;

/// <summary>Integration coverage for Story 6.4 restart durability and recovery behavior.</summary>
[Collection("AspireIngestionPipeline")]
[Trait("Category", "Integration")]
[Trait("Category", "IntegrationSlow")]
public sealed class PipelinePersistenceIntegrationTests
{
    private static readonly TimeSpan DefaultTimeout = TimeSpan.FromMinutes(3);

    private readonly AspireIngestionPipelineFixture _fixture;

    /// <summary>Initializes a new instance of the <see cref="PipelinePersistenceIntegrationTests"/> class.</summary>
    /// <param name="fixture">Shared Aspire topology fixture.</param>
    public PipelinePersistenceIntegrationTests(AspireIngestionPipelineFixture fixture)
        => _fixture = fixture;

    [Fact]
    public async Task RestartTopology_InFlightUrlIngestion_ShouldResumeWithoutDuplicateWrites()
    {
        string tenantId = $"tenant-{Guid.NewGuid():N}";
        string caseId = await CreateTenantAndCaseAsync(tenantId).ConfigureAwait(false);

        await using ScriptedHttpServer server = await ScriptedHttpServer.StartAsync(async (_, cancellationToken) =>
        {
            await Task.Delay(TimeSpan.FromSeconds(6), cancellationToken).ConfigureAwait(false);
            return ScriptedHttpResponse.Text("Restart durability proof document.");
        }).ConfigureAwait(false);

        string sourceUri = server.GetUri("/durability/resume.txt").ToString();
        UrlIngestionResponse accepted = await PostUrlIngestionAsync(tenantId, caseId, sourceUri).ConfigureAwait(false);

        await WaitForRequestCountAsync(server, 1, TimeSpan.FromSeconds(45)).ConfigureAwait(false);
        _ = await WaitForCaseStatusAsync(
            tenantId,
            caseId,
            status => status.ExtractingCount >= 1,
            DefaultTimeout).ConfigureAwait(false);

        TimeSpan restartElapsed = await _fixture.RestartTopologyAsync().ConfigureAwait(false);
        restartElapsed.ShouldBeLessThan(TimeSpan.FromSeconds(60));

        (string syntacticKey, string semanticKey) = await WaitForBackendWritesAsync(tenantId, caseId, sourceUri).ConfigureAwait(false);

        string[] syntacticKeys = await ListKeysAsync($"{tenantId}:mu:*").ConfigureAwait(false);
        string[] semanticKeys = await ListKeysAsync($"{tenantId}:vec:*").ConfigureAwait(false);
        syntacticKeys.Length.ShouldBe(1);
        semanticKeys.Length.ShouldBe(1);
        syntacticKey.ShouldBe(syntacticKeys.Single());
        semanticKey.ShouldBe(semanticKeys.Single());

        long graphCount = await CountGraphNodesAsync(tenantId, caseId, sourceUri).ConfigureAwait(false);
        graphCount.ShouldBe(1);
    }

    [Fact]
    public async Task RestartTopology_InFlightUrlIngestion_ShouldKeepSingleDedupKeyAndStableMemoryUnitId()
    {
        string tenantId = $"tenant-{Guid.NewGuid():N}";
        string caseId = await CreateTenantAndCaseAsync(tenantId).ConfigureAwait(false);

        await using ScriptedHttpServer server = await ScriptedHttpServer.StartAsync(async (_, cancellationToken) =>
        {
            await Task.Delay(TimeSpan.FromSeconds(8), cancellationToken).ConfigureAwait(false);
            return ScriptedHttpResponse.Text("Dedup durability proof document.");
        }).ConfigureAwait(false);

        string sourceUri = server.GetUri("/durability/dedup.txt").ToString();
        UrlIngestionResponse accepted = await PostUrlIngestionAsync(tenantId, caseId, sourceUri).ConfigureAwait(false);

        await WaitForRequestCountAsync(server, 1, TimeSpan.FromSeconds(45)).ConfigureAwait(false);
        _ = await WaitForCaseStatusAsync(
            tenantId,
            caseId,
            status => status.ExtractingCount >= 1,
            DefaultTimeout).ConfigureAwait(false);

        _ = await _fixture.RestartTopologyAsync().ConfigureAwait(false);

        (string syntacticKey, _) = await WaitForBackendWritesAsync(tenantId, caseId, sourceUri).ConfigureAwait(false);
        string memoryUnitId = syntacticKey.Split(':').Last();

        string[] dedupKeys = await WaitForSingleDedupValueAsync(tenantId, caseId, memoryUnitId, DefaultTimeout).ConfigureAwait(false);
        dedupKeys.Length.ShouldBe(1);

        RedisValue dedupValue = await _fixture.RedisConnection.GetDatabase().StringGetAsync(dedupKeys[0]).ConfigureAwait(false);
        dedupValue.ToString().ShouldBe(memoryUnitId);

        MemoryUnit indexed = await WaitForMemoryUnitAsync(
            tenantId,
            caseId,
            memoryUnitId,
            memoryUnit => memoryUnit.Status == MemoryUnitStatus.Indexed,
            DefaultTimeout).ConfigureAwait(false);

        indexed.Id.ShouldBe(memoryUnitId);
        indexed.SourceUri.ShouldBe(sourceUri);
        indexed.Status.ShouldBe(MemoryUnitStatus.Indexed);
    }

    [Fact]
    public async Task RestartTopology_InFlightUrlIngestion_ShouldRestoreCaseCounterActorState()
    {
        int logStartIndex = _fixture.LogEntryCount;
        string tenantId = $"tenant-{Guid.NewGuid():N}";
        string caseId = await CreateTenantAndCaseAsync(tenantId).ConfigureAwait(false);

        await using ScriptedHttpServer server = await ScriptedHttpServer.StartAsync(async (_, cancellationToken) =>
        {
            await Task.Delay(TimeSpan.FromSeconds(10), cancellationToken).ConfigureAwait(false);
            return ScriptedHttpResponse.Text("Counter durability proof document.");
        }).ConfigureAwait(false);

        string sourceUri = server.GetUri("/durability/counter.txt").ToString();
        UrlIngestionResponse accepted = await PostUrlIngestionAsync(tenantId, caseId, sourceUri).ConfigureAwait(false);

        await WaitForRequestCountAsync(server, 1, TimeSpan.FromSeconds(45)).ConfigureAwait(false);

        CaseStatusDetail statusBeforeRestart = await WaitForCaseStatusAsync(
            tenantId,
            caseId,
            status => status.ExtractingCount >= 1,
            DefaultTimeout).ConfigureAwait(false);
        statusBeforeRestart.ExtractingCount.ShouldBeGreaterThanOrEqualTo(1);

        _ = accepted;
        _ = await _fixture.RestartTopologyAsync().ConfigureAwait(false);

        CaseStatusDetail statusAfterRestart = await WaitForCaseStatusAsync(
            tenantId,
            caseId,
            status => status.QueuedCount + status.ExtractingCount + status.EmbeddingCount + status.IndexingCount > 0,
            DefaultTimeout).ConfigureAwait(false);
        (statusAfterRestart.QueuedCount + statusAfterRestart.ExtractingCount + statusAfterRestart.EmbeddingCount + statusAfterRestart.IndexingCount)
            .ShouldBeGreaterThan(0);

        await WaitForWorkflowRuntimeStatusAsync(
            tenantId,
            accepted.InstanceId,
            "Completed",
            DefaultTimeout,
            () => BuildRestartFailureDiagnosticsAsync(
                tenantId,
                caseId,
                accepted.InstanceId,
                server,
                statusBeforeRestart,
                statusAfterRestart,
                logStartIndex)).ConfigureAwait(false);
        server.RequestCount.ShouldBeLessThanOrEqualTo(
            5,
            "URL fetching must use only the durable workflow retry budget, without nested HTTP resilience retries.");

        CaseStatusDetail drained = await WaitForCaseStatusAsync(
            tenantId,
            caseId,
            status => status.QueuedCount == 0 && status.ExtractingCount == 0 && status.EmbeddingCount == 0 && status.IndexingCount == 0,
            DefaultTimeout).ConfigureAwait(false);

        drained.QueuedCount.ShouldBe(0);
        drained.ExtractingCount.ShouldBe(0);
        drained.EmbeddingCount.ShouldBe(0);
        drained.IndexingCount.ShouldBe(0);
    }

    [Fact]
    public async Task RestartTopology_ShouldPreserveEmbeddingRateLimiterBudget()
    {
        string tenantId = $"tenant-{Guid.NewGuid():N}";
        string caseId = await CreateTenantAndCaseAsync(tenantId).ConfigureAwait(false);

        TenantEmbeddingConfig currentConfig = await GetTenantEmbeddingConfigAsync(tenantId).ConfigureAwait(false);
        await UpdateTenantEmbeddingConfigAsync(
            tenantId,
            currentConfig with { RateLimitPerMinute = 1, ReindexRequired = false }).ConfigureAwait(false);

        string firstSourceUri = $"file:///{Guid.NewGuid():N}-budget-1.txt";
        _ = await PostInlineIngestionAsync(
            tenantId,
            caseId,
            firstSourceUri,
            "Rate limiter persistence first document.").ConfigureAwait(false);

        _ = await WaitForBackendWritesAsync(tenantId, caseId, firstSourceUri).ConfigureAwait(false);

        _ = await _fixture.RestartTopologyAsync().ConfigureAwait(false);

        string secondSourceUri = $"file:///{Guid.NewGuid():N}-budget-2.txt";
        _ = await PostInlineIngestionAsync(
            tenantId,
            caseId,
            secondSourceUri,
            "Rate limiter persistence second document.").ConfigureAwait(false);

        FailedUnitsPage failedUnits = await WaitForFailedUnitsPageAsync(
            tenantId,
            caseId,
            page => page.Units.Any(unit => string.Equals(unit.SourceUri, secondSourceUri, StringComparison.Ordinal)),
            DefaultTimeout).ConfigureAwait(false);

        FailedUnitSummary rateLimited = failedUnits.Units.Single(unit => string.Equals(unit.SourceUri, secondSourceUri, StringComparison.Ordinal));
        rateLimited.Stage.ShouldBe("embedding");

        MemoryUnit failedMemoryUnit = await WaitForMemoryUnitAsync(
            tenantId,
            caseId,
            rateLimited.MemoryUnitId,
            memoryUnit => memoryUnit.Status == MemoryUnitStatus.Failed &&
                memoryUnit.FailureDetails is not null &&
                string.Equals(memoryUnit.FailureDetails.Stage, "embedding", StringComparison.Ordinal),
            DefaultTimeout).ConfigureAwait(false);

        failedMemoryUnit.FailureDetails.ShouldNotBeNull();
        failedMemoryUnit.FailureDetails!.Stage.ShouldBe("embedding");
    }

    [Fact]
    public async Task RestartTopology_ShouldPreserveOrRehydrateCorpusStatistics()
    {
        string tenantId = $"tenant-{Guid.NewGuid():N}";
        string caseId = await CreateTenantAndCaseAsync(tenantId).ConfigureAwait(false);
        string sourceUri = $"file:///{Guid.NewGuid():N}.txt";
        const string searchQuery = "corpus statistics durability validation";

        _ = await PostInlineIngestionAsync(
            tenantId,
            caseId,
            sourceUri,
            "Corpus statistics durability validation document.").ConfigureAwait(false);

        _ = await WaitForBackendWritesAsync(tenantId, caseId, sourceUri).ConfigureAwait(false);

        HybridSearchResult beforeRestart = await WaitForHybridSearchAsync(
            tenantId,
            searchQuery,
            result => result.Results.Any(resultItem => string.Equals(resultItem.SourceUri, sourceUri, StringComparison.Ordinal)) &&
                !result.UnavailableAxes.Contains("syntactic"),
            DefaultTimeout).ConfigureAwait(false);

        _ = await _fixture.RestartTopologyAsync().ConfigureAwait(false);

        HybridSearchResult afterRestart = await WaitForHybridSearchAsync(
            tenantId,
            searchQuery,
            result => result.Results.Any(resultItem => string.Equals(resultItem.SourceUri, sourceUri, StringComparison.Ordinal)) &&
                !result.UnavailableAxes.Contains("syntactic"),
            DefaultTimeout).ConfigureAwait(false);

        afterRestart.Results.Any(resultItem => string.Equals(resultItem.SourceUri, sourceUri, StringComparison.Ordinal)).ShouldBeTrue();
        afterRestart.TotalCount.ShouldBeGreaterThanOrEqualTo(beforeRestart.TotalCount);
        afterRestart.UnavailableAxes.ShouldNotContain("syntactic");
    }

    [Fact]
    public async Task RestartTopology_ShouldKeepFailedUnitVisibleAndAllowReingestionAfterRecovery()
    {
        string tenantId = $"tenant-{Guid.NewGuid():N}";
        string caseId = await CreateTenantAndCaseAsync(tenantId).ConfigureAwait(false);
        int failureMode = 1;

        await using ScriptedHttpServer server = await ScriptedHttpServer.StartAsync((_, _) =>
        {
            ScriptedHttpResponse response = Volatile.Read(ref failureMode) == 1
                ? ScriptedHttpResponse.Text("upstream failure", HttpStatusCode.InternalServerError)
                : ScriptedHttpResponse.Text("recovered after restart");
            return ValueTask.FromResult(response);
        }).ConfigureAwait(false);

        string sourceUri = server.GetUri("/durability/failed-unit.txt").ToString();
        _ = await PostUrlIngestionAsync(tenantId, caseId, sourceUri).ConfigureAwait(false);

        FailedUnitsPage failedBeforeRestart = await WaitForFailedUnitsPageAsync(
            tenantId,
            caseId,
            page => page.TotalCount == 1,
            DefaultTimeout).ConfigureAwait(false);

        FailedUnitSummary failedUnit = failedBeforeRestart.Units.Single();
        MemoryUnit failedMemoryUnit = await WaitForMemoryUnitAsync(
            tenantId,
            caseId,
            failedUnit.MemoryUnitId,
            memoryUnit => memoryUnit.Status == MemoryUnitStatus.Failed && memoryUnit.FailureDetails is not null,
            DefaultTimeout).ConfigureAwait(false);

        failedMemoryUnit.Status.ShouldBe(MemoryUnitStatus.Failed);
        failedMemoryUnit.FailureDetails.ShouldNotBeNull();

        _ = await _fixture.RestartTopologyAsync().ConfigureAwait(false);

        FailedUnitsPage failedAfterRestart = await WaitForFailedUnitsPageAsync(
            tenantId,
            caseId,
            page => page.TotalCount == 1,
            DefaultTimeout).ConfigureAwait(false);
        failedAfterRestart.Units.Single().MemoryUnitId.ShouldBe(failedUnit.MemoryUnitId);

        MemoryUnit failedAfterRestartDetail = await WaitForMemoryUnitAsync(
            tenantId,
            caseId,
            failedUnit.MemoryUnitId,
            memoryUnit => memoryUnit.Status == MemoryUnitStatus.Failed && memoryUnit.FailureDetails is not null,
            DefaultTimeout).ConfigureAwait(false);
        failedAfterRestartDetail.FailureDetails.ShouldNotBeNull();

        Interlocked.Exchange(ref failureMode, 0);

        string reingestionWorkflowId = await ReingestFailedUnitAsync(tenantId, caseId, failedUnit.MemoryUnitId).ConfigureAwait(false);
        reingestionWorkflowId.ShouldNotBeNullOrWhiteSpace();

        await WaitForWorkflowRuntimeStatusAsync(tenantId, reingestionWorkflowId, "Completed", DefaultTimeout).ConfigureAwait(false);

        (string recoveredSyntacticKey, string recoveredSemanticKey) = await WaitForBackendWritesAsync(
            tenantId,
            caseId,
            sourceUri).ConfigureAwait(false);

        string recoveredMemoryUnitId = recoveredSyntacticKey.Split(':').Last();
        recoveredMemoryUnitId.ShouldBe(failedUnit.MemoryUnitId);
        IndexSchemaDefinitions.TryParseSemanticMemoryUnitId(
            tenantId,
            recoveredSemanticKey,
            out string recoveredSemanticMemoryUnitId).ShouldBeTrue();
        recoveredSemanticMemoryUnitId.ShouldBe(failedUnit.MemoryUnitId);

        _ = await WaitForSingleDedupValueAsync(tenantId, caseId, failedUnit.MemoryUnitId, DefaultTimeout).ConfigureAwait(false);

        FailedUnitsPage cleared = await WaitForFailedUnitsPageAsync(
            tenantId,
            caseId,
            page => page.TotalCount == 0,
            DefaultTimeout).ConfigureAwait(false);
        cleared.TotalCount.ShouldBe(0);
    }

    [Fact]
    public async Task RestartTopology_ShouldPreserveIndexedRedisBackedDataAcrossControlledRestart()
    {
        string tenantId = $"tenant-{Guid.NewGuid():N}";
        string caseId = await CreateTenantAndCaseAsync(tenantId).ConfigureAwait(false);
        string sourceUri = $"file:///{Guid.NewGuid():N}.txt";

        _ = await PostInlineIngestionAsync(
            tenantId,
            caseId,
            sourceUri,
            "Controlled restart durability validation document.").ConfigureAwait(false);

        (string syntacticKey, string semanticKey) = await WaitForBackendWritesAsync(tenantId, caseId, sourceUri).ConfigureAwait(false);
        string memoryUnitId = syntacticKey.Split(':').Last();
        string dedupKey = BuildDedupKey(tenantId, caseId, sourceUri);
        _ = await WaitForSingleDedupValueAsync(tenantId, caseId, memoryUnitId, DefaultTimeout).ConfigureAwait(false);

        _ = await _fixture.RestartTopologyAsync().ConfigureAwait(false);

        IDatabase redis = _fixture.RedisConnection.GetDatabase();
        (await redis.KeyExistsAsync(syntacticKey).ConfigureAwait(false)).ShouldBeTrue();
        (await redis.KeyExistsAsync(semanticKey).ConfigureAwait(false)).ShouldBeTrue();
        (await redis.KeyExistsAsync(dedupKey).ConfigureAwait(false)).ShouldBeTrue();
        (await redis.StringGetAsync(dedupKey).ConfigureAwait(false)).ToString().ShouldBe(memoryUnitId);

        // Story 26.2 AC8 (NFR16) — zero memory-unit loss across an AOF-backed restart: the single ingested
        // unit survives with no loss and no duplicate syntactic hash created by the restart/replay.
        string[] survivingSyntacticKeys = await ListKeysAsync($"{tenantId}:mu:*").ConfigureAwait(false);
        survivingSyntacticKeys.Length.ShouldBe(1);
        survivingSyntacticKeys[0].ShouldBe(syntacticKey);

        MemoryUnit indexed = await WaitForMemoryUnitAsync(
            tenantId,
            caseId,
            memoryUnitId,
            memoryUnit => memoryUnit.Status == MemoryUnitStatus.Indexed,
            DefaultTimeout).ConfigureAwait(false);

        indexed.Id.ShouldBe(memoryUnitId);
        indexed.Status.ShouldBe(MemoryUnitStatus.Indexed);
    }

    private async Task<string> CreateTenantAndCaseAsync(string tenantId)
    {
        await EnsureTenantActiveAsync(tenantId).ConfigureAwait(false);
        return await CreateCaseAsync(tenantId).ConfigureAwait(false);
    }

    private async Task EnsureTenantActiveAsync(string tenantId)
    {
        using HttpResponseMessage provisionResponse = await _fixture.MemoriesClient
            .PostAsJsonAsync(
                "/api/v1/tenants",
                new TenantProvisioningInput(tenantId, $"Tenant {tenantId}"),
                MemoriesJsonContext.Options)
            .ConfigureAwait(false);

        provisionResponse.StatusCode.ShouldBe(HttpStatusCode.Accepted);

        DateTimeOffset deadline = DateTimeOffset.UtcNow.Add(DefaultTimeout);
        while (DateTimeOffset.UtcNow < deadline)
        {
            using HttpResponseMessage tenantResponse = await _fixture.MemoriesClient
                .GetAsync($"/api/v1/tenants/{tenantId}")
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

    private async Task<string> CreateCaseAsync(string tenantId)
    {
        using HttpResponseMessage response = await _fixture.MemoriesClient
            .PostAsJsonAsync(
                $"/api/v1/tenants/{tenantId}/cases",
                new CreateCaseInput(tenantId, $"Case {Guid.NewGuid():N}", "Story 6.4 restart validation"),
                MemoriesJsonContext.Options)
            .ConfigureAwait(false);

        response.StatusCode.ShouldBe(HttpStatusCode.Created);

        MemoriesCase? created = await response.Content.ReadFromJsonAsync<MemoriesCase>(MemoriesJsonContext.Options).ConfigureAwait(false);
        created.ShouldNotBeNull();
        created.Id.ShouldNotBeNullOrWhiteSpace();
        return created.Id;
    }

    private async Task<UrlIngestionResponse> PostUrlIngestionAsync(string tenantId, string caseId, string sourceUri)
    {
        using HttpResponseMessage response = await _fixture.MemoriesClient
            .PostAsJsonAsync(
                "/api/v1/ingest/url",
                new UrlIngestionRequest
                {
                    TenantId = tenantId,
                    CaseId = caseId,
                    Url = sourceUri,
                    IngestedBy = "integration@test.local",
                },
                MemoriesJsonContext.Options)
            .ConfigureAwait(false);

        response.StatusCode.ShouldBe(HttpStatusCode.Accepted);
        UrlIngestionResponse? accepted = await response.Content
            .ReadFromJsonAsync<UrlIngestionResponse>(MemoriesJsonContext.Options)
            .ConfigureAwait(false);
        accepted.ShouldNotBeNull();
        accepted.InstanceId.ShouldNotBeNullOrWhiteSpace();
        return accepted;
    }

    private async Task<string> PostInlineIngestionAsync(string tenantId, string caseId, string sourceUri, string content)
    {
        IngestionInput input = new()
        {
            TenantId = tenantId,
            CaseId = caseId,
            SourceUri = sourceUri,
            ContentBytes = Encoding.UTF8.GetBytes(content),
            ContentType = "text/plain",
            SourceType = SourceType.File,
            IngestedBy = "integration@test.local",
        };

        using HttpResponseMessage response = await _fixture.MemoriesClient
            .PostAsJsonAsync("/api/v1/ingest", input, MemoriesJsonContext.Options)
            .ConfigureAwait(false);

        response.StatusCode.ShouldBe(HttpStatusCode.Accepted);
        using JsonDocument document = JsonDocument.Parse(await response.Content.ReadAsStringAsync().ConfigureAwait(false));
        string instanceId = document.RootElement.GetProperty("instanceId").GetString() ?? string.Empty;
        instanceId.ShouldNotBeNullOrWhiteSpace();
        return instanceId;
    }

    private async Task<string> ReingestFailedUnitAsync(string tenantId, string caseId, string memoryUnitId)
    {
        using HttpResponseMessage response = await _fixture.MemoriesClient
            .PostAsync($"/api/v1/tenants/{tenantId}/cases/{caseId}/memory-units/{memoryUnitId}/re-ingest", content: null)
            .ConfigureAwait(false);

        response.StatusCode.ShouldBe(HttpStatusCode.Accepted);

        using JsonDocument document = JsonDocument.Parse(await response.Content.ReadAsStringAsync().ConfigureAwait(false));
        string workflowInstanceId = document.RootElement.GetProperty("newWorkflowInstanceId").GetString() ?? string.Empty;
        workflowInstanceId.ShouldNotBeNullOrWhiteSpace();
        return workflowInstanceId;
    }

    private async Task WaitForWorkflowRuntimeStatusAsync(
        string tenantId,
        string instanceId,
        string expectedRuntimeStatus,
        TimeSpan timeout,
        Func<Task<string>>? failureDiagnostics = null)
    {
        DateTimeOffset deadline = DateTimeOffset.UtcNow.Add(timeout);
        string lastPayload = string.Empty;

        while (DateTimeOffset.UtcNow < deadline)
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, $"/api/v1/ingest/{instanceId}");
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(
                "Bearer",
                AspireIngestionPipelineFixture.MintServerBearer(tenantId));
            using HttpResponseMessage response = await _fixture.MemoriesClient.SendAsync(request).ConfigureAwait(false);
            if (response.StatusCode == HttpStatusCode.OK)
            {
                lastPayload = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                if (ReachedRuntimeStatus(lastPayload, expectedRuntimeStatus))
                {
                    return;
                }

                if (TryReadRuntimeStatus(lastPayload, out string? actualRuntimeStatus)
                    && IsTerminalRuntimeStatus(actualRuntimeStatus))
                {
                    string diagnostics = failureDiagnostics is null
                        ? "Additional diagnostics: not requested."
                        : await CaptureFailureDiagnosticsAsync(failureDiagnostics).ConfigureAwait(false);
                    throw new InvalidOperationException(
                        $"Workflow '{instanceId}' reached unexpected terminal runtimeStatus='{actualRuntimeStatus}' " +
                        $"while waiting for '{expectedRuntimeStatus}'. Payload: {lastPayload}{Environment.NewLine}{diagnostics}");
                }
            }

            await Task.Delay(TimeSpan.FromSeconds(1)).ConfigureAwait(false);
        }

        throw new TimeoutException(
            $"Workflow '{instanceId}' did not reach runtimeStatus='{expectedRuntimeStatus}' within {timeout}. Last payload: {lastPayload}");
    }

    private async Task<string> BuildRestartFailureDiagnosticsAsync(
        string tenantId,
        string caseId,
        string instanceId,
        ScriptedHttpServer server,
        CaseStatusDetail statusBeforeRestart,
        CaseStatusDetail statusAfterRestart,
        int logStartIndex)
    {
        string daprWorkflowState;
        try
        {
            daprWorkflowState = await _fixture
                .GetDaprWorkflowStateDiagnosticAsync(instanceId)
                .ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            daprWorkflowState = $"unavailable ({ex.GetType().Name}: {ex.Message})";
        }

        string currentCounterState;
        try
        {
            CaseIngestionCounts current = await _fixture
                .CreateCaseIngestionCounterActorProxy(tenantId, caseId)
                .GetCountsAsync()
                .ConfigureAwait(false);
            currentCounterState = FormatCounts(current);
        }
        catch (Exception ex)
        {
            currentCounterState = $"unavailable ({ex.GetType().Name}: {ex.Message})";
        }

        IReadOnlyList<AspireIngestionPipelineFixture.CapturedLogEntry> allLogs = _fixture.GetLogEntriesSince(logStartIndex);
        AspireIngestionPipelineFixture.CapturedLogEntry[] relevantLogs =
        [
            .. allLogs.Where(entry =>
                    (entry.Level >= Microsoft.Extensions.Logging.LogLevel.Warning
                        && !entry.Message.StartsWith("__hexalith_activity__", StringComparison.Ordinal))
                    || entry.Message.Contains(instanceId, StringComparison.Ordinal)
                    || entry.Message.Contains(tenantId, StringComparison.Ordinal)
                    || entry.Message.Contains(caseId, StringComparison.Ordinal))
                .TakeLast(40),
        ];
        if (relevantLogs.Length == 0)
        {
            relevantLogs = [.. allLogs.TakeLast(40)];
        }

        string formattedLogs = relevantLogs.Length == 0
            ? "n/a"
            : string.Join(
                Environment.NewLine,
                relevantLogs.Select(entry => $"[{entry.Level}] {entry.Category}: {entry.Message}"));

        return $"""
            Restart failure diagnostics:
            Dapr workflow state: {daprWorkflowState}
            Scripted HTTP request count: {server.RequestCount}
            Counter before restart: {FormatCounts(statusBeforeRestart)}
            Counter after restart: {FormatCounts(statusAfterRestart)}
            Counter at failure: {currentCounterState}
            Relevant captured logs:
            {formattedLogs}
            """;
    }

    private static async Task<string> CaptureFailureDiagnosticsAsync(Func<Task<string>> diagnosticFactory)
    {
        try
        {
            return await diagnosticFactory().ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            return $"Additional diagnostic capture failed ({ex.GetType().Name}: {ex.Message}).";
        }
    }

    private static string FormatCounts(CaseStatusDetail status)
        => $"queued={status.QueuedCount}, extracting={status.ExtractingCount}, embedding={status.EmbeddingCount}, indexing={status.IndexingCount}";

    private static string FormatCounts(CaseIngestionCounts counts)
        => $"queued={counts.Queued}, extracting={counts.Extracting}, embedding={counts.Embedding}, indexing={counts.Indexing}";

    private static bool TryReadRuntimeStatus(string payload, out string runtimeStatus)
    {
        using JsonDocument document = JsonDocument.Parse(payload);
        if (document.RootElement.TryGetProperty("runtimeStatus", out JsonElement value))
        {
            if (value.ValueKind == JsonValueKind.String && !string.IsNullOrWhiteSpace(value.GetString()))
            {
                runtimeStatus = value.GetString()!;
                return true;
            }

            if (value.ValueKind == JsonValueKind.Number && value.TryGetInt32(out int ordinal))
            {
                runtimeStatus = ordinal switch
                {
                    3 => "Completed",
                    5 => "Failed",
                    6 => "Canceled",
                    7 => "Terminated",
                    _ => string.Empty,
                };
                return runtimeStatus.Length > 0;
            }
        }

        runtimeStatus = string.Empty;
        return false;
    }

    private static bool IsTerminalRuntimeStatus(string runtimeStatus)
        => string.Equals(runtimeStatus, "Completed", StringComparison.OrdinalIgnoreCase)
            || string.Equals(runtimeStatus, "Failed", StringComparison.OrdinalIgnoreCase)
            || string.Equals(runtimeStatus, "Canceled", StringComparison.OrdinalIgnoreCase)
            || string.Equals(runtimeStatus, "Terminated", StringComparison.OrdinalIgnoreCase);

    private static bool ReachedRuntimeStatus(string payload, string expectedRuntimeStatus)
    {
        using JsonDocument document = JsonDocument.Parse(payload);
        JsonElement root = document.RootElement;

        if (TryReadRuntimeStatus(payload, out string actualRuntimeStatus)
            && string.Equals(actualRuntimeStatus, expectedRuntimeStatus, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (string.Equals(expectedRuntimeStatus, "Completed", StringComparison.OrdinalIgnoreCase)
            && root.TryGetProperty("isWorkflowCompleted", out JsonElement completed)
            && completed.ValueKind == JsonValueKind.True)
        {
            return true;
        }

        return false;
    }

    private static async Task WaitForRequestCountAsync(ScriptedHttpServer server, int minimumRequestCount, TimeSpan timeout)
    {
        ArgumentNullException.ThrowIfNull(server);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(minimumRequestCount);

        DateTimeOffset deadline = DateTimeOffset.UtcNow.Add(timeout);
        while (DateTimeOffset.UtcNow < deadline)
        {
            if (server.RequestCount >= minimumRequestCount)
            {
                return;
            }

            await Task.Delay(TimeSpan.FromMilliseconds(200)).ConfigureAwait(false);
        }

        throw new TimeoutException(
            $"Scripted HTTP server did not observe {minimumRequestCount} request(s) within {timeout}. Current count: {server.RequestCount}.");
    }

    private async Task<(string SyntacticKey, string SemanticKey)> WaitForBackendWritesAsync(
        string tenantId,
        string caseId,
        string sourceUri)
    {
        DateTimeOffset deadline = DateTimeOffset.UtcNow.Add(DefaultTimeout);
        IServer redisServer = _fixture.RedisConnection.GetServer(_fixture.RedisConnection.GetEndPoints().Single());
        string[] lastSyntacticKeys = [];
        string[] lastSemanticKeys = [];
        long lastGraphCount = -1;

        while (DateTimeOffset.UtcNow < deadline)
        {
            string[] syntacticKeys = redisServer.Keys(pattern: $"{tenantId}:mu:*")
                .Select(key => key.ToString())
                .ToArray();
            string[] semanticKeys = redisServer.Keys(pattern: $"{tenantId}:vec:*")
                .Select(key => key.ToString())
                .ToArray();
            long graphCount = await CountGraphNodesAsync(tenantId, caseId, sourceUri).ConfigureAwait(false);

            lastSyntacticKeys = syntacticKeys;
            lastSemanticKeys = semanticKeys;
            lastGraphCount = graphCount;

            if (syntacticKeys.Length == 1 && semanticKeys.Length == 1 && graphCount == 1)
            {
                return (syntacticKeys[0], semanticKeys[0]);
            }

            await Task.Delay(TimeSpan.FromSeconds(2)).ConfigureAwait(false);
        }

        throw new TimeoutException(
            $"The ingestion pipeline did not finish durable writes for tenant '{tenantId}', case '{caseId}' within {DefaultTimeout}. " +
            $"Last observed syntactic keys ({lastSyntacticKeys.Length}): [{string.Join(", ", lastSyntacticKeys)}]; " +
            $"semantic keys ({lastSemanticKeys.Length}): [{string.Join(", ", lastSemanticKeys)}]; " +
            $"graph count: {lastGraphCount}.");
    }

    private async Task<string[]> ListKeysAsync(string pattern)
    {
        IServer redisServer = _fixture.RedisConnection.GetServer(_fixture.RedisConnection.GetEndPoints().Single());
        return await Task.FromResult(redisServer.Keys(pattern: pattern).Select(key => key.ToString()).ToArray()).ConfigureAwait(false);
    }

    private async Task<string[]> WaitForSingleDedupValueAsync(
        string tenantId,
        string caseId,
        string expectedMemoryUnitId,
        TimeSpan timeout)
    {
        string pattern = $"dedup:{tenantId}:{caseId}:*";
        DateTimeOffset deadline = DateTimeOffset.UtcNow.Add(timeout);
        IDatabase redis = _fixture.RedisConnection.GetDatabase();
        string[] lastKeys = [];
        string lastValue = string.Empty;

        while (DateTimeOffset.UtcNow < deadline)
        {
            string[] dedupKeys = await ListKeysAsync(pattern).ConfigureAwait(false);
            lastKeys = dedupKeys;

            if (dedupKeys.Length == 1)
            {
                RedisValue value = await redis.StringGetAsync(dedupKeys[0]).ConfigureAwait(false);
                lastValue = value.ToString();
                if (string.Equals(lastValue, expectedMemoryUnitId, StringComparison.Ordinal))
                {
                    return dedupKeys;
                }
            }

            await Task.Delay(TimeSpan.FromSeconds(1)).ConfigureAwait(false);
        }

        throw new TimeoutException(
            $"Dedup key matching '{pattern}' did not resolve to '{expectedMemoryUnitId}' within {timeout}. " +
            $"Last observed keys ({lastKeys.Length}): [{string.Join(", ", lastKeys)}]; last value: '{lastValue}'.");
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
                .GetAsync($"/api/v1/tenants/{tenantId}/cases/{caseId}/status")
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

    private async Task<FailedUnitsPage> WaitForFailedUnitsPageAsync(
        string tenantId,
        string caseId,
        Func<FailedUnitsPage, bool> predicate,
        TimeSpan timeout)
    {
        DateTimeOffset deadline = DateTimeOffset.UtcNow.Add(timeout);
        while (DateTimeOffset.UtcNow < deadline)
        {
            using HttpResponseMessage response = await _fixture.MemoriesClient
                .GetAsync($"/api/v1/tenants/{tenantId}/cases/{caseId}/failed-units")
                .ConfigureAwait(false);

            if (response.StatusCode == HttpStatusCode.OK)
            {
                FailedUnitsPage? page = await response.Content
                    .ReadFromJsonAsync<FailedUnitsPage>(MemoriesJsonContext.Options)
                    .ConfigureAwait(false);

                if (page is not null && predicate(page))
                {
                    return page;
                }
            }

            await Task.Delay(TimeSpan.FromSeconds(2)).ConfigureAwait(false);
        }

        throw new TimeoutException($"Failed-units endpoint for '{tenantId}/{caseId}' did not satisfy the predicate within {timeout}.");
    }

    private async Task<MemoryUnit> WaitForMemoryUnitAsync(
        string tenantId,
        string caseId,
        string memoryUnitId,
        Func<MemoryUnit, bool> predicate,
        TimeSpan timeout)
    {
        DateTimeOffset deadline = DateTimeOffset.UtcNow.Add(timeout);
        while (DateTimeOffset.UtcNow < deadline)
        {
            using HttpResponseMessage response = await _fixture.MemoriesClient
                .GetAsync($"/api/v1/tenants/{tenantId}/cases/{caseId}/memory-units/{memoryUnitId}")
                .ConfigureAwait(false);

            if (response.StatusCode == HttpStatusCode.OK)
            {
                MemoryUnit? memoryUnit = await response.Content
                    .ReadFromJsonAsync<MemoryUnit>(MemoriesJsonContext.Options)
                    .ConfigureAwait(false);

                if (memoryUnit is not null && predicate(memoryUnit))
                {
                    return memoryUnit;
                }
            }

            await Task.Delay(TimeSpan.FromSeconds(2)).ConfigureAwait(false);
        }

        throw new TimeoutException(
            $"Memory unit '{memoryUnitId}' in '{tenantId}/{caseId}' did not satisfy the predicate within {timeout}.");
    }

    private async Task<TenantEmbeddingConfig> GetTenantEmbeddingConfigAsync(string tenantId)
    {
        using HttpResponseMessage response = await _fixture.MemoriesClient
            .GetAsync($"/api/v1/tenants/{tenantId}/embedding-config")
            .ConfigureAwait(false);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        TenantEmbeddingConfig? config = await response.Content
            .ReadFromJsonAsync<TenantEmbeddingConfig>(MemoriesJsonContext.Options)
            .ConfigureAwait(false);
        config.ShouldNotBeNull();
        return config;
    }

    private async Task UpdateTenantEmbeddingConfigAsync(string tenantId, TenantEmbeddingConfig config)
    {
        using HttpResponseMessage response = await _fixture.MemoriesClient
            .PutAsJsonAsync($"/api/v1/tenants/{tenantId}/embedding-config", config, MemoriesJsonContext.Options)
            .ConfigureAwait(false);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    private async Task<HybridSearchResult> WaitForHybridSearchAsync(
        string tenantId,
        string query,
        Func<HybridSearchResult, bool> predicate,
        TimeSpan timeout)
    {
        string encodedQuery = Uri.EscapeDataString(query);
        DateTimeOffset deadline = DateTimeOffset.UtcNow.Add(timeout);

        while (DateTimeOffset.UtcNow < deadline)
        {
            using HttpResponseMessage response = await _fixture.MemoriesClient
                .GetAsync($"/api/v1/search?tenantId={tenantId}&query={encodedQuery}&axis=hybrid&axes=syntactic")
                .ConfigureAwait(false);

            if (response.StatusCode == HttpStatusCode.OK)
            {
                HybridSearchResult? result = await response.Content
                    .ReadFromJsonAsync<HybridSearchResult>(MemoriesJsonContext.Options)
                    .ConfigureAwait(false);

                if (result is not null && predicate(result))
                {
                    return result;
                }
            }

            await Task.Delay(TimeSpan.FromSeconds(1)).ConfigureAwait(false);
        }

        throw new TimeoutException($"Hybrid search for tenant '{tenantId}' did not satisfy the predicate within {timeout}.");
    }

    private async Task<long> CountGraphNodesAsync(string tenantId, string caseId, string sourceUri)
    {
        FalkorDB falkor = new(_fixture.FalkorDbConnection.GetDatabase());
        ResultSet result = await falkor.SelectGraph(tenantId).QueryAsync(
            "MATCH (m:MemoryUnit {caseId: $caseId, sourceUri: $sourceUri}) RETURN count(m) as cnt",
            new Dictionary<string, object>
            {
                ["caseId"] = caseId,
                ["sourceUri"] = sourceUri,
            }).ConfigureAwait(false);

        result.Count.ShouldBe(1);
        IEnumerator<Record> enumerator = result.GetEnumerator();
        enumerator.MoveNext().ShouldBeTrue();
        return enumerator.Current.GetValue<long>("cnt");
    }

    private static string BuildDedupKey(string tenantId, string caseId, string sourceUri)
    {
        byte[] hash = SHA256.HashData(Encoding.UTF8.GetBytes(sourceUri));
        return $"dedup:{tenantId}:{caseId}:{Convert.ToHexString(hash).ToLowerInvariant()}";
    }
}

#pragma warning restore xUnit1030
