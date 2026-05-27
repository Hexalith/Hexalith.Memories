// <copyright file="TenantMetricsService.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Tenants;

using System.Globalization;
using System.Net;

using Hexalith.Memories.Contracts.V1;
using Hexalith.Memories.Server.Infrastructure;

using Microsoft.Extensions.Logging;

using StackExchange.Redis;

/// <summary>
/// Computes operator-facing per-tenant metrics (memory-unit count, index sizes, index health,
/// last-activity timestamp) for the Story 5.5 listing and configuration-view endpoints (AC1, AC2).
/// <para>
/// Performance guard (Task 1.7): a list of N tenants triggers ~N×4 backend calls. The FalkorDB
/// <c>MATCH (n) RETURN count(n)</c> query is O(|V|) — acceptable for MVP (tenant count &lt; ~100,
/// bounded graph size). Caching is explicitly deferred (anti-pattern: speculative complexity).
/// </para>
/// </summary>
public sealed partial class TenantMetricsService
{
    private const int ScanPageSize = 1000;

    private readonly IConnectionMultiplexer _redis;
    private readonly IConnectionMultiplexer _falkorDb;
    private readonly ILogger<TenantMetricsService> _logger;

    /// <summary>Initializes a new instance of the <see cref="TenantMetricsService"/> class.</summary>
    /// <param name="redis">The Redis connection multiplexer (keyed <c>"redis"</c>).</param>
    /// <param name="falkorDb">The FalkorDB connection multiplexer (keyed <c>"falkordb"</c>).</param>
    /// <param name="logger">Logger.</param>
    public TenantMetricsService(
        [FromKeyedServices("redis")] IConnectionMultiplexer redis,
        [FromKeyedServices("falkordb")] IConnectionMultiplexer falkorDb,
        ILogger<TenantMetricsService> logger)
    {
        _redis = redis;
        _falkorDb = falkorDb;
        _logger = logger;
    }

