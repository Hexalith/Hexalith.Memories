// <copyright file="IngestionRetryIntegrationTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.IntegrationTests.Ingestion;

using System.Net;

using Hexalith.Memories.Contracts.V1;
using Hexalith.Memories.IntegrationTests.Fixtures;

using Shouldly;

[Collection("AspireIngestionPipeline")]
[Trait("Category", "Integration")]
public sealed class IngestionRetryIntegrationTests
{
    private readonly AspireIngestionPipelineFixture _fixture;

    public IngestionRetryIntegrationTests(AspireIngestionPipelineFixture fixture) => _fixture = fixture;

    [Fact]
    public async Task TransientIngestionFailure_ShouldCompleteSuccessfullyAfterRetries()
    {
        IngestionIntegrationTestDriver driver = new(_fixture);
        string unique = Guid.NewGuid().ToString("N");
        string tenantId = $"tenant-retry-{unique[..12]}";
        string caseId = await driver.CreateTenantAndCaseAsync(tenantId).ConfigureAwait(true);
        int attempts = 0;
        await using ScriptedHttpServer source = await ScriptedHttpServer.StartAsync((_, _) =>
        {
            int attempt = Interlocked.Increment(ref attempts);
            ScriptedHttpResponse response = attempt <= 2
                ? ScriptedHttpResponse.Text("transient source failure", HttpStatusCode.InternalServerError)
                : ScriptedHttpResponse.Text($"durable retry canary {unique}");
            return ValueTask.FromResult(response);
        }).ConfigureAwait(true);
        string sourceUri = source.GetUri($"/retry/{unique}.txt").ToString();

        UrlIngestionResponse accepted = await driver.PostUrlIngestionAsync(tenantId, caseId, sourceUri).ConfigureAwait(true);
        string workflow = await driver.WaitForWorkflowRuntimeStatusAsync(
            tenantId,
            accepted.InstanceId,
            "Completed").ConfigureAwait(true);
        string memoryUnitId = IngestionIntegrationTestDriver.TryExtractMemoryUnitId(workflow) ?? accepted.InstanceId;
        MemoryUnit indexed = await driver.WaitForMemoryUnitAsync(
            tenantId,
            caseId,
            memoryUnitId,
            unit => unit.Status == MemoryUnitStatus.Indexed).ConfigureAwait(true);
        FailedUnitsPage failed = await driver.WaitForFailedUnitsPageAsync(
            tenantId,
            caseId,
            page => page.TotalCount == 0).ConfigureAwait(true);
        (string syntacticKey, string semanticKey) = await driver.WaitForSingleBackendWriteAsync(
            tenantId,
            caseId,
            sourceUri).ConfigureAwait(true);

        accepted.InstanceId.ShouldNotBeNullOrWhiteSpace();
        indexed.Id.ShouldBe(memoryUnitId);
        indexed.SourceUri.ShouldBe(sourceUri);
        failed.Units.ShouldBeEmpty();
        Volatile.Read(ref attempts).ShouldBeGreaterThanOrEqualTo(3);
        Volatile.Read(ref attempts).ShouldBeLessThanOrEqualTo(7);
        (await driver.ListRedisKeysAsync($"{tenantId}:mu:*").ConfigureAwait(true)).ShouldBe([syntacticKey]);
        (await driver.ListRedisKeysAsync($"{tenantId}:vec:*").ConfigureAwait(true)).ShouldBe([semanticKey]);
    }
}
