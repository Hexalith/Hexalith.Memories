namespace Hexalith.Memories.Server.Endpoints;

using Hexalith.Memories.Server.Authentication;
using Hexalith.Memories.Server.Ingestion;

using Microsoft.AspNetCore.Mvc;

/// <summary>Exact tenant/source inventory, independent of the current case and source deduplication pointer.</summary>
internal static class SourceInventoryEndpoints
{
    internal const string Route = MemoriesRoutes.SourceInventory;

    /// <summary>Maps bounded authenticated inventory reads and admission fencing.</summary>
    internal static IEndpointRouteBuilder MapSourceInventoryEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet(MemoriesRoutes.SourceInventory, async (SourceInventoryStore store, IIngestionWorkflowStateReader workflows, SourceInventoryFailureReconciler failures, string tenantId, [FromQuery] string sourceUri,
            [FromQuery] string? cursor, [FromQuery] int? pageSize, CancellationToken cancellationToken) =>
        {
            try
            {
                SourceInventoryPage page = await store.ReadAsync(tenantId, sourceUri, cursor, pageSize ?? 100, cancellationToken).ConfigureAwait(false);
                using var observation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                observation.CancelAfter(TimeSpan.FromSeconds(5));
                foreach (SourceInventoryEntry entry in page.Entries.Where(entry => entry.WorkflowInstanceId is not null
                    && !entry.TerminalVerified))
                {
                    if (page.AdmissionClosed && entry.SchedulingState == "Unscheduled")
                    {
                        await failures.ReconcileAsync(tenantId, sourceUri, entry, observation.Token).ConfigureAwait(false);
                        continue;
                    }
                    Dapr.Workflow.WorkflowState? state = await workflows.GetWorkflowStateAsync(entry.WorkflowInstanceId!, true, observation.Token).ConfigureAwait(false);
                    IngestionInput? stored = null;
                    try { if (state?.Exists == true) { stored = state.ReadInputAs<IngestionInput>(); } }
                    catch (System.Text.Json.JsonException) { }
                    if (stored?.TenantId != tenantId || stored.CaseId != entry.CaseId || stored.SourceUri != sourceUri) { continue; }
                    if (page.AdmissionClosed && state?.RuntimeStatus == Dapr.Workflow.WorkflowRuntimeStatus.Failed)
                    {
                        await failures.ReconcileAsync(tenantId, sourceUri, entry, observation.Token).ConfigureAwait(false);
                        continue;
                    }
                    if (IngestionWorkflowStatusMapper.TryMap(entry.WorkflowInstanceId!, state, out IngestionWorkflowStatus? status)
                        && status is { RuntimeStatus: "Completed" } && status.TenantId == tenantId && status.CaseId == entry.CaseId
                        && status.MemoryUnitId == entry.MemoryUnitId && (status.MemoryUnitStatus == MemoryUnitStatus.Indexed
                            || status is { MemoryUnitStatus: MemoryUnitStatus.Failed, FailureSummary: "source-refresh-superseded" }))
                    {
                        if (entry.CandidateMemoryUnitId is { } candidate && candidate != entry.MemoryUnitId && !entry.CandidateArtifactsDeleted)
                        {
                            if (!page.AdmissionClosed) { continue; }
                            await failures.ReconcileDiscardedAsync(tenantId, sourceUri, entry, observation.Token).ConfigureAwait(false);
                        }
                        await store.RecordTerminalEvidenceAsync(tenantId, sourceUri, entry, observation.Token).ConfigureAwait(false);
                    }
                }
                return Results.Ok(await store.ReadAsync(tenantId, sourceUri, cursor, pageSize ?? 100, cancellationToken).ConfigureAwait(false));
            }
            catch (ArgumentException) { return Results.BadRequest(new ErrorResponse("INVALID_INPUT", "Invalid inventory scope or page size.", "Supply an exact tenant/source and a page size from 1 to 100.")); }
            catch (InvalidOperationException) { return Results.Conflict(new ErrorResponse("INVENTORY_INCOMPLETE", "Inventory changed or could not be read safely.", "Retain discovered obligations and retry from the first page.")); }
        }).AddEndpointFilter<TenantAuthorizationEndpointFilter>();
        app.MapPost(MemoriesRoutes.SourceInventoryFence, async (SourceInventoryStore store, string tenantId, [FromQuery] string sourceUri,
            [FromQuery] bool permanent, CancellationToken cancellationToken) =>
        {
            await store.FenceAsync(tenantId, sourceUri, permanent, cancellationToken).ConfigureAwait(false);
            return Results.NoContent();
        }).AddEndpointFilter<TenantAuthorizationEndpointFilter>();
        return app;
    }
}
