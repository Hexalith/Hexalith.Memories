// <copyright file="DaprObservedEventTypeStoreTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.EventStore.Tests;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Dapr.Client;

using Hexalith.Memories.EventStore;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

using NSubstitute;

using Shouldly;

/// <summary>spec-infrastructure-dependency-abstraction (F6, ADR-IDA-001) — unit tests for the Dapr-state
/// migration of the observed-event-type store. Covers idempotent/duplicate, late/out-of-order writes,
/// in-memory window filtering, the ETag-CAS cardinality cap + warning 9142, fail-open posture, input
/// validation, and the pinned cap/TTL constants. Real cross-key atomicity and TTL expiry are Tier-2
/// integration concerns.</summary>
public sealed class DaprObservedEventTypeStoreTests
{
    private const string Store = FakeDaprStateStore.StoreName;
    private static readonly DateTimeOffset Now = DateTimeOffset.FromUnixTimeMilliseconds(1_700_000_000_000);

    [Fact]
    public void AggregatesIndexCardinalityCap_ShouldBePinnedAt1024()
        => DaprObservedEventTypeStore.AggregatesIndexCardinalityCap.ShouldBe(1024L);

    [Fact]
    public void KeyTtl_ShouldBeTwiceWindow()
        => DaprObservedEventTypeStore.KeyTtl.ShouldBe(TimeSpan.FromHours(48));

    [Fact]
    public async Task RecordObservationAsync_WithRejectedTenantTag_ShouldThrowArgumentException()
    {
        DaprObservedEventTypeStore store = CreateStore(new FakeDaprStateStore());

        ArgumentException ex = await Should.ThrowAsync<ArgumentException>(() =>
            store.RecordObservationAsync("__rejected__", "Claims", "ClaimSubmittedV2", Now, CancellationToken.None));

        ex.Message.ShouldContain("__rejected__");
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("\t")]
    public async Task RecordObservationAsync_WithEmptyTenantId_ShouldThrowArgumentException(string tenantId)
    {
        DaprObservedEventTypeStore store = CreateStore(new FakeDaprStateStore());

        _ = await Should.ThrowAsync<ArgumentException>(() =>
            store.RecordObservationAsync(tenantId, "Claims", "ClaimSubmittedV2", Now, CancellationToken.None));
    }

    [Fact]
    public async Task RecordObservationAsync_HappyPath_RecordsAndIndexes()
    {
        FakeDaprStateStore fake = new();
        DaprObservedEventTypeStore store = CreateStore(fake);

        await store.RecordObservationAsync("acme", "Claims", "ClaimSubmittedV2", Now, CancellationToken.None);

        IReadOnlyList<ObservedEventType> observed =
            await store.GetObservedTypesAsync("acme", "Claims", TimeSpan.FromHours(24), CancellationToken.None);

        observed.Count.ShouldBe(1);
        observed[0].EventType.ShouldBe("ClaimSubmittedV2");
        observed[0].Count.ShouldBe(1);

        IReadOnlyList<ObservedEventType> all =
            await store.GetAllObservedTypesAsync("acme", TimeSpan.FromHours(24), CancellationToken.None);
        all.Count.ShouldBe(1);
    }

    [Fact]
    public async Task RecordObservationAsync_Duplicate_IncrementsCount()
    {
        FakeDaprStateStore fake = new();
        DaprObservedEventTypeStore store = CreateStore(fake);

        await store.RecordObservationAsync("acme", "Claims", "ClaimSubmittedV2", Now, CancellationToken.None);
        await store.RecordObservationAsync("acme", "Claims", "ClaimSubmittedV2", Now, CancellationToken.None);

        IReadOnlyList<ObservedEventType> observed =
            await store.GetObservedTypesAsync("acme", "Claims", TimeSpan.FromHours(24), CancellationToken.None);
        observed[0].Count.ShouldBe(2);
    }

    [Fact]
    public async Task RecordObservationAsync_LateOutOfOrderWrite_KeepsMaxLastSeen()
    {
        FakeDaprStateStore fake = new();
        DaprObservedEventTypeStore store = CreateStore(fake);
        DateTimeOffset newer = Now;
        DateTimeOffset older = Now.AddMinutes(-30);

        // Newer observation first, then a late (older) delivery: lastSeenAt must stay at the newer time.
        await store.RecordObservationAsync("acme", "Claims", "ClaimSubmittedV2", newer, CancellationToken.None);
        await store.RecordObservationAsync("acme", "Claims", "ClaimSubmittedV2", older, CancellationToken.None);

        IReadOnlyList<ObservedEventType> observed =
            await store.GetObservedTypesAsync("acme", "Claims", TimeSpan.FromHours(24), CancellationToken.None);
        observed[0].Count.ShouldBe(2);
        observed[0].LastSeenAt.ShouldBe(newer);
    }

