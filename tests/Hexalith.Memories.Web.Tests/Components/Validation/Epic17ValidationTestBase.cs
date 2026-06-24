// <copyright file="Epic17ValidationTestBase.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Web.Tests.Components.Validation;

using System.Collections.Generic;
using System.Linq;

using AngleSharp.Dom;
using AngleSharp.Html.Parser;

using Bunit;

using Hexalith.FrontComposer.Testing;
using Hexalith.Memories.Contracts.V1;
using Hexalith.Memories.Web.Components.Evidence;
using Hexalith.Memories.Web.Components.Grid;
using Hexalith.Memories.Web.Components.Interaction;
using Hexalith.Memories.Web.Components.Lenses.AgentPacket;
using Hexalith.Memories.Web.Components.Lenses.Benchmark;
using Hexalith.Memories.Web.Components.Lenses.CaseActivity;
using Hexalith.Memories.Web.Components.Lenses.Ingestion;
using Hexalith.Memories.Web.Components.Lenses.OperatorHealth;
using Hexalith.Memories.Web.Components.Recovery;

/// <summary>
/// Story 17.5 — shared base for the cross-surface responsive/accessibility validation sweeps.
/// <para>
/// Every Epic 17 Memories web surface is a component-specimen rendered through bUnit and the
/// <see cref="FrontComposerTestBase"/> host. There is no runnable Memories web route, Playwright host,
/// or axe/forced-colors/screen-reader harness for these components (the RCL is host-less by design,
/// see <c>Hexalith.Memories.Web.csproj</c>), so the browser/assistive-technology dimensions are recorded
/// as deferred gaps in <see cref="Epic17ValidationInventory"/> rather than silently passed.
/// </para>
/// </summary>
public abstract class Epic17ValidationTestBase : FrontComposerTestBase
{
    /// <summary>The packet-driven Epic 17 surfaces covered by the cross-surface sweeps.</summary>
    public static readonly IReadOnlyList<string> PacketSurfaces =
    [
        "EvidenceCockpit",
        "TrustStrip",
        "RecoveryActionPanel",
        "EvidenceGrid",
        "CommandSurface",
        "ContextNavigation",
        "CaseActivityTrail",
        "IngestionLifecycleTracker",
        "OperatorHealthMatrix",
        "BenchmarkResultComparator",
        "AgentPacketInspector",
    ];

    /// <summary>Initializes a new instance of the <see cref="Epic17ValidationTestBase"/> class.</summary>
    protected Epic17ValidationTestBase() => Host.ValidateVersionAlignment();

    /// <summary>xUnit member-data source over <see cref="PacketSurfaces"/>.</summary>
    public static IEnumerable<object[]> PacketSurfaceNames()
        => PacketSurfaces.Select(static s => new object[] { s });

    /// <summary>
    /// Renders one packet-driven surface with its established parameter contract and returns the rendered
    /// markup. The cockpit composes the scope header, trust strip, source citation stack, retrieval axis
    /// breakdown, graph path summary, and recovery panel, so those sub-surfaces are covered transitively.
    /// </summary>
    protected string RenderSurface(string surface, EvidencePacket packet) => surface switch
        {
            "EvidenceCockpit" => Render<MemoriesEvidenceCockpit>(p => p
                .Add(c => c.Packet, packet)).Markup,
            "TrustStrip" => Render<MemoriesTrustStrip>(p => p
                .Add(c => c.Packet, packet)
                .Add(c => c.Mode, MemoriesTrustStrip.TrustStripMode.Packet)).Markup,
            "RecoveryActionPanel" => Render<MemoriesRecoveryActionPanel>(p => p
                .Add(c => c.Packet, packet)).Markup,
            "EvidenceGrid" => Render<MemoriesEvidenceGrid>(p => p
                .Add(c => c.Packet, packet)).Markup,
            "CommandSurface" => Render<MemoriesCommandSurface>(p => p
                .Add(c => c.Packet, packet)
                .Add(c => c.Snapshot, SnapshotFor(packet))
                .Add(c => c.ActiveTenantId, packet.Scope.TenantId)
                .Add(c => c.ActiveCaseId, packet.Scope.CaseId)).Markup,
            "ContextNavigation" => Render<MemoriesContextNavigation>(p => p
                .Add(c => c.Packet, packet)
                .Add(c => c.Snapshot, SnapshotFor(packet))
                .Add(c => c.ActiveTenantId, packet.Scope.TenantId)
                .Add(c => c.ActiveCaseId, packet.Scope.CaseId)).Markup,
            "CaseActivityTrail" => Render<MemoriesCaseActivityTrail>(p => p
                .Add(c => c.Packet, packet)
                .Add(c => c.ReturnRoute, "memories/evidence?packet=memory-a")).Markup,
            "IngestionLifecycleTracker" => Render<MemoriesIngestionLifecycleTracker>(p => p
                .Add(c => c.Packet, packet)).Markup,
            "OperatorHealthMatrix" => Render<MemoriesOperatorHealthMatrix>(p => p
                .Add(c => c.Packet, packet)).Markup,
            "BenchmarkResultComparator" => Render<MemoriesBenchmarkResultComparator>(p => p
                .Add(c => c.Packet, packet)).Markup,
            "AgentPacketInspector" => Render<MemoriesAgentPacketInspector>(p => p
                .Add(c => c.Packet, packet)).Markup,
            _ => throw new System.InvalidOperationException($"Unknown Epic 17 surface '{surface}'."),
        };

    /// <summary>Parses rendered markup and returns every element matching a CSS selector.</summary>
    protected static IReadOnlyList<IElement> QueryAll(string markup, string selector)
    {
        IDocument document = new HtmlParser().ParseDocument($"<body>{markup}</body>");
        return document.QuerySelectorAll(selector).ToList();
    }

    /// <summary>Builds an interaction snapshot whose scope tracks the packet, keeping command/navigation valid.</summary>
    protected static InteractionContextSnapshot SnapshotFor(EvidencePacket packet)
        => new(
            packet.Scope.TenantId,
            packet.Scope.CaseId,
            packet.Result.Query,
            packet.State,
            InteractionContextSnapshot.SupportedContractVersion,
            "memories/evidence?packet=memory-a",
            InteractionTargetKind.Packet,
            null,
            []);
}
