namespace Hexalith.Memories.Server.Ingestion;

using Hexalith.Memories.Server.Activities.Cases;
using Hexalith.Memories.Server.Activities.Ingestion;
using Hexalith.Memories.Server.Graph;
using StackExchange.Redis;

/// <summary>Physically reconciles retained producer artifacts only after independently proven terminal or unscheduled work.</summary>
internal sealed class SourceInventoryFailureReconciler(
    [FromKeyedServices("redis")] IConnectionMultiplexer redis,
    [FromKeyedServices("falkordb")] IConnectionMultiplexer graph,
    IGraphQueryBuilder queries, IWorkflowPayloadStore payloads, SourceInventoryStore inventory)
{
    /// <summary>Deletes exact scoped artifacts, preserving the obligation on every partial failure.</summary>
    internal async Task ReconcileAsync(string tenant, string source, SourceInventoryEntry entry, CancellationToken token)
    {
        if (entry.MemoryUnitId is { } canonical && entry.CandidateMemoryUnitId is { } candidate && canonical != candidate)
        {
            // Output persistence can succeed while its activity acknowledgements are lost.
            // Failed producer proof settles the writer, while retaining its canonical winner.
            await ReconcileDiscardedAsync(tenant, source, entry, token).ConfigureAwait(false);
            await inventory.RecordTerminalEvidenceAsync(tenant, source, entry, token).ConfigureAwait(false);
            return;
        }
        await DeleteArtifactsAsync(tenant, source, entry, token).ConfigureAwait(false);
        await inventory.RecordFailedDeletionAsync(tenant, source, entry, entry.CandidateMemoryUnitId ?? entry.WorkflowInstanceId!, token).ConfigureAwait(false);
    }

    /// <summary>Reconciles the retained loser only after independently scoped successful workflow completion.</summary>
    internal async Task ReconcileDiscardedAsync(string tenant, string source, SourceInventoryEntry entry, CancellationToken token)
    {
        if (entry.CandidateMemoryUnitId is null || entry.MemoryUnitId is null || entry.CandidateMemoryUnitId == entry.MemoryUnitId)
        { throw new InvalidOperationException("source-inventory-discarded-output-mismatch"); }
        await DeleteArtifactsAsync(tenant, source, entry, token).ConfigureAwait(false);
        await inventory.RecordDiscardedDeletionAsync(tenant, source, entry, token).ConfigureAwait(false);
    }

    private async Task DeleteArtifactsAsync(string tenant, string source, SourceInventoryEntry entry, CancellationToken token)
    {
        string candidate = entry.CandidateMemoryUnitId ?? entry.WorkflowInstanceId
            ?? throw new InvalidOperationException("source-inventory-failed-candidate-missing");
        await new DeleteMemoryUnitProjectionActivity(redis, graph, queries, inventory, payloadStore: payloads).RunAsync(null!,
            new(tenant, entry.CaseId, candidate, [])).WaitAsync(token).ConfigureAwait(false);
        if (entry.WorkflowInstanceId is { } workflow && workflow != candidate)
        {
            await new WorkflowPayloadLedger(redis).PurgeAsync(tenant, workflow, payloads, token).ConfigureAwait(false);
        }
        if (entry.SourcePayloadReference is { } payload)
        {
            if (payload.TenantId != tenant || (payload.MemoryUnitId != entry.WorkflowInstanceId && payload.MemoryUnitId != DedupKeyBuilder.BuildKey(tenant, entry.CaseId, source)))
            { throw new InvalidOperationException("source-inventory-failed-payload-scope"); }
            if (payload.MemoryUnitId != candidate && payload.MemoryUnitId != entry.WorkflowInstanceId)
            {
                await new WorkflowPayloadLedger(redis).PurgeAsync(tenant, payload.MemoryUnitId, payloads, token).ConfigureAwait(false);
            }
            await payloads.DeleteAsync(payload, token).ConfigureAwait(false);
            try
            {
                _ = await payloads.ReadAsync(payload, tenant, payload.MemoryUnitId, payload.ContentKind, token).ConfigureAwait(false);
                throw new InvalidOperationException("source-inventory-failed-payload-retained");
            }
            catch (WorkflowPayloadException error) when (error.ErrorCode == "PAYLOAD_NOT_FOUND") { }
        }
        IDatabase db = redis.GetDatabase();
        await db.KeyDeleteAsync(PersistFailedUnitActivity.BuildHashKey(tenant, candidate)).WaitAsync(token).ConfigureAwait(false);
        await db.SortedSetRemoveAsync(PersistFailedUnitActivity.BuildSortedSetKey(tenant, entry.CaseId), candidate).WaitAsync(token).ConfigureAwait(false);
    }
}
