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
        ResultSet result = await falkor.SelectGraph(graphId).QueryAsync(query, parameters);

        // Assert — node was created
        ResultSet countResult = await falkor.SelectGraph(graphId).QueryAsync(
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
            768, "integration@example.com", DateTimeOffset.UtcNow, "{}");

        // Act — execute twice (MERGE should be idempotent)
        await falkor.SelectGraph(graphId).QueryAsync(query, parameters);
        await falkor.SelectGraph(graphId).QueryAsync(query, parameters);

        // Assert — only one node exists
        ResultSet countResult = await falkor.SelectGraph(graphId).QueryAsync(
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
        await falkor.SelectGraph(graphId).QueryAsync(caseQuery, caseParams);

        (string muQuery, IDictionary<string, object> muParams) = _builder.BuildMergeMemoryUnitNode(
            "mu-edge-001", "case-edge-001", "content", "hash",
            "file:///t.txt", SourceType.File, "provider", 768, "integration@example.com", DateTimeOffset.UtcNow, "{}");
        await falkor.SelectGraph(graphId).QueryAsync(muQuery, muParams);

        // Act — create Contains edge
        (string edgeQuery, IDictionary<string, object> edgeParams) = _builder.BuildMergeEdge(
            "case-edge-001", "mu-edge-001", EdgeType.Contains, EdgeTypeDefaults.Contains, EdgeOrigin.Explicit);
        await falkor.SelectGraph(graphId).QueryAsync(edgeQuery, edgeParams);

        // Assert — edge exists
        ResultSet edgeResult = await falkor.SelectGraph(graphId).QueryAsync(
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
        await falkor.SelectGraph(graphId).QueryAsync(caseQuery, caseParams);

        (string sourceMuQuery, IDictionary<string, object> sourceMuParams) = _builder.BuildMergeMemoryUnitNode(
            sharedId, "case-collision-001", "source content", "hash-source",
            "file:///source.txt", SourceType.File, "provider", 3, "integration@example.com", DateTimeOffset.UtcNow, "{}");
        await falkor.SelectGraph(graphId).QueryAsync(sourceMuQuery, sourceMuParams);

        (string targetMuQuery, IDictionary<string, object> targetMuParams) = _builder.BuildMergeMemoryUnitNode(
            targetId, "case-collision-001", "target content", "hash-target",
            "file:///target.txt", SourceType.File, "provider", 3, "integration@example.com", DateTimeOffset.UtcNow, "{}");
        await falkor.SelectGraph(graphId).QueryAsync(targetMuQuery, targetMuParams);

        // Act
        (string edgeQuery, IDictionary<string, object> edgeParams) = _builder.BuildMergeEdge(
            sharedId, targetId, EdgeType.Contains, EdgeTypeDefaults.Contains, EdgeOrigin.Explicit);
        await falkor.SelectGraph(graphId).QueryAsync(edgeQuery, edgeParams);

        // Assert
        ResultSet caseEdgeCount = await falkor.SelectGraph(graphId).QueryAsync(
            "MATCH (:Case {id: $sourceId})-[r:CONTAINS]->(:MemoryUnit {id: $targetId}) RETURN count(r) as cnt",
            new Dictionary<string, object> { ["sourceId"] = sharedId, ["targetId"] = targetId });

        ResultSet memoryUnitEdgeCount = await falkor.SelectGraph(graphId).QueryAsync(
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
        const string hostileContent = "TENANT-A-' MATCH (n) DETACH DELETE n //";
        const string tenantBContent = "TENANT-B-PRIVATE";
        FalkorDB falkor = new(_falkorDb.Connection.GetDatabase());

        // Act — send hostile user content as a parameter to tenant A's graph.
        (string queryA, IDictionary<string, object> parametersA) = _builder.BuildMergeMemoryUnitNode(
            "mu-a", "case-1", hostileContent, "hash-a", "file:///a.txt", SourceType.File,
            "provider", 3, "integration@example.com", DateTimeOffset.UtcNow, "{}");
        queryA.ShouldNotContain(hostileContent);
        parametersA["content"].ShouldBe(hostileContent);
        await falkor.SelectGraph(tenantA).QueryAsync(queryA, parametersA);

        (string queryB, IDictionary<string, object> parametersB) = _builder.BuildMergeMemoryUnitNode(
            "mu-b", "case-1", tenantBContent, "hash-b", "file:///b.txt", SourceType.File,
            "provider", 3, "integration@example.com", DateTimeOffset.UtcNow, "{}");
        await falkor.SelectGraph(tenantB).QueryAsync(queryB, parametersB);

        // Assert — each graph contains only its own node, despite the injected-looking value.
        ResultSet resultA = await falkor.SelectGraph(tenantA).QueryAsync(
            "MATCH (n:MemoryUnit) RETURN count(n) as cnt", new Dictionary<string, object>());
        ResultSet resultB = await falkor.SelectGraph(tenantB).QueryAsync(
            "MATCH (n:MemoryUnit) RETURN count(n) as cnt", new Dictionary<string, object>());

        ReadCount(resultA).ShouldBe(1);
        ReadCount(resultB).ShouldBe(1);
        ResultSet storedA = await falkor.SelectGraph(tenantA).QueryAsync(
            "MATCH (n:MemoryUnit {id: $id}) RETURN n.content as content",
            new Dictionary<string, object> { ["id"] = "mu-a" });
        ResultSet storedB = await falkor.SelectGraph(tenantB).QueryAsync(
            "MATCH (n:MemoryUnit {id: $id}) RETURN n.content as content",
            new Dictionary<string, object> { ["id"] = "mu-b" });
        ReadContent(storedA).ShouldBe(hostileContent);
        ReadContent(storedB).ShouldBe(tenantBContent);

        ResultSet foreignNodeInA = await falkor.SelectGraph(tenantA).QueryAsync(
            "MATCH (n:MemoryUnit {id: $id}) RETURN count(n) as cnt",
            new Dictionary<string, object> { ["id"] = "mu-b" });
        ResultSet foreignNodeInB = await falkor.SelectGraph(tenantB).QueryAsync(
            "MATCH (n:MemoryUnit {id: $id}) RETURN count(n) as cnt",
            new Dictionary<string, object> { ["id"] = "mu-a" });
        ReadCount(foreignNodeInA).ShouldBe(0);
        ReadCount(foreignNodeInB).ShouldBe(0);
    }

    private static string ReadContent(ResultSet result)
    {
        result.Count.ShouldBe(1);

        var enumerator = result.GetEnumerator();
        enumerator.MoveNext().ShouldBeTrue();
        return enumerator.Current.GetValue<string>("content");
    }

    private static long ReadCount(ResultSet result)
    {
        result.Count.ShouldBe(1);

        var enumerator = result.GetEnumerator();
        enumerator.MoveNext().ShouldBeTrue();
        return enumerator.Current.GetValue<long>("cnt");
    }
}
