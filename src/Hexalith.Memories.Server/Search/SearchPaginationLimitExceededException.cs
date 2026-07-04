// <copyright file="SearchPaginationLimitExceededException.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Search;

/// <summary>Signals that a search request asks beyond the supported candidate retrieval window.</summary>
internal sealed class SearchPaginationLimitExceededException : Exception
{
    /// <summary>Initializes a new instance of the <see cref="SearchPaginationLimitExceededException"/> class.</summary>
    /// <param name="searchMode">The search mode that rejected the request.</param>
    /// <param name="offset">The normalized offset.</param>
    /// <param name="maxResults">The normalized page size.</param>
    /// <param name="maxCandidateWindow">The maximum supported candidate window.</param>
    /// <param name="innerException">The optional inner exception.</param>
    public SearchPaginationLimitExceededException(
        string searchMode,
        int offset,
        int maxResults,
        int maxCandidateWindow,
        Exception? innerException = null)
        : base(
            $"{searchMode} search supports offset + maxResults up to {maxCandidateWindow}. " +
            $"Requested offset {offset} with maxResults {maxResults}.",
            innerException)
    {
        SearchMode = searchMode;
        Offset = offset;
        MaxResults = maxResults;
        MaxCandidateWindow = maxCandidateWindow;
    }

    /// <summary>Gets the search mode that rejected the request.</summary>
    public string SearchMode { get; }

    /// <summary>Gets the normalized offset.</summary>
    public int Offset { get; }

    /// <summary>Gets the normalized page size.</summary>
    public int MaxResults { get; }

    /// <summary>Gets the maximum supported candidate window.</summary>
    public int MaxCandidateWindow { get; }
}
