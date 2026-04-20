// <copyright file="RepairPlanCalculatorTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Tests.Consistency;

using Hexalith.Memories.Contracts.V1;
using Hexalith.Memories.Server.Consistency;

using Shouldly;

/// <summary>
/// Story 8.2 — pins AC #2 (discrepancy recommendation field) and AC #7 (unrepairable
/// flagging) via the pure-function repair-plan calculator. Also guards Risk #8
/// (authoritative source ambiguity): every <c>F</c>-syntactic combination maps to an
/// Orphan or <c>Unrepairable</c> recommendation.
/// </summary>
public class RepairPlanCalculatorTests
{
    /// <summary>
    /// Story 8.2 AC #2 + AC #7 + Risk #8. Pure mapping — every one of the eight presence
    /// combinations maps to the recommendation prescribed by the story's "What 8.2 adds" #7
    /// table. Syntactic-false rows map to <c>RemoveOrphaned*</c> or <c>Unrepairable</c>.
    /// </summary>
    [Theory]
    [InlineData(true, true, true, ConsistencyRepairRecommendation.NoOp)]
    [InlineData(true, false, true, ConsistencyRepairRecommendation.ReIndexSemantic)]
    [InlineData(true, true, false, ConsistencyRepairRecommendation.ReIndexGraph)]
    [InlineData(true, false, false, ConsistencyRepairRecommendation.ReIndexSemanticAndGraph)]
    [InlineData(false, true, true, ConsistencyRepairRecommendation.RemoveOrphanedSemanticAndGraph)]
    [InlineData(false, true, false, ConsistencyRepairRecommendation.RemoveOrphanedSemantic)]
    [InlineData(false, false, true, ConsistencyRepairRecommendation.RemoveOrphanedGraph)]
    [InlineData(false, false, false, ConsistencyRepairRecommendation.Unrepairable)]
    public void Calculate_EveryPresenceCombination_MapsToExpectedRecommendation(
        bool syntactic,
        bool semantic,
        bool graph,
        ConsistencyRepairRecommendation expected)
    {
        ConsistencyRepairRecommendation actual = RepairPlanCalculator.Calculate(syntactic, semantic, graph);

        actual.ShouldBe(expected);
    }

    /// <summary>
    /// Risk #8 regression guard: every syntactic-missing row must be either an orphan removal
    /// or unrepairable. A future refactor that accidentally returns a re-index recommendation
    /// when syntactic is absent would attempt to re-derive content from embeddings — which is
    /// impossible and would produce corrupt state.
    /// </summary>
    [Theory]
    [InlineData(true, true)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(false, false)]
    public void Calculate_SyntacticMissing_DoesNotRecommendReIndex(bool semantic, bool graph)
    {
        ConsistencyRepairRecommendation actual = RepairPlanCalculator.Calculate(syntactic: false, semantic, graph);

        actual.ShouldNotBe(ConsistencyRepairRecommendation.ReIndexSemantic);
        actual.ShouldNotBe(ConsistencyRepairRecommendation.ReIndexGraph);
        actual.ShouldNotBe(ConsistencyRepairRecommendation.ReIndexSemanticAndGraph);
    }
}
