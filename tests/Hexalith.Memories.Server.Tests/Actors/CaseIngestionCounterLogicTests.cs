// <copyright file="CaseIngestionCounterLogicTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Tests.Actors;

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
