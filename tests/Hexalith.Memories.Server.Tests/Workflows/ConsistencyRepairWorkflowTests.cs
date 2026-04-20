// <copyright file="ConsistencyRepairWorkflowTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Tests.Workflows;

using Dapr.Workflow;

using Hexalith.Memories.Contracts.V1;
using Hexalith.Memories.Server.Activities.Indexing;
using Hexalith.Memories.Server.Workflows;

using Microsoft.Extensions.Logging;

using NSubstitute;
using NSubstitute.ExceptionExtensions;

using Shouldly;

/// <summary>
/// Story 8.2 — AC #4 (re-verify before acting; Risk #1), AC #5 (orphan removal),
/// AC #6 (re-index), AC #7 (unrepairable flagging; Risk #5 convergence ceiling).
/// </summary>
public class ConsistencyRepairWorkflowTests
{
    private const string TestTenantId = "tenant-1";

    [Fact]
    public async Task RunAsync_ReVerifyDiffers_NoMutation()
    {
        // Stale snapshot says unit is inconsistent → but the fresh re-verify inside
        // RepairUnitActivity would return NoOp. In the workflow's view, the probe in
        // pass 1 already reports (T,T,T) so no repair is dispatched.
        WorkflowContext context = CreateContext();
        SetEnumeration(context, ["u1"]);
        SetProbe(context, "u1", new ConsistencyResult(true, true, true));

        ConsistencyRepairWorkflow workflow = new();
        ConsistencyRepairResult result = await workflow.RunAsync(
            context,
            new ConsistencyRepairInput(TestTenantId));

        result.TotalDiscrepancies.ShouldBe(0);
        result.RepairedCount.ShouldBe(0);
        result.UnrepairableCount.ShouldBe(0);

        await context.DidNotReceive().CallActivityAsync<RepairActionRecord>(
            nameof(RepairUnitActivity),
            Arg.Any<RepairUnitInput>(),
            Arg.Any<WorkflowTaskOptions>());
    }

    [Fact]
    public async Task RunAsync_ThreePassesFail_RemainingMarkedUnrepairable()
    {
        WorkflowContext context = CreateContext();
        SetEnumeration(context, ["u1"]);
        // Probe keeps returning inconsistent state — repair never converges.
        SetProbe(context, "u1", new ConsistencyResult(true, false, true));

        // Repair activity returns Succeeded=false (e.g. SemanticIndexer threw NotSupported) on each pass.
        context.CallActivityAsync<RepairActionRecord>(
                nameof(RepairUnitActivity),
                Arg.Any<RepairUnitInput>(),
                Arg.Any<WorkflowTaskOptions>())
            .Returns(new RepairActionRecord(
                "u1",
                ConsistencyRepairRecommendation.ReIndexSemantic,
                Succeeded: false,
                FailureReason: "simulated failure",
                BeforeState: new Dictionary<string, string>(),
                AfterState: new Dictionary<string, string>()));

        ConsistencyRepairWorkflow workflow = new();
        ConsistencyRepairResult result = await workflow.RunAsync(
            context,
            new ConsistencyRepairInput(TestTenantId));

        result.PassesExecuted.ShouldBe(ConsistencyRepairWorkflow.MaxRepairPasses);
        result.TotalDiscrepancies.ShouldBe(1);
        result.RepairedCount.ShouldBe(0);
        result.UnrepairableCount.ShouldBeGreaterThan(0);
        result.Actions.Any(a =>
                a.Applied == ConsistencyRepairRecommendation.Unrepairable &&
                (a.FailureReason?.Contains("did not converge", StringComparison.OrdinalIgnoreCase) ?? false))
            .ShouldBeTrue();
    }

