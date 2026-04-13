namespace Hexalith.Memories.Server.Tests.Cases;

using System.IO;
using System.Net;
using System.Text.RegularExpressions;

using Hexalith.Memories.Contracts.V1;
using Hexalith.Memories.Server.Cases;
using Hexalith.Memories.Server.Graph;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

using NSubstitute;

using Shouldly;

using StackExchange.Redis;

using CaseRecord = Hexalith.Memories.Contracts.V1.Case;

public class CaseServiceTests
{
    [Fact]
    public async Task CreateCaseAsync_ShouldStoreHashAndCreateGraphNode()
    {
        // Arrange
        (IConnectionMultiplexer redis, IDatabase redisDb) = CreateMockRedis();
        (IConnectionMultiplexer falkorDb, IDatabase falkorDbDb) = CreateMockFalkorDb();
        IGraphQueryBuilder builder = CreateMockBuilder();
        ILogger<CaseService> logger = NullLogger<CaseService>.Instance;
        CaseService service = new(redis, falkorDb, builder, CreateMockActivityService(), logger);

        var input = new CreateCaseInput("tenant-1", "Test Case", "A description");

        // Act
        CaseRecord result = await service.CreateCaseAsync(input, CancellationToken.None);

        // Assert
        result.TenantId.ShouldBe("tenant-1");
        result.Name.ShouldBe("Test Case");
        result.Description.ShouldBe("A description");
        result.Status.ShouldBe(CaseStatus.Active);
        result.MemoryUnitCount.ShouldBe(0);
        result.Id.ShouldNotBeNullOrWhiteSpace();
        Regex.IsMatch(result.Id, "^[0-9A-HJKMNP-TV-Z]{26}$").ShouldBeTrue();

        await redisDb.Received(1).HashSetAsync(
            Arg.Is<RedisKey>(k => k.ToString().StartsWith("tenant-1:case:")),
            Arg.Any<HashEntry[]>(),
            Arg.Any<CommandFlags>());

        builder.Received(1).BuildMergeCaseNode(
            Arg.Any<string>(), "Test Case", "tenant-1", Arg.Any<DateTimeOffset>());
    }

    [Fact]
    public async Task ListCasesAsync_ShouldReturnNewestCasesBeforeApplyingLimit()
    {
        // Arrange
        HashEntry[] olderCase = CreateCaseHash(
            id: "case-older",
            createdAt: "2026-04-01T08:00:00+00:00",
            lastUpdated: "2026-04-01T08:00:00+00:00",
            name: "Older");
        HashEntry[] newestCase = CreateCaseHash(
            id: "case-newest",
            createdAt: "2026-04-01T10:00:00+00:00",
            lastUpdated: "2026-04-01T10:00:00+00:00",
            name: "Newest");
        HashEntry[] middleCase = CreateCaseHash(
            id: "case-middle",
            createdAt: "2026-04-01T09:00:00+00:00",
            lastUpdated: "2026-04-01T09:00:00+00:00",
            name: "Middle");
        HashEntry[] activityStreamSentinel =
        [
            new("type", "caseCreated"),
        ];

        (IConnectionMultiplexer redis, IDatabase redisDb, IServer _) = CreateMockRedisWithKeys(
            ("tenant-1:case:case-older", olderCase),
            ("tenant-1:case:case-newest", newestCase),
            ("tenant-1:case:case-middle", middleCase),
            ("tenant-1:case:case-newest:activity", activityStreamSentinel));
        (IConnectionMultiplexer falkorDb, _) = CreateMockFalkorDb();
        IGraphQueryBuilder builder = CreateMockBuilder();
        ILogger<CaseService> logger = NullLogger<CaseService>.Instance;
        CaseService service = new(redis, falkorDb, builder, CreateMockActivityService(), logger);

        // Act
        List<CaseRecord> result = await service.ListCasesAsync("tenant-1", maxResults: 2, cancellationToken: CancellationToken.None);

        // Assert
        result.Select(item => item.Id).ToList().ShouldBe(["case-newest", "case-middle"]);
        builder.Received(2).BuildCountCaseMemoryUnits(Arg.Any<string>());
        await redisDb.Received(3).HashGetAllAsync(Arg.Any<RedisKey>(), Arg.Any<CommandFlags>());
        await redisDb.DidNotReceive().HashGetAllAsync("tenant-1:case:case-newest:activity", Arg.Any<CommandFlags>());
    }

    [Fact]
    public async Task ListCasesAsync_WhenTenantHasNoCases_ShouldReturnEmptyList()
    {
        // Arrange
        (IConnectionMultiplexer redis, _, _) = CreateMockRedisWithKeys();
        (IConnectionMultiplexer falkorDb, _) = CreateMockFalkorDb();
        IGraphQueryBuilder builder = CreateMockBuilder();
        ILogger<CaseService> logger = NullLogger<CaseService>.Instance;
        CaseService service = new(redis, falkorDb, builder, CreateMockActivityService(), logger);

        // Act
        List<CaseRecord> result = await service.ListCasesAsync("tenant-1", cancellationToken: CancellationToken.None);

        // Assert
        result.ShouldBeEmpty();
        builder.DidNotReceive().BuildCountCaseMemoryUnits(Arg.Any<string>());
    }

