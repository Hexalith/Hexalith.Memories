// <copyright file="TraverseEndpointTokenBudgetTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Tests.Graph;

using Hexalith.Memories.Contracts.V1;
using Hexalith.Memories.Server.Graph;

using Shouldly;

/// <summary>
/// Story 10.2 Task 13.2 — Tier-2 endpoint-wiring coverage for the token-budget plumbing
/// applied between <c>/api/tenants/{tenantId}/traverse</c> and <c>GraphTraversalService</c>.
/// Exercises <see cref="TraverseResponseMetadataApplier"/> directly because that helper is
/// the in-memory surface the traverse endpoint defers to once the service has produced a
/// <see cref="TraversalResult"/>.
/// </summary>
public sealed class TraverseEndpointTokenBudgetTests
{
    [Fact]
    public void HappyPath_TokenBudget_PrunesLeavesAndPopulatesOmittedCount()
    {
        TraversalNode root = Node("root", 0, "child", "leaf-a", "leaf-b");
        TraversalNode child = Node("child", 1, "root", "deep");
        TraversalNode deep = Node("deep", 2, "child");
        TraversalNode leafA = Node("leaf-a", 1, "root");
        TraversalNode leafB = Node("leaf-b", 1, "root");
        TraversalResult source = BuildTraversalResult(
            "root",
            depth: 2,
            nodes: [root, child, deep, leafA, leafB],
            gapMarkers: [
                new TraversalGapMarker(
                    "gap-leaf-a",
                    1,
                    [new TraversalEdgeInfo(EdgeType.CausedBy, 1.0f, EdgeOrigin.Explicit, "leaf-a", "incoming")]),
            ]);

        TraversalResult applied = TraverseResponseMetadataApplier.ApplyTraversal(source, budget: 140);

        applied.Nodes
            .Select(static node => node.MemoryUnitId)
            .ShouldContain("root");
        applied.Nodes
            .Select(static node => node.MemoryUnitId)
            .ShouldContain("child");
        applied.Nodes
            .Select(static node => node.MemoryUnitId)
            .ShouldContain("deep");
        applied.OmittedCount.ShouldBeGreaterThan(0);
        applied.OmittedReason.ShouldBe(OmittedReason.TokenBudget);
        applied.PrimaryPathIntact.ShouldBeTrue();
        applied.GapMarkers.ShouldBeEmpty();
        applied.EstimatedTokensTotal.ShouldBeGreaterThan(0);
    }

    [Fact]
    public void TightBudget_PreservesPrimaryCausalPath_OverLeafBranches()
    {
        TraversalNode root = Node("root", 0, "child", "branch");
        TraversalNode child = Node("child", 1, "root", "deep");
        TraversalNode deep = Node("deep", 2, "child");
        TraversalNode branch = Node("branch", 1, "root");
        TraversalResult source = BuildTraversalResult(
            "root",
            depth: 2,
            nodes: [root, child, deep, branch],
            gapMarkers: []);

        TraversalResult applied = TraverseResponseMetadataApplier.ApplyTraversal(source, budget: 140);

        applied.Nodes
            .Select(static node => node.MemoryUnitId)
            .ShouldContain("root");
        applied.Nodes
            .Select(static node => node.MemoryUnitId)
            .ShouldContain("child");
        applied.Nodes
            .Select(static node => node.MemoryUnitId)
            .ShouldContain("deep");
        applied.Nodes
            .Select(static node => node.MemoryUnitId)
            .ShouldNotContain("branch");
        applied.PrimaryPathIntact.ShouldBeTrue();
        applied.OmittedCount.ShouldBe(1);
        applied.OmittedReason.ShouldBe(OmittedReason.TokenBudget);
    }

    [Fact]
    public void GapMarkers_ReferencingPrunedNodes_AreDropped()
    {
        TraversalNode root = Node("root", 0, "child", "leaf");
        TraversalNode child = Node("child", 1, "root");
        TraversalNode leaf = Node("leaf", 1, "root");
        TraversalGapMarker dangling = new(
            "gap-leaf",
            1,
            [new TraversalEdgeInfo(EdgeType.CausedBy, 1.0f, EdgeOrigin.Explicit, "leaf", "incoming")]);
        TraversalGapMarker retained = new(
            "gap-child",
            1,
            [new TraversalEdgeInfo(EdgeType.CausedBy, 1.0f, EdgeOrigin.Explicit, "child", "incoming")]);
        TraversalResult source = BuildTraversalResult(
            "root",
            depth: 2,
            nodes: [root, child, leaf],
            gapMarkers: [dangling, retained]);

        TraversalResult applied = TraverseResponseMetadataApplier.ApplyTraversal(source, budget: 100);

        applied.Nodes
            .Select(static node => node.MemoryUnitId)
            .ShouldNotContain("leaf");
        applied.GapMarkers
            .Select(static marker => marker.MissingNodeId)
            .ShouldBe(["gap-child"]);
    }

    [Fact]
    public void NoBudget_PreservesAllNodes_AndAllGapMarkers()
    {
        TraversalNode root = Node("root", 0, "child");
        TraversalNode child = Node("child", 1, "root", "deep");
        TraversalNode deep = Node("deep", 2, "child");
        TraversalGapMarker keepGap = new(
            "gap-deep",
            2,
            [new TraversalEdgeInfo(EdgeType.CausedBy, 1.0f, EdgeOrigin.Explicit, "deep", "incoming")]);
        TraversalResult source = BuildTraversalResult(
            "root",
            depth: 2,
            nodes: [root, child, deep],
            gapMarkers: [keepGap]);

        TraversalResult applied = TraverseResponseMetadataApplier.ApplyTraversal(source, budget: null);

        applied.Nodes.Count.ShouldBe(source.Nodes.Count);
        applied.OmittedCount.ShouldBe(0);
        applied.OmittedReason.ShouldBe(OmittedReason.None);
        applied.PrimaryPathIntact.ShouldBeTrue();
        applied.GapMarkers.ShouldBe([keepGap]);
    }

    private static TraversalNode Node(string id, int hopDistance, params string[] connectedIds)
        => new(
            id,
            new string('x', 80),
            $"file:///{id}.txt",
            SourceType.File,
            DateTimeOffset.Parse("2026-01-01T00:00:00Z"),
            hopDistance,
            connectedIds.Select(connectedId => new TraversalEdgeInfo(
                EdgeType.CausedBy,
                1.0f,
                EdgeOrigin.Explicit,
                connectedId,
                "outgoing")).ToArray());

    private static TraversalResult BuildTraversalResult(
        string startNodeId,
        int depth,
        IReadOnlyList<TraversalNode> nodes,
        IReadOnlyList<TraversalGapMarker> gapMarkers)
        => new(startNodeId, depth, nodes, nodes.Count)
        {
            GapMarkers = gapMarkers,
        };
}
