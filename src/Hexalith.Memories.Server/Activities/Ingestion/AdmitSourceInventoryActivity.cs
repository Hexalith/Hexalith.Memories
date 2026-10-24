namespace Hexalith.Memories.Server.Activities.Ingestion;

using Dapr.Workflow;
using Hexalith.Memories.Server.Ingestion;

/// <summary>Durably admits child producers before scheduling; fences stop every new source producer.</summary>
internal sealed class AdmitSourceInventoryActivity(SourceInventoryStore inventory) : WorkflowActivity<SourceInventoryAdmissionInput, IngestionInput>
{
    /// <inheritdoc />
    public override async Task<IngestionInput> RunAsync(WorkflowActivityContext context, SourceInventoryAdmissionInput input)
    {
        await inventory.AdmitAsync(input.WorkflowInstanceId, input.Input, CancellationToken.None).ConfigureAwait(false);
        return input.Input with { SourceInventoryTracked = true };
    }
}
