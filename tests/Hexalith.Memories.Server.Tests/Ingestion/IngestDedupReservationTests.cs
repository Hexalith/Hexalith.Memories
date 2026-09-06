// <copyright file="IngestDedupReservationTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Tests.Ingestion;

using Hexalith.Memories.Server.Activities.Ingestion;
using Hexalith.Memories.Server.Ingestion;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

using NSubstitute;
using NSubstitute.ExceptionExtensions;

using Shouldly;

using StackExchange.Redis;

/// <summary>
/// Story 18.4 (AC3) — the authoritative deterministic unit proof that the REST-ingress preflight reservation
/// closes the concurrent same-source ingest race: exactly one ingest wins (atomic <c>SET … NX</c>) and the
/// loser observes the winner's workflow instance id. This class owns substitute-controlled deterministic
/// coverage; <c>IngestDedupReservationIntegrationTests</c> owns the production-backed two-thread Redis race proof.
/// </summary>
public class IngestDedupReservationTests
{
    private static readonly TimeSpan Ttl = TimeSpan.FromHours(24);

    [Fact]
    public async Task TryReserveAsync_FirstIngest_WinsAndOwnsItsInstanceId()
    {
        (IDatabase db, IConnectionMultiplexer redis) = CreateRedis();
        SetNxReturns(db, true);
        IngestDedupReservation reservation = new(redis, NullLogger<IngestDedupReservation>.Instance);

        IngestReservationResult result = await reservation.TryReserveAsync(
            "tenant-1", "case-1", "file:///doc.pdf", idempotencyToken: null, "wf-winner", Ttl, CancellationToken.None);

        result.Outcome.ShouldBe(IngestReservationOutcome.Reserved);
        result.ExistingInstanceId.ShouldBe("wf-winner");
    }

    [Fact]
    public async Task TryReserveAsync_CanceledBeforeDispatch_ThrowsWithoutCallingRedis()
    {
        (IDatabase db, IConnectionMultiplexer redis) = CreateRedis();
        IngestDedupReservation reservation = new(redis, NullLogger<IngestDedupReservation>.Instance);
        using CancellationTokenSource cancellation = new();
        cancellation.Cancel();

        await Should.ThrowAsync<OperationCanceledException>(() => reservation.TryReserveAsync(
            "tenant-1", "case-1", "file:///doc.pdf", idempotencyToken: null, "wf-candidate", Ttl, cancellation.Token));

        redis.DidNotReceive().GetDatabase(Arg.Any<int>(), Arg.Any<object>());
        db.ReceivedCalls().ShouldBeEmpty();
    }

    [Fact]
    public async Task TryReserveAsync_CanceledPendingSet_LateSuccessUsesAtomicOwnerCheckedCleanup()
    {
        (IDatabase db, IConnectionMultiplexer redis) = CreateRedis();
        TaskCompletionSource<bool> pendingSet = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource cleanupDispatched = new(TaskCreationOptions.RunContinuationsAsynchronously);
        db.StringSetAsync(
                Arg.Any<RedisKey>(), Arg.Any<RedisValue>(), Arg.Any<TimeSpan?>(),
                Arg.Any<When>())
            .Returns(_ => pendingSet.Task);
        db.ScriptEvaluateAsync(
                Arg.Any<string>(), Arg.Any<RedisKey[]>(), Arg.Any<RedisValue[]>(), Arg.Any<CommandFlags>())
            .Returns(_ =>
            {
                cleanupDispatched.TrySetResult();
                return Task.FromResult(RedisResult.Create((RedisValue)1L));
            });
        IngestDedupReservation reservation = new(redis, NullLogger<IngestDedupReservation>.Instance);
        using CancellationTokenSource cancellation = new();

        Task<IngestReservationResult> attempt = reservation.TryReserveAsync(
            "tenant-1", "case-1", "file:///doc.pdf", idempotencyToken: null, "wf-candidate", Ttl, cancellation.Token);
        attempt.IsCompleted.ShouldBeFalse();

        cancellation.Cancel();
        await Should.ThrowAsync<OperationCanceledException>(() => attempt);

        pendingSet.SetResult(true);
        await cleanupDispatched.Task.WaitAsync(TestContext.Current.CancellationToken);

        string expectedKey = "ingest-reserve:" + DedupKeyBuilder.BuildKey("tenant-1", "case-1", "file:///doc.pdf");
        var cleanupCall = db.ReceivedCalls().Single(call => call.GetMethodInfo().Name == nameof(IDatabase.ScriptEvaluateAsync));
        object?[] cleanupArguments = cleanupCall.GetArguments();
        ((string)cleanupArguments[0]!).ShouldBe(
            "if redis.call('GET', KEYS[1]) == ARGV[1] then return redis.call('DEL', KEYS[1]) else return 0 end");
        RedisKey[] cleanupKeys = (RedisKey[])cleanupArguments[1]!;
        cleanupKeys.Length.ShouldBe(1);
        cleanupKeys[0].ShouldBe((RedisKey)expectedKey);
        RedisValue[] cleanupValues = (RedisValue[])cleanupArguments[2]!;
        cleanupValues.Length.ShouldBe(1);
        cleanupValues[0].ShouldBe((RedisValue)"wf-candidate");
        await db.DidNotReceive().KeyDeleteAsync(Arg.Any<RedisKey>(), Arg.Any<CommandFlags>());
    }

