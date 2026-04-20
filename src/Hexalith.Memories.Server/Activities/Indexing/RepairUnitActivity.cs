// <copyright file="RepairUnitActivity.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Activities.Indexing;

using Dapr.Workflow;

using Hexalith.Memories.Contracts.V1;
using Hexalith.Memories.Server.Consistency;
using Hexalith.Memories.Server.Graph;

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
        ArgumentException.ThrowIfNullOrWhiteSpace(input.MemoryUnitId);

        CancellationToken ct = CancellationToken.None;

        // Short-circuit: if the stale recommendation is already Unrepairable and the caller
        // opted out of attempting it, surface the flag directly without re-probing.
        if (input.Recommendation == ConsistencyRepairRecommendation.Unrepairable && !input.IncludeUnrepairable)
        {
            LogUnrepairable(_logger, input.TenantId, input.MemoryUnitId);
            return new RepairActionRecord(
                input.MemoryUnitId,
                ConsistencyRepairRecommendation.Unrepairable,
                Succeeded: false,
                FailureReason: "Unrepairable — nothing remains in any backend or syntactic source is gone.",
                BeforeState: PresenceMap(false, false, false),
                AfterState: PresenceMap(false, false, false));
        }

        // Risk #1: fresh re-verify before any write. If the unit is consistent now, skip.
        ConsistencyInspectionResult freshInspection;
        try
        {
            freshInspection = await _inspectionService
                .InspectAsync(input.TenantId, input.MemoryUnitId, ct)
                .ConfigureAwait(false);
        }
        catch (KeyNotFoundException)
        {
            // All three backends now report absent — nothing to repair.
            return new RepairActionRecord(
                input.MemoryUnitId,
                ConsistencyRepairRecommendation.NoOp,
                Succeeded: true,
                FailureReason: null,
                BeforeState: PresenceMap(false, false, false),
                AfterState: PresenceMap(false, false, false));
        }

        IReadOnlyDictionary<string, string> beforeState = PresenceMap(
            freshInspection.SyntacticPresent,
            freshInspection.SemanticPresent,
            freshInspection.GraphPresent);

        ConsistencyRepairRecommendation recommendation = freshInspection.Recommendation;

        if (recommendation == ConsistencyRepairRecommendation.NoOp)
        {
            LogNoOpRepair(_logger, input.TenantId, input.MemoryUnitId);
            return new RepairActionRecord(
                input.MemoryUnitId,
                ConsistencyRepairRecommendation.NoOp,
                Succeeded: true,
                FailureReason: null,
                BeforeState: beforeState,
                AfterState: beforeState);
        }

        if (recommendation == ConsistencyRepairRecommendation.Unrepairable)
        {
            LogUnrepairable(_logger, input.TenantId, input.MemoryUnitId);
            return new RepairActionRecord(
                input.MemoryUnitId,
                ConsistencyRepairRecommendation.Unrepairable,
                Succeeded: false,
                FailureReason: "Unrepairable — nothing remains in any backend or syntactic source is gone.",
                BeforeState: beforeState,
                AfterState: beforeState);
        }

        try
        {
            IReadOnlyDictionary<string, string> afterState = await ApplyRepairAsync(
                input.TenantId,
                input.MemoryUnitId,
                recommendation,
                ct).ConfigureAwait(false);

            LogRepairActionApplied(
                _logger,
                input.TenantId,
                input.MemoryUnitId,
                recommendation.ToString());

            return new RepairActionRecord(
                input.MemoryUnitId,
                recommendation,
                Succeeded: true,
                FailureReason: null,
                BeforeState: beforeState,
                AfterState: afterState);
        }
        catch (Exception ex)
        {
            LogRepairActionFailed(_logger, input.TenantId, input.MemoryUnitId, recommendation.ToString(), ex.Message);
            return new RepairActionRecord(
                input.MemoryUnitId,
                recommendation,
                Succeeded: false,
                FailureReason: ex.Message,
                BeforeState: beforeState,
                AfterState: beforeState);
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
        await db.KeyDeleteAsync($"{tenantId}:vec:{memoryUnitId}").WaitAsync(ct).ConfigureAwait(false);
    }

    private async Task DeleteGraphNodeAsync(string tenantId, string memoryUnitId, CancellationToken ct)
    {
        FalkorDB falkor = new(_falkorDb.GetDatabase());
        (string query, IDictionary<string, object> parameters) = _graphQueryBuilder.BuildDeleteMemoryUnitNode(memoryUnitId);
        await falkor.QueryAsync(tenantId, query, parameters).WaitAsync(GraphOperationTimeout, ct).ConfigureAwait(false);
    }

    private static IReadOnlyDictionary<string, string> PresenceMap(bool syntactic, bool semantic, bool graph)
        => new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["syntactic"] = syntactic ? "present" : "absent",
            ["semantic"] = semantic ? "present" : "absent",
            ["graph"] = graph ? "present" : "absent",
        };

    [LoggerMessage(
        EventId = 8202,
        Level = LogLevel.Information,
        Message = "RepairActionApplied tenant '{TenantId}' unit '{MemoryUnitId}' action {Recommendation}")]
    private static partial void LogRepairActionApplied(
        ILogger logger, string tenantId, string memoryUnitId, string recommendation);

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
        Message = "RepairActionFailed tenant '{TenantId}' unit '{MemoryUnitId}' action {Recommendation}: {Error}")]
    private static partial void LogRepairActionFailed(
        ILogger logger, string tenantId, string memoryUnitId, string recommendation, string error);
}
