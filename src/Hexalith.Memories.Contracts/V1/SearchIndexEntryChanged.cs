// <copyright file="SearchIndexEntryChanged.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Contracts.V1;

/// <summary>
/// Domain-agnostic integration event announcing that a single searchable entry in a Memories
/// search index was created or revised. Carried as the CloudEvent <c>data</c> payload on the
/// ingestion topic; the CloudEvent <c>id</c> is the stable source identity echoed back verbatim
/// as <see cref="ScoredResult.SourceUri"/>.
/// </summary>
/// <remarks>
/// The entry identity is the composite key (<see cref="TenantId"/>, <see cref="AggregateId"/>).
/// Receivers MUST index it idempotently with upsert (replace-by-key) semantics so re-delivery of
/// the same state is harmless and a revised entry overwrites the prior one (no stale text). A
/// producer publishes one curated entry per source aggregate carrying the full current snapshot;
/// <see cref="SearchIndexEntryRemoved"/> signals deletion.
/// </remarks>
public sealed record SearchIndexEntryChanged
{
    /// <summary>Gets the identifier of the search index (Memories tenant partition) the entry belongs to.</summary>
    public required string TenantId { get; init; }

    /// <summary>Gets the source aggregate identifier — the primary key, within the index, for upsert semantics.</summary>
    public required string AggregateId { get; init; }

    /// <summary>Gets the searchable text indexed for this entry (e.g. a display name and its identifier).</summary>
    public required string Text { get; init; }

    /// <summary>Gets the structured, exactly-matched attributes the entry can be filtered by (e.g. <c>status</c>).</summary>
    public Dictionary<string, string> Attributes { get; init; } = new(StringComparer.Ordinal);

    /// <summary>Gets the correlation identifier for tracing, when supplied by the producer.</summary>
    public string? CorrelationId { get; init; }

    /// <summary>Gets the causation identifier for tracing, when supplied by the producer.</summary>
    public string? CausationId { get; init; }
}
