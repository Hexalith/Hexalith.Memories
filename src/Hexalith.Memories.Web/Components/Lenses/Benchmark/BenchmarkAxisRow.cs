// <copyright file="BenchmarkAxisRow.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Web.Components.Lenses.Benchmark;

/// <summary>
/// A single, sanitized retrieval-axis evidence row shown as the benchmark comparator proxy.
/// </summary>
/// <remarks>
/// Story 17.4 — this is retrieval-relevance axis evidence (EvidencePacket.Evidence.AxisEvidence), NOT a
/// benchmark NDCG@10 score. The score is paired with a text equivalent and an accessible bar so visual
/// layout is never the only way to read it. <see cref="ScorePercent"/> is a presentation-only scaling of a
/// finite [0,1] score for the bar; <see cref="SafeScore"/> is the authoritative text value.
/// </remarks>
/// <param name="Axis">Sanitized axis name.</param>
/// <param name="SafeScore">Sanitized score text equivalent, or the unavailable fallback.</param>
/// <param name="ScorePercent">Presentation-only [0,100] bar value; 0 when no finite score is available.</param>
/// <param name="HasScore">Whether a finite score was available.</param>
/// <param name="SafeNormalization">Sanitized normalization method, or the unavailable fallback.</param>
/// <param name="SafeDescription">Sanitized explain description, or the unavailable fallback.</param>
public sealed record BenchmarkAxisRow(
    string Axis,
    string SafeScore,
    int ScorePercent,
    bool HasScore,
    string SafeNormalization,
    string SafeDescription);
