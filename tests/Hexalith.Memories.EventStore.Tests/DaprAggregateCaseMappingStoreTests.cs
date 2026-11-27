// <copyright file="DaprAggregateCaseMappingStoreTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.EventStore.Tests;

using System.Collections.Generic;

using Dapr;
using Dapr.Client;

using Hexalith.Memories.EventStore;

using Microsoft.Extensions.Options;

using NSubstitute;

using Shouldly;

/// <summary>spec-infrastructure-dependency-abstraction (F6, ADR-IDA-001, review D1) — unit tests for the
/// per-aggregate-type FirstWrite Dapr-state mapping store. Covers first-writer-wins, delete-by-case,
/// creation lock, idempotent/duplicate writes, CAS-exhaustion fail-loud, and tenant purge.</summary>
public sealed class DaprAggregateCaseMappingStoreTests
{
    private const string Store = FakeDaprStateStore.StoreName;
    private const string IndexKey = "tenant-1:eventstore:aggregate-case-map-index";

    private static DaprAggregateCaseMappingStore CreateStore(FakeDaprStateStore fake)
        => new(fake.CreateClient(), Options.Create(new EventStoreStateStoreOptions { StateStoreName = Store }));

    [Fact]
    public async Task TryStoreCaseIdAsync_WhenAbsent_StoresAndReturnsTrue()
    {
        FakeDaprStateStore fake = new();
        DaprAggregateCaseMappingStore store = CreateStore(fake);

        bool stored = await store.TryStoreCaseIdAsync("tenant-1", "Claims", "case-1", CancellationToken.None);

        stored.ShouldBeTrue();
        (await store.GetCaseIdAsync("tenant-1", "Claims", CancellationToken.None)).ShouldBe("case-1");
        fake.TryGet(IndexKey, out List<string>? index).ShouldBeTrue();
        index.ShouldNotBeNull();
        index.ShouldContain("Claims");
    }

    [Fact]
    public async Task TryStoreCaseIdAsync_WhenAlreadyMapped_IsFirstWriterWins()
    {
        FakeDaprStateStore fake = new();
        DaprAggregateCaseMappingStore store = CreateStore(fake);
        _ = await store.TryStoreCaseIdAsync("tenant-1", "Claims", "case-1", CancellationToken.None);

        bool second = await store.TryStoreCaseIdAsync("tenant-1", "Claims", "case-2", CancellationToken.None);

        second.ShouldBeFalse();
        (await store.GetCaseIdAsync("tenant-1", "Claims", CancellationToken.None)).ShouldBe("case-1");
    }

