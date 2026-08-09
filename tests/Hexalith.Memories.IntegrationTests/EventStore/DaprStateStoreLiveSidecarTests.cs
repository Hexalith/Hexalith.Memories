// <copyright file="DaprStateStoreLiveSidecarTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.IntegrationTests.EventStore;

using Dapr;
using Dapr.Client;

using Hexalith.Memories.EventStore;
using Hexalith.Memories.IntegrationTests.Fixtures;

using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;

using Shouldly;

/// <summary>spec-infrastructure-dependency-abstraction A4 — Tier-2 live-sidecar proofs for
/// <see cref="DaprAggregateCaseMappingStore"/> and <see cref="DaprObservedEventTypeStore"/> against a
/// real Dapr <c>statestore</c> (not <c>FakeDaprStateStore</c>): FirstWrite / first-writer-wins, ETag
/// concurrency, and TTL metadata honoring.</summary>
[Collection("DaprStateSidecar")]
[Trait("Category", "Integration")]
[Trait("Category", "LiveSidecar")]
[Trait("Tier", "2")]
public sealed class DaprStateStoreLiveSidecarTests
{
    private readonly DaprStateSidecarFixture _fixture;

    public DaprStateStoreLiveSidecarTests(DaprStateSidecarFixture fixture) => _fixture = fixture;

    [Fact]
    public async Task AggregateCaseMapping_FirstWrite_IsFirstWriterWinsAgainstLiveStatestore()
    {
        DaprAggregateCaseMappingStore store = CreateMappingStore();
        string tenantId = $"ida-map-{Guid.NewGuid():N}"[..28];

        (await store.TryStoreCaseIdAsync(tenantId, "Claims", "case-winner", CancellationToken.None))
            .ShouldBeTrue();
        (await store.TryStoreCaseIdAsync(tenantId, "Claims", "case-loser", CancellationToken.None))
            .ShouldBeFalse();

        (await store.GetCaseIdAsync(tenantId, "Claims", CancellationToken.None)).ShouldBe("case-winner");
        (await store.GetAggregateCountAsync(tenantId, CancellationToken.None)).ShouldBe(1);

        // Unrelated aggregate types must not contend (per-key FirstWrite redesign).
        (await store.TryStoreCaseIdAsync(tenantId, "Orders", "case-orders", CancellationToken.None))
            .ShouldBeTrue();
        (await store.GetAggregateCountAsync(tenantId, CancellationToken.None)).ShouldBe(2);
    }

    [Fact]
    public async Task AggregateCaseMapping_CreationLock_FirstWriteIsExclusiveAgainstLiveStatestore()
    {
        DaprAggregateCaseMappingStore store = CreateMappingStore();
        string tenantId = $"ida-lock-{Guid.NewGuid():N}"[..28];

        bool first = await store.TryAcquireCreationLockAsync(
            tenantId, "Claims", TimeSpan.FromSeconds(30), CancellationToken.None);
        bool second = await store.TryAcquireCreationLockAsync(
            tenantId, "Claims", TimeSpan.FromSeconds(30), CancellationToken.None);

        first.ShouldBeTrue();
        second.ShouldBeFalse();

        await store.ReleaseCreationLockAsync(tenantId, "Claims", CancellationToken.None);
        bool reacquired = await store.TryAcquireCreationLockAsync(
            tenantId, "Claims", TimeSpan.FromSeconds(30), CancellationToken.None);
        reacquired.ShouldBeTrue();
    }

    [Fact]
    public async Task ObservedEventType_DuplicateAndLateWrites_AreSafeAgainstLiveStatestore()
    {
        FakeTimeProvider clock = new(DateTimeOffset.UtcNow);
        DaprObservedEventTypeStore store = CreateObservedStore(clock);
        string tenantId = $"ida-obs-{Guid.NewGuid():N}"[..28];
        DateTimeOffset newer = clock.GetUtcNow();
        DateTimeOffset older = newer.AddMinutes(-15);

        await store.RecordObservationAsync(tenantId, "Claims", "ClaimSubmittedV2", newer, CancellationToken.None);
        await store.RecordObservationAsync(tenantId, "Claims", "ClaimSubmittedV2", newer, CancellationToken.None);
        await store.RecordObservationAsync(tenantId, "Claims", "ClaimSubmittedV2", older, CancellationToken.None);

        IReadOnlyList<ObservedEventType> observed = await store.GetObservedTypesAsync(
            tenantId, "Claims", TimeSpan.FromHours(1), CancellationToken.None);

        observed.Count.ShouldBe(1);
        observed[0].Count.ShouldBe(3);
        observed[0].LastSeenAt.ToUnixTimeMilliseconds().ShouldBe(newer.ToUnixTimeMilliseconds());
    }

    [Fact]
    public async Task ObservedEventType_TtlMetadata_ExpiresKeyAgainstLiveRedisStatestore()
    {
        // Directly prove the Redis state component honors ttlInSeconds (the store's write path uses it).
        string key = $"ida-ttl-{Guid.NewGuid():N}";
        await _fixture.DaprClient.SaveStateAsync(
            _fixture.StateStoreName,
            key,
            "ephemeral",
            metadata: new Dictionary<string, string> { ["ttlInSeconds"] = "1" },
            cancellationToken: CancellationToken.None);

        (await _fixture.DaprClient.GetStateAsync<string>(_fixture.StateStoreName, key))
            .ShouldBe("ephemeral");

        await Task.Delay(TimeSpan.FromSeconds(3));

        string? afterTtl = await _fixture.DaprClient.GetStateAsync<string>(_fixture.StateStoreName, key);
        afterTtl.ShouldBeNull();
    }

