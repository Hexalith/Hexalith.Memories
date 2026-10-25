namespace Hexalith.Memories.IntegrationTests.Ingestion;

using Dapr.Workflow;
using Hexalith.Memories.Contracts.V1;
using Hexalith.Memories.Server.Activities.Ingestion;
using Hexalith.Memories.Server.Workflows.Contracts;

/// <summary>Executes the historical content-bearing description activity protocol.</summary>
public sealed class ContentBearingLegacyRetryWorkflow : Workflow<NaturalLanguageEmbeddingRetryInput, NaturalLanguageDescriptionResult>
{
    /// <inheritdoc/>
    public override Task<NaturalLanguageDescriptionResult> RunAsync(WorkflowContext context, NaturalLanguageEmbeddingRetryInput input)
        => context.CallActivityAsync<NaturalLanguageDescriptionResult>(nameof(GenerateNaturalLanguageDescriptionActivity),
            new NaturalLanguageDescriptionInput(input.TenantId, input.MemoryUnitId, input.RawJsonPayload, input.EventType, input.AggregateType));
}
