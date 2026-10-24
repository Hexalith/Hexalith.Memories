namespace Hexalith.Memories.Server.Activities.Ingestion;

using Dapr.Workflow;
using Hexalith.Memories.Server.Ingestion;

/// <summary>Retains canonical producer output durably before acknowledging completion.</summary>
internal sealed class RecordSourceInventoryOutputActivity(SourceInventoryStore inventory, RefreshPublicationStore publication, IConfiguration? configuration = null, IHostEnvironment? environment = null)
    : WorkflowActivity<SourceInventoryOutputInput, bool>
{
    /// <inheritdoc />
    public override async Task<bool> RunAsync(WorkflowActivityContext context, SourceInventoryOutputInput input)
    {
        await publication.CommitOrdinaryAsync(input.TenantId, input.CaseId, input.SourceUri, input.MemoryUnitId, CancellationToken.None).ConfigureAwait(false);
        await inventory.CompleteAsync(input.TenantId, input.SourceUri, input.WorkflowInstanceId, input.CaseId, input.MemoryUnitId,
            CancellationToken.None).ConfigureAwait(false);
        if (environment?.IsProduction() == false && configuration?.GetValue("Memories:Testing:EnableSourceInventoryQualification", false) == true
            && configuration.GetValue("Memories:Testing:UseFakeEmbedding", false))
        {
            int delay = Math.Clamp(configuration.GetValue("Memories:Testing:SourceInventoryOutputDelayMilliseconds", 0), 0, 5000);
            if (delay > 0) { await Task.Delay(delay).ConfigureAwait(false); }
        }
        return true;
    }
}
