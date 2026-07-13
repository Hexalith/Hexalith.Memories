// <copyright file="RestoreWorkflow.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Workflows;

using Dapr.Workflow;

using Hexalith.Memories.Server.Activities.Restore;

using Microsoft.Extensions.Logging;

/// <summary>
/// Story 26.2 — durable restore orchestration. Mirrors ingestion (a Dapr Workflow, not a hand-rolled queue) so
/// a long-running restore that re-embeds every unit is resumable, retried, and observable. Two phases:
/// <list type="number">
/// <item>restore the byte-exact data plane (syntactic hashes, cases/members, graph nodes + edges) in one
/// idempotent activity;</item>
/// <item>re-derive semantic vectors per unit (re-embed) so search works after restore.</item>
/// </list>
/// The orchestrator carries only ids and the staging key — the large payload stays out of workflow state.
/// Replay-safe: no wall-clock, randomness, or I/O in the orchestrator; every side effect is in an activity.
/// Idempotent: every activity is HSET-overwrite / MERGE, so a retried or resumed restore converges.
/// </summary>
public sealed class RestoreWorkflow : Workflow<RestoreWorkflowInput, RestoreWorkflowResult>
{
    /// <inheritdoc/>
    public override async Task<RestoreWorkflowResult> RunAsync(WorkflowContext context, RestoreWorkflowInput input)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(input);

        ILogger logger = context.CreateReplaySafeLogger<RestoreWorkflow>();

        context.SetCustomStatus("restoring-data-plane");
        RestoreDataPlaneResult dataPlane = await context.CallActivityAsync<RestoreDataPlaneResult>(
            nameof(RestoreDataPlaneActivity),
            new RestoreDataPlaneInput(input.TenantId, input.CaseId, input.StagingKey, input.RequestedBy),
            DefaultRetry());

        // Re-index each restored unit sequentially. Sequential (not fan-out) deliberately bounds pressure on the
        // embedding provider during a bulk restore; each re-index is idempotent so a resume re-runs safely.
        context.SetCustomStatus("reindexing");
        foreach (string memoryUnitId in dataPlane.MemoryUnitIds)
        {
            _ = await context.CallActivityAsync<RestoreReindexResult>(
                nameof(RestoreReindexUnitActivity),
                new RestoreReindexInput(input.TenantId, memoryUnitId),
                DefaultRetry());
        }

        // Best-effort staging cleanup (the TTL is the backstop if this is skipped).
        context.SetCustomStatus("cleaning-up");
        _ = await context.CallActivityAsync<bool>(
            nameof(DeleteRestoreStagingActivity),
            input.StagingKey,
            DefaultRetry());

        context.SetCustomStatus("completed");
        logger.LogInformation(
            "Restore complete for tenant {TenantId}: {Units} memory units, {Cases} cases, {Edges} edges restored, {Skipped} records skipped.",
            input.TenantId,
            dataPlane.MemoryUnitIds.Count,
            dataPlane.RestoredCaseCount,
            dataPlane.RestoredEdgeCount,
            dataPlane.SkippedRecords);

        return new RestoreWorkflowResult(
            dataPlane.MemoryUnitIds.Count,
            dataPlane.RestoredCaseCount,
            dataPlane.RestoredEdgeCount,
            dataPlane.SkippedRecords);
    }

    private static WorkflowTaskOptions DefaultRetry() => new(
        new WorkflowRetryPolicy(
            maxNumberOfAttempts: 3,
            firstRetryInterval: TimeSpan.FromSeconds(2),
            backoffCoefficient: 2.0,
            maxRetryInterval: TimeSpan.FromMinutes(1)));
}
