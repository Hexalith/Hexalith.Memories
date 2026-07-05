// <copyright file="IngestionPayloadClaimCheckTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Tests.Ingestion;

using Hexalith.Memories.Contracts.V1;
using Hexalith.Memories.Server.Ingestion;
using Hexalith.Memories.TestHelpers.Factories;

using NSubstitute;

using Shouldly;

public sealed class IngestionPayloadClaimCheckTests
{
    [Fact]
    public async Task PrepareAsync_NonUrlPayload_SavesSourceBytesAndReturnsSlimInput()
    {
        IWorkflowPayloadStore payloadStore = Substitute.For<IWorkflowPayloadStore>();
        byte[] contentBytes = [1, 2, 3, 4];
        WorkflowPayloadReference reference = new(
            "mu-1:sourcebytes:hash:source",
            "hash",
            contentBytes.Length,
            WorkflowPayloadKind.SourceBytes,
            "test-tenant",
            "mu-1");
        payloadStore
            .SaveAsync(
                "test-tenant",
                "mu-1",
                WorkflowPayloadKind.SourceBytes,
                Arg.Any<ReadOnlyMemory<byte>>(),
                "source",
                Arg.Any<CancellationToken>())
            .Returns(reference);
        IngestionInput input = IngestionInputFactory.Create(contentBytes: contentBytes);

        IngestionInput result = await IngestionPayloadClaimCheck.PrepareAsync(payloadStore, "mu-1", input);

        result.ContentBytes.ShouldBeNull();
        result.PayloadReference.ShouldBe(reference);
        await payloadStore.Received(1).SaveAsync(
            "test-tenant",
            "mu-1",
            WorkflowPayloadKind.SourceBytes,
            Arg.Is<ReadOnlyMemory<byte>>(payload => payload.ToArray().SequenceEqual(contentBytes)),
            "source",
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task PrepareAsync_UrlPayload_DoesNotClaimCheckSchedulingInput()
    {
        IWorkflowPayloadStore payloadStore = Substitute.For<IWorkflowPayloadStore>();
        IngestionInput input = IngestionInputFactory.Create(
            sourceType: SourceType.Url,
            sourceUri: "https://example.com/doc",
            contentBytes: null);

        IngestionInput result = await IngestionPayloadClaimCheck.PrepareAsync(payloadStore, "mu-1", input);

        result.ShouldBe(input);
        await payloadStore.DidNotReceiveWithAnyArgs().SaveAsync(default!, default!, default, default, default, default);
    }
}
