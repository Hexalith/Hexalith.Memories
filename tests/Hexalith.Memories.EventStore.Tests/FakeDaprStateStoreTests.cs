// <copyright file="FakeDaprStateStoreTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.EventStore.Tests;

using Dapr.Client;

using Microsoft.Extensions.Time.Testing;

using Shouldly;

/// <summary>Verifies the persistence behavior of the Dapr test double used by the EventStore adapters.</summary>
public sealed class FakeDaprStateStoreTests
{
    private const string Store = FakeDaprStateStore.StoreName;
    private static readonly StateOptions FirstWrite = new() { Concurrency = ConcurrencyMode.FirstWrite };
    private static readonly StateOptions LastWrite = new() { Concurrency = ConcurrencyMode.LastWrite };

    [Fact]
    public async Task MutableValuesAreDetachedOnSeedReadAndSuccessfulSave()
    {
        FakeDaprStateStore fake = new();
        using DaprClient client = fake.CreateClient();
        List<string> index = ["Claims"];
        fake.Seed("index", index);
        index.Add("OutsideSeed");

        (List<string>? loaded, string etag) = await client.GetStateAndETagAsync<List<string>?>(Store, "index");
        loaded.ShouldBe(["Claims"]);
        loaded!.Add("Orders");
        (await client.GetStateAsync<List<string>>(Store, "index")).ShouldBe(["Claims"]);

        (await client.TrySaveStateAsync(Store, "index", loaded, etag)).ShouldBeTrue();
        loaded.Add("OutsideSave");
        fake.TryGet("index", out List<string>? snapshot).ShouldBeTrue();
        snapshot.ShouldBe(["Claims", "Orders"]);
        snapshot!.Clear();
        (await client.GetStateAsync<List<string>>(Store, "index")).ShouldBe(["Claims", "Orders"]);
    }

    [Fact]
    public async Task RejectedDictionaryWriteLeavesPersistedValueAndEtagUnchanged()
    {
        FakeDaprStateStore fake = new();
        using DaprClient client = fake.CreateClient();
        fake.Seed("names", new Dictionary<string, string> { ["owner"] = "original" });
        (Dictionary<string, string>? values, string etag) =
            await client.GetStateAndETagAsync<Dictionary<string, string>?>(Store, "names");
        values!["owner"] = "attempted";
        fake.FailNextSaves("names", count: 1);

        (await client.TrySaveStateAsync(Store, "names", values, etag, FirstWrite)).ShouldBeFalse();

        (Dictionary<string, string>? persisted, string persistedEtag) =
            await client.GetStateAndETagAsync<Dictionary<string, string>?>(Store, "names");
        persisted!["owner"].ShouldBe("original");
        persistedEtag.ShouldBe(etag);
    }

    [Fact]
    public async Task FirstWritePreventsAStaleCreationOrUpdate()
    {
        using DaprClient client = new FakeDaprStateStore().CreateClient();
        (await client.TrySaveStateAsync(Store, "lock", "winner", string.Empty, FirstWrite)).ShouldBeTrue();
        (_, string etag) = await client.GetStateAndETagAsync<string>(Store, "lock");

        (await client.TrySaveStateAsync(Store, "lock", "second", string.Empty, FirstWrite)).ShouldBeFalse();
        (await client.TrySaveStateAsync(Store, "lock", "updated", etag, FirstWrite)).ShouldBeTrue();
        (await client.TrySaveStateAsync(Store, "lock", "stale", etag, FirstWrite)).ShouldBeFalse();
        (await client.TryDeleteStateAsync(Store, "lock", etag, FirstWrite)).ShouldBeFalse();
        (await client.GetStateAsync<string>(Store, "lock")).ShouldBe("updated");
    }

    [Fact]
    public async Task OrdinaryLastWriteCanIgnoreThePreviousVersion()
    {
        using DaprClient client = new FakeDaprStateStore().CreateClient();
        (await client.TrySaveStateAsync(Store, "status", "before", string.Empty, LastWrite)).ShouldBeTrue();
        (_, string etag) = await client.GetStateAndETagAsync<string>(Store, "status");
        (await client.TrySaveStateAsync(Store, "status", "before-repair", etag)).ShouldBeTrue();
        (await client.TrySaveStateAsync(Store, "status", "stale", etag)).ShouldBeFalse();

        (await client.TrySaveStateAsync(Store, "status", "repaired", etag, LastWrite)).ShouldBeTrue();
        (await client.GetStateAsync<string>(Store, "status")).ShouldBe("repaired");
    }

    [Fact]
    public async Task ExpiryUsesTheClockAndAllowsFirstWriteAfterExpiry()
    {
        FakeTimeProvider clock = new(DateTimeOffset.UnixEpoch);
        FakeDaprStateStore fake = new(clock);
        using DaprClient writer = fake.CreateClient();
        using DaprClient reader = fake.CreateClient();
        Dictionary<string, string> ttl = new() { ["ttlInSeconds"] = "5" };
        (await writer.TrySaveStateAsync(Store, "lock", "first", string.Empty, FirstWrite, ttl)).ShouldBeTrue();

        clock.Advance(TimeSpan.FromSeconds(4));
        (await reader.GetBulkStateAsync<string>(Store, ["lock"], parallelism: null)).ShouldHaveSingleItem().Value.ShouldBe("first");
        clock.Advance(TimeSpan.FromSeconds(1));
        (await reader.GetBulkStateAsync<string>(Store, ["lock"], parallelism: null)).ShouldBeEmpty();
        (string? expired, string expiredEtag) = await writer.GetStateAndETagAsync<string?>(Store, "lock");
        expired.ShouldBeNull();
        expiredEtag.ShouldBeEmpty();
        fake.ContainsKey("lock").ShouldBeFalse();

        (await reader.TrySaveStateAsync(Store, "lock", "second", string.Empty, FirstWrite)).ShouldBeTrue();
        (await writer.GetStateAsync<string>(Store, "lock")).ShouldBe("second");
    }

    [Fact]
    public async Task SuccessfulUpdatesRefreshPreserveAndClearExpiry()
    {
        FakeTimeProvider clock = new(DateTimeOffset.UnixEpoch);
        FakeDaprStateStore fake = new(clock);
        using DaprClient client = fake.CreateClient();
        Dictionary<string, string> ttl = new() { ["ttlInSeconds"] = "5" };
        (await client.TrySaveStateAsync(Store, "keep", "first", string.Empty, LastWrite, ttl)).ShouldBeTrue();
        (await client.TrySaveStateAsync(Store, "clear", "first", string.Empty, LastWrite, ttl)).ShouldBeTrue();

        clock.Advance(TimeSpan.FromSeconds(3));
        (await client.TrySaveStateAsync(Store, "keep", "refresh", string.Empty, LastWrite, ttl)).ShouldBeTrue();
        (await client.TrySaveStateAsync(Store, "keep", "preserve", string.Empty, LastWrite)).ShouldBeTrue();
        (await client.TrySaveStateAsync(Store, "clear", "persist", string.Empty, LastWrite,
            new Dictionary<string, string> { ["ttlInSeconds"] = "0" })).ShouldBeTrue();
        clock.Advance(TimeSpan.FromSeconds(3));
        fake.ContainsKey("keep").ShouldBeTrue();
        clock.Advance(TimeSpan.FromSeconds(2));
        fake.ContainsKey("keep").ShouldBeFalse();
        fake.ContainsKey("clear").ShouldBeTrue();
    }
}