    [Fact]
    public async Task GetCaseAsync_WhenNotFound_ShouldReturnNull()
    {
        // Arrange
        (IConnectionMultiplexer redis, IDatabase redisDb) = CreateMockRedis();
        (IConnectionMultiplexer falkorDb, _) = CreateMockFalkorDb();
        IGraphQueryBuilder builder = CreateMockBuilder();
        ILogger<CaseService> logger = NullLogger<CaseService>.Instance;
        CaseService service = new(redis, falkorDb, builder, CreateMockActivityService(), logger);

        redisDb.HashGetAllAsync(Arg.Any<RedisKey>(), Arg.Any<CommandFlags>())
            .Returns(Array.Empty<HashEntry>());

        // Act
        CaseRecord? result = await service.GetCaseAsync("tenant-1", "nonexistent", CancellationToken.None);

        // Assert
        result.ShouldBeNull();
    }

    [Fact]
    public async Task GetCaseAsync_WhenFound_ShouldReturnCase()
    {
        // Arrange
        (IConnectionMultiplexer redis, IDatabase redisDb) = CreateMockRedis();
        (IConnectionMultiplexer falkorDb, _) = CreateMockFalkorDb();
        IGraphQueryBuilder builder = CreateMockBuilder();
        ILogger<CaseService> logger = NullLogger<CaseService>.Instance;
        CaseService service = new(redis, falkorDb, builder, CreateMockActivityService(), logger);

        HashEntry[] fakeHash =
        [
            new("id", "case-123"),
            new("tenantId", "tenant-1"),
            new("name", "Found Case"),
            new("description", "desc"),
            new("status", "active"),
            new("createdAt", "2026-04-01T10:00:00+00:00"),
            new("lastUpdated", "2026-04-01T11:00:00+00:00"),
            new("memoryUnitCount", "0"),
        ];
        redisDb.HashGetAllAsync(Arg.Any<RedisKey>(), Arg.Any<CommandFlags>())
            .Returns(fakeHash);

        // Act
        CaseRecord? result = await service.GetCaseAsync("tenant-1", "case-123", CancellationToken.None);

        // Assert
        result.ShouldNotBeNull();
        result!.Id.ShouldBe("case-123");
        result.TenantId.ShouldBe("tenant-1");
        result.Name.ShouldBe("Found Case");
        result.Description.ShouldBe("desc");
        result.Status.ShouldBe(CaseStatus.Active);
    }

    private static IGraphQueryBuilder CreateMockBuilder()
    {
        IGraphQueryBuilder builder = Substitute.For<IGraphQueryBuilder>();

        builder.BuildMergeCaseNode(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<DateTimeOffset>())
            .Returns(("MERGE (c:Case {id: $caseId}) SET c.name = $name", new Dictionary<string, object> { ["caseId"] = "mock" }));

        builder.BuildCountCaseMemoryUnits(Arg.Any<string>())
            .Returns(("MATCH (c:Case {id: $caseId})-[:CONTAINS]->(m) RETURN count(m) AS count", new Dictionary<string, object> { ["caseId"] = "mock" }));

        return builder;
    }

    private static (IConnectionMultiplexer, IDatabase) CreateMockRedis()
    {
        IDatabase db = Substitute.For<IDatabase>();
        IConnectionMultiplexer redis = Substitute.For<IConnectionMultiplexer>();
        redis.GetDatabase(Arg.Any<int>(), Arg.Any<object>()).Returns(db);
        return (redis, db);
    }

    private static (IConnectionMultiplexer, IDatabase, IServer) CreateMockRedisWithKeys(
        params (string Key, HashEntry[] Entries)[] cases)
    {
        IDatabase db = Substitute.For<IDatabase>();
        IConnectionMultiplexer redis = Substitute.For<IConnectionMultiplexer>();
        IServer server = Substitute.For<IServer>();
        EndPoint endpoint = new DnsEndPoint("localhost", 6379);
        Dictionary<string, HashEntry[]> entriesByKey = cases.ToDictionary(item => item.Key, item => item.Entries);

        redis.GetDatabase(Arg.Any<int>(), Arg.Any<object>()).Returns(db);
        db.Multiplexer.Returns(redis);
        redis.GetEndPoints(Arg.Any<bool>()).Returns([endpoint]);
        redis.GetServer(Arg.Any<EndPoint>(), Arg.Any<object>()).Returns(server);
        server.Keys(Arg.Any<int>(), Arg.Any<RedisValue>(), Arg.Any<int>(), Arg.Any<long>(), Arg.Any<int>(), Arg.Any<CommandFlags>())
            .Returns(cases.Select(item => (RedisKey)item.Key).ToArray());
        db.HashGetAllAsync(Arg.Any<RedisKey>(), Arg.Any<CommandFlags>())
            .Returns(callInfo =>
            {
                string key = callInfo.Arg<RedisKey>().ToString();
                return entriesByKey.TryGetValue(key, out HashEntry[]? entries)
                    ? entries
                    : Array.Empty<HashEntry>();
            });

        return (redis, db, server);
    }

