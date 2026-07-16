// <copyright file="ScoreNormalizer.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Search;

/// <summary>
/// Pure static functions that normalize raw search scores from each axis to [0.0, 1.0].
/// All methods are deterministic with no I/O or state.
/// </summary>
internal static class ScoreNormalizer
{
    /// <summary>
    /// Normalizes a raw BM25 score from RediSearch to [0.0, 1.0] using saturation normalization.
    /// The saturation constant <c>k</c> adapts to corpus size and average document length (bytes per document
    /// from RediSearch index metadata), ensuring mid-range BM25 scores normalize to ~0.5 regardless of corpus characteristics.
    /// </summary>
    /// <param name="rawScore">The raw BM25 score (unbounded positive range).</param>
    /// <param name="documentCount">The number of documents in the tenant's corpus.</param>
    /// <param name="averageDocumentLength">The average document length in bytes (from <c>DocTableSizeMB / NumDocs</c>).</param>
    /// <returns>A normalized score in [0.0, 1.0], or 0.0 for invalid inputs.</returns>
    internal static double NormalizeBm25(double rawScore, int documentCount, double averageDocumentLength)
    {
        if (!double.IsFinite(rawScore) ||
            rawScore <= 0.0 ||
            documentCount <= 0 ||
            !double.IsFinite(averageDocumentLength) ||
            averageDocumentLength <= 0.0)
        {
            return 0.0;
        }

        // Corpus-adaptive saturation constant:
        // - log2(docCount+1) scales with corpus size (more docs = higher threshold)
        // - avgDocLen/100 scales with document length (longer docs = higher raw BM25)
        double k = Math.Log2((double)documentCount + 1.0) * (averageDocumentLength / 100.0);
        if (!double.IsFinite(k) || k <= 0.0)
        {
            return 0.0;
        }

        return Math.Clamp(rawScore / (rawScore + k), 0.0, 1.0);
    }

    /// <summary>
    /// Normalizes a cosine similarity score. Since Redis Vector already returns similarity in [0.0, 1.0]
    /// (after distance-to-similarity conversion in <c>SemanticSearchService</c>), this is a defensive clamp.
    /// </summary>
    /// <param name="cosineScore">The cosine similarity score.</param>
    /// <returns>The score clamped to [0.0, 1.0], or 0.0 if the input is NaN or Infinity.</returns>
    internal static double NormalizeCosine(double cosineScore)
    {
        if (!double.IsFinite(cosineScore))
        {
            return 0.0;
        }

        return Math.Clamp(cosineScore, 0.0, 1.0);
    }

    /// <summary>
    /// Normalizes a graph proximity score via inverse hop distance with decay: <c>1 / (1 + hopDistance)</c>.
    /// Hop 0 → 1.0, Hop 1 → 0.5, Hop 2 → 0.333, Hop 3 → 0.25.
    /// </summary>
    /// <param name="hopDistance">The number of hops from the start node (must be non-negative).</param>
    /// <returns>A normalized score in (0.0, 1.0].</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="hopDistance"/> is negative.</exception>
    internal static double NormalizeGraphProximity(int hopDistance)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(hopDistance);
        return Math.Clamp(1.0 / (1.0 + hopDistance), 0.0, 1.0);
    }
}
