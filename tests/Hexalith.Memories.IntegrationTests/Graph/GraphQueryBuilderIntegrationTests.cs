namespace Hexalith.Memories.IntegrationTests.Graph;

using Hexalith.Memories.Contracts.V1;
using Hexalith.Memories.IntegrationTests.Fixtures;
using Hexalith.Memories.Server.Graph;

using NFalkorDB;

using Shouldly;

using StackExchange.Redis;

/// <summary>
/// Integration tests verifying GraphQueryBuilder output executes correctly against real FalkorDB.
/// Validates Cypher query correctness, MERGE idempotency, and edge creation.
/// </summary>
[Collection("FalkorDB")]
[Trait("Category", "Integration")]
public class GraphQueryBuilderIntegrationTests
{
    private readonly FalkorDbFixture _falkorDb;
    private readonly GraphQueryBuilder _builder = new();

    public GraphQueryBuilderIntegrationTests(FalkorDbFixture falkorDb) => _falkorDb = falkorDb;

    [Fact]
    public async Task BuildMergeCaseNode_ShouldCreateNodeInFalkorDb()
    {
        // Arrange
        string graphId = $"test-{Guid.NewGuid():N}";
        FalkorDB falkor = new(_falkorDb.Connection.GetDatabase());

        // Act
        (string query, IDictionary<string, object> parameters) = _builder.BuildMergeCaseNode("case-001");
        ResultSet result = await falkor.QueryAsync(graphId, query, parameters);

        // Assert — node was created
        ResultSet countResult = await falkor.QueryAsync(
            graphId,
            "MATCH (c:Case {id: $id}) RETURN count(c) as cnt",
            new Dictionary<string, object> { ["id"] = "case-001" });

        ReadCount(countResult).ShouldBe(1);
    }

    [Fact]
    public async Task BuildMergeMemoryUnitNode_ShouldBeIdempotent()
    {
        // Arrange
        string graphId = $"test-{Guid.NewGuid():N}";
        FalkorDB falkor = new(_falkorDb.Connection.GetDatabase());

        (string query, IDictionary<string, object> parameters) = _builder.BuildMergeMemoryUnitNode(
            "mu-idem-001", "case-001", "test content", "hash123",
            "file:///test.txt", SourceType.File, "google:text-embedding-004",
            768, DateTimeOffset.UtcNow);

        // Act — execute twice (MERGE should be idempotent)
        await falkor.QueryAsync(graphId, query, parameters);
        await falkor.QueryAsync(graphId, query, parameters);

        // Assert — only one node exists
        ResultSet countResult = await falkor.QueryAsync(
            graphId,
            "MATCH (m:MemoryUnit {id: $id}) RETURN count(m) as cnt",
            new Dictionary<string, object> { ["id"] = "mu-idem-001" });

        ReadCount(countResult).ShouldBe(1);
    }

    [Fact]
    public async Task BuildMergeEdge_Contains_ShouldCreateEdgeInFalkorDb()
    {
        // Arrange
        string graphId = $"test-{Guid.NewGuid():N}";
        FalkorDB falkor = new(_falkorDb.Connection.GetDatabase());

        // Create case and memory unit nodes first
        (string caseQuery, IDictionary<string, object> caseParams) = _builder.BuildMergeCaseNode("case-edge-001");
        await falkor.QueryAsync(graphId, caseQuery, caseParams);

        (string muQuery, IDictionary<string, object> muParams) = _builder.BuildMergeMemoryUnitNode(
            "mu-edge-001", "case-edge-001", "content", "hash",
            "file:///t.txt", SourceType.File, "provider", 768, DateTimeOffset.UtcNow);
        await falkor.QueryAsync(graphId, muQuery, muParams);

        // Act — create Contains edge
        (string edgeQuery, IDictionary<string, object> edgeParams) = _builder.BuildMergeEdge(
            "case-edge-001", "mu-edge-001", EdgeType.Contains, EdgeTypeDefaults.Contains, EdgeOrigin.Explicit);
        await falkor.QueryAsync(graphId, edgeQuery, edgeParams);

        // Assert — edge exists
        ResultSet edgeResult = await falkor.QueryAsync(
            graphId,
            "MATCH (:Case {id: $caseId})-[r:CONTAINS]->(:MemoryUnit {id: $muId}) RETURN count(r) as cnt",
            new Dictionary<string, object> { ["caseId"] = "case-edge-001", ["muId"] = "mu-edge-001" });

        ReadCount(edgeResult).ShouldBe(1);
    }