    [Fact]
    public async Task GetCaseStatusAsync_WhenNotFound_ShouldReturnNull()
    {
        // Arrange
        (IConnectionMultiplexer redis, IDatabase redisDb) = CreateMockRedis();
        (IConnectionMultiplexer falkorDb, _) = CreateMockFalkorDb();
        IGraphQueryBuilder builder = CreateMockBuilder();
        ILogger<CaseService> logger = NullLogger<CaseService>.Instance;
        CaseService service = new(redis, falkorDb, builder, CreateMockActivityService(), logger);

        redisDb.HashGetAllAsync(Arg.Any<RedisKey>(), Arg.Any<CommandFlags>())
            .Returns(Array.Empty<HashEntry>());

        // Act
        CaseStatusDetail? result = await service.GetCaseStatusAsync("tenant-1", "nonexistent", CancellationToken.None);

        // Assert
        result.ShouldBeNull();
    }

    [Fact]
    public async Task GetCaseStatusAsync_WhenFound_ShouldReturnStatusWithHealthIndicators()
    {
        // Arrange
        (IConnectionMultiplexer redis, IDatabase redisDb) = CreateMockRedis();
        (IConnectionMultiplexer falkorDb, _) = CreateMockFalkorDb();
        IGraphQueryBuilder builder = CreateMockBuilder();
        ILogger<CaseService> logger = NullLogger<CaseService>.Instance;

        // Create activity service with mocked Redis that returns activity data
        (IConnectionMultiplexer activityRedis, IDatabase activityDb) = CreateMockRedis();
        activityDb.StreamRangeAsync(
            Arg.Any<RedisKey>(),
            Arg.Any<RedisValue?>(),
            Arg.Any<RedisValue?>(),
            Arg.Is<int?>(c => c == 1),
            Arg.Is<Order>(o => o == Order.Descending),
            Arg.Any<CommandFlags>())
            .Returns(_ => new StreamEntry[]
            {
                new("1712345678901-0",
                [
                    new NameValueEntry("type", "memoryUnitIngested"),
                    new NameValueEntry("actor", "system"),
                    new NameValueEntry("description", "Ingested"),
                ]),
            });

        // For GetFailedCountAsync: return mix of events
        activityDb.StreamRangeAsync(
            Arg.Any<RedisKey>(),
            Arg.Any<RedisValue?>(),
            Arg.Any<RedisValue?>(),
            Arg.Is<int?>(c => c == null || c > 1),
            Arg.Any<Order>(),
            Arg.Any<CommandFlags>())
            .Returns(_ => new StreamEntry[]
            {
                new("1-0",
                [
                    new NameValueEntry("type", "ingestionFailed"),
                    new NameValueEntry("actor", "system"),
                    new NameValueEntry("description", "Failed"),
                ]),
                new("2-0",
                [
                    new NameValueEntry("type", "memoryUnitIngested"),
                    new NameValueEntry("actor", "system"),
                    new NameValueEntry("description", "OK"),
                ]),
            });

        CaseActivityService activityService = new(activityRedis, NullLogger<CaseActivityService>.Instance);
        CaseService service = new(redis, falkorDb, builder, activityService, logger);

        HashEntry[] fakeHash =
        [
            new("id", "case-123"),
            new("tenantId", "tenant-1"),
            new("name", "Test Case"),
            new("description", "desc"),
            new("status", "active"),
            new("createdAt", "2026-04-01T10:00:00+00:00"),
            new("lastUpdated", "2026-04-01T11:00:00+00:00"),
        ];
        redisDb.HashGetAllAsync(Arg.Any<RedisKey>(), Arg.Any<CommandFlags>())
            .Returns(fakeHash);

        // Act
        CaseStatusDetail? result = await service.GetCaseStatusAsync("tenant-1", "case-123", CancellationToken.None);

        // Assert
        result.ShouldNotBeNull();
        result.Id.ShouldBe("case-123");
        result.Name.ShouldBe("Test Case");
        result.IndexedCount.ShouldBe(result.MemoryUnitCount);
        result.FailedCount.ShouldBe(1);
        result.LastActivityAt.ShouldNotBeNull();
    }

    // --- Member operation tests ---

    [Fact]
    public async Task AddMemberAsync_WhenNew_ShouldReturnCreatedTrueAndCallHSETNX()
    {
        // Arrange
        (IConnectionMultiplexer redis, IDatabase redisDb) = CreateMockRedis();
        (IConnectionMultiplexer falkorDb, _) = CreateMockFalkorDb();
        IGraphQueryBuilder builder = CreateMockBuilder();
        ILogger<CaseService> logger = NullLogger<CaseService>.Instance;
        CaseService service = new(redis, falkorDb, builder, CreateMockActivityService(), logger);

        redisDb.HashLengthAsync(Arg.Any<RedisKey>(), Arg.Any<CommandFlags>())
            .Returns(0L);
        redisDb.HashSetAsync(Arg.Any<RedisKey>(), Arg.Any<RedisValue>(), Arg.Any<RedisValue>(), Arg.Is<When>(w => w == When.NotExists), Arg.Any<CommandFlags>())
            .Returns(true);

        var input = new AddCaseMemberInput("user-alice", CaseMemberType.User);

        // Act
        (CaseMember member, bool created) = await service.AddMemberAsync("tenant-1", "case-001", input, CancellationToken.None);

        // Assert
        created.ShouldBeTrue();
        member.MemberId.ShouldBe("user-alice");
        member.MemberType.ShouldBe(CaseMemberType.User);

        await redisDb.Received(1).HashSetAsync(
            Arg.Is<RedisKey>(k => k.ToString() == "tenant-1:case:case-001:members"),
            Arg.Is<RedisValue>(f => f.ToString() == "user-alice"),
            Arg.Any<RedisValue>(),
            When.NotExists,
            Arg.Any<CommandFlags>());
    }

