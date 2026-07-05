// <copyright file="QueueNaturalLanguageEmbeddingRetryActivityTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Tests.Activities.Ingestion;

using System.Text;

using Dapr.Workflow;

using Hexalith.Memories.Contracts.V1;
using Hexalith.Memories.Server.Activities.Ingestion;
using Hexalith.Memories.Server.Ingestion;
using Hexalith.Memories.Server.NaturalLanguage;

using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

using NSubstitute;

using Shouldly;

public class QueueNaturalLanguageEmbeddingRetryActivityTests
{
    [Fact]
    public void Truncate_ShortString_ReturnsUnchanged()
    {
        QueueNaturalLanguageEmbeddingRetryActivity.Truncate("{}", 4096).ShouldBe("{}");
    }

    [Fact]
    public void Truncate_LongString_TruncatesToMaxBytes()
    {
        string input = new string('A', 5000);
        string result = QueueNaturalLanguageEmbeddingRetryActivity.Truncate(input, 4096);

        Encoding.UTF8.GetByteCount(result).ShouldBeLessThanOrEqualTo(4096);
    }

    [Fact]
    public void Truncate_EmptyInput_ReturnsEmpty()
    {
        QueueNaturalLanguageEmbeddingRetryActivity.Truncate(string.Empty, 4096).ShouldBe(string.Empty);
    }

    [Fact]
    public void Truncate_NullInput_ReturnsEmpty()
    {
        QueueNaturalLanguageEmbeddingRetryActivity.Truncate(null!, 4096).ShouldBe(string.Empty);
    }

    [Fact]
    public void Truncate_MultiByteInput_DoesNotEmitReplacementCharacterOrOverflowByteBudget()
    {
        string input = string.Concat(Enumerable.Repeat("🙂", 8));

        string result = QueueNaturalLanguageEmbeddingRetryActivity.Truncate(input, 9);

        Encoding.UTF8.GetByteCount(result).ShouldBeLessThanOrEqualTo(9);
        result.ShouldNotContain('�');
        result.ShouldBe("🙂🙂");
    }

    [Fact]
    public void Truncate_ZeroBudget_ReturnsEmpty()
    {
        QueueNaturalLanguageEmbeddingRetryActivity.Truncate("payload", 0).ShouldBe(string.Empty);
    }

    [Fact]
    public async Task RunAsync_WithRawPayloadReference_EnqueuesResolvedBoundedPayload()
    {
        IFailedNaturalLanguageEmbeddingRegistry registry = Substitute.For<IFailedNaturalLanguageEmbeddingRegistry>();
        IWorkflowPayloadStore payloadStore = Substitute.For<IWorkflowPayloadStore>();
        byte[] rawPayload = Encoding.UTF8.GetBytes("{\"counterId\":\"c-1\",\"increment\":42}");
        WorkflowPayloadReference reference = new(
            "mu-1:sourcebytes:hash",
            "hash",
            rawPayload.Length,
            WorkflowPayloadKind.SourceBytes,
            "tenant-a",
            "mu-1");
        payloadStore
            .ReadAsync(reference, "tenant-a", "mu-1", WorkflowPayloadKind.SourceBytes, Arg.Any<CancellationToken>())
            .Returns(rawPayload);
        QueueNaturalLanguageEmbeddingRetryActivity activity = new(
            registry,
            Options.Create(new NaturalLanguageDescriptionOptions { QueuedPayloadMaxBytes = 20 }),
            NullLogger<QueueNaturalLanguageEmbeddingRetryActivity>.Instance,
            payloadStore);
        QueueNaturalLanguageEmbeddingRetryInput input = new(
            "tenant-a",
            "mu-1",
            string.Empty,
            "CounterIncremented",
            "Counter",
            "case-1",
            "google:text-embedding-004",
            "gemini-embedding-001",
            768,
            1234L,
            reference);

        bool result = await activity.RunAsync(Substitute.For<WorkflowActivityContext>(), input);

        result.ShouldBeTrue();
        await registry.Received(1).EnqueueAsync(
            Arg.Is<FailedNaturalLanguageEmbeddingRecord>(record =>
                record.TenantId == "tenant-a"
                && record.MemoryUnitId == "mu-1"
                && Encoding.UTF8.GetByteCount(record.TruncatedRawJsonPayload) <= 20
                && record.TruncatedRawJsonPayload == "{\"counterId\":\"c-1\",\""
                && record.QueuedAtTicks == 1234L),
            Arg.Any<CancellationToken>());
    }
}
