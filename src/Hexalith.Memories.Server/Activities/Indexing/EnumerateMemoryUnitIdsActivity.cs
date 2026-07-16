// <copyright file="EnumerateMemoryUnitIdsActivity.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Activities.Indexing;

using System.Net;

using Dapr.Workflow;

using Hexalith.Memories.Server.Graph;
using Hexalith.Memories.Server.Infrastructure;

using Microsoft.Extensions.Logging;

using NFalkorDB;

using StackExchange.Redis;

/// <summary>
/// Story 8.2 — enumerates the union of memory unit IDs across the three backends
/// (syntactic SCAN, semantic SCAN, graph MATCH). Used by
/// <c>ConsistencyVerificationWorkflow</c> as the authoritative source of units to probe.
/// </summary>
/// <remarks>
/// <para>
/// Risk #3 mitigation: orphans present only in one backend (e.g. a vector hash without a
/// corresponding syntactic hash) would be missed by a syntactic-only enumeration. Unioning
/// all three backends guarantees every unit reachable from any backend shows up.
/// </para>
/// <para>
/// Risk #6 mitigation: all Redis enumeration is cursor-based (<c>IServer.KeysAsync</c>
/// with a <c>pageSize</c>) — never the blocking <c>KEYS</c> command.
/// </para>
/// <para>
/// Task 1.2a cap: when the un-capped union size exceeds
/// <see cref="EnumerateMemoryUnitIdsInput.MaxUnits"/> (default 50,000), the returned list
/// is truncated and <c>Truncated=true</c>. Operators should shard the audit across passes.
/// </para>
/// </remarks>
public sealed partial class EnumerateMemoryUnitIdsActivity
    : WorkflowActivity<EnumerateMemoryUnitIdsInput, EnumerateMemoryUnitIdsResult>
{
    private const int ScanPageSize = 1000;
    private static readonly TimeSpan GraphOperationTimeout = TimeSpan.FromSeconds(30);

    private readonly IConnectionMultiplexer _redis;
    private readonly IConnectionMultiplexer _falkorDb;
    private readonly IGraphQueryBuilder _graphQueryBuilder;
    private readonly ILogger<EnumerateMemoryUnitIdsActivity> _logger;

    /// <summary>Initializes a new instance of the <see cref="EnumerateMemoryUnitIdsActivity"/> class.</summary>
    public EnumerateMemoryUnitIdsActivity(
        [FromKeyedServices("redis")] IConnectionMultiplexer redis,
        [FromKeyedServices("falkordb")] IConnectionMultiplexer falkorDb,
        IGraphQueryBuilder graphQueryBuilder,
        ILogger<EnumerateMemoryUnitIdsActivity> logger)
    {
        _redis = redis;
        _falkorDb = falkorDb;
        _graphQueryBuilder = graphQueryBuilder;
        _logger = logger;
    }

    /// <inheritdoc/>
    public override async Task<EnumerateMemoryUnitIdsResult> RunAsync(
        WorkflowActivityContext context,
        EnumerateMemoryUnitIdsInput input)
    {
        ArgumentNullException.ThrowIfNull(input);
        TenantIdGuard.Validate(input.TenantId);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(input.MaxUnits);

        // CancellationToken is not passed directly by DAPR to activities; the token is implicit
        // in the workflow timeout. We still guard against cancellation from WaitAsync calls below
        // so the three enumerations can short-circuit on FalkorDB / Redis failures.
        CancellationToken ct = CancellationToken.None;

        HashSet<string> union = new(StringComparer.Ordinal);

        Task<IReadOnlyList<string>> syntacticScan = ScanAsync(
            pattern: IndexSchemaDefinitions.GetSyntacticKeyPrefix(input.TenantId),
            parseMemoryUnitId: key => IndexSchemaDefinitions.TryParseSyntacticMemoryUnitId(input.TenantId, key, out string id) ? id : null,
            ct);
        Task<IReadOnlyList<string>> semanticScan = ScanAsync(
            pattern: IndexSchemaDefinitions.GetSemanticKeyPrefix(input.TenantId),
            parseMemoryUnitId: key => IndexSchemaDefinitions.TryParseSemanticMemoryUnitId(input.TenantId, key, out string id) ? id : null,
            ct);
        Task<IReadOnlyList<string>> graphScan = EnumerateGraphIdsAsync(input.TenantId, ct);

        IReadOnlyList<string>[] scans = await Task.WhenAll(syntacticScan, semanticScan, graphScan).ConfigureAwait(false);

        foreach (IReadOnlyList<string> scan in scans)
        {
            foreach (string id in scan)
            {
                if (!string.IsNullOrEmpty(id))
                {
                    union.Add(id);
                }
            }
        }

        List<string> ordered = union.ToList();
        ordered.Sort(StringComparer.Ordinal);

        long totalUnion = ordered.Count;
        bool truncated = totalUnion > input.MaxUnits;

        if (truncated)
        {
            LogEnumerationTruncated(_logger, input.TenantId, totalUnion, input.MaxUnits);
            ordered = ordered.Take(input.MaxUnits).ToList();
        }

        return new EnumerateMemoryUnitIdsResult(ordered, totalUnion, truncated);
    }

    private async Task<IReadOnlyList<string>> ScanAsync(
        string pattern,
        Func<RedisKey, string?> parseMemoryUnitId,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(parseMemoryUnitId);

        IServer? server = GetAnyServer(_redis);
        if (server is null)
        {
            throw new InvalidOperationException(
                $"No connected Redis server is available to enumerate keys matching '{pattern}*'.");
        }

        string fullPattern = pattern + "*";
        int prefixLength = pattern.Length;
        List<string> ids = [];

        try
        {
            await foreach (RedisKey key in server.KeysAsync(pattern: fullPattern, pageSize: ScanPageSize).WithCancellation(ct))
            {
                string? memoryUnitId = parseMemoryUnitId(key);
                if (!string.IsNullOrEmpty(memoryUnitId))
                {
                    ids.Add(memoryUnitId);
                }
            }

            return ids;
        }
        catch (RedisException ex)
        {
            LogScanFailed(_logger, pattern, ex.Message);
            throw new InvalidOperationException(
                $"Redis scan failed while enumerating keys matching '{fullPattern}'.",
                ex);
        }
    }

    private async Task<IReadOnlyList<string>> EnumerateGraphIdsAsync(string tenantId, CancellationToken ct)
    {
        try
        {
            FalkorDB falkor = new(_falkorDb.GetDatabase());
            (string query, IDictionary<string, object> parameters) = _graphQueryBuilder.BuildEnumerateMemoryUnitIds();

            ResultSet result = await falkor
                .SelectGraph(tenantId).QueryAsync(query, parameters)
                .WaitAsync(GraphOperationTimeout, ct)
                .ConfigureAwait(false);

            List<string> ids = new(result.Count);
            foreach (Record record in result)
            {
                if (record.Values.Count > 0)
                {
                    string? id = record.Values[0]?.ToString();
                    if (!string.IsNullOrEmpty(id))
                    {
                        ids.Add(id);
                    }
                }
            }

            return ids;
        }
        catch (RedisException ex)
        {
            LogGraphEnumerationFailed(_logger, tenantId, ex.Message);
            throw new InvalidOperationException(
                $"Graph enumeration failed for tenant '{tenantId}'.",
                ex);
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

    [LoggerMessage(
        EventId = 8204,
        Level = LogLevel.Warning,
        Message = "EnumerationTruncated tenant '{TenantId}' found {TotalCount} memory units, returning first {MaxUnits}; shard audit across passes")]
    private static partial void LogEnumerationTruncated(ILogger logger, string tenantId, long totalCount, int maxUnits);

    [LoggerMessage(
        EventId = 8220,
        Level = LogLevel.Warning,
        Message = "RedisScanFailed pattern '{Pattern}': {Error}")]
    private static partial void LogScanFailed(ILogger logger, string pattern, string error);

    [LoggerMessage(
        EventId = 8221,
        Level = LogLevel.Warning,
        Message = "GraphEnumerationFailed tenant '{TenantId}': {Error}")]
    private static partial void LogGraphEnumerationFailed(ILogger logger, string tenantId, string error);
}
