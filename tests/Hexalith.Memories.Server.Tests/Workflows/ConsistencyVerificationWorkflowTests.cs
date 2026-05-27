// <copyright file="ConsistencyVerificationWorkflowTests.cs" company="ITANEO">
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

using Shouldly;

/// <summary>
/// Story 8.2 — AC #1 (workflow orchestration), AC #2 (count invariant + discrepancy shape),
/// AC #8 (batched processing), Risk #2 (bounded fan-out), Risk #7 (10K truncation).
/// </summary>
public class ConsistencyVerificationWorkflowTests
{
    private const string TestTenantId = "tenant-1";

    [Fact]
    public async Task RunAsync_EmptyTenant_ReturnsZeroDiscrepancies()
    {
        WorkflowContext context = CreateContext();
        SetEnumeration(context, []);

        ConsistencyVerificationWorkflow workflow = new();
        ConsistencyVerificationResult result = await workflow.RunAsync(
            context,
            new ConsistencyVerificationInput(TestTenantId));

        result.TotalUnits.ShouldBe(0);
        result.ConsistentCount.ShouldBe(0);
        result.InconsistentCount.ShouldBe(0);
        result.Discrepancies.Count.ShouldBe(0);

        // No probe dispatches on empty tenant.
        await context.DidNotReceive().CallActivityAsync<ConsistencyResult>(
            nameof(VerifyConsistencyActivity),
            Arg.Any<ConsistencyInput>(),
            Arg.Any<WorkflowTaskOptions>());
    }

    [Fact]
    public async Task RunAsync_AllConsistent_ReturnsZeroDiscrepancies()
    {
        WorkflowContext context = CreateContext();
        List<string> ids = Enumerable.Range(0, 10).Select(i => $"u{i:D4}").ToList();
        SetEnumeration(context, ids);
        SetAllProbes(context, new ConsistencyResult(true, true, true));

        ConsistencyVerificationWorkflow workflow = new();
        ConsistencyVerificationResult result = await workflow.RunAsync(
            context,
            new ConsistencyVerificationInput(TestTenantId));

        result.TotalUnits.ShouldBe(10);
        result.ConsistentCount.ShouldBe(10);
        result.InconsistentCount.ShouldBe(0);
        result.NoteCount.ShouldBe(0);
        result.Discrepancies.Count.ShouldBe(0);
        result.Notes.ShouldBeEmpty();
    }

    [Fact]
    public async Task RunAsync_AggregateCounts_InvariantHolds()
    {
        WorkflowContext context = CreateContext();
        List<string> ids = ["c0", "c1", "c2", "n0", "i0", "i1"];
        SetEnumeration(context, ids);

        context.CallActivityAsync<ConsistencyResult>(
                nameof(VerifyConsistencyActivity),
                Arg.Is<ConsistencyInput>(i => i.MemoryUnitId.StartsWith('c')),
                Arg.Any<WorkflowTaskOptions>())
            .Returns(new ConsistencyResult(true, true, true));

        context.CallActivityAsync<ConsistencyResult>(
                nameof(VerifyConsistencyActivity),
                Arg.Is<ConsistencyInput>(i => i.MemoryUnitId.StartsWith('n')),
                Arg.Any<WorkflowTaskOptions>())
            .Returns(new ConsistencyResult(true, true, true)
            {
                NaturalLanguageSemanticExists = false,
                NaturalLanguageEmbeddingStatus = NaturalLanguageEmbeddingStatus.Indexed,
                ConsistencyNote = "Missing backends: semantic-nl",
            });

        context.CallActivityAsync<ConsistencyResult>(
                nameof(VerifyConsistencyActivity),
                Arg.Is<ConsistencyInput>(i => i.MemoryUnitId.StartsWith('i')),
                Arg.Any<WorkflowTaskOptions>())
            .Returns(new ConsistencyResult(true, false, true));

        ConsistencyVerificationWorkflow workflow = new();
        ConsistencyVerificationResult result = await workflow.RunAsync(
            context,
            new ConsistencyVerificationInput(TestTenantId));

        (result.ConsistentCount + result.InconsistentCount).ShouldBe(result.TotalUnits);
        result.NoteCount.ShouldBe(1);
        result.TotalNoteCount.ShouldBe(1);
        result.Discrepancies.Count.ShouldBe(result.TotalDiscrepancyCount);
    }

