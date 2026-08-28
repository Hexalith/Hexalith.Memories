// <copyright file="ApplyDerivedStoreCorrectionActivity.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.DerivedStores;

using Dapr.Workflow;

using Hexalith.Memories.Contracts.V1;
using Hexalith.Memories.Contracts.V1.DerivedStores;
using Hexalith.Memories.Server.Activities.Indexing;
using Hexalith.Memories.Server.Activities.Ingestion;
using Hexalith.Memories.Server.Ingestion;
using Hexalith.Memories.Server.Workflows.Contracts;

/// <summary>Applies and durably fences one complete tenant/intake correction convergence.</summary>
internal sealed class ApplyDerivedStoreCorrectionActivity(
    RedisDerivedStoreService store,
    ExtractContentActivity extractContent,
    GenerateChunkEmbeddingsActivity generateChunkEmbeddings,
    GenerateNaturalLanguageDescriptionActivity generateNaturalLanguageDescription,
    GenerateEmbeddingActivity generateEmbedding,
    IndexSyntacticActivity indexSyntactic,
    IndexSemanticChunksActivity indexSemantic,
    IndexNaturalLanguageSemanticActivity indexNaturalLanguageSemantic,
    IndexGraphActivity indexGraph,
    IWorkflowPayloadStore payloadStore)
    : WorkflowActivity<DerivedStoreCorrectionWorkflowInput, DerivedStoreCorrectionStatus>
{
    /// <inheritdoc/>
    public override Task<DerivedStoreCorrectionStatus> RunAsync(
        WorkflowActivityContext context,
        DerivedStoreCorrectionWorkflowInput input)
    {
        ArgumentNullException.ThrowIfNull(input);
        return store.ApplyCorrectionAsync(
            input.TenantId,
            input.OperationId,
            (artifact, status, cancellationToken) => RegenerateAsync(
                context,
                artifact,
                status,
                cancellationToken),
            CancellationToken.None);
    }

    private async Task RegenerateAsync(
        WorkflowActivityContext context,
        DurableDerivedStoreSourceArtifact artifact,
        DerivedStoreCorrectionStatus status,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ExtractionResult extraction = await extractContent.RunAsync(
            context,
            new ExtractionInput(
                artifact.SourceUri,
                artifact.SourceBytes,
                artifact.ContentType,
                artifact.SourceType,
                artifact.TenantId,
                artifact.MemoryUnitId)).ConfigureAwait(false);
        ChunkEmbeddingBatchResult embedding = await generateChunkEmbeddings.RunAsync(
            context,
            new EmbeddingInput(
                artifact.TenantId,
                extraction.ExtractedContent,
                EmbeddingContentKind.Payload,
                extraction.ExtractedContentReference)).ConfigureAwait(false);

        string model = embedding.Model ?? ResolveModel(embedding.Provider);
        ChunkEmbeddingResult firstChunk = embedding.Chunks[0];
        var indexInput = new IndexInput
        {
            MemoryUnitId = artifact.MemoryUnitId,
            TenantId = artifact.TenantId,
            CaseId = status.CorrectedCaseId,
            Content = extraction.ExtractedContent,
            ContentReference = extraction.ExtractedContentReference,
            ContentHash = extraction.ContentHash,
            SourceUri = artifact.SourceUri,
            SourceType = artifact.SourceType,
            IngestedBy = artifact.IngestedBy,
            IngestedAt = artifact.CapturedAtUtc,
            EmbeddingVector = firstChunk.Vector,
            EmbeddingVectorReference = firstChunk.VectorReference,
            EmbeddingProvider = embedding.Provider,
            EmbeddingModel = model,
            EmbeddingDimensions = embedding.Dimensions,
            Metadata = artifact.Metadata,
            CausationId = artifact.CausationId,
            CorrelationId = artifact.CorrelationId,
        };
        var semanticInput = new SemanticChunkIndexInput
        {
            MemoryUnitId = artifact.MemoryUnitId,
            TenantId = artifact.TenantId,
            CaseId = status.CorrectedCaseId,
            Chunks = embedding.Chunks,
            EmbeddingProvider = embedding.Provider,
            EmbeddingModel = model,
            EmbeddingDimensions = embedding.Dimensions,
            Metadata = artifact.Metadata,
        };

        try
        {
            _ = await indexSyntactic.RunAsync(context, indexInput).ConfigureAwait(false);
            _ = await indexSemantic.RunAsync(context, semanticInput).ConfigureAwait(false);
            _ = await indexGraph.RunAsync(context, indexInput).ConfigureAwait(false);

            if (artifact.SourceType == SourceType.Event)
            {
                string rawJson = System.Text.Encoding.UTF8.GetString(artifact.SourceBytes);
                string eventType = artifact.Metadata.TryGetValue("cloudevent.type", out MetadataField? type)
                    ? type.Value
                    : "unknown";
                string? aggregateType = artifact.Metadata.TryGetValue("event.aggregateType", out MetadataField? aggregate)
                    ? aggregate.Value
                    : null;
                NaturalLanguageDescriptionResult description = await generateNaturalLanguageDescription.RunAsync(
                    context,
                    new NaturalLanguageDescriptionInput(
                        artifact.TenantId,
                        artifact.MemoryUnitId,
                        rawJson,
                        eventType,
                        aggregateType)).ConfigureAwait(false);
                EmbeddingResult naturalLanguageEmbedding = await generateEmbedding.RunAsync(
                    context,
                    new EmbeddingInput(
                        artifact.TenantId,
                        description.Description,
                        EmbeddingContentKind.NaturalLanguageDescription)).ConfigureAwait(false);
                _ = await indexNaturalLanguageSemantic.RunAsync(
                    context,
                    new NaturalLanguageIndexInput
                    {
                        MemoryUnitId = artifact.MemoryUnitId,
                        TenantId = artifact.TenantId,
                        CaseId = status.CorrectedCaseId,
                        EmbeddingVector = naturalLanguageEmbedding.Vector,
                        EmbeddingProvider = naturalLanguageEmbedding.Provider,
                        EmbeddingModel = naturalLanguageEmbedding.Model ?? ResolveModel(naturalLanguageEmbedding.Provider),
                        EmbeddingDimensions = naturalLanguageEmbedding.Dimensions,
                        NaturalLanguageDescription = description.Description,
                        DescriptionConfidence = description.EstimatedConfidence,
                        ConfidenceSource = description.ConfidenceSource,
                    }).ConfigureAwait(false);
            }
        }
        finally
        {
            await DeleteReferenceAsync(extraction.ExtractedContentReference).ConfigureAwait(false);
            foreach (ChunkEmbeddingResult chunk in embedding.Chunks)
            {
                await DeleteReferenceAsync(chunk.TextReference).ConfigureAwait(false);
                await DeleteReferenceAsync(chunk.VectorReference).ConfigureAwait(false);
            }
        }
    }

    private Task DeleteReferenceAsync(WorkflowPayloadReference? reference)
        => reference is null ? Task.CompletedTask : payloadStore.DeleteAsync(reference, CancellationToken.None);

    private static string ResolveModel(string provider)
    {
        int separator = provider.IndexOf(':', StringComparison.Ordinal);
        return separator >= 0 && separator < provider.Length - 1 ? provider[(separator + 1)..] : provider;
    }
}
