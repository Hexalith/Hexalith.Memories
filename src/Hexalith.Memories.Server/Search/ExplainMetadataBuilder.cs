// <copyright file="ExplainMetadataBuilder.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Search;

using Hexalith.Memories.Contracts.V1;

/// <summary>
/// Builds <see cref="SearchExplanation"/> metadata for search responses when explain mode is enabled.
/// Pure static functions with no I/O or state.
/// </summary>
internal static class ExplainMetadataBuilder
{
    /// <summary>The standard caveat included in all explain responses.</summary>
    internal const string Caveat = "Confidence scores measure query-result relevance, NOT factual accuracy or data completeness";

    // Descriptions must stay in sync with ScoreNormalizer methods. Update here when normalization formulas change.
    private static readonly Dictionary<string, AxisExplanation> AxisExplanations = new(StringComparer.OrdinalIgnoreCase)
    {
        ["syntactic"] = new AxisExplanation
        {
            NormalizationMethod = "bm25_saturation",
            Description = "BM25 saturation normalization: score / (score + k), where k = log2(docCount + 1) * (avgDocLen / 100)",
        },
        ["semantic"] = new AxisExplanation
        {
            NormalizationMethod = "cosine_clamp",
            Description = "Cosine similarity in [0.0, 1.0] with defensive clamp (Redis vector already returns similarity)",
        },
        ["graph"] = new AxisExplanation
        {
            NormalizationMethod = "inverse_hop_decay",
            Description = "Inverse hop distance with decay: 1.0 / (1.0 + hopDistance)",
        },
    };

    /// <summary>
    /// Builds explain metadata for a hybrid (multi-axis) search, including all active axes and fusion weights.
    /// </summary>
    /// <param name="activeAxes">The set of axis names that were enabled for this search.</param>
    /// <param name="weights">The fusion weights applied during scoring.</param>
    /// <returns>A <see cref="SearchExplanation"/> with axis details for each active axis.</returns>
    internal static SearchExplanation BuildForHybrid(IReadOnlySet<string> activeAxes, FusionWeights weights)
    {
        Dictionary<string, AxisExplanation> axisDetails = new(StringComparer.OrdinalIgnoreCase);
        foreach (string axis in activeAxes)
        {
            if (AxisExplanations.TryGetValue(axis, out AxisExplanation? explanation))
            {
                axisDetails[axis.ToLowerInvariant()] = explanation;
            }
        }

        return new SearchExplanation
        {
            Caveat = Caveat,
            AxisDetails = axisDetails,
            WeightsUsed = weights,
        };
    }

    /// <summary>
    /// Builds explain metadata for a single-axis search (no fusion weights).
    /// </summary>
    /// <param name="axisName">The name of the single search axis.</param>
    /// <returns>A <see cref="SearchExplanation"/> with one axis detail entry.</returns>
    internal static SearchExplanation BuildForSingleAxis(string axisName)
    {
        Dictionary<string, AxisExplanation> axisDetails = new(StringComparer.OrdinalIgnoreCase);
        if (AxisExplanations.TryGetValue(axisName, out AxisExplanation? explanation))
        {
            axisDetails[axisName.ToLowerInvariant()] = explanation;
        }

        return new SearchExplanation
        {
            Caveat = Caveat,
            AxisDetails = axisDetails,
            WeightsUsed = null,
        };
    }
}
