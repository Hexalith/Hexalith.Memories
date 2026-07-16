// <copyright file="RecoveryStateMapper.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Web.Components.Recovery;

using System.Globalization;

using Hexalith.Memories.Contracts.V1;
using Hexalith.Memories.Web.Components.Evidence;

/// <summary>
/// Pure, deterministic mapping from a canonical Evidence Packet to a <see cref="RecoveryStateViewModel"/>.
/// </summary>
/// <remarks>
/// <para>Story 17.2 — this adapter is a presentation mapping over <c>Contracts.V1</c> Evidence Packet
/// fields. It never mutates the packet, executes recovery work, retries ingestion, alters authorization,
/// or infers a cause from anything other than named contract fields.</para>
/// <para>State precedence is explicit and side-channel safe: authorization/inaccessible scope outranks
/// every other state and never reveals whether matching evidence exists; conflicting/disputed evidence
/// outranks confident-answer framing; degraded, stale, and compressed conditions remain visible as
/// secondary risk markers unless they are the highest-risk state.</para>
/// </remarks>
public static class RecoveryStateMapper
{
    /// <summary>Maps a packet to its recovery-state view model.</summary>
    /// <param name="packet">The canonical Evidence Packet.</param>
    /// <returns>The typed, sanitized recovery-state projection.</returns>
    public static RecoveryStateViewModel Map(EvidencePacket packet)
    {
        ArgumentNullException.ThrowIfNull(packet);

        RecoveryStateKind kind = ResolveStateKind(packet);
        RecoveryStateTrace trace = RecoveryStateTraceability.For(kind);
        bool restrictive = IsRestrictive(packet);

        IReadOnlyList<RecoveryActionView> actions = BuildActions(packet, restrictive);
        (RecoveryActionView? primary, IReadOnlyList<RecoveryActionView> secondary) = SplitPrimary(actions);
        IReadOnlyList<RecoveryRiskMarker> markers = BuildRiskMarkers(packet, kind);
        (IReadOnlyList<string> omittedNames, IReadOnlyList<RecoveryExpansionView> expansions) =
            BuildOmittedDetails(packet, kind);
        string clue = BuildDiagnosticClue(packet, kind);

        return new RecoveryStateViewModel(
            StateKind: kind,
            TitleKey: trace.TitleKey,
            ExplanationKey: trace.ExplanationKey,
            DiagnosticClueLabelKey: RecoveryResourceKeys.DiagnosticClueLabel,
            DiagnosticClueCode: clue,
            Severity: trace.Severity,
            AffectedCapabilityKey: trace.AffectedCapabilityKey,
            TenantId: EvidenceDisplay.SafeText(packet.Scope.TenantId, "unknown tenant"),
            CaseId: string.IsNullOrWhiteSpace(packet.Scope.CaseId)
                ? null
                : EvidenceDisplay.SafeText(packet.Scope.CaseId, "tenant scope"),
            PrimaryAction: primary,
            SecondaryActions: secondary,
            RiskMarkers: markers,
            OmittedDetailNames: omittedNames,
            Expansions: expansions,
            ContractSources: trace.ContractSources);
    }

    /// <summary>
    /// Resolves the primary presentation state from named contract fields using explicit precedence.
    /// </summary>
    /// <param name="packet">The canonical Evidence Packet.</param>
    /// <returns>The derived <see cref="RecoveryStateKind"/>.</returns>
    public static RecoveryStateKind ResolveStateKind(EvidencePacket packet)
    {
        ArgumentNullException.ThrowIfNull(packet);

        EvidencePacketEvidence evidence = packet.Evidence;
        EvidencePacketOmittedDetails omitted = packet.OmittedDetails;
        EvidencePacketResultSummary result = packet.Result;

        // 1. Authorization / inaccessible scope outranks everything. Side-channel safe: result counts
        //    are deliberately ignored so the UI never reveals whether matching evidence exists.
        if (IsRestrictive(packet) || omitted.Reason == EvidencePacketOmissionReason.Authorization)
        {
            return RecoveryStateKind.Unauthorized;
        }

        bool hasSources = packet.Sources.Count > 0 || result.ReturnedCount > 0;

        // 2. Conflicting / disputed: an answer exists but backend health disagrees with it.
        if (hasSources && evidence.Degraded)
        {
            return RecoveryStateKind.Conflicting;
        }

        // 3. No-result family: nothing was returned. Distinguish only what the contract safely allows.
        if (!hasSources)
        {
            if (result.HasIndexedMemoryUnits == false)
            {
                return RecoveryStateKind.NotIngestedYet;
            }

            if (evidence.Degraded
                || evidence.AllEnabledAxesUnavailable == true
                || omitted.Reason == EvidencePacketOmissionReason.BackendUnavailable)
            {
                return RecoveryStateKind.DegradedBackend;
            }

            if (packet.Graph.GapMarkers.Count > 0 || HasGraphAxisUnavailable(evidence))
            {
                return RecoveryStateKind.GraphGap;
            }

            if (packet.State == EvidencePacketState.Stale)
            {
                return RecoveryStateKind.StaleMemory;
            }

            if (packet.State == EvidencePacketState.Empty && result.HasIndexedMemoryUnits == true)
            {
                return RecoveryStateKind.NoMatch;
            }

            // No safe signal to distinguish wrong-case, unknown-ingestion, or insufficient data.
            return RecoveryStateKind.InsufficientEvidence;
        }

        // 4. Sources present but qualified.
        return packet.State switch
        {
            EvidencePacketState.Stale => RecoveryStateKind.StaleMemory,
            EvidencePacketState.Weak => RecoveryStateKind.Weak,
            EvidencePacketState.Degraded => RecoveryStateKind.DegradedBackend,
            EvidencePacketState.Partial => RecoveryStateKind.InsufficientEvidence,
            EvidencePacketState.PendingExpansion => RecoveryStateKind.Compressed,
            EvidencePacketState.Empty => RecoveryStateKind.NoMatch,
            EvidencePacketState.Complete => ResolveCompleteState(packet),
            _ => RecoveryStateKind.Unknown,
        };
    }

