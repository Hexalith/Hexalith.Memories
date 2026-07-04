// <copyright file="CreateCaseCommand.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.EventStore.Domain.Commands;

using Hexalith.Memories.EventStore.Domain;

/// <summary>Creates a case aggregate before projection fan-out.</summary>
public sealed record CreateCaseCommand(
    string TenantId,
    string CaseId,
    string Name,
    string? Description,
    DateTimeOffset CreatedAt) : IMemoriesCommandContract
{
    /// <inheritdoc/>
    public static string Domain => MemoriesDomain.Cases;

    /// <inheritdoc/>
    public static string CommandType => "create-case";

    /// <inheritdoc/>
    public string AggregateId => CaseId;
}
