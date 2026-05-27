// <copyright file="IngestionWorkflowDualEmbeddingTests.cs" company="ITANEO">
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
using Hexalith.Memories.Server.NaturalLanguage;
using Hexalith.Memories.Server.Workflows;
using Hexalith.Memories.TestHelpers.Factories;

using Microsoft.Extensions.Logging;

using NSubstitute;
using NSubstitute.ExceptionExtensions;

using Shouldly;

[Collection(Hexalith.Memories.Server.Tests.Ingestion.RetryPolicyBuilderStateCollection.Name)]
public class IngestionWorkflowDualEmbeddingTests
{
    private static readonly DateTime TestTimestamp = new(2026, 3, 29, 10, 0, 0, DateTimeKind.Utc);
    private static readonly Guid TestGuid = Guid.Parse("22222222-2222-2222-2222-222222222222");

    public IngestionWorkflowDualEmbeddingTests()
    {
        RetryPolicyBuilder.ResetToDefaults();
        NaturalLanguageDescriptionOptionsSnapshot.ResetToDefaults();
    }

    [Fact]
    public async Task SourceTypeEvent_SuccessPath_SchedulesFourActivities()
    {
        IngestionInput input = CreateEventInput();
        WorkflowContext context = CreateMockContext();
        SetupHappyPathIncludingNl(context, input);
        IngestionWorkflow workflow = new();

        IngestionResult result = await workflow.RunAsync(context, input);

        result.NaturalLanguageEmbeddingStatus.ShouldBe(NaturalLanguageEmbeddingStatus.Indexed);

        // Confirm all four indexing activities were scheduled.
        await context.Received().CallActivityAsync<IndexResult>(
            nameof(IndexSyntacticActivity), Arg.Any<IndexInput>(), Arg.Any<WorkflowTaskOptions>());
        await context.Received().CallActivityAsync<IndexResult>(
            nameof(IndexSemanticActivity), Arg.Any<IndexInput>(), Arg.Any<WorkflowTaskOptions>());
        await context.Received().CallActivityAsync<IndexResult>(
            nameof(IndexGraphActivity), Arg.Any<IndexInput>(), Arg.Any<WorkflowTaskOptions>());
        await context.Received().CallActivityAsync<IndexResult>(
            nameof(IndexNaturalLanguageSemanticActivity), Arg.Any<NaturalLanguageIndexInput>(), Arg.Any<WorkflowTaskOptions>());

        // Second GenerateEmbeddingActivity call carries the NL content kind.
        await context.Received().CallActivityAsync<EmbeddingResult>(
            nameof(GenerateEmbeddingActivity),
            Arg.Is<EmbeddingInput>(e => e.ContentKind == EmbeddingContentKind.NaturalLanguageDescription),
            Arg.Any<WorkflowTaskOptions>());
    }

    [Fact]
    public async Task SourceTypeEvent_LlmUnavailable_QueuesAndProceedsWithRawEmbedding()
    {
        IngestionInput input = CreateEventInput();
        WorkflowContext context = CreateMockContext();
        SetupHappyPathIncludingNl(context, input);

        // Override NL description activity to throw → workflow catches and queues.
        context.CallActivityAsync<NaturalLanguageDescriptionResult>(
                nameof(GenerateNaturalLanguageDescriptionActivity),
                Arg.Any<NaturalLanguageDescriptionInput>(),
                Arg.Any<WorkflowTaskOptions>())
            .ThrowsAsync(new NaturalLanguageDescriptionUnavailableException("LLM outage", "component", "corr-1"));

        IngestionWorkflow workflow = new();
        IngestionResult result = await workflow.RunAsync(context, input);

        result.NaturalLanguageEmbeddingStatus.ShouldBe(NaturalLanguageEmbeddingStatus.Queued);
        result.Status.ShouldBe(MemoryUnitStatus.Indexed);

        await context.Received().CallActivityAsync<bool>(
            nameof(QueueNaturalLanguageEmbeddingRetryActivity),
            Arg.Any<QueueNaturalLanguageEmbeddingRetryInput>(),
            Arg.Any<WorkflowTaskOptions>());

        await context.Received().CallActivityAsync<IndexResult>(
            nameof(IndexSyntacticActivity),
            Arg.Is<IndexInput>(i =>
                i.Metadata.ContainsKey("event.naturalLanguageEmbeddingStatus")
                && i.Metadata["event.naturalLanguageEmbeddingStatus"].Value == NaturalLanguageEmbeddingStatus.Queued.ToString()),
            Arg.Any<WorkflowTaskOptions>());

        await context.DidNotReceive().CallActivityAsync<IndexResult>(
            nameof(IndexNaturalLanguageSemanticActivity),
            Arg.Any<NaturalLanguageIndexInput>(),
            Arg.Any<WorkflowTaskOptions>());
    }

