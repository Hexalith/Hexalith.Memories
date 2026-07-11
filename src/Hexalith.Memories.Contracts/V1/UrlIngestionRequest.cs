// <copyright file="UrlIngestionRequest.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Contracts.V1;

/// <summary>Request body for POST /api/v1/ingest/url — ingests a single URL into the pipeline.</summary>
public sealed record UrlIngestionRequest
{
    public required string TenantId { get; init; }

    public required string CaseId { get; init; }

    public required string Url { get; init; }

    public required string IngestedBy { get; init; }

    public Dictionary<string, MetadataField> Metadata
    {
        get => field ??= [];
        init => field = value ?? [];
    }

    public string? CausationId { get; init; }

    public string? CorrelationId { get; init; }
}
