// <copyright file="IngestionInput.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Contracts.V1;

/// <summary>Input for the ingestion workflow — a single file to be processed through the full pipeline.</summary>
public sealed record IngestionInput
{
    public required string TenantId { get; init; }

    public required string CaseId { get; init; }

    public required string SourceUri { get; init; }

    /// <summary>Gets the payload bytes. Required (non-null, non-empty) when <see cref="SourceType"/> is <see cref="SourceType.File"/>; MUST be null (or empty) when <see cref="SourceType"/> is <see cref="SourceType.Url"/> — the workflow fetches the body via FetchUrlActivity.</summary>
    public byte[]? ContentBytes { get; init; }

    public required string ContentType { get; init; }

    public required SourceType SourceType { get; init; }

    public required string IngestedBy { get; init; }

    // Pinned to StringComparer.Ordinal (decision D6 — committed-branch review 2026-04-24) so the
    // CloudEvent metadata keys the workflow reads back (e.g., "cloudevent.type",
    // "event.aggregateType") match exactly what the producers wrote. Default
    // EqualityComparer<string>.Default is also ordinal today, but pinning makes the contract
    // explicit and guards against future ambiguity.
    public Dictionary<string, MetadataField> Metadata
    {
        get => field ??= new Dictionary<string, MetadataField>(StringComparer.Ordinal);
        init => field = value is null
            ? new Dictionary<string, MetadataField>(StringComparer.Ordinal)
            : new Dictionary<string, MetadataField>(value, StringComparer.Ordinal);
    }

    public string? CausationId { get; init; }

    public string? CorrelationId { get; init; }
}
