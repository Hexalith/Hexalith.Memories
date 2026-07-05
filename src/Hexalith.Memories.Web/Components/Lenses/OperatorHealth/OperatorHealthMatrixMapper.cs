// <copyright file="OperatorHealthMatrixMapper.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Web.Components.Lenses.OperatorHealth;

using System.Globalization;

using Hexalith.Memories.Contracts.V1;
using Hexalith.Memories.Web.Components.Evidence;
using Hexalith.Memories.Web.Components.Recovery;

/// <summary>
/// Pure, deterministic projection of a canonical Evidence Packet into the Operator Health Matrix (AC3).
/// </summary>
/// <remarks>
/// Story 17.4 — a fixed set of checks derived only from named contract fields, reusing the Story 17.2
/// recovery grammar for affected capability and severity so the same degraded or trust-blocking condition
/// reads identically across lenses. Evidence clues are whitelisted enum tokens and counts only; under a
/// restrictive scope, non-authorization checks are suppressed to unknown/unavailable so backend internals
/// never leak past the authorization boundary.
/// </remarks>
public static class OperatorHealthMatrixMapper
{
    /// <summary>Maps a packet into the Operator Health Matrix view model.</summary>
    /// <param name="packet">The canonical Evidence Packet.</param>
    /// <param name="role">The active role-density profile (ordering only).</param>
    /// <returns>The typed, sanitized health matrix.</returns>
    public static OperatorHealthViewModel Map(EvidencePacket packet, LensRole role)
    {
        ArgumentNullException.ThrowIfNull(packet);

        RecoveryStateViewModel recovery = RecoveryStateMapper.Map(packet);
        bool restrictive = EvidenceDisplay.IsRestrictiveScope(packet.Scope.IsolationStatus)
            || packet.State == EvidencePacketState.Unauthorized;
        Dictionary<EvidencePacketRecoveryKind, RecoveryActionView> actions = CollectActions(recovery);

        List<OperatorHealthCheckRow> checks =
        [
            TenantIsolation(packet, actions),
            Authorization(packet, actions, restrictive),
            RetrievalBackend(packet, actions, restrictive),
            AxisAvailability(packet, actions, restrictive),
            GraphContext(packet, actions, restrictive),
            DetailCompleteness(packet, actions, restrictive),
        ];

        // Operator and team-lead densities surface the most severe checks first; developer and agent
        // integrator densities keep the fixed declared order. Reordering never changes a check's status,
        // capability, evidence, or action — proven by role-invariance tests.
        IReadOnlyList<OperatorHealthCheckRow> ordered = role is LensRole.Operator or LensRole.TeamLead
            ? checks.OrderByDescending(c => (int)c.Severity).ToArray()
            : checks;

        DateTimeOffset? lastChecked = packet.Metadata?.Freshness?.LastCheckedAt;

        return new OperatorHealthViewModel(
            ordered,
            LastCheckedAvailable: lastChecked.HasValue,
            SafeLastChecked: EvidenceDisplay.TimestampLabel(lastChecked, "last-checked unavailable"),
            LastCheckedNoteKey: OperatorHealthResourceKeys.LastCheckedNote,
            HighestSeverity: checks.Max(c => c.Severity),
            HasTrustBlocking: checks.Any(c => c.TrustBlocking));
    }

    private static OperatorHealthCheckRow TenantIsolation(
        EvidencePacket packet,
        Dictionary<EvidencePacketRecoveryKind, RecoveryActionView> actions)
    {
        OperatorCheckStatus status = packet.Scope.IsolationStatus switch
        {
            EvidencePacketIsolationStatus.Authorized => OperatorCheckStatus.Healthy,
            EvidencePacketIsolationStatus.Unauthorized => OperatorCheckStatus.Blocked,
            EvidencePacketIsolationStatus.Unknown => OperatorCheckStatus.Blocked,
            _ => OperatorCheckStatus.Unknown,
        };
        bool blocked = status == OperatorCheckStatus.Blocked;
        return Build(
            OperatorCheckKind.TenantIsolation,
            RecoveryCapability.Access,
            status,
            Clue($"isolation={Token(packet.Scope.IsolationStatus)}"),
            blocked ? EvidencePacketRecoveryKind.CheckAuthorization : EvidencePacketRecoveryKind.None,
            actions,
            restrictive: blocked,
            trustBlocking: blocked);
    }

