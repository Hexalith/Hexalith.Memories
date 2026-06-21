// <copyright file="SearchIndexEntryRemoved.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Contracts.V1;

/// <summary>
/// Domain-agnostic integration event announcing that a single searchable entry was removed from a
/// Memories search index. Carried as the CloudEvent <c>data</c> payload on the ingestion topic.
/// </summary>
/// <remarks>
/// Receivers MUST remove the entry idempotently by the composite key
/// (<see cref="TenantId"/>, <see cref="AggregateId"/>). It is the deletion counterpart of
/// <see cref="SearchIndexEntryChanged"/>. Producers whose aggregates are soft-deleted (never
/// hard-removed) only ever publish <see cref="SearchIndexEntryChanged"/> and never this event.
/// </remarks>
public sealed record SearchIndexEntryRemoved
{
    /// <summary>Gets the identifier of the search index (Memories tenant partition) the entry belonged to.</summary>
    public required string TenantId { get; init; }

    /// <summary>Gets the source aggregate identifier — the primary key, within the index, of the removed entry.</summary>
    public required string AggregateId { get; init; }

    /// <summary>Gets the correlation identifier for tracing, when supplied by the producer.</summary>
    public string? CorrelationId { get; init; }

    /// <summary>Gets the causation identifier for tracing, when supplied by the producer.</summary>
    public string? CausationId { get; init; }
}
