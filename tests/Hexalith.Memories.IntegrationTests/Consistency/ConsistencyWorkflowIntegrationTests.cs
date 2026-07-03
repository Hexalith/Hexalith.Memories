// <copyright file="ConsistencyWorkflowIntegrationTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.IntegrationTests.Consistency;

using System.Net;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

using Hexalith.Memories.Contracts.V1;
using Hexalith.Memories.IntegrationTests.Fixtures;

using NFalkorDB;

using Shouldly;

using StackExchange.Redis;

/// <summary>
/// Story 8.2 — end-to-end integration for the consistency verify / repair workflows against
/// the Aspire-hosted Redis Stack + FalkorDB + Dapr sidecar.
/// </summary>
[Collection("AspireIngestionPipeline")]
[Trait("Category", "Integration")]
public sealed class ConsistencyWorkflowIntegrationTests
{
    private static readonly TimeSpan WorkflowTimeout = TimeSpan.FromMinutes(2);

    private readonly AspireIngestionPipelineFixture _fixture;

    /// <summary>Initializes a new instance of the <see cref="ConsistencyWorkflowIntegrationTests"/> class.</summary>
    /// <param name="fixture">The Aspire integration fixture.</param>
    public ConsistencyWorkflowIntegrationTests(AspireIngestionPipelineFixture fixture) => _fixture = fixture;

    /// <summary>Verification on a clean tenant reports zero discrepancies.</summary>
    [Fact]
    public async Task VerifyOnCleanTenant_ReportsZeroDiscrepancies()
    {
        string tenantId = await _fixture.ProvisionActiveTenantAsync($"tenant-consistency-clean-{Guid.NewGuid():N}");

        ConsistencyVerificationStatus status = await StartAndWaitForVerificationAsync(tenantId);

        status.Status.ShouldBe("Completed");
        status.Result.ShouldNotBeNull();
        status.Result.TenantId.ShouldBe(tenantId);
        status.Result.TotalUnits.ShouldBe(0);
        status.Result.InconsistentCount.ShouldBe(0);
        status.Result.TotalDiscrepancyCount.ShouldBe(0);
        status.Result.Discrepancies.ShouldBeEmpty();
    }

    /// <summary>Manually-seeded orphan is detected with the correct recommendation.</summary>
    [Fact]
    public async Task SeedOrphanThenVerify_ReportsOneDiscrepancyWithCorrectRecommendation()
    {
        string tenantId = await _fixture.ProvisionActiveTenantAsync($"tenant-consistency-orphan-{Guid.NewGuid():N}");
        string memoryUnitId = Guid.NewGuid().ToString("D");

        await SeedSyntacticMemoryUnitHashAsync(
            tenantId,
            caseId: "case-synthetic",
            memoryUnitId,
            content: $"syntactic-only orphan {Guid.NewGuid():N}");

        ConsistencyVerificationStatus status = await StartAndWaitForVerificationAsync(tenantId);

        status.Status.ShouldBe("Completed");
        status.Result.ShouldNotBeNull();
        ConsistencyDiscrepancy discrepancy = status.Result.Discrepancies
            .Single(d => string.Equals(d.MemoryUnitId, memoryUnitId, StringComparison.Ordinal));
        discrepancy.SyntacticPresent.ShouldBeTrue();
        discrepancy.SemanticPresent.ShouldBeFalse();
        discrepancy.GraphPresent.ShouldBeFalse();
        discrepancy.Recommendation.ShouldBe(ConsistencyRepairRecommendation.ReIndexSemanticAndGraph);
        status.Result.TotalDiscrepancyCount.ShouldBeGreaterThanOrEqualTo(1);
    }

