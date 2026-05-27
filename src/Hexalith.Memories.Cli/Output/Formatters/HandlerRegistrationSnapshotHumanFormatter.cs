// <copyright file="HandlerRegistrationSnapshotHumanFormatter.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Cli.Output.Formatters;

using System;
using System.Linq;

using Hexalith.Memories.Contracts.V1;

/// <summary>Human-readable rendering of <see cref="HandlerRegistrationSnapshot"/> for
/// <c>memories handlers list</c>.</summary>
public sealed class HandlerRegistrationSnapshotHumanFormatter : IOutputFormatter<HandlerRegistrationSnapshot>
{
    /// <inheritdoc />
    public OutputFormat Format => OutputFormat.Human;

    /// <inheritdoc />
    public void Write(HandlerRegistrationSnapshot value, TextWriter writer)
    {
        ArgumentNullException.ThrowIfNull(value);
        ArgumentNullException.ThrowIfNull(writer);

        if (value.Handlers.Count == 0)
        {
            writer.WriteLine("No handlers registered.");
            return;
        }

        foreach (HandlerRegistration handler in value.Handlers)
        {
            string lastEvent = string.IsNullOrWhiteSpace(handler.LastEventAt) ? "never" : handler.LastEventAt;
            string eventTypes = handler.ObservedEventTypes.Count == 0
                ? "(none observed in last 24h)"
                : string.Join(", ", handler.ObservedEventTypes.Select(t => t.EventType).Distinct(StringComparer.Ordinal));
            string error = string.IsNullOrWhiteSpace(handler.Error) ? string.Empty : $" error={handler.Error}";

            writer.WriteLine(
                $"{handler.TenantId} {handler.SourcePrefix} events={handler.EventsProcessedCount} last={lastEvent} types={eventTypes}{error}");
        }

        writer.WriteLine();
    }
}