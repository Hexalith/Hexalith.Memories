// <copyright file="OrphanSemanticIndexReconciler.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Hosting;

using Hexalith.Memories.Server.Infrastructure;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

using StackExchange.Redis;

/// <summary>Story 9.2 Task 4.10 — one-shot startup reconciler that sweeps orphan natural-language semantic
/// indexes (<c>*:memories:vec:nl</c>) with no matching raw <c>:memories:vec</c> sibling. Runs once on
/// server startup, idempotent under repeated runs.
///
/// Review D3: coverage is <b>startup-recovery only</b>. Post-startup SIGKILL-during-provisioning is
/// NOT rediscovered until the next pod restart. Mid-workflow failures during normal operation are
/// covered by <c>DeleteRedisVectorIndexActivity</c>'s dual-drop logic (Task 4.6) and by the
/// provisioning workflow's compensation path. If post-startup orphan indexes become an
/// operational burden, promote this service to an interval loop (see deferred-work F9).</summary>
public sealed partial class OrphanSemanticIndexReconciler : BackgroundService
{
    private readonly IConnectionMultiplexer _redis;
    private readonly ILogger<OrphanSemanticIndexReconciler> _logger;

    public OrphanSemanticIndexReconciler(
        [FromKeyedServices("redis")] IConnectionMultiplexer redis,
        ILogger<OrphanSemanticIndexReconciler> logger)
    {
        _redis = redis;
        _logger = logger;
    }

    /// <inheritdoc/>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            await ReconcileAsync(stoppingToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // Service shutdown — reconciler exits cleanly.
        }
        catch (Exception ex) when (ex is RedisException or IOException or TimeoutException)
        {
            // Reconciler is best-effort: do NOT crash the host if Redis is momentarily unreachable.
            // Orphan indexes are a hygiene concern, not a correctness one. We intentionally do NOT
            // swallow programming errors (NullReferenceException / ArgumentException / etc.) — those
            // should surface via host-level fault handling.
            LogReconcilerFailed(_logger, ex);
        }
    }

    internal async Task ReconcileAsync(CancellationToken cancellationToken)
    {
        IDatabase db = _redis.GetDatabase();
        RedisResult listResult = await db.ExecuteAsync("FT._LIST").ConfigureAwait(false);
        string[] indexNames = ReadIndexNames(listResult);
        if (indexNames.Length == 0)
        {
            LogReconcilerStarted(_logger, 0, 0);
            return;
        }

        HashSet<string> rawIndexes = new(StringComparer.Ordinal);
        List<string> nlIndexes = [];

        foreach (string name in indexNames)
        {
            if (name.EndsWith(IndexSchemaDefinitions.NaturalLanguageSemanticIndexSuffix, StringComparison.Ordinal))
            {
                nlIndexes.Add(name);
            }
            else if (name.EndsWith(IndexSchemaDefinitions.SemanticIndexSuffix, StringComparison.Ordinal))
            {
                rawIndexes.Add(name);
            }
        }

        int droppedCount = 0;
        foreach (string nlIndex in nlIndexes)
        {
            cancellationToken.ThrowIfCancellationRequested();
            string tenantId = nlIndex[..^IndexSchemaDefinitions.NaturalLanguageSemanticIndexSuffix.Length];
            string expectedRaw = tenantId + IndexSchemaDefinitions.SemanticIndexSuffix;
            if (rawIndexes.Contains(expectedRaw))
            {
                continue;
            }

            try
            {
                await db.ExecuteAsync("FT.DROPINDEX", nlIndex, "DD").ConfigureAwait(false);
                droppedCount++;
                LogOrphanDropped(_logger, nlIndex, tenantId);
            }
            catch (RedisServerException ex) when (ex.Message.Contains("Unknown index", StringComparison.OrdinalIgnoreCase))
            {
                // Race: the index vanished between FT._LIST and FT.DROPINDEX — treat as success.
            }
        }

        LogReconcilerStarted(_logger, nlIndexes.Count, droppedCount);
    }

    private string[] ReadIndexNames(RedisResult raw)
    {
        try
        {
            RedisResult[]? items = (RedisResult[]?)raw;
            if (items is null || items.Length == 0)
            {
                return [];
            }

            string[] names = new string[items.Length];
            for (int i = 0; i < items.Length; i++)
            {
                names[i] = items[i].ToString() ?? string.Empty;
            }

            return names;
        }
        catch (InvalidCastException ex)
        {
            // Review P22: a future Redis Stack `FT._LIST` shape change would make the array cast
            // throw. Returning `[]` silently is indistinguishable from "no indexes" in the healthy
            // path; emit a Warning naming the observed type so operators see the drift.
            LogFtListShapeUnexpected(_logger, ex, raw.Resp2Type.ToString());
            return [];
        }
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "OrphanSemanticIndexReconciler swept {NlIndexCount} NL indexes, dropped {DroppedCount} orphan(s).")]
    private static partial void LogReconcilerStarted(ILogger logger, int nlIndexCount, int droppedCount);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Dropped orphan NL semantic index {IndexName} (no raw sibling for tenant {TenantId}).")]
    private static partial void LogOrphanDropped(ILogger logger, string indexName, string tenantId);

    [LoggerMessage(Level = LogLevel.Error, Message = "OrphanSemanticIndexReconciler startup sweep failed — orphan indexes (if any) remain until next startup.")]
    private static partial void LogReconcilerFailed(ILogger logger, Exception exception);

    [LoggerMessage(Level = LogLevel.Warning, Message = "OrphanSemanticIndexReconciler: FT._LIST returned unexpected shape (observed RedisResult type: {ObservedType}) — treating as empty.")]
    private static partial void LogFtListShapeUnexpected(ILogger logger, Exception exception, string observedType);
}
