// <copyright file="SearchResult.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Contracts.V1;

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
}
