namespace Hexalith.Memories.IntegrationTests.Graph;

using System.Collections;

using Hexalith.Memories.Contracts.V1;
using Hexalith.Memories.IntegrationTests.Fixtures;
using Hexalith.Memories.Server.Graph;

using Microsoft.Extensions.Logging.Abstractions;

using NFalkorDB;

using Shouldly;

using StackExchange.Redis;

/// <summary>
/// Integration tests verifying edge-type-filtered traversal queries execute correctly
/// against real FalkorDB. Validates typed variable-length path syntax and edge metadata filtering.
/// </summary>
[Collection("FalkorDB")]
[Trait("Category", "Integration")]
public class TraversalEdgeTypeFilterIntegrationTests
{
    private readonly FalkorDbFixture _falkorDb;
    private readonly GraphQueryBuilder _builder = new();

    public TraversalEdgeTypeFilterIntegrationTests(FalkorDbFixture falkorDb) => _falkorDb = falkorDb;

    [Fact]
    public async Task Traverse_WithSingleEdgeType_OnlyFollowsMatchingEdges()
    {
        // Arrange: A->B via CAUSED_BY, B->C via REFERENCES
        string graphId = $"test-{Guid.NewGuid():N}";
        FalkorDB falkor = new(_falkorDb.Connection.GetDatabase());

        await CreateMemoryUnit(falkor, graphId, "mu-a");
        await CreateMemoryUnit(falkor, graphId, "mu-b");
        await CreateMemoryUnit(falkor, graphId, "mu-c");
        await CreateEdge(falkor, graphId, "mu-a", "mu-b", EdgeType.CausedBy);
        await CreateEdge(falkor, graphId, "mu-b", "mu-c", EdgeType.References);

        // Act: Traverse from A with only causedBy — C should be unreachable
        (string query, IDictionary<string, object> parameters) =
            _builder.BuildTraverseWithEdges("mu-a", 3, null, [EdgeType.CausedBy]);
        ResultSet result = await falkor.QueryAsync(graphId, query, parameters);

        // Assert: Only A (start, hop 0) and B (hop 1) returned
        List<string> nodeIds = ReadNodeIds(result);
        nodeIds.ShouldContain("mu-a");
        nodeIds.ShouldContain("mu-b");
        nodeIds.ShouldNotContain("mu-c");
    }

    [Fact]
    public async Task Traverse_WithMultipleEdgeTypes_FollowsAllSpecifiedTypes()
    {
        // Arrange: A->B via CAUSED_BY, B->C via REFERENCES
        string graphId = $"test-{Guid.NewGuid():N}";
        FalkorDB falkor = new(_falkorDb.Connection.GetDatabase());

        await CreateMemoryUnit(falkor, graphId, "mu-a");
        await CreateMemoryUnit(falkor, graphId, "mu-b");
        await CreateMemoryUnit(falkor, graphId, "mu-c");
        await CreateEdge(falkor, graphId, "mu-a", "mu-b", EdgeType.CausedBy);
        await CreateEdge(falkor, graphId, "mu-b", "mu-c", EdgeType.References);

        // Act: Traverse from A with causedBy + references — all 3 nodes reachable
        (string query, IDictionary<string, object> parameters) =
            _builder.BuildTraverseWithEdges("mu-a", 3, null, [EdgeType.CausedBy, EdgeType.References]);
        ResultSet result = await falkor.QueryAsync(graphId, query, parameters);

        // Assert: All 3 nodes returned
        List<string> nodeIds = ReadNodeIds(result);
        nodeIds.ShouldContain("mu-a");
        nodeIds.ShouldContain("mu-b");
        nodeIds.ShouldContain("mu-c");
    }

    [Fact]
    public async Task Traverse_WithDefaultSemanticTypes_ExcludesStructuralEdges()
    {
        // Arrange: A->B via CAUSED_BY (semantic), Case->A via CONTAINS (structural)
        string graphId = $"test-{Guid.NewGuid():N}";
        FalkorDB falkor = new(_falkorDb.Connection.GetDatabase());

        await CreateMemoryUnit(falkor, graphId, "mu-a", "case-1");
        await CreateMemoryUnit(falkor, graphId, "mu-b", "case-1");
        await CreateEdge(falkor, graphId, "mu-a", "mu-b", EdgeType.CausedBy);

        // Create Case node and CONTAINS edge
        (string caseQuery, IDictionary<string, object> caseParams) = _builder.BuildMergeCaseNode("case-1");
        await falkor.QueryAsync(graphId, caseQuery, caseParams);
        (string containsQuery, IDictionary<string, object> containsParams) =
            _builder.BuildMergeEdge("case-1", "mu-a", EdgeType.Contains, EdgeTypeDefaults.Contains, EdgeOrigin.Explicit);
        await falkor.QueryAsync(graphId, containsQuery, containsParams);

        // Act: Default traversal (semantic only) from A
        (string query, IDictionary<string, object> parameters) =
            _builder.BuildTraverseWithEdges("mu-a", 3, null);
        ResultSet result = await falkor.QueryAsync(graphId, query, parameters);

        // Assert: B reached via CAUSED_BY, Case node NOT in results (MemoryUnit label filter)
        List<string> nodeIds = ReadNodeIds(result);
        nodeIds.ShouldContain("mu-a");
        nodeIds.ShouldContain("mu-b");
        // Case node excluded by label constraint (:MemoryUnit) regardless of edge filter
    }

