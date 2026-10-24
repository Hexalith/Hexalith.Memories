namespace Hexalith.Memories.Server.Activities.Ingestion;

using Dapr.Workflow;
using Hexalith.Memories.Server.Ingestion;

/// <summary>Repairs acknowledged refresh publication across backends after a lost response or restart.</summary>
internal sealed class ReconcileRefreshPublicationActivity(RefreshPublicationStore publication) : WorkflowActivity<RefreshPublicationInput, bool>
{
    /// <inheritdoc />
    public override Task<bool> RunAsync(WorkflowActivityContext context, RefreshPublicationInput input)
        => publication.PublishAsync(input.Input, input.MemoryUnitId, CancellationToken.None);
}
