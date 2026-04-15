// <copyright file="IngestionWorkflowTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Tests.Workflows;

using System.Text;

using Dapr.Workflow;

using Hexalith.Memories.Contracts.V1;
using Hexalith.Memories.Server.Activities.Indexing;
using Hexalith.Memories.Server.Activities.Ingestion;
using Hexalith.Memories.Server.Ingestion;
using Hexalith.Memories.Server.Search;
using Hexalith.Memories.Server.Workflows;
using Hexalith.Memories.TestHelpers.Factories;

using Microsoft.Extensions.Logging;

using NSubstitute;

using StackExchange.Redis;

using Shouldly;

public class IngestionWorkflowTests
{
    private static readonly DateTime TestTimestamp = new(2026, 3, 29, 10, 0, 0, DateTimeKind.Utc);
    private static readonly Guid TestGuid = Guid.Parse("11111111-1111-1111-1111-111111111111");

    // --- AC1: Full pipeline orchestration ---

    [Fact]
    public async Task RunAsync_HappyPath_ShouldCallAllActivitiesInOrder()
    {
        IngestionInput input = IngestionInputFactory.Create();
        WorkflowContext context = CreateMockContext();
        List<string> callLog = [];
        SetupHappyPathActivities(context, input, callLog: callLog);
        IngestionWorkflow workflow = new();

        IngestionResult result = await workflow.RunAsync(context, input);

        result.ShouldNotBeNull();
        result.Status.ShouldBe(MemoryUnitStatus.Indexed);
        result.MemoryUnitId.ShouldBe(TestGuid.ToString());
        result.WasDuplicate.ShouldBeFalse();
        result.ConsistencyNote.ShouldBeNull();
        callLog.ShouldBe(
        [
            nameof(CheckIdempotencyActivity),
            nameof(ValidateContentActivity),
            nameof(ExtractContentActivity),
            nameof(GenerateEmbeddingActivity),
            nameof(IndexSyntacticActivity),
            nameof(IndexSemanticActivity),
            nameof(IndexGraphActivity),
            nameof(VerifyConsistencyActivity),
            nameof(SaveDedupKeyActivity),
            nameof(RecordCaseActivityActivity),
        ]);
    }

    [Fact]
    public async Task RunAsync_HappyPath_ShouldReturnIndexedStatusWithMemoryUnitId()
    {
        IngestionInput input = IngestionInputFactory.Create();
        WorkflowContext context = CreateMockContext();
        SetupHappyPathActivities(context, input);
        IngestionWorkflow workflow = new();

        IngestionResult result = await workflow.RunAsync(context, input);

        result.Status.ShouldBe(MemoryUnitStatus.Indexed);
        result.MemoryUnitId.ShouldNotBeNullOrWhiteSpace();
        result.WasDuplicate.ShouldBeFalse();
        result.ConsistencyNote.ShouldBeNull();
    }

    [Fact]
    public async Task RunAsync_FanOut_ShouldCallAllThreeIndexingActivities()
    {
        IngestionInput input = IngestionInputFactory.Create();
        WorkflowContext context = CreateMockContext();
        List<string> callLog = [];
        SetupPreIndexActivities(context, input, callLog);

        TaskCompletionSource<IndexResult> syntacticTask = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource<IndexResult> semanticTask = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource<IndexResult> graphTask = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource<bool> allIndexTasksScheduled = new(TaskCreationOptions.RunContinuationsAsynchronously);
        int scheduledCount = 0;

        Task<IndexResult> RegisterIndexTask(string activityName, Task<IndexResult> task)
        {
            callLog.Add(activityName);
            if (Interlocked.Increment(ref scheduledCount) == 3)
            {
                allIndexTasksScheduled.TrySetResult(true);
            }

            return task;
        }

        context.CallActivityAsync<IndexResult>(
                nameof(IndexSyntacticActivity), Arg.Any<IndexInput>(), Arg.Any<WorkflowTaskOptions>())
            .Returns(_ => RegisterIndexTask(nameof(IndexSyntacticActivity), syntacticTask.Task));
        context.CallActivityAsync<IndexResult>(
                nameof(IndexSemanticActivity), Arg.Any<IndexInput>(), Arg.Any<WorkflowTaskOptions>())
            .Returns(_ => RegisterIndexTask(nameof(IndexSemanticActivity), semanticTask.Task));
        context.CallActivityAsync<IndexResult>(
                nameof(IndexGraphActivity), Arg.Any<IndexInput>(), Arg.Any<WorkflowTaskOptions>())
            .Returns(_ => RegisterIndexTask(nameof(IndexGraphActivity), graphTask.Task));
        context.CallActivityAsync<ConsistencyResult>(
                nameof(VerifyConsistencyActivity), Arg.Any<ConsistencyInput>(), Arg.Any<WorkflowTaskOptions>())
            .Returns(_ =>
            {
                callLog.Add(nameof(VerifyConsistencyActivity));
                return Task.FromResult(new ConsistencyResult(true, true, true));
            });
        context.CallActivityAsync<bool>(
                nameof(SaveDedupKeyActivity), Arg.Any<DedupKeyInput>(), Arg.Any<WorkflowTaskOptions>())
            .Returns(_ =>
            {
                callLog.Add(nameof(SaveDedupKeyActivity));
                return Task.FromResult(true);
            });

        IngestionWorkflow workflow = new();

        Task<IngestionResult> runTask = workflow.RunAsync(context, input);

        await allIndexTasksScheduled.Task.WaitAsync(TimeSpan.FromSeconds(1));

        callLog.ShouldContain(nameof(IndexSyntacticActivity));
        callLog.ShouldContain(nameof(IndexSemanticActivity));
        callLog.ShouldContain(nameof(IndexGraphActivity));
        callLog.ShouldNotContain(nameof(VerifyConsistencyActivity));

        syntacticTask.SetResult(new IndexResult("syntactic", TestGuid.ToString(), input.TenantId));
        semanticTask.SetResult(new IndexResult("semantic", TestGuid.ToString(), input.TenantId));
        graphTask.SetResult(new IndexResult("graph", TestGuid.ToString(), input.TenantId));

        IngestionResult result = await runTask;

        await context.Received().CallActivityAsync<IndexResult>(
            nameof(IndexSyntacticActivity), Arg.Any<IndexInput>(), Arg.Any<WorkflowTaskOptions>());
        await context.Received().CallActivityAsync<IndexResult>(
            nameof(IndexSemanticActivity), Arg.Any<IndexInput>(), Arg.Any<WorkflowTaskOptions>());
        await context.Received().CallActivityAsync<IndexResult>(
            nameof(IndexGraphActivity), Arg.Any<IndexInput>(), Arg.Any<WorkflowTaskOptions>());
        result.Status.ShouldBe(MemoryUnitStatus.Indexed);
    }

