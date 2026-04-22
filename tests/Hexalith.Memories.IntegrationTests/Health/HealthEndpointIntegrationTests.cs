// <copyright file="HealthEndpointIntegrationTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.IntegrationTests.Health;

using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

using Hexalith.Memories.IntegrationTests.Fixtures;

using Shouldly;

/// <summary>
/// Story 8.1 Task 6.1 — Aspire-based end-to-end checks for the <c>/health</c>, <c>/alive</c>,
/// and <c>/ready</c> endpoints. Complements the in-memory
/// <c>ReadyEndpointAggregationTests</c> (Server.Tests, Tier-1): this suite exercises the
/// REAL backend health checks against the Aspire-hosted Redis Stack + FalkorDB + Dapr
/// sidecar, so only Docker-equipped environments execute it.
/// <para>
/// The backend-down scenario (Task 6.1 bullet #2) is skipped: the current
/// <see cref="AspireIngestionPipelineFixture"/> does not expose a stop-resource primitive,
/// and <see cref="ReadyEndpointAggregationTests"/> already proves the aggregate-Degraded
/// behavior wire-to-wire via an in-memory DI override.
/// </para>
/// </summary>
[Collection("AspireIngestionPipeline")]
[Trait("Category", "Integration")]
public sealed class HealthEndpointIntegrationTests
{
    private readonly AspireIngestionPipelineFixture _fixture;

    private static readonly TimeSpan HealthTransitionTimeout = TimeSpan.FromSeconds(45);
    private static readonly TimeSpan HealthPollInterval = TimeSpan.FromSeconds(1);

    public HealthEndpointIntegrationTests(AspireIngestionPipelineFixture fixture) => _fixture = fixture;

    [Fact]
    public async Task ReadyEndpoint_AllHealthy_Returns200WithFiveEntries()
    {
        using HttpResponseMessage response = await _fixture.MemoriesClient.GetAsync("/ready");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        response.Content.Headers.ContentType?.MediaType.ShouldBe("application/json");

        JsonElement root = await response.Content.ReadFromJsonAsync<JsonElement>();
        root.GetProperty("schemaVersion").GetInt32().ShouldBe(1);
        root.GetProperty("status").GetString().ShouldBe("Healthy");

        JsonElement entries = root.GetProperty("entries");
        foreach (string name in new[] { "dapr-sidecar", "dapr-statestore", "redisearch", "redis-vector", "falkordb" })
        {
            entries.GetProperty(name).GetProperty("status").GetString().ShouldBe("Healthy");
        }
    }

    [Fact]
    public async Task AliveEndpoint_Default_Returns200WithSidecarAndSelfOnly()
    {
        using HttpResponseMessage response = await _fixture.MemoriesClient.GetAsync("/alive");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        JsonElement root = await response.Content.ReadFromJsonAsync<JsonElement>();
        root.GetProperty("status").GetString().ShouldBe("Healthy");

        JsonElement entries = root.GetProperty("entries");
        entries.TryGetProperty("self", out _).ShouldBeTrue();
        entries.TryGetProperty("dapr-sidecar", out _).ShouldBeTrue();
        entries.TryGetProperty("redisearch", out _).ShouldBeFalse();
        entries.TryGetProperty("redis-vector", out _).ShouldBeFalse();
        entries.TryGetProperty("falkordb", out _).ShouldBeFalse();
    }

    [Fact]
    [Trait("Category", "IntegrationSlow")]
    public async Task ReadyEndpoint_FalkorDbDown_ReturnsDegradedWithCapabilities()
    {
        try
        {
            using CancellationTokenSource cts = new(HealthTransitionTimeout);
            await _fixture.StopFalkorDbContainerAsync(cts.Token);

            JsonElement root = await WaitForEndpointAsync(
                "/ready",
                HttpStatusCode.OK,
                "Degraded",
                cts.Token);

            JsonElement falkor = root.GetProperty("entries").GetProperty("falkordb");
            falkor.GetProperty("status").GetString().ShouldBe("Degraded");
            falkor.GetProperty("affectedCapabilities").EnumerateArray()
                .Select(e => e.GetString()!)
                .ShouldContain("graph-traversal");
        }
        finally
        {
            _ = await _fixture.RestartTopologyAsync();
        }
    }

    [Fact]
    [Trait("Category", "IntegrationSlow")]
    public async Task AliveEndpoint_DaprSidecarDown_Returns503Unhealthy()
    {
        try
        {
            using CancellationTokenSource cts = new(HealthTransitionTimeout);
            await _fixture.StopDaprSidecarAsync(cts.Token);

            JsonElement root = await WaitForEndpointAsync(
                "/alive",
                HttpStatusCode.ServiceUnavailable,
                "Unhealthy",
                cts.Token);

            JsonElement sidecar = root.GetProperty("entries").GetProperty("dapr-sidecar");
            sidecar.GetProperty("status").GetString().ShouldBe("Unhealthy");
        }
        finally
        {
            _ = await _fixture.RestartTopologyAsync();
        }
    }

    private async Task<JsonElement> WaitForEndpointAsync(
        string path,
        HttpStatusCode expectedStatusCode,
        string expectedStatus,
        CancellationToken cancellationToken)
    {
        DateTimeOffset deadline = DateTimeOffset.UtcNow.Add(HealthTransitionTimeout);
        HttpStatusCode? lastStatusCode = null;
        string? lastBody = null;
        Exception? lastException = null;

        while (DateTimeOffset.UtcNow < deadline)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                using HttpResponseMessage response = await _fixture.MemoriesClient.GetAsync(path, cancellationToken).ConfigureAwait(false);
                lastStatusCode = response.StatusCode;
                lastBody = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

                if (response.StatusCode == expectedStatusCode)
                {
                    using JsonDocument document = JsonDocument.Parse(lastBody);
                    JsonElement root = document.RootElement.Clone();
                    if (root.GetProperty("status").GetString() == expectedStatus)
                    {
                        return root;
                    }
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                lastException = ex;
            }

            await Task.Delay(HealthPollInterval, cancellationToken).ConfigureAwait(false);
        }

        throw new TimeoutException(
            $"Endpoint '{path}' did not reach status {expectedStatusCode}/{expectedStatus} within {HealthTransitionTimeout}. " +
            $"Last HTTP status: {lastStatusCode?.ToString() ?? "n/a"}. " +
            $"Last body: {lastBody ?? "n/a"}. " +
            $"Last exception: {lastException?.Message ?? "n/a"}.");
    }
}
