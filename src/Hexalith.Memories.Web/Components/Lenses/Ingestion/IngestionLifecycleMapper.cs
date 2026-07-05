// <copyright file="IngestionLifecycleMapper.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Web.Components.Lenses.Ingestion;

using Hexalith.Memories.Contracts.V1;
using Hexalith.Memories.Web.Components.Evidence;
using Hexalith.Memories.Web.Components.Recovery;

/// <summary>
/// Pure, deterministic projection of a canonical Evidence Packet into the Ingestion Lifecycle Tracker (AC2).
/// </summary>
/// <remarks>
/// Story 17.4 — the tracker renders optional Story 2.7 ingestion stage taxonomy when the canonical packet
/// supplies it, and otherwise keeps the stage unavailable boundary. Recovery is offered only through the
/// shared recovery grammar and only when safe. Under a restrictive scope, unit detail is suppressed and a single
/// authorization row remains.
/// </remarks>
public static class IngestionLifecycleMapper
{
    /// <summary>Maps a packet into the Ingestion Lifecycle Tracker view model.</summary>
    /// <param name="packet">The canonical Evidence Packet.</param>
    /// <param name="role">The active role-density profile (ordering only).</param>
    /// <returns>The typed, sanitized ingestion tracker.</returns>
    public static IngestionLifecycleViewModel Map(EvidencePacket packet, LensRole role)
    {
        ArgumentNullException.ThrowIfNull(packet);

        RecoveryStateViewModel recovery = RecoveryStateMapper.Map(packet);
        bool restrictive = EvidenceDisplay.IsRestrictiveScope(packet.Scope.IsolationStatus)
            || packet.State == EvidencePacketState.Unauthorized;

        if (restrictive)
        {
            IngestionUnitRow unauthorized = new(
                UnitId: "unit unavailable",
                StageAvailability: LensFieldAvailability.Unauthorized,
                SafeStage: "stage unavailable",
                Outcome: IngestionOutcome.Unauthorized,
                OutcomeLabelKey: IngestionLifecycleResourceKeys.Outcome(IngestionOutcome.Unauthorized),
                SafeFailureSummary: recovery.DiagnosticClueCode,
                AffectedCapabilityKey: recovery.AffectedCapabilityKey,
                RecoveryAvailable: recovery.PrimaryAction is { Availability: RecoveryActionAvailability.Available },
                RecoveryActionKey: recovery.PrimaryAction is { } ua ? RecoveryResourceKeys.Action(ua.Kind) : null,
                RecoveryKind: recovery.PrimaryAction?.Kind ?? EvidencePacketRecoveryKind.None,
                Severity: recovery.Severity);

            return new IngestionLifecycleViewModel(
                [unauthorized],
                StageTaxonomyAvailable: false,
                StageNoteKey: IngestionLifecycleResourceKeys.StageNote,
                HighestSeverity: recovery.Severity,
                IsEmpty: false,
                EmptyReasonKey: IngestionLifecycleResourceKeys.Empty);
        }

        bool backendUnavailable = packet.OmittedDetails.Reason == EvidencePacketOmissionReason.BackendUnavailable;
        bool degraded = packet.Evidence.Degraded || packet.Evidence.AllEnabledAxesUnavailable == true;

        List<IngestionUnitRow> units = [];
        foreach (EvidencePacketSource source in packet.Sources)
        {
            IngestionOutcome outcome = backendUnavailable
                ? IngestionOutcome.BackendUnavailable
                : degraded
                    ? IngestionOutcome.Degraded
                    : packet.Result.HasIndexedMemoryUnits == true
                        ? IngestionOutcome.Indexed
                        : IngestionOutcome.Unknown;
            units.Add(BuildUnit(EvidenceDisplay.SafeText(source.MemoryUnitId, "memory unit unavailable"), outcome, recovery, source.Ingestion));
        }

        if (units.Count == 0)
        {
            IngestionOutcome aggregate = packet.Result.HasIndexedMemoryUnits == false
                ? IngestionOutcome.NotIngestedYet
                : backendUnavailable
                    ? IngestionOutcome.BackendUnavailable
                    : degraded
                        ? IngestionOutcome.Degraded
                        : IngestionOutcome.Unknown;

            // Only surface a tenant-wide aggregate row when the outcome is a meaningful ingestion signal;
            // a search that simply matched nothing leaves the tracker empty rather than inventing a unit.
            if (aggregate is IngestionOutcome.NotIngestedYet or IngestionOutcome.BackendUnavailable or IngestionOutcome.Degraded)
            {
                units.Add(BuildUnit("tenant scope", aggregate, recovery, ingestion: null));
            }
        }

        // Operator density leads with the most severe units; other roles keep deterministic source order.
        IReadOnlyList<IngestionUnitRow> ordered = role == LensRole.Operator
            ? units.OrderByDescending(u => (int)u.Severity).ToArray()
            : units;

        RecoverySeverity highest = units.Count == 0
            ? RecoverySeverity.None
            : units.Max(u => u.Severity);

        return new IngestionLifecycleViewModel(
            ordered,
            StageTaxonomyAvailable: units.Any(static unit => unit.StageAvailability == LensFieldAvailability.Available),
            StageNoteKey: IngestionLifecycleResourceKeys.StageNote,
            HighestSeverity: highest,
            IsEmpty: units.Count == 0,
            EmptyReasonKey: IngestionLifecycleResourceKeys.Empty);
    }

    private static IngestionUnitRow BuildUnit(
        string unitId,
        IngestionOutcome outcome,
        RecoveryStateViewModel recovery,
        EvidencePacketIngestionMetadata? ingestion)
    {
        bool recoverable = outcome is IngestionOutcome.Degraded
            or IngestionOutcome.BackendUnavailable
            or IngestionOutcome.NotIngestedYet;
        RecoveryActionView? action = recoverable ? recovery.PrimaryAction : null;

        // Failure summary uses the shared, whitelisted diagnostic clue only; never raw payloads or paths.
        string failure = outcome is IngestionOutcome.Indexed or IngestionOutcome.Unknown
            ? string.Empty
            : recovery.DiagnosticClueCode;

        return new IngestionUnitRow(
            UnitId: unitId,
            StageAvailability: ingestion is null ? LensFieldAvailability.Unavailable : LensFieldAvailability.Available,
            SafeStage: ingestion is null
                ? "stage unavailable"
                : EvidenceDisplay.SafeText(EvidenceDisplay.Label(ingestion.Stage), "stage unavailable"),
            Outcome: outcome,
            OutcomeLabelKey: IngestionLifecycleResourceKeys.Outcome(outcome),
            SafeFailureSummary: failure,
            AffectedCapabilityKey: recovery.AffectedCapabilityKey,
            RecoveryAvailable: action is { Availability: RecoveryActionAvailability.Available },
            RecoveryActionKey: action is { } a ? RecoveryResourceKeys.Action(a.Kind) : null,
            RecoveryKind: action?.Kind ?? EvidencePacketRecoveryKind.None,
            Severity: Severity(outcome));
    }

    private static RecoverySeverity Severity(IngestionOutcome outcome) => outcome switch
    {
        IngestionOutcome.Indexed => RecoverySeverity.Info,
        IngestionOutcome.NotIngestedYet => RecoverySeverity.Warning,
        IngestionOutcome.Degraded => RecoverySeverity.Warning,
        IngestionOutcome.BackendUnavailable => RecoverySeverity.Warning,
        IngestionOutcome.Unauthorized => RecoverySeverity.Critical,
        _ => RecoverySeverity.Caution,
    };
}
