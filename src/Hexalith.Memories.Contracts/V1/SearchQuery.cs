// <copyright file="SearchQuery.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Contracts.V1;

/// <summary>Input for a syntactic (BM25) search query scoped to a tenant.</summary>
public sealed record SearchQuery
{
    /// <summary>Gets the tenant identifier to scope the search.</summary>
    public required string TenantId { get; init; }

    /// <summary>Gets the search terms to match against indexed content.</summary>
    public required string Query { get; init; }

    /// <summary>Gets an optional case identifier to further scope results.</summary>
    public string? CaseId { get; init; }

    /// <summary>Gets an optional source type filter to restrict results (e.g., "file", "url", "text", "api").</summary>
    public string? SourceTypeFilter { get; init; }

    /// <summary>Gets an optional metadata text query to filter results by metadata content.</summary>
    public string? MetadataQuery { get; init; }

    /// <summary>Gets the maximum number of results to return (default 10).</summary>
    public int MaxResults { get; init; } = 10;

    /// <summary>Gets the number of results to skip for pagination (default 0).</summary>
    public int Offset { get; init; } = 0;
}
