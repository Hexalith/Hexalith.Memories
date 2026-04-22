// <copyright file="TenantStatusAccessorAdapter.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.EventStoreIntegration;

using Hexalith.Memories.Contracts.V1;
using Hexalith.Memories.EventStore;
using Hexalith.Memories.Server.Tenants;

/// <summary>Server-side adapter implementing <see cref="ITenantStatusAccessor"/> over the existing
/// <see cref="TenantRegistryService"/>. Maps the richer <see cref="TenantStatus"/> enum into the
/// narrower <see cref="EventStoreTenantStatus"/> view the EventStore router needs (ADR 9.1-D).</summary>
internal sealed class TenantStatusAccessorAdapter : ITenantStatusAccessor
{
    private readonly TenantRegistryService _registry;

    public TenantStatusAccessorAdapter(TenantRegistryService registry)
    {
        ArgumentNullException.ThrowIfNull(registry);
        _registry = registry;
    }

    public async Task<EventStoreTenantStatus?> GetStatusAsync(string tenantId, CancellationToken cancellationToken)
    {
        TenantInfo? tenant = await _registry.GetTenantAsync(tenantId, cancellationToken).ConfigureAwait(false);
        if (tenant is null)
        {
            return null;
        }

        return tenant.Status switch
        {
            TenantStatus.Provisioning => EventStoreTenantStatus.Provisioning,
            TenantStatus.Active => EventStoreTenantStatus.Active,
            TenantStatus.Deleting => EventStoreTenantStatus.Deleting,
            TenantStatus.Failed or TenantStatus.CompensationFailed => EventStoreTenantStatus.Unavailable,
            _ => EventStoreTenantStatus.Unavailable,
        };
    }
}
