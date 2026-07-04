// <copyright file="UpdateTenantLifecycleStatusCommand.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.EventStore.Domain.Commands;

using Hexalith.Memories.Contracts.V1;
using Hexalith.Memories.EventStore.Domain;

/// <summary>Records a tenant lifecycle status change before read-model projection.</summary>
public sealed record UpdateTenantLifecycleStatusCommand(
    string TenantId,
    TenantStatus Status,
    DateTimeOffset UpdatedAt) : IMemoriesCommandContract
{
    /// <inheritdoc/>
    public static string Domain => MemoriesDomain.Tenants;

    /// <inheritdoc/>
    public static string CommandType => "update-tenant-lifecycle-status";

    /// <inheritdoc/>
    public string AggregateId => TenantId;
}
