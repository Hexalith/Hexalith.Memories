// <copyright file="NaturalLanguageEmbeddingRetryWorkflowTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Tests.Workflows;

using Dapr.Workflow;

using Hexalith.Memories.Contracts.V1;
using Hexalith.Memories.Server.Activities.Indexing;
using Hexalith.Memories.Server.Activities.Ingestion;
using Hexalith.Memories.Server.Workflows;

using NSubstitute;

using Shouldly;

public class NaturalLanguageEmbeddingRetryWorkflowTests
{
    [Fact]
    public async Task RunAsync_MemoryUnitMissingAtRetryStart_ReturnsDeletedReasonWithoutCallingLlm()
    {
        WorkflowContext context = Substitute.For<WorkflowContext>();
        context.CallActivityAsync<bool>(
                nameof(CheckMemoryUnitExistsActivity),
                Arg.Any<ConsistencyInput>())
            .Returns(Task.FromResult(false));

        NaturalLanguageEmbeddingRetryWorkflow workflow = new();

        NaturalLanguageEmbeddingRetryResult result = await workflow.RunAsync(context, CreateInput());

        result.Indexed.ShouldBeFalse();
        result.Reason.ShouldBe("memory-unit-deleted-during-retry");

        await context.DidNotReceive().CallActivityAsync<NaturalLanguageDescriptionResult>(
            nameof(GenerateNaturalLanguageDescriptionActivity),
            Arg.Any<NaturalLanguageDescriptionInput>());
    }

    [Fact]
    public async Task RunAsync_MemoryUnitDeletedBeforeIndex_ReturnsDeletedReasonWithoutWritingNlHash()
    {
        WorkflowContext context = Substitute.For<WorkflowContext>();
        context.CallActivityAsync<bool>(
                nameof(CheckMemoryUnitExistsActivity),
                Arg.Any<ConsistencyInput>())
            .Returns(Task.FromResult(true), Task.FromResult(false));
        context.CallActivityAsync<NaturalLanguageDescriptionResult>(
                nameof(GenerateNaturalLanguageDescriptionActivity),
                Arg.Any<NaturalLanguageDescriptionInput>())
            .Returns(Task.FromResult(new NaturalLanguageDescriptionResult(
                "A business action happened.",
                null,
                ConfidenceSource.Constant,
                "llm",
                "gpt-4o-mini")));
        context.CallActivityAsync<EmbeddingResult>(
                nameof(GenerateEmbeddingActivity),
                Arg.Any<EmbeddingInput>())
            .Returns(Task.FromResult(new EmbeddingResult([0.1f, 0.2f, 0.3f], "openai", 3)
            {
                Model = "text-embedding-3-small",
            }));

        NaturalLanguageEmbeddingRetryWorkflow workflow = new();

        NaturalLanguageEmbeddingRetryResult result = await workflow.RunAsync(context, CreateInput());

        result.Indexed.ShouldBeFalse();
        result.Reason.ShouldBe("memory-unit-deleted-during-retry");

        await context.DidNotReceive().CallActivityAsync<IndexResult>(
            nameof(IndexNaturalLanguageSemanticActivity),
            Arg.Any<NaturalLanguageIndexInput>());
    }

    private static NaturalLanguageEmbeddingRetryInput CreateInput() => new(
        TenantId: "tenant-a",
        MemoryUnitId: "mu-001",
        RawJsonPayload: "{\"counter\":1}",
        EventType: "CounterIncrementedV1",
        AggregateType: "Counter",
        CaseId: "case-001",
        EmbeddingProvider: "openai",
        EmbeddingModel: "text-embedding-3-small",
        EmbeddingDimensions: 3);
}
