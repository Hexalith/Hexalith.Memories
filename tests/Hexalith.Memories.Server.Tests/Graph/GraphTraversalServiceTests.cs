namespace Hexalith.Memories.Server.Tests.Graph;

using Hexalith.Memories.Contracts.V1;
using Hexalith.Memories.Server.Graph;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

using NSubstitute;

using Shouldly;

using StackExchange.Redis;

public class GraphTraversalServiceTests
{
    // --- TraverseAsync: graph-not-found returns empty ---

    [Fact]
    public async Task TraverseAsync_GraphNotFound_ReturnsEmptyResult()
    {
        // Arrange
        (IConnectionMultiplexer falkorDb, IDatabase db) = CreateMockFalkorDb();
        IGraphQueryBuilder builder = new GraphQueryBuilder();
        IConnectionMultiplexer redis = Substitute.For<IConnectionMultiplexer>();
        ILogger<GraphTraversalService> logger = NullLogger<GraphTraversalService>.Instance;
        GraphTraversalService service = new(falkorDb, redis, builder, logger);

        // Simulate graph-not-found error
        db.ExecuteAsync(Arg.Any<string>(), Arg.Any<object[]>())
            .Returns<RedisResult>(x => throw new RedisServerException("Graph not found"));
        db.ExecuteAsync(Arg.Any<string>(), Arg.Any<ICollection<object>>(), Arg.Any<CommandFlags>())
            .Returns<RedisResult>(x => throw new RedisServerException("Graph not found"));

        // Act
        TraversalResult result = await service.TraverseAsync("tenant-1", "mu-001", 3, null, null, CancellationToken.None);

        // Assert
        result.StartNodeId.ShouldBe("mu-001");
        result.Depth.ShouldBe(3);
        result.Nodes.ShouldBeEmpty();
        result.TotalNodeCount.ShouldBe(0);
    }

    [Fact]
    public async Task TraverseAsync_EmptyGraph_ReturnsEmptyResult()
    {
        // Arrange
        (IConnectionMultiplexer falkorDb, IDatabase _) = CreateMockFalkorDbWithEmptyResult();
        IGraphQueryBuilder builder = new GraphQueryBuilder();
        IConnectionMultiplexer redis = Substitute.For<IConnectionMultiplexer>();
        ILogger<GraphTraversalService> logger = NullLogger<GraphTraversalService>.Instance;
        GraphTraversalService service = new(falkorDb, redis, builder, logger);

        // Act
        TraversalResult result = await service.TraverseAsync("tenant-1", "mu-001", 0, null, null, CancellationToken.None);

        // Assert
        result.StartNodeId.ShouldBe("mu-001");
        result.Depth.ShouldBe(0);
        result.Nodes.ShouldBeEmpty();
        result.TotalNodeCount.ShouldBe(0);
    }

    // --- ParseEdgeType mapping tests ---

    [Theory]
    [InlineData("CAUSED_BY", EdgeType.CausedBy)]
    [InlineData("CORRELATED_WITH", EdgeType.CorrelatedWith)]
    [InlineData("REFERENCES", EdgeType.References)]
    [InlineData("CONTAINS", EdgeType.Contains)]
    [InlineData("ANNOTATES", EdgeType.Annotates)]
    public void ParseEdgeType_KnownLabels_ShouldMapCorrectly(string cypherLabel, EdgeType expected)
    {
        GraphTraversalService.ParseEdgeType(cypherLabel).ShouldBe(expected);
    }

    [Fact]
    public void ParseEdgeType_UnknownLabel_ShouldThrow()
    {
        Should.Throw<ArgumentOutOfRangeException>(() => GraphTraversalService.ParseEdgeType("UNKNOWN_TYPE"));
    }

    // --- ParseEdgeOrigin mapping tests ---

    [Theory]
    [InlineData("explicit", EdgeOrigin.Explicit)]
    [InlineData("inferred", EdgeOrigin.Inferred)]
    public void ParseEdgeOrigin_KnownValues_ShouldMapCorrectly(string value, EdgeOrigin expected)
    {
        GraphTraversalService.ParseEdgeOrigin(value).ShouldBe(expected);
    }

    [Fact]
    public void ParseEdgeOrigin_UnknownValue_ShouldThrow()
    {
        Should.Throw<ArgumentOutOfRangeException>(() => GraphTraversalService.ParseEdgeOrigin("unknown"));
    }

