// <copyright file="SearchExplanation.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Contracts.V1;

using System.Text.Json.Serialization;

/// <summary>Response-level metadata describing how search scores were computed. Present only when explain=true.</summary>
public sealed record SearchExplanation
{
    /// <summary>Gets the confidence score caveat reminding consumers that scores measure query-result relevance, not factual accuracy.</summary>
    public required string Caveat { get; init; }

    /// <summary>Gets the per-axis normalization details, keyed by axis name ("syntactic", "semantic", "graph").</summary>
    public required IReadOnlyDictionary<string, AxisExplanation> AxisDetails { get; init; }

    /// <summary>Gets the fusion weights applied during hybrid scoring. Null for single-axis searches.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public FusionWeights? WeightsUsed { get; init; }
}

/// <summary>Describes the normalization method applied to a single search axis.</summary>
public sealed record AxisExplanation
{
    /// <summary>Gets the machine-readable normalization method name (e.g., "bm25_saturation").</summary>
    public required string NormalizationMethod { get; init; }

    /// <summary>Gets a human-readable description of the normalization formula.</summary>
    public required string Description { get; init; }
}
