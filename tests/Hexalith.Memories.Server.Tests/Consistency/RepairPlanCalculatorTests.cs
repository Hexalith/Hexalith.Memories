// <copyright file="RepairPlanCalculatorTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Tests.Consistency;

/// <summary>
/// ATDD RED-phase seminal tests for Story 8.2 — Consistency Verification &amp; Repair.
/// Pins AC #2 (discrepancy recommendation field) and AC #7 (unrepairable flagging)
/// via the pure-function repair-plan calculator. Also guards Risk #8 (authoritative
/// source ambiguity) by asserting every <c>F</c>-syntactic combination maps to an
/// Orphan or <c>Unrepairable</c> recommendation.
/// </summary>
/// <remarks>
/// Each test is <see cref="FactAttribute.Skip"/>-gated until Story 8.2 Task 3.4
/// lands <c>Hexalith.Memories.Server.Consistency.RepairPlanCalculator</c> and
/// <c>Hexalith.Memories.Contracts.V1.ConsistencyRepairRecommendation</c>.
/// To activate: remove the <c>Skip</c> argument, uncomment the <c>using</c>
/// directives, and replace <c>Assert.Fail</c> with the assertion shown in
/// the test's blueprint block.
/// </remarks>
public class RepairPlanCalculatorTests
{
    // Blueprint — uncomment when target types exist (Task 3.4):
    //
    // using Hexalith.Memories.Contracts.V1;
    // using Hexalith.Memories.Server.Consistency;
    // using Shouldly;
    //
    // public static TheoryData<bool, bool, bool, ConsistencyRepairRecommendation> PresenceCombinations => new()
    // {
    //     { true,  true,  true,  ConsistencyRepairRecommendation.NoOp },
    //     { true,  false, true,  ConsistencyRepairRecommendation.ReIndexSemantic },
    //     { true,  true,  false, ConsistencyRepairRecommendation.ReIndexGraph },
    //     { true,  false, false, ConsistencyRepairRecommendation.ReIndexSemanticAndGraph },
    //     { false, true,  true,  ConsistencyRepairRecommendation.RemoveOrphanedSemanticAndGraph },
    //     { false, true,  false, ConsistencyRepairRecommendation.RemoveOrphanedSemantic },
    //     { false, false, true,  ConsistencyRepairRecommendation.RemoveOrphanedGraph },
    //     { false, false, false, ConsistencyRepairRecommendation.Unrepairable },
    // };

    /// <summary>
    /// ATDD RED — Story 8.2 AC #2 + AC #7 + Risk #8.
    /// Expected contract: <c>RepairPlanCalculator.Calculate(bool syntactic, bool semantic, bool graph)</c>
    /// returns the <c>ConsistencyRepairRecommendation</c> dictated by the presence matrix in the story's
    /// Section "What 8.2 adds" #7 (lines 53-63 of the story artifact). The function is pure — no DI,
    /// no ctor, just a switch expression over three booleans. Every combination with syntactic=<c>false</c>
    /// maps to an Orphan or <c>Unrepairable</c> outcome (Risk #8: syntactic hash is authoritative).
    /// </summary>
    [Theory(Skip = "ATDD RED — awaiting RepairPlanCalculator (Story 8.2 Task 3.4)")]
    [InlineData(true, true, true, "NoOp")]
    [InlineData(true, false, true, "ReIndexSemantic")]
    [InlineData(true, true, false, "ReIndexGraph")]
    [InlineData(true, false, false, "ReIndexSemanticAndGraph")]
    [InlineData(false, true, true, "RemoveOrphanedSemanticAndGraph")]
    [InlineData(false, true, false, "RemoveOrphanedSemantic")]
    [InlineData(false, false, true, "RemoveOrphanedGraph")]
    [InlineData(false, false, false, "Unrepairable")]
    public void Calculate_EveryPresenceCombination_MapsToExpectedRecommendation(
        bool syntactic,
        bool semantic,
        bool graph,
        string expectedRecommendation)
    {
        // Arrange (activate when RepairPlanCalculator exists):
        //
        // ConsistencyRepairRecommendation expected = Enum.Parse<ConsistencyRepairRecommendation>(expectedRecommendation);
        //
        // Act:
        //
        // ConsistencyRepairRecommendation actual = RepairPlanCalculator.Calculate(syntactic, semantic, graph);
        //
        // Assert:
        //
        // actual.ShouldBe(expected);

        Assert.Fail(
            $"ATDD RED (8.2-UNIT-001) — implement RepairPlanCalculator.Calculate. "
            + $"Expected Calculate({syntactic}, {semantic}, {graph}) == ConsistencyRepairRecommendation.{expectedRecommendation}.");
    }
}
