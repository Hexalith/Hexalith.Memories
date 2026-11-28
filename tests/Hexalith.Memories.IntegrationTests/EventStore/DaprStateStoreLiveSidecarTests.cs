// <copyright file="DaprStateStoreLiveSidecarTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.IntegrationTests.EventStore;

using Dapr;
using Dapr.Client;

using Grpc.Core;

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

    [Theory]
    [InlineData(true, "Orders")]
    [InlineData(false, "Claims")]
    public async Task AggregateCaseMapping_MixedVersionIndexCreation_DependsOnWhichWriterCreatesTheRowAsync(
        bool legacyWritesFirst, string expectedAggregateType)
    {
        DaprClient client = _fixture.DaprClient;
        string storeName = _fixture.StateStoreName;
        string indexKey = $"ida-mixed-{Guid.NewGuid():N}:eventstore:aggregate-case-map-index";
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        StateOptions firstWrite = new() { Concurrency = ConcurrencyMode.FirstWrite };

        // Both writers read before either saves. The legacy writer omitted the options on an absent index.
        (List<string>? absentIndex, string absentEtag) = await client.GetStateAndETagAsync<List<string>?>(
            storeName, indexKey, cancellationToken: cancellationToken);
        absentIndex.ShouldBeNull();
        absentEtag.ShouldBeEmpty();
        (await client.TrySaveStateAsync<List<string>>(
            storeName, indexKey, ["Claims"], absentEtag, legacyWritesFirst ? null : firstWrite,
            cancellationToken: cancellationToken)).ShouldBeTrue();
        (await client.GetStateAsync<List<string>>(storeName, indexKey, cancellationToken: cancellationToken))
            .ShouldBe(["Claims"]);

        // A stale FirstWrite request can overwrite an unmarked row. A marked row rejects an empty ETag.
        bool secondSaved;
        try
        {
            secondSaved = await client.TrySaveStateAsync<List<string>>(
                storeName, indexKey, ["Orders"], absentEtag, legacyWritesFirst ? firstWrite : null,
                cancellationToken: cancellationToken);
        }
        catch (DaprException ex) when (ex.InnerException is RpcException { StatusCode: StatusCode.Internal } rpc
            && rpc.Status.Detail.Contains($"failed to set key {_fixture.AppId}||{indexKey} script:", StringComparison.Ordinal))
        {
            // The pinned Redis component surfaces this empty-ETag conflict as a Lua script error.
            secondSaved = false;
        }

        secondSaved.ShouldBe(legacyWritesFirst);
        (await client.GetStateAsync<List<string>>(storeName, indexKey, cancellationToken: cancellationToken))
            .ShouldBe([expectedAggregateType]);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task AggregateCaseMapping_ExistingIndex_PreservesMembershipAndRejectsStaleETagAsync(bool firstWriteCreation)
    {
        DaprClient client = _fixture.DaprClient;
        string storeName = _fixture.StateStoreName;
        string tenantId = $"ida-etag-{Guid.NewGuid():N}"[..28];
        string indexKey = $"{tenantId}:eventstore:aggregate-case-map-index";
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        StateOptions? creationOptions = firstWriteCreation ? new() { Concurrency = ConcurrencyMode.FirstWrite } : null;

        (await client.TrySaveStateAsync<List<string>>(
            storeName, indexKey, ["Claims"], string.Empty, creationOptions, cancellationToken: cancellationToken))
            .ShouldBeTrue();
        (List<string> originalIndex, string originalEtag) = await client.GetStateAndETagAsync<List<string>>(
            storeName, indexKey, cancellationToken: cancellationToken);
        originalIndex.ShouldBe(["Claims"]);
        originalEtag.ShouldNotBeNullOrEmpty();

        DaprAggregateCaseMappingStore store = CreateMappingStore();
        (await store.TryStoreCaseIdAsync(tenantId, "Orders", "case-orders", cancellationToken)).ShouldBeTrue();
        (await client.TrySaveStateAsync<List<string>>(
            storeName, indexKey, ["Claims", "Invoices"], originalEtag, cancellationToken: cancellationToken))
            .ShouldBeFalse();

        (List<string> persistedIndex, string persistedEtag) = await client.GetStateAndETagAsync<List<string>>(
            storeName, indexKey, cancellationToken: cancellationToken);
        persistedIndex.ShouldBe(["Claims", "Orders"]);
        persistedEtag.ShouldNotBeNullOrEmpty();
        persistedEtag.ShouldNotBe(originalEtag);
    }

    [Fact]
    public async Task AggregateCaseMapping_CaseDeletionPrunesIndexAndAllowsRemap()
    {
        DaprAggregateCaseMappingStore store = CreateMappingStore();
        string tenantId = $"ida-cd-{Guid.NewGuid():N}"[..28];
        string mapKey = $"{tenantId}:eventstore:aggregate-case-map:Claims";
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        (await store.TryStoreCaseIdAsync(tenantId, "Claims", "case-delete", cancellationToken)).ShouldBeTrue();
        (await store.TryStoreCaseIdAsync(tenantId, "Orders", "case-keep", cancellationToken)).ShouldBeTrue();
        (await store.DeleteCaseMappingsAsync(tenantId, "case-delete", cancellationToken)).ShouldBe(1);
        (await store.GetCaseIdAsync(tenantId, "Claims", cancellationToken)).ShouldBeNull();
        (await store.GetCaseIdAsync(tenantId, "Orders", cancellationToken)).ShouldBe("case-keep");
        (await store.GetAggregateCountAsync(tenantId, cancellationToken)).ShouldBe(1);
        (await store.DeleteCaseMappingsAsync(tenantId, "case-delete", cancellationToken)).ShouldBe(0);

        (string? missing, string missingEtag) = await _fixture.DaprClient.GetStateAndETagAsync<string>(
            _fixture.StateStoreName, mapKey, cancellationToken: cancellationToken);
        missing.ShouldBeNull();
        missingEtag.ShouldBeEmpty();
        (await store.TryStoreCaseIdAsync(tenantId, "Claims", "case-replacement", cancellationToken)).ShouldBeTrue();
        (await store.GetCaseIdAsync(tenantId, "Claims", cancellationToken)).ShouldBe("case-replacement");
        (await store.GetAggregateCountAsync(tenantId, cancellationToken)).ShouldBe(2);
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
    public async Task ObservedEventType_RecoversMissingDiscoveryAndRenewsIndexesAgainstLiveStatestore()
    {
        FakeTimeProvider clock = new(DateTimeOffset.UtcNow);
        DaprObservedEventTypeStore store = CreateObservedStore(clock);
        string tenantId = $"ida-recover-{Guid.NewGuid():N}"[..28];
        string discoveryKey = $"{tenantId}:eventstore:observed-aggregates";
        string writtenKey = $"{tenantId}:eventstore:observed-written-aggregates";
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        TimeSpan window = TimeSpan.FromHours(1);
        await store.RecordObservationAsync(tenantId, "Claims", "ClaimSubmitted", clock.GetUtcNow(), cancellationToken);
        await store.RecordObservationAsync(tenantId, "Orders", "OrderSubmitted", clock.GetUtcNow(), cancellationToken);
        await _fixture.DaprClient.DeleteStateAsync(_fixture.StateStoreName, discoveryKey, cancellationToken: cancellationToken);
        (await store.GetAllObservedTypesAsync(tenantId, window, cancellationToken)).ShouldBeEmpty();

        await store.RecordObservationAsync(tenantId, "Claims", "ClaimSubmitted", clock.GetUtcNow(), cancellationToken);

        IReadOnlyList<ObservedEventType> recovered = await store.GetAllObservedTypesAsync(tenantId, window, cancellationToken);
        recovered.Select(observation => observation.AggregateType).ShouldBe(["Claims", "Orders"], ignoreOrder: true);
        recovered.Single(observation => observation.AggregateType == "Orders").Count.ShouldBe(1);

        Dictionary<string, string> shortTtl = new() { ["ttlInSeconds"] = "3" };
        foreach (string key in new[] { discoveryKey, writtenKey })
        {
            (List<string> index, string etag) = await _fixture.DaprClient.GetStateAndETagAsync<List<string>>(
                _fixture.StateStoreName, key, cancellationToken: cancellationToken);
            index.ShouldBe(["Claims", "Orders"], ignoreOrder: true);
            (await _fixture.DaprClient.TrySaveStateAsync(
                _fixture.StateStoreName, key, index, etag, metadata: shortTtl, cancellationToken: cancellationToken))
                .ShouldBeTrue();
        }

        await store.RecordObservationAsync(tenantId, "Claims", "ClaimSubmitted", clock.GetUtcNow(), cancellationToken);
        await Task.Delay(TimeSpan.FromSeconds(4), cancellationToken);

        IReadOnlyList<ObservedEventType> refreshed = await store.GetAllObservedTypesAsync(tenantId, window, cancellationToken);
        refreshed.Select(observation => observation.AggregateType).ShouldBe(["Claims", "Orders"], ignoreOrder: true);
        refreshed.Single(observation => observation.AggregateType == "Claims").Count.ShouldBe(3);
        (await _fixture.DaprClient.GetStateAsync<List<string>>(_fixture.StateStoreName, writtenKey, cancellationToken: cancellationToken))
            .ShouldBe(["Claims", "Orders"], ignoreOrder: true);
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
