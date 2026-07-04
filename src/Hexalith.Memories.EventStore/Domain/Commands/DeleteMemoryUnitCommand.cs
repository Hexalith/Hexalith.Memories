// <copyright file="DeleteMemoryUnitCommand.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.EventStore.Domain.Commands;

using Hexalith.Memories.EventStore.Domain;

/// <summary>Deletes a memory unit aggregate and starts projection cleanup.</summary>
public sealed record DeleteMemoryUnitCommand(
    string TenantId,
    string CaseId,
    string MemoryUnitId,
    IReadOnlyList<string> AnnotationMemoryUnitIds,
    DateTimeOffset DeletedAt) : IMemoriesCommandContract
{
    /// <inheritdoc/>
    public static string Domain => MemoriesDomain.MemoryUnits;

    /// <inheritdoc/>
    public static string CommandType => "delete-memory-unit";

    /// <inheritdoc/>
    public string AggregateId => MemoryUnitId;
}
