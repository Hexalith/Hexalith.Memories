namespace Hexalith.Memories.Server.Tests.Actors;

using System.Reflection;

using Dapr.Actors;
using Dapr.Actors.Runtime;

using Hexalith.Memories.Server.Actors;

using Microsoft.Extensions.Logging.Abstractions;

using NSubstitute;

using Shouldly;

using StackExchange.Redis;

public class CorpusStatisticsActorTests
{
    private const string TenantId = "test-tenant";

    [Fact]
    public async Task GetDocumentCountAsync_WithExistingState_ShouldReturnCachedValue()
    {
        // Arrange
        (CorpusStatisticsActor actor, IActorStateManager stateManager) = CreateActorWithMockState();
        CorpusStatistics stats = new(100, 5242.88, DateTimeOffset.UtcNow);
        SetupExistingState(stateManager, stats);

        // Act
        int docCount = await actor.GetDocumentCountAsync();

        // Assert
        docCount.ShouldBe(100);
        await stateManager.Received(1).SetStateAsync("corpusStats", stats, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetAverageDocumentLengthAsync_WithExistingState_ShouldReturnCachedValue()
    {
        // Arrange
        (CorpusStatisticsActor actor, IActorStateManager stateManager) = CreateActorWithMockState();
        CorpusStatistics stats = new(100, 5242.88, DateTimeOffset.UtcNow);
        SetupExistingState(stateManager, stats);

        // Act
        double avgDocLen = await actor.GetAverageDocumentLengthAsync();

        // Assert
        avgDocLen.ShouldBe(5242.88);
        await stateManager.Received(1).SetStateAsync("corpusStats", stats, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetStatisticsAsync_WithExistingState_ShouldReturnFullSnapshot()
    {
        // Arrange
        DateTimeOffset refreshedAt = DateTimeOffset.UtcNow;
        (CorpusStatisticsActor actor, IActorStateManager stateManager) = CreateActorWithMockState();
        CorpusStatistics stats = new(250, 4096.0, refreshedAt);
        SetupExistingState(stateManager, stats);

        // Act
        CorpusStatistics result = await actor.GetStatisticsAsync();

        // Assert
        result.DocumentCount.ShouldBe(250);
        result.AverageDocumentLength.ShouldBe(4096.0);
        result.LastRefreshedAt.ShouldBe(refreshedAt);
        await stateManager.Received(1).SetStateAsync("corpusStats", stats, Arg.Any<CancellationToken>());
    }

    [Fact]
    public void ParseFtInfoResult_ValidResult_ShouldExtractDocCountAndAvgLength()
    {
        // FT.INFO returns flat key-value pairs: [key1, val1, key2, val2, ...]
        // num_docs=100, doc_table_size_mb=0.5
        // avgDocLen = (0.5 * 1024 * 1024) / 100 = 5242.88
        DateTimeOffset refreshedAt = DateTimeOffset.UtcNow;
        RedisResult raw = RedisResult.Create(
        [
            RedisResult.Create(new RedisValue("index_name")),
            RedisResult.Create(new RedisValue("test-tenant:memories:idx")),
            RedisResult.Create(new RedisValue("num_docs")),
            RedisResult.Create(new RedisValue("100")),
            RedisResult.Create(new RedisValue("doc_table_size_mb")),
            RedisResult.Create(new RedisValue("0.5")),
        ]);

        CorpusStatistics stats = CorpusStatisticsActor.ParseFtInfoResult(raw, refreshedAt);

        stats.DocumentCount.ShouldBe(100);
        stats.AverageDocumentLength.ShouldBe(5242.88, tolerance: 0.01);
        stats.LastRefreshedAt.ShouldBe(refreshedAt);
    }

    [Fact]
    public void ParseFtInfoResult_EmptyResult_ShouldReturnZeroStats()
    {
        DateTimeOffset refreshedAt = DateTimeOffset.UtcNow;
        RedisResult raw = RedisResult.Create(Array.Empty<RedisResult>());

        CorpusStatistics stats = CorpusStatisticsActor.ParseFtInfoResult(raw, refreshedAt);

        stats.DocumentCount.ShouldBe(0);
        stats.AverageDocumentLength.ShouldBe(0.0);
    }

    [Fact]
    public void ParseFtInfoResult_NullResult_ShouldReturnZeroStats()
    {
        DateTimeOffset refreshedAt = DateTimeOffset.UtcNow;

        CorpusStatistics stats = CorpusStatisticsActor.ParseFtInfoResult(null!, refreshedAt);

        stats.DocumentCount.ShouldBe(0);
        stats.AverageDocumentLength.ShouldBe(0.0);
    }

    [Fact]
    public void ParseFtInfoResult_WithNestedArrays_ShouldSkipGracefully()
    {
        // Some FT.INFO fields (index_definition, attributes) contain nested arrays.
        // Parser should skip non-BulkString keys and value types gracefully.
        DateTimeOffset refreshedAt = DateTimeOffset.UtcNow;
        RedisResult raw = RedisResult.Create(
        [
            RedisResult.Create(new RedisValue("index_name")),
            RedisResult.Create(new RedisValue("test-tenant:memories:idx")),
            RedisResult.Create(new RedisValue("index_definition")),
            RedisResult.Create(Array.Empty<RedisResult>()), // nested array value — should be skipped
            RedisResult.Create(new RedisValue("num_docs")),
            RedisResult.Create(new RedisValue("42")),
            RedisResult.Create(new RedisValue("attributes")),
            RedisResult.Create(Array.Empty<RedisResult>()), // another nested array
            RedisResult.Create(new RedisValue("doc_table_size_mb")),
            RedisResult.Create(new RedisValue("1.0")),
        ]);

        CorpusStatistics stats = CorpusStatisticsActor.ParseFtInfoResult(raw, refreshedAt);

        stats.DocumentCount.ShouldBe(42);
        // avgDocLen = (1.0 * 1024 * 1024) / 42 = 24966.095...
        stats.AverageDocumentLength.ShouldBe(24966.095, tolerance: 1.0);
    }

    [Fact]
    public void ParseFtInfoResult_NaNDocTableSize_ShouldReturnZeroAverageLength()
    {
        DateTimeOffset refreshedAt = DateTimeOffset.UtcNow;
        RedisResult raw = RedisResult.Create(
        [
            RedisResult.Create(new RedisValue("num_docs")),
            RedisResult.Create(new RedisValue("42")),
            RedisResult.Create(new RedisValue("doc_table_size_mb")),
            RedisResult.Create(new RedisValue("NaN")),
        ]);

        CorpusStatistics stats = CorpusStatisticsActor.ParseFtInfoResult(raw, refreshedAt);

        stats.DocumentCount.ShouldBe(42);
        stats.AverageDocumentLength.ShouldBe(0.0);
    }

    [Fact]
    public void ParseFtInfoResult_DocumentCountAboveIntMax_ShouldReturnZeroStats()
    {
        DateTimeOffset refreshedAt = DateTimeOffset.UtcNow;
        RedisResult raw = RedisResult.Create(
        [
            RedisResult.Create(new RedisValue("num_docs")),
            RedisResult.Create(new RedisValue("2147483648")),
            RedisResult.Create(new RedisValue("doc_table_size_mb")),
            RedisResult.Create(new RedisValue("1.0")),
        ]);

        CorpusStatistics stats = CorpusStatisticsActor.ParseFtInfoResult(raw, refreshedAt);

        stats.DocumentCount.ShouldBe(0);
        stats.AverageDocumentLength.ShouldBe(0.0);
    }

    private static (CorpusStatisticsActor Actor, IActorStateManager StateManager) CreateActorWithMockState()
    {
        IActorStateManager stateManager = Substitute.For<IActorStateManager>();
        IConnectionMultiplexer redis = Substitute.For<IConnectionMultiplexer>();

        ActorHost host = ActorHost.CreateForTest<CorpusStatisticsActor>(
            new ActorTestOptions { ActorId = new ActorId(TenantId) });

        CorpusStatisticsActor actor = new(host, redis, NullLogger<CorpusStatisticsActor>.Instance);

        PropertyInfo? prop = typeof(Actor).GetProperty("StateManager", BindingFlags.Public | BindingFlags.Instance);
        prop?.SetValue(actor, stateManager);

        return (actor, stateManager);
    }

    private static void SetupExistingState(IActorStateManager stateManager, CorpusStatistics stats)
    {
        stateManager.TryGetStateAsync<CorpusStatistics>("corpusStats", Arg.Any<CancellationToken>())
            .Returns(new ConditionalValue<CorpusStatistics>(true, stats));
    }
}
