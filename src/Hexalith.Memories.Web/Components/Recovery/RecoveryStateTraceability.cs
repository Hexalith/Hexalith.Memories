// <copyright file="RecoveryStateTraceability.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Web.Components.Recovery;

/// <summary>
/// Field-level traceability table from every <see cref="RecoveryStateKind"/> to the named Evidence
/// Packet fields that justify it, plus its localization keys, severity, and affected capability.
/// </summary>
/// <remarks>
/// Story 17.2 — this table is the single source of truth consumed by <see cref="RecoveryStateMapper"/>.
/// A rendered state, diagnostic clue, affected capability, or recovery action must trace back to a named
/// contract source here; states with no side-channel-safe contract signal record
/// <see cref="NoContractSource"/> instead of inferring a cause.
/// </remarks>
public static class RecoveryStateTraceability
{
    /// <summary>Sentinel for states that have no side-channel-safe Evidence Packet source.</summary>
    public const string NoContractSource = "(none — no safe contract source)";

    /// <summary>Gets the traceability rows, one per recovery state.</summary>
    public static IReadOnlyList<RecoveryStateTrace> Entries { get; } =
    [
        Trace(
            RecoveryStateKind.Supported,
            RecoveryCapability.AnswerSupport,
            RecoverySeverity.None,
            "EvidencePacket.State",
            "EvidencePacket.Evidence.EvidenceStrength"),
        Trace(
            RecoveryStateKind.Weak,
            RecoveryCapability.EvidenceStrength,
            RecoverySeverity.Caution,
            "EvidencePacket.State",
            "EvidencePacket.Evidence.EvidenceStrength"),
        Trace(
            RecoveryStateKind.StaleMemory,
            RecoveryCapability.Freshness,
            RecoverySeverity.Caution,
            "EvidencePacket.State"),
        Trace(
            RecoveryStateKind.DegradedBackend,
            RecoveryCapability.Retrieval,
            RecoverySeverity.Warning,
            "EvidencePacket.Evidence.Degraded",
            "EvidencePacket.Evidence.AllEnabledAxesUnavailable",
            "EvidencePacket.OmittedDetails.Reason",
            "EvidencePacket.State"),
        Trace(
            RecoveryStateKind.Unauthorized,
            RecoveryCapability.Access,
            RecoverySeverity.Critical,
            "EvidencePacket.Scope.IsolationStatus",
            "EvidencePacket.State",
            "EvidencePacket.OmittedDetails.Reason"),
        Trace(
            RecoveryStateKind.Compressed,
            RecoveryCapability.DetailCompleteness,
            RecoverySeverity.Caution,
            "EvidencePacket.OmittedDetails.Reason",
            "EvidencePacket.OmittedDetails.OmittedCount",
            "EvidencePacket.State"),
        Trace(
            RecoveryStateKind.Conflicting,
            RecoveryCapability.AnswerConfidence,
            RecoverySeverity.Warning,
            "EvidencePacket.Evidence.Degraded",
            "EvidencePacket.Evidence.AxesUsed",
            "EvidencePacket.Evidence.UnavailableAxes"),
        Trace(
            RecoveryStateKind.NoMatch,
            RecoveryCapability.Search,
            RecoverySeverity.Caution,
            "EvidencePacket.State",
            "EvidencePacket.Result.HasIndexedMemoryUnits",
            "EvidencePacket.Result.ReturnedCount"),
        Trace(
            RecoveryStateKind.NotIngestedYet,
            RecoveryCapability.Ingestion,
            RecoverySeverity.Warning,
            "EvidencePacket.Result.HasIndexedMemoryUnits"),
        Trace(
            RecoveryStateKind.WrongCase,
            RecoveryCapability.Search,
            RecoverySeverity.Caution,
            NoContractSource),
        Trace(
            RecoveryStateKind.GraphGap,
            RecoveryCapability.GraphContext,
            RecoverySeverity.Warning,
            "EvidencePacket.Graph.GapMarkers",
            "EvidencePacket.Graph.Available",
            "EvidencePacket.Evidence.UnavailableAxes"),
        Trace(
            RecoveryStateKind.InsufficientEvidence,
            RecoveryCapability.AnswerSupport,
            RecoverySeverity.Caution,
            "EvidencePacket.State",
            "EvidencePacket.Evidence.EvidenceStrength",
            "EvidencePacket.Result.ReturnedCount"),
        Trace(
            RecoveryStateKind.Unknown,
            RecoveryCapability.AnswerSupport,
            RecoverySeverity.Caution,
            NoContractSource),
    ];

    /// <summary>Gets the traceability row for a recovery state.</summary>
    /// <param name="kind">The recovery state.</param>
    /// <returns>The matching <see cref="RecoveryStateTrace"/>.</returns>
    public static RecoveryStateTrace For(RecoveryStateKind kind)
        => Entries.Single(e => e.Kind == kind);

    private static RecoveryStateTrace Trace(
        RecoveryStateKind kind,
        RecoveryCapability capability,
        RecoverySeverity severity,
        params string[] contractSources)
        => new(
            kind,
            RecoveryResourceKeys.Title(kind),
            RecoveryResourceKeys.Explanation(kind),
            capability,
            RecoveryResourceKeys.Capability(capability),
            severity,
            contractSources);
}
