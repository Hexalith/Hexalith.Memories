// <copyright file="MemoryUnitAggregateState.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.EventStore.Domain.States;

using Hexalith.Memories.EventStore.Domain.Events;

/// <summary>Folded memory-unit aggregate state for A3-owned mutations.</summary>
public sealed class MemoryUnitAggregateState
{
    /// <summary>Gets the tenant identifier.</summary>
    public string TenantId { get; private set; } = string.Empty;

    /// <summary>Gets the case identifier.</summary>
    public string CaseId { get; private set; } = string.Empty;

    /// <summary>Gets the memory-unit identifier.</summary>
    public string MemoryUnitId { get; private set; } = string.Empty;

    /// <summary>Gets a value indicating whether deletion was requested.</summary>
    public bool DeletionRequested { get; private set; }

    /// <summary>Applies annotation intent.</summary>
    public void Apply(AnnotationRequestedEvent @event)
    {
        ArgumentNullException.ThrowIfNull(@event);
        TenantId = @event.TenantId;
        CaseId = @event.CaseId;
        MemoryUnitId = @event.AnnotationMemoryUnitId;
    }

    /// <summary>Applies memory-unit deletion intent.</summary>
    public void Apply(MemoryUnitDeletionRequestedEvent @event)
    {
        ArgumentNullException.ThrowIfNull(@event);
        TenantId = @event.TenantId;
        CaseId = @event.CaseId;
        MemoryUnitId = @event.MemoryUnitId;
        DeletionRequested = true;
    }
}
