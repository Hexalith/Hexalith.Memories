// <copyright file="RecoveryStateViewModel.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Web.Components.Recovery;

/// <summary>
/// Typed, pure recovery-state projection of a canonical Evidence Packet for the recovery panel.
/// </summary>
/// <remarks>
/// Story 17.2 — produced by <see cref="RecoveryStateMapper.Map"/>. All user-facing strings are exposed
/// as localization keys (resolved by the component through <c>IStringLocalizer</c>); dynamic values are
/// pre-sanitized. The model carries no raw payloads, secrets, paths, or unsanitized contract text.
/// </remarks>
/// <param name="StateKind">The derived presentation state.</param>
/// <param name="TitleKey">Localization key for the state title.</param>
/// <param name="ExplanationKey">Localization key for the short explanation.</param>
/// <param name="DiagnosticClueLabelKey">Localization key for the diagnostic clue label.</param>
/// <param name="DiagnosticClueCode">Sanitized whitelisted diagnostic code string (enum tokens and counts only).</param>
/// <param name="Severity">Severity of the state.</param>
/// <param name="AffectedCapabilityKey">Localization key for the affected capability.</param>
/// <param name="TenantId">Sanitized tenant identifier for action context.</param>
/// <param name="CaseId">Sanitized case identifier for action context, or null for tenant scope.</param>
/// <param name="PrimaryAction">The single safest primary action, or null when the packet exposes none.</param>
/// <param name="SecondaryActions">Supporting or disabled secondary actions, in packet order.</param>
/// <param name="RiskMarkers">Secondary risk markers that decorate the primary state.</param>
/// <param name="OmittedDetailNames">
/// Sanitized names of omitted detail groups and fields, so compressed evidence is announced as omitted,
/// not absent. Empty when the scope is restrictive, preserving redaction parity.
/// </param>
/// <param name="Expansions">Sanitized expansion handles describing how to retrieve omitted detail groups.</param>
/// <param name="ContractSources">Named Evidence Packet fields that drive this state, for traceability.</param>
public sealed record RecoveryStateViewModel(
    RecoveryStateKind StateKind,
    string TitleKey,
    string ExplanationKey,
    string DiagnosticClueLabelKey,
    string DiagnosticClueCode,
    RecoverySeverity Severity,
    string AffectedCapabilityKey,
    string TenantId,
    string? CaseId,
    RecoveryActionView? PrimaryAction,
    IReadOnlyList<RecoveryActionView> SecondaryActions,
    IReadOnlyList<RecoveryRiskMarker> RiskMarkers,
    IReadOnlyList<string> OmittedDetailNames,
    IReadOnlyList<RecoveryExpansionView> Expansions,
    IReadOnlyList<string> ContractSources);
