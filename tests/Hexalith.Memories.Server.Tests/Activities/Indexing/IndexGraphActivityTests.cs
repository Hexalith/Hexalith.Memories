namespace Hexalith.Memories.Server.Tests.Activities.Indexing;

using System.Text.Json;

using Dapr.Workflow;

using Hexalith.Memories.Contracts.V1;
using Hexalith.Memories.Server.Activities.Indexing;
using Hexalith.Memories.Server.Graph;

using Microsoft.Extensions.Logging;

using NSubstitute;

using Shouldly;

using StackExchange.Redis;

public class IndexGraphActivityTests
{
    [Fact]
    public async Task RunAsync_ShouldCallBuilderInCorrectOrder()
    {
        // Arrange
        IGraphQueryBuilder builder = CreateMockBuilder();
        (IConnectionMultiplexer falkorDb, IDatabase db) = CreateMockFalkorDb();
        ILogger<IndexGraphActivity> logger = Substitute.For<ILogger<IndexGraphActivity>>();
        IndexInput input = CreateTestInput();
        WorkflowActivityContext context = Substitute.For<WorkflowActivityContext>();

        IndexGraphActivity activity = new(falkorDb, builder, logger);

        // Act
        IndexResult result = await activity.RunAsync(context, input);

        // Assert
        result.Backend.ShouldBe("graph");
        result.MemoryUnitId.ShouldBe(input.MemoryUnitId);
        result.TenantId.ShouldBe(input.TenantId);

        Received.InOrder(() =>
        {
            builder.BuildMergeCaseNode(input.CaseId);
            builder.BuildMergeMemoryUnitNode(
                input.MemoryUnitId, input.CaseId, input.Content, input.ContentHash,
                input.SourceUri, input.SourceType, input.EmbeddingProvider,
                input.EmbeddingModel,
                input.EmbeddingDimensions, input.IngestedBy, input.IngestedAt,
                JsonSerializer.Serialize(input.Metadata, MemoriesJsonContext.Options));
            builder.BuildMergeEdge(input.CaseId, input.MemoryUnitId, EdgeType.Contains, Arg.Any<float>(), EdgeOrigin.Explicit);
        });
    }

    [Fact]
    public async Task RunAsync_WithCausationId_ShouldCreateCausedByEdge()
    {
        // Arrange
        IGraphQueryBuilder builder = CreateMockBuilder();
        (IConnectionMultiplexer falkorDb, _) = CreateMockFalkorDb();
        ILogger<IndexGraphActivity> logger = Substitute.For<ILogger<IndexGraphActivity>>();
        IndexInput input = CreateTestInput() with { CausationId = "mu-cause-001" };
        WorkflowActivityContext context = Substitute.For<WorkflowActivityContext>();

        IndexGraphActivity activity = new(falkorDb, builder, logger);

        // Act
        await activity.RunAsync(context, input);

        // Assert
        builder.Received(1).BuildMergeStubNode("mu-cause-001");
        builder.Received(1).BuildMergeEdge(
            "mu-cause-001", input.MemoryUnitId, EdgeType.CausedBy,
            EdgeTypeDefaults.CausedBy, EdgeOrigin.Explicit);
    }

    [Fact]
    public async Task RunAsync_WithCorrelationId_ShouldCreateCorrelatedWithEdge()
    {
        // Arrange
        IGraphQueryBuilder builder = CreateMockBuilder();
        (IConnectionMultiplexer falkorDb, _) = CreateMockFalkorDb();
        ILogger<IndexGraphActivity> logger = Substitute.For<ILogger<IndexGraphActivity>>();
        IndexInput input = CreateTestInput() with { CorrelationId = "mu-corr-001" };
        WorkflowActivityContext context = Substitute.For<WorkflowActivityContext>();

        IndexGraphActivity activity = new(falkorDb, builder, logger);

        // Act
        await activity.RunAsync(context, input);

        // Assert
        builder.Received(1).BuildMergeStubNode("mu-corr-001");
        builder.Received(1).BuildMergeEdge(
            "mu-corr-001", input.MemoryUnitId, EdgeType.CorrelatedWith,
            EdgeTypeDefaults.CorrelatedWith, EdgeOrigin.Explicit);
    }