    [Fact]
    public void ParseEdgeCollection_DictionaryShape_ShouldMapEdge()
    {
        List<object> edgesRaw =
        [
            new Dictionary<string, object?>
            {
                ["edgeType"] = "CAUSED_BY",
                ["confidence"] = 0.75d,
                ["origin"] = "explicit",
                ["connectedId"] = "mu-002",
                ["direction"] = "outgoing",
            },
        ];

        List<TraversalEdgeInfo> edges = GraphTraversalService.ParseEdgeCollection(edgesRaw);

        edges.Count.ShouldBe(1);
        edges[0].EdgeType.ShouldBe(EdgeType.CausedBy);
        edges[0].Confidence.ShouldBe(0.75f);
        edges[0].Origin.ShouldBe(EdgeOrigin.Explicit);
        edges[0].ConnectedNodeId.ShouldBe("mu-002");
        edges[0].Direction.ShouldBe("outgoing");
    }

    [Fact]
    public void ParseEdgeCollection_RedisValueArrayShape_ShouldMapEdge()
    {
        List<object> edgesRaw =
        [
            new RedisValue[] { "CORRELATED_WITH", "0.8", "inferred", "mu-003", "incoming" },
        ];

        List<TraversalEdgeInfo> edges = GraphTraversalService.ParseEdgeCollection(edgesRaw);

        edges.Count.ShouldBe(1);
        edges[0].EdgeType.ShouldBe(EdgeType.CorrelatedWith);
        edges[0].Confidence.ShouldBe(0.8f);
        edges[0].Origin.ShouldBe(EdgeOrigin.Inferred);
        edges[0].ConnectedNodeId.ShouldBe("mu-003");
        edges[0].Direction.ShouldBe("incoming");
    }

    [Fact]
    public void ParseEdgeCollection_UnknownOrMalformedEntries_ShouldSkipThem()
    {
        List<object> edgesRaw =
        [
            new object[] { "UNKNOWN_EDGE", 1.0d, "explicit", "mu-004", "outgoing" },
            42,
        ];

        List<TraversalEdgeInfo> edges = GraphTraversalService.ParseEdgeCollection(edgesRaw);

        edges.ShouldBeEmpty();
    }

    // --- ParseSourceType mapping tests ---

    [Theory]
    [InlineData("file", SourceType.File)]
    [InlineData("url", SourceType.Url)]
    [InlineData("event", SourceType.Event)]
    [InlineData("command", SourceType.Command)]
    [InlineData("projection", SourceType.Projection)]
    [InlineData("discussion", SourceType.Discussion)]
    [InlineData("annotation", SourceType.Annotation)]
    public void ParseSourceType_KnownValues_ShouldMapCorrectly(string value, SourceType expected)
    {
        GraphTraversalService.ParseSourceType(value).ShouldBe(expected);
    }

    [Fact]
    public void ParseSourceType_UnknownValue_ShouldThrow()
    {
        Should.Throw<ArgumentOutOfRangeException>(() => GraphTraversalService.ParseSourceType("unknown"));
    }

    // --- TruncateContent tests ---

    [Fact]
    public void TruncateContent_ShortContent_ShouldReturnAsIs()
    {
        string content = "Short content under 200 chars";
        GraphTraversalService.TruncateContent(content).ShouldBe(content);
    }

    [Fact]
    public void TruncateContent_Exactly200Chars_ShouldReturnAsIs()
    {
        string content = new('x', 200);
        GraphTraversalService.TruncateContent(content).ShouldBe(content);
    }

    [Fact]
    public void TruncateContent_LongContent_ShouldTruncateAtWordBoundary()
    {
        string content = new string('a', 150) + " " + new string('b', 100);
        string result = GraphTraversalService.TruncateContent(content);

        result.Length.ShouldBeLessThanOrEqualTo(204); // 200 + "..."
        result.ShouldEndWith("...");
    }

    [Fact]
    public void TruncateContent_LongContentNoSpaces_ShouldTruncateAt200()
    {
        string content = new('x', 300);
        string result = GraphTraversalService.TruncateContent(content);

        result.ShouldBe(new string('x', 200) + "...");
    }

    // --- EdgeType forwarding tests (Story 4.2) ---

