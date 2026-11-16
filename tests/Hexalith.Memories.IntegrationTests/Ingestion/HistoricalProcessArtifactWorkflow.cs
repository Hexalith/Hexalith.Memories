namespace Hexalith.Memories.IntegrationTests.Ingestion;

using Dapr.Workflow;
using Hexalith.Memories.Contracts.V1;
using Hexalith.Memories.Server.Activities.Indexing;
using Hexalith.Memories.Server.Activities.Ingestion;

/// <summary>Extends the retained historical chunk producer with the real untracked NL writer, without seeded artifacts.</summary>
public sealed class HistoricalProcessArtifactWorkflow : Workflow<IngestionInput, IngestionResult>
{
    /// <inheritdoc />
    public override async Task<IngestionResult> RunAsync(WorkflowContext context, IngestionInput input)
    {
        IngestionResult result = await new HistoricalCandidateArtifactWorkflow().RunAsync(context, input);
        await context.CallActivityAsync<IndexResult>(nameof(IndexNaturalLanguageSemanticActivity), new NaturalLanguageIndexInput
        {
            TenantId = input.TenantId, CaseId = input.CaseId, MemoryUnitId = context.InstanceId,
            EmbeddingProvider = "google", EmbeddingModel = "synthetic", EmbeddingDimensions = 3,
            EmbeddingVector = [1f, 0f, 0f], NaturalLanguageDescription = "Owned actual historical process NL.",
            ConfidenceSource = ConfidenceSource.Constant,
        });
        return result;
    }
}