    [Fact]
    public async Task RunAsync_WithoutCausationOrCorrelation_ShouldOnlyCreateContainsEdge()
    {
        // Arrange
        IGraphQueryBuilder builder = CreateMockBuilder();
        (IConnectionMultiplexer falkorDb, _) = CreateMockFalkorDb();
        ILogger<IndexGraphActivity> logger = Substitute.For<ILogger<IndexGraphActivity>>();
        IndexInput input = CreateTestInput();
        WorkflowActivityContext context = Substitute.For<WorkflowActivityContext>();

        IndexGraphActivity activity = new(falkorDb, builder, logger);

        // Act
        await activity.RunAsync(context, input);

        // Assert
        builder.Received(1).BuildMergeEdge(
            input.CaseId, input.MemoryUnitId, EdgeType.Contains,
            Arg.Any<float>(), EdgeOrigin.Explicit);

        builder.DidNotReceive().BuildMergeEdge(
            Arg.Any<string>(), Arg.Any<string>(), EdgeType.CausedBy,
            Arg.Any<float>(), Arg.Any<EdgeOrigin>());

        builder.DidNotReceive().BuildMergeEdge(
            Arg.Any<string>(), Arg.Any<string>(), EdgeType.CorrelatedWith,
            Arg.Any<float>(), Arg.Any<EdgeOrigin>());

        builder.DidNotReceive().BuildMergeStubNode(Arg.Any<string>());
    }

    [Fact]
    public async Task RunAsync_WithBothCausationAndCorrelation_ShouldCreateThreeEdges()
    {
        // Arrange
        IGraphQueryBuilder builder = CreateMockBuilder();
        (IConnectionMultiplexer falkorDb, _) = CreateMockFalkorDb();
        ILogger<IndexGraphActivity> logger = Substitute.For<ILogger<IndexGraphActivity>>();
        IndexInput input = CreateTestInput() with { CausationId = "mu-cause", CorrelationId = "mu-corr" };
        WorkflowActivityContext context = Substitute.For<WorkflowActivityContext>();

        IndexGraphActivity activity = new(falkorDb, builder, logger);

        // Act
        await activity.RunAsync(context, input);

        // Assert — 3 edges: Contains + CausedBy + CorrelatedWith
        builder.Received(3).BuildMergeEdge(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<EdgeType>(),
            Arg.Any<float>(), Arg.Any<EdgeOrigin>());

        builder.Received(2).BuildMergeStubNode(Arg.Any<string>());
    }

    [Fact]
    public async Task RunAsync_ShouldUseTenantIdAsGraphId()
    {
        // Arrange
        IGraphQueryBuilder builder = CreateMockBuilder();
        (IConnectionMultiplexer falkorDb, IDatabase db) = CreateMockFalkorDb();
        ILogger<IndexGraphActivity> logger = Substitute.For<ILogger<IndexGraphActivity>>();
        IndexInput input = CreateTestInput();
        WorkflowActivityContext context = Substitute.For<WorkflowActivityContext>();

        IndexGraphActivity activity = new(falkorDb, builder, logger);

        // Act
        await activity.RunAsync(context, input);

        // Assert — GetDatabase called and GRAPH.QUERY receives the tenant graph identifier
        falkorDb.Received().GetDatabase(Arg.Any<int>(), Arg.Any<object>());

        bool usedTenantGraphId = false;
        foreach (var call in db.ReceivedCalls())
        {
            object?[] arguments = call.GetArguments();
            if (ContainsGraphId(arguments, input.TenantId))
            {
                usedTenantGraphId = true;
                break;
            }
        }

        usedTenantGraphId.ShouldBeTrue();
    }

    [Fact]
    public async Task RunAsync_InvalidTenantId_ShouldThrow()
    {
        // Arrange
        IGraphQueryBuilder builder = CreateMockBuilder();
        (IConnectionMultiplexer falkorDb, _) = CreateMockFalkorDb();
        ILogger<IndexGraphActivity> logger = Substitute.For<ILogger<IndexGraphActivity>>();
        IndexInput input = CreateTestInput() with { TenantId = "bad tenant; DROP" };
        WorkflowActivityContext context = Substitute.For<WorkflowActivityContext>();

        IndexGraphActivity activity = new(falkorDb, builder, logger);

        // Act & Assert
        await Should.ThrowAsync<ArgumentException>(
            () => activity.RunAsync(context, input));
    }

