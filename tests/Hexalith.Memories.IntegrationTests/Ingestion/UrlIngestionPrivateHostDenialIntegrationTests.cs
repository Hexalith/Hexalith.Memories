// <copyright file="UrlIngestionPrivateHostDenialIntegrationTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.IntegrationTests.Ingestion;

using System.Net;
using System.Net.Http.Json;

using Hexalith.Memories.Contracts.V1;
using Hexalith.Memories.IntegrationTests.Fixtures;
using Hexalith.Memories.Server.Actors;
using Hexalith.Memories.Server.Ingestion;

using Shouldly;

using StackExchange.Redis;

/// <summary>Verifies that the private-host policy denies URL ingestion before its side effects.</summary>
[Collection(nameof(UrlIngestionPrivateHostDenialCollection))]
[Trait("Category", "Integration")]
public sealed class UrlIngestionPrivateHostDenialIntegrationTests
{
    [Fact]
    public async Task UrlIngestion_PrivateIpWithAllowDisabled_ShouldRejectBeforeScheduling()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        await using ScriptedHttpServer source = await ScriptedHttpServer.StartAsync((_, _) =>
            ValueTask.FromResult(ScriptedHttpResponse.Text("Private-host denial canary.")));
        string sourceUri = source.GetUri("/document.txt").AbsoluteUri;

        var fixture = new AspireIngestionPipelineFixture(
            EmbeddingProviderTestMode.GoogleFake,
            embeddingProviderSecret: null,
            allowPrivateHosts: false);
        await fixture.InitializeAsync(cancellationToken);
        await using (fixture)
        {
            IngestionIntegrationTestDriver driver = new(fixture);
            string unique = Guid.NewGuid().ToString("N");
            string tenantId = $"tenant-url-deny-{unique[..10]}";
            string caseId = await driver.CreateTenantAndCaseAsync(tenantId, cancellationToken: cancellationToken);
            IDatabase redis = fixture.RedisConnection.GetDatabase();
            ICaseIngestionCounterActor counter = fixture.CreateCaseIngestionCounterActorProxy(tenantId, caseId);
            string activityKey = $"{tenantId}:case:{caseId}:activity";
            CaseIngestionCounts countsBefore = await counter.GetCountsAsync().WaitAsync(cancellationToken);
            const string workflowStatePattern = "*||dapr.internal.*.workflow||*||*";
            HashSet<string> workflowsBefore = (await driver.ListRedisKeysAsync(workflowStatePattern))
                .Select(GetWorkflowInstanceId).ToHashSet(StringComparer.Ordinal);
            workflowsBefore.ShouldNotBeEmpty("Tenant and case setup established workflow state in this topology.");
            StreamEntry[] activityBefore = await redis.StreamRangeAsync(activityKey).WaitAsync(cancellationToken);

            using HttpResponseMessage response = await fixture.MemoriesClient.PostAsJsonAsync(
                "/api/v1/ingest/url",
                new UrlIngestionRequest
                {
                    TenantId = tenantId,
                    CaseId = caseId,
                    Url = sourceUri,
                    IngestedBy = "integration@test.local",
                },
                MemoriesJsonContext.Options,
                cancellationToken);

            response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
            ErrorResponse error = (await response.Content.ReadFromJsonAsync<ErrorResponse>(
                MemoriesJsonContext.Options,
                cancellationToken)).ShouldNotBeNull();
            error.Code.ShouldBe("INVALID_URL");

            var registry = new RedisIngestionWorkflowInFlightRegistry(fixture.RedisConnection);
            (await registry.ListAsync(cancellationToken)).ShouldNotContain(entry => entry.TenantId == tenantId);
            HashEntry[] registeredMembers = await redis.HashGetAllAsync(RedisIngestionWorkflowInFlightRegistry.InstanceMemberLookupKey)
                .WaitAsync(cancellationToken);
            registeredMembers.ShouldNotContain(member => member.Value.ToString().StartsWith(
                tenantId + RedisIngestionWorkflowInFlightRegistry.MemberSeparator, StringComparison.Ordinal));
            // Setup workflows may finish writing or pruning their own keys after the snapshot.
            (await driver.ListRedisKeysAsync(workflowStatePattern)).Select(GetWorkflowInstanceId)
                .Except(workflowsBefore, StringComparer.Ordinal)
                .ShouldBeEmpty("A rejected URL must not create a new workflow instance.");
            (await redis.KeyExistsAsync(SourceInventoryStore.BuildKey(tenantId, sourceUri)).WaitAsync(cancellationToken)).ShouldBeFalse();
            (await driver.ListRedisKeysAsync($"{tenantId}:mu:*")).ShouldBeEmpty();
            (await driver.ListRedisKeysAsync($"{tenantId}:failed-unit:*")).ShouldBeEmpty();
            (await counter.GetCountsAsync().WaitAsync(cancellationToken)).ShouldBe(countsBefore);
            StreamEntry[] activityAfter = await redis.StreamRangeAsync(activityKey).WaitAsync(cancellationToken);
            activityAfter.Select(entry => entry.Id).ShouldBe(activityBefore.Select(entry => entry.Id));
        }

        source.RequestCount.ShouldBe(0);
    }

    private static string GetWorkflowInstanceId(string key) => key.Split("||", 4, StringSplitOptions.None)[2];
}
