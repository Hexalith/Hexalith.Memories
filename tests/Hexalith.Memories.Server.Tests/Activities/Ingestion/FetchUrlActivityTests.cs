// <copyright file="FetchUrlActivityTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Tests.Activities.Ingestion;

using Dapr.Workflow;

using Hexalith.Memories.Contracts.V1;
using Hexalith.Memories.Server.Activities.Ingestion;
using Hexalith.Memories.Server.Ingestion;

using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

using NSubstitute;

using Shouldly;

public class FetchUrlActivityTests
{
    [Fact]
    public async Task RunAsync_HappyPath_ReturnsFetcherResult()
    {
        IUrlContentFetcher fetcher = Substitute.For<IUrlContentFetcher>();
        UrlFetchResult expected = new([1, 2, 3], "text/plain", 3, "https://example.com/final", 200);
        fetcher.FetchAsync(Arg.Any<Uri>(), Arg.Any<CancellationToken>()).Returns(expected);

        FetchUrlActivity activity = new(fetcher, CreateGate(), NullLogger<FetchUrlActivity>.Instance);
        WorkflowActivityContext context = Substitute.For<WorkflowActivityContext>();

        UrlFetchResult result = await activity.RunAsync(context, new FetchUrlInput("https://example.com/doc", "mu-1", "tenant-a"));

        result.ShouldBe(expected);
    }

    [Fact]
    public async Task RunAsync_WithPayloadStore_ReturnsReferenceWithoutFetchedBytes()
    {
        IUrlContentFetcher fetcher = Substitute.For<IUrlContentFetcher>();
        byte[] fetchedBytes = [1, 2, 3];
        fetcher.FetchAsync(Arg.Any<Uri>(), Arg.Any<CancellationToken>())
            .Returns(new UrlFetchResult(fetchedBytes, "text/plain", fetchedBytes.Length, "https://example.com/final", 200));
        IWorkflowPayloadStore payloadStore = Substitute.For<IWorkflowPayloadStore>();
        WorkflowPayloadReference reference = new(
            "mu-1:fetchedurlbytes:hash",
            "hash",
            fetchedBytes.Length,
            WorkflowPayloadKind.FetchedUrlBytes,
            "tenant-a",
            "mu-1");
        payloadStore
            .SaveAsync(
                "tenant-a",
                "mu-1",
                WorkflowPayloadKind.FetchedUrlBytes,
                Arg.Any<ReadOnlyMemory<byte>>(),
                null,
                Arg.Any<CancellationToken>())
            .Returns(reference);

        FetchUrlActivity activity = new(
            fetcher,
            CreateGate(),
            NullLogger<FetchUrlActivity>.Instance,
            payloadStore);
        WorkflowActivityContext context = Substitute.For<WorkflowActivityContext>();

        UrlFetchResult result = await activity.RunAsync(context, new FetchUrlInput("https://example.com/doc", "mu-1", "tenant-a"));

        result.ContentBytes.ShouldBeEmpty();
        result.PayloadReference.ShouldBe(reference);
        result.ContentLength.ShouldBe(fetchedBytes.Length);
        await payloadStore.Received(1).SaveAsync(
            "tenant-a",
            "mu-1",
            WorkflowPayloadKind.FetchedUrlBytes,
            Arg.Is<ReadOnlyMemory<byte>>(payload => payload!.ToArray().SequenceEqual(fetchedBytes)),
            null,
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RunAsync_FetcherThrowsUrlFetchException_RethrowsForWorkflowRetry()
    {
        IUrlContentFetcher fetcher = Substitute.For<IUrlContentFetcher>();
        fetcher.FetchAsync(Arg.Any<Uri>(), Arg.Any<CancellationToken>())
            .Returns<UrlFetchResult>(_ => throw new UrlFetchException("URL_TIMEOUT", "timeout"));

        FetchUrlActivity activity = new(fetcher, CreateGate(), NullLogger<FetchUrlActivity>.Instance);
        WorkflowActivityContext context = Substitute.For<WorkflowActivityContext>();

        UrlFetchException ex = await Should.ThrowAsync<UrlFetchException>(
            () => activity.RunAsync(context, new FetchUrlInput("https://example.com/doc", "mu-1", "tenant-a")));

        ex.ErrorCode.ShouldBe("URL_TIMEOUT");
    }

    [Fact]
    public async Task RunAsync_InvalidUrl_ThrowsInvalidUrl()
    {
        IUrlContentFetcher fetcher = Substitute.For<IUrlContentFetcher>();
        FetchUrlActivity activity = new(fetcher, CreateGate(), NullLogger<FetchUrlActivity>.Instance);
        WorkflowActivityContext context = Substitute.For<WorkflowActivityContext>();

        UrlFetchException ex = await Should.ThrowAsync<UrlFetchException>(
            () => activity.RunAsync(context, new FetchUrlInput("not-a-url", "mu-1", "tenant-a")));

        ex.ErrorCode.ShouldBe("INVALID_URL");
    }

    [Fact]
    public async Task RunAsync_NullInput_Throws()
    {
        IUrlContentFetcher fetcher = Substitute.For<IUrlContentFetcher>();
        FetchUrlActivity activity = new(fetcher, CreateGate(), NullLogger<FetchUrlActivity>.Instance);
        WorkflowActivityContext context = Substitute.For<WorkflowActivityContext>();

        await Should.ThrowAsync<ArgumentNullException>(
            () => activity.RunAsync(context, null!));
    }

    [Fact]
    public async Task RunAsync_MissingTenantId_ThrowsArgumentException()
    {
        IUrlContentFetcher fetcher = Substitute.For<IUrlContentFetcher>();
        FetchUrlActivity activity = new(fetcher, CreateGate(), NullLogger<FetchUrlActivity>.Instance);
        WorkflowActivityContext context = Substitute.For<WorkflowActivityContext>();

        await Should.ThrowAsync<ArgumentException>(
            () => activity.RunAsync(context, new FetchUrlInput("https://example.com/doc", "mu-1")));
    }

    private static PerTenantConcurrencyGate CreateGate()
    {
        IngestionSettings settings = new()
        {
            PerTenantExtractionConcurrency = 4,
            ExtractionGateAcquireTimeoutSeconds = 10,
        };
        return new PerTenantConcurrencyGate(
            Options.Create(settings),
            NullLogger<PerTenantConcurrencyGate>.Instance);
    }
}
