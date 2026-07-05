// <copyright file="EvidencePacketBenchmarkQuery.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Contracts.V1;

using System.Text.Json.Serialization;

/// <summary>Per-query benchmark evidence attached to an Evidence Packet benchmark run.</summary>
/// <param name="QueryId">Stable benchmark query identifier.</param>
/// <param name="HybridNdcg10">Hybrid retrieval NDCG@10 score for this query, when known.</param>
/// <param name="BestSingleAxisNdcg10">Best single-axis NDCG@10 score for this query, when known.</param>
/// <param name="ThresholdPassed">Whether this query passed its threshold, when known.</param>
public sealed record EvidencePacketBenchmarkQuery(
    string QueryId,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] double? HybridNdcg10 = null,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] double? BestSingleAxisNdcg10 = null,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] bool? ThresholdPassed = null);
