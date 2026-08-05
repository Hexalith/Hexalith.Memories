namespace Hexalith.Memories.Server.Tests.Cases;

using System.Diagnostics;
using System.IO;
using System.Net;
using System.Text.RegularExpressions;

using Dapr.Workflow;

using Hexalith.Memories.Contracts.V1;
using Hexalith.Memories.EventStore.Domain.Commands;
using Hexalith.Memories.Server.Activities.Cases;
using Hexalith.Memories.Server.Cases;
using Hexalith.Memories.Server.EventStoreIntegration;
using Hexalith.Memories.Server.Graph;
using Hexalith.Memories.Server.Infrastructure;
using Hexalith.Memories.Server.Ingestion;
using Hexalith.Memories.Server.NaturalLanguage;
using Hexalith.Memories.Server.Workflows;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

using NSubstitute;

using Shouldly;

using StackExchange.Redis;

using CaseRecord = Hexalith.Memories.Contracts.V1.Case;

public class CaseServiceTests
{
    [Fact]
    public async Task CreateCaseAsync_ShouldAcceptCommandBeforeSchedulingProjectionWorkflow()
    {
        // Arrange
        (IConnectionMultiplexer redis, IDatabase redisDb) = CreateMockRedis();
        (IConnectionMultiplexer falkorDb, _) = CreateMockFalkorDb();
        IGraphQueryBuilder builder = CreateMockBuilder();
        ILogger<CaseService> logger = NullLogger<CaseService>.Instance;
        List<string> operationLog = [];
        CapturingCommandStore commandStore = new(operationLog);
        CapturingProjectionWorkflowScheduler workflowScheduler = new(operationLog);
        CaseService service = new(
            redis,
            falkorDb,
            builder,
            CreateMockActivityService(),
            CreateMockWorkflowClient(),
            CreateMockActorProxyFactory(),
            logger,
            commandStore,
            workflowScheduler);

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

        commandStore.AcceptedCommands.Count.ShouldBe(1);
        CreateCaseCommand command = commandStore.AcceptedCommands[0].ShouldBeOfType<CreateCaseCommand>();
        command.TenantId.ShouldBe("tenant-1");
        command.CaseId.ShouldBe(result.Id);
        command.Name.ShouldBe("Test Case");
        command.Description.ShouldBe("A description");

        workflowScheduler.ScheduledWorkflows.Count.ShouldBe(1);
        ScheduledProjectionWorkflow workflow = workflowScheduler.ScheduledWorkflows[0];
        workflow.WorkflowName.ShouldBe(nameof(CaseCreationProjectionWorkflow));
        workflow.InstanceId.ShouldBe($"case-create-{result.Id}");
        workflow.Input.ShouldNotBeNull();

        // Story 21.2 AC1: the EventStore command accept is the authoritative write and must
        // complete before projection fan-out starts.
        operationLog.ShouldBe(
        [
            $"accept:{CreateCaseCommand.CommandType}",
            $"schedule:{nameof(CaseCreationProjectionWorkflow)}",
        ]);

        await redisDb.DidNotReceive().HashSetAsync(
            Arg.Any<RedisKey>(),
            Arg.Any<HashEntry[]>(),
            Arg.Any<CommandFlags>());

        builder.DidNotReceive().BuildMergeCaseNode(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<DateTimeOffset>());
    }

    [Fact]
    public async Task CreateCaseAsync_WhenCommandAcceptFails_ShouldNotScheduleProjectionOrWriteReadModels()
    {
        // Arrange
        (IConnectionMultiplexer redis, IDatabase redisDb) = CreateMockRedis();
        (IConnectionMultiplexer falkorDb, _) = CreateMockFalkorDb();
        IGraphQueryBuilder builder = CreateMockBuilder();
        CapturingProjectionWorkflowScheduler workflowScheduler = new();
        CaseService service = new(
            redis,
            falkorDb,
            builder,
            CreateMockActivityService(),
            CreateMockWorkflowClient(),
            CreateMockActorProxyFactory(),
            NullLogger<CaseService>.Instance,
            new FailingCommandStore(),
            workflowScheduler);

        var input = new CreateCaseInput("tenant-1", "Test Case", "A description");

        // Act / Assert
        InvalidOperationException exception = await Should.ThrowAsync<InvalidOperationException>(
            () => service.CreateCaseAsync(input, CancellationToken.None));

        exception.Message.ShouldBe("EVENTSTORE_UNAVAILABLE");
        workflowScheduler.ScheduledWorkflows.ShouldBeEmpty();
        await redisDb.DidNotReceive().HashSetAsync(
            Arg.Any<RedisKey>(),
            Arg.Any<HashEntry[]>(),
            Arg.Any<CommandFlags>());
        builder.DidNotReceive().BuildMergeCaseNode(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<DateTimeOffset>());
    }

