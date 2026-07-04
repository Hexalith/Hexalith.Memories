// <copyright file="RegisterTenantCommand.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.EventStore.Domain.Commands;

using Hexalith.Memories.EventStore.Domain;

/// <summary>Records the authoritative tenant registration event before read-model projection.</summary>
public sealed record RegisterTenantCommand(
    string TenantId,
    string DisplayName,
    DateTimeOffset RegisteredAt) : IMemoriesCommandContract
{
    /// <inheritdoc/>
    public static string Domain => MemoriesDomain.Tenants;

    /// <inheritdoc/>
    public static string CommandType => "register-tenant";

    /// <inheritdoc/>
    public string AggregateId => TenantId;
}
