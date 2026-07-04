// <copyright file="MemoryUnitAggregate.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.EventStore.Domain.Aggregates;

using Hexalith.Memories.EventStore.Domain.Results;
using Hexalith.Memories.EventStore.Domain.Commands;
using Hexalith.Memories.EventStore.Domain.Events;
using Hexalith.Memories.EventStore.Domain.States;

/// <summary>Pure command handler for memory-unit mutations.</summary>
public static class MemoryUnitAggregate
{
    /// <summary>Handles annotation intent.</summary>
    public static MemoriesDomainResult Handle(RequestAnnotationCommand command, MemoryUnitAggregateState? state)
    {
        ArgumentNullException.ThrowIfNull(command);
        ValidateRequired(command.TenantId, command.CaseId, command.AnnotationMemoryUnitId);
        ArgumentException.ThrowIfNullOrWhiteSpace(command.TargetMemoryUnitId);
        ArgumentException.ThrowIfNullOrWhiteSpace(command.Content);
        return state is not null
            ? MemoriesDomainResult.NoOp()
            : MemoriesDomainResult.Success([
                new AnnotationRequestedEvent(
                    command.TenantId,
                    command.CaseId,
                    command.AnnotationMemoryUnitId,
                    command.TargetMemoryUnitId,
                    command.SourceUri,
                    command.Content,
                    command.AnnotationType,
                    command.IngestedBy,
                    command.RequestedAt),
            ]);
    }

    /// <summary>Handles memory-unit deletion intent.</summary>
    public static MemoriesDomainResult Handle(DeleteMemoryUnitCommand command, MemoryUnitAggregateState? state)
    {
        ArgumentNullException.ThrowIfNull(command);
        ValidateRequired(command.TenantId, command.CaseId, command.MemoryUnitId);
        return state is { DeletionRequested: true }
            ? MemoriesDomainResult.NoOp()
            : MemoriesDomainResult.Success([
                new MemoryUnitDeletionRequestedEvent(
                    command.TenantId,
                    command.CaseId,
                    command.MemoryUnitId,
                    command.AnnotationMemoryUnitIds,
                    command.DeletedAt),
            ]);
    }

    private static void ValidateRequired(string tenantId, string caseId, string memoryUnitId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        ArgumentException.ThrowIfNullOrWhiteSpace(caseId);
        ArgumentException.ThrowIfNullOrWhiteSpace(memoryUnitId);
    }
}
