// <copyright file="FailedUnitInput.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Contracts.V1;

/// <summary>Input for <c>PersistFailedUnitActivity</c> (Story 6.3 NFR19, FR11). Carries safe fields needed
/// to rebuild an <see cref="IngestionInput"/> for re-ingestion without storing raw payload bytes in the failed-unit record.</summary>
public sealed record FailedUnitInput(
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
