// <copyright file="TenantContextEnforcementTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Tests.Tenants;

using System.Net;

using Dapr.Client;
using Dapr.Workflow;

using Hexalith.Memories.Contracts.V1;
using Hexalith.Memories.Server.Cases;
using Hexalith.Memories.Server.Graph;
using Hexalith.Memories.Server.Tenants;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

using NSubstitute;

using Shouldly;

using StackExchange.Redis;

using CaseRecord = Hexalith.Memories.Contracts.V1.Case;

/// <summary>Unit tests for Story 5.4 — tenant context enforcement (AC1, AC2).</summary>
public sealed class TenantContextEnforcementTests
{
    // ---------------------------------------------------------------------------------------------
    // AC1: TenantStatusGuard.ValidateTenantExistsAsync (existence-only check)
    // ---------------------------------------------------------------------------------------------
    [Fact]
    public async Task ValidateTenantExistsAsync_UnknownTenant_ReturnsTenantNotFound()
    {
        TenantStatusGuard guard = new(CreateRegistryReturning(null));

        ErrorResponse? error = await guard.ValidateTenantExistsAsync("missing", CancellationToken.None);

        error.ShouldNotBeNull();
        error.Code.ShouldBe("TENANT_NOT_FOUND");
    }

    [Theory]
    [InlineData(TenantStatus.Active)]
    [InlineData(TenantStatus.Provisioning)]
    [InlineData(TenantStatus.Deleting)]
    [InlineData(TenantStatus.Failed)]
    [InlineData(TenantStatus.CompensationFailed)]
    public async Task ValidateTenantExistsAsync_AnyExistingStatus_ReturnsNull(TenantStatus status)
    {
        var tenant = new TenantInfo("tenant-a", "Tenant A", status, DateTimeOffset.UtcNow);
        TenantStatusGuard guard = new(CreateRegistryReturning(tenant));

        ErrorResponse? error = await guard.ValidateTenantExistsAsync("tenant-a", CancellationToken.None);

        error.ShouldBeNull();
    }

    // ---------------------------------------------------------------------------------------------
    // AC1: TenantStatusGuard.ToHttpResult maps TENANT_NOT_FOUND to 404 and other status errors to 409
    // ---------------------------------------------------------------------------------------------
    [Fact]
    public void ToHttpResult_TenantNotFound_Returns404()
    {
        IResult result = TenantStatusGuard.ToHttpResult(
            new ErrorResponse("TENANT_NOT_FOUND", "missing", "list"));

        NotFound<ErrorResponse> notFound = result.ShouldBeOfType<NotFound<ErrorResponse>>();
        notFound.StatusCode.ShouldBe((int)HttpStatusCode.NotFound);
    }

    [Theory]
    [InlineData("TENANT_DELETING")]
    [InlineData("TENANT_PROVISIONING")]
    [InlineData("TENANT_FAILED")]
    [InlineData("TENANT_UNAVAILABLE")]
    public void ToHttpResult_NonActiveStatusErrors_Return409(string code)
    {
        IResult result = TenantStatusGuard.ToHttpResult(new ErrorResponse(code, "nope", "fix"));

        Conflict<ErrorResponse> conflict = result.ShouldBeOfType<Conflict<ErrorResponse>>();
        conflict.StatusCode.ShouldBe((int)HttpStatusCode.Conflict);
    }