    // --- AC2: Consistency verification ---

    [Fact]
    public async Task RunAsync_AllBackendsPresent_ShouldReturnNullConsistencyNote()
    {
        IngestionInput input = IngestionInputFactory.Create();
        WorkflowContext context = CreateMockContext();
        SetupHappyPathActivities(context, input);
        IngestionWorkflow workflow = new();

        IngestionResult result = await workflow.RunAsync(context, input);

        result.ConsistencyNote.ShouldBeNull();
    }

    [Fact]
    public async Task RunAsync_MissingBackend_ShouldReturnConsistencyNote()
    {
        IngestionInput input = IngestionInputFactory.Create();
        WorkflowContext context = CreateMockContext();
        SetupHappyPathActivities(context, input,
            consistency: new ConsistencyResult(true, true, false));
        IngestionWorkflow workflow = new();

        IngestionResult result = await workflow.RunAsync(context, input);

        result.Status.ShouldBe(MemoryUnitStatus.Indexed);
        result.ConsistencyNote.ShouldNotBeNull();
        result.ConsistencyNote.ShouldContain("graph");
    }

    // --- AC3: Saga compensation ---

    [Fact]
    public async Task RunAsync_SemanticFails_ShouldOnlyCleanupCompletedBackends()
    {
        IngestionInput input = IngestionInputFactory.Create();
        WorkflowContext context = CreateMockContext();
        SetupPreIndexActivities(context, input);

        // Syntactic succeeds, semantic fails (faulted task), graph succeeds
        context.CallActivityAsync<IndexResult>(
                nameof(IndexSyntacticActivity), Arg.Any<IndexInput>(), Arg.Any<WorkflowTaskOptions>())
            .Returns(new IndexResult("syntactic", TestGuid.ToString(), input.TenantId));
        context.CallActivityAsync<IndexResult>(
                nameof(IndexSemanticActivity), Arg.Any<IndexInput>(), Arg.Any<WorkflowTaskOptions>())
            .Returns(Task.FromException<IndexResult>(new InvalidOperationException("Semantic indexing failed")));
        context.CallActivityAsync<IndexResult>(
                nameof(IndexGraphActivity), Arg.Any<IndexInput>(), Arg.Any<WorkflowTaskOptions>())
            .Returns(new IndexResult("graph", TestGuid.ToString(), input.TenantId));

        // Compensation activities
        context.CallActivityAsync<bool>(nameof(CleanupSyntacticActivity), Arg.Any<CleanupInput>(), Arg.Any<WorkflowTaskOptions>())
            .Returns(true);
        context.CallActivityAsync<bool>(nameof(CleanupGraphActivity), Arg.Any<CleanupInput>(), Arg.Any<WorkflowTaskOptions>())
            .Returns(true);

        IngestionWorkflow workflow = new();

        await Should.ThrowAsync<InvalidOperationException>(
            () => workflow.RunAsync(context, input));

        // Syntactic and Graph were cleaned up, Semantic was NOT
        await context.Received().CallActivityAsync<bool>(
            nameof(CleanupSyntacticActivity), Arg.Any<CleanupInput>(), Arg.Any<WorkflowTaskOptions>());
        await context.Received().CallActivityAsync<bool>(
            nameof(CleanupGraphActivity), Arg.Any<CleanupInput>(), Arg.Any<WorkflowTaskOptions>());
        await context.DidNotReceive().CallActivityAsync<bool>(
            nameof(CleanupSemanticActivity), Arg.Any<CleanupInput>(), Arg.Any<WorkflowTaskOptions>());
    }