    [Fact]
    public async Task GetObservedTypesAsync_ExcludesObservationsOutsideWindow()
    {
        FakeDaprStateStore fake = new();
        DaprObservedEventTypeStore store = CreateStore(fake);
        // Observed 90 minutes ago; a 60-minute window must exclude it.
        await store.RecordObservationAsync("acme", "Claims", "OldEventV1", Now.AddMinutes(-90), CancellationToken.None);
        await store.RecordObservationAsync("acme", "Claims", "FreshEventV1", Now.AddMinutes(-5), CancellationToken.None);

        IReadOnlyList<ObservedEventType> observed =
            await store.GetObservedTypesAsync("acme", "Claims", TimeSpan.FromMinutes(60), CancellationToken.None);

        observed.Count.ShouldBe(1);
        observed[0].EventType.ShouldBe("FreshEventV1");
    }

    [Fact]
    public async Task GetObservedTypesAsync_OrdersMostRecentFirst()
    {
        FakeDaprStateStore fake = new();
        DaprObservedEventTypeStore store = CreateStore(fake);
        await store.RecordObservationAsync("acme", "Claims", "First", Now.AddMinutes(-20), CancellationToken.None);
        await store.RecordObservationAsync("acme", "Claims", "Second", Now.AddMinutes(-5), CancellationToken.None);

        IReadOnlyList<ObservedEventType> observed =
            await store.GetObservedTypesAsync("acme", "Claims", TimeSpan.FromHours(1), CancellationToken.None);

        observed[0].EventType.ShouldBe("Second");
        observed[1].EventType.ShouldBe("First");
    }

    [Fact]
    public async Task GetObservedTypesAsync_WithNoState_ReturnsEmpty()
    {
        DaprObservedEventTypeStore store = CreateStore(new FakeDaprStateStore());

        (await store.GetObservedTypesAsync("acme", "Claims", TimeSpan.FromHours(24), CancellationToken.None))
            .ShouldBeEmpty();
    }

    [Fact]
    public async Task GetAllObservedTypesAsync_WithNoIndex_ReturnsEmpty()
    {
        DaprObservedEventTypeStore store = CreateStore(new FakeDaprStateStore());

        (await store.GetAllObservedTypesAsync("acme", TimeSpan.FromHours(24), CancellationToken.None))
            .ShouldBeEmpty();
    }

    [Fact]
    public async Task RecordObservationAsync_WhenHardCapRejectsNewAggregate_ShouldEmitWarning9142_ButStillRecordObservation()
    {
        FakeDaprStateStore fake = new();
        List<string> fullIndex = [];
        for (int i = 0; i < 1024; i++)
        {
            fullIndex.Add($"Aggregate{i}");
        }

        fake.Seed("acme:eventstore:observed-aggregates", fullIndex);

        List<(LogLevel Level, int EventId)> captures = [];
        DaprObservedEventTypeStore store = CreateStore(fake, new CapturingTestLogger(captures));

        await store.RecordObservationAsync("acme", "OverflowAggregate", "SomeEventV1", Now, CancellationToken.None);

        captures.ShouldContain(c => c.EventId == 9142 && c.Level == LogLevel.Warning);
        // The index stays capped (new aggregate not admitted) ...
        fake.TryGet("acme:eventstore:observed-aggregates", out List<string>? index).ShouldBeTrue();
        index!.Count.ShouldBe(1024);
        index.ShouldNotContain("OverflowAggregate");
        // ... but the observation data itself is still recorded (cap bounds the index, not the data).
        IReadOnlyList<ObservedEventType> observed =
            await store.GetObservedTypesAsync("acme", "OverflowAggregate", TimeSpan.FromHours(24), CancellationToken.None);
        observed.Count.ShouldBe(1);
    }

