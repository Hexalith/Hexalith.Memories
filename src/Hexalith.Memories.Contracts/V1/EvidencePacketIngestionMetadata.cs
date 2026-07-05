// <copyright file="EvidencePacketIngestionMetadata.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Contracts.V1;

using System.Text.Json.Serialization;

/// <summary>Ingestion lifecycle metadata for an Evidence Packet source.</summary>
/// <param name="Stage">Stable ingestion stage taxonomy value.</param>
/// <param name="StageDetail">Optional safe stage detail supplied by the producer.</param>
/// <param name="UpdatedAt">Timestamp when the ingestion stage was updated, when known.</param>
/// <param name="RetryCount">Retry count for this ingestion unit, when known.</param>
public sealed record EvidencePacketIngestionMetadata(
    EvidencePacketIngestionStage Stage,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? StageDetail = null,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] DateTimeOffset? UpdatedAt = null,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] int? RetryCount = null);
