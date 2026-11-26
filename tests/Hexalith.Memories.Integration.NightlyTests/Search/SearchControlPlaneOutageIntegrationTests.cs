// <copyright file="SearchControlPlaneOutageIntegrationTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.IntegrationTests.Search;

using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

using Hexalith.Memories.Contracts.V1;
using Hexalith.Memories.IntegrationTests.Fixtures;

using Shouldly;

using StackExchange.Redis;

[Collection("AspireIngestionPipeline")]
[Trait("Category", "IntegrationSlow")]
public sealed class SearchControlPlaneOutageIntegrationTests(AspireIngestionPipelineFixture fixture)
{
    [Fact]
    public async Task HybridSearch_AllStorageBackendsStopped_FailsClosedAndRecovers()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        IngestionIntegrationTestDriver driver = new(fixture);
        string marker = Guid.NewGuid().ToString("N");
        string tenant = $"tenant-search-outage-{marker[..10]}";
        string caseId = await driver.CreateTenantAndCaseAsync(tenant, cancellationToken: cancellationToken);
        string sourceUri = $"file:///{marker}-search-outage.txt";
        string workflow = await driver.PostInlineIngestionAsync(tenant, caseId, sourceUri, marker, cancellationToken: cancellationToken);
        string completed = await driver.WaitForWorkflowRuntimeStatusAsync(tenant, workflow, "Completed", cancellationToken: cancellationToken);
        string unitId = JsonSerializer.Deserialize<IngestionWorkflowStatus>(completed, MemoriesJsonContext.Options)
            .ShouldNotBeNull().MemoryUnitId.ShouldNotBeNull();
        (string syntacticKey, string semanticKey) = await driver.WaitForSingleBackendWriteAsync(tenant, caseId, sourceUri, cancellationToken: cancellationToken);
        HashEntry[] syntacticBefore = await driver.ReadHashSnapshotAsync(syntacticKey);
        HashEntry[] semanticBefore = await driver.ReadHashSnapshotAsync(semanticKey);
        HybridSearchResult before = await driver.GetHybridSearchAsync(tenant, caseId, unitId, marker, cancellationToken);
        AssertAllAxesAvailable(before, unitId);
        (await driver.CountGraphNodesAsync(tenant, caseId, sourceUri)).ShouldBe(1);

        await fixture.StopFalkorDbContainerAsync(cancellationToken);
        try
        {
            await fixture.SetRedisStackRunningAsync(running: false, cancellationToken: cancellationToken);
            using var outageBudget = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            outageBudget.CancelAfter(TimeSpan.FromSeconds(15));
            using HttpResponseMessage unavailable = await fixture.MemoriesClient.GetAsync(
                $"/api/v1/search?tenantId={tenant}&caseId={caseId}&query={marker}&axis=hybrid" +
                $"&axes=syntactic,semantic,graph&graphStartNodeId={unitId}&depth=1",
                outageBudget.Token);
            unavailable.StatusCode.ShouldBe(HttpStatusCode.ServiceUnavailable);
            unavailable.Headers.RetryAfter.ShouldNotBeNull().Delta.ShouldBe(TimeSpan.FromSeconds(5));
            (await unavailable.Content.ReadFromJsonAsync<ErrorResponse>(MemoriesJsonContext.Options, outageBudget.Token))
                .ShouldNotBeNull().Code.ShouldBe("DAPR_UNAVAILABLE");
        }
        finally
        {
            try
            {
                await fixture.SetRedisStackRunningAsync(running: true, cancellationToken: CancellationToken.None);
            }
            finally
            {
                await fixture.StartFalkorDbContainerAsync();
            }
        }

        (await fixture.GetTenantRegistryEntryAsync(tenant, cancellationToken)).ShouldNotBeNull().Tenant.Status.ShouldBe(TenantStatus.Active);
        (await driver.ReadHashSnapshotAsync(syntacticKey)).ShouldBe(syntacticBefore);
        (await driver.ReadHashSnapshotAsync(semanticKey)).ShouldBe(semanticBefore);
        (await driver.CountGraphNodesAsync(tenant, caseId, sourceUri)).ShouldBe(1);
        HybridSearchResult recovered = await driver.GetHybridSearchAsync(tenant, caseId, unitId, marker, cancellationToken);
        AssertAllAxesAvailable(recovered, unitId);
    }

    private static void AssertAllAxesAvailable(HybridSearchResult response, string memoryUnitId)
    {
        response.Degraded.ShouldBeFalse();
        response.UnavailableAxes.ShouldBeEmpty();
        FusedScoredResult hit = response.Results.Single(item => item.MemoryUnitId == memoryUnitId);
        hit.SyntacticScore.ShouldNotBeNull();
        hit.SemanticScore.ShouldNotBeNull();
        hit.GraphScore.ShouldNotBeNull();
    }
}