    /// <summary>Repair converges from a seeded-orphan state to fully consistent.</summary>
    [Fact]
    public async Task SeedOrphanThenRepair_ConvergesToConsistent()
    {
        string tenantId = await _fixture.ProvisionActiveTenantAsync($"tenant-consistency-repair-{Guid.NewGuid():N}");
        string memoryUnitId = Guid.NewGuid().ToString("D");

        await SeedGraphMemoryUnitAsync(
            tenantId,
            caseId: "case-synthetic",
            memoryUnitId,
            content: $"graph-only orphan {Guid.NewGuid():N}");

        ConsistencyVerificationStatus preRepair = await StartAndWaitForVerificationAsync(tenantId);
        preRepair.Result.ShouldNotBeNull();
        preRepair.Result.Discrepancies
            .ShouldContain(d => string.Equals(d.MemoryUnitId, memoryUnitId, StringComparison.Ordinal)
                && d.Recommendation == ConsistencyRepairRecommendation.RemoveOrphanedGraph);

        ConsistencyRepairStatus repair = await StartAndWaitForRepairAsync(tenantId);

        repair.Status.ShouldBe("Completed");
        repair.Result.ShouldNotBeNull();
        repair.Result.TotalDiscrepancies.ShouldBeGreaterThanOrEqualTo(1);
        repair.Result.RepairedCount.ShouldBeGreaterThanOrEqualTo(1);
        RepairActionRecord action = repair.Result.Actions
            .Single(a => string.Equals(a.MemoryUnitId, memoryUnitId, StringComparison.Ordinal));
        action.Applied.ShouldBe(ConsistencyRepairRecommendation.RemoveOrphanedGraph);
        action.Succeeded.ShouldBeTrue();

        ConsistencyVerificationStatus postRepair = await StartAndWaitForVerificationAsync(tenantId);
        postRepair.Result.ShouldNotBeNull();
        postRepair.Result.Discrepancies
            .ShouldNotContain(d => string.Equals(d.MemoryUnitId, memoryUnitId, StringComparison.Ordinal));
    }

    private async Task<ConsistencyVerificationStatus> StartAndWaitForVerificationAsync(string tenantId)
    {
        using HttpResponseMessage response = await _fixture.MemoriesClient.PostAsJsonAsync(
            $"/api/tenants/{tenantId}/consistency/verify",
            new ConsistencyVerificationRequest(tenantId, BatchSize: 10),
            MemoriesJsonContext.Options);
        response.StatusCode.ShouldBe(HttpStatusCode.Accepted);

        string instanceId = await ReadWorkflowInstanceIdAsync(response.Content);
        return await WaitForVerificationCompletedAsync(tenantId, instanceId);
    }

    private async Task<ConsistencyRepairStatus> StartAndWaitForRepairAsync(string tenantId)
    {
        using HttpResponseMessage response = await _fixture.MemoriesClient.PostAsJsonAsync(
            $"/api/tenants/{tenantId}/consistency/repair",
            new ConsistencyRepairRequest(tenantId, BatchSize: 10),
            MemoriesJsonContext.Options);
        response.StatusCode.ShouldBe(HttpStatusCode.Accepted);

        string instanceId = await ReadWorkflowInstanceIdAsync(response.Content);
        return await WaitForRepairCompletedAsync(tenantId, instanceId);
    }

    private async Task<ConsistencyVerificationStatus> WaitForVerificationCompletedAsync(
        string tenantId,
        string instanceId)
    {
        DateTimeOffset deadline = DateTimeOffset.UtcNow.Add(WorkflowTimeout);
        while (DateTimeOffset.UtcNow < deadline)
        {
            using HttpResponseMessage response = await _fixture.MemoriesClient.GetAsync(
                $"/api/tenants/{tenantId}/consistency/verify/{instanceId}");
            response.StatusCode.ShouldBe(HttpStatusCode.OK);

            ConsistencyVerificationStatus? status = await response.Content.ReadFromJsonAsync<ConsistencyVerificationStatus>(
                MemoriesJsonContext.Options);
            status.ShouldNotBeNull();
            if (string.Equals(status.Status, "Completed", StringComparison.Ordinal))
            {
                return status;
            }

            status.Status.ShouldNotBe("Failed");
            status.Status.ShouldNotBe("Terminated");
            await Task.Delay(TimeSpan.FromMilliseconds(500));
        }

        throw new TimeoutException($"Consistency verification workflow '{instanceId}' did not complete within {WorkflowTimeout}.");
    }

