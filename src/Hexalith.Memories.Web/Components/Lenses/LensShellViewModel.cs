// <copyright file="LensShellViewModel.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Web.Components.Lenses;

using Hexalith.Memories.Contracts.V1;
using Hexalith.Memories.Web.Components.Recovery;

/// <summary>
/// The shared lens shell projection: the trust context every Story 17.4 lens keeps visible or
/// keyboard-reachable — tenant, case, active lens, active role, trust state, confidence, freshness, and a
/// return path to the originating Evidence Packet or surface.
/// </summary>
/// <remarks>
/// Story 17.4 — built by <see cref="LensShellMapper.Map"/> from the canonical Evidence Packet plus the
/// shared Story 17.2 recovery grammar. All user-facing strings are localization keys; dynamic values are
/// pre-sanitized. The same packet state, confidence, and freshness render identically across all five
/// lenses (cross-lens consistency), and confidence is suppressed under a restrictive scope so the shell
/// never leaks evidence existence past an authorization boundary.
/// </remarks>
/// <param name="Lens">The active lens.</param>
/// <param name="Role">The active role-density profile.</param>
/// <param name="LensTitleKey">Localization key for the active lens title.</param>
/// <param name="RoleLabelKey">Localization key for the active role label.</param>
/// <param name="TenantId">Sanitized tenant identifier.</param>
/// <param name="CaseId">Sanitized case identifier, or null for tenant-wide scope.</param>
/// <param name="IsolationStatus">Scope isolation status from the packet.</param>
/// <param name="StateKind">Shared recovery/state grammar kind for the packet.</param>
/// <param name="StateTitleKey">Localization key for the packet state title.</param>
/// <param name="Severity">Shared severity for the packet state.</param>
/// <param name="AffectedCapabilityKey">Localization key for the affected capability.</param>
/// <param name="ConfidenceLabel">
/// Sanitized confidence label, or the unavailable fallback when the scope is restrictive so confidence
/// never leaks past the authorization boundary.
/// </param>
/// <param name="FreshnessLabel">Sanitized freshness label (sentinel until Story 2.7 exposes a field).</param>
/// <param name="ContractVersion">The canonical contract version consumed by this slice.</param>
/// <param name="ReturnRoute">Sanitized return route to the originating packet or surface.</param>
/// <param name="Restrictive">Whether the scope is restrictive (unauthorized or unknown isolation).</param>
/// <param name="ExpandedByDefault">Whether the role density expands secondary detail by default.</param>
/// <param name="DetailLevel">The role density detail level.</param>
public sealed record LensShellViewModel(
    LensKind Lens,
    LensRole Role,
    string LensTitleKey,
    string RoleLabelKey,
    string TenantId,
    string? CaseId,
    EvidencePacketIsolationStatus IsolationStatus,
    RecoveryStateKind StateKind,
    string StateTitleKey,
    RecoverySeverity Severity,
    string AffectedCapabilityKey,
    string ConfidenceLabel,
    string FreshnessLabel,
    string ContractVersion,
    string ReturnRoute,
    bool Restrictive,
    bool ExpandedByDefault,
    LensDetailLevel DetailLevel);
