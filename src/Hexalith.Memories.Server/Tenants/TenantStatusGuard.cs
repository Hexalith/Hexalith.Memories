// <copyright file="TenantStatusGuard.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Tenants;

using Hexalith.Memories.Contracts.V1;

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
        TenantInfo? tenant = await registry.GetTenantAsync(tenantId, ct).ConfigureAwait(false);
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
}
