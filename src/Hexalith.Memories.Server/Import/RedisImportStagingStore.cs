// <copyright file="RedisImportStagingStore.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Import;

using System.Globalization;
using System.Security.Cryptography;

using Microsoft.Extensions.DependencyInjection;

using StackExchange.Redis;

/// <summary>
/// Redis-backed <see cref="IImportStagingStore"/> (Story 26.2). Payloads are split into bounded chunks so a
/// 512 MB upload is never represented by one large managed byte array or one Redis bulk string.
/// </summary>
internal sealed class RedisImportStagingStore : IImportStagingStore
{
    /// <summary>The staging key time-to-live: long enough for a large restore to drain, short enough to self-clean.</summary>
    internal static readonly TimeSpan StagingTtl = TimeSpan.FromHours(12);

    private const int ChunkBytes = 1024 * 1024;

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

        using MemoryStream stream = new(payload, writable: false);
        return await StageAsync(tenantId, instanceId, stream, payload.LongLength, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task<string> StageAsync(
        string tenantId,
        string instanceId,
        Stream payload,
        long maxBytes,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        ArgumentException.ThrowIfNullOrWhiteSpace(instanceId);
        ArgumentNullException.ThrowIfNull(payload);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maxBytes);

        string key = BuildKey(tenantId, instanceId);
        IDatabase db = _redis.GetDatabase();
        byte[] buffer = new byte[ChunkBytes];
        int chunkCount = 0;
        long length = 0;
        using IncrementalHash hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);

