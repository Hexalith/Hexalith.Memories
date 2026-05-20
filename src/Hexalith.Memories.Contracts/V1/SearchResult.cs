// <copyright file="SearchResult.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Contracts.V1;

using System.Text.Json.Serialization;

/// <summary>Search response envelope containing ranked results and metadata.</summary>
public sealed record SearchResult
{
    /// <summary>Gets the ranked list of search results.</summary>
    public required IReadOnlyList<ScoredResult> Results { get; init; }

    /// <summary>Gets the total number of matching documents (may exceed returned results).</summary>
    public required long TotalCount { get; init; }

    /// <summary>Gets a value indicating whether the tenant currently has indexed memory units.</summary>
    public required bool HasIndexedMemoryUnits { get; init; }

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

    /// <summary>Gets a value indicating whether any expected backend component was unavailable.</summary>
    /// <remarks>
    /// Single-axis results use a simple boolean because only one search axis contributes to the result.
    /// Hybrid results additionally expose <see cref="HybridSearchResult.AllEnabledAxesUnavailable"/>
    /// to distinguish partial degradation from total multi-axis failure.
    /// </remarks>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public bool Degraded { get; init; }

    /// <summary>Gets the axis or component names that were unavailable at runtime.</summary>
    /// <remarks>
    /// Empty or omitted means the single-axis endpoint executed against its expected dependencies.
    /// Hybrid callers should also inspect <see cref="HybridSearchResult.AllEnabledAxesUnavailable"/>
    /// when they need the tri-state all-axes failure signal.
    /// </remarks>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IReadOnlyList<string>? UnavailableAxes { get; init; }

    /// <summary>Gets the axes that contributed to the response.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IReadOnlyList<string>? AxesUsed { get; init; }

    /// <summary>Gets the canonical evidence packet projection, when a surface attaches one.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public EvidencePacket? EvidencePacket { get; init; }
}
