// <copyright file="InboundRateLimitEndpointFilter.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.RateLimiting;

using System.Globalization;
using System.Threading.RateLimiting;

using Hexalith.Memories.Server.Authentication;
using Hexalith.Memories.Server.Endpoints;
using Hexalith.Memories.Server.Telemetry;
using Hexalith.Memories.Telemetry;

using Microsoft.AspNetCore.Http;

/// <summary>Endpoint filter limiter for body-bound tenant requests after tenant authorization has run.</summary>
internal sealed class InboundRateLimitEndpointFilter(
    IAuthorizedTenantAccessor authorizedTenantAccessor,
    InboundRequestRateLimiter limiter) : IEndpointFilter
{
    /// <inheritdoc />
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(next);

        string tenantTag = authorizedTenantAccessor.TryGetAuthorizedTenant(out string tenantId)
            ? tenantId
            : MemoriesMeter.RejectedTenantTag;

        using RateLimitLease lease = await limiter
            .AcquireAsync("tenant:" + tenantTag, context.HttpContext.RequestAborted)
            .ConfigureAwait(false);
        if (!lease.IsAcquired)
        {
            TelemetryMetricsRecorder.RecordRateLimitRejection(tenantTag, "RATE_LIMIT_EXCEEDED");
            SetRetryAfterHeader(context.HttpContext, lease);
            return ErrorResults.RateLimitExceededResult();
        }

        return await next(context).ConfigureAwait(false);
    }

    private static void SetRetryAfterHeader(HttpContext context, RateLimitLease lease)
    {
        if (lease.TryGetMetadata(MetadataName.RetryAfter, out TimeSpan retryAfter))
        {
            context.Response.Headers.RetryAfter = Math.Ceiling(retryAfter.TotalSeconds)
                .ToString(CultureInfo.InvariantCulture);
        }
    }
}