    [Fact]
    public async Task AddMemberAsync_WhenAlreadyExists_ShouldReturnCreatedFalseAndNoActivity()
    {
        // Arrange
        (IConnectionMultiplexer redis, IDatabase redisDb) = CreateMockRedis();
        (IConnectionMultiplexer falkorDb, _) = CreateMockFalkorDb();
        IGraphQueryBuilder builder = CreateMockBuilder();
        ILogger<CaseService> logger = NullLogger<CaseService>.Instance;

        (IConnectionMultiplexer activityRedis, IDatabase activityDb) = CreateMockRedis();
        CaseActivityService activityService = new(activityRedis, NullLogger<CaseActivityService>.Instance);
        CaseService service = new(redis, falkorDb, builder, activityService, logger);

        redisDb.HashLengthAsync(Arg.Any<RedisKey>(), Arg.Any<CommandFlags>())
            .Returns(5L);
        // HSETNX returns false -- member already exists
        redisDb.HashSetAsync(Arg.Any<RedisKey>(), Arg.Any<RedisValue>(), Arg.Any<RedisValue>(), Arg.Is<When>(w => w == When.NotExists), Arg.Any<CommandFlags>())
            .Returns(false);

        string existingJson = "{\"memberId\":\"user-alice\",\"memberType\":\"user\",\"addedAt\":\"2026-04-01T10:00:00+00:00\"}";
        redisDb.HashGetAsync(Arg.Any<RedisKey>(), Arg.Is<RedisValue>(v => v.ToString() == "user-alice"), Arg.Any<CommandFlags>())
            .Returns(new RedisValue(existingJson));

        var input = new AddCaseMemberInput("user-alice", CaseMemberType.User);

        // Act
        (CaseMember member, bool created) = await service.AddMemberAsync("tenant-1", "case-001", input, CancellationToken.None);

        // Assert
        created.ShouldBeFalse();
        member.MemberId.ShouldBe("user-alice");

        // No activity event for idempotent add
        await activityDb.DidNotReceive().StreamAddAsync(
            Arg.Any<RedisKey>(),
            Arg.Any<NameValueEntry[]>(),
            Arg.Any<RedisValue?>(),
            Arg.Any<int?>(),
            Arg.Any<bool>(),
            Arg.Any<CommandFlags>());
    }

    [Fact]
    public async Task AddMemberAsync_WhenLimitReached_ShouldThrowInvalidOperationException()
    {
        // Arrange
        (IConnectionMultiplexer redis, IDatabase redisDb) = CreateMockRedis();
        (IConnectionMultiplexer falkorDb, _) = CreateMockFalkorDb();
        IGraphQueryBuilder builder = CreateMockBuilder();
        ILogger<CaseService> logger = NullLogger<CaseService>.Instance;
        CaseService service = new(redis, falkorDb, builder, CreateMockActivityService(), logger);

        redisDb.HashLengthAsync(Arg.Any<RedisKey>(), Arg.Any<CommandFlags>())
            .Returns(1000L);

        var input = new AddCaseMemberInput("user-alice", CaseMemberType.User);

        // Act & Assert
        InvalidOperationException ex = await Should.ThrowAsync<InvalidOperationException>(
            () => service.AddMemberAsync("tenant-1", "case-001", input, CancellationToken.None));

        ex.Message.ShouldContain("maximum");
        ex.Message.ShouldContain("1000");
    }

    [Fact]
    public async Task AddMemberAsync_WhenAtLimitButMemberAlreadyExists_ShouldReturnCreatedFalse()
    {
        // Arrange
        (IConnectionMultiplexer redis, IDatabase redisDb) = CreateMockRedis();
        (IConnectionMultiplexer falkorDb, _) = CreateMockFalkorDb();
        IGraphQueryBuilder builder = CreateMockBuilder();
        ILogger<CaseService> logger = NullLogger<CaseService>.Instance;

        (IConnectionMultiplexer activityRedis, IDatabase activityDb) = CreateMockRedis();
        CaseActivityService activityService = new(activityRedis, NullLogger<CaseActivityService>.Instance);
        CaseService service = new(redis, falkorDb, builder, activityService, logger);

        redisDb.HashLengthAsync(Arg.Any<RedisKey>(), Arg.Any<CommandFlags>())
            .Returns(1000L);
        redisDb.HashGetAsync(Arg.Any<RedisKey>(), Arg.Is<RedisValue>(v => v.ToString() == "user-alice"), Arg.Any<CommandFlags>())
            .Returns(new RedisValue("{\"memberId\":\"user-alice\",\"memberType\":\"user\",\"addedAt\":\"2026-04-01T10:00:00+00:00\"}"));

        var input = new AddCaseMemberInput("user-alice", CaseMemberType.User);

        // Act
        (CaseMember member, bool created) = await service.AddMemberAsync("tenant-1", "case-001", input, CancellationToken.None);

        // Assert
        created.ShouldBeFalse();
        member.MemberId.ShouldBe("user-alice");
        await activityDb.DidNotReceive().StreamAddAsync(
            Arg.Any<RedisKey>(),
            Arg.Any<NameValueEntry[]>(),
            Arg.Any<RedisValue?>(),
            Arg.Any<int?>(),
            Arg.Any<bool>(),
            Arg.Any<CommandFlags>());
    }

