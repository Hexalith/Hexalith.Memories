// <copyright file="SourceUriMemoryUnitLookupTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Tests.Ingestion;

using Hexalith.Memories.EventStore;
using Hexalith.Memories.Server.Activities.Ingestion;
using Hexalith.Memories.Server.Ingestion;

using NSubstitute;
using NSubstitute.ExceptionExtensions;

using Shouldly;

using StackExchange.Redis;

/// <summary>
/// Story 18.5 (AC2/AC3/AC6) — the lookup seam reads the permanent dedup record by exact key, excludes the
/// transient reservation marker, and lets backend failures propagate (so the endpoint never degrades a Redis
/// outage to a false not-found). Mirrors <see cref="IngestDedupReservationTests"/>' substitute-based shape.
/// </summary>
public class SourceUriMemoryUnitLookupTests
{
    private const string Tenant = "tenant-1";
    private const string Case = "case-1";
    private const string SourceUri = "file:///doc.pdf";

    [Fact]
    public async Task ResolveMemoryUnitIdAsync_KeyHoldsMemoryUnitId_ReturnsIt()
    {
        (IDatabase db, IConnectionMultiplexer redis) = CreateRedis();
        string key = DedupKeyBuilder.BuildKey(Tenant, Case, SourceUri);
        db.StringGetAsync(key).Returns((RedisValue)"mu-123");
        SourceUriMemoryUnitLookup lookup = new(redis);

        string? id = await lookup.ResolveMemoryUnitIdAsync(Tenant, Case, SourceUri, CancellationToken.None);

        id.ShouldBe("mu-123");
    }

    [Fact]
    public async Task ResolveMemoryUnitIdAsync_KeyMissing_ReturnsNull()
    {
        (IDatabase db, IConnectionMultiplexer redis) = CreateRedis();
        db.StringGetAsync(Arg.Any<RedisKey>(), Arg.Any<CommandFlags>()).Returns(RedisValue.Null);
        SourceUriMemoryUnitLookup lookup = new(redis);

        string? id = await lookup.ResolveMemoryUnitIdAsync(Tenant, Case, SourceUri, CancellationToken.None);

        id.ShouldBeNull();
    }

    [Fact]
    public async Task ResolveMemoryUnitIdAsync_KeyHoldsTransientReservationMarker_ReturnsNull()
    {
        // AC3 — the EventStore preflight transiently writes "reserved" to the permanent dedup key; it must
        // never be handed back as if it were a committed MemoryUnitId.
        (IDatabase db, IConnectionMultiplexer redis) = CreateRedis();
        string key = DedupKeyBuilder.BuildKey(Tenant, Case, SourceUri);
        db.StringGetAsync(key).Returns((RedisValue)PreflightDedupReservation.ReservedValue);
        SourceUriMemoryUnitLookup lookup = new(redis);

        string? id = await lookup.ResolveMemoryUnitIdAsync(Tenant, Case, SourceUri, CancellationToken.None);

        id.ShouldBeNull();
    }

    [Fact]
    public async Task ResolveMemoryUnitIdAsync_BuildsExactDedupKey_AndDoesNotReimplementHash()
    {
        (IDatabase db, IConnectionMultiplexer redis) = CreateRedis();
        SourceUriMemoryUnitLookup lookup = new(redis);

        await lookup.ResolveMemoryUnitIdAsync(Tenant, Case, SourceUri, CancellationToken.None);

        string expectedKey = DedupKeyBuilder.BuildKey(Tenant, Case, SourceUri);
        await db.Received(1).StringGetAsync(expectedKey, Arg.Any<CommandFlags>());
    }

    [Fact]
    public async Task ResolveMemoryUnitIdAsync_DifferentTenant_MissesViaDistinctKey()
    {
        // AC5 — tenant isolation is structural: the key embeds the tenant, so a lookup under tenant-2 for a
        // URI committed under tenant-1 reads a different (absent) key and returns not-found.
        (IDatabase db, IConnectionMultiplexer redis) = CreateRedis();
        string tenant1Key = DedupKeyBuilder.BuildKey(Tenant, Case, SourceUri);
        db.StringGetAsync(tenant1Key).Returns((RedisValue)"mu-tenant1");
        SourceUriMemoryUnitLookup lookup = new(redis);

        string? underTenant1 = await lookup.ResolveMemoryUnitIdAsync(Tenant, Case, SourceUri, CancellationToken.None);
        string? underTenant2 = await lookup.ResolveMemoryUnitIdAsync("tenant-2", Case, SourceUri, CancellationToken.None);

        underTenant1.ShouldBe("mu-tenant1");
        underTenant2.ShouldBeNull();
    }