        try
        {
            while (true)
            {
                int read = await payload.ReadAsync(buffer, cancellationToken).ConfigureAwait(false);
                if (read == 0)
                {
                    break;
                }

                length += read;
                if (length > maxBytes)
                {
                    throw new InvalidDataException($"Import payload exceeds the {maxBytes} byte limit.");
                }

                hash.AppendData(buffer, 0, read);
                await db.StringSetAsync(
                    BuildChunkKey(key, chunkCount++),
                    buffer.AsMemory(0, read),
                    StagingTtl).ConfigureAwait(false);
            }

            HashEntry[] metadata =
            [
                new HashEntry("chunkCount", chunkCount),
                new HashEntry("length", length),
                new HashEntry("sha256", Convert.ToHexString(hash.GetHashAndReset())),
                new HashEntry("instanceId", instanceId),
                new HashEntry("started", 0),
                new HashEntry("lastRenewed", DateTimeOffset.UtcNow.ToUnixTimeSeconds()),
            ];
            await db.HashSetAsync(key, metadata).ConfigureAwait(false);
            _ = await db.KeyExpireAsync(key, StagingTtl).ConfigureAwait(false);
            return key;
        }
        catch
        {
            await DeleteChunkKeysAsync(db, key, chunkCount).ConfigureAwait(false);
            _ = await db.KeyDeleteAsync(key).ConfigureAwait(false);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<byte[]?> RetrieveAsync(string stagingKey, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(stagingKey);
        cancellationToken.ThrowIfCancellationRequested();

        IDatabase db = _redis.GetDatabase();
        await using Stream? stream = await OpenReadAsync(stagingKey, cancellationToken).ConfigureAwait(false);
        if (stream is null)
        {
            return null;
        }

        if (stream.Length > int.MaxValue)
        {
            throw new InvalidDataException("Staged import is too large to materialize as a byte array.");
        }

        using MemoryStream buffer = new((int)stream.Length);
        await stream.CopyToAsync(buffer, cancellationToken).ConfigureAwait(false);
        return buffer.ToArray();
    }

    /// <inheritdoc/>
    public async Task<Stream?> OpenReadAsync(string stagingKey, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(stagingKey);
        cancellationToken.ThrowIfCancellationRequested();

        IDatabase db = _redis.GetDatabase();
        RedisValue[] values = await db.HashGetAsync(stagingKey, ["chunkCount", "length"]).ConfigureAwait(false);
        if (!values[0].HasValue || !values[1].HasValue)
        {
            return null;
        }

        if (!int.TryParse(values[0].ToString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int chunkCount)
            || !long.TryParse(values[1].ToString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out long length)
            || chunkCount < 0
            || length < 0)
        {
            throw new InvalidDataException($"Staging metadata for '{stagingKey}' is corrupt.");
        }

        return new RedisChunkReadStream(db, stagingKey, chunkCount, length);
    }

    /// <inheritdoc/>
    public async Task RenewAsync(string stagingKey, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(stagingKey);
        cancellationToken.ThrowIfCancellationRequested();

        IDatabase db = _redis.GetDatabase();
        RedisValue[] retention = await db.HashGetAsync(stagingKey, ["chunkCount", "lastRenewed"]).ConfigureAwait(false);
        RedisValue chunkCountValue = retention[0];
        if (!int.TryParse(chunkCountValue.ToString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int chunkCount))
        {
            throw new InvalidDataException($"Staging metadata for '{stagingKey}' is missing or corrupt.");
        }

        long now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        if (long.TryParse(retention[1].ToString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out long lastRenewed)
            && now - lastRenewed < TimeSpan.FromHours(1).TotalSeconds)
        {
            return;
        }

        _ = await db.HashSetAsync(stagingKey, "lastRenewed", now).ConfigureAwait(false);
        IBatch batch = db.CreateBatch();
        List<Task<bool>> expirations = new(chunkCount + 3)
        {
            batch.KeyExpireAsync(stagingKey, StagingTtl),
            batch.KeyExpireAsync(BuildReindexKey(stagingKey), StagingTtl),
        };
        for (int index = 0; index < chunkCount; index++)
        {
            expirations.Add(batch.KeyExpireAsync(BuildChunkKey(stagingKey, index), StagingTtl));
        }

        RedisValue[] lease = await db.HashGetAsync(stagingKey, ["leaseKey", "leaseValue"]).ConfigureAwait(false);
        if (lease[0].HasValue && lease[1].HasValue)
        {
            RedisValue current = await db.StringGetAsync(lease[0].ToString()).ConfigureAwait(false);
            if (current == lease[1])
            {
                expirations.Add(batch.KeyExpireAsync(lease[0].ToString(), StagingTtl));
            }
        }

        batch.Execute();
        _ = await Task.WhenAll(expirations).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task ResetReindexIdsAsync(string stagingKey, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(stagingKey);
        cancellationToken.ThrowIfCancellationRequested();
        _ = await _redis.GetDatabase().KeyDeleteAsync(BuildReindexKey(stagingKey)).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task AppendReindexIdsAsync(
        string stagingKey,
        IReadOnlyList<string> memoryUnitIds,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(stagingKey);
        ArgumentNullException.ThrowIfNull(memoryUnitIds);
        cancellationToken.ThrowIfCancellationRequested();

        IDatabase db = _redis.GetDatabase();
        string key = BuildReindexKey(stagingKey);
        const int batchSize = 1000;
        for (int offset = 0; offset < memoryUnitIds.Count; offset += batchSize)
        {
            RedisValue[] values = memoryUnitIds
                .Skip(offset)
                .Take(batchSize)
                .Select(static id => (RedisValue)id)
                .ToArray();
            _ = await db.ListRightPushAsync(key, values).ConfigureAwait(false);
        }

        _ = await db.KeyExpireAsync(key, StagingTtl).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<string>> ReadReindexIdsAsync(
        string stagingKey,
        long offset,
        int count,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(stagingKey);
        ArgumentOutOfRangeException.ThrowIfNegative(offset);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(count);
        cancellationToken.ThrowIfCancellationRequested();

        IDatabase db = _redis.GetDatabase();
        RedisValue[] values = await db.ListRangeAsync(
            BuildReindexKey(stagingKey),
            offset,
            checked(offset + count - 1L)).ConfigureAwait(false);
        return values.Select(static value => value.ToString()).ToArray();
    }

    /// <inheritdoc/>
    public async Task<RestoreLeaseResult> AcquireRestoreLeaseAsync(
        string stagingKey,
        string tenantId,
        string? caseId,
        string instanceId,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(stagingKey);
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        ArgumentException.ThrowIfNullOrWhiteSpace(instanceId);
        cancellationToken.ThrowIfCancellationRequested();

        IDatabase db = _redis.GetDatabase();
        RedisValue hash = await db.HashGetAsync(stagingKey, "sha256").ConfigureAwait(false);
        if (!hash.HasValue)
        {
            throw new InvalidDataException($"Staging metadata for '{stagingKey}' has no content hash.");
        }

        string leaseKey = BuildLeaseKey(tenantId, caseId);
        string leaseValue = instanceId + "|" + hash.ToString();
        if (await db.StringSetAsync(leaseKey, leaseValue, StagingTtl, When.NotExists).ConfigureAwait(false))
        {
            try
            {
                await StoreLeaseMetadataAsync(db, stagingKey, leaseKey, leaseValue).ConfigureAwait(false);
                return new RestoreLeaseResult(true, instanceId, false);
            }
            catch
            {
                await ReleaseLeaseIfOwnedAsync(db, leaseKey, leaseValue).ConfigureAwait(false);
                throw;
            }
        }

        RedisValue existing = await db.StringGetAsync(leaseKey).ConfigureAwait(false);
        string existingText = existing.ToString();
        int separator = existingText.IndexOf('|', StringComparison.Ordinal);
        string existingInstance = separator > 0 ? existingText[..separator] : string.Empty;
        string existingHash = separator > 0 ? existingText[(separator + 1)..] : string.Empty;
        if (string.Equals(existingHash, hash.ToString(), StringComparison.Ordinal))
        {
            return new RestoreLeaseResult(false, existingInstance, true);
        }

        return new RestoreLeaseResult(false, existingInstance, false);
    }

    /// <inheritdoc/>
    public async Task<bool> OwnsRestoreLeaseAsync(string stagingKey, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        IDatabase db = _redis.GetDatabase();
        RedisValue[] lease = await db.HashGetAsync(stagingKey, ["leaseKey", "leaseValue"]).ConfigureAwait(false);
        return lease[0].HasValue
            && lease[1].HasValue
            && await db.StringGetAsync(lease[0].ToString()).ConfigureAwait(false) == lease[1];
    }

    /// <inheritdoc/>
    public async Task<bool> HasRestoreStartedAsync(string stagingKey, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        RedisValue value = await _redis.GetDatabase().HashGetAsync(stagingKey, "started").ConfigureAwait(false);
        return value == "1";
    }

    /// <inheritdoc/>
    public async Task MarkRestoreStartedAsync(string stagingKey, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _ = await _redis.GetDatabase().HashSetAsync(stagingKey, "started", 1).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task DeleteAsync(string stagingKey, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(stagingKey);
        cancellationToken.ThrowIfCancellationRequested();

        IDatabase db = _redis.GetDatabase();
        RedisValue[] metadata = await db.HashGetAsync(
            stagingKey,
            ["chunkCount", "leaseKey", "leaseValue"]).ConfigureAwait(false);
        if (int.TryParse(metadata[0].ToString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int chunkCount))
        {
            await DeleteChunkKeysAsync(db, stagingKey, chunkCount).ConfigureAwait(false);
        }

        _ = await db.KeyDeleteAsync(BuildReindexKey(stagingKey)).ConfigureAwait(false);
        if (metadata[1].HasValue && metadata[2].HasValue)
        {
            await ReleaseLeaseIfOwnedAsync(db, metadata[1].ToString(), metadata[2].ToString()).ConfigureAwait(false);
        }

        _ = await db.KeyDeleteAsync(stagingKey).ConfigureAwait(false);
    }

    /// <summary>Builds the tenant-prefixed staging key.</summary>
    /// <param name="tenantId">The tenant identifier.</param>
    /// <param name="instanceId">The restore workflow instance id.</param>
    /// <returns>The staging key.</returns>
    internal static string BuildKey(string tenantId, string instanceId)
        => $"{tenantId}:import:staging:{instanceId}";

    /// <summary>Builds one staged payload chunk key.</summary>
    internal static string BuildChunkKey(string stagingKey, int index)
        => $"{stagingKey}:chunk:{index.ToString(CultureInfo.InvariantCulture)}";

    private static string BuildLeaseKey(string tenantId, string? caseId)
    {
        _ = caseId;

        // Serialize all restore scopes within one tenant. A tenant restore overlaps every case target, so
        // separate tenant/case lease keys would permit destructive cross-scope concurrency.
        return $"{tenantId}:restore:lease";
    }

    private static string BuildReindexKey(string stagingKey) => stagingKey + ":reindex";

    private static async Task DeleteChunkKeysAsync(IDatabase db, string stagingKey, int chunkCount)
    {
        const int batchSize = 1000;
        for (int offset = 0; offset < chunkCount; offset += batchSize)
        {
            RedisKey[] keys = Enumerable.Range(offset, Math.Min(batchSize, chunkCount - offset))
                .Select(index => (RedisKey)BuildChunkKey(stagingKey, index))
                .ToArray();
            _ = await db.KeyDeleteAsync(keys).ConfigureAwait(false);
        }
    }

    private static async Task StoreLeaseMetadataAsync(
        IDatabase db,
        string stagingKey,
        string leaseKey,
        string leaseValue)
    {
        await db.HashSetAsync(
            stagingKey,
            [new HashEntry("leaseKey", leaseKey), new HashEntry("leaseValue", leaseValue)]).ConfigureAwait(false);
        _ = await db.KeyExpireAsync(stagingKey, StagingTtl).ConfigureAwait(false);
    }

    private static async Task ReleaseLeaseIfOwnedAsync(IDatabase db, string leaseKey, string leaseValue)
    {
        const string releaseScript = "if redis.call('GET', KEYS[1]) == ARGV[1] then return redis.call('DEL', KEYS[1]) else return 0 end";
        _ = await db.ScriptEvaluateAsync(
            releaseScript,
            [(RedisKey)leaseKey],
            [(RedisValue)leaseValue]).ConfigureAwait(false);
    }
}
