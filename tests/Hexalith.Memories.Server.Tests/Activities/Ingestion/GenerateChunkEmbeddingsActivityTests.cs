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
