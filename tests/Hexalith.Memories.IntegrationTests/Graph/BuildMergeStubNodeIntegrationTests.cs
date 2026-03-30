namespace Hexalith.Memories.IntegrationTests.Graph;

using Hexalith.Memories.Contracts.V1;
using Hexalith.Memories.IntegrationTests.Fixtures;
using Hexalith.Memories.Server.Graph;

using NFalkorDB;

using Shouldly;

using StackExchange.Redis;

/// <summary>
/// Integration tests for BuildMergeStubNode — verifies stub node creation
/// against a real FalkorDB instance. Stub nodes are placeholders for
/// CausationId/CorrelationId references that may not yet be fully indexed.
/// </summary>
[Collection("FalkorDB")]
[Trait("Category", "Integration")]
public class BuildMergeStubNodeIntegrationTests
{
    private readonly FalkorDbFixture _falkorDb;
    private readonly GraphQueryBuilder _builder = new();

    public BuildMergeStubNodeIntegrationTests(FalkorDbFixture falkorDb) => _falkorDb = falkorDb;

    [Fact]
    public async Task BuildMergeStubNode_ShouldCreateMinimalNodeInFalkorDb()
    {
        // Arrange
        string graphId = $"test-stub-{Guid.NewGuid():N}";
        FalkorDB falkor = new(_falkorDb.Connection.GetDatabase());
        string stubId = "mu-stub-001";

        // Act
        (string query, IDictionary<string, object> parameters) = _builder.BuildMergeStubNode(stubId);
        await falkor.QueryAsync(graphId, query, parameters);

        // Assert — stub node exists with only id property
        ResultSet result = await falkor.QueryAsync(
            graphId,
            "MATCH (m:MemoryUnit {id: $id}) RETURN m.id as nodeId",
            new Dictionary<string, object> { ["id"] = stubId });

        ReadString(result, "nodeId").ShouldBe(stubId);
    }

    [Fact]
    public async Task BuildMergeStubNode_ShouldBeIdempotent()
    {
        // Arrange
        string graphId = $"test-stub-{Guid.NewGuid():N}";
        FalkorDB falkor = new(_falkorDb.Connection.GetDatabase());
        string stubId = "mu-stub-idem-001";

        (string query, IDictionary<string, object> parameters) = _builder.BuildMergeStubNode(stubId);

        // Act — execute twice
        await falkor.QueryAsync(graphId, query, parameters);
        await falkor.QueryAsync(graphId, query, parameters);

        // Assert — only one node
        ResultSet countResult = await falkor.QueryAsync(
            graphId,
            "MATCH (m:MemoryUnit {id: $id}) RETURN count(m) as cnt",
            new Dictionary<string, object> { ["id"] = stubId });

        ReadCount(countResult).ShouldBe(1);
    }

    [Fact]
    public async Task BuildMergeStubNode_ThenFullMerge_ShouldEnrichSameNode()
    {
        // Arrange — create stub first, then full node via BuildMergeMemoryUnitNode
        string graphId = $"test-stub-enrich-{Guid.NewGuid():N}";
        FalkorDB falkor = new(_falkorDb.Connection.GetDatabase());
        string memoryUnitId = "mu-stub-then-full-001";

        // Act — create stub
        (string stubQuery, IDictionary<string, object> stubParams) = _builder.BuildMergeStubNode(memoryUnitId);
        await falkor.QueryAsync(graphId, stubQuery, stubParams);

        // Act — enrich with full node
        (string fullQuery, IDictionary<string, object> fullParams) = _builder.BuildMergeMemoryUnitNode(
            memoryUnitId, "case-001", "enriched content", "hash-full",
            "file:///enriched.txt", SourceType.File, "google:text-embedding-004",
            768, "integration@example.com", DateTimeOffset.UtcNow, "{}");
        await falkor.QueryAsync(graphId, fullQuery, fullParams);

        // Assert — still one node, now with full properties
        ResultSet countResult = await falkor.QueryAsync(
            graphId,
            "MATCH (m:MemoryUnit {id: $id}) RETURN count(m) as cnt",
            new Dictionary<string, object> { ["id"] = memoryUnitId });
        ReadCount(countResult).ShouldBe(1);

        // Assert — node has enriched content
        ResultSet contentResult = await falkor.QueryAsync(
            graphId,
            "MATCH (m:MemoryUnit {id: $id}) RETURN m.content as content",
            new Dictionary<string, object> { ["id"] = memoryUnitId });
        ReadString(contentResult, "content").ShouldBe("enriched content");
    }

    [Fact]
    public async Task BuildMergeStubNode_ShouldSupportEdgeCreationToStub()
    {
        // Arrange — create a full node and a stub, then link them
        string graphId = $"test-stub-edge-{Guid.NewGuid():N}";
        FalkorDB falkor = new(_falkorDb.Connection.GetDatabase());
        string fullNodeId = "mu-full-001";
        string stubNodeId = "mu-stub-edge-001";

        // Create full node
        (string fullQuery, IDictionary<string, object> fullParams) = _builder.BuildMergeMemoryUnitNode(
            fullNodeId, "case-001", "full content", "hash",
            "file:///full.txt", SourceType.File, "provider", 768, "integration@example.com", DateTimeOffset.UtcNow, "{}");
        await falkor.QueryAsync(graphId, fullQuery, fullParams);

        // Create stub node
        (string stubQuery, IDictionary<string, object> stubParams) = _builder.BuildMergeStubNode(stubNodeId);
        await falkor.QueryAsync(graphId, stubQuery, stubParams);

        // Act — create CausedBy edge from full node to stub
        (string edgeQuery, IDictionary<string, object> edgeParams) = _builder.BuildMergeEdge(
            fullNodeId, stubNodeId, EdgeType.CausedBy, EdgeTypeDefaults.CausedBy, EdgeOrigin.Inferred);
        await falkor.QueryAsync(graphId, edgeQuery, edgeParams);

        // Assert — edge exists
        ResultSet edgeResult = await falkor.QueryAsync(
            graphId,
            "MATCH (:MemoryUnit {id: $sourceId})-[r:CAUSED_BY]->(:MemoryUnit {id: $targetId}) RETURN count(r) as cnt",
            new Dictionary<string, object> { ["sourceId"] = fullNodeId, ["targetId"] = stubNodeId });

        ReadCount(edgeResult).ShouldBeGreaterThan(0, "Should be able to create edges to stub nodes");
    }

    private static long ReadCount(ResultSet result)
    {
        result.Count.ShouldBe(1);

        var enumerator = result.GetEnumerator();
        enumerator.MoveNext().ShouldBeTrue();
        return enumerator.Current.GetValue<long>("cnt");
    }

    private static string ReadString(ResultSet result, string key)
    {
        result.Count.ShouldBe(1);

        var enumerator = result.GetEnumerator();
        enumerator.MoveNext().ShouldBeTrue();
        return enumerator.Current.GetValue<string>(key);
    }
}
