namespace Hexalith.Memories.Server.Activities.Ingestion;

using Dapr.Workflow;
using Hexalith.Memories.Server.Ingestion;

/// <summary>Retains the artifact identity before the producer performs any content writes.</summary>
internal sealed class RecordSourceInventoryCandidateActivity(SourceInventoryStore inventory) : WorkflowActivity<SourceInventoryOutputInput, bool>
{
    /// <inheritdoc />
    public override async Task<bool> RunAsync(WorkflowActivityContext context, SourceInventoryOutputInput input)
    {
        await inventory.RecordCandidateAsync(input.TenantId, input.SourceUri, input.WorkflowInstanceId, input.MemoryUnitId, CancellationToken.None).ConfigureAwait(false);
        return true;
    }
}
