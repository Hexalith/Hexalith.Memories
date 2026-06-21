// <copyright file="ISearchIndexMaintenance.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.EventStore;

using Hexalith.Memories.Contracts.V1;

/// <summary>Server-owned adapter that maintains a curated search index from
/// <see cref="SearchIndexEntryChanged"/> / <see cref="SearchIndexEntryRemoved"/> integration events.
///
/// <para>Curated entries are upserted by the composite key (<paramref name="indexTenantId"/>,
/// <see cref="SearchIndexEntryChanged.AggregateId"/>) and bypass the generic raw-event ingestion
/// workflow: there is exactly one searchable document per source aggregate, so a revised entry
/// overwrites the prior one (no stale text) and re-delivery of the same state is harmless. The
/// <c>cloudevent.id</c> is preserved verbatim as the searchable hit's <c>SourceUri</c> so callers can
/// recover the source aggregate id from a result.</para>
///
/// <para>This abstraction lives in the EventStore package so <see cref="EventIngestionService"/> can route
/// curated events without taking a compile-time reference on the Server-side RediSearch types (ADR 9.1-D),
/// mirroring <see cref="ITenantStatusAccessor"/>.</para>
/// </summary>
public interface ISearchIndexMaintenance
{
    /// <summary>Upserts a single curated entry into the resolved index, replacing any prior entry that
    /// shares the same (<paramref name="indexTenantId"/>, <see cref="SearchIndexEntryChanged.AggregateId"/>)
    /// key.</summary>
    /// <param name="indexTenantId">The resolved Memories tenant partition (the search index) the entry
    /// belongs to. Authoritative — taken from routing, not from the event payload.</param>
    /// <param name="sourceUri">The stable source identity (the CloudEvent <c>id</c>) echoed back verbatim as
    /// the search hit's <c>SourceUri</c>.</param>
    /// <param name="entry">The curated entry snapshot carrying the searchable text and attributes.</param>
    /// <param name="caseId">The resolved case id stored alongside the entry for schema parity; optional.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A task that completes when the entry has been indexed.</returns>
    Task ApplyEntryChangedAsync(
        string indexTenantId,
        string sourceUri,
        SearchIndexEntryChanged entry,
        string? caseId,
        CancellationToken cancellationToken);

    /// <summary>Deletes the curated entry identified by (<paramref name="indexTenantId"/>,
    /// <see cref="SearchIndexEntryRemoved.AggregateId"/>). Idempotent: removing a non-existent entry is a
    /// no-op.</summary>
    /// <param name="indexTenantId">The resolved Memories tenant partition (the search index) the entry
    /// belongs to. Authoritative — taken from routing, not from the event payload.</param>
    /// <param name="entry">The removal snapshot carrying the aggregate id to delete.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A task that completes when the entry has been removed.</returns>
    Task ApplyEntryRemovedAsync(
        string indexTenantId,
        SearchIndexEntryRemoved entry,
        CancellationToken cancellationToken);
}
