// <copyright file="IngestionPayloadClaimCheckTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Tests.Ingestion;

using Hexalith.Memories.Contracts.V1;
using Hexalith.Memories.Server.Ingestion;
using Hexalith.Memories.Server.NaturalLanguage;
using Hexalith.Memories.TestHelpers.Factories;

using Microsoft.Extensions.Options;

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

    [Fact]
    public async Task PrepareInputAsync_CapturesWorkflowConfigurationBeforeClaimCheckSlimming()
    {
        IWorkflowPayloadStore payloadStore = Substitute.For<IWorkflowPayloadStore>();
        byte[] contentBytes = [9, 8, 7, 6];
        WorkflowPayloadReference reference = new(
            "mu-2:sourcebytes:hash:source",
            "hash",
            contentBytes.Length,
            WorkflowPayloadKind.SourceBytes,
            "test-tenant",
            "mu-2");
        payloadStore
            .SaveAsync(
                "test-tenant",
                "mu-2",
                WorkflowPayloadKind.SourceBytes,
                Arg.Any<ReadOnlyMemory<byte>>(),
                "source",
                Arg.Any<CancellationToken>())
            .Returns(reference);
        IngestionSettings ingestionSettings = new()
        {
            RetryPolicies = new(StringComparer.Ordinal)
            {
                ["ExtractContentActivity"] = new ActivityRetryPolicy
                {
                    MaxAttempts = 2,
                    FirstRetryIntervalSeconds = 3,
                    BackoffCoefficient = 1.1,
                    MaxRetryIntervalSeconds = 30,
                },
            },
        };
        IngestionWorkflowConfigurationCapture capture = new(
            Options.Create(ingestionSettings),
            Options.Create(new NaturalLanguageDescriptionOptions { PersistInMetadata = true }));
        IngestionInput input = IngestionInputFactory.Create(contentBytes: contentBytes);

        IngestionInput result = await DaprIngestionWorkflowScheduler.PrepareInputAsync(
            payloadStore,
            "mu-2",
            input,
            capture);

        result.ContentBytes.ShouldBeNull();
        result.PayloadReference.ShouldBe(reference);
        result.WorkflowConfiguration.ShouldNotBeNull();
        result.WorkflowConfiguration.NaturalLanguage.PersistInMetadata.ShouldBeTrue();
        WorkflowActivityRetryPolicy retry = result.WorkflowConfiguration.Retry.ActivityOverrides["ExtractContentActivity"];
        retry.MaxAttempts.ShouldBe(2);
        retry.FirstRetryIntervalSeconds.ShouldBe(3);
        retry.BackoffCoefficient.ShouldBe(1.1);
        retry.MaxRetryIntervalSeconds.ShouldBe(30);
    }
}
