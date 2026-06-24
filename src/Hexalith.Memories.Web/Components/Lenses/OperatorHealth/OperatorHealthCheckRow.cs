// <copyright file="OperatorHealthCheckRow.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Web.Components.Lenses.OperatorHealth;

using Hexalith.Memories.Contracts.V1;
using Hexalith.Memories.Web.Components.Recovery;

/// <summary>
/// A single, sanitized Operator Health Matrix cell.
/// </summary>
/// <remarks>
/// Story 17.4 — each cell shows per-check status, affected capability, a whitelisted evidence clue, and a
/// safe next action. Tenant isolation failure, unauthorized scope, schema mismatch, and backend
/// unavailability are trust-blocking states, not decorative warnings. Evidence never contains connection
/// strings, tokens, keys, local paths, stack traces, tenant-sensitive diagnostics, provider internals, or
/// serialized packets.
/// </remarks>
/// <param name="Kind">The health check kind.</param>
/// <param name="CheckLabelKey">Localization key for the check name.</param>
/// <param name="Status">The check status.</param>
/// <param name="StatusLabelKey">Localization key for the status label.</param>
/// <param name="AffectedCapabilityKey">Localization key for the affected capability.</param>
/// <param name="SafeEvidence">Sanitized, whitelisted evidence clue, or the unavailable fallback.</param>
/// <param name="NextActionKey">Localization key for the safe next action, or null when none applies.</param>
/// <param name="NextActionAvailable">Whether the next action is safe to activate now.</param>
/// <param name="NextActionKind">The recovery kind that drives the next action, or None.</param>
/// <param name="TrustBlocking">Whether the check is a trust-blocking state.</param>
/// <param name="Severity">Severity for the cell badge and announcement politeness.</param>
public sealed record OperatorHealthCheckRow(
    OperatorCheckKind Kind,
    string CheckLabelKey,
    OperatorCheckStatus Status,
    string StatusLabelKey,
    string AffectedCapabilityKey,
    string SafeEvidence,
    string? NextActionKey,
    bool NextActionAvailable,
    EvidencePacketRecoveryKind NextActionKind,
    bool TrustBlocking,
    RecoverySeverity Severity);
