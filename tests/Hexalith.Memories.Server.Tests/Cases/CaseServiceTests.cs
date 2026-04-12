namespace Hexalith.Memories.Server.Tests.Cases;

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
