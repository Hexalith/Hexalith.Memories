// <copyright file="RateLimitingIntegrationTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.IntegrationTests.Ingestion;

using Hexalith.Memories.IntegrationTests.Fixtures;

/// <summary>
/// Story 6.2 integration tests for per-tenant rate limiting and starvation prevention.
/// All scenarios are <c>[Fact(Skip)]</c> — Story 6.3 unskips them once its retry harness provides
/// a deterministic 429-producing provider test double and the Aspire fixture is wired to tenant
/// configuration overrides.
/// </summary>
[Collection("AspireIngestionPipeline")]
[Trait("Category", "Integration")]
public sealed class RateLimitingIntegrationTests
{
    private readonly AspireIngestionPipelineFixture _fixture;

    public RateLimitingIntegrationTests(AspireIngestionPipelineFixture fixture) => _fixture = fixture;

    [RunnableSkippedFact("Unskipped by Story 6.3 — requires Aspire fixture + 429 test-double from retry harness.")]
    public void TwoTenantIsolation_ShouldEnforceIndependentCeilings()
    {
        // AC1, AC10: two tenants with different ceilings (500 vs 3000) run concurrently.
        // Assert each actor holds independent RateLimitState; t1 throttles at 500, t2 does not.
        _ = _fixture;
    }

    [RunnableSkippedFact("Unskipped by Story 6.3 — requires Aspire fixture + 429 test-double from retry harness.")]
    public void BatchVsSingleIngest_ShouldNotStarveRealTimeTenant()
    {
        // AC2: t1 submits 500-file batch, t2 submits single file within 1 s.
        // Assert t2 P50 latency stays within 2× single-tenant baseline.
        _ = _fixture;
    }

    [RunnableSkippedFact("Unskipped by Story 6.3 — requires Aspire fixture + 429 test-double from retry harness.")]
    public void Provider429_ShouldReportToActorAndRetry()
    {
        // AC3, AC4: provider returns 429 for first 3 attempts, success on 4th.
        // Assert memory unit ends Indexed, ReportRateLimitedAsync called with Retry-After value,
        // actor state pauses budget for Retry-After window.
        _ = _fixture;
    }
}