    [Fact]
    public async Task RunAsync_AllIndexingFails_ShouldNotCallAnyCleanup()
    {
        IngestionInput input = IngestionInputFactory.Create();
        WorkflowContext context = CreateMockContext();
        SetupPreIndexActivities(context, input);

        // All three fail (faulted tasks)
        context.CallActivityAsync<IndexResult>(
                nameof(IndexSyntacticActivity), Arg.Any<IndexInput>(), Arg.Any<WorkflowTaskOptions>())
            .Returns(Task.FromException<IndexResult>(new InvalidOperationException("Syntactic failed")));
        context.CallActivityAsync<IndexResult>(
                nameof(IndexSemanticActivity), Arg.Any<IndexInput>(), Arg.Any<WorkflowTaskOptions>())
            .Returns(Task.FromException<IndexResult>(new InvalidOperationException("Semantic failed")));
        context.CallActivityAsync<IndexResult>(
                nameof(IndexGraphActivity), Arg.Any<IndexInput>(), Arg.Any<WorkflowTaskOptions>())
            .Returns(Task.FromException<IndexResult>(new InvalidOperationException("Graph failed")));

        IngestionWorkflow workflow = new();

        await Should.ThrowAsync<InvalidOperationException>(
            () => workflow.RunAsync(context, input));

        await context.DidNotReceive().CallActivityAsync<bool>(
            nameof(CleanupSyntacticActivity), Arg.Any<CleanupInput>(), Arg.Any<WorkflowTaskOptions>());
        await context.DidNotReceive().CallActivityAsync<bool>(
            nameof(CleanupSemanticActivity), Arg.Any<CleanupInput>(), Arg.Any<WorkflowTaskOptions>());
        await context.DidNotReceive().CallActivityAsync<bool>(
            nameof(CleanupGraphActivity), Arg.Any<CleanupInput>(), Arg.Any<WorkflowTaskOptions>());
    }

    [Fact]
    public async Task RunAsync_CleanupFailure_ShouldStillAttemptRemainingCleanup()
    {
        IngestionInput input = IngestionInputFactory.Create();
        WorkflowContext context = CreateMockContext();
        SetupPreIndexActivities(context, input);

        context.CallActivityAsync<IndexResult>(
                nameof(IndexSyntacticActivity), Arg.Any<IndexInput>(), Arg.Any<WorkflowTaskOptions>())
            .Returns(new IndexResult("syntactic", TestGuid.ToString(), input.TenantId));
        context.CallActivityAsync<IndexResult>(
                nameof(IndexSemanticActivity), Arg.Any<IndexInput>(), Arg.Any<WorkflowTaskOptions>())
            .Returns(Task.FromException<IndexResult>(new InvalidOperationException("Semantic indexing failed")));
        context.CallActivityAsync<IndexResult>(
                nameof(IndexGraphActivity), Arg.Any<IndexInput>(), Arg.Any<WorkflowTaskOptions>())
            .Returns(new IndexResult("graph", TestGuid.ToString(), input.TenantId));

        context.CallActivityAsync<bool>(
                nameof(CleanupSyntacticActivity), Arg.Any<CleanupInput>(), Arg.Any<WorkflowTaskOptions>())
            .Returns(Task.FromException<bool>(new InvalidOperationException("Cleanup failed")));
        context.CallActivityAsync<bool>(
                nameof(CleanupGraphActivity), Arg.Any<CleanupInput>(), Arg.Any<WorkflowTaskOptions>())
            .Returns(true);

        IngestionWorkflow workflow = new();

        await Should.ThrowAsync<InvalidOperationException>(
            () => workflow.RunAsync(context, input));

        await context.Received().CallActivityAsync<bool>(
            nameof(CleanupSyntacticActivity), Arg.Any<CleanupInput>(), Arg.Any<WorkflowTaskOptions>());
        await context.Received().CallActivityAsync<bool>(
            nameof(CleanupGraphActivity), Arg.Any<CleanupInput>(), Arg.Any<WorkflowTaskOptions>());
    }