    [Fact]
    public async Task CreateAnnotationAsync_ShouldAcceptCommandBeforeSchedulingProjectionWorkflow()
    {
        // Arrange
        (IConnectionMultiplexer redis, IDatabase redisDb) = CreateMockRedis();
        (IConnectionMultiplexer falkorDb, _) = CreateMockFalkorDbWithMuIds("mu-001");
        IGraphQueryBuilder builder = CreateMockBuilder();
        builder.BuildCheckMemoryUnitExists(Arg.Any<string>())
            .Returns(("MATCH (m:MemoryUnit {id: $id}) RETURN m.id", new Dictionary<string, object> { ["id"] = "mu-001" }));
        List<string> operationLog = [];
        CapturingCommandStore commandStore = new(operationLog);
        CapturingProjectionWorkflowScheduler workflowScheduler = new(operationLog);
        IngestionWorkflowConfigurationCapture workflowConfigurationCapture = new(
            Options.Create(new IngestionSettings
            {
                RetryPolicies = new Dictionary<string, ActivityRetryPolicy>(StringComparer.Ordinal)
                {
                    ["ExtractContentActivity"] = new ActivityRetryPolicy
                    {
                        MaxAttempts = 9,
                        FirstRetryIntervalSeconds = 13,
                        BackoffCoefficient = 1.75,
                        MaxRetryIntervalSeconds = 97,
                    },
                },
            }),
            Options.Create(new NaturalLanguageDescriptionOptions { PersistInMetadata = true }));
        using Activity activity = new("annotation-parent-capture");
        activity.SetIdFormat(ActivityIdFormat.W3C);
        activity.SetParentId("00-33333333333333333333333333333333-4444444444444444-01");
        activity.TraceStateString = "vendor=case-service-proof";
        activity.Start();
        CaseService service = new(
            redis,
            falkorDb,
            builder,
            CreateMockActivityService(),
            CreateMockWorkflowClient(),
            CreateMockActorProxyFactory(),
            NullLogger<CaseService>.Instance,
            commandStore,
            workflowScheduler,
            workflowConfigurationCapture,
            new WorkflowTraceContextCapture());

        redisDb.HashGetAllAsync(Arg.Is<RedisKey>(k => k!.ToString() == IndexSchemaDefinitions.BuildSyntacticKey("tenant-1", "mu-001")), Arg.Any<CommandFlags>())
            .Returns(
            [
                new HashEntry("id", "mu-001"),
                new HashEntry("caseId", "case-001"),
                new HashEntry("content", "Original content"),
                new HashEntry("contentHash", "hash-001"),
                new HashEntry("sourceUri", "file:///doc.txt"),
                new HashEntry("sourceType", "file"),
                new HashEntry("ingestedBy", "user@test.local"),
                new HashEntry("ingestedAt", "2026-07-01T10:00:00+00:00"),
                new HashEntry("lastUpdated", "2026-07-01T10:00:00+00:00"),
            ]);
        redisDb.KeyExistsAsync(Arg.Is<RedisKey>(k => k!.ToString() == IndexSchemaDefinitions.BuildSemanticKey("tenant-1", "mu-001")), Arg.Any<CommandFlags>())
            .Returns(true);

        CreateAnnotationInput input = new("tenant-1", "case-001", "mu-001", "Correction text", "annotator@test.local", "correction");

        // Act
        (MemoryUnit Annotation, string WorkflowInstanceId)? result = await service.CreateAnnotationAsync(input, CancellationToken.None);

        // Assert
        result.ShouldNotBeNull();
        result.Value.Annotation.Status.ShouldBe(MemoryUnitStatus.Queued);

        RequestAnnotationCommand command = commandStore.AcceptedCommands.ShouldHaveSingleItem().ShouldBeOfType<RequestAnnotationCommand>();
        command.TenantId.ShouldBe("tenant-1");
        command.CaseId.ShouldBe("case-001");
        command.TargetMemoryUnitId.ShouldBe("mu-001");
        command.AnnotationMemoryUnitId.ShouldBe(result.Value.Annotation.Id);

        ScheduledProjectionWorkflow workflow = workflowScheduler.ScheduledWorkflows.ShouldHaveSingleItem();
        workflow.WorkflowName.ShouldBe(nameof(AnnotationProjectionWorkflow));
        workflow.InstanceId.ShouldBe($"annotation-project-{result.Value.Annotation.Id}");
        AnnotationProjectionInput projectionInput = workflow.Input.ShouldBeOfType<AnnotationProjectionInput>();
        IngestionWorkflowConfiguration capturedConfiguration = projectionInput.WorkflowConfiguration.ShouldNotBeNull();
        capturedConfiguration.NaturalLanguage.PersistInMetadata.ShouldBeTrue();
        WorkflowActivityRetryPolicy capturedRetry =
            capturedConfiguration.Retry.ActivityOverrides["ExtractContentActivity"];
        capturedRetry.MaxAttempts.ShouldBe(9);
        capturedRetry.FirstRetryIntervalSeconds.ShouldBe(13);
        capturedRetry.BackoffCoefficient.ShouldBe(1.75);
        capturedRetry.MaxRetryIntervalSeconds.ShouldBe(97);
        WorkflowTraceContext capturedTrace = projectionInput.TraceContext.ShouldNotBeNull();
        capturedTrace.TraceParent.ShouldBe(activity.Id);
        capturedTrace.TraceState.ShouldBe("vendor=case-service-proof");

        operationLog.ShouldBe(
        [
            $"accept:{RequestAnnotationCommand.CommandType}",
            $"schedule:{nameof(AnnotationProjectionWorkflow)}",
        ]);

        // Story 21.2: graph stub/edge writes moved into AnnotationProjectionWorkflow activities.
        builder.DidNotReceive().BuildMergeStubNode(Arg.Any<string>(), Arg.Any<DateTimeOffset>());
        builder.DidNotReceive().BuildMergeEdge(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<EdgeType>(), Arg.Any<float>(), Arg.Any<EdgeOrigin>());
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
        CaseService service = new(redis, falkorDb, builder, CreateMockActivityService(), CreateMockWorkflowClient(), CreateMockActorProxyFactory(), logger);

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
        CaseService service = new(redis, falkorDb, builder, CreateMockActivityService(), CreateMockWorkflowClient(), CreateMockActorProxyFactory(), logger);

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
        CaseService service = new(redis, falkorDb, builder, CreateMockActivityService(), CreateMockWorkflowClient(), CreateMockActorProxyFactory(), logger);

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
        CaseService service = new(redis, falkorDb, builder, CreateMockActivityService(), CreateMockWorkflowClient(), CreateMockActorProxyFactory(), logger);

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

        builder.BuildListAnnotationIds(Arg.Any<string>())
            .Returns(("MATCH (a:MemoryUnit)-[:ANNOTATES]->(m:MemoryUnit {id: $memoryUnitId}) RETURN a.id AS annotationId", new Dictionary<string, object> { ["memoryUnitId"] = "mock" }));

        builder.BuildDeleteMemoryUnitNode(Arg.Any<string>())
            .Returns(callInfo => ($"MATCH (m:MemoryUnit {{id: $id}}) DETACH DELETE m", (IDictionary<string, object>)new Dictionary<string, object> { ["id"] = callInfo.Arg<string>()! }));

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
        CaseService service = new(redis, falkorDb, builder, CreateMockActivityService(), CreateMockWorkflowClient(), CreateMockActorProxyFactory(), logger);

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
        CaseService service = new(redis, falkorDb, builder, activityService, CreateMockWorkflowClient(), CreateMockActorProxyFactory(), logger);

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
        result.QueuedCount.ShouldBe(0);
        result.ExtractingCount.ShouldBe(0);
        result.EmbeddingCount.ShouldBe(0);
        result.IndexingCount.ShouldBe(0);
    }

