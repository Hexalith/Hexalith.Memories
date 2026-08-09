namespace Hexalith.Memories.Server.Tests.Activities.Tenants;

using System.Collections.Generic;

using Dapr.Client;
using Dapr.Workflow;

using Hexalith.Memories.Contracts.V1;
using Hexalith.Memories.EventStore;
using Hexalith.Memories.Server.Activities.Tenants;
using Hexalith.Memories.Server.Infrastructure;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;

using NSubstitute;

using Shouldly;

using StackExchange.Redis;

public class DeleteTenantDataKeysActivityTests
{
    [Fact]
    public async Task RunAsync_ShouldDeleteAllTenantDataKeysUsingExpectedPrefixes()
    {
        IDatabase db = Substitute.For<IDatabase>();
        db.Database.Returns(0);
        db.KeyDeleteAsync(Arg.Any<RedisKey[]>(), Arg.Any<CommandFlags>())
            .Returns(callInfo => (long)((RedisKey[])callInfo[0]!).Length);

        IServer server = Substitute.For<IServer>();
        server.IsConnected.Returns(true);
        IReadOnlyDictionary<string, string> patterns = BuildExpectedPatternKeys("tenant-1");
        foreach (KeyValuePair<string, string> pattern in patterns)
        {
            server.KeysAsync(0, pattern.Key, 1000, Arg.Any<long>(), Arg.Any<int>(), Arg.Any<CommandFlags>())
                .Returns(GetKeys(pattern.Value));
        }

        IConnectionMultiplexer redis = Substitute.For<IConnectionMultiplexer>();
        redis.GetDatabase(Arg.Any<int>(), Arg.Any<object>()).Returns(db);
        redis.GetServers().Returns(new[] { server });

        IAggregateCaseMappingStore mappingStore = Substitute.For<IAggregateCaseMappingStore>();
        IObservedEventTypeStore observedStore = Substitute.For<IObservedEventTypeStore>();
        ILogger<DeleteTenantDataKeysActivity> logger = Substitute.For<ILogger<DeleteTenantDataKeysActivity>>();
        WorkflowActivityContext context = Substitute.For<WorkflowActivityContext>();
        DeleteTenantDataKeysActivity activity = new(
            redis, mappingStore, observedStore, CreateLifetime(), logger);

        bool result = await activity.RunAsync(context, new TenantDeletionInput("tenant-1"));

        result.ShouldBeTrue();
        await db.Received(patterns.Count).KeyDeleteAsync(Arg.Any<RedisKey[]>(), Arg.Any<CommandFlags>());
        foreach (KeyValuePair<string, string> pattern in patterns)
        {
            _ = server.Received(1).KeysAsync(0, pattern.Key, 1000, Arg.Any<long>(), Arg.Any<int>(), Arg.Any<CommandFlags>());
            await db.Received(1).KeyDeleteAsync(
                Arg.Is<RedisKey[]>(keys => keys!.Select(static k => k.ToString()).SequenceEqual(new[] { pattern.Value })),
                Arg.Any<CommandFlags>());
        }

        await mappingStore.Received(1).DeleteAllTenantDataAsync("tenant-1", Arg.Any<CancellationToken>());
        await observedStore.Received(1).DeleteAllTenantDataAsync("tenant-1", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RunAsync_WhenNoConnectedServer_ShouldThrowInvalidOperationException()
    {
        IDatabase db = Substitute.For<IDatabase>();
        db.Database.Returns(0);

        IServer disconnectedServer = Substitute.For<IServer>();
        disconnectedServer.IsConnected.Returns(false);

        IConnectionMultiplexer redis = Substitute.For<IConnectionMultiplexer>();
        redis.GetDatabase(Arg.Any<int>(), Arg.Any<object>()).Returns(db);
        redis.GetServers().Returns(new[] { disconnectedServer });

        IAggregateCaseMappingStore mappingStore = Substitute.For<IAggregateCaseMappingStore>();
        IObservedEventTypeStore observedStore = Substitute.For<IObservedEventTypeStore>();
        ILogger<DeleteTenantDataKeysActivity> logger = Substitute.For<ILogger<DeleteTenantDataKeysActivity>>();
        WorkflowActivityContext context = Substitute.For<WorkflowActivityContext>();
        DeleteTenantDataKeysActivity activity = new(
            redis, mappingStore, observedStore, CreateLifetime(), logger);

        await Should.ThrowAsync<InvalidOperationException>(() => activity.RunAsync(context, new TenantDeletionInput("tenant-1")));
        await mappingStore.DidNotReceive().DeleteAllTenantDataAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
        await observedStore.DidNotReceive().DeleteAllTenantDataAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RunAsync_PurgesDaprStateForRequestedTenantOnly_FailClosedOnOtherTenant()
    {
        // Tenant-isolation negative evidence (review D3): purge is keyed by the activity input tenant id.
        IDatabase db = Substitute.For<IDatabase>();
        db.Database.Returns(0);
        db.KeyDeleteAsync(Arg.Any<RedisKey[]>(), Arg.Any<CommandFlags>()).Returns(0L);

        IServer server = Substitute.For<IServer>();
        server.IsConnected.Returns(true);
        foreach (string pattern in BuildExpectedPatternKeys("tenant-a").Keys)
        {
            server.KeysAsync(0, pattern, 1000, Arg.Any<long>(), Arg.Any<int>(), Arg.Any<CommandFlags>())
                .Returns(EmptyKeys());
        }

        IConnectionMultiplexer redis = Substitute.For<IConnectionMultiplexer>();
        redis.GetDatabase(Arg.Any<int>(), Arg.Any<object>()).Returns(db);
        redis.GetServers().Returns([server]);

        IAggregateCaseMappingStore mappingStore = Substitute.For<IAggregateCaseMappingStore>();
        IObservedEventTypeStore observedStore = Substitute.For<IObservedEventTypeStore>();
        DeleteTenantDataKeysActivity activity = new(
            redis, mappingStore, observedStore, CreateLifetime(), Substitute.For<ILogger<DeleteTenantDataKeysActivity>>());

        _ = await activity.RunAsync(Substitute.For<WorkflowActivityContext>(), new TenantDeletionInput("tenant-a"));

        await mappingStore.Received(1).DeleteAllTenantDataAsync("tenant-a", Arg.Any<CancellationToken>());
        await observedStore.Received(1).DeleteAllTenantDataAsync("tenant-a", Arg.Any<CancellationToken>());
        await mappingStore.DidNotReceive().DeleteAllTenantDataAsync("tenant-b", Arg.Any<CancellationToken>());
        await observedStore.DidNotReceive().DeleteAllTenantDataAsync("tenant-b", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RunAsync_WithRealDaprStores_PurgesSeededKeysForInputTenantOnly()
    {
        // review patch #11: non-mock purge over FakeDaprStateStore-backed real store implementations.
        InMemoryDaprStateStore fake = new();
        DaprClient client = fake.CreateClient();
        DaprAggregateCaseMappingStore mappingStore = new(
            client, Options.Create(new EventStoreStateStoreOptions { StateStoreName = InMemoryDaprStateStore.StoreName }));
        DaprObservedEventTypeStore observedStore = new(
            client,
            Options.Create(new EventStoreStateStoreOptions { StateStoreName = InMemoryDaprStateStore.StoreName }),
            new FakeTimeProvider(DateTimeOffset.UtcNow),
            NullLogger<DaprObservedEventTypeStore>.Instance);

        _ = await mappingStore.TryStoreCaseIdAsync("tenant-a", "Claims", "case-a", CancellationToken.None);
        _ = await mappingStore.TryStoreCaseIdAsync("tenant-b", "Claims", "case-b", CancellationToken.None);
        await observedStore.RecordObservationAsync(
            "tenant-a", "Claims", "ClaimSubmittedV2", DateTimeOffset.UtcNow, CancellationToken.None);
        await observedStore.RecordObservationAsync(
            "tenant-b", "Claims", "ClaimSubmittedV2", DateTimeOffset.UtcNow, CancellationToken.None);

        IDatabase db = Substitute.For<IDatabase>();
        db.Database.Returns(0);
        db.KeyDeleteAsync(Arg.Any<RedisKey[]>(), Arg.Any<CommandFlags>()).Returns(0L);
        IServer server = Substitute.For<IServer>();
        server.IsConnected.Returns(true);
        foreach (string pattern in BuildExpectedPatternKeys("tenant-a").Keys)
        {
            server.KeysAsync(0, pattern, 1000, Arg.Any<long>(), Arg.Any<int>(), Arg.Any<CommandFlags>())
                .Returns(EmptyKeys());
        }

        IConnectionMultiplexer redis = Substitute.For<IConnectionMultiplexer>();
        redis.GetDatabase(Arg.Any<int>(), Arg.Any<object>()).Returns(db);
        redis.GetServers().Returns([server]);

        DeleteTenantDataKeysActivity activity = new(
            redis, mappingStore, observedStore, CreateLifetime(), NullLogger<DeleteTenantDataKeysActivity>.Instance);

        _ = await activity.RunAsync(Substitute.For<WorkflowActivityContext>(), new TenantDeletionInput("tenant-a"));

        (await mappingStore.GetCaseIdAsync("tenant-a", "Claims", CancellationToken.None)).ShouldBeNull();
        (await mappingStore.GetCaseIdAsync("tenant-b", "Claims", CancellationToken.None)).ShouldBe("case-b");
        (await observedStore.GetObservedTypesAsync("tenant-a", "Claims", TimeSpan.FromHours(1), CancellationToken.None))
            .ShouldBeEmpty();
        (await observedStore.GetObservedTypesAsync("tenant-b", "Claims", TimeSpan.FromHours(1), CancellationToken.None))
            .Count.ShouldBe(1);
        fake.ContainsKey("tenant-a:eventstore:aggregate-case-map-index").ShouldBeFalse();
        fake.ContainsKey("tenant-b:eventstore:aggregate-case-map-index").ShouldBeTrue();
    }

    private static IHostApplicationLifetime CreateLifetime()
    {
        IHostApplicationLifetime lifetime = Substitute.For<IHostApplicationLifetime>();
        lifetime.ApplicationStopping.Returns(CancellationToken.None);
        return lifetime;
    }

    private static async IAsyncEnumerable<RedisKey> EmptyKeys()
    {
        await Task.Yield();
        yield break;
    }

    private static async IAsyncEnumerable<RedisKey> GetKeys(params string[] keys)
    {
        foreach (string key in keys)
        {
            yield return (RedisKey)key;
            await Task.Yield();
        }
    }

    private static IReadOnlyDictionary<string, string> BuildExpectedPatternKeys(string tenantId)
        => new Dictionary<string, string>(StringComparer.Ordinal)
        {
            [$"{tenantId}:case:*"] = $"{tenantId}:case:case-1",
            [$"dedup:{tenantId}:*"] = $"dedup:{tenantId}:case-1:hash",
            [$"{tenantId}:eventstore:*"] = $"{tenantId}:eventstore:aggregate-case-map",
            [$"{tenantId}:embedding-migration:*"] = $"{tenantId}:embedding-migration:active",
            [IndexSchemaDefinitions.GetSyntacticKeyPrefix(tenantId) + "*"] = IndexSchemaDefinitions.BuildSyntacticKey(tenantId, "mu-1"),
            [IndexSchemaDefinitions.GetSemanticKeyPrefix(tenantId) + "*"] = IndexSchemaDefinitions.BuildSemanticKey(tenantId, "mu-1"),
            [IndexSchemaDefinitions.GetNaturalLanguageSemanticKeyPrefix(tenantId) + "*"] = IndexSchemaDefinitions.BuildNaturalLanguageSemanticKey(tenantId, "mu-1"),
            [IndexSchemaDefinitions.GetLegacyNaturalLanguageSemanticKeyPrefix(tenantId) + "*"] = IndexSchemaDefinitions.BuildLegacyNaturalLanguageSemanticKey(tenantId, "mu-1"),
        };

    /// <summary>Minimal in-memory Dapr state double for activity-level real-store purge evidence.</summary>
    private sealed class InMemoryDaprStateStore
    {
        public const string StoreName = "statestore";

        private readonly Dictionary<string, (object? Value, string Etag)> _entries = new(StringComparer.Ordinal);
        private long _etagSequence;

        public bool ContainsKey(string key) => _entries.ContainsKey(key);

        public DaprClient CreateClient()
        {
            DaprClient client = Substitute.For<DaprClient>();
            SetupType<Dictionary<string, DaprObservedEventTypeStore.ObservationCounter>>(client);
            SetupType<List<string>>(client);
            SetupType<string>(client);

            _ = client.DeleteStateAsync(
                    StoreName, Arg.Any<string>(), Arg.Any<StateOptions?>(), Arg.Any<IReadOnlyDictionary<string, string>?>(), Arg.Any<CancellationToken>())
                .Returns(ci =>
                {
                    _ = _entries.Remove(ci.ArgAt<string>(1));
                    return Task.CompletedTask;
                });

            _ = client.TryDeleteStateAsync(
                    StoreName, Arg.Any<string>(), Arg.Any<string>(), Arg.Any<StateOptions?>(), Arg.Any<IReadOnlyDictionary<string, string>?>(), Arg.Any<CancellationToken>())
                .Returns(ci =>
                {
                    string key = ci.ArgAt<string>(1);
                    string etag = ci.ArgAt<string>(2);
                    string current = _entries.TryGetValue(key, out (object? Value, string Etag) entry) ? entry.Etag : string.Empty;
                    if (!string.Equals(etag, current, StringComparison.Ordinal))
                    {
                        return Task.FromResult(false);
                    }

                    _ = _entries.Remove(key);
                    return Task.FromResult(true);
                });

            return client;
        }

        private string NextEtag() => (++_etagSequence).ToString(System.Globalization.CultureInfo.InvariantCulture);

        private void SetupType<T>(DaprClient client)
            where T : class
        {
            _ = client.GetStateAndETagAsync<T?>(
                    StoreName, Arg.Any<string>(), Arg.Any<ConsistencyMode?>(), Arg.Any<IReadOnlyDictionary<string, string>?>(), Arg.Any<CancellationToken>())
                .Returns(ci =>
                {
                    string key = ci.ArgAt<string>(1);
                    return _entries.TryGetValue(key, out (object? Value, string Etag) entry)
                        ? ((T?)entry.Value, entry.Etag)
                        : (default, string.Empty);
                });

            _ = client.GetStateAsync<T?>(
                    StoreName, Arg.Any<string>(), Arg.Any<ConsistencyMode?>(), Arg.Any<IReadOnlyDictionary<string, string>?>(), Arg.Any<CancellationToken>())
                .Returns(ci =>
                {
                    string key = ci.ArgAt<string>(1);
                    return _entries.TryGetValue(key, out (object? Value, string Etag) entry) ? (T?)entry.Value : default;
                });

            _ = client.TrySaveStateAsync(
                    StoreName, Arg.Any<string>(), Arg.Any<T>(), Arg.Any<string>(), Arg.Any<StateOptions?>(), Arg.Any<IReadOnlyDictionary<string, string>?>(), Arg.Any<CancellationToken>())
                .Returns(ci =>
                {
                    string key = ci.ArgAt<string>(1);
                    T value = ci.ArgAt<T>(2);
                    string etag = ci.ArgAt<string>(3);
                    string current = _entries.TryGetValue(key, out (object? Value, string Etag) entry) ? entry.Etag : string.Empty;
                    if (!string.Equals(etag, current, StringComparison.Ordinal))
                    {
                        return Task.FromResult(false);
                    }

                    _entries[key] = (value, NextEtag());
                    return Task.FromResult(true);
                });
        }
    }
}