    private static OperatorHealthCheckRow Authorization(
        EvidencePacket packet,
        Dictionary<EvidencePacketRecoveryKind, RecoveryActionView> actions,
        bool restrictive)
    {
        bool unauthorized = restrictive
            || packet.State == EvidencePacketState.Unauthorized
            || packet.OmittedDetails.Reason == EvidencePacketOmissionReason.Authorization;
        OperatorCheckStatus status = unauthorized ? OperatorCheckStatus.Blocked : OperatorCheckStatus.Healthy;

        // Side-channel safe: only the state token is shown for a blocked scope, never omission counts.
        return Build(
            OperatorCheckKind.Authorization,
            RecoveryCapability.Access,
            status,
            Clue($"state={Token(packet.State)}"),
            unauthorized ? EvidencePacketRecoveryKind.CheckAuthorization : EvidencePacketRecoveryKind.None,
            actions,
            restrictive: unauthorized,
            trustBlocking: unauthorized);
    }

    private static OperatorHealthCheckRow RetrievalBackend(
        EvidencePacket packet,
        Dictionary<EvidencePacketRecoveryKind, RecoveryActionView> actions,
        bool restrictive)
    {
        if (restrictive)
        {
            return Suppressed(OperatorCheckKind.RetrievalBackend, RecoveryCapability.Retrieval);
        }

        bool backendUnavailable = packet.OmittedDetails.Reason == EvidencePacketOmissionReason.BackendUnavailable;
        bool allAxesDown = packet.Evidence.AllEnabledAxesUnavailable == true;
        OperatorCheckStatus status = backendUnavailable || allAxesDown
            ? OperatorCheckStatus.Blocked
            : packet.Evidence.Degraded
                ? OperatorCheckStatus.Degraded
                : OperatorCheckStatus.Healthy;

        return Build(
            OperatorCheckKind.RetrievalBackend,
            RecoveryCapability.Retrieval,
            status,
            Clue($"degraded={Bool(packet.Evidence.Degraded)}", Omission(packet.OmittedDetails.Reason)),
            status == OperatorCheckStatus.Healthy ? EvidencePacketRecoveryKind.None : EvidencePacketRecoveryKind.InspectBackendHealth,
            actions,
            restrictive: false,
            trustBlocking: status == OperatorCheckStatus.Blocked);
    }

    private static OperatorHealthCheckRow AxisAvailability(
        EvidencePacket packet,
        Dictionary<EvidencePacketRecoveryKind, RecoveryActionView> actions,
        bool restrictive)
    {
        if (restrictive)
        {
            return Suppressed(OperatorCheckKind.AxisAvailability, RecoveryCapability.EvidenceStrength);
        }

        int count = packet.Evidence.UnavailableAxes.Count;
        OperatorCheckStatus status = count > 0 ? OperatorCheckStatus.Degraded : OperatorCheckStatus.Healthy;
        return Build(
            OperatorCheckKind.AxisAvailability,
            RecoveryCapability.EvidenceStrength,
            status,
            Clue(Count("unavailableAxes", count)),
            count > 0 ? EvidencePacketRecoveryKind.InspectBackendHealth : EvidencePacketRecoveryKind.None,
            actions,
            restrictive: false,
            trustBlocking: false);
    }

    private static OperatorHealthCheckRow GraphContext(
        EvidencePacket packet,
        Dictionary<EvidencePacketRecoveryKind, RecoveryActionView> actions,
        bool restrictive)
    {
        if (restrictive)
        {
            return Suppressed(OperatorCheckKind.GraphContext, RecoveryCapability.GraphContext);
        }

        int gaps = packet.Graph.GapMarkers.Count;
        OperatorCheckStatus status = !packet.Graph.Available && gaps > 0
            ? OperatorCheckStatus.Degraded
            : gaps > 0
                ? OperatorCheckStatus.Caution
                : packet.Graph.Available
                    ? OperatorCheckStatus.Healthy
                    : OperatorCheckStatus.Caution;
        return Build(
            OperatorCheckKind.GraphContext,
            RecoveryCapability.GraphContext,
            status,
            Clue($"available={Bool(packet.Graph.Available)}", Count("graphGaps", gaps)),
            status == OperatorCheckStatus.Healthy ? EvidencePacketRecoveryKind.None : EvidencePacketRecoveryKind.UseTraversal,
            actions,
            restrictive: false,
            trustBlocking: false);
    }