    // ---------------------------------------------------------------------------------------------
    // AC2: CaseService.GetMemoryUnitAsync detects tenant mismatch, logs Critical, and returns null
    // ---------------------------------------------------------------------------------------------
    [Fact]
    public async Task GetMemoryUnitAsync_TenantMismatch_ReturnsNullAndLogsCritical()
    {
        TenantMismatchMonitor.ResetForTests();
        long baseline = TenantMismatchMonitor.MismatchCount;

        HashEntry[] muHash =
        [
            new("id", "mu-xyz"),
            new("tenantId", "tenant-b"),
            new("caseId", "case-1"),
            new("content", "payload"),
        ];

        (IConnectionMultiplexer redis, IDatabase redisDb) = CreateMockRedis();
        redisDb.HashGetAllAsync(Arg.Any<RedisKey>(), Arg.Any<CommandFlags>()).Returns(muHash);

        (IConnectionMultiplexer falkor, _) = CreateMockFalkorDb();
        CapturingLogger<CaseService> logger = new();
        CaseService service = new(
            redis,
            falkor,
            Substitute.For<IGraphQueryBuilder>(),
            CreateActivityService(),
            CreateWorkflowClient(),
            CreateMockActorProxyFactory(),
            logger);

        MemoryUnit? result = await service.GetMemoryUnitAsync("tenant-a", "mu-xyz", CancellationToken.None);

        result.ShouldBeNull();
        logger.Entries.ShouldContain(e => e.Level == LogLevel.Critical);
        (TenantMismatchMonitor.MismatchCount - baseline).ShouldBe(1);
    }

    [Fact]
    public async Task GetMemoryUnitAsync_TenantMatch_ReturnsMemoryUnit()
    {
        TenantMismatchMonitor.ResetForTests();

        HashEntry[] muHash =
        [
            new("id", "mu-xyz"),
            new("tenantId", "tenant-a"),
            new("caseId", "case-1"),
            new("content", "payload"),
        ];

        (IConnectionMultiplexer redis, IDatabase redisDb) = CreateMockRedis();
        redisDb.HashGetAllAsync(Arg.Any<RedisKey>(), Arg.Any<CommandFlags>()).Returns(muHash);
        (IConnectionMultiplexer falkor, _) = CreateMockFalkorDb();

        CaseService service = new(
            redis,
            falkor,
            Substitute.For<IGraphQueryBuilder>(),
            CreateActivityService(),
            CreateWorkflowClient(),
            CreateMockActorProxyFactory(),
            NullLogger<CaseService>.Instance);

        MemoryUnit? result = await service.GetMemoryUnitAsync("tenant-a", "mu-xyz", CancellationToken.None);

        result.ShouldNotBeNull();
        result!.Id.ShouldBe("mu-xyz");
        TenantMismatchMonitor.MismatchCount.ShouldBe(0);
    }

    [Fact]
    public async Task GetMemoryUnitAsync_LegacyHashWithoutTenantIdField_ReturnsRecord()
    {
        // Hashes written before Story 5.4 do not carry a tenantId field. Backward compatibility:
        // mismatch detection must treat this as a match (primary defense is the key prefix), not a
        // false-positive corruption signal.
        TenantMismatchMonitor.ResetForTests();

        HashEntry[] muHash =
        [
            new("id", "mu-legacy"),
            new("caseId", "case-1"),
            new("content", "payload"),
        ];

        (IConnectionMultiplexer redis, IDatabase redisDb) = CreateMockRedis();
        redisDb.HashGetAllAsync(Arg.Any<RedisKey>(), Arg.Any<CommandFlags>()).Returns(muHash);
        (IConnectionMultiplexer falkor, _) = CreateMockFalkorDb();

        CaseService service = new(
            redis,
            falkor,
            Substitute.For<IGraphQueryBuilder>(),
            CreateActivityService(),
            CreateWorkflowClient(),
            CreateMockActorProxyFactory(),
            NullLogger<CaseService>.Instance);

        MemoryUnit? result = await service.GetMemoryUnitAsync("tenant-a", "mu-legacy", CancellationToken.None);

        result.ShouldNotBeNull();
        TenantMismatchMonitor.MismatchCount.ShouldBe(0);
    }