    [Fact]
    public async Task TraverseAsync_WithEdgeTypes_PassesToQueryBuilder()
    {
        // Arrange
        (IConnectionMultiplexer falkorDb, IDatabase db) = CreateMockFalkorDb();
        IGraphQueryBuilder builder = Substitute.For<IGraphQueryBuilder>();
        IConnectionMultiplexer redis = Substitute.For<IConnectionMultiplexer>();
        ILogger<GraphTraversalService> logger = NullLogger<GraphTraversalService>.Instance;
        GraphTraversalService service = new(falkorDb, redis, builder, logger);

        List<EdgeType> edgeTypes = [EdgeType.CausedBy, EdgeType.CorrelatedWith];
        builder.BuildTraverseWithEdges("mu-001", 3, null, edgeTypes)
            .Returns(("MATCH p = (start:MemoryUnit {id: $startId}) RETURN start", new Dictionary<string, object> { ["startId"] = "mu-001" }));

        // Simulate graph-not-found to short-circuit execution
        db.ExecuteAsync(Arg.Any<string>(), Arg.Any<object[]>())
            .Returns<RedisResult>(x => throw new RedisServerException("Graph not found"));
        db.ExecuteAsync(Arg.Any<string>(), Arg.Any<ICollection<object>>(), Arg.Any<CommandFlags>())
            .Returns<RedisResult>(x => throw new RedisServerException("Graph not found"));

        // Act
        _ = await service.TraverseAsync("tenant-1", "mu-001", 3, null, edgeTypes, CancellationToken.None);

        // Assert — verify the 4-param overload was called with the correct edge types
        builder.Received(1).BuildTraverseWithEdges("mu-001", 3, null, edgeTypes);
    }

    [Fact]
    public async Task TraverseAsync_WithNullEdgeTypes_PassesNullToQueryBuilder()
    {
        // Arrange
        (IConnectionMultiplexer falkorDb, IDatabase db) = CreateMockFalkorDb();
        IGraphQueryBuilder builder = Substitute.For<IGraphQueryBuilder>();
        IConnectionMultiplexer redis = Substitute.For<IConnectionMultiplexer>();
        ILogger<GraphTraversalService> logger = NullLogger<GraphTraversalService>.Instance;
        GraphTraversalService service = new(falkorDb, redis, builder, logger);

        builder.BuildTraverseWithEdges("mu-001", 3, null, null)
            .Returns(("MATCH p = (start:MemoryUnit {id: $startId}) RETURN start", new Dictionary<string, object> { ["startId"] = "mu-001" }));

        db.ExecuteAsync(Arg.Any<string>(), Arg.Any<object[]>())
            .Returns<RedisResult>(x => throw new RedisServerException("Graph not found"));
        db.ExecuteAsync(Arg.Any<string>(), Arg.Any<ICollection<object>>(), Arg.Any<CommandFlags>())
            .Returns<RedisResult>(x => throw new RedisServerException("Graph not found"));

        // Act
        _ = await service.TraverseAsync("tenant-1", "mu-001", 3, null, null, CancellationToken.None);

        // Assert — null forwarded, not default-resolved at service level
        builder.Received(1).BuildTraverseWithEdges("mu-001", 3, null, null);
    }

    // --- Helpers ---

    private static (IConnectionMultiplexer, IDatabase) CreateMockFalkorDb()
    {
        IDatabase db = Substitute.For<IDatabase>();
        IConnectionMultiplexer falkorDb = Substitute.For<IConnectionMultiplexer>();
        falkorDb.GetDatabase(Arg.Any<int>(), Arg.Any<object>()).Returns(db);
        return (falkorDb, db);
    }

    private static (IConnectionMultiplexer, IDatabase) CreateMockFalkorDbWithEmptyResult()
    {
        IDatabase db = Substitute.For<IDatabase>();
        IConnectionMultiplexer falkorDb = Substitute.For<IConnectionMultiplexer>();
        falkorDb.GetDatabase(Arg.Any<int>(), Arg.Any<object>()).Returns(db);

        RedisResult emptyResult = CreateEmptyFalkorDbResult();
        db.ExecuteAsync(Arg.Any<string>(), Arg.Any<object[]>())
            .Returns(emptyResult);
        db.ExecuteAsync(Arg.Any<string>(), Arg.Any<ICollection<object>>(), Arg.Any<CommandFlags>())
            .Returns(emptyResult);

        return (falkorDb, db);
    }

    private static RedisResult CreateEmptyFalkorDbResult() => RedisResult.Create(
    [
        RedisResult.Create(Array.Empty<RedisResult>()),
        RedisResult.Create(Array.Empty<RedisResult>()),
        RedisResult.Create(
        [
            RedisResult.Create(new RedisValue("Nodes created: 0")),
            RedisResult.Create(new RedisValue("Properties set: 0")),
            RedisResult.Create(new RedisValue("Relationships created: 0")),
            RedisResult.Create(new RedisValue("Cached execution: 0")),
            RedisResult.Create(new RedisValue("Query internal execution time: 0.1 milliseconds")),
        ]),
    ]);
}
