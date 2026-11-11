namespace Hexalith.Memories.Server.Activities.Ingestion;

using Dapr.Workflow;
using Hexalith.Memories.Server.Ingestion;

/// <summary>Durably admits child producers before scheduling; fences stop every new source producer.</summary>
internal sealed class AdmitSourceInventoryActivity(SourceInventoryStore inventory, IDaprWorkflowClient workflowClient) : WorkflowActivity<SourceInventoryAdmissionInput, IngestionInput>
{
    /// <inheritdoc />
    public override async Task<IngestionInput> RunAsync(WorkflowActivityContext context, SourceInventoryAdmissionInput input)
    {
        WorkflowState? existing = await workflowClient.GetWorkflowStateAsync(input.WorkflowInstanceId, true, CancellationToken.None).ConfigureAwait(false);
        bool requireExisting = existing?.Exists == true;
        if (requireExisting)
        {
            IngestionInput? prior = existing!.ReadInputAs<IngestionInput>();
            if (existing.WorkflowName != nameof(Workflows.IngestionWorkflow) || prior?.SourceInventoryTracked != true
                || prior.TenantId != input.Input.TenantId || prior.CaseId != input.Input.CaseId || prior.SourceUri != input.Input.SourceUri)
            { throw new InvalidOperationException("source-inventory-existing-workflow-identity"); }
        }
        await inventory.AdmitAsync(input.WorkflowInstanceId, input.Input, CancellationToken.None, requireExisting).ConfigureAwait(false);
        return input.Input with { SourceInventoryTracked = true };
    }
}
