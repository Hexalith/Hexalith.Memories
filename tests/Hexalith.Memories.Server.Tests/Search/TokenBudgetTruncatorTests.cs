// <copyright file="TokenBudgetTruncatorTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Tests.Search;

using Hexalith.Memories.Contracts.V1;
using Hexalith.Memories.Server.Search;

using Shouldly;

public class TokenBudgetTruncatorTests
{
    [Fact]
    public void EstimateTokensForSnippet_ShouldUseCeilingCharQuarterPlusOverhead()
    {
        TokenBudgetTruncator.EstimateTokensForSnippet("12345", overhead: 3).ShouldBe(5);
    }

    [Fact]
    public void TruncateByRank_WhenBudgetIsNull_ShouldKeepAllResults()
    {
        string[] ranked = ["a", "b", "c"];

        var result = TokenBudgetTruncator.TruncateByRank(ranked, null, _ => 10);

        result.Kept.ShouldBe(ranked);
        result.Omitted.ShouldBe(0);
        result.EstimatedTokensTotal.ShouldBe(30);
        result.OmittedReason.ShouldBe(OmittedReason.None);
    }

    [Fact]
    public void TruncateByRank_WhenBudgetIsTight_ShouldKeepRankedPrefixAndReportOmissions()
    {
        string[] ranked = ["first", "second", "third"];

        var result = TokenBudgetTruncator.TruncateByRank(ranked, 25, _ => 10);

        result.Kept.ShouldBe(["first", "second"]);
        result.Omitted.ShouldBe(1);
        result.EstimatedTokensTotal.ShouldBe(30);
        result.OmittedReason.ShouldBe(OmittedReason.TokenBudget);
    }

    [Fact]
    public void TruncateByRank_WhenEstimatorReturnsNegative_ShouldTreatEstimateAsZero()
    {
        string[] ranked = ["first", "second"];

        var result = TokenBudgetTruncator.TruncateByRank(ranked, 1, _ => -10);

        result.Kept.ShouldBe(ranked);
        result.Omitted.ShouldBe(0);
        result.EstimatedTokensTotal.ShouldBe(0);
    }

    [Fact]
    public void TruncateTraversal_WhenBudgetIsTight_ShouldPruneLeafBranchBeforePrimaryPath()
    {
        TraversalNode root = Node("root", 0, "child", "branch");
        TraversalNode child = Node("child", 1, "root", "deep");
        TraversalNode deep = Node("deep", 2, "child");
        TraversalNode branch = Node("branch", 1, "root");

        var result = TokenBudgetTruncator.TruncateTraversal(
            [root, child, deep, branch],
            30,
            _ => 10);

        result.Kept.Select(static node => node.MemoryUnitId).ShouldBe(["root", "child", "deep"]);
        result.Omitted.ShouldBe(1);
        result.PrimaryPathIntact.ShouldBeTrue();
        result.OmittedReason.ShouldBe(OmittedReason.TokenBudget);
    }

    [Fact]
    public void TruncateTraversal_WhenBudgetIsBelowPrimaryPath_ShouldReportPrimaryPathBroken()
    {
        TraversalNode root = Node("root", 0, "child");
        TraversalNode child = Node("child", 1, "root", "deep");
        TraversalNode deep = Node("deep", 2, "child");

        var result = TokenBudgetTruncator.TruncateTraversal(
            [root, child, deep],
            15,
            _ => 10);

        result.Kept.Select(static node => node.MemoryUnitId).ShouldBe(["root"]);
        result.Omitted.ShouldBe(2);
        result.PrimaryPathIntact.ShouldBeFalse();
    }

    private static TraversalNode Node(string id, int hopDistance, params string[] connectedIds)
        => new(
            id,
            $"content for {id}",
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
}
