namespace Hexalith.Memories.Server.Tests.Cases;

using Hexalith.Memories.Contracts.V1;
using Hexalith.Memories.Server.Cases;

using Microsoft.Extensions.Logging.Abstractions;

using NSubstitute;
using NSubstitute.ExceptionExtensions;

using Shouldly;

using StackExchange.Redis;

public class CaseActivityServiceTests
{
    [Fact]
    public async Task RecordEventAsync_ShouldCallStreamAddWithCorrectKeyAndFields()
    {
        // Arrange
        (IConnectionMultiplexer redis, IDatabase db) = CreateMockRedis();
        CaseActivityService service = new(redis, NullLogger<CaseActivityService>.Instance);

        // Act
        bool result = await service.RecordEventAsync(
            "tenant-1", "case-001",
            CaseActivityEventType.MemoryUnitIngested,
            "user-123", "Unit indexed", "mu-abc");

        // Assert
        result.ShouldBeTrue();
        IEnumerable<NSubstitute.Core.ICall> calls = db.ReceivedCalls()
            .Where(c => c.GetMethodInfo().Name == "StreamAddAsync");
        calls.Count().ShouldBe(1);
        NSubstitute.Core.ICall call = calls.First();
        RedisKey key = (RedisKey)call.GetArguments()[0]!;
        key.ToString().ShouldBe("tenant-1:case:case-001:activity");
        NameValueEntry[] entries = (NameValueEntry[])call.GetArguments()[1]!;
        entries.ShouldContain(e => e.Name == "type" && e.Value == "memoryUnitIngested");
        entries.ShouldContain(e => e.Name == "actor" && e.Value == "user-123");
        entries.ShouldContain(e => e.Name == "description" && e.Value == "Unit indexed");
        entries.ShouldContain(e => e.Name == "memoryUnitId" && e.Value == "mu-abc");
    }

    [Fact]
    public async Task RecordEventAsync_WhenMemoryUnitIdIsNull_ShouldNotIncludeField()
    {
        // Arrange
        (IConnectionMultiplexer redis, IDatabase db) = CreateMockRedis();
        CaseActivityService service = new(redis, NullLogger<CaseActivityService>.Instance);

        // Act
        await service.RecordEventAsync(
            "tenant-1", "case-001",
            CaseActivityEventType.CaseCreated,
            "system", "Case created", null);

        // Assert
        IEnumerable<NSubstitute.Core.ICall> calls = db.ReceivedCalls()
            .Where(c => c.GetMethodInfo().Name == "StreamAddAsync");
        calls.Count().ShouldBe(1);
        NameValueEntry[] entries = (NameValueEntry[])calls.First().GetArguments()[1]!;
        entries.Length.ShouldBe(3);
        entries.ShouldNotContain(e => e.Name == "memoryUnitId");
    }

    [Fact]
    public async Task RecordEventAsync_OnException_ShouldReturnFalseAndNotThrow()
    {
        // Arrange
        IDatabase db = Substitute.For<IDatabase>();
        IConnectionMultiplexer redis = Substitute.For<IConnectionMultiplexer>();
        redis.GetDatabase(Arg.Any<int>(), Arg.Any<object>()).Returns(db);

        // Throw on any StreamAddAsync call regardless of overload
        db.StreamAddAsync(Arg.Any<RedisKey>(), Arg.Any<NameValueEntry[]>(), Arg.Any<RedisValue?>(), Arg.Any<int?>(), Arg.Any<bool>(), Arg.Any<CommandFlags>())
            .ThrowsAsync(new RedisConnectionException(ConnectionFailureType.UnableToConnect, "Connection refused"));
        db.StreamAddAsync(Arg.Any<RedisKey>(), Arg.Any<NameValueEntry[]>(), Arg.Any<RedisValue?>(), Arg.Any<long?>(), Arg.Any<bool>(), Arg.Any<long?>(), Arg.Any<StreamTrimMode>(), Arg.Any<CommandFlags>())
            .ThrowsAsync(new RedisConnectionException(ConnectionFailureType.UnableToConnect, "Connection refused"));

        CaseActivityService service = new(redis, NullLogger<CaseActivityService>.Instance);

        // Act
        bool result = await service.RecordEventAsync(
            "tenant-1", "case-001",
            CaseActivityEventType.CaseCreated,
            "system", "Case created", null);

        // Assert
        result.ShouldBeFalse();
    }

