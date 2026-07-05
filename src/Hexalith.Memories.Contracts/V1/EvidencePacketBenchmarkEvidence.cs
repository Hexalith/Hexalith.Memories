// <copyright file="EvidencePacketBenchmarkEvidence.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Contracts.V1;

using System.Text.Json.Serialization;

/// <summary>Benchmark evidence attached to an Evidence Packet.</summary>
/// <param name="HybridNdcg10">Hybrid retrieval NDCG@10 score, when known.</param>
/// <param name="SyntacticNdcg10">Syntactic-only NDCG@10 score, when known.</param>
/// <param name="SemanticNdcg10">Semantic-only NDCG@10 score, when known.</param>
/// <param name="GraphNdcg10">Graph-only NDCG@10 score, when known.</param>
/// <param name="Threshold">Required thesis-validation threshold, when known.</param>
/// <param name="ThresholdPassed">Whether the benchmark passed the threshold, when known.</param>
/// <param name="CorpusId">Benchmark corpus or fixture identifier, when known.</param>
/// <param name="RunId">Benchmark run identifier, when known.</param>
/// <param name="RunAt">Timestamp when the benchmark ran, when known.</param>
/// <param name="PerQuery">Per-query benchmark evidence, when known.</param>
/// <param name="EvidenceUri">Safe reproducible evidence URI, when known.</param>
public sealed record EvidencePacketBenchmarkEvidence(
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] double? HybridNdcg10 = null,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] double? SyntacticNdcg10 = null,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] double? SemanticNdcg10 = null,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] double? GraphNdcg10 = null,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] double? Threshold = null,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] bool? ThresholdPassed = null,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? CorpusId = null,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? RunId = null,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] DateTimeOffset? RunAt = null,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] IReadOnlyList<EvidencePacketBenchmarkQuery>? PerQuery = null,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? EvidenceUri = null);