    private static RecoveryStateKind ResolveCompleteState(EvidencePacket packet)
    {
        // A complete answer is only "supported" when nothing disputes or qualifies it. Compression and
        // unavailable axes must never be smoothed into a confident answer (AC3).
        if (IsCompressed(packet.OmittedDetails.Reason))
        {
            return RecoveryStateKind.Compressed;
        }

        if (packet.Evidence.UnavailableAxes.Count > 0)
        {
            return RecoveryStateKind.Conflicting;
        }

        return packet.Evidence.EvidenceStrength switch
        {
            EvidencePacketEvidenceStrength.None or EvidencePacketEvidenceStrength.Unknown
                => RecoveryStateKind.InsufficientEvidence,
            EvidencePacketEvidenceStrength.Weak => RecoveryStateKind.Weak,
            _ => RecoveryStateKind.Supported,
        };
    }

    private static bool IsRestrictive(EvidencePacket packet)
        => EvidenceDisplay.IsRestrictiveScope(packet.Scope.IsolationStatus)
            || packet.State == EvidencePacketState.Unauthorized;

    private static bool IsCompressed(EvidencePacketOmissionReason reason)
        => reason is EvidencePacketOmissionReason.TokenBudget or EvidencePacketOmissionReason.Combined;

    private static bool HasGraphAxisUnavailable(EvidencePacketEvidence evidence)
    {
        foreach (string axis in evidence.UnavailableAxes)
        {
            if (axis.Contains("graph", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private static IReadOnlyList<RecoveryActionView> BuildActions(EvidencePacket packet, bool restrictive)
    {
        if (packet.Recovery.Count == 0)
        {
            return [];
        }

        List<RecoveryActionView> actions = new(packet.Recovery.Count);
        foreach (EvidencePacketRecoveryAction action in packet.Recovery)
        {
            // When the scope is restrictive, the only action safe to emit is checking authorization.
            // Every scope-expanding or permission-dependent action renders disabled with a reason
            // rather than being hidden or auto-executed.
            bool available = !restrictive || action.Kind == EvidencePacketRecoveryKind.CheckAuthorization;
            actions.Add(new RecoveryActionView(
                Kind: action.Kind,
                Label: EvidenceDisplay.SafeText(action.Label, "recovery action"),
                Guidance: EvidenceDisplay.SafeText(action.Guidance, "no guidance"),
                Target: EvidenceDisplay.SafeText(action.Target, "no target"),
                IsPrimary: false,
                Availability: available
                    ? RecoveryActionAvailability.Available
                    : RecoveryActionAvailability.Unavailable,
                DisabledReasonKey: available ? null : RecoveryResourceKeys.DisabledAuthRequired));
        }

        return actions;
    }

    private static (RecoveryActionView? Primary, IReadOnlyList<RecoveryActionView> Secondary) SplitPrimary(
        IReadOnlyList<RecoveryActionView> actions)
    {
        int primaryIndex = -1;
        int bestRank = int.MaxValue;
        for (int i = 0; i < actions.Count; i++)
        {
            if (actions[i].Availability != RecoveryActionAvailability.Available)
            {
                continue;
            }

            int rank = SafetyRank(actions[i].Kind);
            if (rank < bestRank)
            {
                bestRank = rank;
                primaryIndex = i;
            }
        }

        RecoveryActionView? primary = primaryIndex >= 0
            ? actions[primaryIndex] with { IsPrimary = true }
            : null;

        List<RecoveryActionView> secondary = [];
        for (int i = 0; i < actions.Count; i++)
        {
            if (i == primaryIndex)
            {
                continue;
            }

            secondary.Add(actions[i]);
        }

        return (primary, secondary);
    }

    // Lower rank = safer. Inspection-only actions outrank scope- or cost-expanding actions so the single
    // primary action is always the safest available remediation.
    private static int SafetyRank(EvidencePacketRecoveryKind kind)
        => kind switch
        {
            EvidencePacketRecoveryKind.CheckAuthorization => 0,
            EvidencePacketRecoveryKind.InspectBackendHealth => 1,
            EvidencePacketRecoveryKind.UseTraversal => 2,
            EvidencePacketRecoveryKind.FetchMemoryUnit => 3,
            EvidencePacketRecoveryKind.Retry => 4,
            EvidencePacketRecoveryKind.IncreaseMaxResults => 5,
            EvidencePacketRecoveryKind.IncreaseTokenBudget => 6,
            EvidencePacketRecoveryKind.BroadenScope => 7,
            _ => 99,
        };

    private static IReadOnlyList<RecoveryRiskMarker> BuildRiskMarkers(EvidencePacket packet, RecoveryStateKind kind)
    {
        // Suppress markers entirely when unauthorized: they could leak detail about restricted content.
        if (kind == RecoveryStateKind.Unauthorized)
        {
            return [];
        }

        List<RecoveryRiskMarker> markers = [];

        if (IsCompressed(packet.OmittedDetails.Reason) && kind != RecoveryStateKind.Compressed)
        {
            markers.Add(new RecoveryRiskMarker(
                RecoveryResourceKeys.RiskMarker("Compressed"),
                RecoverySeverity.Caution,
                "compressed"));
        }

        if (packet.State == EvidencePacketState.Stale && kind != RecoveryStateKind.StaleMemory)
        {
            markers.Add(new RecoveryRiskMarker(
                RecoveryResourceKeys.RiskMarker("Stale"),
                RecoverySeverity.Caution,
                "stale"));
        }

        if (packet.Evidence.Degraded
            && kind != RecoveryStateKind.DegradedBackend
            && kind != RecoveryStateKind.Conflicting)
        {
            markers.Add(new RecoveryRiskMarker(
                RecoveryResourceKeys.RiskMarker("Degraded"),
                RecoverySeverity.Warning,
                "degraded"));
        }

        return markers;
    }

    private static (IReadOnlyList<string> Names, IReadOnlyList<RecoveryExpansionView> Expansions) BuildOmittedDetails(
        EvidencePacket packet,
        RecoveryStateKind kind)
    {
        // Redaction/scope parity: never surface omitted detail names or expansion handles for restricted
        // scopes, where they could leak hints about content the caller may not access.
        if (kind == RecoveryStateKind.Unauthorized)
        {
            return ([], []);
        }

        EvidencePacketOmittedDetails omitted = packet.OmittedDetails;
        List<string> names = [];
        foreach (string group in omitted.DetailGroups)
        {
            names.Add(EvidenceDisplay.SafeText(group, "detail group"));
        }

        foreach (string field in omitted.FieldNames)
        {
            names.Add(EvidenceDisplay.SafeText(field, "field"));
        }

        List<RecoveryExpansionView> expansions = new(omitted.ExpansionHandles.Count);
        foreach (EvidencePacketExpansionHandle handle in omitted.ExpansionHandles)
        {
            // The opaque Handle value is deliberately not surfaced; only the sanitized target group and
            // guidance are shown so expanding stays a host-routed, contract-scoped action.
            expansions.Add(new RecoveryExpansionView(
                handle.Kind,
                EvidenceDisplay.SafeText(handle.TargetDetailGroup, "detail group"),
                EvidenceDisplay.SafeText(handle.Guidance, "no guidance")));
        }

        return (names, expansions);
    }

    private static string BuildDiagnosticClue(EvidencePacket packet, RecoveryStateKind kind)
    {
        // Whitelisted codes only: enum tokens and counts. Never raw markers, ids, payloads, or paths.
        List<string> parts = [];

        if (kind == RecoveryStateKind.Unauthorized)
        {
            // Side-channel safe: omit counts so the clue cannot signal whether restricted evidence exists.
            parts.Add($"isolation={Token(packet.Scope.IsolationStatus)}");
            parts.Add($"state={Token(packet.State)}");
        }
        else
        {
            parts.Add($"state={Token(packet.State)}");
            parts.Add($"evidence={Token(packet.Evidence.EvidenceStrength)}");

            if (packet.OmittedDetails.Reason != EvidencePacketOmissionReason.None)
            {
                parts.Add($"omission={Token(packet.OmittedDetails.Reason)}");
            }

            if (packet.OmittedDetails.OmittedCount > 0)
            {
                parts.Add(string.Create(CultureInfo.InvariantCulture, $"omittedCount={packet.OmittedDetails.OmittedCount}"));
            }

            if (packet.Evidence.UnavailableAxes.Count > 0)
            {
                parts.Add(string.Create(CultureInfo.InvariantCulture, $"unavailableAxes={packet.Evidence.UnavailableAxes.Count}"));
            }

            if (packet.Graph.GapMarkers.Count > 0)
            {
                parts.Add(string.Create(CultureInfo.InvariantCulture, $"graphGaps={packet.Graph.GapMarkers.Count}"));
            }
        }

        // Defense in depth: the assembled string is whitelisted codes, but still pass it through the
        // shared sanitizer so it can never carry unexpected sensitive content.
        return EvidenceDisplay.SafeText(string.Join("; ", parts), "diagnostic unavailable");
    }

    private static string Token(Enum value)
    {
        string raw = value.ToString();
        return raw.Length == 0
            ? raw
            : char.ToLowerInvariant(raw[0]) + raw[1..];
    }
}