    [Fact]
    public async Task SourceTypeFile_SkipsDualEmbeddingBranch()
    {
        IngestionInput input = IngestionInputFactory.Create(sourceType: SourceType.File);
        WorkflowContext context = CreateMockContext();
        SetupHappyPathIncludingNl(context, input);
        IngestionWorkflow workflow = new();

        IngestionResult result = await workflow.RunAsync(context, input);

        result.NaturalLanguageEmbeddingStatus.ShouldBe(NaturalLanguageEmbeddingStatus.NotApplicable);

        await context.DidNotReceive().CallActivityAsync<NaturalLanguageDescriptionResult>(
            nameof(GenerateNaturalLanguageDescriptionActivity),
            Arg.Any<NaturalLanguageDescriptionInput>(),
            Arg.Any<WorkflowTaskOptions>());
        await context.DidNotReceive().CallActivityAsync<IndexResult>(
            nameof(IndexNaturalLanguageSemanticActivity),
            Arg.Any<NaturalLanguageIndexInput>(),
            Arg.Any<WorkflowTaskOptions>());
    }

    [Fact]
    public async Task SourceTypeUrl_SkipsDualEmbeddingBranch()
    {
        IngestionInput input = IngestionInputFactory.Create(sourceType: SourceType.Url);
        WorkflowContext context = CreateMockContext();
        SetupHappyPathIncludingNl(context, input);

        // URL path reaches FetchUrlActivity first — stub it.
        byte[] fetched = Encoding.UTF8.GetBytes("fetched body");
        context.CallActivityAsync<UrlFetchResult>(
                nameof(FetchUrlActivity), Arg.Any<FetchUrlInput>(), Arg.Any<WorkflowTaskOptions>())
            .Returns(_ => Task.FromResult(new UrlFetchResult(
                fetched, "text/plain", fetched.LongLength, input.SourceUri, 200)));

        IngestionWorkflow workflow = new();
        IngestionResult result = await workflow.RunAsync(context, input);

        result.NaturalLanguageEmbeddingStatus.ShouldBe(NaturalLanguageEmbeddingStatus.NotApplicable);
        await context.DidNotReceive().CallActivityAsync<NaturalLanguageDescriptionResult>(
            nameof(GenerateNaturalLanguageDescriptionActivity),
            Arg.Any<NaturalLanguageDescriptionInput>(),
            Arg.Any<WorkflowTaskOptions>());
    }

    [Fact]
    public async Task NaturalLanguageDescriptionMetadata_PersistedOnlyWhenConfigured()
    {
        // Default: PersistInMetadata = false → no metadata injection.
        IngestionInput input = CreateEventInput();
        WorkflowContext context = CreateMockContext();
        SetupHappyPathIncludingNl(context, input);
        IngestionWorkflow workflow = new();

        _ = await workflow.RunAsync(context, input);

        await context.Received().CallActivityAsync<IndexResult>(
            nameof(IndexSyntacticActivity),
            Arg.Is<IndexInput>(i => !i.Metadata.ContainsKey("event.naturalLanguageDescription")),
            Arg.Any<WorkflowTaskOptions>());

        // Flip the snapshot flag ON.
        NaturalLanguageDescriptionOptionsSnapshot.ResetToDefaults();
        NaturalLanguageDescriptionOptionsSnapshot.Initialize(
            Microsoft.Extensions.Options.Options.Create(
                new NaturalLanguageDescriptionOptions { PersistInMetadata = true }));

        IngestionInput input2 = CreateEventInput();
        WorkflowContext context2 = CreateMockContext();
        SetupHappyPathIncludingNl(context2, input2);

        _ = await workflow.RunAsync(context2, input2);

        await context2.Received().CallActivityAsync<IndexResult>(
            nameof(IndexSyntacticActivity),
            Arg.Is<IndexInput>(i =>
                i.Metadata.ContainsKey("event.naturalLanguageDescription")
                && i.Metadata["event.naturalLanguageDescription"].Value == "A business action happened."),
            Arg.Any<WorkflowTaskOptions>());
    }

    private static IngestionInput CreateEventInput()
    {
        IngestionInput baseInput = IngestionInputFactory.Create(sourceType: SourceType.Event);
        Dictionary<string, MetadataField> metadata = new(baseInput.Metadata)
        {
            ["cloudevent.type"] = new("TestEventV1", MetadataOrigin.Ai, 1.0f),
            ["event.aggregateType"] = new("Aggregate", MetadataOrigin.Ai, 1.0f),
        };

        return baseInput with
        {
            ContentBytes = Encoding.UTF8.GetBytes("""{"foo":"bar"}"""),
            Metadata = metadata,
        };
    }

