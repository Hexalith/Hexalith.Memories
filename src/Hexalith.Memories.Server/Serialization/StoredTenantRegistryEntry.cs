// <copyright file="StoredTenantRegistryEntry.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Serialization;

using Hexalith.Memories.Server.Tenants;

/// <summary>Durable tenant registry entry including workflow ownership.</summary>
internal sealed record StoredTenantRegistryEntry(
    StoredTenantInfo Tenant,
    string? WorkflowInstanceId,
    DateTimeOffset LastUpdated = default)
{
    public static implicit operator StoredTenantRegistryEntry(TenantRegistryEntry value)
        => PersistenceModelMapper.ToStored(value);

    public static implicit operator TenantRegistryEntry(StoredTenantRegistryEntry value)
        => PersistenceModelMapper.ToContract(value);
}