    /// <summary>
    /// Counts the tenant's memory units via <c>SCAN</c> against <c>{tenantId}:mu:*</c>.
    /// Returns <see langword="null"/> if Redis is unavailable (availability failure must not be
    /// reported as zero count).
    /// </summary>
    /// <param name="tenantId">The tenant identifier.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The memory unit count, or null on unavailability.</returns>
    public async Task<long?> GetMemoryUnitCountAsync(string tenantId, CancellationToken ct)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        try
        {
            IServer? server = GetAnyServer(_redis);
            if (server is null)
            {
                return null;
            }

            long count = 0;
            await foreach (RedisKey _ in server.KeysAsync(pattern: $"{tenantId}:mu:*", pageSize: ScanPageSize).WithCancellation(ct))
            {
                count++;
            }

            return count;
        }
        catch (RedisException ex)
        {
            LogBackendUnavailable(_logger, "Redis SCAN", tenantId, ex.Message);
            return null;
        }
    }

    /// <summary>
    /// Queries each backend for its document / node count and health. Never throws; per-backend
    /// failures are caught and surfaced as <c>null</c> count + <see cref="IndexHealth.Unknown"/>.
    /// </summary>
    /// <param name="tenantId">The tenant identifier.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Paired sizes/status tuple.</returns>
    public async Task<(TenantIndexSizes Sizes, TenantIndexStatus Status)> GetIndexSizesAsync(
        string tenantId,
        CancellationToken ct)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);

        (long? syntacticCount, IndexHealth syntacticHealth) = await GetRedisIndexCountAsync(
            IndexSchemaDefinitions.GetSyntacticIndexName(tenantId), tenantId, ct).ConfigureAwait(false);

        (long? semanticCount, IndexHealth semanticHealth) = await GetRedisIndexCountAsync(
            IndexSchemaDefinitions.GetSemanticIndexName(tenantId), tenantId, ct).ConfigureAwait(false);

        (long? graphCount, IndexHealth graphHealth) = await GetFalkorDbNodeCountAsync(tenantId, ct).ConfigureAwait(false);

        return (
            new TenantIndexSizes(syntacticCount, semanticCount, graphCount),
            new TenantIndexStatus(syntacticHealth, semanticHealth, graphHealth));
    }

    /// <summary>
    /// Returns the tenant's <c>lastActivityAt</c> timestamp — persisted by
    /// <see cref="Hexalith.Memories.Server.Activities.Indexing.IndexSyntacticActivity"/> as a hash
    /// field under <c>{tenantId}:metadata</c> (Amendment A + T). Missing (fresh tenant) or
    /// unavailable Redis → <see langword="null"/>.
    /// </summary>
    /// <param name="tenantId">The tenant identifier.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The timestamp, or null.</returns>
    public async Task<DateTimeOffset?> GetLastActivityAtAsync(string tenantId, CancellationToken ct)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        try
        {
            IDatabase db = _redis.GetDatabase();
            RedisValue raw = await db.HashGetAsync($"{tenantId}:metadata", "lastActivityAt").ConfigureAwait(false);
            if (raw.IsNullOrEmpty)
            {
                return null;
            }

            if (long.TryParse((string?)raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out long ticks)
                && ticks >= 0
                && ticks <= DateTime.MaxValue.Ticks)
            {
                return new DateTimeOffset(ticks, TimeSpan.Zero);
            }

            LogInvalidActivityValue(_logger, tenantId, (string?)raw ?? "<null>");
            return null;
        }
        catch (RedisException ex)
        {
            LogBackendUnavailable(_logger, "Redis HGET {tenantId}:metadata", tenantId, ex.Message);
            return null;
        }
    }

    private static IServer? GetAnyServer(IConnectionMultiplexer redis)
    {
        foreach (EndPoint endpoint in redis.GetEndPoints())
        {
            IServer server = redis.GetServer(endpoint);
            if (server.IsConnected)
            {
                return server;
            }
        }

        return null;
    }

    private async Task<(long? Count, IndexHealth Health)> GetRedisIndexCountAsync(
        string indexName,
        string tenantId,
        CancellationToken ct)
    {
        try
        {
            IDatabase db = _redis.GetDatabase();
            RedisResult result = await db.ExecuteAsync("FT.INFO", indexName).ConfigureAwait(false);

            if (IndexSchemaDefinitions.TryGetDocumentCount(result, out int docCount))
            {
                return (docCount, IndexHealth.Ready);
            }

            // Well-formed response but num_docs absent/unparseable → capability reduced.
            return (null, IndexHealth.Degraded);
        }
        catch (RedisServerException ex) when (ex.Message.Contains("no such index", StringComparison.OrdinalIgnoreCase))
        {
            return (null, IndexHealth.Missing);
        }
        catch (RedisServerException ex) when (
            ex.Message.Contains("LOADING", StringComparison.OrdinalIgnoreCase) ||
            ex.Message.Contains("BUSY", StringComparison.OrdinalIgnoreCase))
        {
            return (null, IndexHealth.Degraded);
        }
        catch (RedisConnectionException ex)
        {
            LogBackendUnavailable(_logger, $"Redis FT.INFO {indexName}", tenantId, ex.Message);
            return (null, IndexHealth.Unknown);
        }
        catch (RedisException ex)
        {
            LogBackendUnavailable(_logger, $"Redis FT.INFO {indexName}", tenantId, ex.Message);
            return (null, IndexHealth.Unknown);
        }
    }

    private async Task<(long? Count, IndexHealth Health)> GetFalkorDbNodeCountAsync(
        string tenantId,
        CancellationToken ct)
    {
        try
        {
            NFalkorDB.FalkorDB falkor = new(_falkorDb.GetDatabase());
            NFalkorDB.ResultSet result = await falkor
                .QueryAsync(tenantId, "MATCH (n) RETURN count(n)")
                .ConfigureAwait(false);

            NFalkorDB.Record? firstRecord = result.FirstOrDefault();
            if (firstRecord is null || firstRecord.Values.Count == 0)
            {
                return (null, IndexHealth.Degraded);
            }

            if (long.TryParse(firstRecord.Values[0]?.ToString(), out long nodeCount))
            {
                return (nodeCount, IndexHealth.Ready);
            }

            return (null, IndexHealth.Degraded);
        }
        catch (RedisServerException ex) when (
            ex.Message.Contains("no such graph", StringComparison.OrdinalIgnoreCase) ||
            ex.Message.Contains("unknown graph", StringComparison.OrdinalIgnoreCase))
        {
            return (null, IndexHealth.Missing);
        }
        catch (RedisConnectionException ex)
        {
            LogBackendUnavailable(_logger, $"FalkorDB graph {tenantId}", tenantId, ex.Message);
            return (null, IndexHealth.Unknown);
        }
        catch (RedisException ex)
        {
            LogBackendUnavailable(_logger, $"FalkorDB graph {tenantId}", tenantId, ex.Message);
            return (null, IndexHealth.Unknown);
        }
        catch (Exception ex)
        {
            // NFalkorDB can surface driver-level parse failures (NullReferenceException, etc.) when
            // the underlying Redis returns a malformed response. Contract: this method must NEVER
            // throw — classify as Unknown so the caller still reports a fully-formed tuple.
            LogBackendUnavailable(_logger, $"FalkorDB graph {tenantId}", tenantId, ex.Message);
            return (null, IndexHealth.Unknown);
        }
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "Tenant metric backend unavailable ({Backend}) for tenant '{TenantId}': {Details}")]
    private static partial void LogBackendUnavailable(ILogger logger, string backend, string tenantId, string details);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Invalid lastActivityAt value '{RawValue}' for tenant '{TenantId}' — returning null")]
    private static partial void LogInvalidActivityValue(ILogger logger, string tenantId, string rawValue);
}
