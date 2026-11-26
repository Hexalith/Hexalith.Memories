// <copyright file="DegradationIntegrationTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.IntegrationTests.Search;

using System.Net;
using System.Net.Http.Json;

using Hexalith.Memories.Contracts.V1;
using Hexalith.Memories.IntegrationTests.Fixtures;

using Shouldly;

using StackExchange.Redis;

/// <summary>Contains the nightly integration cases for DegradationIntegrationTests.</summary>
public sealed partial class DegradationIntegrationTests
{
    [Fact]
    [Trait("Category", "IntegrationSlow")]
    public async Task HybridSearch_FalkorDbStopped_ShouldDegradeToSyntacticAndSemantic()
    {
        IngestionIntegrationTestDriver driver = new(_fixture);
        string unique = Guid.NewGuid().ToString("N");
        string tenantId = $"tenant-falkor-{unique[..10]}";
        string sourceUri = $"file:///{unique}-falkor-recovery.txt";
        string canary = $"Falkor recovery canary {unique}";
        string caseId = await driver.CreateTenantAndCaseAsync(tenantId);
        string instanceId = await driver.PostInlineIngestionAsync(tenantId, caseId, sourceUri, canary);
        string workflow = await driver.WaitForWorkflowRuntimeStatusAsync(tenantId, instanceId, "Completed");
        string memoryUnitId = IngestionIntegrationTestDriver.TryExtractMemoryUnitId(workflow) ?? instanceId;
        (string syntacticKey, string semanticKey) = await driver.WaitForSingleBackendWriteAsync(
            tenantId,
            caseId,
            sourceUri);

        HashEntry[] syntacticBefore = await driver.ReadHashSnapshotAsync(syntacticKey);
        HashEntry[] semanticBefore = await driver.ReadHashSnapshotAsync(semanticKey);
        HybridSearchResult before = await driver.GetHybridSearchAsync(tenantId, caseId, memoryUnitId, unique);
        before.Degraded.ShouldBeFalse();
        before.UnavailableAxes.ShouldBeEmpty();
        before.Results.ShouldContain(result => result.MemoryUnitId == memoryUnitId && result.ContentSnippet.Contains(unique, StringComparison.Ordinal));
        (await driver.CountGraphNodesAsync(tenantId, caseId, sourceUri)).ShouldBe(1);

        bool stopped = false;
        try
        {
            await _fixture.StopFalkorDbContainerAsync();
            stopped = true;

            HybridSearchResult degraded = await driver.GetHybridSearchAsync(tenantId, caseId, memoryUnitId, unique);
            degraded.Degraded.ShouldBeTrue();
            degraded.UnavailableAxes.ShouldBe(["graph"]);
            degraded.Results.ShouldContain(result =>
                result.MemoryUnitId == memoryUnitId &&
                result.ContentSnippet.Contains(unique, StringComparison.Ordinal) &&
                (result.SyntacticScore.HasValue || result.SemanticScore.HasValue));

            using HttpResponseMessage traversal = await _fixture.MemoriesClient.GetAsync(
                $"/api/v1/tenants/{tenantId}/traverse?startNodeId={memoryUnitId}&depth=1");
            traversal.StatusCode.ShouldBe(HttpStatusCode.ServiceUnavailable);
            ErrorResponse? graphError = await traversal.Content.ReadFromJsonAsync<ErrorResponse>(MemoriesJsonContext.Options);
            graphError.ShouldNotBeNull();
            graphError.Code.ShouldBe("GRAPH_UNAVAILABLE");
            traversal.Headers.GetValues("Retry-After").Single().ShouldBe("5");

            TenantSummary tenant = await driver.WaitForNewestTenantSummaryAsync(
                tenantId,
                item => item.IndexStatus.Graph == IndexHealth.Unknown);
            tenant.IndexStatus.Graph.ShouldBe(IndexHealth.Unknown);
            tenant.IndexStatus.Syntactic.ShouldBe(IndexHealth.Ready);
            tenant.IndexStatus.Semantic.ShouldBe(IndexHealth.Ready);

            (await driver.ReadHashSnapshotAsync(syntacticKey)).ShouldBe(syntacticBefore);
            (await driver.ReadHashSnapshotAsync(semanticKey)).ShouldBe(semanticBefore);
        }
        finally
        {
            if (stopped)
            {
                await _fixture.StartFalkorDbContainerAsync();
            }
        }

        (await driver.CountGraphNodesAsync(tenantId, caseId, sourceUri)).ShouldBe(1);
        (await driver.ReadHashSnapshotAsync(syntacticKey)).ShouldBe(syntacticBefore);
        (await driver.ReadHashSnapshotAsync(semanticKey)).ShouldBe(semanticBefore);
        HybridSearchResult recovered = await driver.GetHybridSearchAsync(tenantId, caseId, memoryUnitId, unique);
        recovered.Degraded.ShouldBeFalse();
        recovered.UnavailableAxes.ShouldBeEmpty();
        recovered.Results.ShouldContain(result => result.MemoryUnitId == memoryUnitId);
    }
}
