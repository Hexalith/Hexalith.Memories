// <copyright file="MemoriesDomainResult.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.EventStore.Domain.Results;

using Hexalith.Memories.EventStore.Domain.Events;

/// <summary>Represents the events emitted by a pure Memories aggregate command handler.</summary>
public sealed record MemoriesDomainResult(IReadOnlyList<IMemoriesEventPayload> Events)
{
    /// <summary>Gets a value indicating whether the command emitted at least one event.</summary>
    public bool IsSuccess => Events.Count > 0;

    /// <summary>Gets a value indicating whether the command was idempotently ignored.</summary>
    public bool IsNoOp => Events.Count == 0;

    /// <summary>Creates a successful domain result.</summary>
    /// <param name="events">The emitted event payloads.</param>
    /// <returns>A successful result.</returns>
    public static MemoriesDomainResult Success(IReadOnlyList<IMemoriesEventPayload> events)
    {
        ArgumentNullException.ThrowIfNull(events);

        return events.Count == 0
            ? throw new ArgumentException("Success result requires at least one event.", nameof(events))
            : new MemoriesDomainResult(events);
    }

    /// <summary>Creates a no-op domain result.</summary>
    /// <returns>An empty result.</returns>
    public static MemoriesDomainResult NoOp() => new([]);
}
