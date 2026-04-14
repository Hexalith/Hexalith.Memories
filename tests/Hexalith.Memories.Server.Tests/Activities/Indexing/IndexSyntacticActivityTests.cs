namespace Hexalith.Memories.Server.Tests.Activities.Indexing;

using System.Text.Json;

using Dapr.Workflow;

using Hexalith.Memories.Contracts.V1;
using Hexalith.Memories.Server.Activities.Indexing;
using Microsoft.Extensions.Logging;

using NSubstitute;
using NSubstitute.ExceptionExtensions;

using Shouldly;

using StackExchange.Redis;

public class IndexSyntacticActivityTests
{
    [Fact]
    public async Task RunAsync_ShouldStoreHashWithCorrectKeyAndFields()
    {
        // Arrange
        IDatabase db = Substitute.For<IDatabase>();
        IConnectionMultiplexer redis = CreateMockMultiplexer(db);
        ILogger<IndexSyntacticActivity> logger = Substitute.For<ILogger<IndexSyntacticActivity>>();
        IndexInput input = CreateTestInput();
        WorkflowActivityContext context = Substitute.For<WorkflowActivityContext>();

        IndexSyntacticActivity activity = new(redis, logger);

        // Act
        IndexResult result = await activity.RunAsync(context, input);

        // Assert
        result.Backend.ShouldBe("syntactic");
        result.MemoryUnitId.ShouldBe(input.MemoryUnitId);
        result.TenantId.ShouldBe(input.TenantId);

        await db.Received(1).HashSetAsync(
            $"{input.TenantId}:mu:{input.MemoryUnitId}",
            Arg.Is<HashEntry[]>(entries =>
                HasEntry(entries, "content", input.Content)
                && HasEntry(entries, "sourceUri", input.SourceUri)
                && HasEntry(entries, "sourceUriText", input.SourceUri)
                && HasEntry(entries, "sourceType", "file")
                && HasEntry(entries, "sourceTypeText", "file")
                && HasEntry(entries, "metadataText", "priority urgent human")
                && HasEntry(entries, "metadataJson", JsonSerializer.Serialize(input.Metadata, MemoriesJsonContext.Options))
                && HasEntry(entries, "ingestedBy", input.IngestedBy)
                && HasEntry(entries, "ingestedAt", input.IngestedAt.ToString("o"))),
            Arg.Any<CommandFlags>());
    }

    [Fact]
    public async Task RunAsync_ShouldUseTenantNamespacedKey()
    {
        // Arrange
        IDatabase db = Substitute.For<IDatabase>();
        IConnectionMultiplexer redis = CreateMockMultiplexer(db);
        ILogger<IndexSyntacticActivity> logger = Substitute.For<ILogger<IndexSyntacticActivity>>();
        IndexInput input = CreateTestInput();
        WorkflowActivityContext context = Substitute.For<WorkflowActivityContext>();

        IndexSyntacticActivity activity = new(redis, logger);

        // Act
        await activity.RunAsync(context, input);

        // Assert
        await db.Received(1).HashSetAsync(
            Arg.Is<RedisKey>(k => k.ToString() == "test-tenant:mu:test-mu-001"),
            Arg.Any<HashEntry[]>(),
            Arg.Any<CommandFlags>());
    }

    [Fact]
    public async Task RunAsync_WhenRedisConnectionFails_ShouldPropagateException()
    {
        // Arrange
        IDatabase db = Substitute.For<IDatabase>();
        db.HashSetAsync(Arg.Any<RedisKey>(), Arg.Any<HashEntry[]>(), Arg.Any<CommandFlags>())
            .ThrowsAsync(new RedisConnectionException(ConnectionFailureType.UnableToConnect, "Connection refused"));

        IConnectionMultiplexer redis = CreateMockMultiplexer(db);
        ILogger<IndexSyntacticActivity> logger = Substitute.For<ILogger<IndexSyntacticActivity>>();
        IndexInput input = CreateTestInput();
        WorkflowActivityContext context = Substitute.For<WorkflowActivityContext>();

        IndexSyntacticActivity activity = new(redis, logger);

        // Act & Assert
        await Should.ThrowAsync<RedisConnectionException>(
            () => activity.RunAsync(context, input));
    }

