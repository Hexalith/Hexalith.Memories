// <copyright file="SearchPaginationOptions.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Search;

/// <summary>Shared pagination bounds for search candidate retrieval windows.</summary>
internal static class SearchPaginationOptions
{
    /// <summary>Maximum public page size accepted by the HTTP search endpoint.</summary>
    public const int MaxPageSize = 100;

    /// <summary>Maximum candidate window supported for graph-scoped and hybrid deep paging.</summary>
    public const int MaxCandidateWindow = 1_000;

    /// <summary>Normalizes an offset to the service convention.</summary>
    /// <param name="offset">The requested offset.</param>
    /// <returns>A non-negative offset.</returns>
    public static int NormalizeOffset(int offset) => Math.Max(offset, 0);

    /// <summary>Normalizes a public page size to the endpoint-supported range.</summary>
    /// <param name="maxResults">The requested page size.</param>
    /// <returns>A page size in the range 1..100.</returns>
    public static int NormalizePageSize(int maxResults) => Math.Clamp(maxResults, 1, MaxPageSize);

    /// <summary>Normalizes an internal candidate retrieval size to the supported search window.</summary>
    /// <param name="maxResults">The requested candidate count.</param>
    /// <returns>A candidate count in the range 1..1000.</returns>
    public static int NormalizeCandidateSize(int maxResults) => Math.Clamp(maxResults, 1, MaxCandidateWindow);

    /// <summary>Calculates the candidate window needed to serve a fused or scoped page.</summary>
    /// <param name="searchMode">The search mode used in validation messages.</param>
    /// <param name="offset">The requested offset.</param>
    /// <param name="maxResults">The requested page size.</param>
    /// <returns>The required candidate window.</returns>
    /// <exception cref="SearchPaginationLimitExceededException">Thrown when the requested window exceeds the supported limit.</exception>
    public static int CalculateCandidateWindow(string searchMode, int offset, int maxResults)
    {
        int normalizedOffset = NormalizeOffset(offset);
        int normalizedMaxResults = NormalizePageSize(maxResults);

        int candidateWindow;
        try
        {
            candidateWindow = checked(normalizedOffset + normalizedMaxResults);
        }
        catch (OverflowException ex)
        {
            throw new SearchPaginationLimitExceededException(searchMode, normalizedOffset, normalizedMaxResults, MaxCandidateWindow, ex);
        }

        if (candidateWindow > MaxCandidateWindow)
        {
            throw new SearchPaginationLimitExceededException(searchMode, normalizedOffset, normalizedMaxResults, MaxCandidateWindow);
        }

        return candidateWindow;
    }
}