    [Fact]
    public async Task RecordObservationAsync_AtCapButAggregateAlreadyKnown_ShouldNotEmitWarning9142()
    {
        FakeDaprStateStore fake = new();
        List<string> fullIndex = ["KnownAggregate"];
        for (int i = 0; i < 1023; i++)
        {
            fullIndex.Add($"Aggregate{i}");
        }

        fake.Seed("acme:eventstore:observed-aggregates", fullIndex);

        List<(LogLevel Level, int EventId)> captures = [];
        DaprObservedEventTypeStore store = CreateStore(fake, new CapturingTestLogger(captures));

        await store.RecordObservationAsync("acme", "KnownAggregate", "SomeEventV1", Now, CancellationToken.None);

        captures.ShouldNotContain(c => c.EventId == 9142);
    }

    [Fact]
    public async Task RecordObservationAsync_OnDaprException_ShouldFailOpen()
    {
        DaprClient client = Substitute.For<DaprClient>();
        client.GetStateAndETagAsync<Dictionary<string, DaprObservedEventTypeStore.ObservationCounter>?>(
                Arg.Any<string>(), Arg.Any<string>(), Arg.Any<ConsistencyMode?>(),
                Arg.Any<IReadOnlyDictionary<string, string>?>(), Arg.Any<CancellationToken>())
            .Returns<(Dictionary<string, DaprObservedEventTypeStore.ObservationCounter>?, string)>(
                _ => throw new Dapr.DaprException("state store unavailable"));
        DaprObservedEventTypeStore store = CreateStore(client);

        await Should.NotThrowAsync(() =>
            store.RecordObservationAsync("acme", "Claims", "ClaimSubmittedV2", Now, CancellationToken.None));
    }

    [Fact]
    public async Task RecordObservationAsync_OnTimeoutException_ShouldFailOpen()
    {
        DaprClient client = Substitute.For<DaprClient>();
        client.GetStateAndETagAsync<Dictionary<string, DaprObservedEventTypeStore.ObservationCounter>?>(
                Arg.Any<string>(), Arg.Any<string>(), Arg.Any<ConsistencyMode?>(),
                Arg.Any<IReadOnlyDictionary<string, string>?>(), Arg.Any<CancellationToken>())
            .Returns<(Dictionary<string, DaprObservedEventTypeStore.ObservationCounter>?, string)>(
                _ => throw new TimeoutException("state store timeout"));
        DaprObservedEventTypeStore store = CreateStore(client);

        await Should.NotThrowAsync(() =>
            store.RecordObservationAsync("acme", "Claims", "ClaimSubmittedV2", Now, CancellationToken.None));
    }

    [Fact]
    public async Task RecordObservationAsync_OnJsonException_ShouldFailOpen()
    {
        DaprClient client = Substitute.For<DaprClient>();
        client.GetStateAndETagAsync<Dictionary<string, DaprObservedEventTypeStore.ObservationCounter>?>(
                Arg.Any<string>(), Arg.Any<string>(), Arg.Any<ConsistencyMode?>(),
                Arg.Any<IReadOnlyDictionary<string, string>?>(), Arg.Any<CancellationToken>())
            .Returns<(Dictionary<string, DaprObservedEventTypeStore.ObservationCounter>?, string)>(
                _ => throw new System.Text.Json.JsonException("corrupt state"));
        DaprObservedEventTypeStore store = CreateStore(client);

        await Should.NotThrowAsync(() =>
            store.RecordObservationAsync("acme", "Claims", "ClaimSubmittedV2", Now, CancellationToken.None));
    }

    [Fact]
    public async Task RecordObservationAsync_OnSidecarShutdownCancellation_ShouldFailOpen()
    {
        DaprClient client = Substitute.For<DaprClient>();
        client.GetStateAndETagAsync<Dictionary<string, DaprObservedEventTypeStore.ObservationCounter>?>(
                Arg.Any<string>(), Arg.Any<string>(), Arg.Any<ConsistencyMode?>(),
                Arg.Any<IReadOnlyDictionary<string, string>?>(), Arg.Any<CancellationToken>())
            .Returns<(Dictionary<string, DaprObservedEventTypeStore.ObservationCounter>?, string)>(
                _ => throw new OperationCanceledException("sidecar shutting down"));
        DaprObservedEventTypeStore store = CreateStore(client);

        await Should.NotThrowAsync(() =>
            store.RecordObservationAsync("acme", "Claims", "ClaimSubmittedV2", Now, CancellationToken.None));
    }

