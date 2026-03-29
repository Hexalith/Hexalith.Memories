namespace Hexalith.Memories.Server.Tests.Activities.Indexing;

using System.Runtime.InteropServices;

using Dapr.Workflow;

using Hexalith.Memories.Contracts.V1;
using Hexalith.Memories.Server.Activities.Indexing;

using Microsoft.Extensions.Logging;

using NSubstitute;
using NSubstitute.ExceptionExtensions;

using Shouldly;

using StackExchange.Redis;

public class IndexSemanticActivityTests
{
    [Fact]
    public async Task RunAsync_ShouldStoreVectorWithCorrectKey()
    {
        // Arrange
        IDatabase db = Substitute.For<IDatabase>();
        IConnectionMultiplexer redis = CreateMockMultiplexer(db);
        ILogger<IndexSemanticActivity> logger = Substitute.For<ILogger<IndexSemanticActivity>>();
        IndexInput input = CreateTestInput();
        WorkflowActivityContext context = Substitute.For<WorkflowActivityContext>();

        IndexSemanticActivity activity = new(redis, logger);

        // Act
        IndexResult result = await activity.RunAsync(context, input);

        // Assert
        result.Backend.ShouldBe("semantic");
        result.MemoryUnitId.ShouldBe(input.MemoryUnitId);
        result.TenantId.ShouldBe(input.TenantId);

        await db.Received(1).HashSetAsync(
            Arg.Is<RedisKey>(k => k.ToString() == "test-tenant:vec:test-mu-001"),
            Arg.Any<HashEntry[]>(),
            Arg.Any<CommandFlags>());
    }

    [Fact]
    public void VectorByteConversion_KnownGoldValues_ShouldBeExact()
    {
        // Gold values: 1.0f, 0.0f, -1.0f
        float[] vector = [1.0f, 0.0f, -1.0f];
        byte[] vectorBytes = MemoryMarshal.AsBytes(vector.AsSpan()).ToArray();

        // 3 floats * 4 bytes = 12 bytes
        vectorBytes.Length.ShouldBe(12);

        // 1.0f in little-endian IEEE 754: 0x00 0x00 0x80 0x3F
        byte[] expected1 = BitConverter.GetBytes(1.0f);
        vectorBytes[0..4].ShouldBe(expected1);

        // 0.0f: 0x00 0x00 0x00 0x00
        byte[] expected0 = BitConverter.GetBytes(0.0f);
        vectorBytes[4..8].ShouldBe(expected0);

        // -1.0f: 0x00 0x00 0x80 0xBF
        byte[] expectedNeg1 = BitConverter.GetBytes(-1.0f);
        vectorBytes[8..12].ShouldBe(expectedNeg1);
    }