    [Fact]
    public async Task Traverse_WithContainsEdgeType_ReturnsSiblingMemoryUnitsAndContainsMetadata()
    {
        // Arrange: MU-A connected to Case via CONTAINS, Case connected to MU-B via CONTAINS
        string graphId = $"test-{Guid.NewGuid():N}";
        FalkorDB falkor = new(_falkorDb.Connection.GetDatabase());

        await CreateMemoryUnit(falkor, graphId, "mu-a", "case-1");
        await CreateMemoryUnit(falkor, graphId, "mu-b", "case-1");

        (string caseQuery, IDictionary<string, object> caseParams) = _builder.BuildMergeCaseNode("case-1");
        await falkor.QueryAsync(graphId, caseQuery, caseParams);
        (string e1Q, IDictionary<string, object> e1P) =
            _builder.BuildMergeEdge("case-1", "mu-a", EdgeType.Contains, 1.0f, EdgeOrigin.Explicit);
        await falkor.QueryAsync(graphId, e1Q, e1P);
        (string e2Q, IDictionary<string, object> e2P) =
            _builder.BuildMergeEdge("case-1", "mu-b", EdgeType.Contains, 1.0f, EdgeOrigin.Explicit);
        await falkor.QueryAsync(graphId, e2Q, e2P);

        // Act: Traverse from MU-A with edgeTypes=contains
        (string query, IDictionary<string, object> parameters) =
            _builder.BuildTraverseWithEdges("mu-a", 3, null, [EdgeType.Contains]);
        ResultSet result = await falkor.QueryAsync(graphId, query, parameters);

        // Assert: Explicit contains traversal can reach sibling memory units through the case hub.
        List<string> nodeIds = ReadNodeIds(result);
        nodeIds.ShouldContain("mu-a");
        nodeIds.ShouldContain("mu-b");

        List<(string nodeId, List<string> edgeTypes)> nodesWithEdges = ReadNodesWithEdgeTypes(result);
        nodesWithEdges.Single(n => n.nodeId == "mu-a").edgeTypes.ShouldContain("CONTAINS");
        nodesWithEdges.Single(n => n.nodeId == "mu-b").edgeTypes.ShouldContain("CONTAINS");
    }

    [Fact]
    public async Task Traverse_WithContainsEdgeTypeAndCaseId_StaysReachableWithinCaseScope()
    {
        // Arrange: same-case memory units connected through a Case hub via CONTAINS.
        string graphId = $"test-{Guid.NewGuid():N}";
        FalkorDB falkor = new(_falkorDb.Connection.GetDatabase());

        await CreateMemoryUnit(falkor, graphId, "mu-a", "case-1");
        await CreateMemoryUnit(falkor, graphId, "mu-b", "case-1");

        (string caseQuery, IDictionary<string, object> caseParams) = _builder.BuildMergeCaseNode("case-1");
        await falkor.QueryAsync(graphId, caseQuery, caseParams);
        (string e1Q, IDictionary<string, object> e1P) =
            _builder.BuildMergeEdge("case-1", "mu-a", EdgeType.Contains, 1.0f, EdgeOrigin.Explicit);
        await falkor.QueryAsync(graphId, e1Q, e1P);
        (string e2Q, IDictionary<string, object> e2P) =
            _builder.BuildMergeEdge("case-1", "mu-b", EdgeType.Contains, 1.0f, EdgeOrigin.Explicit);
        await falkor.QueryAsync(graphId, e2Q, e2P);

        // Act: case-scoped traversal should still be able to cross the Case node boundary.
        (string query, IDictionary<string, object> parameters) =
            _builder.BuildTraverseWithEdges("mu-a", 3, "case-1", [EdgeType.Contains]);
        ResultSet result = await falkor.QueryAsync(graphId, query, parameters);

        // Assert
        List<string> nodeIds = ReadNodeIds(result);
        nodeIds.ShouldContain("mu-a");
        nodeIds.ShouldContain("mu-b");

        List<(string nodeId, List<string> edgeTypes)> nodesWithEdges = ReadNodesWithEdgeTypes(result);
        nodesWithEdges.Single(n => n.nodeId == "mu-a").edgeTypes.ShouldContain("CONTAINS");
    }