    [Fact]
    public async Task RunAsync_OneOfEachDiscrepancyType_AllRecommendationsRepresented()
    {
        WorkflowContext context = CreateContext();

        Dictionary<string, ConsistencyResult> perUnit = new()
        {
            ["tft"] = new(true, false, true),   // ReIndexSemantic
            ["ttf"] = new(true, true, false),   // ReIndexGraph
            ["tff"] = new(true, false, false),  // ReIndexSemanticAndGraph
            ["ftt"] = new(false, true, true),   // RemoveOrphanedSemanticAndGraph
            ["ftf"] = new(false, true, false),  // RemoveOrphanedSemantic
            ["fft"] = new(false, false, true),  // RemoveOrphanedGraph
            ["fff"] = new(false, false, false), // Unrepairable
        };

        SetEnumeration(context, perUnit.Keys.ToList());

        foreach ((string id, ConsistencyResult probe) in perUnit)
        {
            string capturedId = id;
            ConsistencyResult capturedProbe = probe;
            context.CallActivityAsync<ConsistencyResult>(
                    nameof(VerifyConsistencyActivity),
                    Arg.Is<ConsistencyInput>(i => i.MemoryUnitId == capturedId),
                    Arg.Any<WorkflowTaskOptions>())
                .Returns(capturedProbe);
        }

        ConsistencyVerificationWorkflow workflow = new();
        ConsistencyVerificationResult result = await workflow.RunAsync(
            context,
            new ConsistencyVerificationInput(TestTenantId));

        result.Discrepancies.Count.ShouldBe(7);
        IReadOnlySet<ConsistencyRepairRecommendation> distinct = result.Discrepancies
            .Select(d => d.Recommendation)
            .ToHashSet();

        distinct.ShouldBe(new HashSet<ConsistencyRepairRecommendation>
        {
            ConsistencyRepairRecommendation.ReIndexSemantic,
            ConsistencyRepairRecommendation.ReIndexGraph,
            ConsistencyRepairRecommendation.ReIndexSemanticAndGraph,
            ConsistencyRepairRecommendation.RemoveOrphanedSemantic,
            ConsistencyRepairRecommendation.RemoveOrphanedGraph,
            ConsistencyRepairRecommendation.RemoveOrphanedSemanticAndGraph,
            ConsistencyRepairRecommendation.Unrepairable,
        }, ignoreOrder: true);
    }

    [Fact]
    public async Task RunAsync_BatchedFanOut_DoesNotExceedBatchSize()
    {
        WorkflowContext context = CreateContext();
        List<string> ids = Enumerable.Range(0, 2000).Select(i => $"u{i:D5}").ToList();
        SetEnumeration(context, ids);

        int inFlight = 0;
        int maxInFlight = 0;
        TaskCompletionSource gate = new(TaskCreationOptions.RunContinuationsAsynchronously);

        context.CallActivityAsync<ConsistencyResult>(
                nameof(VerifyConsistencyActivity),
                Arg.Any<ConsistencyInput>(),
                Arg.Any<WorkflowTaskOptions>())
            .Returns(_ => AwaitProbeAsync());

        async Task<ConsistencyResult> AwaitProbeAsync()
        {
            int active = Interlocked.Increment(ref inFlight);
            UpdateMax(ref maxInFlight, active);
            if (active == 500)
            {
                gate.TrySetResult();
            }

            await gate.Task.ConfigureAwait(false);
            _ = Interlocked.Decrement(ref inFlight);
            return new ConsistencyResult(true, true, true);
        }

        ConsistencyVerificationWorkflow workflow = new();
        await workflow.RunAsync(
            context,
            new ConsistencyVerificationInput(TestTenantId, BatchSize: 500));

        maxInFlight.ShouldBe(500);
        await context.Received(2000).CallActivityAsync<ConsistencyResult>(
            nameof(VerifyConsistencyActivity),
            Arg.Any<ConsistencyInput>(),
            Arg.Any<WorkflowTaskOptions>());
    }

    [Fact]
    public async Task RunAsync_TenThousandAndOneDiscrepancies_ResultTruncatedAt10000()
    {
        WorkflowContext context = CreateContext();
        List<string> ids = Enumerable.Range(0, 10_001).Select(i => $"u{i:D6}").ToList();
        SetEnumeration(context, ids);
        SetAllProbes(context, new ConsistencyResult(false, true, false)); // RemoveOrphanedSemantic

        ConsistencyVerificationWorkflow workflow = new();
        ConsistencyVerificationResult result = await workflow.RunAsync(
            context,
            new ConsistencyVerificationInput(TestTenantId, BatchSize: 5000));

        result.Discrepancies.Count.ShouldBe(10_000);
        result.TotalDiscrepancyCount.ShouldBe(10_001);
        result.TruncatedAt.ShouldNotBeNull();
        result.InconsistentCount.ShouldBe(10_001);
        result.NoteCount.ShouldBe(0);
        result.TotalNoteCount.ShouldBe(0);
        result.Notes.ShouldBeEmpty();
    }

