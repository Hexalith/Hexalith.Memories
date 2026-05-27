// <copyright file="CorpusStatisticsActor.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Actors;

using System.Globalization;

using Dapr.Actors.Runtime;

using Microsoft.Extensions.Logging;

using StackExchange.Redis;

/// <summary>
/// DAPR Actor that caches per-tenant RediSearch corpus statistics (document count, average document length).
/// Actor ID = tenant ID. Statistics are refreshed from RediSearch FT.INFO on a 5-minute timer.
/// </summary>
internal sealed partial class CorpusStatisticsActor : Actor, ICorpusStatisticsActor
{
    private const string StateName = "corpusStats";

    private readonly IConnectionMultiplexer _redis;
    private readonly ILogger<CorpusStatisticsActor> _logger;

    /// <summary>Initializes a new instance of the <see cref="CorpusStatisticsActor"/> class.</summary>
    /// <param name="host">The actor host provided by the DAPR runtime.</param>
    /// <param name="redis">The Redis connection multiplexer (keyed "redis").</param>
    /// <param name="logger">The logger.</param>
    public CorpusStatisticsActor(
        ActorHost host,
        [FromKeyedServices("redis")] IConnectionMultiplexer redis,
        ILogger<CorpusStatisticsActor> logger)
        : base(host)
    {
        _redis = redis;
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task<int> GetDocumentCountAsync()
    {
        CorpusStatistics stats = await GetOrRefreshStatsAsync().ConfigureAwait(false);
        return stats.DocumentCount;
    }

    /// <inheritdoc/>
    public async Task<double> GetAverageDocumentLengthAsync()
    {
        CorpusStatistics stats = await GetOrRefreshStatsAsync().ConfigureAwait(false);
        return stats.AverageDocumentLength;
    }

    /// <inheritdoc/>
    public async Task<CorpusStatistics> GetStatisticsAsync()
    {
        return await GetOrRefreshStatsAsync().ConfigureAwait(false);
    }

    /// <summary>
    /// Parses a raw FT.INFO RedisResult into <see cref="CorpusStatistics"/>.
    /// FT.INFO returns a flat key-value array: [key1, val1, key2, val2, ...].
    /// Some values may be nested arrays (e.g., index_definition, attributes) — these are skipped.
    /// </summary>
    /// <param name="raw">The raw RedisResult from FT.INFO.</param>
    /// <param name="refreshedAt">The timestamp to record for the refresh.</param>
    /// <returns>Parsed corpus statistics, or zero-valued statistics if parsing fails.</returns>
    internal static CorpusStatistics ParseFtInfoResult(RedisResult raw, DateTimeOffset refreshedAt)
    {
        if (raw is null || raw.IsNull)
        {
            return new CorpusStatistics(0, 0.0, refreshedAt);
        }

        RedisResult[] items;
        try
        {
            items = (RedisResult[])raw!;
        }
        catch (InvalidCastException)
        {
            return new CorpusStatistics(0, 0.0, refreshedAt);
        }

        int docCount = 0;
        double docTableSizeMB = 0.0;

        for (int i = 0; i < items.Length - 1; i += 2)
        {
            // Skip non-string keys (nested arrays from certain FT.INFO fields)
            if (items[i].Resp2Type != ResultType.BulkString)
            {
                continue;
            }

            string key = (string)items[i]!;

            // Skip values that are nested arrays
            if (items[i + 1].Resp2Type != ResultType.BulkString && items[i + 1].Resp2Type != ResultType.Integer)
            {
                continue;
            }

            if (key == "num_docs")
            {
                if (TryReadDocumentCount(items[i + 1], out int parsedDocumentCount))
                {
                    docCount = parsedDocumentCount;
                }
            }
            else if (key == "doc_table_size_mb")
            {
                if (TryReadNonNegativeDouble(items[i + 1], out double parsedDocTableSizeMB))
                {
                    docTableSizeMB = parsedDocTableSizeMB;
                }
            }
        }

        double avgDocLen = docCount > 0 && docTableSizeMB > 0.0
            ? (docTableSizeMB * 1024 * 1024) / docCount
            : 0.0;

        return new CorpusStatistics(docCount, avgDocLen, refreshedAt);
    }

    /// <inheritdoc/>
    protected override async Task OnActivateAsync()
    {
        await RegisterTimerAsync(
            "RefreshCorpusStats",
            nameof(RefreshStatsCallbackAsync),
            null,
            dueTime: TimeSpan.Zero,
            period: TimeSpan.FromMinutes(5)).ConfigureAwait(false);
    }

    /// <summary>Timer callback that refreshes corpus statistics from RediSearch FT.INFO.</summary>
    /// <param name="state">Timer state (unused).</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    internal async Task RefreshStatsCallbackAsync(byte[] state)
    {
        string tenantId = Id.GetId();
        string indexName = $"{tenantId}:memories:idx";

        try
        {
            IDatabase db = _redis.GetDatabase();
            RedisResult raw = await db.ExecuteAsync("FT.INFO", indexName).ConfigureAwait(false);

            CorpusStatistics stats = ParseFtInfoResult(raw, DateTimeOffset.UtcNow);

            await StateManager.SetStateAsync(StateName, stats).ConfigureAwait(false);
            LogStatsRefreshed(_logger, tenantId, stats.DocumentCount, stats.AverageDocumentLength);
        }
        catch (RedisServerException ex) when (IsMissingIndexError(ex))
        {
            // Index doesn't exist yet — set zero stats
            CorpusStatistics emptyStats = new(0, 0.0, DateTimeOffset.UtcNow);
            await StateManager.SetStateAsync(StateName, emptyStats).ConfigureAwait(false);
            LogIndexNotFound(_logger, tenantId, indexName);
        }
        catch (RedisConnectionException ex)
        {
            // Transient connection failure — retain previous cached state
            LogRedisRefreshFailure(_logger, "connection failure", tenantId, ex.Message);
        }
        catch (TimeoutException ex)
        {
            // Transient timeout — retain previous cached state
            LogRedisRefreshFailure(_logger, "timeout", tenantId, ex.Message);
        }
    }

    private async Task<CorpusStatistics> GetOrRefreshStatsAsync()
    {
        ConditionalValue<CorpusStatistics> result = await StateManager
            .TryGetStateAsync<CorpusStatistics>(StateName)
            .ConfigureAwait(false);

        if (result.HasValue)
        {
            return await PersistStatsBeforeReturnAsync(result.Value).ConfigureAwait(false);
        }

        // First call before timer fires — trigger inline refresh
        await RefreshStatsCallbackAsync([]).ConfigureAwait(false);

        ConditionalValue<CorpusStatistics> retryResult = await StateManager
            .TryGetStateAsync<CorpusStatistics>(StateName)
            .ConfigureAwait(false);

        CorpusStatistics stats = retryResult.HasValue
            ? retryResult.Value
            : new CorpusStatistics(0, 0.0, DateTimeOffset.UtcNow);

        return await PersistStatsBeforeReturnAsync(stats).ConfigureAwait(false);
    }

    private static bool IsMissingIndexError(RedisServerException ex)
        => ex.Message.Contains("unknown index name", StringComparison.OrdinalIgnoreCase)
        || ex.Message.Contains("no such index", StringComparison.OrdinalIgnoreCase);

    private static bool TryReadDocumentCount(RedisResult result, out int docCount)
    {
        long parsedDocumentCount;

        if (result.Resp2Type == ResultType.Integer)
        {
            parsedDocumentCount = (long)result;
        }
        else if (result.Resp2Type == ResultType.BulkString &&
            long.TryParse((string)result!, NumberStyles.Integer, CultureInfo.InvariantCulture, out long bulkStringDocumentCount))
        {
            parsedDocumentCount = bulkStringDocumentCount;
        }
        else
        {
            docCount = 0;
            return false;
        }

        if (parsedDocumentCount is < 0 or > int.MaxValue)
        {
            docCount = 0;
            return false;
        }

        docCount = (int)parsedDocumentCount;
        return true;
    }

    private static bool TryReadNonNegativeDouble(RedisResult result, out double value)
    {
        if (result.Resp2Type == ResultType.Integer)
        {
            value = (long)result;
            return value >= 0.0;
        }

        if (result.Resp2Type == ResultType.BulkString &&
            double.TryParse((string)result!, NumberStyles.Float, CultureInfo.InvariantCulture, out double parsedValue) &&
            double.IsFinite(parsedValue) &&
            parsedValue >= 0.0)
        {
            value = parsedValue;
            return true;
        }

        value = 0.0;
        return false;
    }

    private async Task<CorpusStatistics> PersistStatsBeforeReturnAsync(CorpusStatistics stats)
    {
        await StateManager.SetStateAsync(StateName, stats).ConfigureAwait(false);
        return stats;
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Corpus stats refreshed for tenant {TenantId}: docCount={DocumentCount}, avgDocLen={AverageDocumentLength:F2}")]
    private static partial void LogStatsRefreshed(ILogger logger, string tenantId, int documentCount, double averageDocumentLength);

    [LoggerMessage(Level = LogLevel.Warning, Message = "RediSearch index {IndexName} not found for tenant {TenantId} — setting zero stats")]
    private static partial void LogIndexNotFound(ILogger logger, string tenantId, string indexName);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Redis {FailureType} during corpus stats refresh for tenant {TenantId}: {ErrorMessage} — retaining cached state")]
    private static partial void LogRedisRefreshFailure(ILogger logger, string failureType, string tenantId, string errorMessage);
}
