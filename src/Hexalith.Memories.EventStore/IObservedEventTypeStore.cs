// <copyright file="IObservedEventTypeStore.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.EventStore;

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

/// <summary>Story 9.3 — per-tenant rolling-window store of observed CloudEvents-type occurrences.
/// Writes are fire-and-forget from the ingestion hot path; the store itself is <b>fail-open</b> on
/// write exceptions (callers should log a warning and continue). Reads are authoritative and MAY throw
/// on connection failure.</summary>
/// <remarks>Substrate separation: this store is DELIBERATELY separate from Story 7.5's 5m in-process
/// <c>RollingCounterStore</c>. The observation window is 24h, the backing is Redis, and the access
/// pattern is rare (operator-triggered). See ADR-9.3-002 for the full rationale. Future contributors
/// MUST NOT extend the 5m ring to cover handler observation.</remarks>
public interface IObservedEventTypeStore
{
    /// <summary>Records an observation of a single CloudEvents-type on an aggregate for a given tenant.
    /// Fail-open: Redis exceptions are logged and swallowed — the ingestion hot path never blocks on
    /// this store.</summary>
    /// <param name="tenantId">Tenant identifier.</param>
    /// <param name="aggregateType">CloudEvents-derived aggregate type (e.g., <c>Claims</c>).</param>
    /// <param name="eventType">CloudEvents <c>type</c> header (e.g., <c>MyApp.Claims.ClaimSubmittedV2</c>).</param>
    /// <param name="observedAt">Observation timestamp in UTC (score for the sorted set).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task RecordObservationAsync(
        string tenantId,
        string aggregateType,
        string eventType,
        DateTimeOffset observedAt,
        CancellationToken cancellationToken);

    /// <summary>Returns observations for a specific aggregate type within the given window.</summary>
    /// <param name="tenantId">Tenant identifier.</param>
    /// <param name="aggregateType">CloudEvents-derived aggregate type.</param>
    /// <param name="window">Window width relative to "now" — observations with <c>lastSeenAt &gt;= now - window</c>.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Ordered list (most recent first) of observed event types + counts.</returns>
    Task<IReadOnlyList<ObservedEventType>> GetObservedTypesAsync(
        string tenantId,
        string aggregateType,
        TimeSpan window,
        CancellationToken cancellationToken);

    /// <summary>Returns observations across every aggregate type this tenant has seen.</summary>
    /// <param name="tenantId">Tenant identifier.</param>
    /// <param name="window">Window width relative to "now".</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Flattened list of observations across all aggregate types.</returns>
    Task<IReadOnlyList<ObservedEventType>> GetAllObservedTypesAsync(
        string tenantId,
        TimeSpan window,
        CancellationToken cancellationToken);
}

/// <summary>Story 9.3 — single observation tuple returned by <see cref="IObservedEventTypeStore"/>.</summary>
/// <param name="AggregateType">CloudEvents-derived aggregate type.</param>
/// <param name="EventType">CloudEvents <c>type</c> header value.</param>
/// <param name="Count">Total observation count in-window.</param>
/// <param name="LastSeenAt">Most-recent observation timestamp.</param>
public sealed record ObservedEventType(
    string AggregateType,
    string EventType,
    long Count,
    DateTimeOffset LastSeenAt);
