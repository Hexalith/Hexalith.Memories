// <copyright file="MemoriesJsonContext.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Contracts.V1;

using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;

/// <summary>Source-generated JSON metadata for commonly exchanged Memories contracts.</summary>
[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(AddCaseMemberInput))]
[JsonSerializable(typeof(CreateAnnotationInput))]
[JsonSerializable(typeof(List<MemoryUnit>))]
[JsonSerializable(typeof(Case))]
[JsonSerializable(typeof(CaseActivityEvent))]
[JsonSerializable(typeof(CaseGroupSummary))]
[JsonSerializable(typeof(CaseActivityEventType))]
[JsonSerializable(typeof(CaseActivityInput))]
[JsonSerializable(typeof(CaseMember))]
[JsonSerializable(typeof(CaseMemberType))]
[JsonSerializable(typeof(CaseStatus))]
[JsonSerializable(typeof(CaseStatusDetail))]
[JsonSerializable(typeof(CreateCaseInput))]
[JsonSerializable(typeof(List<Case>))]
[JsonSerializable(typeof(List<CaseActivityEvent>))]
[JsonSerializable(typeof(List<CaseGroupSummary>))]
[JsonSerializable(typeof(List<CaseMember>))]
[JsonSerializable(typeof(AxisExplanation))]
[JsonSerializable(typeof(Dictionary<string, AxisExplanation>))]
[JsonSerializable(typeof(Dictionary<string, MetadataField>))]
[JsonSerializable(typeof(ConfidencePromotionRequest))]
[JsonSerializable(typeof(ConfidencePromotionResult))]
[JsonSerializable(typeof(EdgeTypeCategory))]
[JsonSerializable(typeof(ErrorResponse))]
[JsonSerializable(typeof(ExtractionInput))]
[JsonSerializable(typeof(FusedScoredResult))]
[JsonSerializable(typeof(FusionWeights))]
[JsonSerializable(typeof(HybridSearchResult))]
[JsonSerializable(typeof(ExtractionResult))]
[JsonSerializable(typeof(FailureDetails))]
[JsonSerializable(typeof(GraphEdge))]
[JsonSerializable(typeof(IReadOnlyList<ScoredResult>))]
[JsonSerializable(typeof(IndexInput))]
[JsonSerializable(typeof(IndexResult))]
[JsonSerializable(typeof(IngestionInput))]
[JsonSerializable(typeof(IngestionResult))]
[JsonSerializable(typeof(MemoryUnit))]
[JsonSerializable(typeof(MetadataField))]
[JsonSerializable(typeof(ScoredResult))]
[JsonSerializable(typeof(SearchExplanation))]
[JsonSerializable(typeof(SearchQuery))]
[JsonSerializable(typeof(SearchResult))]
[JsonSerializable(typeof(TenantEmbeddingConfig))]
[JsonSerializable(typeof(TraversalNode))]
[JsonSerializable(typeof(TraversalEdgeInfo))]
[JsonSerializable(typeof(TraversalGapMarker))]
[JsonSerializable(typeof(IReadOnlyList<TraversalGapMarker>))]
[JsonSerializable(typeof(TraversalResult))]
[JsonSerializable(typeof(IReadOnlyList<TraversalNode>))]
[JsonSerializable(typeof(TenantStatus))]
[JsonSerializable(typeof(TenantInfo))]
[JsonSerializable(typeof(TenantProvisioningInput))]
[JsonSerializable(typeof(TenantProvisioningResult))]
[JsonSerializable(typeof(TenantStatusUpdateInput))]
[JsonSerializable(typeof(TenantDeletionInput))]
[JsonSerializable(typeof(TenantDeletionResult))]
[JsonSerializable(typeof(BatchedGraphDeletionInput))]
[JsonSerializable(typeof(BatchedGraphDeletionResult))]
[JsonSerializable(typeof(IReadOnlyList<TenantInfo>))]
[JsonSerializable(typeof(TenantIsolationCheckResult))]
[JsonSerializable(typeof(TenantIsolationVerificationResult))]
// Story 5.5: tenant configuration & listing contracts.
[JsonSerializable(typeof(TenantSummary))]
[JsonSerializable(typeof(IReadOnlyList<TenantSummary>))]
[JsonSerializable(typeof(List<TenantSummary>))]
[JsonSerializable(typeof(TenantIndexSizes))]
[JsonSerializable(typeof(TenantIndexStatus))]
[JsonSerializable(typeof(IndexHealth))]
[JsonSerializable(typeof(TenantConfigurationView))]
[JsonSerializable(typeof(TenantUpdateInput))]
internal sealed partial class MemoriesJsonSourceGenerationContext : JsonSerializerContext;

/// <summary>Shared JSON serialization options for all Memories contracts.</summary>
public static class MemoriesJsonContext
{
    /// <summary>Gets the shared serializer options for Memories contracts and workflow payloads.</summary>
    public static JsonSerializerOptions Options { get; } = CreateOptions();

    private static JsonSerializerOptions CreateOptions()
        => new(JsonSerializerDefaults.Web)
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            TypeInfoResolver = JsonTypeInfoResolver.Combine(
                MemoriesJsonSourceGenerationContext.Default,
                new DefaultJsonTypeInfoResolver()),
        };
}