    [Fact]
    public async Task RunAsync_IndexingFailure_ShouldAttachFailureDetails()
    {
        IngestionInput input = IngestionInputFactory.Create();
        WorkflowContext context = CreateMockContext();
        SetupPreIndexActivities(context, input);

        context.CallActivityAsync<IndexResult>(
                nameof(IndexSyntacticActivity), Arg.Any<IndexInput>(), Arg.Any<WorkflowTaskOptions>())
            .Returns(new IndexResult("syntactic", TestGuid.ToString(), input.TenantId));
        context.CallActivityAsync<IndexResult>(
                nameof(IndexSemanticActivity), Arg.Any<IndexInput>(), Arg.Any<WorkflowTaskOptions>())
            .Returns(Task.FromException<IndexResult>(new InvalidOperationException("Semantic indexing failed")));
        context.CallActivityAsync<IndexResult>(
                nameof(IndexGraphActivity), Arg.Any<IndexInput>(), Arg.Any<WorkflowTaskOptions>())
            .Returns(new IndexResult("graph", TestGuid.ToString(), input.TenantId));

        context.CallActivityAsync<bool>(
                nameof(CleanupSyntacticActivity), Arg.Any<CleanupInput>(), Arg.Any<WorkflowTaskOptions>())
            .Returns(true);
        context.CallActivityAsync<bool>(
                nameof(CleanupGraphActivity), Arg.Any<CleanupInput>(), Arg.Any<WorkflowTaskOptions>())
            .Returns(true);

        IngestionWorkflow workflow = new();

        InvalidOperationException ex = await Should.ThrowAsync<InvalidOperationException>(
            () => workflow.RunAsync(context, input));

        ex.Data[nameof(FailureDetails)].ShouldBeOfType<FailureDetails>();
        FailureDetails details = (FailureDetails)ex.Data[nameof(FailureDetails)]!;
        details.Stage.ShouldBe("indexing");
        details.ErrorCode.ShouldBe(nameof(InvalidOperationException));
        details.RetryCount.ShouldBe(5);
        ex.Data[nameof(MemoryUnitStatus)].ShouldBe(MemoryUnitStatus.Failed);
        ex.Data["MemoryUnitId"].ShouldBe(TestGuid.ToString());
    }

    [Fact]
    public async Task RunAsync_UrlFetchFailure_ShouldPreserveUrlFetchErrorCode()
    {
        IngestionInput input = IngestionInputFactory.Create(sourceType: SourceType.Url, sourceUri: "https://example.com/doc", contentBytes: null);
        WorkflowContext context = CreateMockContext();
        SetupPreIndexActivities(context, input);
        context.CallActivityAsync<UrlFetchResult>(
                nameof(FetchUrlActivity), Arg.Any<FetchUrlInput>(), Arg.Any<WorkflowTaskOptions>())
            .Returns(Task.FromException<UrlFetchResult>(new UrlFetchException("URL_TIMEOUT", "URL fetch timed out after 30s.")));

        IngestionWorkflow workflow = new();

        UrlFetchException ex = await Should.ThrowAsync<UrlFetchException>(
            () => workflow.RunAsync(context, input));

        FailureDetails details = (FailureDetails)ex.Data[nameof(FailureDetails)]!;
        details.Stage.ShouldBe("fetching");
        details.ErrorCode.ShouldBe("URL_TIMEOUT");
        details.RetryCount.ShouldBe(5);
    }

    // --- AC: Activity recording resilience ---

    [Fact]
    public async Task RunAsync_ActivityRecordingFailure_ShouldStillSucceed()
    {
        IngestionInput input = IngestionInputFactory.Create();
        WorkflowContext context = CreateMockContext();
        SetupHappyPathActivities(context, input);

        // Override: activity recording throws
        context.CallActivityAsync<bool>(
                nameof(RecordCaseActivityActivity), Arg.Any<CaseActivityInput>())
            .Returns(Task.FromException<bool>(new RedisConnectionException(ConnectionFailureType.UnableToConnect, "Connection refused")));

        IngestionWorkflow workflow = new();

        IngestionResult result = await workflow.RunAsync(context, input);

        result.Status.ShouldBe(MemoryUnitStatus.Indexed);
        result.WasDuplicate.ShouldBeFalse();
    }

    [Fact]
    public async Task RunAsync_IndexingFailsAndActivityRecordingFails_ShouldPropagateOriginalException()
    {
        IngestionInput input = IngestionInputFactory.Create();
        WorkflowContext context = CreateMockContext();
        SetupPreIndexActivities(context, input);

        // Indexing fails
        context.CallActivityAsync<IndexResult>(
                nameof(IndexSyntacticActivity), Arg.Any<IndexInput>(), Arg.Any<WorkflowTaskOptions>())
            .Returns(new IndexResult("syntactic", TestGuid.ToString(), input.TenantId));
        context.CallActivityAsync<IndexResult>(
                nameof(IndexSemanticActivity), Arg.Any<IndexInput>(), Arg.Any<WorkflowTaskOptions>())
            .Returns(Task.FromException<IndexResult>(new InvalidOperationException("Semantic indexing failed")));
        context.CallActivityAsync<IndexResult>(
                nameof(IndexGraphActivity), Arg.Any<IndexInput>(), Arg.Any<WorkflowTaskOptions>())
            .Returns(new IndexResult("graph", TestGuid.ToString(), input.TenantId));

        // Compensation
        context.CallActivityAsync<bool>(nameof(CleanupSyntacticActivity), Arg.Any<CleanupInput>(), Arg.Any<WorkflowTaskOptions>())
            .Returns(true);
        context.CallActivityAsync<bool>(nameof(CleanupGraphActivity), Arg.Any<CleanupInput>(), Arg.Any<WorkflowTaskOptions>())
            .Returns(true);

        // Activity recording also fails
        context.CallActivityAsync<bool>(
                nameof(RecordCaseActivityActivity), Arg.Any<CaseActivityInput>())
            .Returns(Task.FromException<bool>(new RedisConnectionException(ConnectionFailureType.UnableToConnect, "Redis down")));

        IngestionWorkflow workflow = new();

        InvalidOperationException ex = await Should.ThrowAsync<InvalidOperationException>(
            () => workflow.RunAsync(context, input));

        // Original exception propagates, not the activity recording one
        ex.Message.ShouldBe("Semantic indexing failed");
    }

