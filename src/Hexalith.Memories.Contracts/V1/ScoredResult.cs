// <copyright file="ScoredResult.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Contracts.V1;

using System.Text.Json.Serialization;

/// <summary>A single search result with relevance score, reusable across all search axes.</summary>
public sealed record ScoredResult
{
    /// <summary>Gets the identifier of the matched memory unit.</summary>
    public required string MemoryUnitId { get; init; }

    /// <summary>Gets the relevance score (raw BM25 for syntactic axis).</summary>
    public required double Score { get; init; }

    /// <summary>Gets a truncated content snippet from the matched memory unit.</summary>
    public required string ContentSnippet { get; init; }

    /// <summary>Gets the source URI of the matched memory unit.</summary>
    public required string SourceUri { get; init; }

    /// <summary>Gets the source type of the matched memory unit.</summary>
    public required SourceType SourceType { get; init; }

    /// <summary>Gets the search axis that produced this result (e.g. "syntactic", "semantic", "graph").</summary>
    public string? Axis { get; init; }

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
