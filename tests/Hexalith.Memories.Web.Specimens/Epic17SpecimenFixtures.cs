// <copyright file="Epic17SpecimenFixtures.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Web.Specimens;

using Hexalith.Memories.Contracts.V1;
using Hexalith.Memories.Web.Components.Filters;
using Hexalith.Memories.Web.Components.Forms;
using Hexalith.Memories.Web.Components.Interaction;

/// <summary>
/// Facade over the shared Epic 17 fixtures used by specimen routes and tests.
/// </summary>
public static class Epic17SpecimenFixtures
{
    /// <summary>Gets a representative Evidence Packet for the requested route.</summary>
    /// <param name="route">The specimen route.</param>
    /// <returns>A canonical test-only packet.</returns>
    public static EvidencePacket PacketFor(Epic17SpecimenRoute route)
    {
        ArgumentNullException.ThrowIfNull(route);

        return route.Slug switch
        {
            "recovery-action-panel" => Epic17RecoveryPacketFixtures.Compressed(),
            "case-activity-trail" => Epic17LensPacketFixtures.Degraded(),
            "ingestion-lifecycle-tracker" => Epic17LensPacketFixtures.NotIngested(),
            "operator-health-matrix" => Epic17LensPacketFixtures.Degraded(),
            "benchmark-result-comparator" => Epic17LensPacketFixtures.Empty(),
            "agent-packet-inspector" => Epic17LensPacketFixtures.Compressed(),
            "evidence-grid" => Epic17EvidencePacketFixtures.MultiSourcePacket(),
            "filter-summary" => Epic17EvidencePacketFixtures.MultiSourcePacket(),
            "lens-shell" => Epic17LensPacketFixtures.Happy(),
            _ => Epic17EvidencePacketFixtures.CompletePacket(),
        };
    }

    /// <summary>Gets a stable interaction snapshot for command, confirmation, and navigation specimens.</summary>
    /// <returns>A tenant/case-scoped snapshot with a safe return route.</returns>
    public static InteractionContextSnapshot Snapshot()
        => new(
            "tenant-a",
            "case-a",
            "find policy context",
            EvidencePacketState.Complete,
            InteractionContextSnapshot.SupportedContractVersion,
            "memories/evidence?packet=memory-a",
            InteractionTargetKind.Source,
            "memory-a",
            Filters());

    /// <summary>Gets active filters for the filter summary specimen.</summary>
    /// <returns>A bounded filter set with trust-effect metadata.</returns>
    public static IReadOnlyList<MemoriesFilter> Filters()
        =>
        [
            new(MemoriesFilterAxis.RetrievalAxis, "semantic", MemoriesFilterEffect.ExcludesAxis, true),
            new(MemoriesFilterAxis.Confidence, "strong", MemoriesFilterEffect.AffectsConfidence, true),
        ];

    /// <summary>Gets a destructive command specimen that must render confirmation copy.</summary>
    /// <returns>An export command view produced by the production mapper.</returns>
    public static MemoriesCommandView ConfirmationCommand()
        => MemoriesCommandSurfaceMapper
            .Map(Epic17EvidencePacketFixtures.CompletePacket(), Snapshot(), "tenant-a", "case-a")
            .Single(static command => command.Kind == MemoriesCommandKind.ExportPacket);

    /// <summary>Gets a dispatchable form request specimen.</summary>
    /// <returns>A shared contract-aware form request.</returns>
    public static MemoriesFormRequest FormRequest() => Epic17FormFixtures.Request();
}
