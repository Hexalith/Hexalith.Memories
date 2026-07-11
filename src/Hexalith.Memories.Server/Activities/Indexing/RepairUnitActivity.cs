// <copyright file="RepairUnitActivity.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Activities.Indexing;

using System.Diagnostics;
using System.Net;

using Dapr.Workflow;

using Hexalith.Memories.Contracts.V1;
using Hexalith.Memories.Server.Consistency;
using Hexalith.Memories.Server.Graph;
using Hexalith.Memories.Server.Infrastructure;

using Microsoft.Extensions.Logging;

using NFalkorDB;

using StackExchange.Redis;

/// <summary>
/// Story 8.2 — repairs a single memory unit by re-verifying its presence across the three
/// backends and dispatching the action dictated by <see cref="RepairPlanCalculator"/>.
/// </summary>
/// <remarks>
/// <para>
/// Risk #1 (load-bearing): the activity re-verifies the unit via
/// <see cref="ConsistencyInspectionService"/> IMMEDIATELY before acting. If the re-verify
/// disagrees with the stale recommendation (e.g. the unit has since become fully
/// consistent), the action is a no-op — preventing destructive writes based on a stale
/// snapshot.
/// </para>
/// <para>
/// Orphan removal uses <c>KeyDeleteAsync</c> (vector) or
/// <c>BuildDeleteMemoryUnitNode</c> (graph, which emits <c>DETACH DELETE</c>).
/// Re-index paths delegate to <see cref="SemanticIndexer"/> and
/// <see cref="GraphNodeMerger"/>.
/// </para>
/// </remarks>
public sealed partial class RepairUnitActivity : WorkflowActivity<RepairUnitInput, RepairActionRecord>
{
    private static readonly TimeSpan GraphOperationTimeout = TimeSpan.FromSeconds(10);

    private readonly IConsistencyInspectionService _inspectionService;
    private readonly ISemanticIndexer _semanticIndexer;
    private readonly IGraphNodeMerger _graphNodeMerger;
    private readonly IConnectionMultiplexer _redis;
    private readonly IConnectionMultiplexer _falkorDb;
    private readonly IGraphQueryBuilder _graphQueryBuilder;
    private readonly ILogger<RepairUnitActivity> _logger;

    /// <summary>Initializes a new instance of the <see cref="RepairUnitActivity"/> class.</summary>
    public RepairUnitActivity(
        IConsistencyInspectionService inspectionService,
        ISemanticIndexer semanticIndexer,
        IGraphNodeMerger graphNodeMerger,
        [FromKeyedServices("redis")] IConnectionMultiplexer redis,
        [FromKeyedServices("falkordb")] IConnectionMultiplexer falkorDb,
        IGraphQueryBuilder graphQueryBuilder,
        ILogger<RepairUnitActivity> logger)
    {
        _inspectionService = inspectionService;
        _semanticIndexer = semanticIndexer;
        _graphNodeMerger = graphNodeMerger;
        _redis = redis;
        _falkorDb = falkorDb;
        _graphQueryBuilder = graphQueryBuilder;
        _logger = logger;
    }