    [Fact]
    public async Task GetRecentActivityAsync_ShouldReturnParsedEvents()
    {
        // Arrange
        (IConnectionMultiplexer redis, IDatabase db) = CreateMockRedis();
        StreamEntry[] fakeEntries =
        [
            CreateStreamEntry("1712345678901-0", "memoryUnitIngested", "user-1", "Ingested", "mu-1"),
            CreateStreamEntry("1712345678900-0", "caseCreated", "system", "Created", null),
        ];
        db.StreamRangeAsync(
            Arg.Any<RedisKey>(),
            Arg.Any<RedisValue?>(),
            Arg.Any<RedisValue?>(),
            Arg.Any<int?>(),
            Arg.Is<Order>(o => o == Order.Descending),
            Arg.Any<CommandFlags>())
            .Returns(_ => fakeEntries);

        CaseActivityService service = new(redis, NullLogger<CaseActivityService>.Instance);

        // Act
        List<CaseActivityEvent> events = await service.GetRecentActivityAsync("tenant-1", "case-001");

        // Assert
        events.Count.ShouldBe(2);
        events[0].Id.ShouldBe("1712345678901-0");
        events[0].EventType.ShouldBe(CaseActivityEventType.MemoryUnitIngested);
        events[0].Actor.ShouldBe("user-1");
        events[0].MemoryUnitId.ShouldBe("mu-1");
        events[0].Timestamp.ShouldBe(DateTimeOffset.FromUnixTimeMilliseconds(1712345678901));
        events[1].EventType.ShouldBe(CaseActivityEventType.CaseCreated);
        events[1].MemoryUnitId.ShouldBeNull();
    }

    [Fact]
    public async Task GetRecentActivityAsync_WhenStreamDoesNotExist_ShouldReturnEmptyList()
    {
        // Arrange
        (IConnectionMultiplexer redis, IDatabase db) = CreateMockRedis();
        db.StreamRangeAsync(
            Arg.Any<RedisKey>(),
            Arg.Any<RedisValue?>(),
            Arg.Any<RedisValue?>(),
            Arg.Any<int?>(),
            Arg.Any<Order>(),
            Arg.Any<CommandFlags>())
            .Returns(_ => Array.Empty<StreamEntry>());

        CaseActivityService service = new(redis, NullLogger<CaseActivityService>.Instance);

        // Act
        List<CaseActivityEvent> events = await service.GetRecentActivityAsync("tenant-1", "case-001");

        // Assert
        events.ShouldBeEmpty();
    }

    [Fact]
    public async Task GetRecentActivityAsync_OnException_ShouldReturnEmptyListAndNotThrow()
    {
        // Arrange
        (IConnectionMultiplexer redis, IDatabase db) = CreateMockRedis();
        db.StreamRangeAsync(
            Arg.Any<RedisKey>(),
            Arg.Any<RedisValue?>(),
            Arg.Any<RedisValue?>(),
            Arg.Any<int?>(),
            Arg.Any<Order>(),
            Arg.Any<CommandFlags>())
            .ThrowsAsync(new RedisConnectionException(ConnectionFailureType.UnableToConnect, "Connection refused"));

        CaseActivityService service = new(redis, NullLogger<CaseActivityService>.Instance);

        // Act
        List<CaseActivityEvent> events = await service.GetRecentActivityAsync("tenant-1", "case-001");

        // Assert
        events.ShouldBeEmpty();
    }

