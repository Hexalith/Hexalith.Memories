// <copyright file="DaprAggregateCaseMappingStoreTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.EventStore.Tests;

using System.Collections.Generic;

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
