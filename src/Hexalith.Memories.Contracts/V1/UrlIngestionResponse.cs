// <copyright file="UrlIngestionResponse.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Contracts.V1;

/// <summary>Response for POST /api/v1/ingest/url — mirrors the shape used by POST /api/v1/ingest.</summary>
/// <param name="InstanceId">Workflow instance identifier.</param>
/// <param name="SourceUri">The URL that was scheduled for ingestion.</param>
/// <param name="MemoryUnitId">Reserved for future correlation; always null for URL ingestion until workflow starts.</param>
/// <param name="SourceType">Always "url" for this endpoint.</param>
public sealed record UrlIngestionResponse(
    string InstanceId,
    string SourceUri,
    string? MemoryUnitId = null,
    string SourceType = "url");