    [Fact]
    public async Task AddMemberAsync_WhenHashSetThrows_ShouldNotRecordActivity()
    {
        // Arrange
        (IConnectionMultiplexer redis, IDatabase redisDb) = CreateMockRedis();
        (IConnectionMultiplexer falkorDb, _) = CreateMockFalkorDb();
        IGraphQueryBuilder builder = CreateMockBuilder();
        ILogger<CaseService> logger = NullLogger<CaseService>.Instance;

        (IConnectionMultiplexer activityRedis, IDatabase activityDb) = CreateMockRedis();
        CaseActivityService activityService = new(activityRedis, NullLogger<CaseActivityService>.Instance);
        CaseService service = new(redis, falkorDb, builder, activityService, logger);

        redisDb.HashLengthAsync(Arg.Any<RedisKey>(), Arg.Any<CommandFlags>())
            .Returns(0L);
        redisDb.HashSetAsync(Arg.Any<RedisKey>(), Arg.Any<RedisValue>(), Arg.Any<RedisValue>(), Arg.Is<When>(w => w == When.NotExists), Arg.Any<CommandFlags>())
            .Returns<bool>(_ => throw new RedisConnectionException(ConnectionFailureType.UnableToConnect, "Connection lost"));

        var input = new AddCaseMemberInput("user-alice", CaseMemberType.User);

        // Act & Assert
        _ = await Should.ThrowAsync<RedisConnectionException>(
            () => service.AddMemberAsync("tenant-1", "case-001", input, CancellationToken.None));

        // Activity event must NOT be recorded when the write failed
        await activityDb.DidNotReceive().StreamAddAsync(
            Arg.Any<RedisKey>(),
            Arg.Any<NameValueEntry[]>(),
            Arg.Any<RedisValue?>(),
            Arg.Any<int?>(),
            Arg.Any<bool>(),
            Arg.Any<CommandFlags>());
    }

    [Fact]
    public async Task AddMemberAsync_WhenStoredMemberJsonIsCorrupt_ShouldThrowInvalidDataException()
    {
        // Arrange
        (IConnectionMultiplexer redis, IDatabase redisDb) = CreateMockRedis();
        (IConnectionMultiplexer falkorDb, _) = CreateMockFalkorDb();
        IGraphQueryBuilder builder = CreateMockBuilder();
        ILogger<CaseService> logger = NullLogger<CaseService>.Instance;
        CaseService service = new(redis, falkorDb, builder, CreateMockActivityService(), logger);

        redisDb.HashLengthAsync(Arg.Any<RedisKey>(), Arg.Any<CommandFlags>())
            .Returns(5L);
        redisDb.HashSetAsync(Arg.Any<RedisKey>(), Arg.Any<RedisValue>(), Arg.Any<RedisValue>(), Arg.Is<When>(w => w == When.NotExists), Arg.Any<CommandFlags>())
            .Returns(false);
        redisDb.HashGetAsync(Arg.Any<RedisKey>(), Arg.Any<RedisValue>(), Arg.Any<CommandFlags>())
            .Returns(new RedisValue("{\"memberId\":\"user-alice\"}"));

        var input = new AddCaseMemberInput("user-alice", CaseMemberType.User);

        // Act & Assert
        _ = await Should.ThrowAsync<InvalidDataException>(
            () => service.AddMemberAsync("tenant-1", "case-001", input, CancellationToken.None));
    }

    [Fact]
    public async Task AddMemberAsync_WhenDeleteBetweenCheckRace_ShouldRetryAndReturnCreated()
    {
        // Arrange: HSETNX returns false, but HashGet returns null (member deleted between)
        (IConnectionMultiplexer redis, IDatabase redisDb) = CreateMockRedis();
        (IConnectionMultiplexer falkorDb, _) = CreateMockFalkorDb();
        IGraphQueryBuilder builder = CreateMockBuilder();
        ILogger<CaseService> logger = NullLogger<CaseService>.Instance;
        CaseService service = new(redis, falkorDb, builder, CreateMockActivityService(), logger);

        redisDb.HashLengthAsync(Arg.Any<RedisKey>(), Arg.Any<CommandFlags>())
            .Returns(5L);
        redisDb.HashSetAsync(Arg.Any<RedisKey>(), Arg.Any<RedisValue>(), Arg.Any<RedisValue>(), Arg.Is<When>(w => w == When.NotExists), Arg.Any<CommandFlags>())
            .Returns(false, true);
        // HashGet returns null -- member was deleted between HSETNX and read
        redisDb.HashGetAsync(Arg.Any<RedisKey>(), Arg.Any<RedisValue>(), Arg.Any<CommandFlags>())
            .Returns(RedisValue.Null);

        var input = new AddCaseMemberInput("user-alice", CaseMemberType.User);

        // Act
        (CaseMember member, bool created) = await service.AddMemberAsync("tenant-1", "case-001", input, CancellationToken.None);

        // Assert -- retry path: created=true
        created.ShouldBeTrue();
        member.MemberId.ShouldBe("user-alice");
    }

