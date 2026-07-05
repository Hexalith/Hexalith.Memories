// <copyright file="HybridSearchResult.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Contracts.V1;

using System.Text.Json.Serialization;

/// <summary>Response envelope for a hybrid (multi-axis) search with fusion scoring and degradation info.</summary>
public sealed record HybridSearchResult
{
    /// <summary>Gets the ranked list of fused search results.</summary>
    public required IReadOnlyList<FusedScoredResult> Results { get; init; }

    /// <summary>Gets the total number of deduplicated fused results before pagination.</summary>
    public required long TotalCount { get; init; }

    /// <summary>Gets a value indicating whether any enabled axis failed at runtime.</summary>
    public required bool Degraded { get; init; }

    /// <summary>Gets the list of axis names that were enabled but failed (e.g., <c>["graph"]</c>).</summary>
    public required IReadOnlyList<string> UnavailableAxes { get; init; }

    /// <summary>
    /// Gets a value indicating whether every enabled-and-attempted axis landed in
    /// <see cref="UnavailableAxes"/>. <c>true</c> = total failure (endpoint should return 503);
    /// <c>false</c> = at least one axis produced a result (endpoint returns 200, possibly degraded);
    /// <c>null</c> = no axis was attempted (all skipped due to caller misconfiguration, endpoint
    /// returns 200 with empty results). Orthogonal to <see cref="Degraded"/>.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? AllEnabledAxesUnavailable { get; init; }

    /// <summary>Gets the original query string echoed back for correlation.</summary>
    public required string Query { get; init; }

    /// <summary>Gets the explain-mode metadata describing normalization methods and fusion weights. Null when explain=false.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public SearchExplanation? Explanation { get; init; }

    /// <summary>Gets the per-case result distribution summary, or null when no case attribution is available.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IReadOnlyList<CaseGroupSummary>? CaseGroups { get; init; }

    /// <summary>Gets the count of results omitted due to response truncation.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public int OmittedCount { get; init; }

    /// <summary>Gets the estimated token count before truncation.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public long EstimatedTokensTotal { get; init; }

    /// <summary>Gets the reason results were omitted from the response.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public OmittedReason OmittedReason { get; init; }

    /// <summary>Gets the axes that contributed to the response.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IReadOnlyList<string>? AxesUsed { get; init; }

    /// <summary>Gets the canonical evidence packet projection, when a surface attaches one.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public EvidencePacket? EvidencePacket { get; init; }
}

/// <summary>A single fused search result with per-axis rank contribution scores and a composite score.</summary>
public sealed record FusedScoredResult
{
    /// <summary>Gets the identifier of the matched memory unit.</summary>
    public required string MemoryUnitId { get; init; }

    /// <summary>Gets the final fused composite score in [0.0, 1.0].</summary>
    public required double CompositeScore { get; init; }

    /// <summary>Gets a truncated content snippet from the matched memory unit.</summary>
    public required string ContentSnippet { get; init; }

    /// <summary>Gets the source URI of the matched memory unit.</summary>
    public required string SourceUri { get; init; }

    /// <summary>Gets the source type of the matched memory unit.</summary>
    public required SourceType SourceType { get; init; }

    /// <summary>Gets the syntactic rank contribution score, or null if the axis was not queried or didn't find this unit.</summary>
    public double? SyntacticScore { get; init; }

    /// <summary>Gets the semantic rank contribution score, or null if the axis was not queried or didn't find this unit.</summary>
    public double? SemanticScore { get; init; }

    /// <summary>Gets the graph rank contribution score, or null if the axis was not queried or didn't find this unit.</summary>
    public double? GraphScore { get; init; }

    /// <summary>Gets the natural-language semantic rank contribution score, or null if the axis was not queried or didn't find this unit.</summary>
    public double? NlScore { get; init; }

    /// <summary>Gets the case identifier of the memory unit, or null if not associated with a case.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? CaseId { get; init; }

    /// <summary>Gets the case name of the memory unit, or null if not resolved.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? CaseName { get; init; }

    /// <summary>Gets the number of annotations linked to this memory unit.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public int AnnotationsCount { get; init; }
}