    [Fact]
    public async Task DeleteAllTenantDataAsync_RemovesCapRejectedObservationKeys()
    {
        FakeDaprStateStore fake = new();
        List<string> fullIndex = [];
        for (int i = 0; i < 1024; i++)
        {
            fullIndex.Add($"Aggregate{i}");
        }

        fake.Seed("acme:eventstore:observed-aggregates", fullIndex);
        DaprObservedEventTypeStore store = CreateStore(fake);
        await store.RecordObservationAsync("acme", "OverflowAggregate", "SomeEventV1", Now, CancellationToken.None);

        await store.DeleteAllTenantDataAsync("acme", CancellationToken.None);

        fake.ContainsKey("acme:eventstore:observed:OverflowAggregate").ShouldBeFalse();
        fake.ContainsKey("acme:eventstore:observed-aggregates").ShouldBeFalse();
        fake.ContainsKey("acme:eventstore:observed-written-aggregates").ShouldBeFalse();
        (await store.GetObservedTypesAsync("acme", "OverflowAggregate", TimeSpan.FromHours(24), CancellationToken.None))
            .ShouldBeEmpty();
    }

    [Fact]
    public async Task RecordObservationAsync_WhenWrittenIndexCasExhausted_CompensatesObservationAndFailsLoud()
    {
        // review patch #4: written-index miss would leave purge-invisible observation keys — compensate + throw.
        FakeDaprStateStore fake = new();
        fake.FailNextSaves("acme:eventstore:observed-written-aggregates", count: 16);
        List<(LogLevel Level, int EventId)> captures = [];
        DaprObservedEventTypeStore store = CreateStore(fake, new CapturingTestLogger(captures));

        InvalidOperationException ex = await Should.ThrowAsync<InvalidOperationException>(() =>
            store.RecordObservationAsync("acme", "Claims", "ClaimSubmittedV2", Now, CancellationToken.None));

        ex.Data["WrittenIndexCasExhausted"].ShouldBe(true);
        fake.ContainsKey("acme:eventstore:observed:Claims").ShouldBeFalse();
        captures.ShouldContain(c => c.Level == LogLevel.Warning || c.Level == LogLevel.Error || c.Level == LogLevel.Information);
    }

    [Fact]
    public async Task RecordObservationAsync_WhenDiscoveryIndexCasExhausted_DoesNotClaimSuccess()
    {
        // review patch #5: discovery CAS exhaustion must log write-failed and skip the success log.
        FakeDaprStateStore fake = new();
        fake.FailNextSaves("acme:eventstore:observed-aggregates", count: 16);
        List<(LogLevel Level, int EventId)> captures = [];
        DaprObservedEventTypeStore store = CreateStore(fake, new CapturingTestLogger(captures));

        await Should.NotThrowAsync(() =>
            store.RecordObservationAsync("acme", "Claims", "ClaimSubmittedV2", Now, CancellationToken.None));

        // Observation + written index remain (purge-safe); discovery not claimed.
        fake.ContainsKey("acme:eventstore:observed:Claims").ShouldBeTrue();
        fake.ContainsKey("acme:eventstore:observed-written-aggregates").ShouldBeTrue();
        captures.ShouldContain(c => c.EventId == 9140); // ObservedEventTypeStoreWriteFailed
        captures.ShouldNotContain(c => c.EventId == 9130); // ObservedEventTypeRecorded
    }

    [Fact]
    public async Task RecordObservationAsync_OnArgumentOutOfRangeException_ShouldFailOpen()
    {
        // review patch #17: unix-ms conversion / corrupt timestamps must not escape fail-open.
        DaprClient client = Substitute.For<DaprClient>();
        client.GetStateAndETagAsync<Dictionary<string, DaprObservedEventTypeStore.ObservationCounter>?>(
                Arg.Any<string>(), Arg.Any<string>(), Arg.Any<ConsistencyMode?>(),
                Arg.Any<IReadOnlyDictionary<string, string>?>(), Arg.Any<CancellationToken>())
            .Returns<(Dictionary<string, DaprObservedEventTypeStore.ObservationCounter>?, string)>(
                _ => throw new ArgumentOutOfRangeException("observedAt"));
        DaprObservedEventTypeStore store = CreateStore(client);

        await Should.NotThrowAsync(() =>
            store.RecordObservationAsync("acme", "Claims", "ClaimSubmittedV2", Now, CancellationToken.None));
    }

