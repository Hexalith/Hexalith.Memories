// <copyright file="ErrorResults.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Endpoints;

using System.Globalization;

using Hexalith.Memories.Contracts.V1;

using Microsoft.AspNetCore.Http;

/// <summary>Creates common endpoint error envelopes and HTTP results without changing the public wire shape.</summary>
internal static class ErrorResults
{
    /// <summary>Creates the common invalid tenant identifier envelope.</summary>
    /// <param name="message">Optional message to preserve a caller-specific validation reason.</param>
    /// <returns>The error response.</returns>
    public static ErrorResponse InvalidTenantId(string? message = null)
        => new(
            "INVALID_TENANT_ID",
            string.IsNullOrWhiteSpace(message)
                ? "TenantId contains invalid characters."
                : message,
            "Use only alphanumeric characters and hyphens for tenant identifiers.");

    /// <summary>Creates the common invalid tenant identifier HTTP result.</summary>
    /// <param name="message">Optional message to preserve a caller-specific validation reason.</param>
    /// <returns>A <c>400 Bad Request</c> result.</returns>
    public static IResult InvalidTenantIdResult(string? message = null) => Results.BadRequest(InvalidTenantId(message));

    /// <summary>Creates the tenant authorization failure envelope.</summary>
    /// <returns>The error response.</returns>
    public static ErrorResponse TenantForbidden()
        => new(
            "TENANT_FORBIDDEN",
            "The authenticated principal is not authorized for the requested tenant.",
            "Use a bearer token containing an authorized tenant claim for this tenant.");

    /// <summary>Creates the tenant authorization failure HTTP result.</summary>
    /// <returns>A <c>403 Forbidden</c> result.</returns>
    public static IResult TenantForbiddenResult()
        => Results.Json(TenantForbidden(), statusCode: StatusCodes.Status403Forbidden);

    /// <summary>Creates the common tenant-not-found envelope.</summary>
    /// <param name="tenantId">The tenant identifier.</param>
    /// <param name="suggestion">The recovery suggestion.</param>
    /// <returns>The error response.</returns>
    public static ErrorResponse TenantNotFound(
        string tenantId,
        string suggestion = "Use GET " + MemoriesRoutes.Tenants + " to list available tenants.")
        => new("TENANT_NOT_FOUND", $"Tenant '{tenantId}' not found.", suggestion);

    /// <summary>Maps a tenant-state envelope to its stable HTTP status.</summary>
    /// <param name="error">The tenant status error.</param>
    /// <returns><c>404</c> for missing tenants; otherwise <c>409</c>.</returns>
    public static IResult TenantStatusResult(ErrorResponse error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return string.Equals(error.Code, "TENANT_NOT_FOUND", StringComparison.Ordinal)
            ? Results.NotFound(error)
            : Results.Conflict(error);
    }

    /// <summary>Creates an invalid input envelope.</summary>
    /// <param name="message">The validation message.</param>
    /// <param name="suggestion">The recovery suggestion.</param>
    /// <param name="code">The error code.</param>
    /// <returns>The error response.</returns>
    public static ErrorResponse InvalidInput(
        string message,
        string suggestion,
        string code = "INVALID_INPUT")
        => new(code, message, suggestion);

    /// <summary>Creates a not-found envelope.</summary>
    /// <param name="code">The error code.</param>
    /// <param name="message">The error message.</param>
    /// <param name="suggestion">The recovery suggestion.</param>
    /// <returns>The error response.</returns>
    public static ErrorResponse NotFound(string code, string message, string suggestion)
        => new(code, message, suggestion);

    /// <summary>Creates a conflict envelope.</summary>
    /// <param name="code">The error code.</param>
    /// <param name="message">The error message.</param>
    /// <param name="suggestion">The recovery suggestion.</param>
    /// <returns>The error response.</returns>
    public static ErrorResponse Conflict(string code, string message, string suggestion)
        => new(code, message, suggestion);

    /// <summary>Creates the common DAPR unavailable envelope.</summary>
    /// <param name="message">Optional message to preserve an endpoint-specific dependency description.</param>
    /// <param name="suggestion">The recovery suggestion.</param>
    /// <returns>The error response.</returns>
    public static ErrorResponse DaprUnavailable(
        string message = "DAPR sidecar is not ready.",
        string suggestion = "Check service health via /healthz and retry.")
        => new("DAPR_UNAVAILABLE", message, suggestion);

