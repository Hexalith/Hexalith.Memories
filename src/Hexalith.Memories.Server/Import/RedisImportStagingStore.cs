// <copyright file="RedisImportStagingStore.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Import;

using Microsoft.Extensions.DependencyInjection;

using StackExchange.Redis;

/// <summary>
/// Redis-backed <see cref="IImportStagingStore"/> (Story 26.2). The staged payload lives under a
/// tenant-prefixed key with a bounded TTL so an abandoned or crashed restore self-cleans. The payload is
/// stored as a single binary value bounded by the endpoint's <c>RequestSizeLimitAttribute</c> ceiling; for
/// corpora larger than that ceiling, restore case-by-case (a chunked/blob-backed staging store is the
/// documented follow-up — see docs/operations/backup-restore.md).
/// </summary>
internal sealed class RedisImportStagingStore : IImportStagingStore
{
    /// <summary>The staging key time-to-live: long enough for a large restore to drain, short enough to self-clean.</summary>
    internal static readonly TimeSpan StagingTtl = TimeSpan.FromHours(6);

    private readonly IConnectionMultiplexer _redis;

    /// <summary>Initializes a new instance of the <see cref="RedisImportStagingStore"/> class.</summary>
    /// <param name="redis">The Redis connection multiplexer (data plane).</param>
    public RedisImportStagingStore([FromKeyedServices("redis")] IConnectionMultiplexer redis)
        => _redis = redis;

    /// <inheritdoc/>
    public async Task<string> StageAsync(string tenantId, string instanceId, byte[] payload, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        ArgumentException.ThrowIfNullOrWhiteSpace(instanceId);
        ArgumentNullException.ThrowIfNull(payload);
        cancellationToken.ThrowIfCancellationRequested();

        string key = BuildKey(tenantId, instanceId);
        IDatabase db = _redis.GetDatabase();
        await db.StringSetAsync(key, payload, StagingTtl).ConfigureAwait(false);
        return key;
    }

    /// <inheritdoc/>
    public async Task<byte[]?> RetrieveAsync(string stagingKey, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(stagingKey);
        cancellationToken.ThrowIfCancellationRequested();

        IDatabase db = _redis.GetDatabase();
        RedisValue value = await db.StringGetAsync(stagingKey).ConfigureAwait(false);
        return value.IsNull ? null : (byte[]?)value;
    }

    /// <inheritdoc/>
    public async Task DeleteAsync(string stagingKey, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(stagingKey);
        cancellationToken.ThrowIfCancellationRequested();

        IDatabase db = _redis.GetDatabase();
        _ = await db.KeyDeleteAsync(stagingKey).ConfigureAwait(false);
    }

    /// <summary>Builds the tenant-prefixed staging key.</summary>
    /// <param name="tenantId">The tenant identifier.</param>
    /// <param name="instanceId">The restore workflow instance id.</param>
    /// <returns>The staging key.</returns>
    internal static string BuildKey(string tenantId, string instanceId)
        => $"{tenantId}:import:staging:{instanceId}";
}