    [Fact]
    public async Task RecordObservationAsync_RepeatObservation_RefreshesIndexTtlMetadataPath()
    {
        // review patch #3: membership hit must still attempt TTL refresh saves (indexes + membership).
        FakeDaprStateStore fake = new();
        DaprObservedEventTypeStore store = CreateStore(fake);
        await store.RecordObservationAsync("acme", "Claims", "ClaimSubmittedV2", Now, CancellationToken.None);

        await store.RecordObservationAsync("acme", "Claims", "ClaimSubmittedV2", Now.AddMinutes(1), CancellationToken.None);

        fake.ContainsKey("acme:eventstore:observed-agg-member:Claims").ShouldBeTrue();
        fake.ContainsKey("acme:eventstore:observed-aggregates").ShouldBeTrue();
        fake.ContainsKey("acme:eventstore:observed-written-aggregates").ShouldBeTrue();
        IReadOnlyList<ObservedEventType> observed =
            await store.GetObservedTypesAsync("acme", "Claims", TimeSpan.FromHours(24), CancellationToken.None);
        observed[0].Count.ShouldBe(2);
    }

    [Fact]
    public async Task RecordObservationAsync_WhenDiscoveryIndexIsMissing_RecoversPreviouslyAdmittedAggregates()
    {
        const string discoveryKey = "acme:eventstore:observed-aggregates";
        FakeDaprStateStore fake = new();
        DaprClient client = fake.CreateClient();
        DaprObservedEventTypeStore store = CreateStore(client);
        await store.RecordObservationAsync("acme", "Claims", "ClaimSubmitted", Now, CancellationToken.None);
        await store.RecordObservationAsync("acme", "Orders", "OrderSubmitted", Now, CancellationToken.None);
        List<string> writtenAggregates =
        [
            "Claims",
            "Overflow",
            .. Enumerable.Range(0, 128).Select(i => $"Unmarked{i}"),
            "Orders",
        ];
        fake.Seed("acme:eventstore:observed-written-aggregates", writtenAggregates);
        fake.Seed("acme:eventstore:observed:Overflow",
            new Dictionary<string, DaprObservedEventTypeStore.ObservationCounter>
            {
                ["OverflowSubmitted"] = new(1, Now.ToUnixTimeMilliseconds()),
            });
        await client.DeleteStateAsync(Store, discoveryKey, cancellationToken: CancellationToken.None);

        await store.RecordObservationAsync("acme", "Claims", "ClaimSubmitted", Now, CancellationToken.None);

        fake.TryGet<List<string>>(discoveryKey, out List<string>? index).ShouldBeTrue();
        index.ShouldBe(["Claims", "Orders"]);
        IReadOnlyList<ObservedEventType> discovered =
            await store.GetAllObservedTypesAsync("acme", TimeSpan.FromHours(24), CancellationToken.None);
        discovered.Select(observation => observation.EventType).ShouldBe(["ClaimSubmitted", "OrderSubmitted"], ignoreOrder: true);
        discovered.Single(observation => observation.AggregateType == "Claims").Count.ShouldBe(2);
        fake.ContainsKey("acme:eventstore:observed:Overflow").ShouldBeTrue();
    }

    [Fact]
    public async Task RecordObservationAsync_WhenDiscoveryDisappearsDuringAdmission_UsesTheCurrentWrittenIndex()
    {
        const string discoveryKey = "acme:eventstore:observed-aggregates";
        const string writtenKey = "acme:eventstore:observed-written-aggregates";
        FakeDaprStateStore fake = new();
        DaprClient client = fake.CreateClient();
        DaprObservedEventTypeStore store = CreateStore(client);
        await store.RecordObservationAsync("acme", "Claims", "ClaimSubmitted", Now, CancellationToken.None);
        await store.RecordObservationAsync("acme", "Orders", "OrderSubmitted", Now, CancellationToken.None);
        fake.Seed(writtenKey, new List<string> { "Claims" });
        await client.DeleteStateAsync(Store, discoveryKey, cancellationToken: CancellationToken.None);
        client.When(c => c.GetStateAndETagAsync<List<string>?>(
                Store, discoveryKey, Arg.Any<ConsistencyMode?>(),
                Arg.Any<IReadOnlyDictionary<string, string>?>(), Arg.Any<CancellationToken>()))
            .Do(_ => fake.Seed(writtenKey, new List<string> { "Claims", "Orders" }));

        await store.RecordObservationAsync("acme", "Claims", "ClaimSubmitted", Now, CancellationToken.None);

        IReadOnlyList<ObservedEventType> discovered =
            await store.GetAllObservedTypesAsync("acme", TimeSpan.FromHours(24), CancellationToken.None);
        discovered.Select(observation => observation.AggregateType).ShouldBe(["Claims", "Orders"], ignoreOrder: true);
    }