    /// <inheritdoc/>
    public override async Task<RepairActionRecord> RunAsync(
        WorkflowActivityContext context,
        RepairUnitInput input)
    {
        ArgumentNullException.ThrowIfNull(input);
        TenantIdGuard.Validate(input.TenantId);
        string memoryUnitId = ConsistencyInspectionService.NormalizeMemoryUnitId(input.MemoryUnitId);

        CancellationToken ct = CancellationToken.None;

        // Short-circuit: if the stale recommendation is already Unrepairable and the caller
        // opted out of attempting it, surface the flag directly without re-probing.
        if (input.Recommendation == ConsistencyRepairRecommendation.Unrepairable && !input.IncludeUnrepairable)
        {
            IReadOnlyDictionary<string, string> absent = PresenceMap(false, false, false);
            LogUnrepairable(_logger, input.TenantId, memoryUnitId);
            LogRepairActionApplied(
                _logger,
                input.TenantId,
                memoryUnitId,
                ConsistencyRepairRecommendation.Unrepairable.ToString(),
                FormatPresence(absent),
                FormatPresence(absent),
                durationMs: 0);
            return new RepairActionRecord(
                memoryUnitId,
                ConsistencyRepairRecommendation.Unrepairable,
                Succeeded: false,
                FailureReason: "Unrepairable — nothing remains in any backend or syntactic source is gone.",
                BeforeState: absent,
                AfterState: absent);
        }

        // Risk #1: fresh re-verify before any write. If the unit is consistent now, skip.
        ConsistencyInspectionResult freshInspection;
        try
        {
            freshInspection = await _inspectionService
                .InspectAsync(input.TenantId, memoryUnitId, ct)
                .ConfigureAwait(false);
        }
        catch (KeyNotFoundException)
        {
            // Fresh (F,F,F) — unit is absent from all three backends at re-verify time. Per
            // RepairPlanCalculator this maps to Unrepairable (not NoOp, which is reserved for
            // the (T,T,T) consistent case). Reporting Succeeded=false keeps the distinction
            // visible to operators: "nothing anywhere" is a bookkeeping gap they must resolve.
            IReadOnlyDictionary<string, string> absent = PresenceMap(false, false, false);
            LogUnrepairable(_logger, input.TenantId, memoryUnitId);
            LogRepairActionApplied(
                _logger,
                input.TenantId,
                memoryUnitId,
                ConsistencyRepairRecommendation.Unrepairable.ToString(),
                FormatPresence(absent),
                FormatPresence(absent),
                durationMs: 0);
            return new RepairActionRecord(
                memoryUnitId,
                ConsistencyRepairRecommendation.Unrepairable,
                Succeeded: false,
                FailureReason: "Unrepairable — unit is absent from all three backends on fresh re-check; cannot re-derive state.",
                BeforeState: absent,
                AfterState: absent);
        }

        IReadOnlyDictionary<string, string> beforeState = PresenceMap(
            freshInspection.SyntacticPresent,
            freshInspection.SemanticPresent,
            freshInspection.GraphPresent);

        ConsistencyRepairRecommendation recommendation = freshInspection.Recommendation;

        if (recommendation == ConsistencyRepairRecommendation.NoOp)
        {
            LogNoOpRepair(_logger, input.TenantId, memoryUnitId);
            LogRepairActionApplied(
                _logger,
                input.TenantId,
                memoryUnitId,
                ConsistencyRepairRecommendation.NoOp.ToString(),
                FormatPresence(beforeState),
                FormatPresence(beforeState),
                durationMs: 0);
            return new RepairActionRecord(
                memoryUnitId,
                ConsistencyRepairRecommendation.NoOp,
                Succeeded: true,
                FailureReason: null,
                BeforeState: beforeState,
                AfterState: beforeState);
        }

        if (recommendation == ConsistencyRepairRecommendation.Unrepairable)
        {
            LogUnrepairable(_logger, input.TenantId, memoryUnitId);
            LogRepairActionApplied(
                _logger,
                input.TenantId,
                memoryUnitId,
                ConsistencyRepairRecommendation.Unrepairable.ToString(),
                FormatPresence(beforeState),
                FormatPresence(beforeState),
                durationMs: 0);
            return new RepairActionRecord(
                memoryUnitId,
                ConsistencyRepairRecommendation.Unrepairable,
                Succeeded: false,
                FailureReason: "Unrepairable — nothing remains in any backend or syntactic source is gone.",
                BeforeState: beforeState,
                AfterState: beforeState);
        }

        Stopwatch stopwatch = Stopwatch.StartNew();
        try
        {
            IReadOnlyDictionary<string, string> afterState = await ApplyRepairAsync(
                input.TenantId,
                memoryUnitId,
                recommendation,
                ct).ConfigureAwait(false);
            stopwatch.Stop();

            LogRepairActionApplied(
                _logger,
                input.TenantId,
                memoryUnitId,
                recommendation.ToString(),
                FormatPresence(beforeState),
                FormatPresence(afterState),
                stopwatch.ElapsedMilliseconds);

            return new RepairActionRecord(
                memoryUnitId,
                recommendation,
                Succeeded: true,
                FailureReason: null,
                BeforeState: beforeState,
                AfterState: afterState);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            IReadOnlyDictionary<string, string> afterState = await CaptureAfterFailureStateAsync(
                input.TenantId,
                memoryUnitId,
                beforeState,
                ct).ConfigureAwait(false);
            LogRepairActionFailed(
                _logger,
                input.TenantId,
                memoryUnitId,
                recommendation.ToString(),
                FormatPresence(beforeState),
                FormatPresence(afterState),
                stopwatch.ElapsedMilliseconds,
                ex.Message);
            return new RepairActionRecord(
                memoryUnitId,
                recommendation,
                Succeeded: false,
                FailureReason: ex.Message,
                BeforeState: beforeState,
                AfterState: afterState);
        }
    }