    /// <summary>Creates the common DAPR unavailable HTTP result.</summary>
    /// <param name="message">Optional message to preserve an endpoint-specific dependency description.</param>
    /// <param name="suggestion">The recovery suggestion.</param>
    /// <returns>A <c>503 Service Unavailable</c> result.</returns>
    public static IResult DaprUnavailableResult(
        string message = "DAPR sidecar is not ready.",
        string suggestion = "Check service health via /healthz and retry.")
        => Results.Json(DaprUnavailable(message, suggestion), statusCode: StatusCodes.Status503ServiceUnavailable);

    /// <summary>Creates the common backend unavailable envelope.</summary>
    /// <param name="message">The error message.</param>
    /// <param name="suggestion">The recovery suggestion.</param>
    /// <param name="code">The error code.</param>
    /// <returns>The error response.</returns>
    public static ErrorResponse BackendUnavailable(
        string message = "Backend unavailable.",
        string suggestion = "Check Redis/FalkorDB connectivity and retry.",
        string code = "BACKEND_UNAVAILABLE")
        => new(code, message, suggestion);

    /// <summary>Creates the common backend unavailable HTTP result.</summary>
    /// <param name="message">The error message.</param>
    /// <param name="suggestion">The recovery suggestion.</param>
    /// <param name="code">The error code.</param>
    /// <returns>A <c>503 Service Unavailable</c> result.</returns>
    public static IResult BackendUnavailableResult(
        string message = "Backend unavailable.",
        string suggestion = "Check Redis/FalkorDB connectivity and retry.",
        string code = "BACKEND_UNAVAILABLE")
        => Results.Json(BackendUnavailable(message, suggestion, code), statusCode: StatusCodes.Status503ServiceUnavailable);

    /// <summary>Creates the common inbound rate-limit envelope.</summary>
    /// <returns>The error response.</returns>
    public static ErrorResponse RateLimitExceeded()
        => new(
            "RATE_LIMIT_EXCEEDED",
            "The tenant request rate limit was exceeded.",
            "Retry after the limiter window resets.");

    /// <summary>Creates the common inbound rate-limit HTTP result.</summary>
    /// <returns>A <c>429 Too Many Requests</c> result.</returns>
    public static IResult RateLimitExceededResult()
        => Results.Json(RateLimitExceeded(), statusCode: StatusCodes.Status429TooManyRequests);

    /// <summary>Creates the source-URI lookup backend-unavailable envelope.</summary>
    /// <returns>The error response.</returns>
    public static ErrorResponse LookupBackendUnavailable()
        => new(
            "LOOKUP_BACKEND_UNAVAILABLE",
            "The lookup backend is temporarily unavailable.",
            "Retry shortly; do not treat this as 'no unit exists'.");

    /// <summary>Creates the source-URI lookup backend-unavailable HTTP result.</summary>
    /// <returns>A <c>503 Service Unavailable</c> result.</returns>
    public static IResult LookupBackendUnavailableResult()
        => Results.Json(LookupBackendUnavailable(), statusCode: StatusCodes.Status503ServiceUnavailable);

    /// <summary>Creates the export backend-unavailable envelope.</summary>
    /// <param name="message">The endpoint-specific message.</param>
    /// <returns>The error response.</returns>
    public static ErrorResponse ExportBackendUnavailable(string message)
        => new(
            "EXPORT_BACKEND_UNAVAILABLE",
            message,
            "Check Redis/FalkorDB connectivity and retry the export.");

    /// <summary>Creates a sanitized unhandled-exception envelope.</summary>
    /// <returns>The error response.</returns>
    public static ErrorResponse UnhandledException()
        => new(
            "UNHANDLED_EXCEPTION",
            "An unexpected server error occurred.",
            "Retry the request. If the problem persists, contact support with the trace identifier.");

    /// <summary>Creates a sanitized unhandled-exception HTTP result.</summary>
    /// <returns>A <c>500 Internal Server Error</c> result.</returns>
    public static IResult UnhandledExceptionResult()
        => Results.Json(UnhandledException(), statusCode: StatusCodes.Status500InternalServerError);

    /// <summary>Sets the retry-after response header when the response is still mutable.</summary>
    /// <param name="httpContext">The HTTP context.</param>
    /// <param name="seconds">The retry-after delta in seconds.</param>
    public static void SetRetryAfter(HttpContext httpContext, int seconds)
    {
        ArgumentNullException.ThrowIfNull(httpContext);

        if (!httpContext.Response.HasStarted && !httpContext.Response.Headers.ContainsKey("Retry-After"))
        {
            httpContext.Response.Headers.RetryAfter = seconds.ToString(CultureInfo.InvariantCulture);
        }
    }
}