    [Fact]
    public async Task GetCaseStatusAsync_WhenCounterActorReturnsCounts_ShouldPopulateInFlightFields()
    {
        // Arrange
        (IConnectionMultiplexer redis, IDatabase redisDb) = CreateMockRedis();
        (IConnectionMultiplexer falkorDb, _) = CreateMockFalkorDb();
        IGraphQueryBuilder builder = CreateMockBuilder();
        ILogger<CaseService> logger = NullLogger<CaseService>.Instance;
        (IConnectionMultiplexer activityRedis, IDatabase activityDb) = CreateMockRedis();
        activityDb.StreamRangeAsync(
            Arg.Any<RedisKey>(),
            Arg.Any<RedisValue?>(),
            Arg.Any<RedisValue?>(),
            Arg.Any<int?>(),
            Arg.Any<Order>(),
            Arg.Any<CommandFlags>())
            .Returns(_ => Array.Empty<StreamEntry>());

        CaseActivityService activityService = new(activityRedis, NullLogger<CaseActivityService>.Instance);
        CaseService service = new(
            redis,
            falkorDb,
            builder,
            activityService,
            CreateMockWorkflowClient(),
            CreateMockActorProxyFactory(new CaseIngestionCounts(2, 3, 4, 5)),
            logger);

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
        result.QueuedCount.ShouldBe(2);
        result.ExtractingCount.ShouldBe(3);
        result.EmbeddingCount.ShouldBe(4);
        result.IndexingCount.ShouldBe(5);
    }

