// <copyright file="TenantEventRouteResolutionStatus.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.EventStore;

/// <summary>Resolution outcome for a CloudEvent routing attempt.</summary>
public enum TenantEventRouteResolutionStatus
{
    /// <summary>Tenant + case resolved successfully; ingestion may proceed.</summary>
    Accepted,

    /// <summary>CloudEvents <c>source</c> matched no entry in <see cref="TenantEventRoutingOptions.SourceToTenantMap"/>.</summary>
    UnknownSource,

    /// <summary>The resolved tenant id does not exist in the registry. Treated as a drop (200) because DAPR would
    /// otherwise retry indefinitely for a tenant that may never be created.</summary>
    TenantNotFound,

    /// <summary>Tenant is in <c>Provisioning</c> state. Signals a retryable (500) outcome — publisher should
    /// back off until the tenant becomes <c>Active</c>.</summary>
    TenantProvisioning,

    /// <summary>Tenant is in <c>Deleting</c> state. Signals a drop (200) — DAPR should not retry.</summary>
    TenantDeleting,

    /// <summary>No case exists for the <c>(tenantId, aggregateType)</c> pair and auto-create is disabled.</summary>
    AutoCreateDisabled,

    /// <summary>Auto-create is enabled but the per-tenant case cap has been reached.</summary>
    CaseCapExceeded,
}