    // --- AC4: Provenance tracking ---

    [Fact]
    public async Task RunAsync_ShouldSetIngestedAtFromWorkflowContext()
    {
        IngestionInput input = IngestionInputFactory.Create();
        WorkflowContext context = CreateMockContext();
        SetupHappyPathActivities(context, input);
        IngestionWorkflow workflow = new();

        IngestionResult result = await workflow.RunAsync(context, input);

        result.IngestedAt.UtcDateTime.ShouldBe(TestTimestamp);
    }

    // Story 5.5 AC6 / FR70 — unit-level fallback for the FR70 golden-path integration test
    // (per Task 6.1 integration-tests deferral pattern): asserts that the ingestion workflow
    // threads EmbeddingResult.Model through to IndexInput.EmbeddingModel so IndexSyntacticActivity
    // can persist it to Redis.
    [Fact]
    public async Task RunAsync_ShouldPropagateEmbeddingModelFromEmbeddingResultToIndexInput()
    {
        IngestionInput input = IngestionInputFactory.Create();
        WorkflowContext context = CreateMockContext();
        SetupHappyPathActivities(context, input);
        IngestionWorkflow workflow = new();

        await workflow.RunAsync(context, input);

        await context.Received().CallActivityAsync<IndexResult>(
            nameof(IndexSyntacticActivity),
            Arg.Is<IndexInput>(i => i.EmbeddingModel == "gemini-embedding-001"),
            Arg.Any<WorkflowTaskOptions>());
    }

    [Fact]
    public async Task RunAsync_WhenEmbeddingResultModelMissing_ShouldDeriveModelFromCompoundProvider()
    {
        IngestionInput input = IngestionInputFactory.Create();
        WorkflowContext context = CreateMockContext();
        SetupHappyPathActivities(context, input);
        context.CallActivityAsync<EmbeddingResult>(
                nameof(GenerateEmbeddingActivity), Arg.Any<EmbeddingInput>(), Arg.Any<WorkflowTaskOptions>())
            .Returns(_ => Task.FromResult(new EmbeddingResult([0.1f, 0.2f, 0.3f], "google:gemini-embedding-001", 3)));
        IngestionWorkflow workflow = new();

        await workflow.RunAsync(context, input);

        await context.Received().CallActivityAsync<IndexResult>(
            nameof(IndexSyntacticActivity),
            Arg.Is<IndexInput>(i => i.EmbeddingModel == "gemini-embedding-001"),
            Arg.Any<WorkflowTaskOptions>());
    }

    [Fact]
    public async Task RunAsync_ShouldPropagateProvenanceToIndexActivities()
    {
        IngestionInput input = IngestionInputFactory.Create(ingestedBy: "reviewer@example.com");
        WorkflowContext context = CreateMockContext();
        SetupHappyPathActivities(context, input);
        IngestionWorkflow workflow = new();

        await workflow.RunAsync(context, input);

        await context.Received().CallActivityAsync<IndexResult>(
            nameof(IndexGraphActivity),
            Arg.Is<IndexInput>(i =>
                i.IngestedBy == input.IngestedBy &&
                i.IngestedAt == new DateTimeOffset(TestTimestamp, TimeSpan.Zero)),
            Arg.Any<WorkflowTaskOptions>());
    }

    // --- AC6: Duplicate detection ---

    [Fact]
    public async Task RunAsync_DuplicateSource_ShouldReturnEarlyWithExistingId()
    {
        IngestionInput input = IngestionInputFactory.Create();
        WorkflowContext context = CreateMockContext();

        // CheckIdempotency returns duplicate
        context.CallActivityAsync<IdempotencyResult>(
                nameof(CheckIdempotencyActivity), Arg.Any<IdempotencyInput>(), Arg.Any<WorkflowTaskOptions>())
            .Returns(new IdempotencyResult(true, "mu-existing"));

        IngestionWorkflow workflow = new();

        IngestionResult result = await workflow.RunAsync(context, input);

        result.WasDuplicate.ShouldBeTrue();
        result.MemoryUnitId.ShouldBe("mu-existing");
        result.Status.ShouldBe(MemoryUnitStatus.Indexed);

        // Validate and Extract should NOT have been called
        await context.DidNotReceive().CallActivityAsync<ValidateResult>(
            nameof(ValidateContentActivity), Arg.Any<IngestionInput>());
        await context.DidNotReceive().CallActivityAsync<ExtractionResult>(
            nameof(ExtractContentActivity), Arg.Any<ExtractionInput>(), Arg.Any<WorkflowTaskOptions>());
    }

