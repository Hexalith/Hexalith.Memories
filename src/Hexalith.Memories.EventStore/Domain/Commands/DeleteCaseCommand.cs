// <copyright file="DeleteCaseCommand.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.EventStore.Domain.Commands;

using Hexalith.Memories.EventStore.Domain;

/// <summary>Deletes a case aggregate and starts projection cleanup.</summary>
public sealed record DeleteCaseCommand(
    string TenantId,
    string CaseId,
    IReadOnlyList<string> MemoryUnitIds,
    DateTimeOffset DeletedAt) : IMemoriesCommandContract
{
    /// <inheritdoc/>
    public static string Domain => MemoriesDomain.Cases;

    /// <inheritdoc/>
    public static string CommandType => "delete-case";

    /// <inheritdoc/>
    public string AggregateId => CaseId;
}
