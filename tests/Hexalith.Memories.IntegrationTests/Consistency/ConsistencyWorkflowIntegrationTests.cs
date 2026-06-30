// <copyright file="ConsistencyWorkflowIntegrationTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.IntegrationTests.Consistency;

/// <summary>
/// Story 8.2 — end-to-end integration for the consistency verify / repair workflows against
/// the Aspire-hosted Redis Stack + FalkorDB + Dapr sidecar.
/// </summary>
/// <remarks>
/// Skip-gated with an updated reason per Phase C landing:
/// <list type="bullet">
///   <item><description>
///     The first two scenarios (clean-tenant verify + seed-orphan detection) exercise the
///     read-only verification path and could run against the Aspire fixture once Docker is
///     available in the runner.
///   </description></item>
///   <item><description>
///     The third scenario (repair convergence) REQUIRES
///     <c>SemanticIndexer.ReIndexFromSyntacticAsync</c> to regenerate embeddings through the
///     rate-limiter actor — that wiring is deferred to a Phase D follow-up story. Until then,
///     a graph-only re-merge path is the only end-to-end convergence scenario the server can
///     deliver.
///   </description></item>
/// </list>
/// Leaving all three tests skipped keeps the integration build green while documenting the
/// follow-up work explicitly.
/// </remarks>
[Trait("Category", "Integration")]
public class ConsistencyWorkflowIntegrationTests
{
    // Blueprint — uncomment when the Aspire fixture is available AND SemanticIndexer regeneration ships:
    //
    // using System.Net;
    // using System.Net.Http.Json;
    // using Hexalith.Memories.Contracts.V1;
    // using Hexalith.Memories.IntegrationTests.Fixtures;
    // using Shouldly;

    /// <summary>Verification on a clean tenant reports zero discrepancies.</summary>
    [RunnableSkippedFact("Story 8.2 Phase C: execute only in the Docker-enabled Aspire integration lane; the fixture exists but the default test lane does not provision containers.")]
    public async Task VerifyOnCleanTenant_ReportsZeroDiscrepancies()
    {
        await Task.Yield();
        Assert.Fail(
            "Integration scenario (8.2-INT-001). Exercise via `dotnet test --filter Category=Integration` "
            + "in a Docker-enabled CI job; enable by deleting the Skip argument.");
    }

    /// <summary>Manually-seeded orphan is detected with the correct recommendation.</summary>
    [RunnableSkippedFact("Story 8.2 Phase C: execute only in the Docker-enabled Aspire integration lane; the fixture exists but the default test lane does not provision containers.")]
    public async Task SeedOrphanThenVerify_ReportsOneDiscrepancyWithCorrectRecommendation()
    {
        await Task.Yield();
        Assert.Fail(
            "Integration scenario (8.2-INT-002). Exercise via `dotnet test --filter Category=Integration` "
            + "in a Docker-enabled CI job.");
    }

    /// <summary>Repair converges from a seeded-orphan state to fully consistent.</summary>
    [RunnableSkippedFact("Story 8.2 Phase C: SemanticIndexer.ReIndexFromSyntacticAsync throws NotSupportedException until embedding regeneration wiring lands (Phase D follow-up).")]
    public async Task SeedOrphanThenRepair_ConvergesToConsistent()
    {
        await Task.Yield();
        Assert.Fail(
            "Integration scenario (8.2-INT-003). Requires SemanticIndexer embedding-regeneration path + "
            + "EmbeddingClient/rate-limiter actor injection; tracked as the Phase D follow-up described in "
            + "Story 8.2 Dev Agent Record.");
    }
}