    // ---------------------------------------------------------------------------------------------
    // AC2: CaseService.GetCaseAsync detects tenant mismatch
    // ---------------------------------------------------------------------------------------------
    [Fact]
    public async Task GetCaseAsync_TenantMismatch_ReturnsNullAndLogsCritical()
    {
        TenantMismatchMonitor.ResetForTests();
        long baseline = TenantMismatchMonitor.MismatchCount;

        HashEntry[] caseHash =
        [
            new("id", "case-1"),
            new("tenantId", "tenant-b"),
            new("name", "leaked case"),
            new("status", "active"),
            new("createdAt", "2026-04-01T00:00:00+00:00"),
            new("lastUpdated", "2026-04-01T00:00:00+00:00"),
        ];

        (IConnectionMultiplexer redis, IDatabase redisDb) = CreateMockRedis();
        redisDb.HashGetAllAsync(Arg.Any<RedisKey>(), Arg.Any<CommandFlags>()).Returns(caseHash);
        (IConnectionMultiplexer falkor, _) = CreateMockFalkorDb();

        CapturingLogger<CaseService> logger = new();
        CaseService service = new(
            redis,
            falkor,
            Substitute.For<IGraphQueryBuilder>(),
            CreateActivityService(),
            CreateWorkflowClient(),
            CreateMockActorProxyFactory(),
            logger);

        CaseRecord? result = await service.GetCaseAsync("tenant-a", "case-1", CancellationToken.None);

        result.ShouldBeNull();
        logger.Entries.ShouldContain(e => e.Level == LogLevel.Critical);
        (TenantMismatchMonitor.MismatchCount - baseline).ShouldBe(1);
    }

    [Fact]
    public async Task ListCasesAsync_TenantMismatch_SkipsCaseAndLogsCritical()
    {
        TenantMismatchMonitor.ResetForTests();
        long baseline = TenantMismatchMonitor.MismatchCount;

        HashEntry[] caseHash =
        [
            new("id", "case-1"),
            new("tenantId", "tenant-b"),
            new("name", "leaked case"),
            new("status", "active"),
            new("createdAt", "2026-04-01T00:00:00+00:00"),
            new("lastUpdated", "2026-04-01T00:00:00+00:00"),
        ];

        (IConnectionMultiplexer redis, IDatabase redisDb) = CreateMockRedis();
        redisDb.HashGetAllAsync(Arg.Any<RedisKey>(), Arg.Any<CommandFlags>()).Returns(caseHash);

        EndPoint endpoint = new IPEndPoint(IPAddress.Loopback, 6379);
        IServer server = Substitute.For<IServer>();
        redis.GetEndPoints(Arg.Any<bool>()).Returns([endpoint]);
        redis.GetServer(Arg.Any<EndPoint>(), Arg.Any<object>()).Returns(server);
        server.Keys(
                Arg.Any<int>(),
                Arg.Any<RedisValue>(),
                Arg.Any<int>(),
                Arg.Any<long>(),
                Arg.Any<int>(),
                Arg.Any<CommandFlags>())
            .Returns([(RedisKey)"tenant-a:case:case-1"]);

        (IConnectionMultiplexer falkor, _) = CreateMockFalkorDb();
        CapturingLogger<CaseService> logger = new();
        CaseService service = new(
            redis,
            falkor,
            Substitute.For<IGraphQueryBuilder>(),
            CreateActivityService(),
            CreateWorkflowClient(),
            CreateMockActorProxyFactory(),
            logger);

        List<CaseRecord> result = await service.ListCasesAsync("tenant-a", 10, CancellationToken.None);

        result.ShouldBeEmpty();
        logger.Entries.ShouldContain(e => e.Level == LogLevel.Critical);
        (TenantMismatchMonitor.MismatchCount - baseline).ShouldBe(1);
    }

    [Fact]
    public async Task GetCaseStatusAsync_TenantMismatch_ReturnsNullAndLogsCritical()
    {
        TenantMismatchMonitor.ResetForTests();
        long baseline = TenantMismatchMonitor.MismatchCount;

        HashEntry[] caseHash =
        [
            new("id", "case-1"),
            new("tenantId", "tenant-b"),
            new("name", "leaked case"),
            new("status", "active"),
            new("createdAt", "2026-04-01T00:00:00+00:00"),
            new("lastUpdated", "2026-04-01T00:00:00+00:00"),
        ];

        (IConnectionMultiplexer redis, IDatabase redisDb) = CreateMockRedis();
        redisDb.HashGetAllAsync(Arg.Any<RedisKey>(), Arg.Any<CommandFlags>()).Returns(caseHash);
        (IConnectionMultiplexer falkor, _) = CreateMockFalkorDb();

        CapturingLogger<CaseService> logger = new();
        CaseService service = new(
            redis,
            falkor,
            Substitute.For<IGraphQueryBuilder>(),
            CreateActivityService(),
            CreateWorkflowClient(),
            CreateMockActorProxyFactory(),
            logger);

        CaseStatusDetail? result = await service.GetCaseStatusAsync("tenant-a", "case-1", CancellationToken.None);

        result.ShouldBeNull();
        logger.Entries.ShouldContain(e => e.Level == LogLevel.Critical);
        (TenantMismatchMonitor.MismatchCount - baseline).ShouldBe(1);
    }

