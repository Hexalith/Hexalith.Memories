// <copyright file="GenerateChunkEmbeddingsActivityTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Tests.Activities.Ingestion;

using Dapr.Actors;
using Dapr.Actors.Client;
using Dapr.Workflow;

using Hexalith.Memories.Contracts.V1;
using Hexalith.Memories.Server.Activities.Ingestion;
using Hexalith.Memories.Server.Actors;
using Hexalith.Memories.Server.Ingestion;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

using NSubstitute;
using NSubstitute.ExceptionExtensions;

using Shouldly;

using StackExchange.Redis;

public class GenerateChunkEmbeddingsActivityTests
{
    [Fact]
    public async Task RunAsync_CallsBatchApiOnceAndMapsVectorsByChunkOrder()
    {
        TenantEmbeddingConfig config = EmbeddingProviderDefaults.Google() with { Dimensions = 3 };
        EmbeddingClient embeddingClient = CreateMockEmbeddingClient(config);
        IActorProxyFactory actorProxyFactory = CreateActorProxyFactory(config);
        GenerateChunkEmbeddingsActivity activity = new(
            embeddingClient,
            actorProxyFactory,
            Options.Create(new ContentChunkingOptions
            {
                MaxEstimatedTokens = 2,
                OverlapEstimatedTokens = 0,
                CharactersPerEstimatedToken = 4,
            }),
            NullLogger<GenerateChunkEmbeddingsActivity>.Instance,
            CreateRedisWithoutMarker());

        ChunkEmbeddingBatchResult result = await activity.RunAsync(
            Substitute.For<WorkflowActivityContext>(),
            new EmbeddingInput("tenant-a", "abcdefghijklmnop"));

        result.Chunks.Count.ShouldBe(2);
        result.Chunks[0].Sequence.ShouldBe(0);
        result.Chunks[0].Text.ShouldBe("abcdefgh");
        result.Chunks[0].Vector.ShouldBe([1f, 0f, 0f]);
        result.Chunks[1].Sequence.ShouldBe(1);
        result.Chunks[1].Text.ShouldBe("ijklmnop");
        result.Chunks[1].Vector.ShouldBe([0f, 1f, 0f]);
        await embeddingClient.Received(1).GenerateBatchAsync(
            Arg.Is<IReadOnlyList<string>>(texts => texts.SequenceEqual(new[] { "abcdefgh", "ijklmnop" })),
            "tenant-a",
            config,
            Arg.Any<CancellationToken>());
        await embeddingClient.DidNotReceive().GenerateAsync(
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<TenantEmbeddingConfig>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RunAsync_MoreChunksThanBatchLimit_CallsBatchApiPerBoundedBatch()
    {
        TenantEmbeddingConfig config = EmbeddingProviderDefaults.Google() with { Dimensions = 3 };
        EmbeddingClient embeddingClient = CreateMockEmbeddingClient(config);
        IActorProxyFactory actorProxyFactory = CreateActorProxyFactory(config, out IEmbeddingRateLimiterActor rateLimiter);
        GenerateChunkEmbeddingsActivity activity = new(
            embeddingClient,
            actorProxyFactory,
            Options.Create(new ContentChunkingOptions
            {
                MaxEstimatedTokens = 1,
                OverlapEstimatedTokens = 0,
                CharactersPerEstimatedToken = 4,
                MaxChunksPerBatch = 2,
            }),
            NullLogger<GenerateChunkEmbeddingsActivity>.Instance,
            CreateRedisWithoutMarker());

        ChunkEmbeddingBatchResult result = await activity.RunAsync(
            Substitute.For<WorkflowActivityContext>(),
            new EmbeddingInput("tenant-a", "abcdefghijkl"));

        result.Chunks.Select(static c => c.Text).ShouldBe(["abcd", "efgh", "ijkl"]);
        await embeddingClient.Received(1).GenerateBatchAsync(
            Arg.Is<IReadOnlyList<string>>(texts => texts.SequenceEqual(new[] { "abcd", "efgh" })),
            "tenant-a",
            config,
            Arg.Any<CancellationToken>());
        await embeddingClient.Received(1).GenerateBatchAsync(
            Arg.Is<IReadOnlyList<string>>(texts => texts.SequenceEqual(new[] { "ijkl" })),
            "tenant-a",
            config,
            Arg.Any<CancellationToken>());
        await rateLimiter.Received(2).TryConsumeAsync();
    }

    [Fact]
    public async Task RunAsync_WithContentReference_ReturnsChunkReferencesWithoutTextOrVectors()
    {
        TenantEmbeddingConfig config = EmbeddingProviderDefaults.Google() with { Dimensions = 3 };
        EmbeddingClient embeddingClient = CreateMockEmbeddingClient(config);
        IActorProxyFactory actorProxyFactory = CreateActorProxyFactory(config);
        IWorkflowPayloadStore payloadStore = Substitute.For<IWorkflowPayloadStore>();
        WorkflowPayloadReference contentReference = CreateReference(
            WorkflowPayloadKind.ExtractedText,
            "mu-1",
            "content",
            16);
        WorkflowPayloadReference textReference = CreateReference(
            WorkflowPayloadKind.ChunkText,
            "mu-1",
            "text-0",
            8);
        WorkflowPayloadReference vectorReference = CreateReference(
            WorkflowPayloadKind.ChunkVector,
            "mu-1",
            "vector-0",
            sizeof(float) * 3);
        payloadStore
            .ReadAsync(contentReference, "tenant-a", "mu-1", WorkflowPayloadKind.ExtractedText, Arg.Any<CancellationToken>())
            .Returns(System.Text.Encoding.UTF8.GetBytes("abcdefgh"));
        payloadStore
            .SaveAsync(
                "tenant-a",
                "mu-1",
                WorkflowPayloadKind.ChunkText,
                Arg.Any<ReadOnlyMemory<byte>>(),
                "0",
                Arg.Any<CancellationToken>())
            .Returns(textReference);
        payloadStore
            .SaveAsync(
                "tenant-a",
                "mu-1",
                WorkflowPayloadKind.ChunkVector,
                Arg.Any<ReadOnlyMemory<byte>>(),
                "0",
                Arg.Any<CancellationToken>())
            .Returns(vectorReference);
        GenerateChunkEmbeddingsActivity activity = new(
            embeddingClient,
            actorProxyFactory,
            Options.Create(new ContentChunkingOptions
            {
                MaxEstimatedTokens = 2,
                OverlapEstimatedTokens = 0,
                CharactersPerEstimatedToken = 4,
            }),
            NullLogger<GenerateChunkEmbeddingsActivity>.Instance,
            CreateRedisWithoutMarker(),
            payloadStore);

        ChunkEmbeddingBatchResult result = await activity.RunAsync(
            Substitute.For<WorkflowActivityContext>(),
            new EmbeddingInput("tenant-a", string.Empty, EmbeddingContentKind.Payload, contentReference));

        ChunkEmbeddingResult chunk = result.Chunks.ShouldHaveSingleItem();
        chunk.Text.ShouldBeEmpty();
        chunk.Vector.ShouldBeEmpty();
        chunk.TextReference.ShouldBe(textReference);
        chunk.VectorReference.ShouldBe(vectorReference);
        await embeddingClient.Received(1).GenerateBatchAsync(
            Arg.Is<IReadOnlyList<string>>(texts => texts.SequenceEqual(new[] { "abcdefgh" })),
            "tenant-a",
            config,
            Arg.Any<CancellationToken>());
        await payloadStore.Received(1).SaveAsync(
            "tenant-a",
            "mu-1",
            WorkflowPayloadKind.ChunkText,
            Arg.Is<ReadOnlyMemory<byte>>(payload => System.Text.Encoding.UTF8.GetString(payload.ToArray()) == "abcdefgh"),
            "0",
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RunAsync_Provider429_ShouldReportEffectiveRetryAfterOnceAndThrowSanitizedMarker()
    {
        TenantEmbeddingConfig config = EmbeddingProviderDefaults.Google() with { Dimensions = 3 };
        EmbeddingClient embeddingClient = CreateMockEmbeddingClient(config);
        embeddingClient.GenerateBatchAsync(
                Arg.Any<IReadOnlyList<string>>(),
                "tenant-a",
                config,
                Arg.Any<CancellationToken>())
            .ThrowsAsync(new EmbeddingRateLimitException("tenant-a") { RetryAfterSeconds = 0 });
        IActorProxyFactory actorProxyFactory = CreateActorProxyFactory(config, out IEmbeddingRateLimiterActor rateLimiter);
        GenerateChunkEmbeddingsActivity activity = new(
            embeddingClient,
            actorProxyFactory,
            Options.Create(new ContentChunkingOptions
            {
                MaxEstimatedTokens = 2,
                OverlapEstimatedTokens = 0,
                CharactersPerEstimatedToken = 4,
            }),
            NullLogger<GenerateChunkEmbeddingsActivity>.Instance,
            CreateRedisWithoutMarker());

        EmbeddingRateLimitException ex = await Should.ThrowAsync<EmbeddingRateLimitException>(
            () => activity.RunAsync(Substitute.For<WorkflowActivityContext>(), new EmbeddingInput("tenant-a", "abcdefgh")));

        ex.RetryAfterSeconds.ShouldBe(30);
        ex.Message.ShouldContain("ProviderRetryAfterSeconds=30");
        await rateLimiter.Received(1).ReportRateLimitedAsync(30);
    }

    private static EmbeddingClient CreateMockEmbeddingClient(TenantEmbeddingConfig config)
    {
        EmbeddingClient client = Substitute.For<EmbeddingClient>(
            Substitute.For<IHttpClientFactory>(),
            Substitute.For<Dapr.Client.DaprClient>(),
            new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Memories:Testing:UseFakeEmbedding"] = "false",
            }).Build(),
            CreateHostEnvironment());
        client.PrimeApiKeyAsync(Arg.Any<string>(), Arg.Any<TenantEmbeddingConfig>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);
        client.GenerateBatchAsync(Arg.Any<IReadOnlyList<string>>(), Arg.Any<string>(), config, Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                IReadOnlyList<string> texts = call.ArgAt<IReadOnlyList<string>>(0);
                return texts.Select(static text => text switch
                {
                    "abcdefgh" or "abcd" => new float[] { 1f, 0f, 0f },
                    "ijklmnop" or "efgh" => new float[] { 0f, 1f, 0f },
                    "ijkl" => new float[] { 0f, 0f, 1f },
                    _ => new float[] { 0.5f, 0.5f, 0.5f },
                }).ToArray();
            });
        return client;
    }

    private static IActorProxyFactory CreateActorProxyFactory(TenantEmbeddingConfig config)
        => CreateActorProxyFactory(config, out _);

    private static IActorProxyFactory CreateActorProxyFactory(
        TenantEmbeddingConfig config,
        out IEmbeddingRateLimiterActor rateLimiter)
    {
        IActorProxyFactory factory = Substitute.For<IActorProxyFactory>();
        rateLimiter = Substitute.For<IEmbeddingRateLimiterActor>();
        ITenantConfigurationActor tenantConfigActor = Substitute.For<ITenantConfigurationActor>();
        tenantConfigActor.GetEmbeddingConfigAsync().Returns(config);
        rateLimiter.TryConsumeAsync().Returns(true);
        factory.CreateActorProxy<IEmbeddingRateLimiterActor>(Arg.Any<ActorId>(), Arg.Any<string>()).Returns(rateLimiter);
        factory.CreateActorProxy<ITenantConfigurationActor>(Arg.Any<ActorId>(), Arg.Any<string>()).Returns(tenantConfigActor);
        return factory;
    }

    private static IHostEnvironment CreateHostEnvironment()
    {
        IHostEnvironment hostEnvironment = Substitute.For<IHostEnvironment>();
        hostEnvironment.EnvironmentName.Returns("Development");
        return hostEnvironment;
    }

    private static WorkflowPayloadReference CreateReference(
        WorkflowPayloadKind kind,
        string memoryUnitId,
        string suffix,
        long byteLength)
        => new(
            $"{memoryUnitId}:{kind.ToString().ToLowerInvariant()}:hash:{suffix}",
            $"hash-{suffix}",
            byteLength,
            kind,
            "tenant-a",
            memoryUnitId);

    private static IConnectionMultiplexer CreateRedisWithoutMarker()
    {
        IDatabase db = Substitute.For<IDatabase>();
        db.HashGetAllAsync(Arg.Any<RedisKey>(), Arg.Any<CommandFlags>())
            .Returns(Task.FromResult(Array.Empty<HashEntry>()));
        IConnectionMultiplexer redis = Substitute.For<IConnectionMultiplexer>();
        redis.GetDatabase().Returns(db);
        redis.GetDatabase(Arg.Any<int>(), Arg.Any<object>()).Returns(db);
        return redis;
    }
}