    [Fact]
    public async Task RunAsync_ShouldUseTenantNamespacedKey()
    {
        // Arrange
        IDatabase db = Substitute.For<IDatabase>();
        IConnectionMultiplexer redis = CreateMockMultiplexer(db);
        ILogger<IndexSemanticActivity> logger = Substitute.For<ILogger<IndexSemanticActivity>>();
        IndexInput input = CreateTestInput();
        WorkflowActivityContext context = Substitute.For<WorkflowActivityContext>();

        IndexSemanticActivity activity = new(redis, logger);

        // Act
        await activity.RunAsync(context, input);

        // Assert
        await db.Received(1).HashSetAsync(
            Arg.Is<RedisKey>(k => k.ToString().StartsWith("test-tenant:vec:")),
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
        ILogger<IndexSemanticActivity> logger = Substitute.For<ILogger<IndexSemanticActivity>>();
        IndexInput input = CreateTestInput();
        WorkflowActivityContext context = Substitute.For<WorkflowActivityContext>();

        IndexSemanticActivity activity = new(redis, logger);

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
        ILogger<IndexSemanticActivity> logger = Substitute.For<ILogger<IndexSemanticActivity>>();
        IndexInput input = CreateTestInput() with { TenantId = "bad tenant; DROP" };
        WorkflowActivityContext context = Substitute.For<WorkflowActivityContext>();

        IndexSemanticActivity activity = new(redis, logger);

        // Act & Assert
        await Should.ThrowAsync<ArgumentException>(
            () => activity.RunAsync(context, input));
    }

    [Fact]
    public async Task RunAsync_IndexAlreadyExistsWithDifferentDimensions_ShouldThrow()
    {
        // Arrange
        IDatabase db = Substitute.For<IDatabase>();
        IConnectionMultiplexer redis = CreateMockMultiplexer(db, existingIndexDimensions: 4);
        ILogger<IndexSemanticActivity> logger = Substitute.For<ILogger<IndexSemanticActivity>>();
        IndexInput input = CreateTestInput();
        WorkflowActivityContext context = Substitute.For<WorkflowActivityContext>();

        IndexSemanticActivity activity = new(redis, logger);

        // Act & Assert
        await Should.ThrowAsync<InvalidOperationException>(
            () => activity.RunAsync(context, input));
    }

    [Fact]
    public async Task RunAsync_NullEmbeddingVector_ShouldThrow()
    {
        // Arrange
        IDatabase db = Substitute.For<IDatabase>();
        IConnectionMultiplexer redis = CreateMockMultiplexer(db);
        ILogger<IndexSemanticActivity> logger = Substitute.For<ILogger<IndexSemanticActivity>>();
        IndexInput input = CreateTestInput() with { EmbeddingVector = null! };
        WorkflowActivityContext context = Substitute.For<WorkflowActivityContext>();

        IndexSemanticActivity activity = new(redis, logger);

        // Act & Assert
        await Should.ThrowAsync<ArgumentNullException>(
            () => activity.RunAsync(context, input));
    }

    [Fact]
    public async Task RunAsync_EmptyEmbeddingVector_ShouldThrow()
    {
        // Arrange
        IDatabase db = Substitute.For<IDatabase>();
        IConnectionMultiplexer redis = CreateMockMultiplexer(db);
        ILogger<IndexSemanticActivity> logger = Substitute.For<ILogger<IndexSemanticActivity>>();
        IndexInput input = CreateTestInput() with { EmbeddingVector = [], EmbeddingDimensions = 0 };
        WorkflowActivityContext context = Substitute.For<WorkflowActivityContext>();

        IndexSemanticActivity activity = new(redis, logger);

        // Act & Assert
        await Should.ThrowAsync<ArgumentException>(
            () => activity.RunAsync(context, input));
    }

    private static IConnectionMultiplexer CreateMockMultiplexer(IDatabase db, int existingIndexDimensions = 3)
    {
        // SearchCommands.Create() calls db.Execute("FT.CREATE", ...) internally.
        // Make it throw "Index already exists" so the activity's try/catch handles it
        // and proceeds to HashSetAsync (the part we actually want to test).
        db.Execute(Arg.Any<string>(), Arg.Any<object[]>())
            .Returns(_ => throw new RedisServerException("Index already exists"));
        db.Execute(Arg.Any<string>(), Arg.Any<ICollection<object>>(), Arg.Any<CommandFlags>())
            .Returns(_ => throw new RedisServerException("Index already exists"));
        db.ExecuteAsync(Arg.Any<string>(), Arg.Any<object[]>())
            .Returns(CreateExistingIndexInfoResult(existingIndexDimensions));
        db.ExecuteAsync(Arg.Any<string>(), Arg.Any<ICollection<object>>(), Arg.Any<CommandFlags>())
            .Returns(CreateExistingIndexInfoResult(existingIndexDimensions));

        IConnectionMultiplexer redis = Substitute.For<IConnectionMultiplexer>();
        redis.GetDatabase(Arg.Any<int>(), Arg.Any<object>()).Returns(db);
        return redis;
    }

    private static RedisResult CreateExistingIndexInfoResult(int dimensions) => RedisResult.Create(
    [
        RedisResult.Create(new RedisValue("attributes")),
        RedisResult.Create(
        [
            RedisResult.Create(
            [
                RedisResult.Create(new RedisValue("identifier")),
                RedisResult.Create(new RedisValue("embedding")),
                RedisResult.Create(new RedisValue("attribute")),
                RedisResult.Create(new RedisValue("embedding")),
                RedisResult.Create(new RedisValue("type")),
                RedisResult.Create(new RedisValue("VECTOR")),
                RedisResult.Create(new RedisValue("dim")),
                RedisResult.Create(new RedisValue(dimensions.ToString())),
            ]),
        ]),
    ]);

    private static IndexInput CreateTestInput() => new()
    {
        MemoryUnitId = "test-mu-001",
        TenantId = "test-tenant",
        CaseId = "test-case-001",
        Content = "Test content",
        ContentHash = "abc123",
        SourceUri = "file:///test.txt",
        SourceType = SourceType.File,
        EmbeddingVector = new float[] { 0.1f, 0.2f, 0.3f },
        EmbeddingProvider = "google:text-embedding-004",
        EmbeddingDimensions = 3,
    };
}
