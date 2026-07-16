// <copyright file="SearchEndpointErrorResponseFactory.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Search;

using Hexalith.Memories.Contracts.V1;

using Hexalith.Memories.Server.Endpoints;
using Hexalith.Memories.Server.Ingestion;

/// <summary>Builds structured error responses for search endpoint failures.</summary>
internal static class SearchEndpointErrorResponseFactory
{
    private const string EmbeddingUnavailableSuggestion = "Check embedding provider configuration or retry later.";
    private const string DimensionMismatchSuggestion = "Align the tenant embedding configuration with the indexed vector dimensions, then reindex and retry.";
    private const string PaginationLimitSuggestion = "Reduce offset or maxResults so offset + maxResults stays within the supported candidate window.";

    /// <summary>Creates a structured 503 response for embedding provider failures.</summary>
    /// <param name="exception">The underlying embedding exception.</param>
    /// <returns>A machine-readable error response.</returns>
    public static ErrorResponse CreateEmbeddingUnavailable(Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);

        return new ErrorResponse(
            "EMBEDDING_UNAVAILABLE",
            exception.Message,
            EmbeddingUnavailableSuggestion);
    }

    /// <summary>Creates a structured 500 response for embedding dimension mismatches.</summary>
    /// <param name="exception">The dimension mismatch exception.</param>
    /// <returns>A machine-readable error response.</returns>
    public static ErrorResponse CreateDimensionMismatch(SemanticSearchDimensionMismatchException exception)
    {
        ArgumentNullException.ThrowIfNull(exception);

        return new ErrorResponse(
            "DIMENSION_MISMATCH",
            exception.Message,
            DimensionMismatchSuggestion);
    }

    /// <summary>Creates a structured 400 response for unsupported deep pagination requests.</summary>
    /// <param name="exception">The pagination-limit exception.</param>
    /// <returns>A machine-readable error response.</returns>
    public static ErrorResponse CreatePaginationLimitExceeded(SearchPaginationLimitExceededException exception)
    {
        ArgumentNullException.ThrowIfNull(exception);

        return ErrorResults.InvalidInput(
            exception.Message,
            PaginationLimitSuggestion,
            "PAGINATION_LIMIT_EXCEEDED");
    }
}
