// <copyright file="FilterAxisTraceability.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Web.Components.Filters;

/// <summary>
/// Field-level traceability table from every <see cref="MemoriesFilterAxis"/> to the named Evidence Packet
/// contract fields it constrains.
/// </summary>
/// <remarks>
/// Story 17.3 (Task 0, AC2) — consumed by <see cref="FilterInspectionMapper"/>. Freshness, time range, and
/// metadata have no canonical Evidence Packet field yet (the producer side is owned by Story 2.7), so they
/// record <see cref="NoContractSource"/> rather than inventing a web-only backing field.
/// </remarks>
public static class FilterAxisTraceability
{
    /// <summary>Sentinel for axes that have no canonical Evidence Packet contract source yet.</summary>
    public const string NoContractSource = "(none — no contract source)";

    /// <summary>Gets the traceability rows, one per filter axis.</summary>
    public static IReadOnlyList<FilterAxisTrace> Entries { get; } =
    [
        Trace(MemoriesFilterAxis.RetrievalAxis, "EvidencePacket.Evidence.AxesUsed", "EvidencePacket.Evidence.UnavailableAxes"),
        Trace(MemoriesFilterAxis.SourceType, "EvidencePacket.Sources.SourceType"),
        Trace(MemoriesFilterAxis.Freshness, NoContractSource),
        Trace(MemoriesFilterAxis.Confidence, "EvidencePacket.Evidence.EvidenceStrength", "EvidencePacket.Sources.Score"),
        Trace(MemoriesFilterAxis.TimeRange, NoContractSource),
        Trace(MemoriesFilterAxis.Metadata, NoContractSource),
        Trace(MemoriesFilterAxis.GraphDepth, "EvidencePacket.Graph.RelatedPath", "EvidencePacket.Graph.EdgeTypes"),
        Trace(MemoriesFilterAxis.EvidenceState, "EvidencePacket.State"),
    ];

    /// <summary>Gets the traceability row for an axis.</summary>
    /// <param name="axis">The filter axis.</param>
    /// <returns>The matching <see cref="FilterAxisTrace"/>.</returns>
    public static FilterAxisTrace For(MemoriesFilterAxis axis)
        => Entries.Single(e => e.Axis == axis);

    private static FilterAxisTrace Trace(MemoriesFilterAxis axis, params string[] contractSources)
        => new(axis, FilterResourceKeys.Axis(axis), contractSources);
}
