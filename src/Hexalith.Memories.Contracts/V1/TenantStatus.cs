// <copyright file="TenantStatus.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Contracts.V1;

using System.Text.Json.Serialization;

/// <summary>Represents the lifecycle status of a tenant.</summary>
[JsonConverter(typeof(CamelCaseStringEnumConverter<TenantStatus>))]
public enum TenantStatus
{
    /// <summary>Tenant is being provisioned.</summary>
    Provisioning,

    /// <summary>Tenant is fully provisioned and active.</summary>
    Active,

    /// <summary>Tenant is being deleted.</summary>
    Deleting,

    /// <summary>Provisioning or deletion failed; tenant can be retried.</summary>
    Failed,

    /// <summary>Provisioning failed AND cleanup of orphaned resources also failed; operator must manually clean up.</summary>
    CompensationFailed,
}
