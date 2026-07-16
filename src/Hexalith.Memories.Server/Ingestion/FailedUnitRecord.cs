// <copyright file="FailedUnitRecord.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Ingestion;

using Hexalith.Memories.Contracts.V1;

/// <summary>Internal complete view of a persisted failed unit, used to rebuild <see cref="IngestionInput"/>
/// for re-ingestion. Not exposed publicly — operators see <see cref="FailedUnitSummary"/> instead.</summary>
internal sealed record FailedUnitRecord(
    string TenantId,
    string CaseId,
    string MemoryUnitId,
    string SourceUri,
    SourceType SourceType,
    string IngestedBy,
    string? ContentType,
    string Stage,
    string ErrorCode,
    string? ErrorMessage,
    int RetryCount,
    DateTimeOffset? LastRetryAt,
    DateTimeOffset FailedAt,
    WorkflowPayloadReference? SourcePayloadReference = null,
    IReadOnlyDictionary<string, MetadataField>? Metadata = null,
    string? CausationId = null,
    string? CorrelationId = null);