    [Fact]
    public async Task TryStoreCaseIdAsync_UnrelatedAggregates_DoNotContendOnSharedDocument()
    {
        FakeDaprStateStore fake = new();
        DaprAggregateCaseMappingStore store = CreateStore(fake);

        (await store.TryStoreCaseIdAsync("tenant-1", "Claims", "case-1", CancellationToken.None)).ShouldBeTrue();
        (await store.TryStoreCaseIdAsync("tenant-1", "Orders", "case-2", CancellationToken.None)).ShouldBeTrue();

        fake.ContainsKey("tenant-1:eventstore:aggregate-case-map:Claims").ShouldBeTrue();
        fake.ContainsKey("tenant-1:eventstore:aggregate-case-map:Orders").ShouldBeTrue();
        (await store.GetAggregateCountAsync("tenant-1", CancellationToken.None)).ShouldBe(2);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task TryStoreCaseIdAsync_WhenTwoWritersCreateIndex_KeepsBothMappings(bool throwsConflict)
    {
        FakeDaprStateStore fake = new();
        DaprClient client = fake.CreateClient();
        DaprAggregateCaseMappingStore store = new(client, Options.Create(new EventStoreStateStoreOptions { StateStoreName = Store }));
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        deadline.CancelAfter(TimeSpan.FromSeconds(5));
        var firstCreationReached = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var winningCreationCompleted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        _ = client.TrySaveStateAsync(
                Store, IndexKey, Arg.Any<List<string>>(), string.Empty, Arg.Any<StateOptions?>(),
                Arg.Any<IReadOnlyDictionary<string, string>?>(), Arg.Any<CancellationToken>())
            .Returns(async call =>
            {
                List<string> update = call.ArgAt<List<string>>(2);
                if (update.ShouldHaveSingleItem() == "Claims")
                {
                    // Both writers read an absent index. Let Orders commit before Claims
                    // attempts its empty-ETag write, which only FirstWrite can reject.
                    firstCreationReached.SetResult();
                    await winningCreationCompleted.Task.WaitAsync(deadline.Token);
                    if (call.ArgAt<StateOptions?>(4)?.Concurrency == ConcurrencyMode.FirstWrite)
                    {
                        if (throwsConflict)
                        {
                            throw new DaprException("FirstWrite rejected by the Redis state component.");
                        }

                        return false;
                    }
                }

                fake.Seed(IndexKey, update);
                return true;
            });

        Task<bool> claims = store.TryStoreCaseIdAsync("tenant-1", "Claims", "case-claims", deadline.Token);
        try
        {
            await firstCreationReached.Task.WaitAsync(deadline.Token);
            (await store.TryStoreCaseIdAsync("tenant-1", "Orders", "case-orders", deadline.Token)).ShouldBeTrue();
        }
        finally
        {
            winningCreationCompleted.SetResult();
            (await claims.WaitAsync(deadline.Token)).ShouldBeTrue();
        }

        (await store.GetCaseIdAsync("tenant-1", "Claims", deadline.Token)).ShouldBe("case-claims");
        (await store.GetCaseIdAsync("tenant-1", "Orders", deadline.Token)).ShouldBe("case-orders");
        (await store.GetAggregateCountAsync("tenant-1", deadline.Token)).ShouldBe(2);
        fake.TryGet(IndexKey, out List<string>? index).ShouldBeTrue();
        index.ShouldBe(["Orders", "Claims"]);
        _ = client.Received(2).TrySaveStateAsync(
            Store, IndexKey, Arg.Any<List<string>>(), string.Empty,
            Arg.Is<StateOptions?>(options => options != null && options.Concurrency == ConcurrencyMode.FirstWrite),
            Arg.Any<IReadOnlyDictionary<string, string>?>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetCaseIdAsync_WhenAbsent_ReturnsNull()
    {
        FakeDaprStateStore fake = new();
        DaprAggregateCaseMappingStore store = CreateStore(fake);

        (await store.GetCaseIdAsync("tenant-1", "Missing", CancellationToken.None)).ShouldBeNull();
    }

    [Fact]
    public async Task GetAggregateCountAsync_ReturnsNumberOfMappings()
    {
        FakeDaprStateStore fake = new();
        DaprAggregateCaseMappingStore store = CreateStore(fake);
        _ = await store.TryStoreCaseIdAsync("tenant-1", "Claims", "case-1", CancellationToken.None);
        _ = await store.TryStoreCaseIdAsync("tenant-1", "Orders", "case-2", CancellationToken.None);

        (await store.GetAggregateCountAsync("tenant-1", CancellationToken.None)).ShouldBe(2);
    }

    [Fact]
    public async Task DeleteCaseMappingsAsync_RemovesOnlyFieldsPointingAtCase()
    {
        FakeDaprStateStore fake = new();
        DaprAggregateCaseMappingStore store = CreateStore(fake);
        _ = await store.TryStoreCaseIdAsync("tenant-1", "Claims", "case-delete", CancellationToken.None);
        _ = await store.TryStoreCaseIdAsync("tenant-1", "Orders", "case-keep", CancellationToken.None);
        _ = await store.TryStoreCaseIdAsync("tenant-1", "Invoices", "case-delete", CancellationToken.None);

        long deleted = await store.DeleteCaseMappingsAsync("tenant-1", "case-delete", CancellationToken.None);

        deleted.ShouldBe(2);
        (await store.GetCaseIdAsync("tenant-1", "Orders", CancellationToken.None)).ShouldBe("case-keep");
        (await store.GetCaseIdAsync("tenant-1", "Claims", CancellationToken.None)).ShouldBeNull();
        (await store.GetAggregateCountAsync("tenant-1", CancellationToken.None)).ShouldBe(1);
    }

    [Fact]
    public async Task DeleteCaseMappingsAsync_WhenMapMissing_ReturnsZero()
    {
        FakeDaprStateStore fake = new();
        DaprAggregateCaseMappingStore store = CreateStore(fake);

        (await store.DeleteCaseMappingsAsync("tenant-1", "case-delete", CancellationToken.None)).ShouldBe(0);
    }

    [Fact]
    public async Task DeleteCaseMappingsAsync_WhenNoMatch_ReturnsZero()
    {
        FakeDaprStateStore fake = new();
        DaprAggregateCaseMappingStore store = CreateStore(fake);
        _ = await store.TryStoreCaseIdAsync("tenant-1", "Claims", "case-keep", CancellationToken.None);

        long deleted = await store.DeleteCaseMappingsAsync("tenant-1", "case-delete", CancellationToken.None);

        deleted.ShouldBe(0);
        (await store.GetCaseIdAsync("tenant-1", "Claims", CancellationToken.None)).ShouldBe("case-keep");
        (await store.GetAggregateCountAsync("tenant-1", CancellationToken.None)).ShouldBe(1);
    }

    [Theory]
    [InlineData("", "case-1")]
    [InlineData(" ", "case-1")]
    [InlineData("tenant-1", "")]
    [InlineData("tenant-1", " ")]
    public async Task DeleteCaseMappingsAsync_WithInvalidInput_ThrowsBeforeStateCall(string tenantId, string caseId)
    {
        FakeDaprStateStore fake = new();
        DaprClient client = fake.CreateClient();
        DaprAggregateCaseMappingStore store =
            new(client, Options.Create(new EventStoreStateStoreOptions { StateStoreName = Store }));

        _ = await Should.ThrowAsync<ArgumentException>(
            () => store.DeleteCaseMappingsAsync(tenantId, caseId, CancellationToken.None));

        _ = client.DidNotReceive().GetStateAndETagAsync<List<string>?>(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<ConsistencyMode?>(),
            Arg.Any<IReadOnlyDictionary<string, string>?>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DeleteAllTenantDataAsync_RemovesIndexEntriesAndLocks()
    {
        FakeDaprStateStore fake = new();
        DaprAggregateCaseMappingStore store = CreateStore(fake);
        _ = await store.TryStoreCaseIdAsync("tenant-1", "Claims", "case-1", CancellationToken.None);
        _ = await store.TryAcquireCreationLockAsync("tenant-1", "Claims", TimeSpan.FromSeconds(30), CancellationToken.None);

        await store.DeleteAllTenantDataAsync("tenant-1", CancellationToken.None);

        (await store.GetCaseIdAsync("tenant-1", "Claims", CancellationToken.None)).ShouldBeNull();
        (await store.GetAggregateCountAsync("tenant-1", CancellationToken.None)).ShouldBe(0);
        fake.ContainsKey(IndexKey).ShouldBeFalse();
        fake.ContainsKey("tenant-1:eventstore:aggregate-case-lock:Claims").ShouldBeFalse();
    }

    [Fact]
    public async Task EnsureIndexedAsync_WhenIndexCasExhausted_RollsBackMapEntryAndThrows()
    {
        // review patch #2/#7: FirstWrite then index CAS exhaustion must not leave an unindexed orphan.
        FakeDaprStateStore fake = new();
        fake.FailNextSaves(IndexKey, count: 16);
        DaprAggregateCaseMappingStore store = CreateStore(fake);

        InvalidOperationException ex = await Should.ThrowAsync<InvalidOperationException>(
            () => store.TryStoreCaseIdAsync("tenant-1", "Claims", "case-1", CancellationToken.None));

        ex.Message.ShouldContain("Failed to index");
        fake.ContainsKey("tenant-1:eventstore:aggregate-case-map:Claims").ShouldBeFalse();
        fake.ContainsKey(IndexKey).ShouldBeFalse();
    }

    [Fact]
    public async Task TryStoreCaseIdAsync_WhenFirstIndexSaveFailsWithoutWinner_RollsBackMapEntryAndThrows()
    {
        FakeDaprStateStore fake = new();
        DaprClient client = fake.CreateClient();
        var failure = new DaprException("The state store could not save the index.");
        _ = client.TrySaveStateAsync(
                Store, IndexKey, Arg.Any<List<string>>(), string.Empty, Arg.Any<StateOptions?>(),
                Arg.Any<IReadOnlyDictionary<string, string>?>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException<bool>(failure));
        DaprAggregateCaseMappingStore store = new(client, Options.Create(new EventStoreStateStoreOptions { StateStoreName = Store }));

        DaprException actual = await Should.ThrowAsync<DaprException>(
            () => store.TryStoreCaseIdAsync("tenant-1", "Claims", "case-claims", CancellationToken.None));

        actual.ShouldBeSameAs(failure);
        fake.ContainsKey("tenant-1:eventstore:aggregate-case-map:Claims").ShouldBeFalse();
        fake.ContainsKey(IndexKey).ShouldBeFalse();
    }

    [Fact]
    public async Task TryStoreCaseIdAsync_WhenLastIndexSaveCommitsButThrows_RecognizesThePublishedMapping()
    {
        FakeDaprStateStore fake = new();
        DaprClient client = fake.CreateClient();
        int indexWrites = 0;
        _ = client.TrySaveStateAsync(
                Store, IndexKey, Arg.Any<List<string>>(), string.Empty, Arg.Any<StateOptions?>(),
                Arg.Any<IReadOnlyDictionary<string, string>?>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                if (++indexWrites < 8)
                {
                    return Task.FromResult(false);
                }

                fake.Seed(IndexKey, call.ArgAt<List<string>>(2));
                return Task.FromException<bool>(new DaprException("The reply was lost after writing the index."));
            });
        DaprAggregateCaseMappingStore store = new(client, Options.Create(new EventStoreStateStoreOptions { StateStoreName = Store }));

        (await store.TryStoreCaseIdAsync("tenant-1", "Claims", "case-claims", CancellationToken.None)).ShouldBeTrue();

        indexWrites.ShouldBe(8);
        (await store.GetCaseIdAsync("tenant-1", "Claims", CancellationToken.None)).ShouldBe("case-claims");
        (await store.GetAggregateCountAsync("tenant-1", CancellationToken.None)).ShouldBe(1);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task TryStoreCaseIdAsync_WhenIndexSaveIsCancelled_OnlyRemovesUnindexedMapping(bool indexCommitted)
    {
        const string mapKey = "tenant-1:eventstore:aggregate-case-map:Claims";
        FakeDaprStateStore fake = new();
        DaprClient client = fake.CreateClient();
        using var cancellation = new CancellationTokenSource();
        _ = client.TrySaveStateAsync(
                Store, IndexKey, Arg.Any<List<string>>(), string.Empty, Arg.Any<StateOptions?>(),
                Arg.Any<IReadOnlyDictionary<string, string>?>(), cancellation.Token)
            .Returns(call =>
            {
                if (indexCommitted)
                {
                    fake.Seed(IndexKey, call.ArgAt<List<string>>(2));
                }

                cancellation.Cancel();
                return Task.FromCanceled<bool>(cancellation.Token);
            });
        _ = client.DeleteStateAsync(
                Store, mapKey, Arg.Any<StateOptions?>(), Arg.Any<IReadOnlyDictionary<string, string>?>(), cancellation.Token)
            .Returns(_ => Task.FromCanceled(cancellation.Token));
        DaprAggregateCaseMappingStore store = new(client, Options.Create(new EventStoreStateStoreOptions { StateStoreName = Store }));

        OperationCanceledException cancelled = await Should.ThrowAsync<OperationCanceledException>(
            () => store.TryStoreCaseIdAsync("tenant-1", "Claims", "case-claims", cancellation.Token));

        cancelled.CancellationToken.ShouldBe(cancellation.Token);
        fake.ContainsKey(mapKey).ShouldBe(indexCommitted);
        fake.ContainsKey(IndexKey).ShouldBe(indexCommitted);
        _ = client.Received(1).GetStateAsync<List<string>?>(
            Store,
            IndexKey,
            Arg.Any<ConsistencyMode?>(),
            Arg.Any<IReadOnlyDictionary<string, string>?>(),
            Arg.Is<CancellationToken>(token => token.CanBeCanceled && token != cancellation.Token));
    }

    [Fact]
    public async Task TryStoreCaseIdAsync_WhenRecoveryReadAlsoFails_PreservesOriginalFailureAndMapping()
    {
        FakeDaprStateStore fake = new();
        DaprClient client = fake.CreateClient();
        var recoveryFailure = new DaprException("The index could not be checked during compensation.");
        var indexFailure = new InvalidOperationException("The index write did not report success.");
        _ = client.TrySaveStateAsync(
                Store, IndexKey, Arg.Any<List<string>>(), string.Empty, Arg.Any<StateOptions?>(),
                Arg.Any<IReadOnlyDictionary<string, string>?>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException<bool>(indexFailure));
        _ = client.GetStateAsync<List<string>?>(
                Store, IndexKey, Arg.Any<ConsistencyMode?>(),
                Arg.Any<IReadOnlyDictionary<string, string>?>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException<List<string>?>(recoveryFailure));
        DaprAggregateCaseMappingStore store = new(client, Options.Create(new EventStoreStateStoreOptions { StateStoreName = Store }));

        InvalidOperationException failure = await Should.ThrowAsync<InvalidOperationException>(
            () => store.TryStoreCaseIdAsync("tenant-1", "Claims", "case-claims", CancellationToken.None));

        failure.ShouldBeSameAs(indexFailure);
        failure.Data["CompensationFailure"].ShouldBeSameAs(recoveryFailure);
        fake.ContainsKey("tenant-1:eventstore:aggregate-case-map:Claims").ShouldBeTrue();
    }

    [Fact]
    public async Task DeleteCaseMappingsAsync_WhenIndexCasExhausted_KeepsMapKeysAndThrows()
    {
        // review patch #1/#7: map deletes are deferred until index save succeeds.
        FakeDaprStateStore fake = new();
        DaprAggregateCaseMappingStore store = CreateStore(fake);
        _ = await store.TryStoreCaseIdAsync("tenant-1", "Claims", "case-delete", CancellationToken.None);
        _ = await store.TryStoreCaseIdAsync("tenant-1", "Orders", "case-keep", CancellationToken.None);
        fake.FailNextSaves(IndexKey, count: 16);

        InvalidOperationException ex = await Should.ThrowAsync<InvalidOperationException>(
            () => store.DeleteCaseMappingsAsync("tenant-1", "case-delete", CancellationToken.None));

        ex.Message.ShouldContain("Failed to delete");
        (await store.GetCaseIdAsync("tenant-1", "Claims", CancellationToken.None)).ShouldBe("case-delete");
        (await store.GetCaseIdAsync("tenant-1", "Orders", CancellationToken.None)).ShouldBe("case-keep");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task TryAcquireCreationLockAsync_WithNonPositiveLease_Throws(int seconds)
    {
        FakeDaprStateStore fake = new();
        DaprAggregateCaseMappingStore store = CreateStore(fake);

        await Should.ThrowAsync<ArgumentOutOfRangeException>(
            () => store.TryAcquireCreationLockAsync("tenant-1", "Claims", TimeSpan.FromSeconds(seconds), CancellationToken.None));
    }

    [Fact]
    public async Task CreationLock_AcquireThenSecondAcquire_IsExclusive()
    {
        FakeDaprStateStore fake = new();
        DaprAggregateCaseMappingStore store = CreateStore(fake);

        bool first = await store.TryAcquireCreationLockAsync("tenant-1", "Claims", TimeSpan.FromSeconds(30), CancellationToken.None);
        bool second = await store.TryAcquireCreationLockAsync("tenant-1", "Claims", TimeSpan.FromSeconds(30), CancellationToken.None);

        first.ShouldBeTrue();
        second.ShouldBeFalse();
    }

    [Fact]
    public async Task CreationLock_ReleaseThenReacquire_Succeeds()
    {
        FakeDaprStateStore fake = new();
        DaprAggregateCaseMappingStore store = CreateStore(fake);
        _ = await store.TryAcquireCreationLockAsync("tenant-1", "Claims", TimeSpan.FromSeconds(30), CancellationToken.None);

        await store.ReleaseCreationLockAsync("tenant-1", "Claims", CancellationToken.None);
        bool reacquired = await store.TryAcquireCreationLockAsync("tenant-1", "Claims", TimeSpan.FromSeconds(30), CancellationToken.None);

        reacquired.ShouldBeTrue();
    }
}
