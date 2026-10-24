namespace Hexalith.Memories.Server.Activities.Ingestion;

using Dapr.Workflow;
using Hexalith.Memories.Server.Ingestion;

/// <summary>Persists candidate provenance before atomically making its exact revision visible.</summary>
internal sealed class PublishRefreshActivity(RefreshPublicationStore publication, SourceInventoryStore inventory)
    : WorkflowActivity<RefreshPublicationInput, bool>
{
    /// <inheritdoc />
    public override async Task<bool> RunAsync(WorkflowActivityContext context, RefreshPublicationInput input)
    {
        await inventory.CompleteAsync(input.Input.TenantId, input.Input.SourceUri, input.MemoryUnitId, input.Input.CaseId,
            input.MemoryUnitId, CancellationToken.None).ConfigureAwait(false);
        return await publication.PublishAsync(input.Input, input.MemoryUnitId, CancellationToken.None).ConfigureAwait(false);
    }
}
