// <copyright file="SyntacticSearchControlPlaneOutageIntegrationTests.cs" company="ITANEO">
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
public sealed class SyntacticSearchControlPlaneOutageIntegrationTests(AspireIngestionPipelineFixture fixture)
{
    [Fact]
    public async Task SyntacticSearch_SharedRedisStopped_FailsClosedWhileGraphRemainsAvailable()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        IngestionIntegrationTestDriver driver = new(fixture);
        string marker = Guid.NewGuid().ToString("N");
        string tenant = $"tenant-syntactic-outage-{marker[..10]}";
        string caseId = await driver.CreateTenantAndCaseAsync(tenant, cancellationToken: cancellationToken);
        string sourceUri = $"file:///{marker}-syntactic-outage.txt";
        string workflow = await driver.PostInlineIngestionAsync(tenant, caseId, sourceUri, marker, cancellationToken);
        string completed = await driver.WaitForWorkflowRuntimeStatusAsync(tenant, workflow, "Completed", cancellationToken: cancellationToken);
        string unitId = JsonSerializer.Deserialize<IngestionWorkflowStatus>(completed, MemoriesJsonContext.Options)
            .ShouldNotBeNull().MemoryUnitId.ShouldNotBeNull();
        (string syntacticKey, _) = await driver.WaitForSingleBackendWriteAsync(tenant, caseId, sourceUri, cancellationToken: cancellationToken);
        HashEntry[] syntacticBefore = await driver.ReadHashSnapshotAsync(syntacticKey);
        string path = $"/api/v1/search?tenantId={tenant}&caseId={caseId}&query={marker}&axis=syntactic";
        await AssertIndexedAsync();

        try
        {
            await fixture.SetRedisStackRunningAsync(running: false, cancellationToken: cancellationToken);
            using var outageBudget = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            outageBudget.CancelAfter(TimeSpan.FromSeconds(15));
            using HttpResponseMessage unavailable = await fixture.MemoriesClient.GetAsync(path, outageBudget.Token);
            unavailable.StatusCode.ShouldBe(HttpStatusCode.ServiceUnavailable);
            unavailable.Headers.RetryAfter.ShouldNotBeNull().Delta.ShouldBe(TimeSpan.FromSeconds(5));
            (await unavailable.Content.ReadFromJsonAsync<ErrorResponse>(MemoriesJsonContext.Options, outageBudget.Token))
                .ShouldNotBeNull().Code.ShouldBe("DAPR_UNAVAILABLE");
            (await driver.CountGraphNodesAsync(tenant, caseId, sourceUri)).ShouldBe(1);
        }
        finally
        {
            await fixture.SetRedisStackRunningAsync(running: true, cancellationToken: CancellationToken.None);
        }

        await AssertIndexedAsync(waitForRecovery: true);
        (await driver.ReadHashSnapshotAsync(syntacticKey)).ShouldBe(syntacticBefore);

        async Task AssertIndexedAsync(bool waitForRecovery = false)
        {
            using var recoveryBudget = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            recoveryBudget.CancelAfter(TimeSpan.FromSeconds(30));
            while (true)
            {
                using HttpResponseMessage response = await fixture.MemoriesClient.GetAsync(path, recoveryBudget.Token);
                if (waitForRecovery && response.StatusCode == HttpStatusCode.ServiceUnavailable)
                {
                    await Task.Delay(TimeSpan.FromMilliseconds(250), recoveryBudget.Token);
                    continue;
                }

                response.StatusCode.ShouldBe(HttpStatusCode.OK);
                SearchResult indexed = (await response.Content.ReadFromJsonAsync<SearchResult>(
                    MemoriesJsonContext.Options, recoveryBudget.Token)).ShouldNotBeNull();
                indexed.Results.ShouldContain(item => item.MemoryUnitId == unitId);
                return;
            }
        }
    }
}