    [Fact]
    public async Task TryReserveAsync_CanceledPendingSet_WhenSuccessorOwnsKey_DoesNotDeleteDirectly()
    {
        (IDatabase db, IConnectionMultiplexer redis) = CreateRedis();
        TaskCompletionSource<bool> pendingSet = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource cleanupDispatched = new(TaskCreationOptions.RunContinuationsAsynchronously);
        db.StringSetAsync(
                Arg.Any<RedisKey>(), Arg.Any<RedisValue>(), Arg.Any<TimeSpan?>(),
                Arg.Any<When>())
            .Returns(_ => pendingSet.Task);

        // Redis returns zero from the value-checked script when the key has expired and a successor owns it.
        db.ScriptEvaluateAsync(
                Arg.Any<string>(), Arg.Any<RedisKey[]>(), Arg.Any<RedisValue[]>(), Arg.Any<CommandFlags>())
            .Returns(_ =>
            {
                cleanupDispatched.TrySetResult();
                return Task.FromResult(RedisResult.Create((RedisValue)0L));
            });
        IngestDedupReservation reservation = new(redis, NullLogger<IngestDedupReservation>.Instance);
        using CancellationTokenSource cancellation = new();

        Task<IngestReservationResult> attempt = reservation.TryReserveAsync(
            "tenant-1", "case-1", "file:///doc.pdf", idempotencyToken: null, "wf-candidate", Ttl, cancellation.Token);

        cancellation.Cancel();
        await Should.ThrowAsync<OperationCanceledException>(() => attempt);
        pendingSet.SetResult(true);
        await cleanupDispatched.Task.WaitAsync(TestContext.Current.CancellationToken);

        await db.DidNotReceive().KeyDeleteAsync(Arg.Any<RedisKey>(), Arg.Any<CommandFlags>());
        RedisValue[] cleanupValues = (RedisValue[])db.ReceivedCalls()
            .Single(call => call.GetMethodInfo().Name == nameof(IDatabase.ScriptEvaluateAsync))
            .GetArguments()[2]!;
        cleanupValues.ShouldBe([(RedisValue)"wf-candidate"]);
    }

    [Fact]
    public async Task TryReserveAsync_CanceledPendingSet_WhenAtomicCleanupFails_ObservesAndLogsFailure()
    {
        (IDatabase db, IConnectionMultiplexer redis) = CreateRedis();
        TaskCompletionSource<bool> pendingSet = new(TaskCreationOptions.RunContinuationsAsynchronously);
        db.StringSetAsync(
                Arg.Any<RedisKey>(), Arg.Any<RedisValue>(), Arg.Any<TimeSpan?>(),
                Arg.Any<When>())
            .Returns(_ => pendingSet.Task);
        db.ScriptEvaluateAsync(
                Arg.Any<string>(), Arg.Any<RedisKey[]>(), Arg.Any<RedisValue[]>(), Arg.Any<CommandFlags>())
            .Returns(_ => Task.FromException<RedisResult>(
                new RedisConnectionException(ConnectionFailureType.UnableToConnect, CommandFlags.None, "down")));
        ILogger<IngestDedupReservation> logger = Substitute.For<ILogger<IngestDedupReservation>>();
        logger.IsEnabled(LogLevel.Warning).Returns(true);
        IngestDedupReservation reservation = new(redis, logger);
        using CancellationTokenSource cancellation = new();

        Task<IngestReservationResult> attempt = reservation.TryReserveAsync(
            "tenant-1", "case-1", "file:///doc.pdf", idempotencyToken: null, "wf-candidate", Ttl, cancellation.Token);

        cancellation.Cancel();
        await Should.ThrowAsync<OperationCanceledException>(() => attempt);
        pendingSet.SetResult(true);
        await WaitForLogEventAsync(logger, eventId: 9133);

        var logCall = logger.ReceivedCalls().Single(call =>
            call.GetMethodInfo().Name == nameof(ILogger.Log)
            && ((EventId)call.GetArguments()[1]!).Id == 9133);
        logCall.GetArguments()[0].ShouldBe(LogLevel.Warning);
    }

