// <copyright file="CaseAggregateState.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.EventStore.Domain.States;

using Hexalith.Memories.EventStore.Domain.Events;

/// <summary>Folded case aggregate state.</summary>
public sealed class CaseAggregateState
{
    /// <summary>Gets the tenant identifier.</summary>
    public string TenantId { get; private set; } = string.Empty;

    /// <summary>Gets the case identifier.</summary>
    public string CaseId { get; private set; } = string.Empty;

    /// <summary>Gets the case name.</summary>
    public string Name { get; private set; } = string.Empty;

    /// <summary>Gets a value indicating whether deletion was requested.</summary>
    public bool DeletionRequested { get; private set; }

    /// <summary>Applies case creation.</summary>
    public void Apply(CaseCreatedEvent @event)
    {
        ArgumentNullException.ThrowIfNull(@event);
        TenantId = @event.TenantId;
        CaseId = @event.CaseId;
        Name = @event.Name;
    }

    /// <summary>Applies case deletion intent.</summary>
    public void Apply(CaseDeletionRequestedEvent @event)
    {
        ArgumentNullException.ThrowIfNull(@event);
        DeletionRequested = true;
    }
}
