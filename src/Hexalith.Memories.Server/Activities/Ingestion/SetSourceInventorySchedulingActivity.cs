namespace Hexalith.Memories.Server.Activities.Ingestion;

using Dapr.Workflow;
using Hexalith.Memories.Server.Ingestion;

/// <summary>Persists deterministic workflow scheduling provenance immediately before or instead of its child call.</summary>
internal sealed class SetSourceInventorySchedulingActivity(SourceInventoryStore inventory) : WorkflowActivity<SourceInventorySchedulingInput, bool>
{
    /// <inheritdoc />
    public override async Task<bool> RunAsync(WorkflowActivityContext context, SourceInventorySchedulingInput input)
    {
        await inventory.SetSchedulingAsync(input.TenantId, input.SourceUri, input.WorkflowInstanceId, input.State, CancellationToken.None).ConfigureAwait(false);
        return true;
    }
}
