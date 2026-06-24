// <copyright file="MemoriesCommandSurfaceMapper.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Web.Components.Interaction;

using Hexalith.Memories.Contracts.V1;
using Hexalith.Memories.Web.Components.Evidence;

/// <summary>Builds safe, scope-revalidated command rows from an Evidence Packet.</summary>
public static class MemoriesCommandSurfaceMapper
{
    /// <summary>Builds command rows for the current packet and snapshot.</summary>
    /// <param name="packet">The current Evidence Packet.</param>
    /// <param name="snapshot">The captured interaction snapshot.</param>
    /// <param name="activeTenantId">The active tenant at render/activation time.</param>
    /// <param name="activeCaseId">The active case at render/activation time.</param>
    /// <returns>Command rows in stable command-palette order.</returns>
    public static IReadOnlyList<MemoriesCommandView> Map(
        EvidencePacket packet,
        InteractionContextSnapshot snapshot,
        string activeTenantId,
        string? activeCaseId)
    {
        ArgumentNullException.ThrowIfNull(packet);
        ArgumentNullException.ThrowIfNull(snapshot);

        InteractionContextValidationResult normal = InteractionContextValidator.Validate(
            packet,
            snapshot,
            activeTenantId,
            activeCaseId);
        InteractionContextValidationResult tenantCheck = InteractionContextValidator.Validate(
            packet,
            snapshot with { TargetKind = InteractionTargetKind.Packet, TargetId = null },
            activeTenantId,
            activeCaseId,
            allowTenantVerification: true);

        string firstSource = packet.Sources.FirstOrDefault()?.MemoryUnitId
            ?? packet.Sources.FirstOrDefault()?.SourceUri
            ?? snapshot.TargetId
            ?? "packet";
        string graphTarget = packet.Graph.RelatedPath.FirstOrDefault()
            ?? packet.Graph.EdgeTypes.FirstOrDefault()
            ?? "graph";

        return
        [
            Command(MemoriesCommandKind.Search, snapshot.Query, normal),
            Command(MemoriesCommandKind.Ingest, packet.Scope.TenantId, normal),
            Command(MemoriesCommandKind.InspectSource, firstSource, ValidateTarget(packet, snapshot, activeTenantId, activeCaseId, InteractionTargetKind.Source, firstSource)),
            Command(MemoriesCommandKind.VerifyTenant, packet.Scope.TenantId, tenantCheck),
            Command(MemoriesCommandKind.OpenGraph, graphTarget, ValidateTarget(packet, snapshot, activeTenantId, activeCaseId, InteractionTargetKind.Graph, graphTarget)),
            Command(MemoriesCommandKind.RetryIngestion, packet.Scope.TenantId, normal, requiresConfirmation: true),
            Command(MemoriesCommandKind.ExportPacket, "evidence-packet", normal, requiresConfirmation: true),
            Command(MemoriesCommandKind.InspectMcpPayload, "mcp-payload", normal),
        ];
    }

    private static InteractionContextValidationResult ValidateTarget(
        EvidencePacket packet,
        InteractionContextSnapshot snapshot,
        string activeTenantId,
        string? activeCaseId,
        InteractionTargetKind kind,
        string target)
        => InteractionContextValidator.Validate(
            packet,
            snapshot with { TargetKind = kind, TargetId = target },
            activeTenantId,
            activeCaseId);

    private static MemoriesCommandView Command(
        MemoriesCommandKind kind,
        string target,
        InteractionContextValidationResult validation,
        bool requiresConfirmation = false)
    {
        string safeTarget = InteractionDisplay.SafeText(target, "packet");
        bool available = validation.IsValid;
        if (kind == MemoriesCommandKind.OpenGraph && string.Equals(safeTarget, "graph", StringComparison.Ordinal))
        {
            available = false;
            validation = validation with
            {
                DisabledReasonKey = InteractionResourceKeys.DisabledReason(InteractionContextValidationReason.MissingTarget),
                Reason = InteractionContextValidationReason.MissingTarget,
            };
        }

        return new MemoriesCommandView(
            kind,
            InteractionResourceKeys.Command(kind),
            safeTarget,
            available,
            available ? null : validation.DisabledReasonKey,
            requiresConfirmation,
            InteractionResourceKeys.CommandConsequence(kind),
            InteractionResourceKeys.CommandRecovery(kind));
    }
}
