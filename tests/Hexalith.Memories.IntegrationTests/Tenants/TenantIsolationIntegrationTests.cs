// <copyright file="TenantIsolationIntegrationTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.IntegrationTests.Tenants;

using System.Net;
using System.Net.Http.Json;
using System.Text;

using Hexalith.Memories.Contracts.V1;
using Hexalith.Memories.IntegrationTests.Fixtures;
using Hexalith.Memories.Server.Graph;

using NFalkorDB;

using Shouldly;

using StackExchange.Redis;

/// <summary>Integration tests for tenant isolation verification.
/// These tests require the Aspire AppHost fixture with Redis, FalkorDB, and DAPR running.
/// Required before Gate 2 sign-off — NFR8 (zero cross-tenant data leakage) is a hard gate.</summary>
[Collection("AspireIngestionPipeline")]
[Trait("Category", "Integration")]
public sealed class TenantIsolationIntegrationTests
{
    private const long GraphQueryTimeoutMilliseconds = 30_000;
    private const string CollisionSourceNodeId = "shared-source";
    private const string CollisionTargetNodeId = "shared-target";

    private static readonly TimeSpan GraphQueryTimeout = TimeSpan.FromMilliseconds(GraphQueryTimeoutMilliseconds);
    private static readonly TimeSpan HttpOperationTimeout = TimeSpan.FromSeconds(60);

    private readonly AspireIngestionPipelineFixture _fixture;
    private readonly GraphQueryBuilder _graphQueryBuilder = new();

    /// <summary>Initializes a new instance of the <see cref="TenantIsolationIntegrationTests"/> class.</summary>
    /// <param name="fixture">The Aspire pipeline fixture.</param>
    public TenantIsolationIntegrationTests(AspireIngestionPipelineFixture fixture) => _fixture = fixture;

    [Fact]
    public async Task VerifyTenant_WithTwoProvisionedTenants_CoreIsolationChecksShouldPass()
    {
        // Arrange: Provision tenant A and B, ingest memory units into both
        // Act: POST /api/v1/tenants/tenant-a/verify
        // Assert: AllPassed == true, all individual checks passed
        string tenantA = await _fixture.ProvisionActiveTenantAsync($"tenant-a-{Guid.NewGuid():N}");
        _ = await _fixture.ProvisionActiveTenantAsync($"tenant-b-{Guid.NewGuid():N}");

        using HttpResponseMessage response = await _fixture.MemoriesClient.PostAsync(
            $"/api/v1/tenants/{tenantA}/verify", null);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        TenantIsolationVerificationResult? result = await response.Content
            .ReadFromJsonAsync<TenantIsolationVerificationResult>(MemoriesJsonContext.Options);

        result.ShouldNotBeNull();
        result.Checks.ShouldNotBeEmpty();
        AssertCoreIsolationChecksPassed(result);
    }

    [Fact]
    public async Task VerifyTenant_IdenticalGraphStructures_ZeroCrossTenantNodes()
    {
        const string nodeMarkerA = "GRAPH-NODE-MARKER-TENANT-A";
        const string nodeMarkerB = "GRAPH-NODE-MARKER-TENANT-B";
        const string edgeMarkerA = "GRAPH-EDGE-MARKER-TENANT-A";
        const string edgeMarkerB = "GRAPH-EDGE-MARKER-TENANT-B";

        string tenantA = await _fixture.ProvisionActiveTenantAsync($"tenant-a-{Guid.NewGuid():N}");
        string tenantB = await _fixture.ProvisionActiveTenantAsync($"tenant-b-{Guid.NewGuid():N}");
        DateTimeOffset fixtureTimestamp = DateTimeOffset.UtcNow;

        await SeedCollisionGraphAsync(tenantA, nodeMarkerA, edgeMarkerA, fixtureTimestamp);
        await SeedCollisionGraphAsync(tenantB, nodeMarkerB, edgeMarkerB, fixtureTimestamp);

        long tenantAEdgeId = await ReadCollisionEdgeIdAsync(tenantA);
        long tenantBEdgeId = await ReadCollisionEdgeIdAsync(tenantB);
        tenantAEdgeId.ShouldBe(
            tenantBEdgeId,
            "FalkorDB relationship ids are graph-scoped, so identical insertion order must produce the collision this proof defends against.");

        TraversalResult tenantATraversal = await TraverseAuthenticatedAsync(tenantA);
        TraversalResult tenantBTraversal = await TraverseAuthenticatedAsync(tenantB);

        AssertTraversalIsFixtureLocal(tenantATraversal, nodeMarkerA, edgeMarkerA, nodeMarkerB, edgeMarkerB);
        AssertTraversalIsFixtureLocal(tenantBTraversal, nodeMarkerB, edgeMarkerB, nodeMarkerA, edgeMarkerA);
    }

