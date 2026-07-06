// <copyright file="SearchEndpointDegradationResponses.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Search;

using Hexalith.Memories.Contracts.V1;
using Hexalith.Memories.Server.Endpoints;

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

/// <summary>
/// Builds the structured degradation responses emitted by the search endpoints.
/// Centralizing these helpers keeps the minimal-API delegates thin and makes the status-code /
/// header behavior directly unit-testable.
/// </summary>
internal static class SearchEndpointDegradationResponses
{
    private const string GraphAxis = "graph";
    private const string PerAxisDegradationType = "per-axis";
    private const string RetryAfterSeconds = "5";
    private const string TotalDegradationType = "total";
    private const string TotalFailureReason = "all enabled axes unavailable";

    internal static IResult BuildBackendUnavailableResponse(
        HttpContext httpContext,
        ILogger logger,
        string axis,
        string tenantId,
        Exception exception)
    {
        SearchEndpointDegradationLog.LogBackendUnavailable(
            logger,
            axis,
            tenantId,
            SearchEndpointDegradationLog.DescribeFailureReason(exception),
            PerAxisDegradationType);
        AppendRetryAfter(httpContext);
        return ErrorResults.BackendUnavailableResult(
            "Search backend is unavailable.",
            "Retry the request; the backend auto-recovers when Redis reconnects.");
    }

    internal static IResult BuildGraphScopedAxisFailureResponse(
        HttpContext httpContext,
        ILogger logger,
        string axis,
        string tenantId,
        string? startNodeId,
        bool innerSearchStarted,
        Exception exception)
        => innerSearchStarted
            ? BuildBackendUnavailableResponse(httpContext, logger, axis, tenantId, exception)
            : BuildGraphUnavailableResponse(httpContext, logger, tenantId, startNodeId, exception);

    internal static IResult BuildGraphTimeoutResponse()
        => Results.Json(
            new ErrorResponse(
                "GRAPH_TIMEOUT",
                "Graph traversal timed out. The graph may be too dense for the requested depth.",
                "Try a smaller depth value."),
            statusCode: StatusCodes.Status504GatewayTimeout);

    internal static IResult BuildGraphUnavailableResponse(
        HttpContext httpContext,
        ILogger logger,
        string tenantId,
        string? startNodeId,
        Exception exception)
    {
        SearchEndpointDegradationLog.LogGraphUnavailable(
            logger,
            GraphAxis,
            tenantId,
            startNodeId,
            SearchEndpointDegradationLog.DescribeFailureReason(exception),
            PerAxisDegradationType);
        AppendRetryAfter(httpContext);
        return Results.Json(
            new ErrorResponse(
                "GRAPH_UNAVAILABLE",
                "Graph backend is unavailable.",
                "Retry the request; graph auto-recovers when FalkorDB reconnects. Check infrastructure status."),
            statusCode: StatusCodes.Status503ServiceUnavailable);
    }

    internal static IResult BuildAllBackendsUnavailableResponse(
        HttpContext httpContext,
        ILogger logger,
        string tenantId,
        IReadOnlyCollection<string> unavailableAxes,
        IReadOnlyCollection<string> enabledAxes)
    {
        string unavailable = string.Join(", ", unavailableAxes.OrderBy(static axis => axis, StringComparer.Ordinal));
        string enabled = string.Join(", ", enabledAxes.OrderBy(static axis => axis, StringComparer.Ordinal));
        SearchEndpointDegradationLog.LogHybridTotalFailure(
            logger,
            tenantId,
            unavailable,
            enabled,
            TotalFailureReason,
            TotalDegradationType);
        AppendRetryAfter(httpContext);
        return Results.Json(
            new ErrorResponse(
                "ALL_BACKENDS_UNAVAILABLE",
                $"All enabled search backends are unavailable: {unavailable}.",
                "Check infrastructure status (Redis Stack, FalkorDB). The service auto-recovers when backends reconnect; retry the request."),
            statusCode: StatusCodes.Status503ServiceUnavailable);
    }

    internal static void AppendRetryAfter(HttpContext httpContext)
    {
        ArgumentNullException.ThrowIfNull(httpContext);

        if (!httpContext.Response.HasStarted && !httpContext.Response.Headers.ContainsKey("Retry-After"))
        {
            httpContext.Response.Headers.Append("Retry-After", RetryAfterSeconds);
        }
    }
}