    [Fact]
    public async Task RunAsync_SuccessfulIngestion_ShouldCallSaveDedupKeyActivity()
    {
        IngestionInput input = IngestionInputFactory.Create();
        WorkflowContext context = CreateMockContext();
        SetupHappyPathActivities(context, input);
        IngestionWorkflow workflow = new();

        await workflow.RunAsync(context, input);

        await context.Received().CallActivityAsync<bool>(
            nameof(SaveDedupKeyActivity), Arg.Any<DedupKeyInput>(), Arg.Any<WorkflowTaskOptions>());
    }

    [Fact]
    public async Task RunAsync_SaveDedupKeyFailure_ShouldRollbackAllIndexedBackends()
    {
        IngestionInput input = IngestionInputFactory.Create();
        WorkflowContext context = CreateMockContext();
        SetupHappyPathActivities(context, input);

        context.CallActivityAsync<bool>(
                nameof(SaveDedupKeyActivity), Arg.Any<DedupKeyInput>(), Arg.Any<WorkflowTaskOptions>())
            .Returns(Task.FromException<bool>(new InvalidOperationException("Dedup save failed")));
        context.CallActivityAsync<bool>(
                nameof(CleanupSyntacticActivity), Arg.Any<CleanupInput>(), Arg.Any<WorkflowTaskOptions>())
            .Returns(true);
        context.CallActivityAsync<bool>(
                nameof(CleanupSemanticActivity), Arg.Any<CleanupInput>(), Arg.Any<WorkflowTaskOptions>())
            .Returns(true);
        context.CallActivityAsync<bool>(
                nameof(CleanupGraphActivity), Arg.Any<CleanupInput>(), Arg.Any<WorkflowTaskOptions>())
            .Returns(true);

        IngestionWorkflow workflow = new();

        InvalidOperationException ex = await Should.ThrowAsync<InvalidOperationException>(
            () => workflow.RunAsync(context, input));

        FailureDetails details = (FailureDetails)ex.Data[nameof(FailureDetails)]!;
        details.Stage.ShouldBe("dedup");

        await context.Received().CallActivityAsync<bool>(
            nameof(CleanupSyntacticActivity), Arg.Any<CleanupInput>(), Arg.Any<WorkflowTaskOptions>());
        await context.Received().CallActivityAsync<bool>(
            nameof(CleanupSemanticActivity), Arg.Any<CleanupInput>(), Arg.Any<WorkflowTaskOptions>());
        await context.Received().CallActivityAsync<bool>(
            nameof(CleanupGraphActivity), Arg.Any<CleanupInput>(), Arg.Any<WorkflowTaskOptions>());
    }

    [Fact]
    public async Task RunAsync_VerifyConsistencyFailure_ShouldAttachVerificationStage()
    {
        IngestionInput input = IngestionInputFactory.Create();
        WorkflowContext context = CreateMockContext();
        SetupPreIndexActivities(context, input);

        context.CallActivityAsync<IndexResult>(
                nameof(IndexSyntacticActivity), Arg.Any<IndexInput>(), Arg.Any<WorkflowTaskOptions>())
            .Returns(new IndexResult("syntactic", TestGuid.ToString(), input.TenantId));
        context.CallActivityAsync<IndexResult>(
                nameof(IndexSemanticActivity), Arg.Any<IndexInput>(), Arg.Any<WorkflowTaskOptions>())
            .Returns(new IndexResult("semantic", TestGuid.ToString(), input.TenantId));
        context.CallActivityAsync<IndexResult>(
                nameof(IndexGraphActivity), Arg.Any<IndexInput>(), Arg.Any<WorkflowTaskOptions>())
            .Returns(new IndexResult("graph", TestGuid.ToString(), input.TenantId));
        context.CallActivityAsync<ConsistencyResult>(
                nameof(VerifyConsistencyActivity), Arg.Any<ConsistencyInput>(), Arg.Any<WorkflowTaskOptions>())
            .Returns(Task.FromException<ConsistencyResult>(new InvalidOperationException("Verification failed")));
        context.CallActivityAsync<bool>(
                nameof(CleanupSyntacticActivity), Arg.Any<CleanupInput>(), Arg.Any<WorkflowTaskOptions>())
            .Returns(true);
        context.CallActivityAsync<bool>(
                nameof(CleanupSemanticActivity), Arg.Any<CleanupInput>(), Arg.Any<WorkflowTaskOptions>())
            .Returns(true);
        context.CallActivityAsync<bool>(
                nameof(CleanupGraphActivity), Arg.Any<CleanupInput>(), Arg.Any<WorkflowTaskOptions>())
            .Returns(true);

        IngestionWorkflow workflow = new();

        InvalidOperationException ex = await Should.ThrowAsync<InvalidOperationException>(
            () => workflow.RunAsync(context, input));

        FailureDetails details = (FailureDetails)ex.Data[nameof(FailureDetails)]!;
        details.Stage.ShouldBe("verifying");
    }