    [Fact]
    public async Task BuildMergeEdge_Contains_ShouldRespectNodeLabelsWhenIdsCollide()
    {
        // Arrange
        string graphId = $"test-{Guid.NewGuid():N}";
        FalkorDB falkor = new(_falkorDb.Connection.GetDatabase());
        const string sharedId = "shared-id-001";
        const string targetId = "target-id-001";

        (string caseQuery, IDictionary<string, object> caseParams) = _builder.BuildMergeCaseNode(sharedId);
        await falkor.QueryAsync(graphId, caseQuery, caseParams);

        (string sourceMuQuery, IDictionary<string, object> sourceMuParams) = _builder.BuildMergeMemoryUnitNode(
            sharedId, "case-collision-001", "source content", "hash-source",
            "file:///source.txt", SourceType.File, "provider", 3, DateTimeOffset.UtcNow);
        await falkor.QueryAsync(graphId, sourceMuQuery, sourceMuParams);

        (string targetMuQuery, IDictionary<string, object> targetMuParams) = _builder.BuildMergeMemoryUnitNode(
            targetId, "case-collision-001", "target content", "hash-target",
            "file:///target.txt", SourceType.File, "provider", 3, DateTimeOffset.UtcNow);
        await falkor.QueryAsync(graphId, targetMuQuery, targetMuParams);

        // Act
        (string edgeQuery, IDictionary<string, object> edgeParams) = _builder.BuildMergeEdge(
            sharedId, targetId, EdgeType.Contains, EdgeTypeDefaults.Contains, EdgeOrigin.Explicit);
        await falkor.QueryAsync(graphId, edgeQuery, edgeParams);

        // Assert
        ResultSet caseEdgeCount = await falkor.QueryAsync(
            graphId,
            "MATCH (:Case {id: $sourceId})-[r:CONTAINS]->(:MemoryUnit {id: $targetId}) RETURN count(r) as cnt",
            new Dictionary<string, object> { ["sourceId"] = sharedId, ["targetId"] = targetId });

        ResultSet memoryUnitEdgeCount = await falkor.QueryAsync(
            graphId,
            "MATCH (:MemoryUnit {id: $sourceId})-[r:CONTAINS]->(:MemoryUnit {id: $targetId}) RETURN count(r) as cnt",
            new Dictionary<string, object> { ["sourceId"] = sharedId, ["targetId"] = targetId });

        ReadCount(caseEdgeCount).ShouldBe(1);
        ReadCount(memoryUnitEdgeCount).ShouldBe(0);
    }

    [Fact]
    public async Task TenantIsolation_SeparateGraphs_ShouldNotLeakData()
    {
        // Arrange — two tenants = two separate FalkorDB graphs
        string tenantA = $"tenant-a-{Guid.NewGuid():N}";
        string tenantB = $"tenant-b-{Guid.NewGuid():N}";
        FalkorDB falkor = new(_falkorDb.Connection.GetDatabase());

        // Act — create node in tenant A only
        (string query, IDictionary<string, object> parameters) = _builder.BuildMergeCaseNode("secret-case");
        await falkor.QueryAsync(tenantA, query, parameters);

        // Assert — tenant B's graph has no nodes
        ResultSet resultB = await falkor.QueryAsync(
            tenantB,
            "MATCH (n) RETURN count(n) as cnt",
            new Dictionary<string, object>());

        ReadCount(resultB).ShouldBe(0);
    }

    private static long ReadCount(ResultSet result)
    {
        result.Count.ShouldBe(1);

        var enumerator = result.GetEnumerator();
        enumerator.MoveNext().ShouldBeTrue();
        return enumerator.Current.GetValue<long>("cnt");
    }
}
