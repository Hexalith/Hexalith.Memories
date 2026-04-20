// <copyright file="RepairUnitActivityTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Tests.Activities.Indexing;

using Dapr.Workflow;

using Hexalith.Memories.Contracts.V1;
using Hexalith.Memories.Server.Activities.Indexing;
using Hexalith.Memories.Server.Consistency;
using Hexalith.Memories.Server.Graph;

using Microsoft.Extensions.Logging;

using NSubstitute;

using Shouldly;

using StackExchange.Redis;

/// <summary>
/// Story 8.2 — AC #4 (re-verify before acting; Risk #1), AC #5 (orphan removal), AC #6
/// (re-index), AC #7 (unrepairable flagging). Covers the 8-test inventory in AC #9.
/// </summary>
public class RepairUnitActivityTests
{
    private const string TestTenantId = "tenant-1";
    private const string TestMemoryUnitId = "01HM5Q9WXGK6T8Q4Z5Y6V7W8X9";

    [Fact]
    public async Task RunAsync_ReVerifyReturnsConsistent_SkipsAction()
    {
        // Stale recommendation said RemoveOrphanedSemantic; fresh re-verify reports (T,T,T).
        Harness harness = CreateHarness(
            freshRecommendation: ConsistencyRepairRecommendation.NoOp,
            syntactic: true, semantic: true, graph: true);

        RepairActionRecord record = await harness.Activity.RunAsync(
            Substitute.For<WorkflowActivityContext>(),
            new RepairUnitInput(
                TestTenantId,
                TestMemoryUnitId,
                ConsistencyRepairRecommendation.RemoveOrphanedSemantic));

        record.Applied.ShouldBe(ConsistencyRepairRecommendation.NoOp);
        record.Succeeded.ShouldBeTrue();

        // Risk #1: no destructive writes dispatched.
        await harness.SemanticIndexer.DidNotReceive().ReIndexFromSyntacticAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
        await harness.GraphNodeMerger.DidNotReceive().ReMergeFromSyntacticAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
        await harness.RedisDb.DidNotReceive().KeyDeleteAsync(Arg.Any<RedisKey>(), Arg.Any<CommandFlags>());
        harness.GraphQueryBuilder.DidNotReceive().BuildDeleteMemoryUnitNode(Arg.Any<string>());
    }

    [Fact]
    public async Task RunAsync_RemoveOrphanedSemantic_DeletesVectorKey()
    {
        Harness harness = CreateHarness(
            freshRecommendation: ConsistencyRepairRecommendation.RemoveOrphanedSemantic,
            syntactic: false, semantic: true, graph: false);

        RepairActionRecord record = await harness.Activity.RunAsync(
            Substitute.For<WorkflowActivityContext>(),
            new RepairUnitInput(
                TestTenantId,
                TestMemoryUnitId,
                ConsistencyRepairRecommendation.RemoveOrphanedSemantic));

        record.Applied.ShouldBe(ConsistencyRepairRecommendation.RemoveOrphanedSemantic);
        record.Succeeded.ShouldBeTrue();
        record.AfterState["syntactic"].ShouldBe("absent");
        record.BeforeState["semantic"].ShouldBe("present");
        record.AfterState["semantic"].ShouldBe("absent");
        record.AfterState["graph"].ShouldBe("absent");

        await harness.RedisDb.Received(1).KeyDeleteAsync(
            Arg.Is<RedisKey>(k => k.ToString() == $"{TestTenantId}:vec:{TestMemoryUnitId}"),
            Arg.Any<CommandFlags>());
        harness.GraphQueryBuilder.DidNotReceive().BuildDeleteMemoryUnitNode(Arg.Any<string>());
    }