    // --- Helpers ---

    private static WorkflowContext CreateMockContext()
    {
        WorkflowContext context = Substitute.For<WorkflowContext>();
        context.NewGuid().Returns(TestGuid);
        context.CurrentUtcDateTime.Returns(TestTimestamp);
        context.CreateReplaySafeLogger<IngestionWorkflow>()
            .Returns(Substitute.For<ILogger>());
        return context;
    }

    private static void SetupPreIndexActivities(WorkflowContext context, IngestionInput input, List<string>? callLog = null)
    {
        // CheckIdempotency — not a duplicate
        context.CallActivityAsync<IdempotencyResult>(
                nameof(CheckIdempotencyActivity), Arg.Any<IdempotencyInput>(), Arg.Any<WorkflowTaskOptions>())
            .Returns(_ =>
            {
                callLog?.Add(nameof(CheckIdempotencyActivity));
                return Task.FromResult(new IdempotencyResult(false, null));
            });

        // Validate
        context.CallActivityAsync<ValidateResult>(
                nameof(ValidateContentActivity), Arg.Any<IngestionInput>())
            .Returns(_ =>
            {
                callLog?.Add(nameof(ValidateContentActivity));
                return Task.FromResult(new ValidateResult(true, null));
            });

        // Extract
        context.CallActivityAsync<ExtractionResult>(
                nameof(ExtractContentActivity), Arg.Any<ExtractionInput>(), Arg.Any<WorkflowTaskOptions>())
            .Returns(_ =>
            {
                callLog?.Add(nameof(ExtractContentActivity));
                return Task.FromResult(new ExtractionResult("Extracted text content", "abc123hash", new DateTimeOffset(TestTimestamp)));
            });

        // Embed
        context.CallActivityAsync<EmbeddingResult>(
                nameof(GenerateEmbeddingActivity), Arg.Any<EmbeddingInput>(), Arg.Any<WorkflowTaskOptions>())
            .Returns(_ =>
            {
                callLog?.Add(nameof(GenerateEmbeddingActivity));
                return Task.FromResult(new EmbeddingResult([0.1f, 0.2f, 0.3f], "google:text-embedding-004", 3)
                {
                    Model = "gemini-embedding-001",
                });
            });

        // Record activity (best-effort, needed for both success and failure paths)
        context.CallActivityAsync<bool>(
                nameof(RecordCaseActivityActivity), Arg.Any<CaseActivityInput>())
            .Returns(_ =>
            {
                callLog?.Add(nameof(RecordCaseActivityActivity));
                return Task.FromResult(true);
            });
    }

    private static void SetupHappyPathActivities(
        WorkflowContext context,
        IngestionInput input,
        ConsistencyResult? consistency = null,
        List<string>? callLog = null)
    {
        SetupPreIndexActivities(context, input, callLog);

        string muId = TestGuid.ToString();

        // Index activities
        context.CallActivityAsync<IndexResult>(
                nameof(IndexSyntacticActivity), Arg.Any<IndexInput>(), Arg.Any<WorkflowTaskOptions>())
            .Returns(_ =>
            {
                callLog?.Add(nameof(IndexSyntacticActivity));
                return Task.FromResult(new IndexResult("syntactic", muId, input.TenantId));
            });
        context.CallActivityAsync<IndexResult>(
                nameof(IndexSemanticActivity), Arg.Any<IndexInput>(), Arg.Any<WorkflowTaskOptions>())
            .Returns(_ =>
            {
                callLog?.Add(nameof(IndexSemanticActivity));
                return Task.FromResult(new IndexResult("semantic", muId, input.TenantId));
            });
        context.CallActivityAsync<IndexResult>(
                nameof(IndexGraphActivity), Arg.Any<IndexInput>(), Arg.Any<WorkflowTaskOptions>())
            .Returns(_ =>
            {
                callLog?.Add(nameof(IndexGraphActivity));
                return Task.FromResult(new IndexResult("graph", muId, input.TenantId));
            });

        // Verify consistency
        context.CallActivityAsync<ConsistencyResult>(
                nameof(VerifyConsistencyActivity), Arg.Any<ConsistencyInput>(), Arg.Any<WorkflowTaskOptions>())
            .Returns(_ =>
            {
                callLog?.Add(nameof(VerifyConsistencyActivity));
                return Task.FromResult(consistency ?? new ConsistencyResult(true, true, true));
            });

        // Save dedup key
        context.CallActivityAsync<bool>(
                nameof(SaveDedupKeyActivity), Arg.Any<DedupKeyInput>(), Arg.Any<WorkflowTaskOptions>())
            .Returns(_ =>
            {
                callLog?.Add(nameof(SaveDedupKeyActivity));
                return Task.FromResult(true);
            });

        // Record activity (best-effort, no retry options)
        context.CallActivityAsync<bool>(
                nameof(RecordCaseActivityActivity), Arg.Any<CaseActivityInput>())
            .Returns(_ =>
            {
                callLog?.Add(nameof(RecordCaseActivityActivity));
                return Task.FromResult(true);
            });
    }

