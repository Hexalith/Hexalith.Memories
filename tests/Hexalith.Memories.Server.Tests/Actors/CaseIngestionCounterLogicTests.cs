// <copyright file="CaseIngestionCounterLogicTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Tests.Actors;

using System.Text.Json;

using Hexalith.Memories.Contracts.V1;
using Hexalith.Memories.Server.Actors;

using Shouldly;

public class CaseIngestionCounterLogicTests
{
    private static CaseIngestionCounterState Empty() => new(0, 0, 0, 0, null);

    [Fact]
    public void Transition_NoneToQueued_IncrementsQueued()
    {
        CaseIngestionCounterLogic logic = new();
        CaseIngestionCounterState next = logic.Transition(Empty(), "none", "queued", "t1");

        next.Queued.ShouldBe(1);
        next.LastTransitionId.ShouldBe("t1");
    }

    [Fact]
    public void Transition_QueuedToExtracting_MovesBucket()
    {
        CaseIngestionCounterLogic logic = new();
        CaseIngestionCounterState start = new(2, 0, 0, 0, "t0");
        CaseIngestionCounterState next = logic.Transition(start, "queued", "extracting", "t1");

        next.Queued.ShouldBe(1);
        next.Extracting.ShouldBe(1);
    }

    [Theory]
    [InlineData("queued", "extracting")]
    [InlineData("extracting", "embedding")]
    [InlineData("embedding", "indexing")]
    [InlineData("indexing", "none")]
    public void Transition_AllPipelineMoves_ProduceExpectedBuckets(string previous, string next)
    {
        CaseIngestionCounterLogic logic = new();
        CaseIngestionCounterState start = new(1, 1, 1, 1, "t0");
        CaseIngestionCounterState result = logic.Transition(start, previous, next, "t1");

        result.LastTransitionId.ShouldBe("t1");
    }

    [Fact]
    public void Transition_DuplicateTransitionId_IsIdempotent_ReturnsSameInstance()
    {
        CaseIngestionCounterLogic logic = new();
        CaseIngestionCounterState start = new(3, 0, 0, 0, "applied");
        CaseIngestionCounterState result = logic.Transition(start, "queued", "extracting", "applied");

        ReferenceEquals(start, result).ShouldBeTrue();
        result.Queued.ShouldBe(3);
        result.Extracting.ShouldBe(0);
    }

    [Fact]
    public void Transition_NonAdjacentReplayForSameWorkflow_IsIdempotent()
    {
        CaseIngestionCounterLogic logic = new();
        CaseIngestionCounterState state = logic.Transition(Empty(), "none", "queued", "workflow-a:1");
        state = logic.Transition(state, "queued", "extracting", "workflow-a:2");

        CaseIngestionCounterState replayed = logic.Transition(state, "none", "queued", "workflow-a:1");

        ReferenceEquals(state, replayed).ShouldBeTrue();
        replayed.Queued.ShouldBe(0);
        replayed.Extracting.ShouldBe(1);
    }

    [Fact]
    public void Transition_InterleavedWorkflowReplay_IsIdempotent()
    {
        CaseIngestionCounterLogic logic = new();
        CaseIngestionCounterState state = logic.Transition(Empty(), "none", "queued", "workflow-a:1");
        state = logic.Transition(state, "none", "queued", "workflow-b:1");

        CaseIngestionCounterState replayed = logic.Transition(state, "none", "queued", "workflow-a:1");

        ReferenceEquals(state, replayed).ShouldBeTrue();
        replayed.Queued.ShouldBe(2);
    }

    [Fact]
    public void Transition_LegacyLastTransitionSeedsReplayWatermark()
    {
        CaseIngestionCounterLogic logic = new();
        CaseIngestionCounterState legacy = new(0, 1, 0, 0, "workflow-a:3");

        CaseIngestionCounterState replayed = logic.Transition(legacy, "none", "queued", "workflow-a:2");

        ReferenceEquals(legacy, replayed).ShouldBeTrue();
        replayed.Queued.ShouldBe(0);
        replayed.Extracting.ShouldBe(1);
    }

    [Fact]
    public void Transition_LegacySerializedStateSeedsReplayWatermark()
    {
        const string legacyJson =
            """{"queued":0,"extracting":1,"embedding":0,"indexing":0,"lastTransitionId":"workflow-a:3"}""";
        CaseIngestionCounterState legacy = JsonSerializer
            .Deserialize<CaseIngestionCounterState>(legacyJson, JsonSerializerOptions.Web)
            .ShouldNotBeNull();
        CaseIngestionCounterLogic logic = new();

        CaseIngestionCounterState replayed = logic.Transition(legacy, "none", "queued", "workflow-a:2");

        ReferenceEquals(legacy, replayed).ShouldBeTrue();
        replayed.Queued.ShouldBe(0);
        replayed.Extracting.ShouldBe(1);
        replayed.AppliedTransitionSequences.ShouldBeNull();
        replayed.AppliedTransitionWorkflowOrder.ShouldBeNull();
    }

