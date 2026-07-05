// <copyright file="TenantStatusGuard.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Tenants;

using Hexalith.Memories.Contracts.V1;

using Microsoft.AspNetCore.Http;

/// <summary>Validates that a tenant is in Active status before allowing operations.
/// Returns structured <see cref="ErrorResponse"/> for non-active states.</summary>
public sealed class TenantStatusGuard(TenantRegistryService registry)
{
    /// <summary>Validates that the tenant exists and is Active.</summary>
    /// <param name="tenantId">The tenant identifier.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>An <see cref="ErrorResponse"/> if the tenant is not active; null if active.</returns>
    public async Task<ErrorResponse?> ValidateTenantActiveAsync(string tenantId, CancellationToken ct)
    {
        TenantInfo? tenant = await registry.GetTenantForStatusGuardAsync(tenantId, ct).ConfigureAwait(false);
        if (tenant is null)
        {
            return new ErrorResponse("TENANT_NOT_FOUND", $"Tenant '{tenantId}' not found.", "List available tenants with GET /api/tenants");
        }

        return tenant.Status switch
        {
            TenantStatus.Active => null,
            TenantStatus.Deleting => new ErrorResponse("TENANT_DELETING", $"Tenant '{tenantId}' is being deleted.", "Wait for deletion to complete."),
            TenantStatus.Provisioning => new ErrorResponse("TENANT_PROVISIONING", $"Tenant '{tenantId}' is still provisioning.", "Wait for provisioning to complete."),
            TenantStatus.Failed or TenantStatus.CompensationFailed => new ErrorResponse("TENANT_FAILED", $"Tenant '{tenantId}' is in state '{tenant.Status}'.", "Retry provisioning or deletion after investigating the failure."),
            _ => new ErrorResponse("TENANT_UNAVAILABLE", $"Tenant '{tenantId}' is in state '{tenant.Status}'.", "Check tenant status and retry."),
        };
    }

    /// <summary>Validates that the tenant exists in the registry, regardless of its current status.
    /// Use for endpoints that must remain callable for tenants in non-active states (e.g. provisioning status, deletion status, verification).</summary>
    /// <param name="tenantId">The tenant identifier.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A <see cref="ErrorResponse"/> with code <c>TENANT_NOT_FOUND</c> if the tenant does not exist; null otherwise.</returns>
    public async Task<ErrorResponse?> ValidateTenantExistsAsync(string tenantId, CancellationToken ct)
    {
        TenantInfo? tenant = await registry.GetTenantForStatusGuardAsync(tenantId, ct).ConfigureAwait(false);
        return tenant is null
            ? new ErrorResponse("TENANT_NOT_FOUND", $"Tenant '{tenantId}' not found.", "List available tenants with GET /api/tenants")
            : null;
    }

    /// <summary>Maps a <see cref="TenantStatusGuard"/> <see cref="ErrorResponse"/> to the appropriate HTTP result:
    /// <c>TENANT_NOT_FOUND</c> -> 404 Not Found, all other status codes (<c>TENANT_DELETING</c>, <c>TENANT_PROVISIONING</c>,
    /// <c>TENANT_FAILED</c>, <c>TENANT_UNAVAILABLE</c>) -> 409 Conflict. Use at every caller that returns a tenant-status error
    /// so the HTTP status stays semantically correct.</summary>
    /// <param name="error">The tenant status error produced by <see cref="ValidateTenantActiveAsync"/> or <see cref="ValidateTenantExistsAsync"/>.</param>
    /// <returns>An <see cref="IResult"/> with the appropriate HTTP status code.</returns>
    public static IResult ToHttpResult(ErrorResponse error)
        => error.Code == "TENANT_NOT_FOUND" ? Results.NotFound(error) : Results.Conflict(error);
}
