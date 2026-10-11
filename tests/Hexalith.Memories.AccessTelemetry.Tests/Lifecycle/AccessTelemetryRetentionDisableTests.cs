namespace Hexalith.Memories.AccessTelemetry.Tests.Lifecycle;

using Dapr.Actors;
using Dapr.Actors.Runtime;

using Hexalith.Memories.AccessTelemetry.Contracts;
using Hexalith.Memories.AccessTelemetry.Lifecycle;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Time.Testing;

using Shouldly;

/// <summary>Verifies a registered actor's retention reminder obeys the live disable control.</summary>
public sealed class AccessTelemetryRetentionDisableTests
{
    [Fact]
    public async Task ExistingReminder_Disabled_SkipsPurgeAndCanBeReenabled()
    {
        IConfigurationRoot configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["AccessTelemetryLifecycle:Enabled"] = "false",
            })
            .Build();
        ActorHost host = ActorHost.CreateForTest<AccessTelemetryLifecycleActor>(
            new ActorTestOptions { ActorId = new ActorId("global") });
        var actor = new AccessTelemetryLifecycleActor(
            host,
            null!,
            null!,
            null!,
            null!,
            null!,
            null!,
            configuration);

        await actor.ReceiveReminderAsync("bounded-purge", [], TimeSpan.Zero, TimeSpan.FromMinutes(1));
        AccessTelemetryLifecycleActor.IsPurgeEnabled(configuration).ShouldBeFalse();

        configuration["AccessTelemetryLifecycle:Enabled"] = "true";
        AccessTelemetryLifecycleActor.IsPurgeEnabled(configuration).ShouldBeTrue();
    }

    [Fact]
    public async Task ExistingReminder_Disabled_PreservesDueRecordUntilReenabled()
    {
        IConfigurationRoot configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["AccessTelemetryLifecycle:Enabled"] = "false",
            })
            .Build();
        DateTimeOffset now = new(2026, 10, 11, 12, 0, 0, TimeSpan.Zero);
        var due = new AccessTelemetryExpiryEntry("record-a", 1, 0, "hash", "2026-10-11T11:00:00.000Z");
        var store = new DueRecordStore(due);
        var options = new AccessTelemetryOptions();
        var processor = new AccessTelemetryLifecycleProcessor(store, TimeProvider.System, options);
        ActorHost host = ActorHost.CreateForTest<AccessTelemetryLifecycleActor>(
            new ActorTestOptions { ActorId = new ActorId("global") });
        var actor = new AccessTelemetryLifecycleActor(
            host,
            processor,
            new FixedClockGate(now),
            null!,
            new AccessTelemetryRuntimeOptionsProvider(options),
            new FixedEvidenceProvider(),
            TimeProvider.System,
            configuration);

        await actor.ReceiveReminderAsync("bounded-purge", [], TimeSpan.Zero, TimeSpan.FromMinutes(1));
        store.DeleteCount.ShouldBe(0);

        configuration["AccessTelemetryLifecycle:Enabled"] = "true";
        // The unit-test host has no Dapr state provider for the actor's post-purge bookkeeping.
        _ = await Should.ThrowAsync<InvalidOperationException>(() =>
            actor.ReceiveReminderAsync("bounded-purge", [], TimeSpan.Zero, TimeSpan.FromMinutes(1)));
        store.DeleteCount.ShouldBe(1);
    }

    [Fact]
    public async Task ActivePurge_DisabledAfterFirstDelete_PreservesRemainingRecordUntilReenabled()
    {
        DateTimeOffset now = new(2026, 10, 11, 10, 0, 0, TimeSpan.Zero);
        IConfigurationRoot configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["AccessTelemetryLifecycle:Enabled"] = "true",
            })
            .Build();
        var store = new ScriptedRetentionStateStore(
            now.AddMinutes(-1),
            () => configuration["AccessTelemetryLifecycle:Enabled"] = "false");
        var processor = new AccessTelemetryLifecycleProcessor(store, new FakeTimeProvider(now));

        AccessTelemetryPurgeResult stopped = await processor.PurgeAsync(
            now,
            CancellationToken.None,
            () => AccessTelemetryLifecycleActor.IsPurgeEnabled(configuration));

        stopped.Purged.ShouldBe(1);
        stopped.HasMore.ShouldBeTrue();
        store.Remaining.ShouldBe(1);

        configuration["AccessTelemetryLifecycle:Enabled"] = "true";
        AccessTelemetryPurgeResult resumed = await processor.PurgeAsync(
            now,
            CancellationToken.None,
            () => AccessTelemetryLifecycleActor.IsPurgeEnabled(configuration));

        resumed.Purged.ShouldBe(1);
        store.Remaining.ShouldBe(0);
    }

    [Fact]
    public async Task ExistingReminder_DisabledAfterFirstDelete_PreservesRemainingDueRecord()
    {
        DateTimeOffset now = new(2026, 10, 11, 10, 0, 0, TimeSpan.Zero);
        IConfigurationRoot configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["AccessTelemetryLifecycle:Enabled"] = "true",
            })
            .Build();
        var store = new ScriptedRetentionStateStore(
            now.AddMinutes(-1),
            () => configuration["AccessTelemetryLifecycle:Enabled"] = "false");
        var options = new AccessTelemetryOptions();
        var actor = new AccessTelemetryLifecycleActor(
            ActorHost.CreateForTest<AccessTelemetryLifecycleActor>(
                new ActorTestOptions { ActorId = new ActorId("global") }),
            new AccessTelemetryLifecycleProcessor(store, new FakeTimeProvider(now), options),
            new FixedClockGate(now),
            null!,
            new AccessTelemetryRuntimeOptionsProvider(options),
            new FixedEvidenceProvider(),
            TimeProvider.System,
            configuration);

        // The test actor has no Dapr state provider, so post-purge bookkeeping fails.
        _ = await Should.ThrowAsync<InvalidOperationException>(() =>
            actor.ReceiveReminderAsync("bounded-purge", [], TimeSpan.Zero, TimeSpan.FromMinutes(1)));

        AccessTelemetryLifecycleActor.IsPurgeEnabled(configuration).ShouldBeFalse();
        store.Remaining.ShouldBe(1);
    }

    private sealed class DueRecordStore(AccessTelemetryExpiryEntry due) : IAccessTelemetryStateStore
    {
        public int DeleteCount { get; private set; }

        public Task<AccessTelemetryStoreWriteStatus> WriteRecordAndIndexAsync(
            AccessTelemetryRecord record,
            AccessTelemetryExpiryEntry expiryEntry,
            int ttlInSeconds,
            CancellationToken cancellationToken) => throw new NotSupportedException();

        public Task<(IReadOnlyList<AccessTelemetryExpiryEntry> Entries, bool HasMoreDueEntries)> GetDueEntriesAsync(
            long dueMinute,
            int limit,
            CancellationToken cancellationToken)
            => Task.FromResult<(IReadOnlyList<AccessTelemetryExpiryEntry>, bool)>(DeleteCount == 0 ? ([due], false) : ([], false));

        public Task<AccessTelemetryDeleteStatus> DeleteAndVerifyAsync(
            AccessTelemetryExpiryEntry entry,
            CancellationToken cancellationToken)
        {
            entry.ShouldBe(due);
            DeleteCount++;
            return Task.FromResult(AccessTelemetryDeleteStatus.Deleted);
        }
    }

    private sealed class FixedEvidenceProvider : ILifecycleClockEvidenceProvider
    {
        public Task<LifecycleClockEvidence> GetAsync(CancellationToken cancellationToken)
            => Task.FromResult(new LifecycleClockEvidence(null!, "process", "instance"));
    }

    private sealed class FixedClockGate(DateTimeOffset now) : IAccessTelemetryClockGate
    {
        public ClockAttestationValidationResult Validate(
            SignedClockAttestation attestation,
            string expectedAppId,
            string expectedProcessEpoch,
            string expectedServiceInstanceId)
            => new(true, AccessTelemetryReason.None, now.ToUnixTimeMilliseconds());
    }
}
