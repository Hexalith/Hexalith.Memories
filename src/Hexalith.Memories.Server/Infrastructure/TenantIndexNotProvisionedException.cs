// <copyright file="TenantIndexNotProvisionedException.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Infrastructure;

/// <summary>Story 23.7 (A34) AC6: raised when a required tenant index is missing at readiness time. Ingestion
/// runs only for active, provisioned tenants; a missing index is an operational provisioning inconsistency and is
/// surfaced clearly instead of being created on demand from the ingestion hot path.</summary>
public sealed class TenantIndexNotProvisionedException : TenantIndexReadinessException
{
    /// <summary>Initializes a new instance of the <see cref="TenantIndexNotProvisionedException"/> class.</summary>
    /// <param name="tenantId">The tenant whose index is missing.</param>
    /// <param name="family">The missing index family.</param>
    /// <param name="indexName">The fully qualified index name that was not found.</param>
    public TenantIndexNotProvisionedException(string tenantId, TenantIndexFamily family, string indexName)
        : base(
            tenantId,
            family,
            $"Required {family} index '{indexName}' is missing for tenant '{tenantId}'. Tenant indexes are created by "
            + "TenantProvisioningWorkflow before a tenant becomes active; ingestion does not create indexes on demand.")
        => IndexName = indexName;

    /// <summary>Gets the fully qualified index name that was not found.</summary>
    public string IndexName { get; }
}