    [Theory]
    [InlineData(0, 1)]
    [InlineData(int.MaxValue, 500)]
    [InlineData(50, 50)]
    public async Task GetRecentActivityAsync_ShouldClampMaxEvents(int input, int expected)
    {
        // Arrange
        (IConnectionMultiplexer redis, IDatabase db) = CreateMockRedis();
        db.StreamRangeAsync(
            Arg.Any<RedisKey>(),
            Arg.Any<RedisValue?>(),
            Arg.Any<RedisValue?>(),
            Arg.Any<int?>(),
            Arg.Any<Order>(),
            Arg.Any<CommandFlags>())
            .Returns(_ => Array.Empty<StreamEntry>());

        CaseActivityService service = new(redis, NullLogger<CaseActivityService>.Instance);

        // Act
        await service.GetRecentActivityAsync("tenant-1", "case-001", input);

        // Assert
        await db.Received(1).StreamRangeAsync(
            Arg.Any<RedisKey>(),
            Arg.Any<RedisValue?>(),
            Arg.Any<RedisValue?>(),
            Arg.Is<int?>(c => c == expected),
            Arg.Any<Order>(),
            Arg.Any<CommandFlags>());
    }

    [Fact]
    public async Task GetFailedCountAsync_ShouldCountOnlyIngestionFailedEvents()
    {
        // Arrange
        (IConnectionMultiplexer redis, IDatabase db) = CreateMockRedis();
        StreamEntry[] fakeEntries =
        [
            CreateStreamEntry("1-0", "caseCreated", "system", "Created", null),
            CreateStreamEntry("2-0", "ingestionFailed", "system", "Failed 1", "mu-1"),
            CreateStreamEntry("3-0", "memoryUnitIngested", "system", "Ingested", "mu-2"),
            CreateStreamEntry("4-0", "ingestionFailed", "system", "Failed 2", "mu-3"),
        ];
        db.StreamRangeAsync(
            Arg.Any<RedisKey>(),
            Arg.Any<RedisValue?>(),
            Arg.Any<RedisValue?>(),
            Arg.Any<int?>(),
            Arg.Any<Order>(),
            Arg.Any<CommandFlags>())
            .Returns(_ => fakeEntries);

        CaseActivityService service = new(redis, NullLogger<CaseActivityService>.Instance);

        // Act
        int count = await service.GetFailedCountAsync("tenant-1", "case-001");

        // Assert
        count.ShouldBe(2);
    }

    [Fact]
    public async Task GetFailedCountAsync_WhenStreamDoesNotExist_ShouldReturnZero()
    {
        // Arrange
        (IConnectionMultiplexer redis, IDatabase db) = CreateMockRedis();
        db.StreamRangeAsync(
            Arg.Any<RedisKey>(),
            Arg.Any<RedisValue?>(),
            Arg.Any<RedisValue?>(),
            Arg.Any<int?>(),
            Arg.Any<Order>(),
            Arg.Any<CommandFlags>())
            .Returns(_ => Array.Empty<StreamEntry>());

        CaseActivityService service = new(redis, NullLogger<CaseActivityService>.Instance);

        // Act
        int count = await service.GetFailedCountAsync("tenant-1", "case-001");

        // Assert
        count.ShouldBe(0);
    }

    [Fact]
    public async Task GetFailedCountAsync_OnException_ShouldReturnZeroAndNotThrow()
    {
        // Arrange
        (IConnectionMultiplexer redis, IDatabase db) = CreateMockRedis();
        db.StreamRangeAsync(
            Arg.Any<RedisKey>(),
            Arg.Any<RedisValue?>(),
            Arg.Any<RedisValue?>(),
            Arg.Any<int?>(),
            Arg.Any<Order>(),
            Arg.Any<CommandFlags>())
            .ThrowsAsync(new RedisTimeoutException("Timed out", CommandStatus.Unknown));

        CaseActivityService service = new(redis, NullLogger<CaseActivityService>.Instance);

        // Act
        int count = await service.GetFailedCountAsync("tenant-1", "case-001");

        // Assert
        count.ShouldBe(0);
    }

