// <copyright file="MemoriesTenantAggregate.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.EventStore.Domain.Aggregates;

using Hexalith.Memories.EventStore.Domain.Results;
using Hexalith.Memories.EventStore.Domain.Commands;
using Hexalith.Memories.EventStore.Domain.Events;
using Hexalith.Memories.EventStore.Domain.States;

/// <summary>Pure command handler for Memories tenant lifecycle semantics.</summary>
public static class MemoriesTenantAggregate
{
    /// <summary>Handles tenant registration.</summary>
    public static MemoriesDomainResult Handle(RegisterTenantCommand command, MemoriesTenantAggregateState? state)
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentException.ThrowIfNullOrWhiteSpace(command.TenantId);
        ArgumentException.ThrowIfNullOrWhiteSpace(command.DisplayName);
        return state is not null
            ? MemoriesDomainResult.NoOp()
            : MemoriesDomainResult.Success([
                new TenantRegisteredEvent(command.TenantId, command.DisplayName, command.RegisteredAt),
            ]);
    }

    /// <summary>Handles tenant status changes.</summary>
    public static MemoriesDomainResult Handle(UpdateTenantLifecycleStatusCommand command, MemoriesTenantAggregateState? state)
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentException.ThrowIfNullOrWhiteSpace(command.TenantId);
        return state is not null && state.Status == command.Status
            ? MemoriesDomainResult.NoOp()
            : MemoriesDomainResult.Success([
                new TenantLifecycleStatusUpdatedEvent(command.TenantId, command.Status, command.UpdatedAt),
            ]);
    }
}
