// <copyright file="IngestionInput.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Contracts.V1;

/// <summary>Input for the ingestion workflow — a single file to be processed through the full pipeline.</summary>
public sealed record IngestionInput : IWorkflowTraceContextCarrier
{
    public required string TenantId { get; init; }

    public required string CaseId { get; init; }

    public required string SourceUri { get; init; }

    /// <summary>Gets the payload bytes. Required (non-null, non-empty) when <see cref="SourceType"/> is <see cref="SourceType.File"/>; MUST be null (or empty) when <see cref="SourceType"/> is <see cref="SourceType.Url"/> — the workflow fetches the body via FetchUrlActivity.</summary>
    public byte[]? ContentBytes { get; init; }

    /// <summary>Gets the optional claim-check reference for non-URL payload bytes. New server schedules use this instead of serializing <see cref="ContentBytes"/> into workflow history.</summary>
    public WorkflowPayloadReference? PayloadReference { get; init; }

    public required string ContentType { get; init; }

    public required SourceType SourceType { get; init; }

    public required string IngestedBy { get; init; }

    // Pinned to StringComparer.Ordinal (decision D6 — committed-branch review 2026-04-24) so the
    // CloudEvent metadata keys the workflow reads back (e.g., "cloudevent.type",
    // "event.aggregateType") match exactly what the producers wrote. Default
    // EqualityComparer<string>.Default is also ordinal today, but pinning makes the contract
    // explicit and guards against future ambiguity.
    // S6-P12 (re-review 2026-04-25): fast-path skips reallocation when the assigned dictionary
    // already uses StringComparer.Ordinal — preserves reference identity for `with`-expressions
    // that round-trip the same Metadata instance.
    public Dictionary<string, MetadataField> Metadata
    {
        get => field ??= new Dictionary<string, MetadataField>(StringComparer.Ordinal);
        init => field = value switch
        {
            null => new Dictionary<string, MetadataField>(StringComparer.Ordinal),
            Dictionary<string, MetadataField> existing when ReferenceEquals(existing.Comparer, StringComparer.Ordinal) => existing,
            _ => new Dictionary<string, MetadataField>(value, StringComparer.Ordinal),
        };
    }

    public string? CausationId { get; init; }

    public string? CorrelationId { get; init; }

    /// <summary>Gets the durable workflow configuration captured at scheduling time.</summary>
    public IngestionWorkflowConfiguration? WorkflowConfiguration { get; init; }

    /// <summary>Gets the serialized request trace context captured before scheduling the workflow.</summary>
    public WorkflowTraceContext? TraceContext { get; init; }

    /// <summary>
    /// Gets the optional explicit idempotency token. When supplied it takes precedence over
    /// <see cref="SourceUri"/> as the dedup identity, so two near-simultaneous ingests carrying the same
    /// token resolve to a single memory unit. When absent (the default), dedup falls back to the
    /// <see cref="SourceUri"/> natural key exactly as before. A supplied token <em>augments</em> the
    /// permanent <c>sourceUri → MemoryUnitId</c> mapping; it never replaces it. Story 18.4.
    /// </summary>
    public string? IdempotencyToken { get; init; }
}