    [Fact]
    public async Task RunAsync_InvalidTenantId_ShouldThrow()
    {
        // Arrange
        IDatabase db = Substitute.For<IDatabase>();
        IConnectionMultiplexer redis = CreateMockMultiplexer(db);
        ILogger<IndexSyntacticActivity> logger = Substitute.For<ILogger<IndexSyntacticActivity>>();
        IndexInput input = CreateTestInput() with { TenantId = "bad tenant; DROP" };
        WorkflowActivityContext context = Substitute.For<WorkflowActivityContext>();

        IndexSyntacticActivity activity = new(redis, logger);

        // Act & Assert
        await Should.ThrowAsync<ArgumentException>(
            () => activity.RunAsync(context, input));
    }

    // Story 5.5 AC6 / FR70 — persist the embedding model alongside the existing provider entry.
    [Fact]
    public async Task RunAsync_ShouldPersistEmbeddingModelHashField()
    {
        IDatabase db = Substitute.For<IDatabase>();
        IConnectionMultiplexer redis = CreateMockMultiplexer(db);
        ILogger<IndexSyntacticActivity> logger = Substitute.For<ILogger<IndexSyntacticActivity>>();
        IndexInput input = CreateTestInput() with { EmbeddingModel = "gemini-embedding-001" };
        WorkflowActivityContext context = Substitute.For<WorkflowActivityContext>();

        IndexSyntacticActivity activity = new(redis, logger);

        await activity.RunAsync(context, input);

        await db.Received(1).HashSetAsync(
            $"{input.TenantId}:mu:{input.MemoryUnitId}",
            Arg.Is<HashEntry[]>(entries =>
                HasEntry(entries, "embeddingModel", "gemini-embedding-001")
                && HasEntry(entries, "embeddingProvider", input.EmbeddingProvider)),
            Arg.Any<CommandFlags>());
    }

    private static IConnectionMultiplexer CreateMockMultiplexer(IDatabase db)
    {
        // SearchCommands.Create() calls db.Execute("FT.CREATE", ...) internally.
        // Make it throw "Index already exists" so the activity's try/catch handles it
        // and proceeds to HashSetAsync (the part we actually want to test).
        db.Execute(Arg.Any<string>(), Arg.Any<object[]>())
            .Returns(_ => throw new RedisServerException("Index already exists"));
        db.Execute(Arg.Any<string>(), Arg.Any<ICollection<object>>(), Arg.Any<CommandFlags>())
            .Returns(_ => throw new RedisServerException("Index already exists"));

        IConnectionMultiplexer redis = Substitute.For<IConnectionMultiplexer>();
        redis.GetDatabase(Arg.Any<int>(), Arg.Any<object>()).Returns(db);
        return redis;
    }

    private static bool HasEntry(IEnumerable<HashEntry> entries, string name, string value)
    {
        foreach (HashEntry entry in entries)
        {
            if (entry.Name == name && entry.Value.ToString() == value)
            {
                return true;
            }
        }

        return false;
    }

    private static IndexInput CreateTestInput() => new()
    {
        MemoryUnitId = "test-mu-001",
        TenantId = "test-tenant",
        CaseId = "test-case-001",
        Content = "Test content for indexing",
        ContentHash = "abc123hash",
        SourceUri = "file:///test.txt",
        SourceType = SourceType.File,
        IngestedBy = "test-user@example.com",
        IngestedAt = DateTimeOffset.Parse("2026-03-29T10:00:00+00:00"),
        EmbeddingVector = new float[] { 0.1f, 0.2f, 0.3f },
        EmbeddingProvider = "google:text-embedding-004",
        EmbeddingModel = "gemini-embedding-001",
        EmbeddingDimensions = 3,
        Metadata = new Dictionary<string, MetadataField>
        {
            ["priority"] = new("urgent", MetadataOrigin.Human, 1.0f),
        },
    };
}
