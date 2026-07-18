// <copyright file="DaprObservedEventTypeStoreTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.EventStore.Tests;

using System;
using System.Collections.Generic;
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
