// <copyright file="IngestionUnitRow.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Web.Components.Lenses.Ingestion;

using Hexalith.Memories.Contracts.V1;
using Hexalith.Memories.Web.Components.Recovery;

/// <summary>
/// A single, sanitized Ingestion Lifecycle Tracker row for one memory unit.
/// </summary>
/// <remarks>
/// Story 17.4 — stage is always an unavailable boundary because the canonical contract exposes no stage
/// taxonomy; outcome, degradation, and recovery derive from named contract fields and the shared recovery
/// grammar. Recovery is offered only when safe; under a restrictive scope it is disabled with a reason.
/// </remarks>
/// <param name="UnitId">Sanitized memory unit identifier.</param>
/// <param name="StageAvailability">Availability of the ingestion stage (always unavailable; gap recorded).</param>
/// <param name="Outcome">The outcome at contract granularity.</param>
/// <param name="OutcomeLabelKey">Localization key for the outcome label.</param>
/// <param name="SafeFailureSummary">Sanitized, whitelisted failure clue, or the no-failure fallback.</param>
/// <param name="AffectedCapabilityKey">Localization key for the affected capability.</param>
/// <param name="RecoveryAvailable">Whether a safe recovery action is available for this unit.</param>
/// <param name="RecoveryActionKey">Localization key for the recovery action, or null when none applies.</param>
/// <param name="RecoveryKind">The recovery kind that drives the action, or None.</param>
/// <param name="Severity">Severity for the row badge and live-region politeness.</param>
public sealed record IngestionUnitRow(
    string UnitId,
    LensFieldAvailability StageAvailability,
    IngestionOutcome Outcome,
    string OutcomeLabelKey,
    string SafeFailureSummary,
    string AffectedCapabilityKey,
    bool RecoveryAvailable,
    string? RecoveryActionKey,
    EvidencePacketRecoveryKind RecoveryKind,
    RecoverySeverity Severity);