    [Fact]
    public async Task AddMemberAsync_WhenConcurrentReAddWinsRetry_ShouldReturnExistingMember()
    {
        // Arrange: HSETNX returns false, HashGet returns null, retry loses to another writer, second read finds existing.
        (IConnectionMultiplexer redis, IDatabase redisDb) = CreateMockRedis();
        (IConnectionMultiplexer falkorDb, _) = CreateMockFalkorDb();
        IGraphQueryBuilder builder = CreateMockBuilder();
        ILogger<CaseService> logger = NullLogger<CaseService>.Instance;

        (IConnectionMultiplexer activityRedis, IDatabase activityDb) = CreateMockRedis();
        CaseActivityService activityService = new(activityRedis, NullLogger<CaseActivityService>.Instance);
        CaseService service = new(redis, falkorDb, builder, activityService, logger);

        redisDb.HashLengthAsync(Arg.Any<RedisKey>(), Arg.Any<CommandFlags>())
            .Returns(5L);
        redisDb.HashSetAsync(Arg.Any<RedisKey>(), Arg.Any<RedisValue>(), Arg.Any<RedisValue>(), Arg.Is<When>(w => w == When.NotExists), Arg.Any<CommandFlags>())
            .Returns(false, false);
        redisDb.HashGetAsync(Arg.Any<RedisKey>(), Arg.Any<RedisValue>(), Arg.Any<CommandFlags>())
            .Returns(
                RedisValue.Null,
                new RedisValue("{\"memberId\":\"user-alice\",\"memberType\":\"user\",\"addedAt\":\"2026-04-01T10:00:00+00:00\"}"));

        var input = new AddCaseMemberInput("user-alice", CaseMemberType.User);

        // Act
        (CaseMember member, bool created) = await service.AddMemberAsync("tenant-1", "case-001", input, CancellationToken.None);

        // Assert
        created.ShouldBeFalse();
        member.MemberId.ShouldBe("user-alice");
        await activityDb.DidNotReceive().StreamAddAsync(
            Arg.Any<RedisKey>(),
            Arg.Any<NameValueEntry[]>(),
            Arg.Any<RedisValue?>(),
            Arg.Any<int?>(),
            Arg.Any<bool>(),
            Arg.Any<CommandFlags>());
    }

    [Fact]
    public async Task RemoveMemberAsync_WhenFound_ShouldReturnTrueAndCallHashDelete()
    {
        // Arrange
        (IConnectionMultiplexer redis, IDatabase redisDb) = CreateMockRedis();
        (IConnectionMultiplexer falkorDb, _) = CreateMockFalkorDb();
        IGraphQueryBuilder builder = CreateMockBuilder();
        ILogger<CaseService> logger = NullLogger<CaseService>.Instance;
        CaseService service = new(redis, falkorDb, builder, CreateMockActivityService(), logger);

        redisDb.HashDeleteAsync(Arg.Any<RedisKey>(), Arg.Any<RedisValue>(), Arg.Any<CommandFlags>())
            .Returns(true);

        // Act
        bool result = await service.RemoveMemberAsync("tenant-1", "case-001", "user-alice", CancellationToken.None);

        // Assert
        result.ShouldBeTrue();
        await redisDb.Received(1).HashDeleteAsync(
            Arg.Is<RedisKey>(k => k.ToString() == "tenant-1:case:case-001:members"),
            Arg.Is<RedisValue>(v => v.ToString() == "user-alice"),
            Arg.Any<CommandFlags>());
    }

    [Fact]
    public async Task RemoveMemberAsync_WhenNotFound_ShouldReturnFalseAndNoActivity()
    {
        // Arrange
        (IConnectionMultiplexer redis, IDatabase redisDb) = CreateMockRedis();
        (IConnectionMultiplexer falkorDb, _) = CreateMockFalkorDb();
        IGraphQueryBuilder builder = CreateMockBuilder();
        ILogger<CaseService> logger = NullLogger<CaseService>.Instance;

        (IConnectionMultiplexer activityRedis, IDatabase activityDb) = CreateMockRedis();
        CaseActivityService activityService = new(activityRedis, NullLogger<CaseActivityService>.Instance);
        CaseService service = new(redis, falkorDb, builder, activityService, logger);

        redisDb.HashDeleteAsync(Arg.Any<RedisKey>(), Arg.Any<RedisValue>(), Arg.Any<CommandFlags>())
            .Returns(false);

        // Act
        bool result = await service.RemoveMemberAsync("tenant-1", "case-001", "user-alice", CancellationToken.None);

        // Assert
        result.ShouldBeFalse();
        await activityDb.DidNotReceive().StreamAddAsync(
            Arg.Any<RedisKey>(),
            Arg.Any<NameValueEntry[]>(),
            Arg.Any<RedisValue?>(),
            Arg.Any<int?>(),
            Arg.Any<bool>(),
            Arg.Any<CommandFlags>());
    }