    /// <summary>Minimal logger that records level + formatted message — avoids NSubstitute proxying
    /// <see cref="ILogger{TCategoryName}"/> for categories whose generic arg is internal.</summary>
    private sealed class CapturingLogger<TCategory> : ILogger<TCategory>
    {
        public List<(LogLevel Level, string Message)> Entries { get; } = [];

        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
            => Entries.Add((logLevel, formatter(state, exception)));
    }

    // ---------------------------------------------------------------------------------------------
    // Helpers — mirrors CaseServiceTests patterns for local isolation.
    // ---------------------------------------------------------------------------------------------
    private static TenantRegistryService CreateRegistryReturning(TenantInfo? tenant)
    {
        DaprClient daprClient = Substitute.For<DaprClient>();
        ILogger<TenantRegistryService> logger = Substitute.For<ILogger<TenantRegistryService>>();

        string tenantId = tenant?.Id ?? "missing";
        TenantRegistryEntry? entry = tenant is not null ? new TenantRegistryEntry(tenant, null) : null;

        daprClient.GetStateAsync<TenantRegistryEntry?>(
                "statestore",
                $"tenant-registry-{tenantId}",
                cancellationToken: Arg.Any<CancellationToken>())
            .Returns(entry);

        return new TenantRegistryService(daprClient, logger);
    }

    private static (IConnectionMultiplexer, IDatabase) CreateMockRedis()
    {
        IDatabase db = Substitute.For<IDatabase>();
        IConnectionMultiplexer redis = Substitute.For<IConnectionMultiplexer>();
        redis.GetDatabase(Arg.Any<int>(), Arg.Any<object>()).Returns(db);
        db.Multiplexer.Returns(redis);
        return (redis, db);
    }

    private static (IConnectionMultiplexer, IDatabase) CreateMockFalkorDb()
    {
        IDatabase db = Substitute.For<IDatabase>();
        IConnectionMultiplexer falkor = Substitute.For<IConnectionMultiplexer>();
        falkor.GetDatabase(Arg.Any<int>(), Arg.Any<object>()).Returns(db);

        RedisResult emptyResult = RedisResult.Create(
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

        db.ExecuteAsync(Arg.Any<string>(), Arg.Any<object[]>()).Returns(emptyResult);
        db.ExecuteAsync(Arg.Any<string>(), Arg.Any<ICollection<object>>(), Arg.Any<CommandFlags>())
            .Returns(emptyResult);
        return (falkor, db);
    }

    private static CaseActivityService CreateActivityService()
    {
        (IConnectionMultiplexer redis, _) = CreateMockRedis();
        return new CaseActivityService(redis, NullLogger<CaseActivityService>.Instance);
    }

    private static DaprWorkflowClient CreateWorkflowClient() => null!;

    private static Dapr.Actors.Client.IActorProxyFactory CreateMockActorProxyFactory()
    {
        Hexalith.Memories.Server.Actors.ICaseIngestionCounterActor proxy =
            Substitute.For<Hexalith.Memories.Server.Actors.ICaseIngestionCounterActor>();
        proxy.GetCountsAsync().Returns(new Hexalith.Memories.Contracts.V1.CaseIngestionCounts(0, 0, 0, 0));
        Dapr.Actors.Client.IActorProxyFactory factory = Substitute.For<Dapr.Actors.Client.IActorProxyFactory>();
        factory.CreateActorProxy(
            Arg.Any<Dapr.Actors.ActorId>(),
            Arg.Any<Type>(),
            Arg.Any<string>(),
            Arg.Any<Dapr.Actors.Client.ActorProxyOptions?>()).Returns(proxy);
        return factory;
    }
}
