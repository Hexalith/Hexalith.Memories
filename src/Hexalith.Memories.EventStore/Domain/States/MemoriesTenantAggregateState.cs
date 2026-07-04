// <copyright file="MemoriesTenantAggregateState.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.EventStore.Domain.States;

using Hexalith.Memories.Contracts.V1;
using Hexalith.Memories.EventStore.Domain.Events;

/// <summary>Folded Memories tenant lifecycle aggregate state.</summary>
public sealed class MemoriesTenantAggregateState
{
    /// <summary>Gets the tenant identifier.</summary>
    public string TenantId { get; private set; } = string.Empty;

    /// <summary>Gets the tenant display name.</summary>
    public string DisplayName { get; private set; } = string.Empty;

    /// <summary>Gets the current tenant status.</summary>
    public TenantStatus Status { get; private set; }

    /// <summary>Applies tenant registration.</summary>
    public void Apply(TenantRegisteredEvent @event)
    {
        ArgumentNullException.ThrowIfNull(@event);
        TenantId = @event.TenantId;
        DisplayName = @event.DisplayName;
        Status = TenantStatus.Provisioning;
    }

    /// <summary>Applies tenant lifecycle status update.</summary>
    public void Apply(TenantLifecycleStatusUpdatedEvent @event)
    {
        ArgumentNullException.ThrowIfNull(@event);
        Status = @event.Status;
    }
}