    [Fact]
    public async Task TryReserveAsync_CanceledPendingWinnerLookup_ThrowsPromptly()
    {
        (IDatabase db, IConnectionMultiplexer redis) = CreateRedis();
        SetNxReturns(db, false);
        TaskCompletionSource<RedisValue> pendingGet = new(TaskCreationOptions.RunContinuationsAsynchronously);
        db.StringGetAsync(Arg.Any<RedisKey>(), Arg.Any<CommandFlags>()).Returns(_ => pendingGet.Task);
        IngestDedupReservation reservation = new(redis, NullLogger<IngestDedupReservation>.Instance);
        using CancellationTokenSource cancellation = new();

        Task<IngestReservationResult> attempt = reservation.TryReserveAsync(
            "tenant-1", "case-1", "file:///doc.pdf", idempotencyToken: null, "wf-loser", Ttl, cancellation.Token);
        attempt.IsCompleted.ShouldBeFalse();

        cancellation.Cancel();
        await Should.ThrowAsync<OperationCanceledException>(() => attempt);
        pendingGet.SetResult(RedisValue.Null);
    }

    [Fact]
    public async Task TryReserveAsync_ConcurrentLoser_ObservesWinnerInstanceId()
    {
        (IDatabase db, IConnectionMultiplexer redis) = CreateRedis();
        SetNxReturns(db, false); // key already held
        db.StringGetAsync(Arg.Any<RedisKey>(), Arg.Any<CommandFlags>())
            .Returns((RedisValue)"wf-winner");
        IngestDedupReservation reservation = new(redis, NullLogger<IngestDedupReservation>.Instance);

        IngestReservationResult result = await reservation.TryReserveAsync(
            "tenant-1", "case-1", "file:///doc.pdf", idempotencyToken: null, "wf-loser", Ttl, CancellationToken.None);

        result.Outcome.ShouldBe(IngestReservationOutcome.DuplicateInFlight);
        result.ExistingInstanceId.ShouldBe("wf-winner");
    }

    [Fact]
    public async Task TryReserveAsync_TwoNearSimultaneousIngests_ExactlyOneWins_LoserGetsWinnerId()
    {
        // Single shared store: the first SET NX succeeds (winner), the second fails and reads the winner's id.
        (IDatabase db, IConnectionMultiplexer redis) = CreateRedis();
        db.StringSetAsync(
                Arg.Any<RedisKey>(), Arg.Any<RedisValue>(), Arg.Any<TimeSpan?>(),
                Arg.Any<When>())
            .Returns(true, false); // winner, then loser
        db.StringGetAsync(Arg.Any<RedisKey>(), Arg.Any<CommandFlags>())
            .Returns((RedisValue)"wf-A");
        IngestDedupReservation reservation = new(redis, NullLogger<IngestDedupReservation>.Instance);

        IngestReservationResult first = await reservation.TryReserveAsync(
            "tenant-1", "case-1", "file:///doc.pdf", idempotencyToken: null, "wf-A", Ttl, CancellationToken.None);
        IngestReservationResult second = await reservation.TryReserveAsync(
            "tenant-1", "case-1", "file:///doc.pdf", idempotencyToken: null, "wf-B", Ttl, CancellationToken.None);

        first.Outcome.ShouldBe(IngestReservationOutcome.Reserved);
        second.Outcome.ShouldBe(IngestReservationOutcome.DuplicateInFlight);
        // Exactly one winner; the loser resolves to the SAME instance id (⇒ same MemoryUnitId for File).
        first.ExistingInstanceId.ShouldBe("wf-A");
        second.ExistingInstanceId.ShouldBe("wf-A");
    }

    [Fact]
    public async Task TryReserveAsync_UsesIngestReserveKeyOverSourceUriDedupKey_WhenNoToken()
    {
        (IDatabase db, IConnectionMultiplexer redis) = CreateRedis();
        SetNxReturns(db, true);
        IngestDedupReservation reservation = new(redis, NullLogger<IngestDedupReservation>.Instance);

        await reservation.TryReserveAsync(
            "tenant-1", "case-1", "file:///doc.pdf", idempotencyToken: null, "wf-1", Ttl, CancellationToken.None);

        string expectedKey = "ingest-reserve:" + DedupKeyBuilder.BuildKey("tenant-1", "case-1", "file:///doc.pdf");
        await db.Received(1).StringSetAsync(
            expectedKey, Arg.Any<RedisValue>(), Arg.Any<TimeSpan?>(),
            When.NotExists);
    }

