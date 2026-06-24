// <copyright file="InteractionContextValidator.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Web.Components.Interaction;

using Hexalith.Memories.Contracts.V1;
using Hexalith.Memories.Web.Components.Evidence;

/// <summary>Revalidates captured Story 17.3 interaction targets before rendering or executing actions.</summary>
public static class InteractionContextValidator
{
    /// <summary>Validates the snapshot against the current packet and active tenant/case.</summary>
    /// <param name="packet">The current Evidence Packet.</param>
    /// <param name="snapshot">The captured interaction snapshot.</param>
    /// <param name="activeTenantId">The active tenant at render/activation time.</param>
    /// <param name="activeCaseId">The active case at render/activation time.</param>
    /// <param name="allowTenantVerification">Whether tenant-verification commands are allowed in restrictive states.</param>
    /// <returns>The validation result.</returns>
    public static InteractionContextValidationResult Validate(
        EvidencePacket packet,
        InteractionContextSnapshot snapshot,
        string activeTenantId,
        string? activeCaseId,
        bool allowTenantVerification = false)
    {
        ArgumentNullException.ThrowIfNull(packet);
        ArgumentNullException.ThrowIfNull(snapshot);

        if (string.IsNullOrWhiteSpace(snapshot.TenantId) || string.IsNullOrWhiteSpace(activeTenantId))
        {
            return Invalid(InteractionContextValidationReason.MissingTenant);
        }

        if (!string.Equals(snapshot.ContractVersion, InteractionContextSnapshot.SupportedContractVersion, StringComparison.Ordinal))
        {
            return Invalid(InteractionContextValidationReason.ContractVersionMismatch);
        }

        if (!string.Equals(snapshot.TenantId, activeTenantId, StringComparison.Ordinal)
            || !string.Equals(snapshot.TenantId, packet.Scope.TenantId, StringComparison.Ordinal))
        {
            return Invalid(InteractionContextValidationReason.TenantChanged);
        }

        if (!string.Equals(snapshot.CaseId ?? string.Empty, activeCaseId ?? string.Empty, StringComparison.Ordinal)
            || !string.Equals(snapshot.CaseId ?? string.Empty, packet.Scope.CaseId ?? string.Empty, StringComparison.Ordinal))
        {
            return Invalid(InteractionContextValidationReason.CaseChanged);
        }

        if (!allowTenantVerification
            && (EvidenceDisplay.IsRestrictiveScope(packet.Scope.IsolationStatus)
                || packet.State == EvidencePacketState.Unauthorized))
        {
            return Invalid(InteractionContextValidationReason.UnauthorizedScope);
        }

        if (!TargetExists(packet, snapshot.TargetKind, snapshot.TargetId))
        {
            return Invalid(InteractionContextValidationReason.MissingTarget);
        }

        return new InteractionContextValidationResult(true, InteractionContextValidationReason.Valid, null);
    }

    private static bool TargetExists(EvidencePacket packet, InteractionTargetKind kind, string? targetId)
    {
        if (kind is InteractionTargetKind.Packet or InteractionTargetKind.OperatorCheck or InteractionTargetKind.McpPacket)
        {
            return true;
        }

        if (string.IsNullOrWhiteSpace(targetId))
        {
            return kind == InteractionTargetKind.Activity;
        }

        return kind switch
        {
            InteractionTargetKind.Source => packet.Sources.Any(source =>
                string.Equals(source.MemoryUnitId, targetId, StringComparison.Ordinal)
                || string.Equals(source.SourceUri, targetId, StringComparison.Ordinal)),
            InteractionTargetKind.Graph => packet.Graph.Available
                && (packet.Graph.RelatedPath.Contains(targetId, StringComparer.Ordinal)
                    || packet.Graph.EdgeTypes.Contains(targetId, StringComparer.Ordinal)),
            InteractionTargetKind.Activity => true,
            _ => true,
        };
    }

    private static InteractionContextValidationResult Invalid(InteractionContextValidationReason reason)
        => new(false, reason, InteractionResourceKeys.DisabledReason(reason));
}