    [Fact]
    public async Task RunAsync_Unrepairable_ReturnsSucceededFalseWithReason()
    {
        Harness harness = CreateHarness(
            freshRecommendation: ConsistencyRepairRecommendation.Unrepairable,
            syntactic: false, semantic: false, graph: false);

        RepairActionRecord record = await harness.Activity.RunAsync(
            Substitute.For<WorkflowActivityContext>(),
            new RepairUnitInput(
                TestTenantId,
                TestMemoryUnitId,
                ConsistencyRepairRecommendation.Unrepairable,
                IncludeUnrepairable: true));

        record.Applied.ShouldBe(ConsistencyRepairRecommendation.Unrepairable);
        record.Succeeded.ShouldBeFalse();
        record.FailureReason.ShouldNotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task RunAsync_FreshReVerifyReportsAllAbsent_ReturnsUnrepairableNotNoOp()
    {
        // Stale recommendation said RemoveOrphanedSemantic; fresh re-verify throws
        // KeyNotFoundException (unit absent in all three backends). Per the repair plan
        // (F,F,F) row, the result must be Unrepairable/Succeeded=false — NOT NoOp. NoOp is
        // reserved for the (T,T,T) consistent case.
        IConsistencyInspectionService inspection = Substitute.For<IConsistencyInspectionService>();
        inspection
            .InspectAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns<ConsistencyInspectionResult>(_ => throw new KeyNotFoundException("absent everywhere"));

        ISemanticIndexer semanticIndexer = Substitute.For<ISemanticIndexer>();
        IGraphNodeMerger graphNodeMerger = Substitute.For<IGraphNodeMerger>();

        IDatabase redisDb = Substitute.For<IDatabase>();
        IConnectionMultiplexer redis = Substitute.For<IConnectionMultiplexer>();
        redis.GetDatabase(Arg.Any<int>(), Arg.Any<object>()).Returns(redisDb);

        IConnectionMultiplexer falkorMux = VerifyConsistencyActivityTestsFactory.CreateFalkorMultiplexer(graphIds: []);
        IGraphQueryBuilder builder = Substitute.For<IGraphQueryBuilder>();

        RepairUnitActivity activity = new(
            inspection,
            semanticIndexer,
            graphNodeMerger,
            redis,
            falkorMux,
            builder,
            Substitute.For<ILogger<RepairUnitActivity>>());

        RepairActionRecord record = await activity.RunAsync(
            Substitute.For<WorkflowActivityContext>(),
            new RepairUnitInput(
                TestTenantId,
                TestMemoryUnitId,
                ConsistencyRepairRecommendation.RemoveOrphanedSemantic));

        record.Applied.ShouldBe(ConsistencyRepairRecommendation.Unrepairable);
        record.Succeeded.ShouldBeFalse();
        record.FailureReason.ShouldNotBeNullOrWhiteSpace();
        record.BeforeState["syntactic"].ShouldBe("absent");
        record.BeforeState["semantic"].ShouldBe("absent");
        record.BeforeState["graph"].ShouldBe("absent");
        record.AfterState.ShouldBe(record.BeforeState, ignoreOrder: true);

        // No destructive writes dispatched.
        await semanticIndexer.DidNotReceive().ReIndexFromSyntacticAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
        await graphNodeMerger.DidNotReceive().ReMergeFromSyntacticAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
        await redisDb.DidNotReceive().KeyDeleteAsync(Arg.Any<RedisKey>(), Arg.Any<CommandFlags>());
        builder.DidNotReceive().BuildDeleteMemoryUnitNode(Arg.Any<string>());
    }

    [Fact]
    public async Task RunAsync_StaleUnrepairableReverifyConsistent_DowngradesToNoOp()
    {
        Harness harness = CreateHarness(
            freshRecommendation: ConsistencyRepairRecommendation.NoOp,
            syntactic: true, semantic: true, graph: true);

        RepairActionRecord record = await harness.Activity.RunAsync(
            Substitute.For<WorkflowActivityContext>(),
            new RepairUnitInput(
                TestTenantId,
                TestMemoryUnitId,
                ConsistencyRepairRecommendation.Unrepairable,
                IncludeUnrepairable: true));

        record.Applied.ShouldBe(ConsistencyRepairRecommendation.NoOp);
        record.Succeeded.ShouldBeTrue();
        await harness.SemanticIndexer.DidNotReceive().ReIndexFromSyntacticAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
        await harness.GraphNodeMerger.DidNotReceive().ReMergeFromSyntacticAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RunAsync_RemoveOrphanedGraph_InvokesDeleteMemoryUnitNode()
    {
        Harness harness = CreateHarness(
            freshRecommendation: ConsistencyRepairRecommendation.RemoveOrphanedGraph,
            syntactic: false, semantic: false, graph: true);

        RepairActionRecord record = await harness.Activity.RunAsync(
            Substitute.For<WorkflowActivityContext>(),
            new RepairUnitInput(
                TestTenantId,
                TestMemoryUnitId,
                ConsistencyRepairRecommendation.RemoveOrphanedGraph));

        record.Applied.ShouldBe(ConsistencyRepairRecommendation.RemoveOrphanedGraph);
        record.Succeeded.ShouldBeTrue();
        record.AfterState["syntactic"].ShouldBe("absent");
        record.AfterState["semantic"].ShouldBe("absent");
        record.AfterState["graph"].ShouldBe("absent");

        harness.GraphQueryBuilder.Received(1).BuildDeleteMemoryUnitNode(TestMemoryUnitId);
        await harness.RedisDb.DidNotReceive().KeyDeleteAsync(Arg.Any<RedisKey>(), Arg.Any<CommandFlags>());
    }

    [Fact]
    public async Task RunAsync_RemoveOrphanedSemanticAndGraph_PerformsBothDeletes()
    {
        Harness harness = CreateHarness(
            freshRecommendation: ConsistencyRepairRecommendation.RemoveOrphanedSemanticAndGraph,
            syntactic: false, semantic: true, graph: true);

        RepairActionRecord record = await harness.Activity.RunAsync(
            Substitute.For<WorkflowActivityContext>(),
            new RepairUnitInput(
                TestTenantId,
                TestMemoryUnitId,
                ConsistencyRepairRecommendation.RemoveOrphanedSemanticAndGraph));

        record.Applied.ShouldBe(ConsistencyRepairRecommendation.RemoveOrphanedSemanticAndGraph);
        record.Succeeded.ShouldBeTrue();

        await harness.RedisDb.Received(1).KeyDeleteAsync(
            Arg.Is<RedisKey>(k => k.ToString() == $"{TestTenantId}:vec:{TestMemoryUnitId}"),
            Arg.Any<CommandFlags>());
        harness.GraphQueryBuilder.Received(1).BuildDeleteMemoryUnitNode(TestMemoryUnitId);
    }

    [Fact]
    public async Task RunAsync_ReIndexSemantic_InvokesSemanticIndexer()
    {
        Harness harness = CreateHarness(
            freshRecommendation: ConsistencyRepairRecommendation.ReIndexSemantic,
            syntactic: true, semantic: false, graph: true);

        RepairActionRecord record = await harness.Activity.RunAsync(
            Substitute.For<WorkflowActivityContext>(),
            new RepairUnitInput(
                TestTenantId,
                TestMemoryUnitId,
                ConsistencyRepairRecommendation.ReIndexSemantic));

        record.Applied.ShouldBe(ConsistencyRepairRecommendation.ReIndexSemantic);
        record.Succeeded.ShouldBeTrue();

        await harness.SemanticIndexer.Received(1).ReIndexFromSyntacticAsync(
            TestTenantId, TestMemoryUnitId, Arg.Any<CancellationToken>());
        await harness.GraphNodeMerger.DidNotReceive().ReMergeFromSyntacticAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RunAsync_ReIndexGraph_InvokesGraphNodeMerger()
    {
        Harness harness = CreateHarness(
            freshRecommendation: ConsistencyRepairRecommendation.ReIndexGraph,
            syntactic: true, semantic: true, graph: false);

        RepairActionRecord record = await harness.Activity.RunAsync(
            Substitute.For<WorkflowActivityContext>(),
            new RepairUnitInput(
                TestTenantId,
                TestMemoryUnitId,
                ConsistencyRepairRecommendation.ReIndexGraph));

        record.Applied.ShouldBe(ConsistencyRepairRecommendation.ReIndexGraph);
        record.Succeeded.ShouldBeTrue();

        await harness.GraphNodeMerger.Received(1).ReMergeFromSyntacticAsync(
            TestTenantId, TestMemoryUnitId, Arg.Any<CancellationToken>());
        await harness.SemanticIndexer.DidNotReceive().ReIndexFromSyntacticAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RunAsync_ReIndexSemanticAndGraph_InvokesBoth()
    {
        Harness harness = CreateHarness(
            freshRecommendation: ConsistencyRepairRecommendation.ReIndexSemanticAndGraph,
            syntactic: true, semantic: false, graph: false);

        RepairActionRecord record = await harness.Activity.RunAsync(
            Substitute.For<WorkflowActivityContext>(),
            new RepairUnitInput(
                TestTenantId,
                TestMemoryUnitId,
                ConsistencyRepairRecommendation.ReIndexSemanticAndGraph));

        record.Applied.ShouldBe(ConsistencyRepairRecommendation.ReIndexSemanticAndGraph);
        record.Succeeded.ShouldBeTrue();

        await harness.SemanticIndexer.Received(1).ReIndexFromSyntacticAsync(
            TestTenantId, TestMemoryUnitId, Arg.Any<CancellationToken>());
        await harness.GraphNodeMerger.Received(1).ReMergeFromSyntacticAsync(
            TestTenantId, TestMemoryUnitId, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RunAsync_ReIndexSemanticAndGraph_GraphMergeFailure_RecordsObservedAfterState()
    {
        IConsistencyInspectionService inspection = Substitute.For<IConsistencyInspectionService>();
        inspection
            .InspectAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(
                new ConsistencyInspectionResult(
                    TestTenantId,
                    TestMemoryUnitId,
                    SyntacticPresent: true,
                    SemanticPresent: false,
                    GraphPresent: false,
                    null,
                    null,
                    null,
                    ConsistencyRepairRecommendation.ReIndexSemanticAndGraph,
                    DateTimeOffset.UtcNow),
                new ConsistencyInspectionResult(
                    TestTenantId,
                    TestMemoryUnitId,
                    SyntacticPresent: true,
                    SemanticPresent: true,
                    GraphPresent: false,
                    null,
                    new ConsistencySemanticDetail(1536, $"{TestTenantId}:vec:{TestMemoryUnitId}"),
                    null,
                    ConsistencyRepairRecommendation.ReIndexGraph,
                    DateTimeOffset.UtcNow));

        ISemanticIndexer semanticIndexer = Substitute.For<ISemanticIndexer>();
        IGraphNodeMerger graphNodeMerger = Substitute.For<IGraphNodeMerger>();
        graphNodeMerger
            .ReMergeFromSyntacticAsync(TestTenantId, TestMemoryUnitId, Arg.Any<CancellationToken>())
            .Returns(Task.FromException(new InvalidOperationException("graph merge failed")));

        IDatabase redisDb = Substitute.For<IDatabase>();
        IConnectionMultiplexer redis = Substitute.For<IConnectionMultiplexer>();
        redis.GetDatabase(Arg.Any<int>(), Arg.Any<object>()).Returns(redisDb);

        RepairUnitActivity activity = new(
            inspection,
            semanticIndexer,
            graphNodeMerger,
            redis,
            VerifyConsistencyActivityTestsFactory.CreateFalkorMultiplexer(graphIds: []),
            Substitute.For<IGraphQueryBuilder>(),
            Substitute.For<ILogger<RepairUnitActivity>>());

        RepairActionRecord record = await activity.RunAsync(
            Substitute.For<WorkflowActivityContext>(),
            new RepairUnitInput(
                TestTenantId,
                TestMemoryUnitId,
                ConsistencyRepairRecommendation.ReIndexSemanticAndGraph));

        record.Applied.ShouldBe(ConsistencyRepairRecommendation.ReIndexSemanticAndGraph);
        record.Succeeded.ShouldBeFalse();
        record.FailureReason.ShouldNotBeNull();
        record.FailureReason.ShouldContain("graph merge failed");
        record.BeforeState["syntactic"].ShouldBe("present");
        record.BeforeState["semantic"].ShouldBe("absent");
        record.BeforeState["graph"].ShouldBe("absent");
        record.AfterState["syntactic"].ShouldBe("present");
        record.AfterState["semantic"].ShouldBe("present");
        record.AfterState["graph"].ShouldBe("absent");

        await semanticIndexer.Received(1).ReIndexFromSyntacticAsync(
            TestTenantId, TestMemoryUnitId, Arg.Any<CancellationToken>());
        await graphNodeMerger.Received(1).ReMergeFromSyntacticAsync(
            TestTenantId, TestMemoryUnitId, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RunAsync_ReIndexSemanticFailure_PreservesBeforeStateOnFailure()
    {
        Harness harness = CreateHarness(
            freshRecommendation: ConsistencyRepairRecommendation.ReIndexSemantic,
            syntactic: true, semantic: false, graph: true);
        harness.SemanticIndexer
            .ReIndexFromSyntacticAsync(TestTenantId, TestMemoryUnitId, Arg.Any<CancellationToken>())
            .Returns(Task.FromException(new InvalidOperationException("semantic pipeline unavailable")));

        RepairActionRecord record = await harness.Activity.RunAsync(
            Substitute.For<WorkflowActivityContext>(),
            new RepairUnitInput(
                TestTenantId,
                TestMemoryUnitId,
                ConsistencyRepairRecommendation.ReIndexSemantic));

        record.Applied.ShouldBe(ConsistencyRepairRecommendation.ReIndexSemantic);
        record.Succeeded.ShouldBeFalse();
        record.FailureReason.ShouldNotBeNull();
        record.FailureReason.ShouldContain("semantic pipeline unavailable");
        record.BeforeState["syntactic"].ShouldBe("present");
        record.BeforeState["semantic"].ShouldBe("absent");
        record.BeforeState["graph"].ShouldBe("present");
        record.AfterState.ShouldBe(record.BeforeState, ignoreOrder: true);
    }

    private static Harness CreateHarness(
        ConsistencyRepairRecommendation freshRecommendation,
        bool syntactic,
        bool semantic,
        bool graph)
    {
        IConsistencyInspectionService inspection = Substitute.For<IConsistencyInspectionService>();
        ConsistencyInspectionResult freshResult = new(
            TestTenantId,
            TestMemoryUnitId,
            syntactic,
            semantic,
            graph,
            null,
            null,
            null,
            freshRecommendation,
            DateTimeOffset.UtcNow);
        inspection
            .InspectAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(freshResult);

        ISemanticIndexer semanticIndexer = Substitute.For<ISemanticIndexer>();
        IGraphNodeMerger graphNodeMerger = Substitute.For<IGraphNodeMerger>();

        IDatabase redisDb = Substitute.For<IDatabase>();
        IConnectionMultiplexer redis = Substitute.For<IConnectionMultiplexer>();
        redis.GetDatabase(Arg.Any<int>(), Arg.Any<object>()).Returns(redisDb);

        IConnectionMultiplexer falkorMux = VerifyConsistencyActivityTestsFactory.CreateFalkorMultiplexer(graphIds: []);

        IGraphQueryBuilder builder = Substitute.For<IGraphQueryBuilder>();
        builder.BuildDeleteMemoryUnitNode(Arg.Any<string>())
            .Returns(("MATCH (m:MemoryUnit {id: $id}) DETACH DELETE m", new Dictionary<string, object> { ["id"] = TestMemoryUnitId }));

        RepairUnitActivity activity = new(
            inspection,
            semanticIndexer,
            graphNodeMerger,
            redis,
            falkorMux,
            builder,
            Substitute.For<ILogger<RepairUnitActivity>>());

        return new Harness(activity, inspection, semanticIndexer, graphNodeMerger, redisDb, builder);
    }

    private sealed record Harness(
        RepairUnitActivity Activity,
        IConsistencyInspectionService Inspection,
        ISemanticIndexer SemanticIndexer,
        IGraphNodeMerger GraphNodeMerger,
        IDatabase RedisDb,
        IGraphQueryBuilder GraphQueryBuilder);
}