    [Fact]
    public async Task TryReserveAsync_WithToken_KeysOnTokenNotSourceUri()
    {
        (IDatabase db, IConnectionMultiplexer redis) = CreateRedis();
        SetNxReturns(db, true);
        IngestDedupReservation reservation = new(redis, NullLogger<IngestDedupReservation>.Instance);

        await reservation.TryReserveAsync(
            "tenant-1", "case-1", "file:///doc.pdf", idempotencyToken: "idem-xyz", "wf-1", Ttl, CancellationToken.None);

        string expectedTokenKey = "ingest-reserve:" + DedupKeyBuilder.BuildTokenKey("tenant-1", "case-1", "idem-xyz");
        await db.Received(1).StringSetAsync(
            expectedTokenKey, Arg.Any<RedisValue>(), Arg.Any<TimeSpan?>(),
            When.NotExists);
    }

    [Fact]
    public async Task TryReserveAsync_NxFailsButKeyAlreadyExpired_FailsOpen()
    {
        // The NX set fails (someone held the key) but by the time we read it the reservation TTL already
        // elapsed and the key is gone. There is no winner id to hand back, so we proceed (fail-open) and let
        // the permanent dedup key / CheckIdempotencyActivity be the authoritative safety net (ADR 9.1-B).
        (IDatabase db, IConnectionMultiplexer redis) = CreateRedis();
        SetNxReturns(db, false);
        db.StringGetAsync(Arg.Any<RedisKey>(), Arg.Any<CommandFlags>())
            .Returns(RedisValue.Null);
        IngestDedupReservation reservation = new(redis, NullLogger<IngestDedupReservation>.Instance);

        IngestReservationResult result = await reservation.TryReserveAsync(
            "tenant-1", "case-1", "file:///doc.pdf", idempotencyToken: null, "wf-1", Ttl, CancellationToken.None);

        result.Outcome.ShouldBe(IngestReservationOutcome.FailOpen);
        result.ExistingInstanceId.ShouldBeNull();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task TryReserveAsync_BlankInstanceId_ThrowsArgumentException(string instanceId)
    {
        (IDatabase _, IConnectionMultiplexer redis) = CreateRedis();
        IngestDedupReservation reservation = new(redis, NullLogger<IngestDedupReservation>.Instance);

        await Should.ThrowAsync<ArgumentException>(() => reservation.TryReserveAsync(
            "tenant-1", "case-1", "file:///doc.pdf", idempotencyToken: null, instanceId, Ttl, CancellationToken.None));
    }

    [Fact]
    public async Task TryReserveAsync_RedisConnectionDown_FailsOpen()
    {
        (IDatabase db, IConnectionMultiplexer redis) = CreateRedis();
        db.StringSetAsync(
                Arg.Any<RedisKey>(), Arg.Any<RedisValue>(), Arg.Any<TimeSpan?>(),
                Arg.Any<When>())
            .ThrowsAsync(new RedisConnectionException(ConnectionFailureType.UnableToConnect, StackExchange.Redis.CommandFlags.None, "down"));
        IngestDedupReservation reservation = new(redis, NullLogger<IngestDedupReservation>.Instance);

        IngestReservationResult result = await reservation.TryReserveAsync(
            "tenant-1", "case-1", "file:///doc.pdf", idempotencyToken: null, "wf-1", Ttl, CancellationToken.None);

        result.Outcome.ShouldBe(IngestReservationOutcome.FailOpen);
        result.ExistingInstanceId.ShouldBeNull();
    }

    [Fact]
    public async Task TryReserveAsync_RedisTimeout_FailsOpen()
    {
        (IDatabase db, IConnectionMultiplexer redis) = CreateRedis();
        db.StringSetAsync(
                Arg.Any<RedisKey>(), Arg.Any<RedisValue>(), Arg.Any<TimeSpan?>(),
                Arg.Any<When>())
            .ThrowsAsync(new RedisTimeoutException(StackExchange.Redis.CommandFlags.None, "timeout", CommandStatus.Unknown));
        IngestDedupReservation reservation = new(redis, NullLogger<IngestDedupReservation>.Instance);

        IngestReservationResult result = await reservation.TryReserveAsync(
            "tenant-1", "case-1", "file:///doc.pdf", idempotencyToken: null, "wf-1", Ttl, CancellationToken.None);

        result.Outcome.ShouldBe(IngestReservationOutcome.FailOpen);
    }

    [Fact]
    public async Task TryReserveAsync_WinnerLookupRedisFailure_FailsOpen()
    {
        (IDatabase db, IConnectionMultiplexer redis) = CreateRedis();
        SetNxReturns(db, false);
        db.StringGetAsync(Arg.Any<RedisKey>(), Arg.Any<CommandFlags>())
            .ThrowsAsync(new RedisConnectionException(ConnectionFailureType.UnableToConnect, StackExchange.Redis.CommandFlags.None, "down"));
        IngestDedupReservation reservation = new(redis, NullLogger<IngestDedupReservation>.Instance);

        IngestReservationResult result = await reservation.TryReserveAsync(
            "tenant-1", "case-1", "file:///doc.pdf", idempotencyToken: null, "wf-1", Ttl, CancellationToken.None);

        result.Outcome.ShouldBe(IngestReservationOutcome.FailOpen);
        result.ExistingInstanceId.ShouldBeNull();
    }

    [Fact]
    public async Task ReleaseAsync_DeletesTheReservationKey()
    {
        (IDatabase db, IConnectionMultiplexer redis) = CreateRedis();
        IngestDedupReservation reservation = new(redis, NullLogger<IngestDedupReservation>.Instance);

        await reservation.ReleaseAsync("tenant-1", "case-1", "file:///doc.pdf", idempotencyToken: null, CancellationToken.None);

        string expectedKey = "ingest-reserve:" + DedupKeyBuilder.BuildKey("tenant-1", "case-1", "file:///doc.pdf");
        await db.Received(1).KeyDeleteAsync(expectedKey, Arg.Any<CommandFlags>());
    }

    [Fact]
    public async Task ReleaseAsync_CanceledPendingDelete_ThrowsPromptly()
    {
        (IDatabase db, IConnectionMultiplexer redis) = CreateRedis();
        TaskCompletionSource<bool> pendingDelete = new(TaskCreationOptions.RunContinuationsAsynchronously);
        db.KeyDeleteAsync(Arg.Any<RedisKey>(), Arg.Any<CommandFlags>()).Returns(_ => pendingDelete.Task);
        IngestDedupReservation reservation = new(redis, NullLogger<IngestDedupReservation>.Instance);
        using CancellationTokenSource cancellation = new();

        Task release = reservation.ReleaseAsync(
            "tenant-1", "case-1", "file:///doc.pdf", idempotencyToken: null, cancellation.Token);
        release.IsCompleted.ShouldBeFalse();

        cancellation.Cancel();
        await Should.ThrowAsync<OperationCanceledException>(() => release);
        pendingDelete.SetResult(true);
    }

    [Fact]
    public async Task ReleaseAsync_RedisFailure_DoesNotThrow()
    {
        // Compensation must never turn a release failure into a hard error (invariant 8); the reservation
        // TTL is the backstop. ReleaseAsync swallows the Redis exception and logs a warning instead.
        (IDatabase db, IConnectionMultiplexer redis) = CreateRedis();
        db.KeyDeleteAsync(Arg.Any<RedisKey>(), Arg.Any<CommandFlags>())
            .ThrowsAsync(new RedisConnectionException(ConnectionFailureType.UnableToConnect, StackExchange.Redis.CommandFlags.None, "down"));
        IngestDedupReservation reservation = new(redis, NullLogger<IngestDedupReservation>.Instance);

        await Should.NotThrowAsync(() => reservation.ReleaseAsync(
            "tenant-1", "case-1", "file:///doc.pdf", idempotencyToken: null, CancellationToken.None));
    }

    private static void SetNxReturns(IDatabase db, bool acquired)
        => db.StringSetAsync(
                Arg.Any<RedisKey>(), Arg.Any<RedisValue>(), Arg.Any<TimeSpan?>(),
                Arg.Any<When>())
            .Returns(acquired);

    private static (IDatabase Db, IConnectionMultiplexer Redis) CreateRedis()
    {
        IDatabase db = Substitute.For<IDatabase>();
        IConnectionMultiplexer redis = Substitute.For<IConnectionMultiplexer>();
        redis.GetDatabase(Arg.Any<int>(), Arg.Any<object>()).Returns(db);
        return (db, redis);
    }

    private static async Task WaitForLogEventAsync(ILogger logger, int eventId)
    {
        while (!logger.ReceivedCalls().Any(call =>
            call.GetMethodInfo().Name == nameof(ILogger.Log)
            && ((EventId)call.GetArguments()[1]!).Id == eventId))
        {
            TestContext.Current.CancellationToken.ThrowIfCancellationRequested();
            await Task.Yield();
        }
    }
}