    [Fact]
    public async Task RunAsync_NotesAndDiscrepancies_HaveIndependentCaps()
    {
        // S6-D1 (re-review 2026-04-25): Discrepancies and Notes have INDEPENDENT 10,000-entry caps.
        // 9,999 discrepancies + 2 notes ⇒ both lists fit within their own budgets, so neither is
        // truncated. Verifies that the previous shared-cap eviction behavior is gone.
        WorkflowContext context = CreateContext();
        List<string> ids = Enumerable.Range(0, 9_999).Select(i => $"d{i:D6}")
            .Concat(["n000000", "n000001"])
            .ToList();
        SetEnumeration(context, ids);

        context.CallActivityAsync<ConsistencyResult>(
                nameof(VerifyConsistencyActivity),
                Arg.Is<ConsistencyInput>(i => i.MemoryUnitId.StartsWith('d')),
                Arg.Any<WorkflowTaskOptions>())
            .Returns(new ConsistencyResult(true, false, true));

        context.CallActivityAsync<ConsistencyResult>(
                nameof(VerifyConsistencyActivity),
                Arg.Is<ConsistencyInput>(i => i.MemoryUnitId.StartsWith('n')),
                Arg.Any<WorkflowTaskOptions>())
            .Returns(new ConsistencyResult(true, true, true)
            {
                NaturalLanguageSemanticExists = false,
                NaturalLanguageEmbeddingStatus = NaturalLanguageEmbeddingStatus.Indexed,
                ConsistencyNote = "Missing backends: semantic-nl",
            });

        ConsistencyVerificationWorkflow workflow = new();
        ConsistencyVerificationResult result = await workflow.RunAsync(
            context,
            new ConsistencyVerificationInput(TestTenantId, BatchSize: 5000));

        result.TotalUnits.ShouldBe(10_001);
        result.ConsistentCount.ShouldBe(2);
        result.InconsistentCount.ShouldBe(9_999);
        result.NoteCount.ShouldBe(2);
        result.TotalNoteCount.ShouldBe(2);
        result.TotalDiscrepancyCount.ShouldBe(9_999);
        result.Discrepancies.Count.ShouldBe(9_999);
        result.Notes.Count.ShouldBe(2);
        result.TruncatedAt.ShouldBeNull();
    }

    [Fact]
    public async Task RunAsync_DiscrepancyCapExceeded_TruncatesDiscrepanciesNotNotes()
    {
        // S6-P1 + S6-D1: when discrepancies exceed their independent cap, NoteCount = notes.Count
        // (in-payload), TotalNoteCount = un-truncated total. Both assert under truncation. 1 note +
        // 10_001 discrepancies ⇒ discrepancies truncated to 10_000, notes untouched.
        WorkflowContext context = CreateContext();
        List<string> ids = Enumerable.Range(0, 10_001).Select(i => $"d{i:D6}")
            .Concat(["n000000"])
            .ToList();
        SetEnumeration(context, ids);

        context.CallActivityAsync<ConsistencyResult>(
                nameof(VerifyConsistencyActivity),
                Arg.Is<ConsistencyInput>(i => i.MemoryUnitId.StartsWith('d')),
                Arg.Any<WorkflowTaskOptions>())
            .Returns(new ConsistencyResult(true, false, true));

        context.CallActivityAsync<ConsistencyResult>(
                nameof(VerifyConsistencyActivity),
                Arg.Is<ConsistencyInput>(i => i.MemoryUnitId.StartsWith('n')),
                Arg.Any<WorkflowTaskOptions>())
            .Returns(new ConsistencyResult(true, true, true)
            {
                NaturalLanguageSemanticExists = false,
                NaturalLanguageEmbeddingStatus = NaturalLanguageEmbeddingStatus.Indexed,
                ConsistencyNote = "Missing backends: semantic-nl",
            });

        ConsistencyVerificationWorkflow workflow = new();
        ConsistencyVerificationResult result = await workflow.RunAsync(
            context,
            new ConsistencyVerificationInput(TestTenantId, BatchSize: 5000));

        result.TotalUnits.ShouldBe(10_002);
        result.Discrepancies.Count.ShouldBe(ConsistencyVerificationWorkflow.MaxDiscrepancyEntries);
        result.TotalDiscrepancyCount.ShouldBe(10_001);
        result.Notes.Count.ShouldBe(1);
        result.NoteCount.ShouldBe(1);
        result.TotalNoteCount.ShouldBe(1);
        result.TruncatedAt.ShouldNotBeNull();
    }