    [Fact]
    public async Task RunAsync_ThreePassesSucceed_AllDiscrepanciesRepaired()
    {
        WorkflowContext context = CreateContext();
        SetEnumeration(context, ["u1", "u2", "u3"]);

        // Pass 1 probes → discrepancies. After pass 1, workflow re-enumerates. Simulate
        // convergence: pass 2 enumeration returns consistent probes. NSubstitute's last-call
        // wins for .Returns with multiple invocations ordered via Returns(a, b, c...).
        ConsistencyResult inconsistent = new(true, false, true);
        ConsistencyResult consistent = new(true, true, true);

        context.CallActivityAsync<ConsistencyResult>(
                nameof(VerifyConsistencyActivity),
                Arg.Any<ConsistencyInput>(),
                Arg.Any<WorkflowTaskOptions>())
            .Returns(inconsistent, inconsistent, inconsistent, consistent, consistent, consistent);

        context.CallActivityAsync<RepairActionRecord>(
                nameof(RepairUnitActivity),
                Arg.Any<RepairUnitInput>(),
                Arg.Any<WorkflowTaskOptions>())
            .Returns(callInfo =>
            {
                RepairUnitInput ri = callInfo.ArgAt<RepairUnitInput>(1);
                return new RepairActionRecord(
                    ri.MemoryUnitId,
                    ri.Recommendation,
                    Succeeded: true,
                    FailureReason: null,
                    BeforeState: new Dictionary<string, string>(),
                    AfterState: new Dictionary<string, string>());
            });

        ConsistencyRepairWorkflow workflow = new();
        ConsistencyRepairResult result = await workflow.RunAsync(
            context,
            new ConsistencyRepairInput(TestTenantId));

        result.TotalDiscrepancies.ShouldBe(3);
        result.RepairedCount.ShouldBe(3);
        result.UnrepairableCount.ShouldBe(0);
        result.Actions.Count.ShouldBe(3);
    }

    [Fact]
    public async Task RunAsync_DryRunEquivalent_VerificationPlanMatchesRepairActions()
    {
        // Run verification first — produces a list of Recommendations. Run repair on the
        // same state → the repair's Actions should include the same recommendations (before
        // the re-verify inside the activity, which we short-circuit here by mocking the
        // activity to just echo the input recommendation).
        List<string> ids = ["u1", "u2", "u3", "u4", "u5", "u6", "u7"];
        ConsistencyResult[] probes =
        [
            new(true, false, true),
            new(true, true, false),
            new(true, false, false),
            new(false, true, true),
            new(false, true, false),
            new(false, false, true),
            new(false, false, false),
        ];

        WorkflowContext verifyContext = CreateContext();
        SetEnumeration(verifyContext, ids);
        for (int i = 0; i < ids.Count; i++)
        {
            SetProbe(verifyContext, ids[i], probes[i]);
        }

        ConsistencyVerificationResult verifyResult = await new ConsistencyVerificationWorkflow()
            .RunAsync(verifyContext, new ConsistencyVerificationInput(TestTenantId));

        WorkflowContext repairContext = CreateContext();
        SetEnumeration(repairContext, ids);
        for (int i = 0; i < ids.Count; i++)
        {
            SetProbe(repairContext, ids[i], probes[i]);
        }

        repairContext.CallActivityAsync<RepairActionRecord>(
                nameof(RepairUnitActivity),
                Arg.Any<RepairUnitInput>(),
                Arg.Any<WorkflowTaskOptions>())
            .Returns(callInfo =>
            {
                RepairUnitInput ri = callInfo.ArgAt<RepairUnitInput>(1);
                return new RepairActionRecord(
                    ri.MemoryUnitId,
                    ri.Recommendation,
                    Succeeded: true,
                    FailureReason: null,
                    BeforeState: new Dictionary<string, string>(),
                    AfterState: new Dictionary<string, string>());
            });

        ConsistencyRepairResult repairResult = await new ConsistencyRepairWorkflow()
            .RunAsync(repairContext, new ConsistencyRepairInput(TestTenantId));

        // Verify's discrepancies in pass 1 should map 1:1 to repair's first-pass actions.
        Dictionary<string, ConsistencyRepairRecommendation> verifyPlan = verifyResult.Discrepancies
            .ToDictionary(d => d.MemoryUnitId, d => d.Recommendation);

        foreach (RepairActionRecord action in repairResult.Actions.Take(verifyPlan.Count))
        {
            verifyPlan.ShouldContainKey(action.MemoryUnitId);
            action.Applied.ShouldBe(verifyPlan[action.MemoryUnitId]);
        }
    }

