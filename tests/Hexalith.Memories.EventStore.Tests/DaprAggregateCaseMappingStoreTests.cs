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

/// <summary>spec-infrastructure-dependency-abstraction (F6, ADR-IDA-001) — unit tests for the Dapr-state
/// migration of the aggregate→case mapping store. Covers first-writer-wins, delete-by-case, the creation
/// lock (set-if-not-exists + release), idempotent/duplicate writes, and input validation. Real ETag CAS
/// contention and TTL are Tier-2 integration concerns.</summary>
public sealed class DaprAggregateCaseMappingStoreTests
{
    private const string Store = FakeDaprStateStore.StoreName;
    private const string MapKey = "tenant-1:eventstore:aggregate-case-map";

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
    }

    [Fact]
    public async Task TryStoreCaseIdAsync_WhenAlreadyMapped_IsFirstWriterWins()
    {
        FakeDaprStateStore fake = new();
        DaprAggregateCaseMappingStore store = CreateStore(fake);
        _ = await store.TryStoreCaseIdAsync("tenant-1", "Claims", "case-1", CancellationToken.None);

        // Duplicate / late write for the same aggregate type must not overwrite the winner.
        bool second = await store.TryStoreCaseIdAsync("tenant-1", "Claims", "case-2", CancellationToken.None);

        second.ShouldBeFalse();
        (await store.GetCaseIdAsync("tenant-1", "Claims", CancellationToken.None)).ShouldBe("case-1");
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
        fake.Seed(MapKey, new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["Claims"] = "case-delete",
            ["Orders"] = "case-keep",
            ["Invoices"] = "case-delete",
        });
        DaprAggregateCaseMappingStore store = CreateStore(fake);

        long deleted = await store.DeleteCaseMappingsAsync("tenant-1", "case-delete", CancellationToken.None);

        deleted.ShouldBe(2);
        fake.TryGet(MapKey, out Dictionary<string, string>? map).ShouldBeTrue();
        map!.Keys.ShouldBe(["Orders"]);
    }

    [Fact]
    public async Task DeleteCaseMappingsAsync_WhenMapMissing_ReturnsZero()
    {
        FakeDaprStateStore fake = new();
        DaprAggregateCaseMappingStore store = CreateStore(fake);

        (await store.DeleteCaseMappingsAsync("tenant-1", "case-delete", CancellationToken.None)).ShouldBe(0);
    }

    [Fact]
    public async Task DeleteCaseMappingsAsync_WhenNoMatch_ReturnsZeroWithoutWrite()
    {
        FakeDaprStateStore fake = new();
        DaprClient client = fake.CreateClient();
        fake.Seed(MapKey, new Dictionary<string, string>(StringComparer.Ordinal) { ["Claims"] = "case-keep" });
        DaprAggregateCaseMappingStore store =
            new(client, Options.Create(new EventStoreStateStoreOptions { StateStoreName = Store }));

        long deleted = await store.DeleteCaseMappingsAsync("tenant-1", "case-delete", CancellationToken.None);

        deleted.ShouldBe(0);
        _ = client.DidNotReceive().TrySaveStateAsync(
            Store, MapKey, Arg.Any<Dictionary<string, string>>(), Arg.Any<string>(),
            Arg.Any<StateOptions?>(), Arg.Any<IReadOnlyDictionary<string, string>?>(), Arg.Any<CancellationToken>());
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

        _ = client.DidNotReceive().GetStateAndETagAsync<Dictionary<string, string>?>(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<ConsistencyMode?>(),
            Arg.Any<IReadOnlyDictionary<string, string>?>(), Arg.Any<CancellationToken>());
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
