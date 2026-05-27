// <copyright file="ITenantStatusAccessor.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.EventStore;

/// <summary>Adapter over the Server-side tenant registry. Abstracts <c>TenantRegistryService</c> and
/// <c>TenantStatusGuard</c> so this package does not take a compile-time reference on Server tenancy types
/// (ADR 9.1-D).</summary>
public interface ITenantStatusAccessor
{
    /// <summary>Looks up the current status of a tenant.</summary>
    /// <param name="tenantId">The tenant identifier.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>
    /// The current status, or <c>null</c> when the tenant does not exist in the registry.
    /// </returns>
    Task<EventStoreTenantStatus?> GetStatusAsync(string tenantId, CancellationToken cancellationToken);
}

/// <summary>Minimal tenant lifecycle-status view the EventStore package needs. Mirrors the subset of
/// <c>Hexalith.Memories.Contracts.V1.TenantStatus</c> that drives routing decisions.</summary>
public enum EventStoreTenantStatus
{
    /// <summary>Tenant is being provisioned — routing returns a retryable outcome.</summary>
    Provisioning,

    /// <summary>Tenant is active — routing proceeds.</summary>
    Active,

    /// <summary>Tenant is being deleted — routing returns a drop outcome.</summary>
    Deleting,

    /// <summary>Tenant is in a non-operational terminal state (Failed / CompensationFailed) — routing returns a drop outcome.</summary>
    Unavailable,
}
