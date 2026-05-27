// <copyright file="EventStoreObservationOptions.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.EventStore;

/// <summary>Story 9.3 — operator-facing kill switch for the observation-store fire-and-forget write path.
/// Bound via <c>EventStoreIntegration:Observation</c> configuration section. Read through
/// <see cref="Microsoft.Extensions.Options.IOptionsMonitor{T}"/> so operators can flip the switch at
/// runtime without a process restart.</summary>
/// <remarks>Intended use: if observation writes begin degrading ingestion (e.g. Redis p99 latency
/// spike), operators can disable writes by setting <c>EventStoreIntegration:Observation:Enabled=false</c>
/// and the hot path picks up the change on the next event. While disabled, handler-registry
/// snapshot reads still return a valid response with <c>EventsProcessedCount = 0</c> for all
/// handlers — the audit log (<c>AccessTelemetryLog</c>) remains the source of truth for durable
/// ingestion tracking.</remarks>
public sealed class EventStoreObservationOptions
{
    /// <summary>Gets or sets a value indicating whether observation-store writes are enabled.
    /// Default: <c>true</c>.</summary>
    public bool Enabled { get; set; } = true;
}
