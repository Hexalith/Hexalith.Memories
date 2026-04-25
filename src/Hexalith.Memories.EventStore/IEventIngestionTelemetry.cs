// <copyright file="IEventIngestionTelemetry.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.EventStore;

/// <summary>Adapter used by the EventStore package to emit access-telemetry events for event-sourced ingestion.
/// Implemented in the Server project as a thin wrapper over the existing <c>AccessTelemetryLog</c> emitters
/// so this package does not reference Server telemetry types (ADR 9.1-D).</summary>
public interface IEventIngestionTelemetry
{
    /// <summary>Emits a single access-telemetry record for one event-ingestion request.</summary>
    /// <param name="tenantId">Tenant id, or <c>__rejected__</c> if the request was rejected before tenant resolution.</param>
    /// <param name="caseId">Case id, or <c>null</c> when no case was resolved.</param>
    /// <param name="cloudEventId">The CloudEvents <c>id</c> (or <c>null</c> when the envelope was malformed).</param>
    /// <param name="aggregateType">The derived aggregate type, or <c>null</c> when not resolved.</param>
    /// <param name="cloudEventType">Story 9.3 — the CloudEvents <c>type</c> header (e.g.,
    /// <c>MyApp.Claims.ClaimSubmittedV2</c>), or <c>null</c> on branches where no envelope was parsed.
    /// Threaded from <c>EventIngestionService</c> so the Server-side adapter can fan out to the
    /// observation store without parsing the envelope twice.</param>
    /// <param name="outcome">The <see cref="EventIngestionOutcome"/> produced.</param>
    /// <param name="durationMs">Elapsed time in milliseconds.</param>
    void RecordIngestion(
        string tenantId,
        string? caseId,
        string? cloudEventId,
        string? aggregateType,
        string? cloudEventType,
        EventIngestionOutcome outcome,
        long durationMs);
}
