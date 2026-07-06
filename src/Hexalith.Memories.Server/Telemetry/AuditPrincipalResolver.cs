// <copyright file="AuditPrincipalResolver.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Telemetry;

using Hexalith.Memories.Telemetry;

/// <summary>Resolves the principal value used by endpoint audit telemetry.</summary>
internal static class AuditPrincipalResolver
{
    /// <summary>Resolves a stable support-safe user identifier for access telemetry.</summary>
    /// <param name="httpContext">The current HTTP context.</param>
    /// <param name="activity">The current tracing activity, if available.</param>
    /// <returns>The audit user identifier.</returns>
    public static string Resolve(HttpContext httpContext, System.Diagnostics.Activity? activity)
    {
        ArgumentNullException.ThrowIfNull(httpContext);

        if (httpContext.User.Identity?.IsAuthenticated != true)
        {
            return AccessTelemetryLog.UserAnonymous;
        }

        string? user = httpContext.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
            ?? httpContext.User.FindFirst("sub")?.Value
            ?? httpContext.User.FindFirst("preferred_username")?.Value
            ?? httpContext.User.FindFirst("name")?.Value;
        if (string.IsNullOrWhiteSpace(user))
        {
            return AccessTelemetryLog.UserAnonymous;
        }

        if (string.Equals(user, AccessTelemetryLog.UserQuickstartWizard, StringComparison.Ordinal))
        {
            activity?.SetTag(MemoriesActivitySource.TagWizardOrigin, true);
        }

        return user;
    }
}
