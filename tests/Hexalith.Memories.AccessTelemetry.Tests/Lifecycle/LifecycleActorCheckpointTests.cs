// <copyright file="LifecycleActorCheckpointTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.AccessTelemetry.Tests.Lifecycle;

using Hexalith.Memories.AccessTelemetry.Contracts;
using Hexalith.Memories.AccessTelemetry.Lifecycle;
using Hexalith.Memories.AccessTelemetry.Tests.Observability;

using Microsoft.Extensions.Time.Testing;

using Shouldly;

/// <summary>Story 27.2 C3 checkpoint for the fixed lifecycle authority and purge.</summary>
[Collection(AccessTelemetryLifecycleMetricsTestCollection.Name)]
public sealed class LifecycleActorCheckpointTests
{
    private static readonly DateTimeOffset Now = new(2026, 7, 18, 10, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task PersistAsync_WritesRecordAndExpiryIndexAtomicallyWithCeilingTtl()
    {
        var store = new InMemoryAccessTelemetryStateStore();
        var processor = new AccessTelemetryLifecycleProcessor(
            store,
            new FakeTimeProvider(Now),
            new AccessTelemetryOptions { Retention = TimeSpan.FromSeconds(3600.1) });
        AccessTelemetryRecord record = Rehash(CreateRecord(Now, Now.AddHours(10)) with
        {
            AcceptedAtUtc = Format(Now.AddMinutes(-5)),
        });

        AccessTelemetryPersistenceResult result = await processor.PersistAsync(record, CancellationToken.None);

        result.Status.ShouldBe(AccessTelemetryPersistenceStatus.Inserted);
        result.TtlInSeconds.ShouldBe(3601);
        store.RecordCount.ShouldBe(1);
        store.IndexCount.ShouldBe(1);
        store.LastTransactionOperationCount.ShouldBe(2);
        AccessTelemetryRecord persisted = store.GetRecord(record.RecordId)!;
        persisted.AcceptedAtUtc.ShouldBe(Format(Now));
        persisted.ExpiresAtUtc.ShouldBe("2026-07-18T11:00:00.100Z");
        persisted.EnvelopeHash.ShouldNotBe(record.EnvelopeHash);
    }

    [Fact]
    public async Task PersistAsync_SameIdHashAndExpiry_IsIdempotent()
    {
        var store = new InMemoryAccessTelemetryStateStore();
        var processor = new AccessTelemetryLifecycleProcessor(store, new FakeTimeProvider(Now));
        AccessTelemetryRecord record = CreateRecord(Now.AddSeconds(-1), Now.AddHours(1));

        AccessTelemetryPersistenceResult first = await processor.PersistAsync(record, CancellationToken.None);
        AccessTelemetryPersistenceResult second = await processor.PersistAsync(record, CancellationToken.None);

        first.Status.ShouldBe(AccessTelemetryPersistenceStatus.Inserted);
        second.Status.ShouldBe(AccessTelemetryPersistenceStatus.Idempotent);
        store.RecordCount.ShouldBe(1);
        store.IndexCount.ShouldBe(1);
    }

    [Fact]
    public async Task PersistAsync_SameIdWithDifferentEnvelopeOrExpiry_ReturnsConflict()
    {
        var store = new InMemoryAccessTelemetryStateStore();
        var processor = new AccessTelemetryLifecycleProcessor(
            store,
            new FakeTimeProvider(Now),
            new AccessTelemetryOptions { Retention = TimeSpan.FromHours(1) });
        AccessTelemetryRecord first = CreateRecord(Now.AddSeconds(-1), Now.AddHours(1));
        AccessTelemetryRecord changed = Rehash(first with { DurationMs = first.DurationMs + 1 });
        var extendedProcessor = new AccessTelemetryLifecycleProcessor(
            store,
            new FakeTimeProvider(Now),
            new AccessTelemetryOptions { Retention = TimeSpan.FromHours(2) });

        await processor.PersistAsync(first, CancellationToken.None);

        (await processor.PersistAsync(changed, CancellationToken.None)).Reason.ShouldBe(AccessTelemetryReason.RecordIdConflict);
        (await extendedProcessor.PersistAsync(first, CancellationToken.None)).Reason.ShouldBe(AccessTelemetryReason.RecordIdConflict);
        processor.Health.ShouldBe(AccessTelemetryHealthState.Unhealthy);
    }

    [Theory]
    [InlineData(1001, 3600, AccessTelemetryReason.ClockUntrusted)]
    [InlineData(-3_600_001, -1, AccessTelemetryReason.Expired)]
    public async Task PersistAsync_FutureOrExpiredSource_FailsClosed(
        int emissionOffsetMilliseconds,
        int expiryOffsetSeconds,
        AccessTelemetryReason expected)
    {
        var processor = new AccessTelemetryLifecycleProcessor(
            new InMemoryAccessTelemetryStateStore(),
            new FakeTimeProvider(Now),
            new AccessTelemetryOptions { Retention = TimeSpan.FromHours(1) });
        AccessTelemetryRecord record = CreateRecord(
            Now.AddMilliseconds(emissionOffsetMilliseconds),
            Now.AddSeconds(expiryOffsetSeconds));

        AccessTelemetryPersistenceResult result = await processor.PersistAsync(record, CancellationToken.None);

        result.Status.ShouldBe(AccessTelemetryPersistenceStatus.Rejected);
        result.Reason.ShouldBe(expected);
    }

    [Fact]
    public async Task PurgeAsync_DeletesAndStronglyVerifiesOnlyDueRecords()
    {
        var store = new InMemoryAccessTelemetryStateStore();
        var clock = new FakeTimeProvider(Now.AddMinutes(-10));
        var processor = new AccessTelemetryLifecycleProcessor(
            store,
            clock,
            new AccessTelemetryOptions { Retention = TimeSpan.FromMinutes(5) });
        AccessTelemetryRecord due = CreateRecord(clock.GetUtcNow().AddSeconds(-1), Now.AddMinutes(-1));
        await processor.PersistAsync(due, CancellationToken.None);
        clock.SetUtcNow(Now);
        AccessTelemetryRecord newer = CreateRecord(clock.GetUtcNow().AddSeconds(-1), Now.AddMinutes(10)) with
        {
            RecordId = new MonotonicRecordIdGenerator().NewId(),
        };
        newer = Rehash(newer);
        await processor.PersistAsync(newer, CancellationToken.None);

        AccessTelemetryPurgeResult result = await processor.PurgeAsync(CancellationToken.None);

        result.Purged.ShouldBe(1);
        result.VerifiedAbsent.ShouldBe(1);
        store.ContainsRecord(due.RecordId).ShouldBeFalse();
        store.ContainsRecord(newer.RecordId).ShouldBeTrue();
        store.IndexCount.ShouldBe(1);
    }

    [Fact]
    public async Task PurgeAsync_IsBoundedTo512RecordsPerActorTurn()
    {
        var store = new InMemoryAccessTelemetryStateStore();
        var insertionClock = new FakeTimeProvider(Now.AddHours(-2));
        var processor = new AccessTelemetryLifecycleProcessor(
            store,
            insertionClock,
            new AccessTelemetryOptions { Retention = TimeSpan.FromHours(1) });
        var ids = new MonotonicRecordIdGenerator();
        for (int index = 0; index < 513; index++)
        {
            AccessTelemetryRecord record = CreateRecord(insertionClock.GetUtcNow(), Now.AddMinutes(-1)) with
            {
                RecordId = ids.NewId(),
            };
            await processor.PersistAsync(Rehash(record), CancellationToken.None);
        }

        insertionClock.SetUtcNow(Now);
        AccessTelemetryPurgeResult result = await processor.PurgeAsync(CancellationToken.None);

        result.Processed.ShouldBe(512);
        result.HasMore.ShouldBeTrue();
        store.RecordCount.ShouldBe(1);
    }

    [Theory]
    [InlineData((int)AccessTelemetryDeleteStatus.StaleIndex, AccessTelemetryHealthState.Healthy)]
    [InlineData((int)AccessTelemetryDeleteStatus.VerificationFailed, AccessTelemetryHealthState.Unhealthy)]
    public async Task PurgeAsync_CountsOnlyVerifiedAbsenceAndSurfacesVerificationFailure(
        int deleteStatusValue,
        AccessTelemetryHealthState expectedHealth)
    {
        AccessTelemetryDeleteStatus deleteStatus = (AccessTelemetryDeleteStatus)deleteStatusValue;
        AccessTelemetryRecord record = CreateRecord(Now.AddHours(-2), Now.AddHours(-1));
        var entry = new AccessTelemetryExpiryEntry(
            record.RecordId,
            AccessTelemetryExpiryIndex.GetExpiryMinute(Now.AddHours(-1)),
            AccessTelemetryExpiryIndex.GetShard(record.RecordId),
            record.EnvelopeHash,
            Format(Now.AddHours(-1)));
        var processor = new AccessTelemetryLifecycleProcessor(
            new ScriptedDeleteStateStore(entry, deleteStatus),
            new FakeTimeProvider(Now),
            new AccessTelemetryOptions { Retention = TimeSpan.FromHours(1) });

        AccessTelemetryPurgeResult result = await processor.PurgeAsync(CancellationToken.None);

        result.Processed.ShouldBe(1);
        result.Purged.ShouldBe(0);
        result.VerifiedAbsent.ShouldBe(0);
        result.LastExpiryMinute.ShouldBe(entry.ExpiryMinute);
        result.LastExpiryShard.ShouldBe(entry.Shard);
        processor.Health.ShouldBe(expectedHealth);
    }

    [Fact]
    public void ExpiryIndex_UsesExactly64DeterministicShards()
    {
        var ids = new MonotonicRecordIdGenerator();
        int[] shards = Enumerable.Range(0, 5000).Select(_ => AccessTelemetryExpiryIndex.GetShard(ids.NewId())).ToArray();

        shards.ShouldAllBe(static value => value >= 0 && value < 64);
        shards.Distinct().Count().ShouldBe(64);
        AccessTelemetryExpiryIndex.GetShard("01HM5Q9WXGK6T8Q4Z5Y6V7W8X9")
            .ShouldBe(AccessTelemetryExpiryIndex.GetShard("01HM5Q9WXGK6T8Q4Z5Y6V7W8X9"));
    }

    [Fact]
    public void MarkerRotation_RequiresEveryLiveWriterAcknowledgementAndOldQueueDrain()
    {
        long now = Now.ToUnixTimeMilliseconds();
        WriterHeartbeat writerA = Heartbeat("writer-a", "old", 0, now + 30_000);
        WriterHeartbeat writerB = Heartbeat("writer-b", "old", 3, now + 30_000);
        MarkerKeyRotationState state = MarkerKeyRotationCoordinator.Stage(
            new MarkerKeyRotationState { ActiveGeneration = "old" },
            "new",
            [writerA, writerB],
            now);

        state = MarkerKeyRotationCoordinator.Acknowledge(state, writerA with { MarkerKeyGeneration = "new" }, now);
        MarkerKeyRotationCoordinator.TryBeginDrain(state, [writerA, writerB], now, out _).ShouldBeFalse();
        state = MarkerKeyRotationCoordinator.Acknowledge(state, writerB with { MarkerKeyGeneration = "new" }, now);
        MarkerKeyRotationCoordinator.TryBeginDrain(state, [writerA, writerB], now, out state).ShouldBeTrue();
        MarkerKeyRotationCoordinator.TryActivate(state, [writerA, writerB], now, now, out _).ShouldBeFalse();

        MarkerKeyRotationCoordinator.TryActivate(
            state,
            [writerA, writerB with { OldKeyQueueCount = 0 }],
            now,
            now,
            out MarkerKeyRotationState active).ShouldBeTrue();
        active.ActiveGeneration.ShouldBe("new");
        active.OldGenerationRetireAfterUnixMilliseconds.ShouldBe(
            now + (long)(TimeSpan.FromDays(7) + TimeSpan.FromMinutes(15) + TimeSpan.FromSeconds(1)).TotalMilliseconds);
    }

    [Fact]
    public void MarkerRotation_ExpiredWriterLeaseCannotBlockActivationOrCreateOldWork()
    {
        long now = Now.ToUnixTimeMilliseconds();
        WriterHeartbeat live = Heartbeat("live", "old", 0, now + 30_000);
        WriterHeartbeat departed = Heartbeat("departed", "old", 9, now - 1);
        MarkerKeyRotationState state = MarkerKeyRotationCoordinator.Stage(
            new MarkerKeyRotationState { ActiveGeneration = "old" },
            "new",
            [live, departed],
            now);
        state = MarkerKeyRotationCoordinator.Acknowledge(state, live with { MarkerKeyGeneration = "new" }, now);

        MarkerKeyRotationCoordinator.TryBeginDrain(state, [live, departed], now, out state).ShouldBeTrue();
        MarkerKeyRotationCoordinator.TryActivate(state, [live, departed], now, now, out state).ShouldBeTrue();
        state.ActiveGeneration.ShouldBe("new");
    }

    private static AccessTelemetryRecord CreateRecord(DateTimeOffset emitted, DateTimeOffset expires)
    {
        AccessTelemetryRecord record = new()
        {
            AcceptedAtUtc = Format(Now),
            CaseMarker = null,
            DurationMs = 42,
            EmittedAtUtc = Format(emitted),
            EnvelopeHash = string.Empty,
            ErrorCode = null,
            EventId = 7501,
            ExpiresAtUtc = Format(expires),
            MarkerKeyId = "mk-2026a",
            OperationType = "search",
            Outcome = "ok",
            QueryParams = new Dictionary<string, object?>(StringComparer.Ordinal)
            {
                ["axis"] = "hybrid",
                ["caseScope"] = "all-authorized",
                ["explain"] = false,
                ["queryLengthBucket"] = "33-128",
                ["subjectPresent"] = true,
                ["weightProfile"] = "configured",
            },
            RecordId = new MonotonicRecordIdGenerator().NewId(),
            ResultCount = 1,
            SchemaVersion = 1,
            SpanId = null,
            TenantMarker = new string('a', 64),
            TraceId = null,
            UserMarker = null,
        };
        return Rehash(record);
    }

    private static string Format(DateTimeOffset value)
        => value.UtcDateTime.ToString("yyyy-MM-dd'T'HH:mm:ss.fff'Z'", System.Globalization.CultureInfo.InvariantCulture);

    private static AccessTelemetryRecord Rehash(AccessTelemetryRecord record)
        => record with { EnvelopeHash = AccessTelemetryCanonicalizer.CalculateEnvelopeHash(record) };

    private static WriterHeartbeat Heartbeat(string id, string generation, int queued, long leaseExpiry)
        => new()
        {
            DeploymentId = "deployment-a",
            ServiceInstanceId = id,
            ProcessEpoch = $"{id}-process",
            MarkerKeyGeneration = generation,
            OldKeyQueueCount = queued,
            LeaseExpiresAtUnixMilliseconds = leaseExpiry,
        };

    private sealed class ScriptedDeleteStateStore(
        AccessTelemetryExpiryEntry entry,
        AccessTelemetryDeleteStatus deleteStatus) : IAccessTelemetryStateStore
    {
        public Task<AccessTelemetryStoreWriteStatus> WriteRecordAndIndexAsync(
            AccessTelemetryRecord record,
            AccessTelemetryExpiryEntry expiryEntry,
            int ttlInSeconds,
            CancellationToken cancellationToken)
            => throw new NotSupportedException();

        public Task<IReadOnlyList<AccessTelemetryExpiryEntry>> GetDueEntriesAsync(
            long dueMinute,
            int limit,
            CancellationToken cancellationToken)
            => Task.FromResult<IReadOnlyList<AccessTelemetryExpiryEntry>>([entry]);

        public Task<AccessTelemetryDeleteStatus> DeleteAndVerifyAsync(
            AccessTelemetryExpiryEntry expiryEntry,
            CancellationToken cancellationToken)
            => Task.FromResult(deleteStatus);
    }
}
