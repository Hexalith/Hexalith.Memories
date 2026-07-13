// <copyright file="TenantAuthorizationEndpointFilter.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Authentication;

using System.Security.Claims;
using System.Text.RegularExpressions;

using Hexalith.Memories.Contracts.V1;
using Hexalith.Memories.Server.Endpoints;

using Microsoft.AspNetCore.Http;

/// <summary>Authorizes Memories Server endpoint tenant arguments against normalized bearer tenant claims.</summary>
public sealed partial class TenantAuthorizationEndpointFilter(
    ILogger<TenantAuthorizationEndpointFilter> logger) : IEndpointFilter
{
    /// <inheritdoc />
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(next);

        string? tenantId = ResolveRequestedTenant(context);
        if (!string.IsNullOrWhiteSpace(tenantId)
            && !TryAuthorizeTenant(context.HttpContext, tenantId, "endpoint-filter", logger, out IResult? result))
        {
            return result;
        }

        return await next(context).ConfigureAwait(false);
    }

    /// <summary>Authorizes the current HTTP context for a requested tenant.</summary>
    /// <param name="context">The HTTP context.</param>
    /// <param name="tenantId">The requested tenant id.</param>
    /// <param name="surface">The route or endpoint surface being authorized.</param>
    /// <param name="logger">The logger.</param>
    /// <param name="result">The denial result when authorization fails.</param>
    /// <returns><c>true</c> when the request is authorized.</returns>
    public static bool TryAuthorizeTenant(
        HttpContext context,
        string? tenantId,
        string surface,
        ILogger logger,
        out IResult? result)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentException.ThrowIfNullOrWhiteSpace(surface);
        ArgumentNullException.ThrowIfNull(logger);

        result = null;
        if (!IsWellFormedTenantId(tenantId))
        {
            result = Deny(context, surface, logger, "MalformedOrMissingTenant");
            return false;
        }

        ClaimsPrincipal user = context.User;
        if (user.Identity?.IsAuthenticated != true)
        {
            result = Deny(context, surface, logger, "UnauthenticatedPrincipal");
            return false;
        }

        string[] tenantClaims = [.. user.FindAll(ServerTenantClaimsTransformation.TenantClaimType).Select(c => c.Value)];
        if (!tenantClaims.Any(value => string.Equals(value, tenantId, StringComparison.Ordinal)))
        {
            result = Deny(context, surface, logger, "TenantClaimMissingOrMismatch");
            return false;
        }

        if (context.Items.TryGetValue(AuthorizedTenantAccessor.HttpContextItemKey, out object? existingTenant))
        {
            if (existingTenant is string capturedTenant
                && string.Equals(capturedTenant, tenantId, StringComparison.Ordinal))
            {
                return true;
            }

            throw new InvalidOperationException("HttpContext.Items contains conflicting tenant authorization state.");
        }

        context.Items[AuthorizedTenantAccessor.HttpContextItemKey] = tenantId;
        return true;
    }

    private static string? ResolveRequestedTenant(EndpointFilterInvocationContext context)
    {
        if (context.HttpContext.Request.RouteValues.TryGetValue("tenantId", out object? routeValue)
            && routeValue is not null)
        {
            return routeValue.ToString();
        }

        if (context.HttpContext.Request.Query.TryGetValue("tenantId", out Microsoft.Extensions.Primitives.StringValues queryValue))
        {
            return queryValue.ToString();
        }

        foreach (object? argument in context.Arguments)
        {
            switch (argument)
            {
                case IngestionInput input:
                    return input.TenantId;
                case UrlIngestionRequest request:
                    return request.TenantId;
                case DirectoryIngestionRequest request:
                    return request.TenantId;
            }
        }

        return null;
    }

    private static IResult Deny(HttpContext context, string surface, ILogger logger, string reason)
    {
        string sourceIp = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        string path = context.Request.Path.HasValue ? context.Request.Path.Value! : string.Empty;
        string claimNames = FormatClaimNamesForLog(context.User);

        logger.LogWarning(
            "Memories Server tenant authorization failed: SecurityEvent={SecurityEvent}, SourceIp={SourceIp}, Path={RequestPath}, Surface={Surface}, Reason={Reason}, ClaimNames={ClaimNames}",
            "TenantAuthorizationDenied",
            sourceIp,
            path,
            surface,
            reason,
            claimNames);

        return ErrorResults.TenantForbiddenResult();
    }

    private static string FormatClaimNamesForLog(ClaimsPrincipal principal)
    {
        string[] allowed =
        [
            ServerTenantClaimsTransformation.TenantClaimType,
            ClaimTypes.NameIdentifier,
            "sub",
            "preferred_username",
            "name",
            "tenant_id",
            "tenants",
            "tid",
            "tenant",
        ];
        HashSet<string> allowedSet = new(allowed, StringComparer.Ordinal);
        return string.Join(
            ";",
            principal.Claims
                .Select(c => c.Type)
                .Where(allowedSet.Contains)
                .Distinct(StringComparer.Ordinal)
                .Order(StringComparer.Ordinal));
    }

    /// <summary>Returns a value indicating whether a tenant id is syntactically valid for authorization.</summary>
    /// <param name="tenantId">The tenant id.</param>
    /// <returns><c>true</c> when the tenant id is well formed.</returns>
    public static bool IsWellFormedTenantId(string? tenantId)
        => !string.IsNullOrWhiteSpace(tenantId) && TenantIdRegex().IsMatch(tenantId);

    [GeneratedRegex("^[A-Za-z0-9_-]{1,128}$", RegexOptions.CultureInvariant)]
    private static partial Regex TenantIdRegex();
}
