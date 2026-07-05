// <copyright file="LensShellMapper.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Web.Components.Lenses;

using Hexalith.Memories.Contracts.V1;
using Hexalith.Memories.Web.Components.Evidence;
using Hexalith.Memories.Web.Components.Interaction;
using Hexalith.Memories.Web.Components.Recovery;

/// <summary>
/// Pure, deterministic mapping from a canonical Evidence Packet to the shared <see cref="LensShellViewModel"/>.
/// </summary>
/// <remarks>
/// Story 17.4 — the shell reuses the Story 17.2 recovery state grammar for trust state, severity, and
/// affected capability so all five lenses stay consistent. It never mutates the packet, executes work, or
/// infers values from anything other than named contract fields and the shared mappers.
/// </remarks>
public static class LensShellMapper
{
    /// <summary>Builds the shared lens shell for a packet, lens, and role.</summary>
    /// <param name="packet">The canonical Evidence Packet.</param>
    /// <param name="lens">The active lens.</param>
    /// <param name="role">The active role-density profile.</param>
    /// <param name="returnRoute">A safe internal route back to the originating packet or surface.</param>
    /// <returns>The shared, sanitized lens shell projection.</returns>
    public static LensShellViewModel Map(
        EvidencePacket packet,
        LensKind lens,
        LensRole role,
        string? returnRoute)
    {
        ArgumentNullException.ThrowIfNull(packet);

        RecoveryStateViewModel recovery = RecoveryStateMapper.Map(packet);
        bool restrictive = EvidenceDisplay.IsRestrictiveScope(packet.Scope.IsolationStatus)
            || packet.State == EvidencePacketState.Unauthorized;
        LensRoleDensityProfile density = LensRoleDensity.For(role);

        // Confidence is a trust signal: suppress it under a restrictive scope so the shell never reveals
        // evidence strength (and therefore evidence existence) past an authorization boundary.
        string confidence = restrictive
            ? LensResourceKeys.ConfidenceUnavailableText
            : EvidenceDisplay.Label(packet.Evidence.EvidenceStrength);

        return new LensShellViewModel(
            Lens: lens,
            Role: role,
            LensTitleKey: LensResourceKeys.LensTitle(lens),
            RoleLabelKey: LensResourceKeys.Role(role),
            TenantId: EvidenceDisplay.SafeText(packet.Scope.TenantId, "unknown tenant"),
            CaseId: string.IsNullOrWhiteSpace(packet.Scope.CaseId)
                ? null
                : EvidenceDisplay.SafeText(packet.Scope.CaseId, "tenant scope"),
            IsolationStatus: packet.Scope.IsolationStatus,
            StateKind: recovery.StateKind,
            StateTitleKey: recovery.TitleKey,
            Severity: recovery.Severity,
            AffectedCapabilityKey: recovery.AffectedCapabilityKey,
            ConfidenceLabel: confidence,
            FreshnessLabel: EvidenceDisplay.FreshnessLabel(packet),
            ContractVersion: InteractionContextSnapshot.SupportedContractVersion,
            ReturnRoute: EvidenceDisplay.SafeText(returnRoute, "return unavailable"),
            Restrictive: restrictive,
            ExpandedByDefault: density.ExpandedByDefault,
            DetailLevel: density.DetailLevel);
    }
}
