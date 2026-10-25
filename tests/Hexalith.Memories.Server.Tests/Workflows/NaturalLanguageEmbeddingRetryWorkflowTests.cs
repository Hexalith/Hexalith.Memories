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
    public async Task RunAsync_LegacyRawInputNeverPropagatesIntoDurableActivityHistory()
    {
        WorkflowContext context = Substitute.For<WorkflowContext>();
        var result = await new NaturalLanguageEmbeddingRetryWorkflow().RunAsync(context, CreateInput());
        result.Indexed.ShouldBeFalse();
        result.Reason.ShouldBe("legacy-retry-input-requires-restart");
        context.ReceivedCalls().ShouldBeEmpty();
    }

    [Theory]
    [InlineData(true, null)]
    [InlineData(false, "memory-unit-deleted-during-retry")]
    public async Task RunAsync_ContentFreeInputDelegatesOnlyToFencedRetryActivity(bool indexed, string? reason)
    {
        WorkflowContext context = Substitute.For<WorkflowContext>();
        var input = CreateInput() with { RawJsonPayload = string.Empty };
        context.CallActivityAsync<NaturalLanguageEmbeddingRetryResult>(
            nameof(Hexalith.Memories.Server.NaturalLanguage.RetryNaturalLanguageEmbeddingActivity), input)
            .Returns(new NaturalLanguageEmbeddingRetryResult(indexed, reason));
        var result = await new NaturalLanguageEmbeddingRetryWorkflow().RunAsync(context, input);
        result.Indexed.ShouldBe(indexed);
        result.Reason.ShouldBe(reason);
        context.ReceivedCalls().Count().ShouldBe(1);
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