    private async Task<IReadOnlyDictionary<string, string>> CaptureAfterFailureStateAsync(
        string tenantId,
        string memoryUnitId,
        IReadOnlyDictionary<string, string> fallback,
        CancellationToken ct)
    {
        try
        {
            ConsistencyInspectionResult inspection = await _inspectionService
                .InspectAsync(tenantId, memoryUnitId, ct)
                .ConfigureAwait(false);

            return PresenceMap(
                inspection.SyntacticPresent,
                inspection.SemanticPresent,
                inspection.GraphPresent);
        }
        catch (KeyNotFoundException)
        {
            return PresenceMap(false, false, false);
        }
        catch (Exception ex)
        {
            LogRepairFailureStateProbeFailed(_logger, tenantId, memoryUnitId, ex.Message);
            return fallback;
        }
    }

    private async Task<IReadOnlyDictionary<string, string>> ApplyRepairAsync(
        string tenantId,
        string memoryUnitId,
        ConsistencyRepairRecommendation recommendation,
        CancellationToken ct)
    {
        switch (recommendation)
        {
            case ConsistencyRepairRecommendation.RemoveOrphanedSemantic:
                await DeleteVectorAsync(tenantId, memoryUnitId, ct).ConfigureAwait(false);
                return PresenceMap(false, false, false);

            case ConsistencyRepairRecommendation.RemoveOrphanedGraph:
                await DeleteGraphNodeAsync(tenantId, memoryUnitId, ct).ConfigureAwait(false);
                return PresenceMap(false, false, false);

            case ConsistencyRepairRecommendation.RemoveOrphanedSemanticAndGraph:
                await DeleteVectorAsync(tenantId, memoryUnitId, ct).ConfigureAwait(false);
                await DeleteGraphNodeAsync(tenantId, memoryUnitId, ct).ConfigureAwait(false);
                return PresenceMap(false, false, false);

            case ConsistencyRepairRecommendation.ReIndexSemantic:
                await _semanticIndexer.ReIndexFromSyntacticAsync(tenantId, memoryUnitId, ct).ConfigureAwait(false);
                return PresenceMap(true, true, true);

            case ConsistencyRepairRecommendation.ReIndexGraph:
                await _graphNodeMerger.ReMergeFromSyntacticAsync(tenantId, memoryUnitId, ct).ConfigureAwait(false);
                return PresenceMap(true, true, true);

            case ConsistencyRepairRecommendation.ReIndexSemanticAndGraph:
                await _semanticIndexer.ReIndexFromSyntacticAsync(tenantId, memoryUnitId, ct).ConfigureAwait(false);
                await _graphNodeMerger.ReMergeFromSyntacticAsync(tenantId, memoryUnitId, ct).ConfigureAwait(false);
                return PresenceMap(true, true, true);

            default:
                throw new InvalidOperationException(
                    $"Unexpected recommendation '{recommendation}' reached the repair dispatcher.");
        }
    }

