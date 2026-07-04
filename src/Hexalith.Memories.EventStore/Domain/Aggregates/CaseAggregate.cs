// <copyright file="CaseAggregate.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.EventStore.Domain.Aggregates;

using Hexalith.Memories.EventStore.Domain.Results;
using Hexalith.Memories.EventStore.Domain.Commands;
using Hexalith.Memories.EventStore.Domain.Events;
using Hexalith.Memories.EventStore.Domain.States;

/// <summary>Pure command handler for case mutations.</summary>
public static class CaseAggregate
{
    /// <summary>Handles case creation.</summary>
    public static MemoriesDomainResult Handle(CreateCaseCommand command, CaseAggregateState? state)
    {
        ArgumentNullException.ThrowIfNull(command);
        ValidateTenantCase(command.TenantId, command.CaseId);
        ArgumentException.ThrowIfNullOrWhiteSpace(command.Name);
        return state is not null
            ? MemoriesDomainResult.NoOp()
            : MemoriesDomainResult.Success([
                new CaseCreatedEvent(command.TenantId, command.CaseId, command.Name, command.Description, command.CreatedAt),
            ]);
    }

    /// <summary>Handles case deletion intent.</summary>
    public static MemoriesDomainResult Handle(DeleteCaseCommand command, CaseAggregateState? state)
    {
        ArgumentNullException.ThrowIfNull(command);
        ValidateTenantCase(command.TenantId, command.CaseId);
        return state is { DeletionRequested: true }
            ? MemoriesDomainResult.NoOp()
            : MemoriesDomainResult.Success([
                new CaseDeletionRequestedEvent(command.TenantId, command.CaseId, command.MemoryUnitIds, command.DeletedAt),
            ]);
    }

    private static void ValidateTenantCase(string tenantId, string caseId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        ArgumentException.ThrowIfNullOrWhiteSpace(caseId);
    }
}