    [Fact]
    public async Task ListMembersAsync_WhenEmpty_ShouldReturnEmptyList()
    {
        // Arrange
        (IConnectionMultiplexer redis, IDatabase redisDb) = CreateMockRedis();
        (IConnectionMultiplexer falkorDb, _) = CreateMockFalkorDb();
        IGraphQueryBuilder builder = CreateMockBuilder();
        ILogger<CaseService> logger = NullLogger<CaseService>.Instance;
        CaseService service = new(redis, falkorDb, builder, CreateMockActivityService(), logger);

        redisDb.HashGetAllAsync(Arg.Is<RedisKey>(k => k.ToString().EndsWith(":members")), Arg.Any<CommandFlags>())
            .Returns(Array.Empty<HashEntry>());

        // Act
        List<CaseMember> result = await service.ListMembersAsync("tenant-1", "case-001", CancellationToken.None);

        // Assert
        result.ShouldBeEmpty();
    }

    [Fact]
    public async Task ListMembersAsync_WhenPopulated_ShouldReturnOrderedByAddedAt()
    {
        // Arrange
        (IConnectionMultiplexer redis, IDatabase redisDb) = CreateMockRedis();
        (IConnectionMultiplexer falkorDb, _) = CreateMockFalkorDb();
        IGraphQueryBuilder builder = CreateMockBuilder();
        ILogger<CaseService> logger = NullLogger<CaseService>.Instance;
        CaseService service = new(redis, falkorDb, builder, CreateMockActivityService(), logger);

        HashEntry[] entries =
        [
            new("user-bob", "{\"memberId\":\"user-bob\",\"memberType\":\"user\",\"addedAt\":\"2026-04-02T10:00:00+00:00\"}"),
            new("user-alice", "{\"memberId\":\"user-alice\",\"memberType\":\"user\",\"addedAt\":\"2026-04-01T10:00:00+00:00\"}"),
        ];
        redisDb.HashGetAllAsync(Arg.Is<RedisKey>(k => k.ToString().EndsWith(":members")), Arg.Any<CommandFlags>())
            .Returns(entries);

        // Act
        List<CaseMember> result = await service.ListMembersAsync("tenant-1", "case-001", CancellationToken.None);

        // Assert
        result.Count.ShouldBe(2);
        result[0].MemberId.ShouldBe("user-alice"); // Earlier AddedAt comes first
        result[1].MemberId.ShouldBe("user-bob");
    }

    [Fact]
    public async Task ListMembersAsync_WhenEntryIsCorrupt_ShouldSkipIt()
    {
        // Arrange
        (IConnectionMultiplexer redis, IDatabase redisDb) = CreateMockRedis();
        (IConnectionMultiplexer falkorDb, _) = CreateMockFalkorDb();
        IGraphQueryBuilder builder = CreateMockBuilder();
        ILogger<CaseService> logger = NullLogger<CaseService>.Instance;
        CaseService service = new(redis, falkorDb, builder, CreateMockActivityService(), logger);

        HashEntry[] entries =
        [
            new("user-bad", "{\"memberId\":\"user-bad\"}"),
            new("user-alice", "{\"memberId\":\"user-alice\",\"memberType\":\"user\",\"addedAt\":\"2026-04-01T10:00:00+00:00\"}"),
        ];
        redisDb.HashGetAllAsync(Arg.Is<RedisKey>(k => k.ToString().EndsWith(":members")), Arg.Any<CommandFlags>())
            .Returns(entries);

        // Act
        List<CaseMember> result = await service.ListMembersAsync("tenant-1", "case-001", CancellationToken.None);

        // Assert
        result.Count.ShouldBe(1);
        result[0].MemberId.ShouldBe("user-alice");
    }

    [Fact]
    public async Task GetMemberCountAsync_ShouldReturnHashLength()
    {
        // Arrange
        (IConnectionMultiplexer redis, IDatabase redisDb) = CreateMockRedis();
        (IConnectionMultiplexer falkorDb, _) = CreateMockFalkorDb();
        IGraphQueryBuilder builder = CreateMockBuilder();
        ILogger<CaseService> logger = NullLogger<CaseService>.Instance;
        CaseService service = new(redis, falkorDb, builder, CreateMockActivityService(), logger);

        redisDb.HashLengthAsync(Arg.Is<RedisKey>(k => k.ToString() == "tenant-1:case:case-001:members"), Arg.Any<CommandFlags>())
            .Returns(42L);

        // Act
        int count = await service.GetMemberCountAsync("tenant-1", "case-001", CancellationToken.None);

        // Assert
        count.ShouldBe(42);
    }