    private static WorkflowContext CreateMockContext()
    {
        WorkflowContext context = Substitute.For<WorkflowContext>();
        context.NewGuid().Returns(TestGuid);
        context.CurrentUtcDateTime.Returns(TestTimestamp);
        context.CreateReplaySafeLogger<IngestionWorkflow>()
            .Returns(Substitute.For<ILogger>());
        return context;
    }

    private static void SetupHappyPathIncludingNl(WorkflowContext context, IngestionInput input)
    {
        string muId = TestGuid.ToString();

        context.CallActivityAsync<IdempotencyResult>(
                nameof(CheckIdempotencyActivity), Arg.Any<IdempotencyInput>(), Arg.Any<WorkflowTaskOptions>())
            .Returns(_ => Task.FromResult(new IdempotencyResult(false, null)));
        context.CallActivityAsync<ValidateResult>(
                nameof(ValidateContentActivity), Arg.Any<IngestionInput>())
            .Returns(_ => Task.FromResult(new ValidateResult(true, null)));
        context.CallActivityAsync<ExtractionResult>(
                nameof(ExtractContentActivity), Arg.Any<ExtractionInput>(), Arg.Any<WorkflowTaskOptions>())
            .Returns(_ => Task.FromResult(new ExtractionResult("Extracted text", "hash", new DateTimeOffset(TestTimestamp))));
        context.CallActivityAsync<EmbeddingResult>(
                nameof(GenerateEmbeddingActivity), Arg.Any<EmbeddingInput>(), Arg.Any<WorkflowTaskOptions>())
            .Returns(_ => Task.FromResult(new EmbeddingResult([0.1f, 0.2f, 0.3f], "openai", 3)
            {
                Model = "text-embedding-3-small",
            }));

        context.CallActivityAsync<NaturalLanguageDescriptionResult>(
                nameof(GenerateNaturalLanguageDescriptionActivity),
                Arg.Any<NaturalLanguageDescriptionInput>(),
                Arg.Any<WorkflowTaskOptions>())
            .Returns(_ => Task.FromResult(new NaturalLanguageDescriptionResult(
                "A business action happened.",
                EstimatedConfidence: 0.85f,
                ConfidenceSource.Logprobs,
                LlmProvider: "llm",
                LlmModel: "gpt-4o-mini")));

        context.CallActivityAsync<bool>(
                nameof(QueueNaturalLanguageEmbeddingRetryActivity),
                Arg.Any<QueueNaturalLanguageEmbeddingRetryInput>(),
                Arg.Any<WorkflowTaskOptions>())
            .Returns(_ => Task.FromResult(true));

        context.CallActivityAsync<IndexResult>(
                nameof(IndexSyntacticActivity), Arg.Any<IndexInput>(), Arg.Any<WorkflowTaskOptions>())
            .Returns(_ => Task.FromResult(new IndexResult("syntactic", muId, input.TenantId)));
        context.CallActivityAsync<IndexResult>(
                nameof(IndexSemanticActivity), Arg.Any<IndexInput>(), Arg.Any<WorkflowTaskOptions>())
            .Returns(_ => Task.FromResult(new IndexResult("semantic", muId, input.TenantId)));
        context.CallActivityAsync<IndexResult>(
                nameof(IndexGraphActivity), Arg.Any<IndexInput>(), Arg.Any<WorkflowTaskOptions>())
            .Returns(_ => Task.FromResult(new IndexResult("graph", muId, input.TenantId)));
        context.CallActivityAsync<IndexResult>(
                nameof(IndexNaturalLanguageSemanticActivity),
                Arg.Any<NaturalLanguageIndexInput>(),
                Arg.Any<WorkflowTaskOptions>())
            .Returns(_ => Task.FromResult(new IndexResult("semantic-nl", muId, input.TenantId)));

        context.CallActivityAsync<ConsistencyResult>(
                nameof(VerifyConsistencyActivity), Arg.Any<ConsistencyInput>(), Arg.Any<WorkflowTaskOptions>())
            .Returns(_ => Task.FromResult(new ConsistencyResult(true, true, true)));
        context.CallActivityAsync<bool>(
                nameof(SaveDedupKeyActivity), Arg.Any<DedupKeyInput>(), Arg.Any<WorkflowTaskOptions>())
            .Returns(_ => Task.FromResult(true));
        context.CallActivityAsync<bool>(
                nameof(RecordCaseActivityActivity), Arg.Any<CaseActivityInput>())
            .Returns(_ => Task.FromResult(true));
        context.CallActivityAsync<bool>(
                nameof(UpdateCaseIngestionCounterActivity),
                Arg.Any<CounterTransitionInput>(),
                Arg.Any<WorkflowTaskOptions>())
            .Returns(_ => Task.FromResult(true));
    }
}
