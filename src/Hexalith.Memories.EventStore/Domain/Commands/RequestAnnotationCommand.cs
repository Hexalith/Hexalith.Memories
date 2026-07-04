// <copyright file="RequestAnnotationCommand.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.EventStore.Domain.Commands;

using Hexalith.Memories.EventStore.Domain;

/// <summary>Records annotation intent before graph/activity projection fan-out.</summary>
public sealed record RequestAnnotationCommand(
    string TenantId,
    string CaseId,
    string AnnotationMemoryUnitId,
    string TargetMemoryUnitId,
    string SourceUri,
    string Content,
    string? AnnotationType,
    string IngestedBy,
    DateTimeOffset RequestedAt) : IMemoriesCommandContract
{
    /// <inheritdoc/>
    public static string Domain => MemoriesDomain.MemoryUnits;

    /// <inheritdoc/>
    public static string CommandType => "request-annotation";

    /// <inheritdoc/>
    public string AggregateId => AnnotationMemoryUnitId;
}
