namespace Hexalith.Memories.IntegrationTests.Ingestion;

using Dapr.Workflow;
using Hexalith.Memories.Contracts.V1;
using Hexalith.Memories.Server.Activities.Indexing;
using Hexalith.Memories.Server.Activities.Ingestion;

/// <summary>Historical untracked producer using the real semantic chunk writer and real durable scheduling.</summary>
public sealed class HistoricalCandidateArtifactWorkflow : Workflow<IngestionInput, IngestionResult>
{
    /// <inheritdoc />
    public override async Task<IngestionResult> RunAsync(WorkflowContext context, IngestionInput input)
    {
        await context.CallActivityAsync<IndexResult>(nameof(IndexSemanticChunksActivity), new SemanticChunkIndexInput
        {
            TenantId = input.TenantId, CaseId = input.CaseId, MemoryUnitId = context.InstanceId,
            EmbeddingProvider = "google", EmbeddingModel = "synthetic", EmbeddingDimensions = 3,
            Chunks = [new ChunkEmbeddingResult { Sequence = 7, Text = "owned historical chunk", StartOffset = 0,
                EndOffset = 22, EstimatedTokens = 4, Vector = [1, 0, 0] }],
        });
        return new(context.InstanceId, MemoryUnitStatus.Indexed, new(context.CurrentUtcDateTime, TimeSpan.Zero), false, null);
    }
}
