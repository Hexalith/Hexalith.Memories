// <copyright file="RedisSearchIndexMaintenanceAdapterTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Tests.EventStoreIntegration;

using System.Text.Json;

using Hexalith.Memories.Contracts.V1;
using Hexalith.Memories.Server.EventStoreIntegration;

using Microsoft.Extensions.Logging.Abstractions;

using NSubstitute;

using Shouldly;

using StackExchange.Redis;

public sealed class RedisSearchIndexMaintenanceAdapterTests
{
    [Fact]
    public async Task ApplyEntryChangedAsync_WritesSearchableCuratedHash_KeyedByAggregateId()
    {
        IDatabase db = Substitute.For<IDatabase>();
        IConnectionMultiplexer redis = MockMultiplexer(db);
        RedisSearchIndexMaintenanceAdapter adapter = new(redis, NullLogger<RedisSearchIndexMaintenanceAdapter>.Instance);

        SearchIndexEntryChanged entry = new()
        {
            TenantId = "tenants-index",
            AggregateId = "t-1",
            Text = "Acme t-1",
            Attributes = new Dictionary<string, string>(StringComparer.Ordinal) { ["status"] = "Active" },
        };

        await adapter.ApplyEntryChangedAsync("tenants-index", "tenant:t-1", entry, "case-1", CancellationToken.None);

        // Deterministic key by (index, aggregateId) → re-publishing the same aggregate overwrites (upsert).
        // Fields mirror the syntactic schema so the existing SyntacticSearchService returns it unchanged:
        // content is the BM25 surface, sourceUri is the verbatim cloudevent.id the BFF parses back.
        await db.Received(1).HashSetAsync(
            "tenants-index:mu:t-1",
            Arg.Is<HashEntry[]>(entries =>
                HasEntry(entries, "id", "t-1")
                && HasEntry(entries, "tenantId", "tenants-index")
                && HasEntry(entries, "content", "Acme t-1")
                && HasEntry(entries, "sourceUri", "tenant:t-1")
                && HasEntry(entries, "sourceUriText", "tenant:t-1")
                && HasEntry(entries, "sourceType", "event")
                && HasEntry(entries, "sourceTypeText", "event")
                && HasEntry(entries, "caseId", "case-1")
                && HasEntry(entries, "cloudeventSubject", "t-1")
                && HasEntry(entries, "metadataText", "status Active")
                && HasEntry(entries, "metadataJson", JsonSerializer.Serialize(entry.Attributes, MemoriesJsonContext.Options))),
            Arg.Any<CommandFlags>());
    }

    [Fact]
    public async Task ApplyEntryRemovedAsync_DeletesByAggregateKey()
    {
        IDatabase db = Substitute.For<IDatabase>();
        IConnectionMultiplexer redis = MockMultiplexer(db);
        RedisSearchIndexMaintenanceAdapter adapter = new(redis, NullLogger<RedisSearchIndexMaintenanceAdapter>.Instance);

        SearchIndexEntryRemoved entry = new() { TenantId = "tenants-index", AggregateId = "t-9" };

        await adapter.ApplyEntryRemovedAsync("tenants-index", entry, CancellationToken.None);

        await db.Received(1).KeyDeleteAsync("tenants-index:mu:t-9", Arg.Any<CommandFlags>());
    }

    [Fact]
    public async Task ApplyEntryChangedAsync_InvalidTenantId_Throws()
    {
        IDatabase db = Substitute.For<IDatabase>();
        IConnectionMultiplexer redis = MockMultiplexer(db);
        RedisSearchIndexMaintenanceAdapter adapter = new(redis, NullLogger<RedisSearchIndexMaintenanceAdapter>.Instance);

        SearchIndexEntryChanged entry = new() { TenantId = "x", AggregateId = "t-1", Text = "Acme" };

        await Should.ThrowAsync<ArgumentException>(
            () => adapter.ApplyEntryChangedAsync("bad tenant; DROP", "tenant:t-1", entry, null, CancellationToken.None));
    }

    [Fact]
    public async Task ApplyEntryChangedAsync_EmptyAggregateId_Throws()
    {
        IDatabase db = Substitute.For<IDatabase>();
        IConnectionMultiplexer redis = MockMultiplexer(db);
        RedisSearchIndexMaintenanceAdapter adapter = new(redis, NullLogger<RedisSearchIndexMaintenanceAdapter>.Instance);

        SearchIndexEntryChanged entry = new() { TenantId = "tenants-index", AggregateId = " ", Text = "Acme" };

        await Should.ThrowAsync<ArgumentException>(
            () => adapter.ApplyEntryChangedAsync("tenants-index", "tenant:t-1", entry, null, CancellationToken.None));
    }

    private static IConnectionMultiplexer MockMultiplexer(IDatabase db)
    {
        // The index already exists in steady state (provisioned by TenantProvisioningWorkflow); the adapter's
        // create-if-missing catches "Index already exists" and proceeds to the hash write.
        RedisResult Execute(string command)
            => command == "FT.CREATE"
                ? throw new RedisServerException("Index already exists")
                : RedisResult.Create(new RedisValue("OK"));

        db.Execute(Arg.Any<string>(), Arg.Any<object[]>())
            .Returns(call => Execute(call.ArgAt<string>(0)));
        db.Execute(Arg.Any<string>(), Arg.Any<ICollection<object>>(), Arg.Any<CommandFlags>())
            .Returns(call => Execute(call.ArgAt<string>(0)));

        IConnectionMultiplexer redis = Substitute.For<IConnectionMultiplexer>();
        redis.GetDatabase(Arg.Any<int>(), Arg.Any<object>()).Returns(db);
        return redis;
    }

    private static bool HasEntry(IEnumerable<HashEntry> entries, string name, string value)
    {
        foreach (HashEntry entry in entries)
        {
            if (entry.Name == name && entry.Value.ToString() == value)
            {
                return true;
            }
        }

        return false;
    }
}