    [Fact]
    public async Task ResolveMemoryUnitIdAsync_DifferentCase_MissesViaDistinctKey()
    {
        // AC5 — case isolation is structural too: the key embeds the case, so a lookup under case-2 for a URI
        // committed under case-1 reads a different (absent) key and returns not-found.
        (IDatabase db, IConnectionMultiplexer redis) = CreateRedis();
        string case1Key = DedupKeyBuilder.BuildKey(Tenant, Case, SourceUri);
        db.StringGetAsync(case1Key).Returns((RedisValue)"mu-case1");
        SourceUriMemoryUnitLookup lookup = new(redis);

        string? underCase1 = await lookup.ResolveMemoryUnitIdAsync(Tenant, Case, SourceUri, CancellationToken.None);
        string? underCase2 = await lookup.ResolveMemoryUnitIdAsync(Tenant, "case-2", SourceUri, CancellationToken.None);

        underCase1.ShouldBe("mu-case1");
        underCase2.ShouldBeNull();
    }

    [Fact]
    public void Constructor_NullRedis_ThrowsArgumentNullException() =>
        Should.Throw<ArgumentNullException>(() => new SourceUriMemoryUnitLookup(null!));

    [Fact]
    public async Task ResolveMemoryUnitIdAsync_RedisServerError_Propagates()
    {
        // AC6 — propagation is not limited to a connection failure: any RedisException subtype (here a server
        // error) must surface so the endpoint maps it to a backend error rather than a false not-found.
        (IDatabase db, IConnectionMultiplexer redis) = CreateRedis();
        db.StringGetAsync(Arg.Any<RedisKey>(), Arg.Any<CommandFlags>())
            .ThrowsAsync(Hexalith.Memories.Server.Tests.RedisExceptionFactory.CreateServerException("LOADING Redis is loading the dataset in memory"));
        SourceUriMemoryUnitLookup lookup = new(redis);

        await Should.ThrowAsync<RedisException>(() =>
            lookup.ResolveMemoryUnitIdAsync(Tenant, Case, SourceUri, CancellationToken.None));
    }

    [Fact]
    public async Task ResolveMemoryUnitIdAsync_RedisConnectionDown_Propagates()
    {
        // AC6 — a backend I/O failure must NOT be swallowed into null; it propagates so the endpoint can map
        // it to a structured backend error rather than a false 404.
        (IDatabase db, IConnectionMultiplexer redis) = CreateRedis();
        db.StringGetAsync(Arg.Any<RedisKey>(), Arg.Any<CommandFlags>())
            .ThrowsAsync(new RedisConnectionException(ConnectionFailureType.UnableToConnect, StackExchange.Redis.CommandFlags.None, "down"));
        SourceUriMemoryUnitLookup lookup = new(redis);

        await Should.ThrowAsync<RedisException>(() =>
            lookup.ResolveMemoryUnitIdAsync(Tenant, Case, SourceUri, CancellationToken.None));
    }

    [Theory]
    [InlineData("", Case, SourceUri)]
    [InlineData("   ", Case, SourceUri)]
    [InlineData(Tenant, "", SourceUri)]
    [InlineData(Tenant, Case, "  ")]
    public async Task ResolveMemoryUnitIdAsync_BlankInputs_ThrowArgumentException(string tenantId, string caseId, string sourceUri)
    {
        (IDatabase _, IConnectionMultiplexer redis) = CreateRedis();
        SourceUriMemoryUnitLookup lookup = new(redis);

        await Should.ThrowAsync<ArgumentException>(() =>
            lookup.ResolveMemoryUnitIdAsync(tenantId, caseId, sourceUri, CancellationToken.None));
    }

    private static (IDatabase Db, IConnectionMultiplexer Redis) CreateRedis()
    {
        IDatabase db = Substitute.For<IDatabase>();
        IConnectionMultiplexer redis = Substitute.For<IConnectionMultiplexer>();
        redis.GetDatabase(Arg.Any<int>(), Arg.Any<object>()).Returns(db);
        return (db, redis);
    }
}