    private async Task DeleteVectorAsync(string tenantId, string memoryUnitId, CancellationToken ct)
    {
        IDatabase db = _redis.GetDatabase();
        List<RedisKey> keys = [IndexSchemaDefinitions.BuildSemanticKey(tenantId, memoryUnitId)];
        IServer? server = GetAnyServer(_redis);
        if (server is not null)
        {
            await foreach (RedisKey key in server.KeysAsync(pattern: IndexSchemaDefinitions.BuildSemanticChunkKeyPattern(tenantId, memoryUnitId)).WithCancellation(ct))
            {
                if (IndexSchemaDefinitions.TryParseSemanticChunkKey(tenantId, key, out string parsedId, out _)
                    && string.Equals(parsedId, memoryUnitId, StringComparison.Ordinal))
                {
                    keys.Add(key);
                }
            }
        }

        await db.KeyDeleteAsync([.. keys]).WaitAsync(ct).ConfigureAwait(false);
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

    private async Task DeleteGraphNodeAsync(string tenantId, string memoryUnitId, CancellationToken ct)
    {
        FalkorDB falkor = new(_falkorDb.GetDatabase());
        (string query, IDictionary<string, object> parameters) = _graphQueryBuilder.BuildDeleteMemoryUnitNode(memoryUnitId);
        await falkor.SelectGraph(tenantId).QueryAsync(query, parameters).WaitAsync(GraphOperationTimeout, ct).ConfigureAwait(false);
    }

    private static IReadOnlyDictionary<string, string> PresenceMap(bool syntactic, bool semantic, bool graph)
        => new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["syntactic"] = syntactic ? "present" : "absent",
            ["semantic"] = semantic ? "present" : "absent",
            ["graph"] = graph ? "present" : "absent",
        };

    private static string FormatPresence(IReadOnlyDictionary<string, string> state)
        => $"syntactic={state["syntactic"]};semantic={state["semantic"]};graph={state["graph"]}";

    [LoggerMessage(
        EventId = 8202,
        Level = LogLevel.Information,
        Message = "RepairActionApplied tenant '{TenantId}' unit '{MemoryUnitId}' action {Recommendation} before=[{BeforeState}] after=[{AfterState}] duration={DurationMs}ms")]
    private static partial void LogRepairActionApplied(
        ILogger logger,
        string tenantId,
        string memoryUnitId,
        string recommendation,
        string beforeState,
        string afterState,
        long durationMs);

    [LoggerMessage(
        EventId = 8203,
        Level = LogLevel.Warning,
        Message = "UnrepairableDiscrepancy tenant '{TenantId}' unit '{MemoryUnitId}'")]
    private static partial void LogUnrepairable(ILogger logger, string tenantId, string memoryUnitId);

    [LoggerMessage(
        EventId = 8222,
        Level = LogLevel.Information,
        Message = "RepairNoOp tenant '{TenantId}' unit '{MemoryUnitId}' (re-verify reports consistent)")]
    private static partial void LogNoOpRepair(ILogger logger, string tenantId, string memoryUnitId);

    [LoggerMessage(
        EventId = 8223,
        Level = LogLevel.Error,
        Message = "RepairActionFailed tenant '{TenantId}' unit '{MemoryUnitId}' action {Recommendation} before=[{BeforeState}] after=[{AfterState}] duration={DurationMs}ms: {Error}")]
    private static partial void LogRepairActionFailed(
        ILogger logger,
        string tenantId,
        string memoryUnitId,
        string recommendation,
        string beforeState,
        string afterState,
        long durationMs,
        string error);

    [LoggerMessage(
        EventId = 8224,
        Level = LogLevel.Warning,
        Message = "RepairFailureStateProbeFailed tenant '{TenantId}' unit '{MemoryUnitId}': {Error}")]
    private static partial void LogRepairFailureStateProbeFailed(
        ILogger logger,
        string tenantId,
        string memoryUnitId,
        string error);
}