    [Fact]
    public async Task GetCaseStatusAsync_WhenDeleting_ShouldReturnDeletionStartedAtFromSameSnapshot()
    {
        // Arrange
        (IConnectionMultiplexer redis, IDatabase redisDb) = CreateMockRedis();
        (IConnectionMultiplexer falkorDb, _) = CreateMockFalkorDb();
        IGraphQueryBuilder builder = CreateMockBuilder();
        ILogger<CaseService> logger = NullLogger<CaseService>.Instance;
        DateTimeOffset deletionStartedAt = DateTimeOffset.Parse("2026-04-13T09:00:00+00:00");

        CaseService service = new(redis, falkorDb, builder, CreateMockActivityService(), CreateMockWorkflowClient(), CreateMockActorProxyFactory(), logger);

        redisDb.HashGetAllAsync(Arg.Any<RedisKey>(), Arg.Any<CommandFlags>())
            .Returns(
            [
                new HashEntry("id", "case-123"),
                new HashEntry("tenantId", "tenant-1"),
                new HashEntry("name", "Deleting Case"),
                new HashEntry("description", "desc"),
                new HashEntry("status", "deleting"),
                new HashEntry("createdAt", "2026-04-01T10:00:00+00:00"),
                new HashEntry("lastUpdated", "2026-04-13T09:00:00+00:00"),
                new HashEntry("deletionStartedAt", deletionStartedAt.ToString("o")),
            ]);

        // Act
        CaseStatusDetail? result = await service.GetCaseStatusAsync("tenant-1", "case-123", CancellationToken.None);

        // Assert
        result.ShouldNotBeNull();
        result.Status.ShouldBe(CaseStatus.Deleting);
        result.DeletionStartedAt.ShouldBe(deletionStartedAt);
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
        CaseService service = new(redis, falkorDb, builder, CreateMockActivityService(), CreateMockWorkflowClient(), CreateMockActorProxyFactory(), logger);

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
            Arg.Is<RedisKey>(k => k!.ToString() == "tenant-1:case:case-001:members"),
            Arg.Is<RedisValue>(f => f!.ToString() == "user-alice"),
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
        CaseService service = new(redis, falkorDb, builder, activityService, CreateMockWorkflowClient(), CreateMockActorProxyFactory(), logger);

        redisDb.HashLengthAsync(Arg.Any<RedisKey>(), Arg.Any<CommandFlags>())
            .Returns(5L);
        // HSETNX returns false -- member already exists
        redisDb.HashSetAsync(Arg.Any<RedisKey>(), Arg.Any<RedisValue>(), Arg.Any<RedisValue>(), Arg.Is<When>(w => w == When.NotExists), Arg.Any<CommandFlags>())
            .Returns(false);

        string existingJson = "{\"memberId\":\"user-alice\",\"memberType\":\"user\",\"addedAt\":\"2026-04-01T10:00:00+00:00\"}";
        redisDb.HashGetAsync(Arg.Any<RedisKey>(), Arg.Is<RedisValue>(v => v!.ToString() == "user-alice"), Arg.Any<CommandFlags>())
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
        CaseService service = new(redis, falkorDb, builder, CreateMockActivityService(), CreateMockWorkflowClient(), CreateMockActorProxyFactory(), logger);

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
        CaseService service = new(redis, falkorDb, builder, activityService, CreateMockWorkflowClient(), CreateMockActorProxyFactory(), logger);

        redisDb.HashLengthAsync(Arg.Any<RedisKey>(), Arg.Any<CommandFlags>())
            .Returns(1000L);
        redisDb.HashGetAsync(Arg.Any<RedisKey>(), Arg.Is<RedisValue>(v => v!.ToString() == "user-alice"), Arg.Any<CommandFlags>())
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
        CaseService service = new(redis, falkorDb, builder, activityService, CreateMockWorkflowClient(), CreateMockActorProxyFactory(), logger);

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
        CaseService service = new(redis, falkorDb, builder, CreateMockActivityService(), CreateMockWorkflowClient(), CreateMockActorProxyFactory(), logger);

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
        CaseService service = new(redis, falkorDb, builder, CreateMockActivityService(), CreateMockWorkflowClient(), CreateMockActorProxyFactory(), logger);

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
        CaseService service = new(redis, falkorDb, builder, activityService, CreateMockWorkflowClient(), CreateMockActorProxyFactory(), logger);

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
        CaseService service = new(redis, falkorDb, builder, CreateMockActivityService(), CreateMockWorkflowClient(), CreateMockActorProxyFactory(), logger);

        redisDb.HashDeleteAsync(Arg.Any<RedisKey>(), Arg.Any<RedisValue>(), Arg.Any<CommandFlags>())
            .Returns(true);

        // Act
        bool result = await service.RemoveMemberAsync("tenant-1", "case-001", "user-alice", CancellationToken.None);

        // Assert
        result.ShouldBeTrue();
        await redisDb.Received(1).HashDeleteAsync(
            Arg.Is<RedisKey>(k => k!.ToString() == "tenant-1:case:case-001:members"),
            Arg.Is<RedisValue>(v => v!.ToString() == "user-alice"),
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
        CaseService service = new(redis, falkorDb, builder, activityService, CreateMockWorkflowClient(), CreateMockActorProxyFactory(), logger);

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
        CaseService service = new(redis, falkorDb, builder, CreateMockActivityService(), CreateMockWorkflowClient(), CreateMockActorProxyFactory(), logger);

        redisDb.HashGetAllAsync(Arg.Is<RedisKey>(k => k!.ToString().EndsWith(":members")), Arg.Any<CommandFlags>())
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
        CaseService service = new(redis, falkorDb, builder, CreateMockActivityService(), CreateMockWorkflowClient(), CreateMockActorProxyFactory(), logger);

        HashEntry[] entries =
        [
            new("user-bob", "{\"memberId\":\"user-bob\",\"memberType\":\"user\",\"addedAt\":\"2026-04-02T10:00:00+00:00\"}"),
            new("user-alice", "{\"memberId\":\"user-alice\",\"memberType\":\"user\",\"addedAt\":\"2026-04-01T10:00:00+00:00\"}"),
        ];
        redisDb.HashGetAllAsync(Arg.Is<RedisKey>(k => k!.ToString().EndsWith(":members")), Arg.Any<CommandFlags>())
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
        CaseService service = new(redis, falkorDb, builder, CreateMockActivityService(), CreateMockWorkflowClient(), CreateMockActorProxyFactory(), logger);

        HashEntry[] entries =
        [
            new("user-bad", "{\"memberId\":\"user-bad\"}"),
            new("user-alice", "{\"memberId\":\"user-alice\",\"memberType\":\"user\",\"addedAt\":\"2026-04-01T10:00:00+00:00\"}"),
        ];
        redisDb.HashGetAllAsync(Arg.Is<RedisKey>(k => k!.ToString().EndsWith(":members")), Arg.Any<CommandFlags>())
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
        CaseService service = new(redis, falkorDb, builder, CreateMockActivityService(), CreateMockWorkflowClient(), CreateMockActorProxyFactory(), logger);

        redisDb.HashLengthAsync(Arg.Is<RedisKey>(k => k!.ToString() == "tenant-1:case:case-001:members"), Arg.Any<CommandFlags>())
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
        CaseService service = new(redis, falkorDb, builder, CreateMockActivityService(), CreateMockWorkflowClient(), CreateMockActorProxyFactory(), logger);

        IBatch batch = Substitute.For<IBatch>();
        redisDb.CreateBatch(Arg.Any<object>()).Returns(batch);
        batch.HashGetAsync(Arg.Is<RedisKey>(k => k.ToString() == "tenant-1:case:case-1"), Arg.Is<RedisValue>(v => v!.ToString() == "name"), Arg.Any<CommandFlags>())
            .Returns(Task.FromResult(new RedisValue("Alpha")));
        batch.HashGetAsync(Arg.Is<RedisKey>(k => k.ToString() == "tenant-1:case:case-2"), Arg.Is<RedisValue>(v => v!.ToString() == "name"), Arg.Any<CommandFlags>())
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
        CaseService service = new(redis, falkorDb, builder, CreateMockActivityService(), CreateMockWorkflowClient(), CreateMockActorProxyFactory(), logger);

        IBatch batch = Substitute.For<IBatch>();
        redisDb.CreateBatch(Arg.Any<object>()).Returns(batch);
        batch.HashGetAsync(Arg.Any<RedisKey>(), Arg.Is<RedisValue>(v => v!.ToString() == "name"), Arg.Any<CommandFlags>())
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
        CaseService service = new(redis, falkorDb, builder, CreateMockActivityService(), CreateMockWorkflowClient(), CreateMockActorProxyFactory(), logger);

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
        CaseService service = new(redis, falkorDb, builder, CreateMockActivityService(), CreateMockWorkflowClient(), CreateMockActorProxyFactory(), logger);

        IBatch batch = Substitute.For<IBatch>();
        redisDb.CreateBatch(Arg.Any<object>()).Returns(batch);
        batch.HashGetAsync(Arg.Is<RedisKey>(k => k.ToString() == "tenant-1:case:case-1"), Arg.Is<RedisValue>(v => v!.ToString() == "name"), Arg.Any<CommandFlags>())
            .Returns(Task.FromResult(new RedisValue("Alpha")));

        // Act
        Dictionary<string, string> result = await service.ResolveNamesAsync("tenant-1", ["case-1", "case-1", "case-1"], CancellationToken.None);

        // Assert
        result.Count.ShouldBe(1);
        result["case-1"].ShouldBe("Alpha");
    }

    // Story 5.5 AC6 / FR70: memory unit hash with embeddingModel field populates MemoryUnit.EmbeddingModel.
    [Fact]
    public async Task GetMemoryUnitAsync_WhenHashHasEmbeddingModel_PopulatesMemoryUnitField()
    {
        (IConnectionMultiplexer redis, IDatabase redisDb) = CreateMockRedis();
        (IConnectionMultiplexer falkorDb, _) = CreateMockFalkorDb();
        IGraphQueryBuilder builder = CreateMockBuilder();
        CaseService service = new(redis, falkorDb, builder, CreateMockActivityService(), CreateMockWorkflowClient(), CreateMockActorProxyFactory(), NullLogger<CaseService>.Instance);

        redisDb.HashGetAllAsync(Arg.Is<RedisKey>(k => k!.ToString() == IndexSchemaDefinitions.BuildSyntacticKey("tenant-1", "mu-001")), Arg.Any<CommandFlags>())
            .Returns(
            [
                new HashEntry("id", "mu-001"),
                new HashEntry("caseId", "case-001"),
                new HashEntry("content", "hi"),
                new HashEntry("contentHash", "h"),
                new HashEntry("sourceUri", "file:///x.txt"),
                new HashEntry("sourceType", "file"),
                new HashEntry("ingestedBy", "u@x"),
                new HashEntry("ingestedAt", "2026-04-14T10:00:00+00:00"),
                new HashEntry("lastUpdated", "2026-04-14T10:00:00+00:00"),
                new HashEntry("embeddingProvider", "google:gemini-embedding-001"),
                new HashEntry("embeddingModel", "gemini-embedding-001"),
                new HashEntry("embeddingDimensions", "768"),
            ]);

        MemoryUnit? result = await service.GetMemoryUnitAsync("tenant-1", "mu-001", CancellationToken.None);

        result.ShouldNotBeNull();
        result.EmbeddingProvider.ShouldBe("google:gemini-embedding-001");
        result.EmbeddingModel.ShouldBe("gemini-embedding-001");
        result.EmbeddingDimensions.ShouldBe(768);
    }

    // Story 5.5 AC6 / FR70: legacy memory unit hash without embeddingModel returns null for that field.
    [Fact]
    public async Task GetMemoryUnitAsync_WhenHashLacksEmbeddingModel_ReturnsNullForField()
    {
        (IConnectionMultiplexer redis, IDatabase redisDb) = CreateMockRedis();
        (IConnectionMultiplexer falkorDb, _) = CreateMockFalkorDb();
        IGraphQueryBuilder builder = CreateMockBuilder();
        CaseService service = new(redis, falkorDb, builder, CreateMockActivityService(), CreateMockWorkflowClient(), CreateMockActorProxyFactory(), NullLogger<CaseService>.Instance);

        redisDb.HashGetAllAsync(Arg.Is<RedisKey>(k => k!.ToString() == IndexSchemaDefinitions.BuildSyntacticKey("tenant-1", "legacy-mu")), Arg.Any<CommandFlags>())
            .Returns(
            [
                new HashEntry("id", "legacy-mu"),
                new HashEntry("caseId", "case-001"),
                new HashEntry("content", "pre-5.5 memory unit"),
                new HashEntry("contentHash", "h"),
                new HashEntry("sourceUri", "file:///x.txt"),
                new HashEntry("sourceType", "file"),
                new HashEntry("ingestedBy", "u@x"),
                new HashEntry("ingestedAt", "2026-03-01T10:00:00+00:00"),
                new HashEntry("lastUpdated", "2026-03-01T10:00:00+00:00"),
                new HashEntry("embeddingProvider", "google:gemini-embedding-001"),
                // No embeddingModel — pre-FR70 data.
            ]);

        MemoryUnit? result = await service.GetMemoryUnitAsync("tenant-1", "legacy-mu", CancellationToken.None);

        result.ShouldNotBeNull();
        result.EmbeddingProvider.ShouldBe("google:gemini-embedding-001");
        result.EmbeddingModel.ShouldBeNull();
    }

    [Fact]
    public async Task GetMemoryUnitAsync_WhenHashLacksId_ShouldUseRequestedIdAndDeserializeMetadata()
    {
        // Arrange
        (IConnectionMultiplexer redis, IDatabase redisDb) = CreateMockRedis();
        (IConnectionMultiplexer falkorDb, _) = CreateMockFalkorDb();
        IGraphQueryBuilder builder = CreateMockBuilder();
        ILogger<CaseService> logger = NullLogger<CaseService>.Instance;
        CaseService service = new(redis, falkorDb, builder, CreateMockActivityService(), CreateMockWorkflowClient(), CreateMockActorProxyFactory(), logger);

        redisDb.HashGetAllAsync(Arg.Is<RedisKey>(k => k!.ToString() == IndexSchemaDefinitions.BuildSyntacticKey("tenant-1", "annotation-001")), Arg.Any<CommandFlags>())
            .Returns(
            [
                new HashEntry("caseId", "case-001"),
                new HashEntry("content", "Correction text"),
                new HashEntry("contentHash", "hash-001"),
                new HashEntry("sourceUri", "annotation:mu-001:annotation-001"),
                new HashEntry("sourceType", "annotation"),
                new HashEntry("ingestedBy", "annotator@test.local"),
                new HashEntry("ingestedAt", "2026-04-13T10:00:00+00:00"),
                new HashEntry("lastUpdated", "2026-04-13T10:00:00+00:00"),
                new HashEntry("metadataJson", "{\"_system.annotation_target\":{\"value\":\"mu-001\",\"origin\":\"human\",\"confidence\":1},\"_system.annotation_type\":{\"value\":\"correction\",\"origin\":\"human\",\"confidence\":1}}"),
            ]);

        // Act
        MemoryUnit? result = await service.GetMemoryUnitAsync("tenant-1", "annotation-001", CancellationToken.None);

        // Assert
        result.ShouldNotBeNull();
        result.Id.ShouldBe("annotation-001");
        result.SourceType.ShouldBe(SourceType.Annotation);
        result.Status.ShouldBe(MemoryUnitStatus.Indexed);
        result.Metadata["_system.annotation_target"].Value.ShouldBe("mu-001");
        result.Metadata["_system.annotation_type"].Value.ShouldBe("correction");
    }

    // --- DeleteMemoryUnitAsync tests ---

    [Fact]
    public async Task DeleteMemoryUnitAsync_MuFoundAndDeleted_ShouldAcceptCommandBeforeSchedulingProjectionCleanup()
    {
        // Arrange
        (IConnectionMultiplexer redis, IDatabase redisDb) = CreateMockRedis();
        (IConnectionMultiplexer falkorDb, _) = CreateMockFalkorDb();
        IGraphQueryBuilder builder = CreateMockBuilder();
        List<string> operationLog = [];
        CapturingCommandStore commandStore = new(operationLog);
        CapturingProjectionWorkflowScheduler workflowScheduler = new(operationLog);
        CaseService service = new(
            redis,
            falkorDb,
            builder,
            CreateMockActivityService(),
            CreateMockWorkflowClient(),
            CreateMockActorProxyFactory(),
            NullLogger<CaseService>.Instance,
            commandStore,
            workflowScheduler);

        redisDb.HashGetAsync(Arg.Is<RedisKey>(k => k.ToString() == IndexSchemaDefinitions.BuildSyntacticKey("tenant-1", "mu-001")), Arg.Is<RedisValue>(v => v!.ToString() == "caseId"), Arg.Any<CommandFlags>())
            .Returns(new RedisValue("case-001"));

        // Act
        bool result = await service.DeleteMemoryUnitAsync("tenant-1", "case-001", "mu-001", CancellationToken.None);

        // Assert
        result.ShouldBeTrue();

        DeleteMemoryUnitCommand command = commandStore.AcceptedCommands.ShouldHaveSingleItem().ShouldBeOfType<DeleteMemoryUnitCommand>();
        command.TenantId.ShouldBe("tenant-1");
        command.CaseId.ShouldBe("case-001");
        command.MemoryUnitId.ShouldBe("mu-001");
        command.AnnotationMemoryUnitIds.ShouldBeEmpty();

        ScheduledProjectionWorkflow workflow = workflowScheduler.ScheduledWorkflows.ShouldHaveSingleItem();
        workflow.WorkflowName.ShouldBe(nameof(MemoryUnitDeletionProjectionWorkflow));
        workflow.InstanceId.ShouldBe("memory-unit-delete-mu-001");
        MemoryUnitDeletionProjectionInput projectionInput = workflow.Input.ShouldBeOfType<MemoryUnitDeletionProjectionInput>();
        projectionInput.TenantId.ShouldBe("tenant-1");
        projectionInput.CaseId.ShouldBe("case-001");
        projectionInput.MemoryUnitId.ShouldBe("mu-001");

        operationLog.ShouldBe(
        [
            $"accept:{DeleteMemoryUnitCommand.CommandType}",
            $"schedule:{nameof(MemoryUnitDeletionProjectionWorkflow)}",
        ]);

        // Story 21.2: syntactic/vector/graph deletion and activity recording are workflow-owned
        // projection cleanup now — the service must not delete read-model keys directly.
        await redisDb.DidNotReceive().KeyDeleteAsync(Arg.Any<RedisKey>(), Arg.Any<CommandFlags>());
        builder.DidNotReceive().BuildDeleteMemoryUnitNode(Arg.Any<string>());
    }

    [Fact]
    public async Task DeleteMemoryUnitAsync_WhenCommandAcceptFails_ShouldNotScheduleProjectionOrTouchReadModels()
    {
        // Arrange
        (IConnectionMultiplexer redis, IDatabase redisDb) = CreateMockRedis();
        (IConnectionMultiplexer falkorDb, _) = CreateMockFalkorDb();
        IGraphQueryBuilder builder = CreateMockBuilder();
        CapturingProjectionWorkflowScheduler workflowScheduler = new();
        CaseService service = new(
            redis,
            falkorDb,
            builder,
            CreateMockActivityService(),
            CreateMockWorkflowClient(),
            CreateMockActorProxyFactory(),
            NullLogger<CaseService>.Instance,
            new FailingCommandStore(),
            workflowScheduler);

        redisDb.HashGetAsync(Arg.Is<RedisKey>(k => k.ToString() == IndexSchemaDefinitions.BuildSyntacticKey("tenant-1", "mu-001")), Arg.Is<RedisValue>(v => v!.ToString() == "caseId"), Arg.Any<CommandFlags>())
            .Returns(new RedisValue("case-001"));

        // Act / Assert — EventStore accept failure must surface and leave every projection intact.
        InvalidOperationException exception = await Should.ThrowAsync<InvalidOperationException>(
            () => service.DeleteMemoryUnitAsync("tenant-1", "case-001", "mu-001", CancellationToken.None));

        exception.Message.ShouldBe("EVENTSTORE_UNAVAILABLE");
        workflowScheduler.ScheduledWorkflows.ShouldBeEmpty();
        await redisDb.DidNotReceive().KeyDeleteAsync(Arg.Any<RedisKey>(), Arg.Any<CommandFlags>());
        builder.DidNotReceive().BuildDeleteMemoryUnitNode(Arg.Any<string>());
    }

    [Fact]
    public async Task DeleteMemoryUnitAsync_MuNotFound_ShouldReturnFalse()
    {
        // Arrange
        (IConnectionMultiplexer redis, IDatabase redisDb) = CreateMockRedis();
        (IConnectionMultiplexer falkorDb, _) = CreateMockFalkorDb();
        IGraphQueryBuilder builder = CreateMockBuilder();
        CaseService service = new(redis, falkorDb, builder, CreateMockActivityService(), CreateMockWorkflowClient(), CreateMockActorProxyFactory(), NullLogger<CaseService>.Instance);

        redisDb.HashGetAsync(Arg.Is<RedisKey>(k => k.ToString() == IndexSchemaDefinitions.BuildSyntacticKey("tenant-1", "mu-missing")), Arg.Is<RedisValue>(v => v!.ToString() == "caseId"), Arg.Any<CommandFlags>())
            .Returns(RedisValue.Null);

        // Act
        bool result = await service.DeleteMemoryUnitAsync("tenant-1", "case-001", "mu-missing", CancellationToken.None);

        // Assert
        result.ShouldBeFalse();
        await redisDb.DidNotReceive().KeyDeleteAsync(Arg.Is<RedisKey>(k => k!.ToString().Contains("mu-missing")), Arg.Any<CommandFlags>());
    }

    [Fact]
    public async Task DeleteMemoryUnitAsync_MuWrongCase_ShouldReturnFalse()
    {
        // Arrange
        (IConnectionMultiplexer redis, IDatabase redisDb) = CreateMockRedis();
        (IConnectionMultiplexer falkorDb, _) = CreateMockFalkorDb();
        IGraphQueryBuilder builder = CreateMockBuilder();
        CaseService service = new(redis, falkorDb, builder, CreateMockActivityService(), CreateMockWorkflowClient(), CreateMockActorProxyFactory(), NullLogger<CaseService>.Instance);

        redisDb.HashGetAsync(Arg.Is<RedisKey>(k => k.ToString() == IndexSchemaDefinitions.BuildSyntacticKey("tenant-1", "mu-001")), Arg.Is<RedisValue>(v => v!.ToString() == "caseId"), Arg.Any<CommandFlags>())
            .Returns(new RedisValue("case-OTHER"));

        // Act
        bool result = await service.DeleteMemoryUnitAsync("tenant-1", "case-001", "mu-001", CancellationToken.None);

        // Assert
        result.ShouldBeFalse();
        await redisDb.DidNotReceive().KeyDeleteAsync(Arg.Is<RedisKey>(k => k!.ToString().Contains("mu-001")), Arg.Any<CommandFlags>());
    }

    [Fact]
    public async Task DeleteMemoryUnitAsync_WhenAnnotationsExist_ShouldCarryAnnotationIdsThroughCommandAndProjectionInput()
    {
        // Arrange
        (IConnectionMultiplexer redis, IDatabase redisDb) = CreateMockRedis();
        (IConnectionMultiplexer falkorDb, _) = CreateMockFalkorDbWithMuIds("ann-001", "ann-002");
        IGraphQueryBuilder builder = CreateMockBuilder();
        CapturingCommandStore commandStore = new();
        CapturingProjectionWorkflowScheduler workflowScheduler = new();
        CaseService service = new(
            redis,
            falkorDb,
            builder,
            CreateMockActivityService(),
            CreateMockWorkflowClient(),
            CreateMockActorProxyFactory(),
            NullLogger<CaseService>.Instance,
            commandStore,
            workflowScheduler);

        redisDb.HashGetAsync(Arg.Is<RedisKey>(k => k.ToString() == IndexSchemaDefinitions.BuildSyntacticKey("tenant-1", "mu-001")), Arg.Is<RedisValue>(v => v!.ToString() == "caseId"), Arg.Any<CommandFlags>())
            .Returns(new RedisValue("case-001"));

        // Act
        bool result = await service.DeleteMemoryUnitAsync("tenant-1", "case-001", "mu-001", CancellationToken.None);

        // Assert — the cascade set is resolved before the command so the durable record and the
        // projection cleanup input both name every annotation memory unit.
        result.ShouldBeTrue();

        DeleteMemoryUnitCommand command = commandStore.AcceptedCommands.ShouldHaveSingleItem().ShouldBeOfType<DeleteMemoryUnitCommand>();
        command.AnnotationMemoryUnitIds.ShouldBe(new[] { "ann-001", "ann-002" });

        MemoryUnitDeletionProjectionInput projectionInput = workflowScheduler.ScheduledWorkflows
            .ShouldHaveSingleItem().Input.ShouldBeOfType<MemoryUnitDeletionProjectionInput>();
        projectionInput.AnnotationMemoryUnitIds.ShouldBe(new[] { "ann-001", "ann-002" });

        await redisDb.DidNotReceive().KeyDeleteAsync(Arg.Any<RedisKey>(), Arg.Any<CommandFlags>());
    }

    // --- DeleteCaseAsync tests ---

    [Fact]
    public async Task DeleteCaseAsync_CaseNotFound_ShouldReturnFalse()
    {
        // Arrange
        (IConnectionMultiplexer redis, IDatabase redisDb) = CreateMockRedis();
        (IConnectionMultiplexer falkorDb, _) = CreateMockFalkorDb();
        IGraphQueryBuilder builder = CreateMockBuilder();
        CaseService service = new(redis, falkorDb, builder, CreateMockActivityService(), CreateMockWorkflowClient(), CreateMockActorProxyFactory(), NullLogger<CaseService>.Instance);

        redisDb.KeyExistsAsync(Arg.Is<RedisKey>(k => k!.ToString() == "tenant-1:case:case-missing"), Arg.Any<CommandFlags>())
            .Returns(false);

        // Act
        bool result = await service.DeleteCaseAsync("tenant-1", "case-missing", CancellationToken.None);

        // Assert
        result.ShouldBeFalse();
        await redisDb.DidNotReceive().HashSetAsync(Arg.Any<RedisKey>(), Arg.Any<HashEntry[]>(), Arg.Any<CommandFlags>());
    }

    [Fact]
    public async Task DeleteCaseAsync_CaseWithZeroMus_ShouldAcceptCommandAndScheduleProjectionCleanup()
    {
        // Arrange
        (IConnectionMultiplexer redis, IDatabase redisDb) = CreateMockRedis();
        (IConnectionMultiplexer falkorDb, _) = CreateMockFalkorDb();
        IGraphQueryBuilder builder = CreateMockBuilder();
        builder.BuildListCaseMemoryUnitIds(Arg.Any<string>())
            .Returns(("MATCH query", new Dictionary<string, object> { ["caseId"] = "case-001" }));
        List<string> operationLog = [];
        CapturingCommandStore commandStore = new(operationLog);
        CapturingProjectionWorkflowScheduler workflowScheduler = new(operationLog);
        CaseService service = new(
            redis,
            falkorDb,
            builder,
            CreateMockActivityService(),
            CreateMockWorkflowClient(),
            CreateMockActorProxyFactory(),
            NullLogger<CaseService>.Instance,
            commandStore,
            workflowScheduler);

        redisDb.KeyExistsAsync(Arg.Is<RedisKey>(k => k!.ToString() == "tenant-1:case:case-001"), Arg.Any<CommandFlags>())
            .Returns(true);

        // Act
        bool result = await service.DeleteCaseAsync("tenant-1", "case-001", CancellationToken.None);

        // Assert
        result.ShouldBeTrue();

        DeleteCaseCommand command = commandStore.AcceptedCommands.ShouldHaveSingleItem().ShouldBeOfType<DeleteCaseCommand>();
        command.TenantId.ShouldBe("tenant-1");
        command.CaseId.ShouldBe("case-001");
        command.MemoryUnitIds.ShouldBeEmpty();

        ScheduledProjectionWorkflow workflow = workflowScheduler.ScheduledWorkflows.ShouldHaveSingleItem();
        workflow.WorkflowName.ShouldBe(nameof(CaseDeletionProjectionWorkflow));
        workflow.InstanceId.ShouldBe("case-delete-case-001");

        operationLog.ShouldBe(
        [
            $"accept:{DeleteCaseCommand.CommandType}",
            $"schedule:{nameof(CaseDeletionProjectionWorkflow)}",
        ]);

        // Story 21.2: the "deleting" status guard and Redis/graph cleanup are workflow-owned
        // projection steps (MarkCaseDeletingActivity, DeleteCaseProjectionActivity) now.
        await redisDb.DidNotReceive().HashSetAsync(Arg.Any<RedisKey>(), Arg.Any<HashEntry[]>(), Arg.Any<CommandFlags>());
        await redisDb.DidNotReceive().KeyDeleteAsync(Arg.Any<RedisKey>(), Arg.Any<CommandFlags>());
        builder.DidNotReceive().BuildDeleteCaseNode(Arg.Any<string>());
    }

    [Fact]
    public async Task DeleteCaseAsync_CaseWithThreeMus_ShouldCarryMemoryUnitIdsThroughCommandAndProjectionInput()
    {
        // Arrange
        (IConnectionMultiplexer redis, IDatabase redisDb) = CreateMockRedis();
        (IConnectionMultiplexer falkorDb, _) = CreateMockFalkorDbWithMuIds("mu-001", "mu-002", "mu-003");
        IGraphQueryBuilder builder = CreateMockBuilder();
        builder.BuildListCaseMemoryUnitIds(Arg.Any<string>())
            .Returns(("LIST query", new Dictionary<string, object> { ["caseId"] = "case-001" }));
        CapturingCommandStore commandStore = new();
        CapturingProjectionWorkflowScheduler workflowScheduler = new();
        CaseService service = new(
            redis,
            falkorDb,
            builder,
            CreateMockActivityService(),
            CreateMockWorkflowClient(),
            CreateMockActorProxyFactory(),
            NullLogger<CaseService>.Instance,
            commandStore,
            workflowScheduler);

        redisDb.KeyExistsAsync(Arg.Is<RedisKey>(k => k!.ToString() == "tenant-1:case:case-001"), Arg.Any<CommandFlags>())
            .Returns(true);

        // Act
        bool result = await service.DeleteCaseAsync("tenant-1", "case-001", CancellationToken.None);

        // Assert — the graph read resolves the member set, but every write goes through the
        // durable command plus workflow-owned projection cleanup.
        result.ShouldBeTrue();

        builder.Received(1).BuildListCaseMemoryUnitIds("case-001");

        DeleteCaseCommand command = commandStore.AcceptedCommands.ShouldHaveSingleItem().ShouldBeOfType<DeleteCaseCommand>();
        command.MemoryUnitIds.ShouldBe(new[] { "mu-001", "mu-002", "mu-003" });

        CaseDeletionProjectionInput projectionInput = workflowScheduler.ScheduledWorkflows
            .ShouldHaveSingleItem().Input.ShouldBeOfType<CaseDeletionProjectionInput>();
        projectionInput.MemoryUnitIds.ShouldBe(new[] { "mu-001", "mu-002", "mu-003" });

        builder.DidNotReceive().BuildDeleteMemoryUnitNode(Arg.Any<string>());
        builder.DidNotReceive().BuildDeleteCaseNode(Arg.Any<string>());
        await redisDb.DidNotReceive().KeyDeleteAsync(Arg.Any<RedisKey>(), Arg.Any<CommandFlags>());
    }

    private static CaseActivityService CreateMockActivityService()
    {
        (IConnectionMultiplexer activityRedis, _) = CreateMockRedis();
        return new CaseActivityService(activityRedis, NullLogger<CaseActivityService>.Instance);
    }

    private static DaprWorkflowClient CreateMockWorkflowClient()
        => null!;

    private sealed class CapturingCommandStore(List<string>? operationLog = null) : IMemoriesCommandStore
    {
        public List<object> AcceptedCommands { get; } = [];

        public Task<string> AcceptAsync<TCommand>(
            string tenantId,
            TCommand command,
            string actorId,
            CancellationToken cancellationToken)
            where TCommand : IMemoriesCommandContract
        {
            tenantId.ShouldNotBeNullOrWhiteSpace();
            actorId.ShouldNotBeNullOrWhiteSpace();
            AcceptedCommands.Add(command);
            operationLog?.Add($"accept:{TCommand.CommandType}");
            return Task.FromResult($"accepted-{AcceptedCommands.Count}");
        }
    }

    private sealed class FailingCommandStore : IMemoriesCommandStore
    {
        public Task<string> AcceptAsync<TCommand>(
            string tenantId,
            TCommand command,
            string actorId,
            CancellationToken cancellationToken)
            where TCommand : IMemoriesCommandContract
            => Task.FromException<string>(new InvalidOperationException("EVENTSTORE_UNAVAILABLE"));
    }

    private sealed class CapturingProjectionWorkflowScheduler(List<string>? operationLog = null) : ICaseProjectionWorkflowScheduler
    {
        public List<ScheduledProjectionWorkflow> ScheduledWorkflows { get; } = [];

        public Task<string> ScheduleAsync(
            string workflowName,
            string instanceId,
            object input,
            CancellationToken cancellationToken)
        {
            ScheduledWorkflows.Add(new ScheduledProjectionWorkflow(workflowName, instanceId, input));
            operationLog?.Add($"schedule:{workflowName}");
            return Task.FromResult(instanceId);
        }
    }

    private sealed record ScheduledProjectionWorkflow(string WorkflowName, string InstanceId, object Input);

    private static Dapr.Actors.Client.IActorProxyFactory CreateMockActorProxyFactory(CaseIngestionCounts? counts = null)
    {
        // CreateActorProxy<T>(ActorId, string) is an extension method on IActorProxyFactory; it calls
        // the non-generic CreateActorProxy(ActorId, Type, string) under the hood. Stub that so the
        // proxy returned is a live NSubstitute mock producing zero counts (matching the fallback
        // behavior operators would see from a cold / just-provisioned actor).
        Hexalith.Memories.Server.Actors.ICaseIngestionCounterActor proxy =
            Substitute.For<Hexalith.Memories.Server.Actors.ICaseIngestionCounterActor>();
        proxy.GetCountsAsync().Returns(Task.FromResult(counts ?? new Hexalith.Memories.Contracts.V1.CaseIngestionCounts(0, 0, 0, 0)));

        Dapr.Actors.Client.IActorProxyFactory factory = Substitute.For<Dapr.Actors.Client.IActorProxyFactory>();
        factory.CreateActorProxy<Hexalith.Memories.Server.Actors.ICaseIngestionCounterActor>(
            Arg.Any<Dapr.Actors.ActorId>(),
            Arg.Any<string>()).Returns(proxy);
        factory.CreateActorProxy(
            Arg.Any<Dapr.Actors.ActorId>(),
            Arg.Any<Type>(),
            Arg.Any<string>(),
            Arg.Any<Dapr.Actors.Client.ActorProxyOptions?>()).Returns(proxy);
        return factory;
    }

    private static (IConnectionMultiplexer, IDatabase) CreateMockFalkorDb()
    {
        IDatabase db = Substitute.For<IDatabase>();
        IConnectionMultiplexer falkorDb = Substitute.For<IConnectionMultiplexer>();
        falkorDb.GetDatabase(Arg.Any<int>(), Arg.Any<object>()).Returns(db);

        RedisResult fakeGraphResult = CreateEmptyFalkorDbResult();

        db.ExecuteAsync(Arg.Any<string>(), Arg.Any<object[]>())
            .Returns(fakeGraphResult);
        db.ExecuteAsync(Arg.Any<string>(), Arg.Any<ICollection<object>>(), Arg.Any<CommandFlags>())
            .Returns(fakeGraphResult);

        return (falkorDb, db);
    }

    private static (IConnectionMultiplexer, IDatabase) CreateMockFalkorDbWithMuIds(params string[] muIds)
    {
        IDatabase db = Substitute.For<IDatabase>();
        IConnectionMultiplexer falkorDb = Substitute.For<IConnectionMultiplexer>();
        falkorDb.GetDatabase(Arg.Any<int>(), Arg.Any<object>()).Returns(db);

        // First call returns MU IDs (for BuildListCaseMemoryUnitIds), subsequent calls return empty
        RedisResult listResult = CreateFalkorDbResultWithMuIds(muIds);
        RedisResult emptyResult = CreateEmptyFalkorDbResult();

        int callCount = 0;
        db.ExecuteAsync(Arg.Any<string>(), Arg.Any<object[]>())
            .Returns(callInfo => Interlocked.Increment(ref callCount) == 1 ? listResult : emptyResult);
        db.ExecuteAsync(Arg.Any<string>(), Arg.Any<ICollection<object>>(), Arg.Any<CommandFlags>())
            .Returns(callInfo => Interlocked.Increment(ref callCount) == 1 ? listResult : emptyResult);

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

    private static RedisResult CreateFalkorDbResultWithMuIds(string[] muIds)
    {
        // FalkorDB compact format: [headers, data_rows, statistics]
        // Header: [[1, "memoryUnitId"]] (1=COLUMN_SCALAR)
        // Data row cell: [2, "mu-id"] (2=SI_STRING)
        RedisResult headers = RedisResult.Create(
        [
            RedisResult.Create(
            [
                RedisResult.Create((RedisValue)1),
                RedisResult.Create(new RedisValue("memoryUnitId")),
            ]),
        ]);

        RedisResult[] dataRows = muIds.Select(id => RedisResult.Create(new[]
        {
            RedisResult.Create(
            [
                RedisResult.Create((RedisValue)2),
                RedisResult.Create(new RedisValue(id)),
            ]),
        })).ToArray();

        RedisResult data = RedisResult.Create(dataRows);

        RedisResult stats = RedisResult.Create(
        [
            RedisResult.Create(new RedisValue("Cached execution: 0")),
            RedisResult.Create(new RedisValue("Query internal execution time: 0.1 milliseconds")),
        ]);

        return RedisResult.Create([headers, data, stats]);
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