    private static OperatorHealthCheckRow DetailCompleteness(
        EvidencePacket packet,
        Dictionary<EvidencePacketRecoveryKind, RecoveryActionView> actions,
        bool restrictive)
    {
        if (restrictive)
        {
            return Suppressed(OperatorCheckKind.DetailCompleteness, RecoveryCapability.DetailCompleteness);
        }

        EvidencePacketOmissionReason reason = packet.OmittedDetails.Reason;
        bool compressed = reason is EvidencePacketOmissionReason.TokenBudget
            or EvidencePacketOmissionReason.Combined
            or EvidencePacketOmissionReason.Density;
        bool redacted = reason == EvidencePacketOmissionReason.Redaction;
        OperatorCheckStatus status = compressed || redacted ? OperatorCheckStatus.Caution : OperatorCheckStatus.Healthy;
        return Build(
            OperatorCheckKind.DetailCompleteness,
            RecoveryCapability.DetailCompleteness,
            status,
            Clue(Omission(reason), packet.OmittedDetails.OmittedCount > 0 ? Count("omittedCount", packet.OmittedDetails.OmittedCount) : string.Empty),
            compressed ? EvidencePacketRecoveryKind.IncreaseTokenBudget : EvidencePacketRecoveryKind.None,
            actions,
            restrictive: false,
            trustBlocking: false);
    }

    private static OperatorHealthCheckRow Suppressed(OperatorCheckKind kind, RecoveryCapability capability)
        => new(
            kind,
            OperatorHealthResourceKeys.Check(kind),
            OperatorCheckStatus.Unknown,
            OperatorHealthResourceKeys.Status(OperatorCheckStatus.Unknown),
            RecoveryResourceKeys.Capability(capability),
            "unavailable",
            NextActionKey: null,
            NextActionAvailable: false,
            NextActionKind: EvidencePacketRecoveryKind.None,
            TrustBlocking: false,
            Severity: RecoverySeverity.Caution);

    private static OperatorHealthCheckRow Build(
        OperatorCheckKind kind,
        RecoveryCapability capability,
        OperatorCheckStatus status,
        string evidence,
        EvidencePacketRecoveryKind desiredAction,
        Dictionary<EvidencePacketRecoveryKind, RecoveryActionView> actions,
        bool restrictive,
        bool trustBlocking)
    {
        string? nextActionKey = desiredAction == EvidencePacketRecoveryKind.None
            ? null
            : RecoveryResourceKeys.Action(desiredAction);

        // The next action is only activatable when the producer sanctioned it (it is present in the packet
        // recovery list and safe). Otherwise it remains guidance text, never an action the producer did not
        // offer. CheckAuthorization stays available under a restrictive scope; everything else disables.
        bool sanctioned = actions.TryGetValue(desiredAction, out RecoveryActionView? view)
            && view.Availability == RecoveryActionAvailability.Available;
        bool available = nextActionKey is not null
            && sanctioned
            && (!restrictive || desiredAction == EvidencePacketRecoveryKind.CheckAuthorization);

        return new OperatorHealthCheckRow(
            kind,
            OperatorHealthResourceKeys.Check(kind),
            status,
            OperatorHealthResourceKeys.Status(status),
            RecoveryResourceKeys.Capability(capability),
            evidence,
            nextActionKey,
            available,
            desiredAction,
            trustBlocking,
            StatusSeverity(status));
    }

    private static Dictionary<EvidencePacketRecoveryKind, RecoveryActionView> CollectActions(RecoveryStateViewModel recovery)
    {
        Dictionary<EvidencePacketRecoveryKind, RecoveryActionView> map = [];
        if (recovery.PrimaryAction is { } primary)
        {
            map[primary.Kind] = primary;
        }

        foreach (RecoveryActionView action in recovery.SecondaryActions)
        {
            map.TryAdd(action.Kind, action);
        }

        return map;
    }

    private static RecoverySeverity StatusSeverity(OperatorCheckStatus status) => status switch
    {
        OperatorCheckStatus.Healthy => RecoverySeverity.None,
        OperatorCheckStatus.Caution => RecoverySeverity.Caution,
        OperatorCheckStatus.Degraded => RecoverySeverity.Warning,
        OperatorCheckStatus.Blocked => RecoverySeverity.Critical,
        _ => RecoverySeverity.Caution,
    };

    private static string Clue(params string[] parts)
        => EvidenceDisplay.SafeText(
            string.Join("; ", parts.Where(static p => !string.IsNullOrEmpty(p))),
            "evidence unavailable");

    private static string Omission(EvidencePacketOmissionReason reason)
        => reason == EvidencePacketOmissionReason.None ? string.Empty : $"omission={Token(reason)}";

    private static string Count(string name, int value)
        => string.Create(CultureInfo.InvariantCulture, $"{name}={value}");

    private static string Bool(bool value) => value ? "true" : "false";

    private static string Token(Enum value)
    {
        string raw = value.ToString();
        return raw.Length == 0 ? raw : char.ToLowerInvariant(raw[0]) + raw[1..];
    }
}
