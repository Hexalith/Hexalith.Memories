// <copyright file="TenantEventRouteResolution.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.EventStore;

/// <summary>Typed outcome of a tenant + case resolution attempt. Replaces nullable
/// <c>TenantEventRoute?</c> results so every branch can be handled explicitly by the controller
/// (AC #7, #10, #11, #14a, #14b).</summary>
/// <param name="Status">The resolution outcome.</param>
/// <param name="Route">The resolved route when <see cref="Status"/> is <see cref="TenantEventRouteResolutionStatus.Accepted"/>; otherwise <c>null</c>.</param>
/// <param name="TenantId">The tenant id that was attempted (may be resolved from the source map even when the status is non-Accepted); <c>null</c> for <see cref="TenantEventRouteResolutionStatus.UnknownSource"/>.</param>
public sealed record TenantEventRouteResolution(
    TenantEventRouteResolutionStatus Status,
    TenantEventRoute? Route,
    string? TenantId)
{
    public static TenantEventRouteResolution Accepted(TenantEventRoute route)
        => new(TenantEventRouteResolutionStatus.Accepted, route, route.TenantId);

    public static TenantEventRouteResolution UnknownSource()
        => new(TenantEventRouteResolutionStatus.UnknownSource, Route: null, TenantId: null);

    public static TenantEventRouteResolution TenantNotFound(string tenantId)
        => new(TenantEventRouteResolutionStatus.TenantNotFound, Route: null, tenantId);

    public static TenantEventRouteResolution TenantProvisioning(string tenantId)
        => new(TenantEventRouteResolutionStatus.TenantProvisioning, Route: null, tenantId);

    public static TenantEventRouteResolution TenantDeleting(string tenantId)
        => new(TenantEventRouteResolutionStatus.TenantDeleting, Route: null, tenantId);

    public static TenantEventRouteResolution AutoCreateDisabled(string tenantId)
        => new(TenantEventRouteResolutionStatus.AutoCreateDisabled, Route: null, tenantId);

    public static TenantEventRouteResolution CaseCapExceeded(string tenantId)
        => new(TenantEventRouteResolutionStatus.CaseCapExceeded, Route: null, tenantId);
}