    [Fact]
    public async Task GetLastActivityTimestampAsync_ShouldReturnTimestampOfMostRecentEvent()
    {
        // Arrange
        (IConnectionMultiplexer redis, IDatabase db) = CreateMockRedis();
        StreamEntry[] fakeEntries =
        [
            CreateStreamEntry("1712345678901-0", "memoryUnitIngested", "system", "Ingested", null),
        ];
        db.StreamRangeAsync(
            Arg.Any<RedisKey>(),
            Arg.Any<RedisValue?>(),
            Arg.Any<RedisValue?>(),
            Arg.Is<int?>(c => c == 1),
            Arg.Is<Order>(o => o == Order.Descending),
            Arg.Any<CommandFlags>())
            .Returns(_ => fakeEntries);

        CaseActivityService service = new(redis, NullLogger<CaseActivityService>.Instance);

        // Act
        DateTimeOffset? timestamp = await service.GetLastActivityTimestampAsync("tenant-1", "case-001");

        // Assert
        timestamp.ShouldNotBeNull();
        timestamp.Value.ShouldBe(DateTimeOffset.FromUnixTimeMilliseconds(1712345678901));
    }

    [Fact]
    public async Task GetLastActivityTimestampAsync_WhenStreamEmpty_ShouldReturnNull()
    {
        // Arrange
        (IConnectionMultiplexer redis, IDatabase db) = CreateMockRedis();
        db.StreamRangeAsync(
            Arg.Any<RedisKey>(),
            Arg.Any<RedisValue?>(),
            Arg.Any<RedisValue?>(),
            Arg.Any<int?>(),
            Arg.Any<Order>(),
            Arg.Any<CommandFlags>())
            .Returns(_ => Array.Empty<StreamEntry>());

        CaseActivityService service = new(redis, NullLogger<CaseActivityService>.Instance);

        // Act
        DateTimeOffset? timestamp = await service.GetLastActivityTimestampAsync("tenant-1", "case-001");

        // Assert
        timestamp.ShouldBeNull();
    }

    [Fact]
    public async Task GetLastActivityTimestampAsync_OnException_ShouldReturnNullAndNotThrow()
    {
        // Arrange
        (IConnectionMultiplexer redis, IDatabase db) = CreateMockRedis();
        db.StreamRangeAsync(
            Arg.Any<RedisKey>(),
            Arg.Any<RedisValue?>(),
            Arg.Any<RedisValue?>(),
            Arg.Any<int?>(),
            Arg.Any<Order>(),
            Arg.Any<CommandFlags>())
            .ThrowsAsync(new RedisConnectionException(ConnectionFailureType.UnableToConnect, "Connection refused"));

        CaseActivityService service = new(redis, NullLogger<CaseActivityService>.Instance);

        // Act
        DateTimeOffset? timestamp = await service.GetLastActivityTimestampAsync("tenant-1", "case-001");

        // Assert
        timestamp.ShouldBeNull();
    }

    private static StreamEntry CreateStreamEntry(string id, string type, string actor, string description, string? memoryUnitId)
    {
        List<NameValueEntry> values =
        [
            new NameValueEntry("type", type),
            new NameValueEntry("actor", actor),
            new NameValueEntry("description", description),
        ];

        if (memoryUnitId is not null)
        {
            values.Add(new NameValueEntry("memoryUnitId", memoryUnitId));
        }

        return new StreamEntry(id, [.. values]);
    }

    private static (IConnectionMultiplexer, IDatabase) CreateMockRedis()
    {
        IDatabase db = Substitute.For<IDatabase>();
        IConnectionMultiplexer redis = Substitute.For<IConnectionMultiplexer>();
        redis.GetDatabase(Arg.Any<int>(), Arg.Any<object>()).Returns(db);
        return (redis, db);
    }
}