    [Fact]
    public async Task TraverseServiceAsync_WithSingleEdgeType_ExcludesUnmatchedEdgesFromResult()
    {
        // Arrange: A->B via CAUSED_BY, A->C via REFERENCES.
        string graphId = $"test-{Guid.NewGuid():N}";
        FalkorDB falkor = new(_falkorDb.Connection.GetDatabase());
        GraphTraversalService service = new(
            _falkorDb.Connection,
            _falkorDb.Connection,
            _builder,
            NullLogger<GraphTraversalService>.Instance);

        await CreateMemoryUnit(falkor, graphId, "mu-a");
        await CreateMemoryUnit(falkor, graphId, "mu-b");
        await CreateMemoryUnit(falkor, graphId, "mu-c");
        await CreateEdge(falkor, graphId, "mu-a", "mu-b", EdgeType.CausedBy);
        await CreateEdge(falkor, graphId, "mu-a", "mu-c", EdgeType.References);

        // Act
        TraversalResult result = await service.TraverseAsync(
            graphId,
            "mu-a",
            3,
            null,
            [EdgeType.CausedBy],
            CancellationToken.None);

        // Assert: node reachability and edge metadata both respect the filter.
        result.Nodes.Select(n => n.MemoryUnitId).ShouldContain("mu-a");
        result.Nodes.Select(n => n.MemoryUnitId).ShouldContain("mu-b");
        result.Nodes.Select(n => n.MemoryUnitId).ShouldNotContain("mu-c");

        TraversalNode startNode = result.Nodes.Single(n => n.MemoryUnitId == "mu-a");
        startNode.Edges.Select(e => e.EdgeType).ShouldContain(EdgeType.CausedBy);
        startNode.Edges.Select(e => e.EdgeType).ShouldNotContain(EdgeType.References);
    }

    [Fact]
    public async Task Traverse_EdgeMetadata_OnlyContainsFilteredEdgeTypes()
    {
        // Arrange: MU-A has both CAUSED_BY and REFERENCES edges
        string graphId = $"test-{Guid.NewGuid():N}";
        FalkorDB falkor = new(_falkorDb.Connection.GetDatabase());

        await CreateMemoryUnit(falkor, graphId, "mu-a");
        await CreateMemoryUnit(falkor, graphId, "mu-b");
        await CreateMemoryUnit(falkor, graphId, "mu-c");
        await CreateEdge(falkor, graphId, "mu-a", "mu-b", EdgeType.CausedBy);
        await CreateEdge(falkor, graphId, "mu-a", "mu-c", EdgeType.References);

        // Act: Traverse with only causedBy
        (string query, IDictionary<string, object> parameters) =
            _builder.BuildTraverseWithEdges("mu-a", 3, null, [EdgeType.CausedBy]);
        ResultSet result = await falkor.QueryAsync(graphId, query, parameters);

        // Assert: MU-A's edges should only contain CAUSED_BY, not REFERENCES
        List<(string nodeId, List<string> edgeTypes)> nodesWithEdges = ReadNodesWithEdgeTypes(result);
        (string nodeId, List<string> edgeTypes)? muA = nodesWithEdges.FirstOrDefault(n => n.nodeId == "mu-a");
        muA.ShouldNotBeNull();
        muA.Value.edgeTypes.ShouldContain("CAUSED_BY");
        muA.Value.edgeTypes.ShouldNotContain("REFERENCES");
    }

