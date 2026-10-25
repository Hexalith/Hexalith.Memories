namespace Hexalith.Memories.Server.NaturalLanguage;

using Dapr.Workflow;
using Hexalith.Memories.Server.Activities.Ingestion;
using Hexalith.Memories.Server.Activities.Indexing;
using Hexalith.Memories.Contracts.V1;
using StackExchange.Redis;

/// <summary>Resolves retry content inside one activity, retaining only identifiers and a content-free result in workflow history.</summary>
internal sealed class RetryNaturalLanguageEmbeddingActivity(
    [FromKeyedServices("redis")] IConnectionMultiplexer redis,
    IServiceProvider services)
    : WorkflowActivity<NaturalLanguageEmbeddingRetryInput, NaturalLanguageEmbeddingRetryResult>
{
    /// <inheritdoc/>
    public override async Task<NaturalLanguageEmbeddingRetryResult> RunAsync(WorkflowActivityContext context, NaturalLanguageEmbeddingRetryInput input)
    {
        Hexalith.Memories.Server.Activities.Indexing.TenantIdGuard.Validate(input.TenantId);
        Hexalith.Memories.Server.Ingestion.IngestionIdentityGuard.Handle(input.MemoryUnitId);
        if (!string.IsNullOrEmpty(input.RawJsonPayload))
        {
            throw new InvalidOperationException("nl-retry-input-must-be-content-free");
        }
        FailedNaturalLanguageEmbeddingRecord? record = await FailedNaturalLanguageEmbeddingRegistry.ReadCurrentAsync(redis.GetDatabase(), input.TenantId, input.MemoryUnitId).ConfigureAwait(false);
        if (record is null)
        {
            return new(false, "memory-unit-deleted-during-retry");
        }
        try
        {
            var describe = ActivatorUtilities.CreateInstance<GenerateNaturalLanguageDescriptionActivity>(services);
            var embed = ActivatorUtilities.CreateInstance<GenerateEmbeddingActivity>(services);
            var index = ActivatorUtilities.CreateInstance<IndexNaturalLanguageSemanticActivity>(services);
            NaturalLanguageDescriptionResult description = await describe.RunAsync(context,
                new NaturalLanguageDescriptionInput(record.TenantId, record.MemoryUnitId, record.TruncatedRawJsonPayload, record.EventType, record.AggregateType)).ConfigureAwait(false);
            if (await FailedNaturalLanguageEmbeddingRegistry.ReadCurrentAsync(redis.GetDatabase(), record.TenantId, record.MemoryUnitId).ConfigureAwait(false) != record)
            {
                return new(false, "retry-payload-retired");
            }
            EmbeddingResult vector = await embed.RunAsync(context,
                new EmbeddingInput(record.TenantId, description.Description, EmbeddingContentKind.NaturalLanguageDescription)).ConfigureAwait(false);
            if (await FailedNaturalLanguageEmbeddingRegistry.ReadCurrentAsync(redis.GetDatabase(), record.TenantId, record.MemoryUnitId).ConfigureAwait(false) != record)
            {
                return new(false, "retry-payload-retired");
            }
            await index.RunAsync(context, new NaturalLanguageIndexInput
            {
                TenantId = record.TenantId,
                MemoryUnitId = record.MemoryUnitId,
                CaseId = record.CaseId,
                EmbeddingVector = vector.Vector,
                EmbeddingProvider = vector.Provider,
                EmbeddingModel = string.IsNullOrWhiteSpace(vector.Model) ? record.EmbeddingModel : vector.Model,
                EmbeddingDimensions = vector.Dimensions,
                NaturalLanguageDescription = description.Description,
                DescriptionConfidence = description.EstimatedConfidence,
                ConfidenceSource = description.ConfidenceSource,
            }).ConfigureAwait(false);
            return new(true);
        }
        catch (NaturalLanguageDescriptionUnavailableException) { return new(false, "llm-still-unavailable"); }
    }
}
