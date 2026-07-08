// <copyright file="TenantAuthorizationMiddleware.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Authentication;

using Hexalith.Memories.Contracts.V1;

using Microsoft.AspNetCore.Http;

/// <summary>Authorizes tenant-scoped path and query API routes before endpoint business logic executes.</summary>
public sealed class TenantAuthorizationMiddleware(
    RequestDelegate next,
    ILogger<TenantAuthorizationMiddleware> logger)
{
    /// <summary>Invokes the middleware.</summary>
    /// <param name="context">The HTTP context.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task InvokeAsync(HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        string? tenantId = GetTenantId(context);
        if (!string.IsNullOrWhiteSpace(tenantId)
            && !TenantAuthorizationEndpointFilter.TryAuthorizeTenant(
                context,
                tenantId,
                context.Request.Path.Value ?? "unknown",
                logger,
                out IResult? result))
        {
            await result!.ExecuteAsync(context).ConfigureAwait(false);
            return;
        }

        await next(context).ConfigureAwait(false);
    }

    private static string? GetTenantId(HttpContext context)
    {
        PathString path = context.Request.Path;
        if (path.StartsWithSegments(MemoriesRoutes.Tenants, out PathString remaining))
        {
            string value = remaining.Value ?? string.Empty;
            string[] segments = value.Split('/', StringSplitOptions.RemoveEmptyEntries);
            return segments.Length > 0 ? segments[0] : null;
        }

        if (path.Equals(MemoriesRoutes.Search, StringComparison.OrdinalIgnoreCase)
            && context.Request.Query.TryGetValue("tenantId", out Microsoft.Extensions.Primitives.StringValues tenantValues))
        {
            return tenantValues.ToString();
        }

        return null;
    }
}