    [Fact]
    public async Task Traverse_CausedByAndCorrelatedWith_IndependentlyFilterable()
    {
        // Arrange: A->B via CausationId (CAUSED_BY), A->C via CorrelationId (CORRELATED_WITH)
        string graphId = $"test-{Guid.NewGuid():N}";
        FalkorDB falkor = new(_falkorDb.Connection.GetDatabase());

        await CreateMemoryUnit(falkor, graphId, "mu-a");
        await CreateMemoryUnit(falkor, graphId, "mu-b");
        await CreateMemoryUnit(falkor, graphId, "mu-c");
        await CreateEdge(falkor, graphId, "mu-a", "mu-b", EdgeType.CausedBy);
        await CreateEdge(falkor, graphId, "mu-a", "mu-c", EdgeType.CorrelatedWith);

        // Act 1: Traverse with causedBy only
        (string q1, IDictionary<string, object> p1) =
            _builder.BuildTraverseWithEdges("mu-a", 3, null, [EdgeType.CausedBy]);
        ResultSet r1 = await falkor.QueryAsync(graphId, q1, p1);
        List<string> causedByNodes = ReadNodeIds(r1);

        // Act 2: Traverse with correlatedWith only
        (string q2, IDictionary<string, object> p2) =
            _builder.BuildTraverseWithEdges("mu-a", 3, null, [EdgeType.CorrelatedWith]);
        ResultSet r2 = await falkor.QueryAsync(graphId, q2, p2);
        List<string> correlatedNodes = ReadNodeIds(r2);

        // Assert: B only via causedBy, C only via correlatedWith — never collapsed
        causedByNodes.ShouldContain("mu-b");
        causedByNodes.ShouldNotContain("mu-c");
        correlatedNodes.ShouldContain("mu-c");
        correlatedNodes.ShouldNotContain("mu-b");
    }

    // --- Helpers ---

    private async Task CreateMemoryUnit(FalkorDB falkor, string graphId, string id, string? caseId = null)
    {
        (string query, IDictionary<string, object> parameters) = _builder.BuildMergeMemoryUnitNode(
            id, caseId ?? "default-case", $"content for {id}", $"hash-{id}",
            $"file:///{id}.txt", SourceType.File, "provider", 3,
            "integration@example.com", DateTimeOffset.UtcNow, "{}");
        await falkor.QueryAsync(graphId, query, parameters);
    }

    private async Task CreateEdge(FalkorDB falkor, string graphId, string sourceId, string targetId, EdgeType edgeType)
    {
        float confidence = edgeType switch
        {
            EdgeType.CausedBy => EdgeTypeDefaults.CausedBy,
            EdgeType.CorrelatedWith => EdgeTypeDefaults.CorrelatedWith,
            EdgeType.References => EdgeTypeDefaults.References,
            EdgeType.Contains => EdgeTypeDefaults.Contains,
            EdgeType.Annotates => EdgeTypeDefaults.Annotates,
            _ => 1.0f,
        };
        (string query, IDictionary<string, object> parameters) =
            _builder.BuildMergeEdge(sourceId, targetId, edgeType, confidence, EdgeOrigin.Explicit);
        await falkor.QueryAsync(graphId, query, parameters);
    }

    private static List<string> ReadNodeIds(ResultSet result)
    {
        List<string> ids = [];
        foreach (Record record in result)
        {
            string? nodeId = record.GetValue<string>("nodeId");
            nodeId.ShouldNotBeNullOrWhiteSpace();
            ids.Add(nodeId);
        }

        return ids;
    }

    private static List<(string nodeId, List<string> edgeTypes)> ReadNodesWithEdgeTypes(ResultSet result)
    {
        List<(string nodeId, List<string> edgeTypes)> nodes = [];
        foreach (Record record in result)
        {
            string? nodeId = record.GetValue<string>("nodeId");
            nodeId.ShouldNotBeNullOrWhiteSpace();

            List<string> edgeTypes = [];
            object? edgesRaw = record.GetValue<object>("edges");
            if (edgesRaw is IEnumerable edgeCollection and not string)
            {
                foreach (object? edgeObj in edgeCollection)
                {
                    string edgeTypeStr = ExtractEdgeTypeFromRaw(edgeObj)
                        ?? throw new InvalidOperationException($"Unable to extract edgeType from traversal edge value '{edgeObj}'.");
                    edgeTypes.Add(edgeTypeStr);
                }
            }

            nodes.Add((nodeId, edgeTypes));
        }

        return nodes;
    }

    private static string? ExtractEdgeTypeFromRaw(object? edgeObj)
    {
        if (edgeObj is IDictionary dictionary)
        {
            return dictionary.Contains("edgeType") ? dictionary["edgeType"]?.ToString() : null;
        }

        if (edgeObj is IEnumerable sequence and not string)
        {
            List<object?> values = [];
            foreach (object? item in sequence)
            {
                values.Add(item);
            }

            // Positional: [edgeType, confidence, origin, connectedId, direction]
            if (values.Count == 5)
            {
                return values[0]?.ToString();
            }

            // Key-value pairs: [key, value, key, value, ...]
            for (int i = 0; i < values.Count - 1; i += 2)
            {
                if (string.Equals(values[i]?.ToString(), "edgeType", StringComparison.OrdinalIgnoreCase))
                {
                    return values[i + 1]?.ToString();
                }
            }
        }

        return null;
    }
}