    [Fact]
    public async Task ResolveNamesAsync_WithMultipleCaseIds_ShouldReturnAllNames()
    {
        // Arrange
        (IConnectionMultiplexer redis, IDatabase redisDb) = CreateMockRedis();
        (IConnectionMultiplexer falkorDb, _) = CreateMockFalkorDb();
        IGraphQueryBuilder builder = CreateMockBuilder();
        ILogger<CaseService> logger = NullLogger<CaseService>.Instance;
        CaseService service = new(redis, falkorDb, builder, CreateMockActivityService(), logger);

        IBatch batch = Substitute.For<IBatch>();
        redisDb.CreateBatch(Arg.Any<object>()).Returns(batch);
        batch.HashGetAsync(Arg.Is<RedisKey>(k => k.ToString() == "tenant-1:case:case-1"), Arg.Is<RedisValue>(v => v.ToString() == "name"), Arg.Any<CommandFlags>())
            .Returns(Task.FromResult(new RedisValue("Alpha")));
        batch.HashGetAsync(Arg.Is<RedisKey>(k => k.ToString() == "tenant-1:case:case-2"), Arg.Is<RedisValue>(v => v.ToString() == "name"), Arg.Any<CommandFlags>())
            .Returns(Task.FromResult(new RedisValue("Beta")));

        // Act
        Dictionary<string, string> result = await service.ResolveNamesAsync("tenant-1", ["case-1", "case-2"], CancellationToken.None);

        // Assert
        result.Count.ShouldBe(2);
        result["case-1"].ShouldBe("Alpha");
        result["case-2"].ShouldBe("Beta");
    }

    [Fact]
    public async Task ResolveNamesAsync_WithUnknownCaseId_ShouldFallBackToCaseId()
    {
        // Arrange
        (IConnectionMultiplexer redis, IDatabase redisDb) = CreateMockRedis();
        (IConnectionMultiplexer falkorDb, _) = CreateMockFalkorDb();
        IGraphQueryBuilder builder = CreateMockBuilder();
        ILogger<CaseService> logger = NullLogger<CaseService>.Instance;
        CaseService service = new(redis, falkorDb, builder, CreateMockActivityService(), logger);

        IBatch batch = Substitute.For<IBatch>();
        redisDb.CreateBatch(Arg.Any<object>()).Returns(batch);
        batch.HashGetAsync(Arg.Any<RedisKey>(), Arg.Is<RedisValue>(v => v.ToString() == "name"), Arg.Any<CommandFlags>())
            .Returns(Task.FromResult(RedisValue.Null));

        // Act
        Dictionary<string, string> result = await service.ResolveNamesAsync("tenant-1", ["unknown-case"], CancellationToken.None);

        // Assert
        result.Count.ShouldBe(1);
        result["unknown-case"].ShouldBe("unknown-case");
    }

    [Fact]
    public async Task ResolveNamesAsync_WithEmptyInput_ShouldReturnEmptyDictionary()
    {
        // Arrange
        (IConnectionMultiplexer redis, _) = CreateMockRedis();
        (IConnectionMultiplexer falkorDb, _) = CreateMockFalkorDb();
        IGraphQueryBuilder builder = CreateMockBuilder();
        ILogger<CaseService> logger = NullLogger<CaseService>.Instance;
        CaseService service = new(redis, falkorDb, builder, CreateMockActivityService(), logger);

        // Act
        Dictionary<string, string> result = await service.ResolveNamesAsync("tenant-1", [], CancellationToken.None);

        // Assert
        result.ShouldBeEmpty();
    }

    [Fact]
    public async Task ResolveNamesAsync_WithDuplicateCaseIds_ShouldDeduplicateAndResolve()
    {
        // Arrange
        (IConnectionMultiplexer redis, IDatabase redisDb) = CreateMockRedis();
        (IConnectionMultiplexer falkorDb, _) = CreateMockFalkorDb();
        IGraphQueryBuilder builder = CreateMockBuilder();
        ILogger<CaseService> logger = NullLogger<CaseService>.Instance;
        CaseService service = new(redis, falkorDb, builder, CreateMockActivityService(), logger);

        IBatch batch = Substitute.For<IBatch>();
        redisDb.CreateBatch(Arg.Any<object>()).Returns(batch);
        batch.HashGetAsync(Arg.Is<RedisKey>(k => k.ToString() == "tenant-1:case:case-1"), Arg.Is<RedisValue>(v => v.ToString() == "name"), Arg.Any<CommandFlags>())
            .Returns(Task.FromResult(new RedisValue("Alpha")));

        // Act
        Dictionary<string, string> result = await service.ResolveNamesAsync("tenant-1", ["case-1", "case-1", "case-1"], CancellationToken.None);

        // Assert
        result.Count.ShouldBe(1);
        result["case-1"].ShouldBe("Alpha");
    }

    private static CaseActivityService CreateMockActivityService()
    {
        (IConnectionMultiplexer activityRedis, _) = CreateMockRedis();
        return new CaseActivityService(activityRedis, NullLogger<CaseActivityService>.Instance);
    }

    private static (IConnectionMultiplexer, IDatabase) CreateMockFalkorDb()
    {
        IDatabase db = Substitute.For<IDatabase>();
        IConnectionMultiplexer falkorDb = Substitute.For<IConnectionMultiplexer>();
        falkorDb.GetDatabase(Arg.Any<int>(), Arg.Any<object>()).Returns(db);

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

    private static HashEntry[] CreateCaseHash(
        string id,
        string createdAt,
        string lastUpdated,
        string name,
        string tenantId = "tenant-1",
        string? description = "desc",
        string status = "active") =>
        [
            new("id", id),
            new("tenantId", tenantId),
            new("name", name),
            new("description", description ?? string.Empty),
            new("status", status),
            new("createdAt", createdAt),
            new("lastUpdated", lastUpdated),
        ];
}