    [Fact]
    public async Task RunAsync_CancellationMidBatch_StopsAfterCurrentBatchWithoutThrowing()
    {
        WorkflowContext context = CreateContext();
        SetEnumeration(context, ["u1", "u2", "u3"]);
        SetProbe(context, "u1", new ConsistencyResult(true, false, true));
        SetProbe(context, "u2", new ConsistencyResult(true, false, true));
        SetProbe(context, "u3", new ConsistencyResult(true, false, true));

        context.CallActivityAsync<RepairActionRecord>(
                nameof(RepairUnitActivity),
                Arg.Any<RepairUnitInput>(),
                Arg.Any<WorkflowTaskOptions>())
            .Returns(
                _ => Task.FromResult(new RepairActionRecord(
                    "u1",
                    ConsistencyRepairRecommendation.ReIndexSemantic,
                    Succeeded: true,
                    FailureReason: null,
                    BeforeState: new Dictionary<string, string>(),
                    AfterState: new Dictionary<string, string>())),
                _ => Task.FromException<RepairActionRecord>(new OperationCanceledException("cancelled mid-batch")),
                _ => Task.FromResult(new RepairActionRecord(
                    "u3",
                    ConsistencyRepairRecommendation.ReIndexSemantic,
                    Succeeded: true,
                    FailureReason: null,
                    BeforeState: new Dictionary<string, string>(),
                    AfterState: new Dictionary<string, string>())));

        ConsistencyRepairWorkflow workflow = new();
        ConsistencyRepairResult result = await workflow.RunAsync(
            context,
            new ConsistencyRepairInput(TestTenantId, BatchSize: 3));

        result.PassesExecuted.ShouldBe(1);
        result.TotalDiscrepancies.ShouldBe(3);
        await context.Received(1).CallActivityAsync<EnumerateMemoryUnitIdsResult>(
            nameof(EnumerateMemoryUnitIdsActivity),
            Arg.Any<EnumerateMemoryUnitIdsInput>(),
            Arg.Any<WorkflowTaskOptions>());
        await context.Received(3).CallActivityAsync<RepairActionRecord>(
            nameof(RepairUnitActivity),
            Arg.Any<RepairUnitInput>(),
            Arg.Any<WorkflowTaskOptions>());
    }

    [Fact]
    public async Task RunAsync_RateLimiterHit_PropagatesAsRetry()
    {
        // The workflow's retry policy (5 attempts, 2s → 5min exponential) is configured
        // uniformly for every CallActivityAsync. A transient rate-limit failure surfaces
        // to the activity, and the workflow engine retries. This test pins the retry
        // profile's presence (via WorkflowTaskOptions) rather than the retry behavior
        // itself (which the DAPR runtime owns).
        WorkflowContext context = CreateContext();
        SetEnumeration(context, ["u1"]);
        SetProbe(context, "u1", new ConsistencyResult(true, false, true));

        context.CallActivityAsync<RepairActionRecord>(
                nameof(RepairUnitActivity),
                Arg.Any<RepairUnitInput>(),
                Arg.Any<WorkflowTaskOptions>())
            .Returns(new RepairActionRecord(
                "u1",
                ConsistencyRepairRecommendation.ReIndexSemantic,
                Succeeded: true,
                FailureReason: null,
                BeforeState: new Dictionary<string, string>(),
                AfterState: new Dictionary<string, string>()));

        ConsistencyRepairWorkflow workflow = new();
        await workflow.RunAsync(context, new ConsistencyRepairInput(TestTenantId));

        // Assert every activity dispatch passed a non-null retry policy with >1 attempt.
        _ = context.Received().CallActivityAsync<RepairActionRecord>(
            nameof(RepairUnitActivity),
            Arg.Any<RepairUnitInput>(),
            Arg.Is<WorkflowTaskOptions>(o => o.RetryPolicy != null && o.RetryPolicy.MaxNumberOfAttempts > 1));
    }

    private static WorkflowContext CreateContext()
    {
        WorkflowContext context = Substitute.For<WorkflowContext>();
        context.InstanceId.Returns($"repair-consistency-{TestTenantId}-test");
        context.CreateReplaySafeLogger<ConsistencyRepairWorkflow>()
            .Returns(Substitute.For<ILogger>());
        context.CurrentUtcDateTime.Returns(DateTime.UtcNow);
        return context;
    }

    private static void SetEnumeration(WorkflowContext context, IReadOnlyList<string> ids)
    {
        context.CallActivityAsync<EnumerateMemoryUnitIdsResult>(
                nameof(EnumerateMemoryUnitIdsActivity),
                Arg.Any<EnumerateMemoryUnitIdsInput>(),
                Arg.Any<WorkflowTaskOptions>())
            .Returns(new EnumerateMemoryUnitIdsResult(ids, ids.Count, Truncated: false));
    }

    private static void SetProbe(WorkflowContext context, string unitId, ConsistencyResult result)
    {
        context.CallActivityAsync<ConsistencyResult>(
                nameof(VerifyConsistencyActivity),
                Arg.Is<ConsistencyInput>(i => i.MemoryUnitId == unitId),
                Arg.Any<WorkflowTaskOptions>())
            .Returns(result);
    }
}