    [Fact]
    public async Task RunAsync_FalkorDbConnectionFailure_ShouldPropagateException()
    {
        // Arrange
        IGraphQueryBuilder builder = CreateMockBuilder();
        IConnectionMultiplexer falkorDb = Substitute.For<IConnectionMultiplexer>();
        falkorDb.GetDatabase(Arg.Any<int>(), Arg.Any<object>())
            .Returns(_ => throw new RedisConnectionException(ConnectionFailureType.UnableToConnect, "Connection refused"));

        ILogger<IndexGraphActivity> logger = Substitute.For<ILogger<IndexGraphActivity>>();
        IndexInput input = CreateTestInput();
        WorkflowActivityContext context = Substitute.For<WorkflowActivityContext>();

        IndexGraphActivity activity = new(falkorDb, builder, logger);

        // Act & Assert
        await Should.ThrowAsync<RedisConnectionException>(
            () => activity.RunAsync(context, input));
    }

    private static IGraphQueryBuilder CreateMockBuilder()
    {
        IGraphQueryBuilder builder = Substitute.For<IGraphQueryBuilder>();

        builder.BuildMergeCaseNode(Arg.Any<string>())
            .Returns(("MERGE (c:Case {id: $caseId})", new Dictionary<string, object> { ["caseId"] = "mock" }));

        builder.BuildMergeMemoryUnitNode(
                Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(),
                Arg.Any<string>(), Arg.Any<SourceType>(), Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<int>(), Arg.Any<string>(), Arg.Any<DateTimeOffset>(), Arg.Any<string>())
            .Returns(("MERGE (m:MemoryUnit {id: $id})", new Dictionary<string, object> { ["id"] = "mock" }));

        builder.BuildMergeEdge(
                Arg.Any<string>(), Arg.Any<string>(), Arg.Any<EdgeType>(),
                Arg.Any<float>(), Arg.Any<EdgeOrigin>())
            .Returns(("MATCH (s), (t) MERGE (s)-[:EDGE]->(t)", new Dictionary<string, object> { ["sourceId"] = "mock" }));

        builder.BuildMergeStubNode(Arg.Any<string>())
            .Returns(("MERGE (m:MemoryUnit {id: $id})", new Dictionary<string, object> { ["id"] = "mock" }));

        return builder;
    }

    private static (IConnectionMultiplexer, IDatabase) CreateMockFalkorDb()
    {
        IDatabase db = Substitute.For<IDatabase>();
        IConnectionMultiplexer falkorDb = Substitute.For<IConnectionMultiplexer>();
        falkorDb.GetDatabase(Arg.Any<int>(), Arg.Any<object>()).Returns(db);

        // FalkorDB.QueryAsync calls db.ExecuteAsync("GRAPH.QUERY", ...) internally.
        // The result must be a 3-element array: [headers, data, statistics].
        RedisResult fakeGraphResult = RedisResult.Create(
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

        db.ExecuteAsync(Arg.Any<string>(), Arg.Any<object[]>())
            .Returns(fakeGraphResult);
        db.ExecuteAsync(Arg.Any<string>(), Arg.Any<ICollection<object>>(), Arg.Any<CommandFlags>())
            .Returns(fakeGraphResult);

        return (falkorDb, db);
    }

    private static IndexInput CreateTestInput() => new()
    {
        MemoryUnitId = "test-mu-001",
        TenantId = "test-tenant",
        CaseId = "test-case-001",
        Content = "Test content for graph indexing",
        ContentHash = "graphhash123",
        SourceUri = "file:///test.txt",
        SourceType = SourceType.File,
        IngestedBy = "test-user@example.com",
        IngestedAt = DateTimeOffset.Parse("2026-03-29T10:00:00+00:00"),
        EmbeddingVector = new float[] { 0.1f, 0.2f, 0.3f },
        EmbeddingProvider = "google:text-embedding-004",
        EmbeddingModel = "gemini-embedding-001",
        EmbeddingDimensions = 3,
    };

    private static bool ContainsGraphId(IEnumerable<object?> arguments, string graphId)
    {
        foreach (object? argument in arguments)
        {
            if (argument is object[] values && values.Length > 0 && values[0]?.ToString() == graphId)
            {
                return true;
            }

            if (argument is ICollection<object> collection)
            {
                foreach (object item in collection)
                {
                    return item?.ToString() == graphId;
                }
            }
        }

        return false;
    }
}
