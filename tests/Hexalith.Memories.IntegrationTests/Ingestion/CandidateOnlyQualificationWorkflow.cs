namespace Hexalith.Memories.IntegrationTests.Ingestion;

using Dapr.Workflow;
using Hexalith.Memories.Contracts.V1;
using Hexalith.Memories.Server.Activities.Ingestion;

/// <summary>Runs the real modern candidate-admission activity at the durable workflow boundary without unrelated extraction dependencies.</summary>
public sealed class CandidateOnlyQualificationWorkflow : Workflow<IngestionInput, IngestionResult>
{
    /// <inheritdoc />
    public override async Task<IngestionResult> RunAsync(WorkflowContext context, IngestionInput input)
    {
        await context.CallActivityAsync<bool>(nameof(RecordSourceInventoryCandidateActivity),
            new SourceInventoryOutputInput(input.TenantId, input.SourceUri, context.InstanceId, input.CaseId, context.InstanceId));
        return new(context.InstanceId, MemoryUnitStatus.Indexed, new(context.CurrentUtcDateTime, TimeSpan.Zero), false, null);
    }
}