    [Fact]
    public async Task RunAsync_IdempotentReEntry_DeterministicResult()
    {
        List<string> ids = ["a", "b", "c"];

        async Task<ConsistencyVerificationResult> RunOnce()
        {
            WorkflowContext context = CreateContext();
            SetEnumeration(context, ids);
            context.CallActivityAsync<ConsistencyResult>(
                    nameof(VerifyConsistencyActivity),
                    Arg.Is<ConsistencyInput>(i => i.MemoryUnitId == "a"),
                    Arg.Any<WorkflowTaskOptions>())
                .Returns(new ConsistencyResult(true, true, true));
            context.CallActivityAsync<ConsistencyResult>(
                    nameof(VerifyConsistencyActivity),
                    Arg.Is<ConsistencyInput>(i => i.MemoryUnitId == "b"),
                    Arg.Any<WorkflowTaskOptions>())
                .Returns(new ConsistencyResult(true, false, true));
            context.CallActivityAsync<ConsistencyResult>(
                    nameof(VerifyConsistencyActivity),
                    Arg.Is<ConsistencyInput>(i => i.MemoryUnitId == "c"),
                    Arg.Any<WorkflowTaskOptions>())
                .Returns(new ConsistencyResult(false, false, false));

            ConsistencyVerificationWorkflow workflow = new();
            return await workflow.RunAsync(context, new ConsistencyVerificationInput(TestTenantId));
        }

        ConsistencyVerificationResult first = await RunOnce();
        ConsistencyVerificationResult second = await RunOnce();

        first.TotalUnits.ShouldBe(second.TotalUnits);
        first.ConsistentCount.ShouldBe(second.ConsistentCount);
        first.InconsistentCount.ShouldBe(second.InconsistentCount);

        IEnumerable<(string, ConsistencyRepairRecommendation)> firstKeys =
            first.Discrepancies.Select(d => (d.MemoryUnitId, d.Recommendation));
        IEnumerable<(string, ConsistencyRepairRecommendation)> secondKeys =
            second.Discrepancies.Select(d => (d.MemoryUnitId, d.Recommendation));

        firstKeys.ShouldBe(secondKeys);
    }

    [Fact]
    public async Task RunAsync_QueuedNaturalLanguageMissing_RemainsConsistentAndRoutesToNotes()
    {
        WorkflowContext context = CreateContext();
        SetEnumeration(context, ["queued-nl"]);
        context.CallActivityAsync<ConsistencyResult>(
                nameof(VerifyConsistencyActivity),
                Arg.Any<ConsistencyInput>(),
                Arg.Any<WorkflowTaskOptions>())
            .Returns(new ConsistencyResult(true, true, true)
            {
                NaturalLanguageSemanticExists = false,
                NaturalLanguageEmbeddingStatus = NaturalLanguageEmbeddingStatus.Queued,
                ConsistencyNote = "Natural-language semantic hash pending queued retry.",
            });

        ConsistencyVerificationWorkflow workflow = new();
        ConsistencyVerificationResult result = await workflow.RunAsync(
            context,
            new ConsistencyVerificationInput(TestTenantId));

        result.ConsistentCount.ShouldBe(1);
        result.InconsistentCount.ShouldBe(0);
        result.NoteCount.ShouldBe(1);
        result.TotalNoteCount.ShouldBe(1);
        result.Discrepancies.ShouldBeEmpty();
        result.Notes.Count.ShouldBe(1);
        result.Notes[0].Recommendation.ShouldBe(ConsistencyRepairRecommendation.NoOp);
        result.Notes[0].ConsistencyNoteKind.ShouldBe(ConsistencyNoteKind.NaturalLanguageEmbeddingQueued);
        string? queuedNote = result.Notes[0].ConsistencyNote;
        queuedNote.ShouldNotBeNull();
        queuedNote.ShouldContain("queued retry");
    }

