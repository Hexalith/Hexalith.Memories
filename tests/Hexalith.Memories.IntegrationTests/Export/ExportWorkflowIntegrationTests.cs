// <copyright file="ExportWorkflowIntegrationTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.IntegrationTests.Export;

/// <summary>
/// Story 8.3 — end-to-end integration for the case + tenant export endpoints against the
/// Aspire-hosted Redis Stack + FalkorDB + Dapr sidecar.
/// </summary>
/// <remarks>
/// Skip-gated with the same protocol as <see cref="Consistency.ConsistencyWorkflowIntegrationTests"/>:
/// the Aspire fixture has an unresolved CS0311 build issue tracked from Story 5.6. Both scenarios
/// target read-only paths (ingest → export → assert) that could be enabled once the fixture builds
/// in CI.
/// </remarks>
[Trait("Category", "Integration")]
public class ExportWorkflowIntegrationTests
{
    /// <summary>Ingest three units into one case, export the case, assert manifest + units + edges round-trip.</summary>
    [RunnableSkippedFact("Aspire fixture build failure tracked in 5.6 Dev Notes")]
    public async Task IngestThreeUnits_ExportCase_RoundTripsThroughStream()
    {
        await Task.Yield();
        Assert.Fail(
            "Integration scenario (8.3-INT-001). Exercise via `dotnet test --filter Category=Integration` "
            + "in a Docker-enabled CI job once the Aspire fixture CS0311 is resolved.");
    }

    /// <summary>Ingest two cases of units, export the tenant, assert all cases + units + edges are present.</summary>
    [RunnableSkippedFact("Aspire fixture build failure tracked in 5.6 Dev Notes")]
    public async Task IngestTwoCases_ExportTenant_ReturnsEverything()
    {
        await Task.Yield();
        Assert.Fail(
            "Integration scenario (8.3-INT-002). Exercise via `dotnet test --filter Category=Integration` "
            + "in a Docker-enabled CI job once the Aspire fixture CS0311 is resolved.");
    }
}
