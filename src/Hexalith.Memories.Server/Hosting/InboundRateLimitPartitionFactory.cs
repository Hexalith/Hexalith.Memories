// <copyright file="InboundRateLimitPartitionFactory.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Hosting;

using System.Globalization;
using System.Threading.RateLimiting;

using Hexalith.Memories.Contracts.V1;
using Hexalith.Memories.Server.Authentication;
using Hexalith.Memories.Server.RateLimiting;
using Hexalith.Memories.Server.Telemetry;
using Hexalith.Memories.Telemetry;

using Microsoft.AspNetCore.RateLimiting;

/// <summary>Creates inbound rate-limit partitions and rejection responses.</summary>
internal static class InboundRateLimitPartitionFactory
{
    /// <summary>Writes the unchanged structured rate-limit rejection response.</summary>
    /// <param name="context">The rejection context.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A value task that completes when the response has been written.</returns>
    public static async ValueTask OnRejectedAsync(OnRejectedContext context, CancellationToken cancellationToken)
    {
        string tenantTag = ResolveRateLimitTenantTag(context.HttpContext);
        TelemetryMetricsRecorder.RecordRateLimitRejection(tenantTag, "RATE_LIMIT_EXCEEDED");
        if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out TimeSpan retryAfter))
        {
            context.HttpContext.Response.Headers.RetryAfter = Math.Ceiling(retryAfter.TotalSeconds)
                .ToString(CultureInfo.InvariantCulture);
        }

        context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
        await context.HttpContext.Response.WriteAsJsonAsync(
            new ErrorResponse(
                "RATE_LIMIT_EXCEEDED",
                "The tenant request rate limit was exceeded.",
                "Retry after the limiter window resets."),
            cancellationToken).ConfigureAwait(false);
    }

    internal static RateLimitPartition<string> CreatePartition(
        HttpContext httpContext,
        InboundRequestRateLimiter limiter)
    {
        ArgumentNullException.ThrowIfNull(httpContext);
        ArgumentNullException.ThrowIfNull(limiter);

        if (!IsRateLimitedApiPath(httpContext))
        {
            return RateLimitPartition.GetNoLimiter("__infrastructure__");
        }

        string partitionKey = ResolveRateLimitPartitionKey(httpContext);
        return limiter.CreatePartition(partitionKey);
    }

    private static bool IsRateLimitedApiPath(HttpContext httpContext)
    {
        PathString path = httpContext.Request.Path;
        if (!path.StartsWithSegments("/api"))
        {
            return false;
        }

        if (HttpMethods.IsPost(httpContext.Request.Method)
            && (path.Equals("/api/ingest", StringComparison.OrdinalIgnoreCase)
                || path.Equals("/api/ingest/url", StringComparison.OrdinalIgnoreCase)
                || path.Equals("/api/ingest/directory", StringComparison.OrdinalIgnoreCase)))
        {
            return false;
        }

        return true;
    }

    private static string ResolveRateLimitPartitionKey(HttpContext httpContext)
    {
        string tenantTag = ResolveRateLimitTenantTag(httpContext);
        if (!string.Equals(tenantTag, MemoriesMeter.RejectedTenantTag, StringComparison.Ordinal))
        {
            return "tenant:" + tenantTag;
        }

        string principal = AuditPrincipalResolver.Resolve(httpContext, activity: null);
        return string.Equals(principal, AccessTelemetryLog.UserAnonymous, StringComparison.Ordinal)
            ? "rejected:" + MemoriesMeter.RejectedTenantTag
            : "principal:" + principal;
    }

    private static string ResolveRateLimitTenantTag(HttpContext httpContext)
    {
        if (httpContext.Items.TryGetValue(AuthorizedTenantAccessor.HttpContextItemKey, out object? value)
            && value is string tenantId
            && !string.IsNullOrWhiteSpace(tenantId))
        {
            return tenantId;
        }

        return MemoriesMeter.RejectedTenantTag;
    }
}
