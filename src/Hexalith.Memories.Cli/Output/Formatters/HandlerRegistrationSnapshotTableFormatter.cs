// <copyright file="HandlerRegistrationSnapshotTableFormatter.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Cli.Output.Formatters;

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

using Hexalith.Memories.Contracts.V1;

/// <summary>Tabular rendering of <see cref="HandlerRegistrationSnapshot"/>.</summary>
public sealed class HandlerRegistrationSnapshotTableFormatter : IOutputFormatter<HandlerRegistrationSnapshot>
{
    /// <inheritdoc />
    public OutputFormat Format => OutputFormat.Table;

    /// <inheritdoc />
    public void Write(HandlerRegistrationSnapshot value, TextWriter writer)
    {
        ArgumentNullException.ThrowIfNull(value);
        ArgumentNullException.ThrowIfNull(writer);

        IEnumerable<IReadOnlyList<string>> rows = value.Handlers.Select(handler =>
        {
            string eventTypes = handler.ObservedEventTypes.Count == 0
                ? "(none observed in last 24h)"
                : string.Join(", ", handler.ObservedEventTypes.Select(t => t.EventType).Distinct(StringComparer.Ordinal));

            return (IReadOnlyList<string>)
            [
                handler.TenantId,
                handler.SourcePrefix,
                handler.EventsProcessedCount.ToString(CultureInfo.InvariantCulture),
                string.IsNullOrWhiteSpace(handler.LastEventAt) ? "never" : handler.LastEventAt,
                eventTypes,
                handler.Error ?? string.Empty,
            ];
        });

        TableWriter.Write(
            writer,
            ["TENANT", "SOURCE", "EVENTS (24H)", "LAST EVENT", "EVENT TYPES", "ERROR"],
            rows);
    }
}