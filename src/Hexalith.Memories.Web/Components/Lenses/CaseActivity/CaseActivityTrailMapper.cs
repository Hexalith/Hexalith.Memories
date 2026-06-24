// <copyright file="CaseActivityTrailMapper.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Web.Components.Lenses.CaseActivity;

using System.Globalization;

using Hexalith.Memories.Contracts.V1;
using Hexalith.Memories.Web.Components.Evidence;
using Hexalith.Memories.Web.Components.Recovery;

/// <summary>
/// Pure, deterministic projection of a canonical Evidence Packet into the Case Activity Trail lens (AC1).
/// </summary>
/// <remarks>
/// Story 17.4 — the trail surfaces only what the packet exposes: ranked source citations, annotation
/// counts, graph relationships and gaps, the current trust state, and safe recovery actions. The canonical
/// contract exposes no activity timestamps, so ordering is deterministic by rank and the lens declares the
/// missing-timestamp gap rather than inferring chronology. Under a restrictive scope, source and graph
/// detail are suppressed for redaction parity and only the trust state plus authorization recovery remain.
/// </remarks>
public static class CaseActivityTrailMapper
{
    /// <summary>Maps a packet into the Case Activity Trail view model.</summary>
    /// <param name="packet">The canonical Evidence Packet.</param>
    /// <param name="role">The active role-density profile (ordering/grouping only).</param>
    /// <returns>The typed, sanitized activity trail.</returns>
    public static CaseActivityTrailViewModel Map(EvidencePacket packet, LensRole role)
    {
        ArgumentNullException.ThrowIfNull(packet);

        RecoveryStateViewModel recovery = RecoveryStateMapper.Map(packet);
        bool restrictive = EvidenceDisplay.IsRestrictiveScope(packet.Scope.IsolationStatus)
            || packet.State == EvidencePacketState.Unauthorized;
        LensRoleDensityProfile density = LensRoleDensity.For(role);

        List<CaseActivityRow> rows = [];
        int order = 0;
        int activityCount = 0;

        if (!restrictive)
        {
            // Source citations — the actual memory changes, source-linked where the contract allows.
            foreach (EvidencePacketSource source in packet.Sources)
            {
                (LensFieldAvailability availability, string safeLink) = ClassifyLink(source.SourceUri, source.MemoryUnitId);
                rows.Add(new CaseActivityRow(
                    order++,
                    CaseActivityKind.SourceCitation,
                    CaseActivityResourceKeys.Kind(CaseActivityKind.SourceCitation),
                    EvidenceDisplay.SafeText(source.Snippet, "snippet unavailable"),
                    availability,
                    safeLink,
                    CaseActivityResourceKeys.LinkStatus(availability),
                    LinkSeverity(availability)));
                activityCount++;

                if (source.AnnotationsCount > 0)
                {
                    rows.Add(new CaseActivityRow(
                        order++,
                        CaseActivityKind.Annotation,
                        CaseActivityResourceKeys.Kind(CaseActivityKind.Annotation),
                        string.Create(CultureInfo.InvariantCulture, $"{source.AnnotationsCount} annotations"),
                        availability,
                        safeLink,
                        CaseActivityResourceKeys.LinkStatus(availability),
                        RecoverySeverity.Info));
                    activityCount++;
                }
            }

            // Graph relationships — only when graph evidence is available.
            if (packet.Graph.Available)
            {
                string edgeContext = packet.Graph.EdgeTypes.Count > 0
                    ? EvidenceDisplay.SafeText(string.Join(", ", packet.Graph.EdgeTypes), "relationship")
                    : "relationship";
                foreach (string node in packet.Graph.RelatedPath)
                {
                    (LensFieldAvailability availability, string safeLink) = ClassifyLink(node, null);
                    rows.Add(new CaseActivityRow(
                        order++,
                        CaseActivityKind.Relationship,
                        CaseActivityResourceKeys.Kind(CaseActivityKind.Relationship),
                        edgeContext,
                        availability,
                        safeLink,
                        CaseActivityResourceKeys.LinkStatus(availability),
                        LinkSeverity(availability)));
                    activityCount++;
                }
            }

            // Graph gaps — missing relationships rendered as explicit gap activity (not silent omission).
            foreach (string marker in packet.Graph.GapMarkers)
            {
                rows.Add(new CaseActivityRow(
                    order++,
                    CaseActivityKind.GraphGap,
                    CaseActivityResourceKeys.Kind(CaseActivityKind.GraphGap),
                    EvidenceDisplay.SafeText(marker, "gap"),
                    LensFieldAvailability.Unavailable,
                    "gap",
                    CaseActivityResourceKeys.LinkStatus(LensFieldAvailability.Unavailable),
                    RecoverySeverity.Caution));
                activityCount++;
            }
        }

        // Current trust state — always present as continuity context, status-labelled via the shared grammar.
        rows.Add(new CaseActivityRow(
            order++,
            CaseActivityKind.TrustState,
            CaseActivityResourceKeys.Kind(CaseActivityKind.TrustState),
            recovery.DiagnosticClueCode,
            restrictive ? LensFieldAvailability.Unauthorized : LensFieldAvailability.Available,
            restrictive ? "unauthorized" : "state",
            RecoveryResourceKeys.Title(recovery.StateKind),
            recovery.Severity));

        // Safe recovery actions attached to the packet (primary first, then any available secondary).
        if (recovery.PrimaryAction is { } primary)
        {
            rows.Add(RecoveryRow(order++, primary));
        }

        foreach (RecoveryActionView action in recovery.SecondaryActions)
        {
            if (action.Availability == RecoveryActionAvailability.Available)
            {
                rows.Add(RecoveryRow(order++, action));
            }
        }

        // Role density changes ONLY the presentation sequence — never which rows exist, their status,
        // links, or severity. A stable sort by a role-specific key preserves canonical order within ties,
        // then Order is reassigned to the display sequence. Tests assert the row multiset is role-invariant.
        IReadOnlyList<CaseActivityRow> finalRows = rows
            .OrderBy(r => RoleSortKey(r, density.Role))
            .Select((r, index) => r with { Order = index })
            .ToArray();

        return new CaseActivityTrailViewModel(
            Rows: finalRows,
            TimestampsAvailable: false,
            OrderingBasisKey: CaseActivityResourceKeys.OrderingBasis,
            IsEmpty: activityCount == 0,
            EmptyReasonKey: CaseActivityResourceKeys.Empty);
    }

