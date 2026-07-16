// <copyright file="NoOpEventIngestionTelemetry.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.EventStore;

/// <summary>Default telemetry adapter used when the host does not override event-ingestion telemetry.
/// Downstream hosts can replace this with a richer adapter, but the package remains operable with no-op
/// telemetry out of the box.</summary>
internal sealed class NoOpEventIngestionTelemetry : IEventIngestionTelemetry
{
    public void RecordIngestion(
        string tenantId,
        string? caseId,
        string? cloudEventId,
        string? aggregateType,
        string? cloudEventType,
        EventIngestionOutcome outcome,
        long durationMs)
    {
    }
}
