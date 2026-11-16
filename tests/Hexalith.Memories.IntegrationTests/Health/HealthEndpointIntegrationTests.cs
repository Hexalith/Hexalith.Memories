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
public sealed partial class HealthEndpointIntegrationTests
{
    private readonly AspireIngestionPipelineFixture _fixture;

    private static readonly TimeSpan HealthTransitionTimeout = TimeSpan.FromSeconds(45);
    private static readonly TimeSpan HealthPollInterval = TimeSpan.FromSeconds(1);
    private static readonly TimeSpan HealthProbeTimeout = TimeSpan.FromSeconds(5);

    public HealthEndpointIntegrationTests(AspireIngestionPipelineFixture fixture) => _fixture = fixture;

    private async Task<JsonElement> WaitForEndpointAsync(
        string path,
        HttpStatusCode expectedStatusCode,
        string expectedStatus,
        CancellationToken cancellationToken = default)
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
                using CancellationTokenSource probeCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                probeCts.CancelAfter(HealthProbeTimeout);
                using HttpResponseMessage response = await _fixture.MemoriesClient.GetAsync(path, probeCts.Token).ConfigureAwait(false);
                lastStatusCode = response.StatusCode;
                lastBody = await response.Content.ReadAsStringAsync(probeCts.Token).ConfigureAwait(false);

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
            catch (OperationCanceledException ex) when (!cancellationToken.IsCancellationRequested)
            {
                lastException = ex;
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