    [Fact]
    public async Task VerifyTenant_MalformedTenantId_Returns400()
    {
        // Run verify with malformed tenant ID, confirm rejection

        using HttpResponseMessage response = await _fixture.MemoriesClient.PostAsync(
            "/api/v1/tenants/tenant_with_underscore/verify", null);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task VerifyTenant_NonExistentTenant_Returns404()
    {
        // Run verify with non-existent tenant ID

        using HttpResponseMessage response = await _fixture.MemoriesClient.PostAsync(
            "/api/v1/tenants/nonexistent-tenant/verify", null);

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        ErrorResponse? error = await response.Content
            .ReadFromJsonAsync<ErrorResponse>(MemoriesJsonContext.Options);
        error.ShouldNotBeNull();
        error.Code.ShouldBe("TENANT_NOT_FOUND");
    }

    [Fact]
    public async Task VerifyTenant_AfterOtherTenantDeleted_IsolationUnaffected()
    {
        // Delete tenant B, run verify on A, confirm A isolation unaffected
        string tenantA = await _fixture.ProvisionActiveTenantAsync($"tenant-a-{Guid.NewGuid():N}");
        string tenantB = await _fixture.ProvisionActiveTenantAsync($"tenant-b-{Guid.NewGuid():N}");

        using HttpResponseMessage deleteResponse = await _fixture.MemoriesClient.DeleteAsync($"/api/v1/tenants/{tenantB}");
        deleteResponse.StatusCode.ShouldBe(HttpStatusCode.Accepted);

        using HttpResponseMessage response = await _fixture.MemoriesClient.PostAsync(
            $"/api/v1/tenants/{tenantA}/verify", null);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        TenantIsolationVerificationResult? result = await response.Content
            .ReadFromJsonAsync<TenantIsolationVerificationResult>(MemoriesJsonContext.Options);

        result.ShouldNotBeNull();
        AssertCoreIsolationChecksPassed(result);
    }

    [Fact]
    public async Task VerifyTenant_PlantedCrossTenantData_DetectsLeakage()
    {
        // Negative test (false-pass prevention): Deliberately plant cross-tenant data
        // (e.g., manually write a hash under tenant A's prefix with tenant B's stored tenantId),
        // run verify on A, confirm the verifier detects the planted leakage.
        // This prevents false-pass bugs in the target-prefix cursor checks.
        string tenantA = await _fixture.ProvisionActiveTenantAsync($"tenant-a-{Guid.NewGuid():N}");
        string tenantB = await _fixture.ProvisionActiveTenantAsync($"tenant-b-{Guid.NewGuid():N}");
        await SeedMemoryUnitHashAsync(tenantA, "case-1", "mu-leak", "Planted cross-tenant payload.", tenantB);

        using HttpResponseMessage response = await _fixture.MemoriesClient.PostAsync(
            $"/api/v1/tenants/{tenantA}/verify", null);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        TenantIsolationVerificationResult? result = await response.Content
            .ReadFromJsonAsync<TenantIsolationVerificationResult>(MemoriesJsonContext.Options);

        result.ShouldNotBeNull();
        result.AllPassed.ShouldBeFalse();
        TenantIsolationCheckResult syntacticCheck = result.Checks.First(c => c.CheckName == "SyntacticIsolation");
        syntacticCheck.Passed.ShouldBeFalse();
        syntacticCheck.Details.ShouldNotBeNull();
        syntacticCheck.Details.ShouldContain(tenantB);
    }

    private async Task SeedCollisionGraphAsync(
        string tenantId,
        string nodeMarker,
        string edgeMarker,
        DateTimeOffset fixtureTimestamp)
    {
        FalkorDB falkor = new(_fixture.FalkorDbConnection.GetDatabase());

        foreach (string memoryUnitId in new[] { CollisionSourceNodeId, CollisionTargetNodeId })
        {
            (string query, IDictionary<string, object> parameters) = _graphQueryBuilder.BuildMergeMemoryUnitNode(
                memoryUnitId,
                "shared-case",
                $"{nodeMarker} content for {memoryUnitId}",
                $"hash-{nodeMarker}-{memoryUnitId}",
                $"file:///{nodeMarker}/{memoryUnitId}.txt",
                SourceType.File,
                "integration-provider",
                3,
                "graph-isolation@integration.test",
                fixtureTimestamp,
                "{}");
            _ = await ExecuteGraphQueryAsync(falkor.SelectGraph(tenantId), query, parameters);
        }

        (string edgeQuery, IDictionary<string, object> edgeParameters) = _graphQueryBuilder.BuildRestoreEdge(
            CollisionSourceNodeId,
            CollisionTargetNodeId,
            EdgeType.CausedBy,
            EdgeTypeDefaults.CausedBy,
            EdgeOrigin.Explicit,
            fixtureTimestamp,
            edgeMarker,
            previousConfidence: null);
        _ = await ExecuteGraphQueryAsync(falkor.SelectGraph(tenantId), edgeQuery, edgeParameters);
    }

    private async Task<long> ReadCollisionEdgeIdAsync(string tenantId)
    {
        FalkorDB falkor = new(_fixture.FalkorDbConnection.GetDatabase());
        (string query, IDictionary<string, object> parameters) = _graphQueryBuilder.BuildListEdgesForMemoryUnits(
            [CollisionSourceNodeId, CollisionTargetNodeId]);
        ResultSet result = await ExecuteGraphQueryAsync(falkor.SelectGraph(tenantId), query, parameters);
        long[] edgeIds = result
            .Select(record => record.GetValue<long>("edgeId"))
            .Distinct()
            .ToArray();

        edgeIds.Length.ShouldBe(1, $"Tenant graph '{tenantId}' must contain exactly one collision-fixture relationship.");
        return edgeIds[0];
    }

    private async Task<TraversalResult> TraverseAuthenticatedAsync(string tenantId)
    {
        using CancellationTokenSource requestTimeout = CancellationTokenSource.CreateLinkedTokenSource(
            TestContext.Current.CancellationToken);
        requestTimeout.CancelAfter(HttpOperationTimeout);
        using HttpResponseMessage response = await _fixture.MemoriesClient.GetAsync(
            $"/api/v1/tenants/{tenantId}/traverse?startNodeId={CollisionSourceNodeId}&depth=1",
            requestTimeout.Token);
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        using CancellationTokenSource readTimeout = CancellationTokenSource.CreateLinkedTokenSource(
            TestContext.Current.CancellationToken);
        readTimeout.CancelAfter(HttpOperationTimeout);
        TraversalResult? result = await response.Content
            .ReadFromJsonAsync<TraversalResult>(MemoriesJsonContext.Options, readTimeout.Token);
        result.ShouldNotBeNull();
        return result;
    }

    private static async Task<ResultSet> ExecuteGraphQueryAsync(
        NFalkorDB.Graph graph,
        string query,
        IDictionary<string, object> parameters)
        => await graph
            .QueryAsync(query, parameters, CommandFlags.None, GraphQueryTimeoutMilliseconds)
            .WaitAsync(GraphQueryTimeout, TestContext.Current.CancellationToken);

    private static void AssertTraversalIsFixtureLocal(
        TraversalResult traversal,
        string ownNodeMarker,
        string ownEdgeMarker,
        string foreignNodeMarker,
        string foreignEdgeMarker)
    {
        string[] expectedNodeIds = [CollisionSourceNodeId, CollisionTargetNodeId];
        traversal.StartNodeId.ShouldBe(CollisionSourceNodeId);
        traversal.Depth.ShouldBe(1);
        traversal.TotalNodeCount.ShouldBe(2);
        traversal.OmittedCount.ShouldBe(0);
        traversal.Degraded.ShouldBeFalse();
        (traversal.UnavailableAxes ?? []).ShouldBeEmpty();
        traversal.PrimaryPathIntact.ShouldBeTrue();
        traversal.GapMarkers.ShouldBeEmpty();
        traversal.Nodes.Select(node => node.MemoryUnitId).OrderBy(id => id, StringComparer.Ordinal)
            .ShouldBe(expectedNodeIds.OrderBy(id => id, StringComparer.Ordinal));
        traversal.Nodes.ShouldAllBe(node => expectedNodeIds.Contains(node.MemoryUnitId, StringComparer.Ordinal));
        traversal.Nodes.ShouldAllBe(node => node.ContentSnippet.StartsWith(ownNodeMarker, StringComparison.Ordinal));
        traversal.Nodes.ShouldAllBe(node => !node.ContentSnippet.Contains(foreignNodeMarker, StringComparison.Ordinal));
        traversal.Nodes.ShouldAllBe(node => node.SourceUri.Contains(ownNodeMarker, StringComparison.Ordinal));
        traversal.Nodes.ShouldAllBe(node => !node.SourceUri.Contains(foreignNodeMarker, StringComparison.Ordinal));

        TraversalEdgeInfo[] edges = [.. traversal.Nodes.SelectMany(node => node.Edges)];
        edges.Length.ShouldBe(2, "Both endpoint nodes must expose exactly one incident view of the seeded relationship.");
        edges.ShouldAllBe(edge => expectedNodeIds.Contains(edge.ConnectedNodeId, StringComparer.Ordinal));
        edges.ShouldAllBe(edge => string.Equals(edge.VerifiedBy, ownEdgeMarker, StringComparison.Ordinal));
        edges.ShouldAllBe(
            edge => edge.VerifiedBy == null || !edge.VerifiedBy.Contains(foreignEdgeMarker, StringComparison.Ordinal));

        TraversalNode sourceNode = traversal.Nodes.Single(node => node.MemoryUnitId == CollisionSourceNodeId);
        TraversalEdgeInfo outgoing = sourceNode.Edges.ShouldHaveSingleItem();
        AssertCollisionEdge(outgoing, CollisionTargetNodeId, "outgoing", ownEdgeMarker);

        TraversalNode targetNode = traversal.Nodes.Single(node => node.MemoryUnitId == CollisionTargetNodeId);
        TraversalEdgeInfo incoming = targetNode.Edges.ShouldHaveSingleItem();
        AssertCollisionEdge(incoming, CollisionSourceNodeId, "incoming", ownEdgeMarker);
    }

    private static void AssertCollisionEdge(
        TraversalEdgeInfo edge,
        string connectedNodeId,
        string direction,
        string ownEdgeMarker)
    {
        edge.ConnectedNodeId.ShouldBe(connectedNodeId);
        edge.EdgeType.ShouldBe(EdgeType.CausedBy);
        edge.Direction.ShouldBe(direction);
        edge.Origin.ShouldBe(EdgeOrigin.Explicit);
        edge.VerifiedBy.ShouldBe(ownEdgeMarker);
    }

    private async Task SeedMemoryUnitHashAsync(
        string tenantId,
        string caseId,
        string memoryUnitId,
        string content,
        string storedTenantId)
    {
        DateTimeOffset now = DateTimeOffset.UtcNow;
        string contentHash = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(Encoding.UTF8.GetBytes(content)));
        await _fixture.RedisConnection.GetDatabase().HashSetAsync(
            $"{tenantId}:mu:{memoryUnitId}",
            [
                new HashEntry("id", memoryUnitId),
                new HashEntry("tenantId", storedTenantId),
                new HashEntry("caseId", caseId),
                new HashEntry("content", content),
                new HashEntry("contentHash", contentHash),
                new HashEntry("sourceUri", $"file:///{memoryUnitId}.txt"),
                new HashEntry("sourceType", SourceType.File.ToString()),
                new HashEntry("ingestedBy", "integration@test.local"),
                new HashEntry("ingestedAt", now.ToString("O")),
                new HashEntry("lastUpdated", now.ToString("O")),
                new HashEntry("status", MemoryUnitStatus.Indexed.ToString()),
                new HashEntry("metadataJson", "{}"),
            ]);
    }

    private static void AssertCoreIsolationChecksPassed(TenantIsolationVerificationResult result)
    {
        foreach (string checkName in new[]
        {
            "IndexExistence",
            "SyntacticIsolation",
            "SemanticIsolation",
            "GraphIsolation",
        })
        {
            TenantIsolationCheckResult check = result.Checks.First(c => c.CheckName == checkName);
            check.Passed.ShouldBe(
                true,
                $"{check.CheckName} failed: {check.Details ?? "(no details)"}. Summary: {result.Summary}");
        }
    }
}