    // Presentational ordering key per role. Operators lead with the highest-severity rows; team leads lead
    // with trust-state and recovery context; developers and agent integrators keep the canonical
    // detail-first (by rank) order. Stable OrderBy preserves canonical sequence within equal keys.
    private static int RoleSortKey(CaseActivityRow row, LensRole role) => role switch
    {
        LensRole.Operator => row.Severity switch
        {
            RecoverySeverity.Critical => 0,
            RecoverySeverity.Warning => 1,
            RecoverySeverity.Caution => 2,
            RecoverySeverity.Info => 3,
            _ => 4,
        },
        LensRole.TeamLead => row.Kind switch
        {
            CaseActivityKind.TrustState => 0,
            CaseActivityKind.Recovery => 1,
            _ => 2,
        },
        _ => 0,
    };

    private static CaseActivityRow RecoveryRow(int order, RecoveryActionView action)
        => new(
            order,
            CaseActivityKind.Recovery,
            CaseActivityResourceKeys.Kind(CaseActivityKind.Recovery),
            action.Guidance,
            LensFieldAvailability.Available,
            action.Target,
            RecoveryResourceKeys.Action(action.Kind),
            RecoverySeverity.Info);

    private static (LensFieldAvailability Availability, string SafeLink) ClassifyLink(string? primary, string? fallback)
    {
        string? chosen = !string.IsNullOrWhiteSpace(primary) ? primary : fallback;
        if (string.IsNullOrWhiteSpace(chosen))
        {
            // Missing source link rendered as an explicit unavailable state, never a broken link.
            return (LensFieldAvailability.Unavailable, "source unavailable");
        }

        string safe = EvidenceDisplay.SafeText(chosen, "source unavailable");
        return safe.Contains(EvidenceDisplay.RedactedMarker, StringComparison.Ordinal)
            ? (LensFieldAvailability.Redacted, safe)
            : (LensFieldAvailability.Available, safe);
    }

    private static RecoverySeverity LinkSeverity(LensFieldAvailability availability) => availability switch
    {
        LensFieldAvailability.Available => RecoverySeverity.Info,
        LensFieldAvailability.Redacted => RecoverySeverity.Caution,
        LensFieldAvailability.Unauthorized => RecoverySeverity.Critical,
        _ => RecoverySeverity.Caution,
    };
}
