namespace Hexalith.Memories.Server.Tests.Cases;

using Dapr.Workflow;

using Hexalith.Memories.Contracts.V1;
using Hexalith.Memories.Server.Activities.Ingestion;
using Hexalith.Memories.Server.Cases;

using Microsoft.Extensions.Logging.Abstractions;

using NSubstitute;
using NSubstitute.ExceptionExtensions;

using Shouldly;

using StackExchange.Redis;

public class RecordCaseActivityActivityTests
{
    [Fact]
    public async Task RunAsync_ShouldCallRecordEventWithCorrectParameters()
    {
        // Arrange
        (IConnectionMultiplexer redis, IDatabase db) = CreateMockRedis();
        CaseActivityService activityService = new(redis, NullLogger<CaseActivityService>.Instance);
        RecordCaseActivityActivity activity = new(activityService);
        WorkflowActivityContext context = Substitute.For<WorkflowActivityContext>();

        var input = new CaseActivityInput(
            "tenant-1",
            "case-001",
            CaseActivityEventType.MemoryUnitIngested,
            "user-123",
            "Memory unit indexed",
            "mu-abc");

        // Act
        bool result = await activity.RunAsync(context, input);

        // Assert
        result.ShouldBeTrue();
        IEnumerable<NSubstitute.Core.ICall> calls = db.ReceivedCalls()
            .Where(c => c.GetMethodInfo().Name == "StreamAddAsync");
        calls.Count().ShouldBe(1);
        RedisKey key = (RedisKey)calls.First().GetArguments()[0]!;
        key.ToString().ShouldBe("tenant-1:case:case-001:activity");
    }

    [Fact]
    public async Task RunAsync_OnFailure_ShouldReturnFalse()
    {
        // Arrange
        (IConnectionMultiplexer redis, IDatabase db) = CreateMockRedis();
        // Cover both StreamAddAsync overloads
        db.StreamAddAsync(Arg.Any<RedisKey>(), Arg.Any<NameValueEntry[]>(), Arg.Any<RedisValue?>(), Arg.Any<int?>(), Arg.Any<bool>(), Arg.Any<CommandFlags>())
            .ThrowsAsync(new RedisConnectionException(ConnectionFailureType.UnableToConnect, StackExchange.Redis.CommandFlags.None, "Connection refused"));
        db.StreamAddAsync(Arg.Any<RedisKey>(), Arg.Any<NameValueEntry[]>(), Arg.Any<RedisValue?>(), Arg.Any<long?>(), Arg.Any<bool>(), Arg.Any<long?>(), Arg.Any<StreamTrimMode>(), Arg.Any<CommandFlags>())
            .ThrowsAsync(new RedisConnectionException(ConnectionFailureType.UnableToConnect, StackExchange.Redis.CommandFlags.None, "Connection refused"));

        CaseActivityService activityService = new(redis, NullLogger<CaseActivityService>.Instance);
        RecordCaseActivityActivity activity = new(activityService);
        WorkflowActivityContext context = Substitute.For<WorkflowActivityContext>();

        var input = new CaseActivityInput(
            "tenant-1", "case-001",
            CaseActivityEventType.CaseCreated,
            "system", "Case created", null);

        // Act
        bool result = await activity.RunAsync(context, input);

        // Assert
        result.ShouldBeFalse();
    }

    private static (IConnectionMultiplexer, IDatabase) CreateMockRedis()
    {
        IDatabase db = Substitute.For<IDatabase>();
        IConnectionMultiplexer redis = Substitute.For<IConnectionMultiplexer>();
        redis.GetDatabase(Arg.Any<int>(), Arg.Any<object>()).Returns(db);
        return (redis, db);
    }
}