    [Fact]
    public async Task RecordObservationAsync_WhenIndexTtlRefreshConflicts_RetriesBothIndexes()
    {
        const string discoveryKey = "acme:eventstore:observed-aggregates";
        const string writtenKey = "acme:eventstore:observed-written-aggregates";
        const string membershipKey = "acme:eventstore:observed-agg-member:Claims";
        FakeDaprStateStore fake = new();
        DaprClient client = fake.CreateClient();
        DaprObservedEventTypeStore store = CreateStore(client);
        await store.RecordObservationAsync("acme", "Claims", "ClaimSubmitted", Now, CancellationToken.None);
        client.ClearReceivedCalls();
        fake.FailNextSaves(discoveryKey, count: 1);
        fake.FailNextSaves(writtenKey, count: 1);
        fake.FailNextSaves(membershipKey, count: 1);

        await store.RecordObservationAsync("acme", "Claims", "ClaimSubmitted", Now, CancellationToken.None);

        foreach (string key in new[] { writtenKey, discoveryKey })
        {
            _ = client.Received(2).TrySaveStateAsync(
                Store, key, Arg.Any<List<string>>(), Arg.Any<string>(), Arg.Any<StateOptions?>(),
                Arg.Is<IReadOnlyDictionary<string, string>?>(metadata => metadata != null && metadata["ttlInSeconds"] == "172800"),
                Arg.Any<CancellationToken>());
        }

        _ = client.Received(2).TrySaveStateAsync(
            Store, membershipKey, "1", Arg.Any<string>(), Arg.Any<StateOptions?>(),
            Arg.Is<IReadOnlyDictionary<string, string>?>(metadata => metadata != null && metadata["ttlInSeconds"] == "172800"),
            Arg.Any<CancellationToken>());

        IReadOnlyList<ObservedEventType> discovered =
            await store.GetAllObservedTypesAsync("acme", TimeSpan.FromHours(24), CancellationToken.None);
        discovered.Single().Count.ShouldBe(2);
    }

    [Fact]
    public async Task RecordObservationAsync_WhenWrittenIndexRefreshExhaustsRetries_PreservesIndexedObservations()
    {
        FakeDaprStateStore fake = new();
        List<(LogLevel Level, int EventId)> captures = [];
        DaprObservedEventTypeStore store = CreateStore(fake, new CapturingTestLogger(captures));
        await store.RecordObservationAsync("acme", "Claims", "ClaimSubmitted", Now, CancellationToken.None);
        captures.Clear();
        fake.FailNextSaves("acme:eventstore:observed-written-aggregates", count: 16);

        InvalidOperationException exception = await Should.ThrowAsync<InvalidOperationException>(() =>
            store.RecordObservationAsync("acme", "Claims", "ClaimSubmitted", Now, CancellationToken.None));

        exception.Data["WrittenIndexCasExhausted"].ShouldBe(true);
        IReadOnlyList<ObservedEventType> discovered =
            await store.GetAllObservedTypesAsync("acme", TimeSpan.FromHours(24), CancellationToken.None);
        discovered.Single().Count.ShouldBe(2);
        captures.ShouldContain(entry => entry.EventId == 9140);
        captures.ShouldNotContain(entry => entry.EventId == 9130);
    }

    private static DaprObservedEventTypeStore CreateStore(FakeDaprStateStore fake, ILogger<DaprObservedEventTypeStore>? logger = null)
        => CreateStore(fake.CreateClient(), logger);

    private static DaprObservedEventTypeStore CreateStore(DaprClient client, ILogger<DaprObservedEventTypeStore>? logger = null)
        => new(
            client,
            Options.Create(new EventStoreStateStoreOptions { StateStoreName = Store }),
            new FixedTimeProvider(Now),
            logger ?? NullLogger<DaprObservedEventTypeStore>.Instance);

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        private readonly DateTimeOffset _now = now;

        public override DateTimeOffset GetUtcNow() => _now;
    }

    private sealed class CapturingTestLogger(List<(LogLevel Level, int EventId)> captures)
        : ILogger<DaprObservedEventTypeStore>
    {
        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter) => captures.Add((logLevel, eventId.Id));
    }
}
