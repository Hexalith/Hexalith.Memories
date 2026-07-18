// <copyright file="RedisSearchIndexMaintenanceAdapterTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Tests.EventStoreIntegration;

using System.Text.Json;

using Hexalith.Memories.Contracts.V1;
using Hexalith.Memories.Server.EventStoreIntegration;
using Hexalith.Memories.Server.Infrastructure;

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
            IndexSchemaDefinitions.BuildSyntacticKey("tenants-index", "t-1"),
            Arg.Is<HashEntry[]>(entries =>
                HasEntry(entries!, "id", "t-1")
                && HasEntry(entries!, "tenantId", "tenants-index")
                && HasEntry(entries!, "content", "Acme t-1")
                && HasEntry(entries!, "sourceUri", "tenant:t-1")
                && HasEntry(entries!, "sourceUriText", "tenant:t-1")
                && HasEntry(entries!, "sourceType", "event")
                && HasEntry(entries!, "sourceTypeText", "event")
                && HasEntry(entries!, "caseId", "case-1")
                && HasEntry(entries!, "cloudeventSubject", "t-1")
                && HasEntry(entries!, "metadataText", "status Active")
                && HasEntry(entries!, "attributeTags", "status=Active")
                && HasEntry(entries!, "metadataJson", JsonSerializer.Serialize(entry.Attributes, MemoriesJsonContext.Options))),
            Arg.Any<CommandFlags>());
    }

    // Story 23.7 (A34) AC7: the curated EventStore search-index path is reconciled onto the shared readiness
    // policy — it no longer creates the index per upsert (no FT.CREATE), and repeated upserts reuse a single
    // memoized FT.INFO verification instead of re-checking on every entry.
    [Fact]
    public async Task ApplyEntryChangedAsync_NeverCreatesIndex_AndReusesMemoizedReadiness()
    {
        IDatabase db = Substitute.For<IDatabase>();
        IConnectionMultiplexer redis = MockMultiplexer(db);
        ITenantIndexReadinessVerifier verifier =
            new TenantIndexReadinessVerifier(NullLogger<TenantIndexReadinessVerifier>.Instance);
        RedisSearchIndexMaintenanceAdapter adapter =
            new(redis, NullLogger<RedisSearchIndexMaintenanceAdapter>.Instance, verifier);

        SearchIndexEntryChanged first = new() { TenantId = "tenants-index", AggregateId = "t-1", Text = "Acme t-1" };
        SearchIndexEntryChanged second = new() { TenantId = "tenants-index", AggregateId = "t-2", Text = "Acme t-2" };

        await adapter.ApplyEntryChangedAsync("tenants-index", "tenant:t-1", first, "case-1", CancellationToken.None);
        await adapter.ApplyEntryChangedAsync("tenants-index", "tenant:t-2", second, "case-1", CancellationToken.None);

        db.DidNotReceive().Execute("FT.CREATE", Arg.Any<object[]>());
        db.Received(1).Execute("FT.INFO", Arg.Any<object[]>());
        await db.Received(2).HashSetAsync(Arg.Any<RedisKey>(), Arg.Any<HashEntry[]>(), Arg.Any<CommandFlags>());
    }

    [Fact]
    public async Task ApplyEntryChangedAsync_MissingIndex_FailsWithoutCreating()
    {
        IDatabase db = Substitute.For<IDatabase>();
        db.Execute(Arg.Any<string>(), Arg.Any<object[]>())
            .Returns(_ => throw new RedisServerException("Unknown index name"));
        db.Execute(Arg.Any<string>(), Arg.Any<ICollection<object>>(), Arg.Any<CommandFlags>())
            .Returns(_ => throw new RedisServerException("Unknown index name"));
        IConnectionMultiplexer redis = Substitute.For<IConnectionMultiplexer>();
        redis.GetDatabase(Arg.Any<int>(), Arg.Any<object>()).Returns(db);
        RedisSearchIndexMaintenanceAdapter adapter = new(redis, NullLogger<RedisSearchIndexMaintenanceAdapter>.Instance);

        SearchIndexEntryChanged entry = new() { TenantId = "tenants-index", AggregateId = "t-1", Text = "Acme t-1" };

        await Should.ThrowAsync<TenantIndexNotProvisionedException>(
            () => adapter.ApplyEntryChangedAsync("tenants-index", "tenant:t-1", entry, null, CancellationToken.None));

        await db.DidNotReceive().HashSetAsync(Arg.Any<RedisKey>(), Arg.Any<HashEntry[]>(), Arg.Any<CommandFlags>());
    }

    [Fact]
    public async Task ApplyEntryRemovedAsync_DeletesByAggregateKey()
    {
        IDatabase db = Substitute.For<IDatabase>();
        IConnectionMultiplexer redis = MockMultiplexer(db);
        RedisSearchIndexMaintenanceAdapter adapter = new(redis, NullLogger<RedisSearchIndexMaintenanceAdapter>.Instance);

        SearchIndexEntryRemoved entry = new() { TenantId = "tenants-index", AggregateId = "t-9" };

        await adapter.ApplyEntryRemovedAsync("tenants-index", entry, CancellationToken.None);

        await db.Received(1).KeyDeleteAsync(IndexSchemaDefinitions.BuildSyntacticKey("tenants-index", "t-9"), Arg.Any<CommandFlags>());
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
        // The index already exists in steady state (provisioned by TenantProvisioningWorkflow); the adapter verifies
        // the provisioned schema with FT.INFO before writing the curated hash.
        RedisResult Execute(string command)
            => command switch
            {
                "FT.CREATE" => throw new RedisServerException("Index already exists"),
                "FT.INFO" => CreateExistingIndexInfoResult(),
                _ => RedisResult.Create(new RedisValue("OK")),
            };

        db.Execute(Arg.Any<string>(), Arg.Any<object[]>())
            .Returns(call => Execute(call.ArgAt<string>(0)));
        db.Execute(Arg.Any<string>(), Arg.Any<ICollection<object>>(), Arg.Any<CommandFlags>())
            .Returns(call => Execute(call.ArgAt<string>(0)));

        IConnectionMultiplexer redis = Substitute.For<IConnectionMultiplexer>();
        redis.GetDatabase(Arg.Any<int>(), Arg.Any<object>()).Returns(db);
        return redis;
    }

    private static RedisResult CreateExistingIndexInfoResult() => RedisResult.Create(
    [
        RedisResult.Create(new RedisValue("index_definition")),
        RedisResult.Create(
        [
            RedisResult.Create(new RedisValue("prefixes")),
            RedisResult.Create([RedisResult.Create(new RedisValue(IndexSchemaDefinitions.GetSyntacticKeyPrefix("tenants-index")))]),
        ]),
        RedisResult.Create(new RedisValue("attributes")),
        RedisResult.Create(
        [
            CreateAttribute("content", "TEXT"),
            CreateAttribute("sourceUriText", "TEXT"),
            CreateAttribute("sourceTypeText", "TEXT"),
            CreateAttribute("metadataText", "TEXT"),
            CreateAttribute("sourceUri", "TAG"),
            CreateAttribute("sourceType", "TAG"),
            CreateAttribute("contentHash", "TAG"),
            CreateAttribute("caseId", "TAG"),
            CreateAttribute("cloudeventSubject", "TAG"),
            CreateAttribute("attributeTags", "TAG"),
            CreateAttribute("embeddingProvider", "TAG"),
        ]),
    ]);

    private static RedisResult CreateAttribute(string identifier, string type) => RedisResult.Create(
    [
        RedisResult.Create(new RedisValue("identifier")),
        RedisResult.Create(new RedisValue(identifier)),
        RedisResult.Create(new RedisValue("attribute")),
        RedisResult.Create(new RedisValue(identifier)),
        RedisResult.Create(new RedisValue("type")),
        RedisResult.Create(new RedisValue(type)),
    ]);

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