    [Fact]
    public void Transition_SerializedReplayWatermarkPreservesIdempotencyAndOrder()
    {
        CaseIngestionCounterLogic logic = new();
        CaseIngestionCounterState state = logic.Transition(Empty(), "none", "queued", "workflow-a:1");
        state = logic.Transition(state, "queued", "extracting", "workflow-a:2");
        state = logic.Transition(state, "none", "queued", "workflow-b:1");
        string json = JsonSerializer.Serialize(state, JsonSerializerOptions.Web);
        CaseIngestionCounterState restored = JsonSerializer
            .Deserialize<CaseIngestionCounterState>(json, JsonSerializerOptions.Web)
            .ShouldNotBeNull();

        CaseIngestionCounterState replayed = logic.Transition(restored, "none", "queued", "workflow-a:1");

        ReferenceEquals(restored, replayed).ShouldBeTrue();
        restored.AppliedTransitionSequences.ShouldBe(state.AppliedTransitionSequences);
        restored.AppliedTransitionWorkflowOrder.ShouldBe(state.AppliedTransitionWorkflowOrder);
        replayed.Queued.ShouldBe(1);
        replayed.Extracting.ShouldBe(1);
    }

    [Fact]
    public void Transition_LedgerIsBoundedAcrossWorkflows()
    {
        CaseIngestionCounterLogic logic = new();
        CaseIngestionCounterState state = Empty();
        for (int index = 1; index <= 300; index++)
        {
            state = logic.Transition(state, "none", "queued", $"workflow-{index}:1");
        }

        state.AppliedTransitionSequences.ShouldNotBeNull();
        state.AppliedTransitionSequences.Count.ShouldBe(256);
        state.AppliedTransitionSequences.ShouldContainKey("workflow-300");
        state.AppliedTransitionWorkflowOrder.ShouldNotBeNull();
        state.AppliedTransitionWorkflowOrder.Length.ShouldBe(256);
        state.AppliedTransitionWorkflowOrder[^1].ShouldBe("workflow-300");
    }

    [Fact]
    public void Transition_RefreshedWorkflowAtLedgerLimitSurvivesEviction()
    {
        CaseIngestionCounterLogic logic = new();
        CaseIngestionCounterState state = Empty();
        for (int index = 1; index <= 256; index++)
        {
            state = logic.Transition(state, "none", "queued", $"workflow-{index}:1");
        }

        state = logic.Transition(state, "queued", "extracting", "workflow-1:2");
        state = logic.Transition(state, "none", "queued", "workflow-257:1");

        state.AppliedTransitionSequences.ShouldNotBeNull();
        state.AppliedTransitionSequences.ShouldContainKey("workflow-1");
        state.AppliedTransitionSequences.ShouldNotContainKey("workflow-2");
        state.AppliedTransitionWorkflowOrder.ShouldNotBeNull();
        state.AppliedTransitionWorkflowOrder[0].ShouldBe("workflow-3");
        state.AppliedTransitionWorkflowOrder[^2].ShouldBe("workflow-1");
        state.AppliedTransitionWorkflowOrder[^1].ShouldBe("workflow-257");

        CaseIngestionCounterState replayed = logic.Transition(state, "none", "queued", "workflow-1:1");

        ReferenceEquals(state, replayed).ShouldBeTrue();
        replayed.Queued.ShouldBe(256);
        replayed.Extracting.ShouldBe(1);
    }

    [Fact]
    public void Transition_DecrementFromZero_StaysAtZero()
    {
        CaseIngestionCounterLogic logic = new();
        CaseIngestionCounterState result = logic.Transition(Empty(), "queued", "extracting", "t1");

        result.Queued.ShouldBe(0);
        result.Extracting.ShouldBe(1);
    }

    [Fact]
    public void Transition_UnknownPreviousStage_Throws()
    {
        CaseIngestionCounterLogic logic = new();
        Should.Throw<ArgumentException>(() => logic.Transition(Empty(), "unknown", "queued", "t1"));
    }

    [Fact]
    public void Transition_UnknownNextStage_Throws()
    {
        CaseIngestionCounterLogic logic = new();
        Should.Throw<ArgumentException>(() => logic.Transition(Empty(), "queued", "bogus", "t1"));
    }

    [Fact]
    public void ToCounts_ProjectsAllFourBuckets()
    {
        CaseIngestionCounterLogic logic = new();
        CaseIngestionCounts counts = logic.ToCounts(new(1, 2, 3, 4, "t1"));

        counts.ShouldBe(new CaseIngestionCounts(1, 2, 3, 4));
    }

    [Fact]
    public void Transition_FullPipelineSequence_LandsAtEmpty()
    {
        CaseIngestionCounterLogic logic = new();
        CaseIngestionCounterState s = Empty();
        s = logic.Transition(s, "none", "queued", "1");
        s = logic.Transition(s, "queued", "extracting", "2");
        s = logic.Transition(s, "extracting", "embedding", "3");
        s = logic.Transition(s, "embedding", "indexing", "4");
        s = logic.Transition(s, "indexing", "none", "5");

        s.Queued.ShouldBe(0);
        s.Extracting.ShouldBe(0);
        s.Embedding.ShouldBe(0);
        s.Indexing.ShouldBe(0);
    }
}