    [Fact]
    public async Task AggregateCaseMapping_CreationLock_StoreWritePathHonorsTtlAgainstLiveStatestore()
    {
        // review patch #12: store write path (TryAcquireCreationLockAsync TTL metadata) expires on live Redis.
        DaprAggregateCaseMappingStore store = CreateMappingStore();
        string tenantId = $"ida-sttl-{Guid.NewGuid():N}"[..28];

        (await store.TryAcquireCreationLockAsync(tenantId, "Claims", TimeSpan.FromSeconds(1), CancellationToken.None))
            .ShouldBeTrue();
        (await store.TryAcquireCreationLockAsync(tenantId, "Claims", TimeSpan.FromSeconds(1), CancellationToken.None))
            .ShouldBeFalse();

        await Task.Delay(TimeSpan.FromSeconds(3));

        (await store.TryAcquireCreationLockAsync(tenantId, "Claims", TimeSpan.FromSeconds(30), CancellationToken.None))
            .ShouldBeTrue();
    }

    [Fact]
    public async Task Stores_DeleteAllTenantDataAsync_PurgesLiveSidecarKeysForTenantOnly()
    {
        // review patch #12: real-sidecar DeleteAllTenantDataAsync against store-written keys.
        FakeTimeProvider clock = new(DateTimeOffset.UtcNow);
        DaprAggregateCaseMappingStore mapping = CreateMappingStore();
        DaprObservedEventTypeStore observed = CreateObservedStore(clock);
        string tenantA = $"ida-pa-{Guid.NewGuid():N}"[..28];
        string tenantB = $"ida-pb-{Guid.NewGuid():N}"[..28];

        (await mapping.TryStoreCaseIdAsync(tenantA, "Claims", "case-a", CancellationToken.None)).ShouldBeTrue();
        (await mapping.TryStoreCaseIdAsync(tenantB, "Claims", "case-b", CancellationToken.None)).ShouldBeTrue();
        await observed.RecordObservationAsync(tenantA, "Claims", "ClaimSubmittedV2", clock.GetUtcNow(), CancellationToken.None);
        await observed.RecordObservationAsync(tenantB, "Claims", "ClaimSubmittedV2", clock.GetUtcNow(), CancellationToken.None);

        await mapping.DeleteAllTenantDataAsync(tenantA, CancellationToken.None);
        await observed.DeleteAllTenantDataAsync(tenantA, CancellationToken.None);

        (await mapping.GetCaseIdAsync(tenantA, "Claims", CancellationToken.None)).ShouldBeNull();
        (await mapping.GetCaseIdAsync(tenantB, "Claims", CancellationToken.None)).ShouldBe("case-b");
        (await observed.GetObservedTypesAsync(tenantA, "Claims", TimeSpan.FromHours(1), CancellationToken.None))
            .ShouldBeEmpty();
        (await observed.GetObservedTypesAsync(tenantB, "Claims", TimeSpan.FromHours(1), CancellationToken.None))
            .Count.ShouldBe(1);
    }

    [Fact]
    public async Task AggregateCaseMapping_ETagFirstWrite_RejectsLostUpdateAgainstLiveStatestore()
    {
        // Raw Dapr API proof that FirstWrite + ETag CAS is enforced by the real component — the
        // store's TryStoreCaseIdAsync / lock path depends on this contract. Redis state.redis may
        // return false OR throw DaprException on FirstWrite conflict; either must leave v1 intact.
        string key = $"ida-cas-{Guid.NewGuid():N}";
        bool first = await _fixture.DaprClient.TrySaveStateAsync(
            _fixture.StateStoreName,
            key,
            "v1",
            etag: string.Empty,
            new StateOptions { Concurrency = ConcurrencyMode.FirstWrite },
            cancellationToken: CancellationToken.None);
        first.ShouldBeTrue();

        bool secondSucceeded = true;
        try
        {
            secondSucceeded = await _fixture.DaprClient.TrySaveStateAsync(
                _fixture.StateStoreName,
                key,
                "v2",
                etag: string.Empty,
                new StateOptions { Concurrency = ConcurrencyMode.FirstWrite },
                cancellationToken: CancellationToken.None);
        }
        catch (DaprException)
        {
            secondSucceeded = false;
        }

        secondSucceeded.ShouldBeFalse();
        (await _fixture.DaprClient.GetStateAsync<string>(_fixture.StateStoreName, key)).ShouldBe("v1");
    }

    private DaprAggregateCaseMappingStore CreateMappingStore()
        => new(
            _fixture.DaprClient,
            Options.Create(new EventStoreStateStoreOptions { StateStoreName = _fixture.StateStoreName }));

    private DaprObservedEventTypeStore CreateObservedStore(TimeProvider clock)
        => new(
            _fixture.DaprClient,
            Options.Create(new EventStoreStateStoreOptions { StateStoreName = _fixture.StateStoreName }),
            clock,
            NullLogger<DaprObservedEventTypeStore>.Instance);
}