    // ==================================================================================
    // Story 5.6 AC5 — Retry policy regression guard (NFR22).
    // These tests pin retry values so a future diff cannot silently weaken the policy.
    // ==================================================================================

    [Fact]
    public void IngestionWorkflow_MainRetryAttempts_ShouldBePinnedAtFive()
    {
        System.Reflection.FieldInfo? field = typeof(IngestionWorkflow).GetField(
            "_mainRetryAttempts",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);

        field.ShouldNotBeNull();
        field!.GetRawConstantValue().ShouldBe(5);
    }

    [Fact]
    public void IngestionWorkflow_CompensationRetryAttempts_ShouldBePinnedAtThree()
    {
        System.Reflection.FieldInfo? field = typeof(IngestionWorkflow).GetField(
            "_compensationRetryAttempts",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);

        field.ShouldNotBeNull();
        field!.GetRawConstantValue().ShouldBe(3);
    }

    [Fact]
    public void IngestionWorkflow_MainRetryPolicy_ShouldPinIntervalsAndCoefficient()
    {
        // Invokes the internal CreateMainRetry() helper introduced by Story 5.6 and asserts the
        // WorkflowRetryPolicy values. If this fails, NFR22 has been silently weakened.
        System.Reflection.MethodInfo? method = typeof(IngestionWorkflow).GetMethod(
            "CreateMainRetry",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);

        method.ShouldNotBeNull();
        object? options = method!.Invoke(null, null);
        options.ShouldNotBeNull();

        WorkflowTaskOptions taskOptions = (WorkflowTaskOptions)options!;
        taskOptions.RetryPolicy.ShouldNotBeNull();

        WorkflowRetryPolicy policy = taskOptions.RetryPolicy!;
        policy.MaxNumberOfAttempts.ShouldBe(5);
        policy.FirstRetryInterval.ShouldBe(TimeSpan.FromSeconds(2));
        policy.BackoffCoefficient.ShouldBe(1.5);
        policy.MaxRetryInterval.ShouldBe(TimeSpan.FromMinutes(5));
    }

    [Fact]
    public void IngestionWorkflow_CompensationRetryPolicy_ShouldPinIntervalsAndCoefficient()
    {
        System.Reflection.MethodInfo? method = typeof(IngestionWorkflow).GetMethod(
            "CreateCompensationRetry",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);

        method.ShouldNotBeNull();
        WorkflowTaskOptions taskOptions = (WorkflowTaskOptions)method!.Invoke(null, null)!;
        WorkflowRetryPolicy policy = taskOptions.RetryPolicy!;

        policy.MaxNumberOfAttempts.ShouldBe(3);
        policy.FirstRetryInterval.ShouldBe(TimeSpan.FromSeconds(1));
        policy.BackoffCoefficient.ShouldBe(2.0);
        policy.MaxRetryInterval.ShouldBe(TimeSpan.FromSeconds(30));
    }

    [Fact]
    public async Task RunAsync_DimensionMismatchFailure_ShouldStillUseMainRetryPolicy()
    {
        IngestionInput input = IngestionInputFactory.Create();
        WorkflowContext context = CreateMockContext();
        SetupPreIndexActivities(context, input);

        context.CallActivityAsync<EmbeddingResult>(
                nameof(GenerateEmbeddingActivity), Arg.Any<EmbeddingInput>(), Arg.Any<WorkflowTaskOptions>())
            .Returns(Task.FromException<EmbeddingResult>(new SemanticSearchDimensionMismatchException(384, 768)));

        IngestionWorkflow workflow = new();

        SemanticSearchDimensionMismatchException ex = await Should.ThrowAsync<SemanticSearchDimensionMismatchException>(
            () => workflow.RunAsync(context, input));

        FailureDetails details = (FailureDetails)ex.Data[nameof(FailureDetails)]!;
        details.Stage.ShouldBe("embedding");
        details.RetryCount.ShouldBe(5);

        await context.Received().CallActivityAsync<EmbeddingResult>(
            nameof(GenerateEmbeddingActivity),
            Arg.Any<EmbeddingInput>(),
            Arg.Is<WorkflowTaskOptions>(options =>
                options.RetryPolicy != null
                && options.RetryPolicy.MaxNumberOfAttempts == 5
                && options.RetryPolicy.FirstRetryInterval == TimeSpan.FromSeconds(2)
                && options.RetryPolicy.BackoffCoefficient == 1.5
                && options.RetryPolicy.MaxRetryInterval == TimeSpan.FromMinutes(5)));
    }
}
