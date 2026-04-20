// <copyright file="ConsistencySyntacticDetail.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Contracts.V1;

/// <summary>
/// Extracted fields from the syntactic <c>{tenantId}:mu:{id}</c> Redis hash, surfaced by the
/// per-unit inspection endpoint. All fields come from <c>IndexSyntacticActivity</c>'s write
/// pattern. Empty strings for fields that are absent on the hash (the hash may have been
/// written by an older ingestion version before a field was introduced).
/// </summary>
/// <param name="ContentHash">Content hash used for idempotency / dedup.</param>
/// <param name="IngestedAt">When the unit was ingested.</param>
/// <param name="SourceUri">The source URI of the unit.</param>
/// <param name="SourceType">Source type (camelCase string from <c>SourceType</c> enum).</param>
/// <param name="CaseId">The case the unit belongs to.</param>
/// <param name="EmbeddingProvider">Provider that produced the embedding (e.g. "gemini").</param>
/// <param name="EmbeddingModel">Model identifier (e.g. "gemini-embedding-001").</param>
public sealed record ConsistencySyntacticDetail(
    string ContentHash,
    DateTimeOffset IngestedAt,
    string SourceUri,
    string SourceType,
    string CaseId,
    string EmbeddingProvider,
    string EmbeddingModel);