    [Fact]
    public async Task RunAsync_IndexedNaturalLanguageMissing_RoutesToNotesNotDiscrepancies()
    {
        // Story 9.2 review D7 (committed-branch review 2026-04-24): when the three-axis repair
        // recommendation is NoOp but the NL semantic sibling is missing (real NL gap), the entry
        // MUST route to `Notes`, not `Discrepancies`. Consumers filtering
        // `Discrepancies.Where(d => d.Recommendation != NoOp)` previously missed NL-only gaps —
        // the split restores the expected "action-required" semantics for Discrepancies while
        // preserving the NL observation in Notes.
        WorkflowContext context = CreateContext();
        SetEnumeration(context, ["indexed-nl-gap"]);
        context.CallActivityAsync<ConsistencyResult>(
                nameof(VerifyConsistencyActivity),
                Arg.Any<ConsistencyInput>(),
                Arg.Any<WorkflowTaskOptions>())
            .Returns(new ConsistencyResult(true, true, true)
            {
                ConsistencyNoteKind = ConsistencyNoteKind.None,
                NaturalLanguageSemanticExists = false,
                NaturalLanguageEmbeddingStatus = NaturalLanguageEmbeddingStatus.Indexed,
                ConsistencyNote = "Missing backends: semantic-nl",
            });

        ConsistencyVerificationWorkflow workflow = new();
        ConsistencyVerificationResult result = await workflow.RunAsync(
            context,
            new ConsistencyVerificationInput(TestTenantId));

        result.ConsistentCount.ShouldBe(1);
        result.InconsistentCount.ShouldBe(0);
        result.NoteCount.ShouldBe(1);
        result.TotalNoteCount.ShouldBe(1);
        result.TotalDiscrepancyCount.ShouldBe(0);
        result.Discrepancies.Count.ShouldBe(0, "NoOp + NL gap is informational — must not appear in Discrepancies.");
        result.Notes.Count.ShouldBe(1);
        result.Notes[0].Recommendation.ShouldBe(ConsistencyRepairRecommendation.NoOp);
        result.Notes[0].NaturalLanguageSemanticPresent.ShouldBeFalse();
        result.Notes[0].NaturalLanguageEmbeddingStatus.ShouldBe(NaturalLanguageEmbeddingStatus.Indexed);
        result.Notes[0].ConsistencyNoteKind.ShouldBe(ConsistencyNoteKind.NaturalLanguageEmbeddingMissing);
        string? consistencyNote = result.Notes[0].ConsistencyNote;
        consistencyNote.ShouldNotBeNull();
        consistencyNote.ShouldContain("semantic-nl");
    }

    [Fact]
    public async Task RunAsync_MultiAxisGapWithNlNote_RoutesToDiscrepanciesNotNotes()
    {
        // Complement to IndexedNaturalLanguageMissing test: when a real multi-axis repair IS
        // required, the NL note rides along as supplementary context on the Discrepancy entry
        // rather than splitting into a second Notes entry.
        WorkflowContext context = CreateContext();
        SetEnumeration(context, ["semantic-missing"]);
        context.CallActivityAsync<ConsistencyResult>(
                nameof(VerifyConsistencyActivity),
                Arg.Any<ConsistencyInput>(),
                Arg.Any<WorkflowTaskOptions>())
            .Returns(new ConsistencyResult(true, false, true)
            {
                ConsistencyNoteKind = ConsistencyNoteKind.None,
                NaturalLanguageSemanticExists = false,
                NaturalLanguageEmbeddingStatus = NaturalLanguageEmbeddingStatus.Indexed,
                ConsistencyNote = "Missing backends: semantic-nl",
            });

        ConsistencyVerificationWorkflow workflow = new();
        ConsistencyVerificationResult result = await workflow.RunAsync(
            context,
            new ConsistencyVerificationInput(TestTenantId));

        result.InconsistentCount.ShouldBe(1);
        result.NoteCount.ShouldBe(0);
        result.Discrepancies.Count.ShouldBe(1);
        result.Discrepancies[0].Recommendation.ShouldBe(ConsistencyRepairRecommendation.ReIndexSemantic);
        result.Discrepancies[0].ConsistencyNoteKind.ShouldBe(ConsistencyNoteKind.NaturalLanguageEmbeddingMissing);
        string? discrepancyNote = result.Discrepancies[0].ConsistencyNote;
        discrepancyNote.ShouldNotBeNull();
        discrepancyNote.ShouldContain("semantic-nl");
        result.Notes.Count.ShouldBe(0);
    }

    private static WorkflowContext CreateContext()
    {
        WorkflowContext context = Substitute.For<WorkflowContext>();
        context.InstanceId.Returns($"verify-consistency-{TestTenantId}-test");
        context.CreateReplaySafeLogger<ConsistencyVerificationWorkflow>()
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

    private static void SetAllProbes(WorkflowContext context, ConsistencyResult result)
    {
        context.CallActivityAsync<ConsistencyResult>(
                nameof(VerifyConsistencyActivity),
                Arg.Any<ConsistencyInput>(),
                Arg.Any<WorkflowTaskOptions>())
            .Returns(result);
    }

    private static void UpdateMax(ref int max, int candidate)
    {
        int snapshot;
        do
        {
            snapshot = max;
            if (candidate <= snapshot)
            {
                return;
            }
        }
        while (Interlocked.CompareExchange(ref max, candidate, snapshot) != snapshot);
    }
}
