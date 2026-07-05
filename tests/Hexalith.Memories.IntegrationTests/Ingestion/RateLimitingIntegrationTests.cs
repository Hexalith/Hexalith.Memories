// <copyright file="RateLimitingIntegrationTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.IntegrationTests.Ingestion;

using Hexalith.Memories.IntegrationTests.Fixtures;

/// <summary>
/// Integration harness placeholders for per-tenant rate limiting and starvation prevention.
/// Story 23.3 proves provider 429 recovery at unit level with DAPR workflow timer mocks, activity
/// actor-feedback assertions, and actor reopen-math tests. These scenarios remain skipped until the
/// Aspire fixture can inject a deterministic 429-then-success embedding provider across a real sidecar run.
/// </summary>
[Collection("AspireIngestionPipeline")]
[Trait("Category", "Integration")]
public sealed class RateLimitingIntegrationTests
{
    private readonly AspireIngestionPipelineFixture _fixture;

    public RateLimitingIntegrationTests(AspireIngestionPipelineFixture fixture) => _fixture = fixture;

    [RunnableSkippedFact("Requires Aspire fixture support for per-tenant ceiling overrides.")]
    public void TwoTenantIsolation_ShouldEnforceIndependentCeilings()
    {
        // AC1, AC10: two tenants with different ceilings (500 vs 3000) run concurrently.
        // Assert each actor holds independent RateLimitState; t1 throttles at 500, t2 does not.
        _ = _fixture;
    }

    [RunnableSkippedFact("Requires Aspire fixture support for per-tenant ceiling overrides.")]
    public void BatchVsSingleIngest_ShouldNotStarveRealTimeTenant()
    {
        // AC2: t1 submits 500-file batch, t2 submits single file within 1 s.
        // Assert t2 P50 latency stays within 2× single-tenant baseline.
        _ = _fixture;
    }

    [RunnableSkippedFact("Story 23.3 unit coverage exists; integration awaits Aspire fixture 429-then-success provider injection.")]
    public void Provider429_ShouldReportToActorAndRetry()
    {
        // Provider returns 429 with Retry-After, workflow waits with durable timer, second call succeeds.
        // Assert memory unit ends Indexed, ReportRateLimitedAsync is activity-owned and called once per
        // provider 429, actor state pauses budget until the Retry-After instant, and no failed-unit is written.
        _ = _fixture;
    }
}