    private async Task<ConsistencyRepairStatus> WaitForRepairCompletedAsync(
        string tenantId,
        string instanceId)
    {
        DateTimeOffset deadline = DateTimeOffset.UtcNow.Add(WorkflowTimeout);
        while (DateTimeOffset.UtcNow < deadline)
        {
            using HttpResponseMessage response = await _fixture.MemoriesClient.GetAsync(
                $"/api/tenants/{tenantId}/consistency/repair/{instanceId}");
            response.StatusCode.ShouldBe(HttpStatusCode.OK);

            ConsistencyRepairStatus? status = await response.Content.ReadFromJsonAsync<ConsistencyRepairStatus>(
                MemoriesJsonContext.Options);
            status.ShouldNotBeNull();
            if (string.Equals(status.Status, "Completed", StringComparison.Ordinal))
            {
                return status;
            }

            status.Status.ShouldNotBe("Failed");
            status.Status.ShouldNotBe("Terminated");
            await Task.Delay(TimeSpan.FromMilliseconds(500));
        }

        throw new TimeoutException($"Consistency repair workflow '{instanceId}' did not complete within {WorkflowTimeout}.");
    }

    private async Task SeedSyntacticMemoryUnitHashAsync(
        string tenantId,
        string caseId,
        string memoryUnitId,
        string content)
    {
        IDatabase db = _fixture.RedisConnection.GetDatabase();
        DateTimeOffset now = DateTimeOffset.UtcNow.AddSeconds(-1);
        await db.HashSetAsync(
            $"{tenantId}:mu:{memoryUnitId}",
            [
                new HashEntry("id", memoryUnitId),
                new HashEntry("tenantId", tenantId),
                new HashEntry("caseId", caseId),
                new HashEntry("content", content),
                new HashEntry("contentHash", ComputeContentHash(content)),
                new HashEntry("sourceUri", $"memory://consistency/{memoryUnitId}"),
                new HashEntry("sourceType", "file"),
                new HashEntry("ingestedBy", "integration@test.local"),
                new HashEntry("ingestedAt", now.ToString("O")),
                new HashEntry("lastUpdated", now.ToString("O")),
                new HashEntry("status", "completed"),
                new HashEntry("metadataJson", "{}"),
            ]);
    }

    private async Task SeedGraphMemoryUnitAsync(
        string tenantId,
        string caseId,
        string memoryUnitId,
        string content)
    {
        FalkorDB falkor = new(_fixture.FalkorDbConnection.GetDatabase());
        await falkor.QueryAsync(
            tenantId,
            """
            MERGE (c:Case {id: $caseId})
            MERGE (m:MemoryUnit {id: $id})
            SET m.caseId = $caseId,
                m.content = $content,
                m.sourceUri = $sourceUri,
                m.sourceType = $sourceType,
                m.ingestedAt = $ingestedAt
            MERGE (c)-[:CONTAINS]->(m)
            """,
            new Dictionary<string, object>
            {
                ["caseId"] = caseId,
                ["id"] = memoryUnitId,
                ["content"] = content,
                ["sourceUri"] = $"memory://consistency/{memoryUnitId}",
                ["sourceType"] = "file",
                ["ingestedAt"] = DateTimeOffset.UtcNow.AddSeconds(-1).ToString("O"),
            });
    }

    private static async Task<string> ReadWorkflowInstanceIdAsync(HttpContent content)
    {
        using JsonDocument document = JsonDocument.Parse(await content.ReadAsStringAsync().ConfigureAwait(false));
        string? instanceId = document.RootElement.GetProperty("workflowInstanceId").GetString();
        instanceId.ShouldNotBeNullOrWhiteSpace();
        return instanceId;
    }

    private static string ComputeContentHash(string content)
        => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(content))).ToLowerInvariant();
}
