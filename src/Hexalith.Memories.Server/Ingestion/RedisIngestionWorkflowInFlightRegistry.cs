// <copyright file="RedisIngestionWorkflowInFlightRegistry.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Ingestion;

using StackExchange.Redis;

/// <summary>Redis implementation of <see cref="IIngestionWorkflowInFlightRegistry"/>.</summary>
internal sealed class RedisIngestionWorkflowInFlightRegistry : IIngestionWorkflowInFlightRegistry
{
    internal const string RegistryKey = "ingestion-workflow:in-flight";
    internal const string InstanceMemberLookupKey = "ingestion-workflow:in-flight:members";
    internal const string InitializedKey = "ingestion-workflow:in-flight:initialized";
    internal const char MemberSeparator = '\u001f';

    private readonly IConnectionMultiplexer _redis;

    /// <summary>Initializes a new instance of the <see cref="RedisIngestionWorkflowInFlightRegistry"/> class.</summary>
    /// <param name="redis">The Redis connection multiplexer.</param>
    /// <param name="timeProvider">The time provider.</param>
    public RedisIngestionWorkflowInFlightRegistry(
        [FromKeyedServices("redis")] IConnectionMultiplexer redis,
        TimeProvider? timeProvider = null)
    {
        ArgumentNullException.ThrowIfNull(redis);
        _redis = redis;
        _ = timeProvider;
    }

    /// <inheritdoc />
    public async Task TrackAsync(IngestionWorkflowInFlightEntry entry, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(entry);
        ArgumentException.ThrowIfNullOrWhiteSpace(entry.TenantId);
        ArgumentException.ThrowIfNullOrWhiteSpace(entry.InstanceId);
        cancellationToken.ThrowIfCancellationRequested();

        IDatabase db = _redis.GetDatabase();
        RedisValue member = CreateMember(entry.TenantId, entry.InstanceId);
        double score = entry.TrackedAt.ToUnixTimeMilliseconds();
        _ = await db.SortedSetAddAsync(RegistryKey, member, score).ConfigureAwait(false);
        _ = await db.HashSetAsync(InstanceMemberLookupKey, entry.InstanceId, member).ConfigureAwait(false);
        _ = await db.StringSetAsync(InitializedKey, "1").ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<IngestionWorkflowInFlightEntry>> ListAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        IDatabase db = _redis.GetDatabase();
        SortedSetEntry[] entries = await db
            .SortedSetRangeByRankWithScoresAsync(RegistryKey, 0, -1, Order.Ascending)
            .ConfigureAwait(false);

        if (entries.Length == 0)
        {
            return [];
        }

        List<IngestionWorkflowInFlightEntry> result = new(entries.Length);
        List<RedisValue> corruptMembers = [];
        foreach (SortedSetEntry entry in entries)
        {
            if (TryParseMember(entry.Element, out string? tenantId, out string? instanceId))
            {
                result.Add(new IngestionWorkflowInFlightEntry(
                    tenantId!,
                    instanceId!,
                    DateTimeOffset.FromUnixTimeMilliseconds(Convert.ToInt64(entry.Score))));
            }
            else
            {
                corruptMembers.Add(entry.Element);
            }
        }

        if (corruptMembers.Count > 0)
        {
            _ = await db.SortedSetRemoveAsync(RegistryKey, [.. corruptMembers]).ConfigureAwait(false);
        }

        return result;
    }

    /// <inheritdoc />
    public async Task<bool> IsInitializedAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        IDatabase db = _redis.GetDatabase();
        return await db.KeyExistsAsync(InitializedKey).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task MarkInitializedAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        IDatabase db = _redis.GetDatabase();
        _ = await db.StringSetAsync(InitializedKey, "1").ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task RemoveAsync(string instanceId, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(instanceId);
        cancellationToken.ThrowIfCancellationRequested();

        IDatabase db = _redis.GetDatabase();
        RedisValue member = await db.HashGetAsync(InstanceMemberLookupKey, instanceId).ConfigureAwait(false);
        if (member.HasValue)
        {
            _ = await db.SortedSetRemoveAsync(RegistryKey, member).ConfigureAwait(false);
        }
        else
        {
            RedisValue[] fallbackMembers = await FindMembersByInstanceIdAsync(db, instanceId).ConfigureAwait(false);
            if (fallbackMembers.Length > 0)
            {
                _ = await db.SortedSetRemoveAsync(RegistryKey, fallbackMembers).ConfigureAwait(false);
            }
        }

        _ = await db.HashDeleteAsync(InstanceMemberLookupKey, instanceId).ConfigureAwait(false);
    }

    internal static RedisValue CreateMember(string tenantId, string instanceId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        ArgumentException.ThrowIfNullOrWhiteSpace(instanceId);

        if (tenantId.Contains(MemberSeparator) || instanceId.Contains(MemberSeparator))
        {
            throw new ArgumentException("Tracked ingestion workflow identifiers cannot contain the registry separator.");
        }

        return $"{tenantId}{MemberSeparator}{instanceId}";
    }

    internal static bool TryParseMember(RedisValue member, out string? tenantId, out string? instanceId)
    {
        string value = member.ToString();
        int separatorIndex = value.IndexOf(MemberSeparator, StringComparison.Ordinal);
        if (separatorIndex <= 0 || separatorIndex == value.Length - 1)
        {
            tenantId = null;
            instanceId = null;
            return false;
        }

        tenantId = value[..separatorIndex];
        instanceId = value[(separatorIndex + 1)..];
        return true;
    }

    private static async Task<RedisValue[]> FindMembersByInstanceIdAsync(IDatabase db, string instanceId)
    {
        RedisValue[] members = await db
            .SortedSetRangeByRankAsync(RegistryKey, 0, -1, Order.Ascending)
            .ConfigureAwait(false);

        if (members.Length == 0)
        {
            return [];
        }

        return members
            .Where(member => TryParseMember(member, out _, out string? parsedInstanceId)
                && string.Equals(parsedInstanceId, instanceId, StringComparison.Ordinal))
            .ToArray();
    }
}
