// <copyright file="GenerateEmbeddingActivityTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Tests.Activities.Ingestion;

using Dapr.Actors;
using Dapr.Actors.Client;
using Dapr.Workflow;

using Hexalith.Memories.Server.Activities.Ingestion;
using Hexalith.Memories.Server.Actors;
using Hexalith.Memories.Server.Ingestion;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

using NSubstitute;
using NSubstitute.ExceptionExtensions;

using Shouldly;

public class GenerateEmbeddingActivityTests
{
    private const string TenantId = "test-tenant";
    private const string TestText = "Hello world";

    [Fact]
    public async Task RunAsync_SuccessfulEmbedding_ReturnsCorrectResult()
    {
        // Arrange
        float[] expectedVector = new float[768];
        EmbeddingClient embeddingClient = CreateMockEmbeddingClient(expectedVector);
        IActorProxyFactory actorProxyFactory = Substitute.For<IActorProxyFactory>();
        IEmbeddingRateLimiterActor rateLimiter = Substitute.For<IEmbeddingRateLimiterActor>();
        rateLimiter.TryConsumeAsync().Returns(true);
        actorProxyFactory.CreateActorProxy<IEmbeddingRateLimiterActor>(
            Arg.Any<ActorId>(),
            Arg.Any<string>())
            .Returns(rateLimiter);
        EmbeddingInput input = new(TenantId, TestText);
        WorkflowActivityContext context = Substitute.For<WorkflowActivityContext>();

        GenerateEmbeddingActivity activity = new(embeddingClient, actorProxyFactory);

        // Act
        EmbeddingResult result = await activity.RunAsync(context, input);

        // Assert
        result.Vector.ShouldBe(expectedVector);
        result.Provider.ShouldBe("google:text-embedding-004");
        result.Dimensions.ShouldBe(768);

        Received.InOrder(() =>
        {
            embeddingClient.PrimeApiKeyAsync(TenantId, Arg.Any<CancellationToken>());
            actorProxyFactory.CreateActorProxy<IEmbeddingRateLimiterActor>(Arg.Any<ActorId>(), "EmbeddingRateLimiterActor");
            rateLimiter.TryConsumeAsync();
            embeddingClient.GenerateAsync(TestText, TenantId, Arg.Any<CancellationToken>());
        });
    }

    [Fact]
    public async Task RunAsync_RateLimitExhausted_ThrowsEmbeddingRateLimitException()
    {
        // Arrange
        EmbeddingClient embeddingClient = CreateMockEmbeddingClient([]);
        IActorProxyFactory actorProxyFactory = CreateMockActorProxyFactory(allowed: false);
        EmbeddingInput input = new(TenantId, TestText);
        WorkflowActivityContext context = Substitute.For<WorkflowActivityContext>();

        GenerateEmbeddingActivity activity = new(embeddingClient, actorProxyFactory);

        // Act & Assert
        EmbeddingRateLimitException ex = await Should.ThrowAsync<EmbeddingRateLimitException>(
            () => activity.RunAsync(context, input));
        ex.TenantId.ShouldBe(TenantId);
    }

    [Fact]
    public async Task RunAsync_EmbeddingClientThrows_ExceptionPropagates()
    {
        // Arrange
        EmbeddingClient embeddingClient = Substitute.For<EmbeddingClient>(
            Substitute.For<System.Net.Http.HttpClient>(),
            Substitute.For<Dapr.Client.DaprClient>(),
            CreateConfiguration(),
            CreateHostEnvironment());
        embeddingClient.PrimeApiKeyAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);
        embeddingClient.GenerateAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new EmbeddingApiException(500, "internal error", TenantId));

        IActorProxyFactory actorProxyFactory = CreateMockActorProxyFactory(allowed: true);
        EmbeddingInput input = new(TenantId, TestText);
        WorkflowActivityContext context = Substitute.For<WorkflowActivityContext>();

        GenerateEmbeddingActivity activity = new(embeddingClient, actorProxyFactory);

        // Act & Assert
        await Should.ThrowAsync<EmbeddingApiException>(
            () => activity.RunAsync(context, input));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task RunAsync_InvalidTenantId_ThrowsArgumentException(string? tenantId)
    {
        // Arrange
        EmbeddingClient embeddingClient = CreateMockEmbeddingClient(new float[768]);
        IActorProxyFactory actorProxyFactory = Substitute.For<IActorProxyFactory>();
        EmbeddingInput input = new(tenantId!, TestText);
        WorkflowActivityContext context = Substitute.For<WorkflowActivityContext>();

        GenerateEmbeddingActivity activity = new(embeddingClient, actorProxyFactory);

        // Act & Assert
        await Should.ThrowAsync<ArgumentException>(() => activity.RunAsync(context, input));
        await embeddingClient.DidNotReceive().PrimeApiKeyAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
        actorProxyFactory.DidNotReceive().CreateActorProxy<IEmbeddingRateLimiterActor>(Arg.Any<ActorId>(), Arg.Any<string>());
    }

    [Fact]
    public async Task RunAsync_PrimeApiKeyFails_DoesNotConsumeRateLimitBudget()
    {
        // Arrange
        EmbeddingClient embeddingClient = Substitute.For<EmbeddingClient>(
            Substitute.For<System.Net.Http.HttpClient>(),
            Substitute.For<Dapr.Client.DaprClient>(),
            CreateConfiguration(),
            CreateHostEnvironment());
        embeddingClient.PrimeApiKeyAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new EmbeddingApiException("missing secret", TenantId));

        IActorProxyFactory actorProxyFactory = Substitute.For<IActorProxyFactory>();
        EmbeddingInput input = new(TenantId, TestText);
        WorkflowActivityContext context = Substitute.For<WorkflowActivityContext>();

        GenerateEmbeddingActivity activity = new(embeddingClient, actorProxyFactory);

        // Act & Assert
        await Should.ThrowAsync<EmbeddingApiException>(() => activity.RunAsync(context, input));
        actorProxyFactory.DidNotReceive().CreateActorProxy<IEmbeddingRateLimiterActor>(Arg.Any<ActorId>(), Arg.Any<string>());
        await embeddingClient.DidNotReceive().GenerateAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RunAsync_UsesCorrectActorIdFromTenantId()
    {
        // Arrange
        float[] vector = new float[768];
        EmbeddingClient embeddingClient = CreateMockEmbeddingClient(vector);
        IActorProxyFactory actorProxyFactory = Substitute.For<IActorProxyFactory>();
        IEmbeddingRateLimiterActor rateLimiter = Substitute.For<IEmbeddingRateLimiterActor>();
        rateLimiter.TryConsumeAsync().Returns(true);

        actorProxyFactory.CreateActorProxy<IEmbeddingRateLimiterActor>(
                Arg.Any<ActorId>(),
                Arg.Any<string>())
            .Returns(rateLimiter);

        EmbeddingInput input = new(TenantId, TestText);
        WorkflowActivityContext context = Substitute.For<WorkflowActivityContext>();

        GenerateEmbeddingActivity activity = new(embeddingClient, actorProxyFactory);

        // Act
        await activity.RunAsync(context, input);

        // Assert
        actorProxyFactory.Received(1).CreateActorProxy<IEmbeddingRateLimiterActor>(
            Arg.Is<ActorId>(id => id.ToString() == TenantId),
            "EmbeddingRateLimiterActor");
    }

    private static EmbeddingClient CreateMockEmbeddingClient(float[] vectorToReturn)
    {
        EmbeddingClient client = Substitute.For<EmbeddingClient>(
            Substitute.For<System.Net.Http.HttpClient>(),
            Substitute.For<Dapr.Client.DaprClient>(),
            CreateConfiguration(),
            CreateHostEnvironment());
        client.PrimeApiKeyAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);
        client.GenerateAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(vectorToReturn);
        return client;
    }

    private static IConfiguration CreateConfiguration(bool useFakeEmbedding = false)
        => new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Memories:Testing:UseFakeEmbedding"] = useFakeEmbedding.ToString(),
            })
            .Build();

    private static IHostEnvironment CreateHostEnvironment(string environmentName = "Development")
    {
        IHostEnvironment hostEnvironment = Substitute.For<IHostEnvironment>();
        hostEnvironment.EnvironmentName.Returns(environmentName);
        return hostEnvironment;
    }

    private static IActorProxyFactory CreateMockActorProxyFactory(bool allowed)
    {
        IActorProxyFactory factory = Substitute.For<IActorProxyFactory>();
        IEmbeddingRateLimiterActor rateLimiter = Substitute.For<IEmbeddingRateLimiterActor>();
        rateLimiter.TryConsumeAsync().Returns(allowed);

        factory.CreateActorProxy<IEmbeddingRateLimiterActor>(
                Arg.Any<ActorId>(),
                Arg.Any<string>())
            .Returns(rateLimiter);

        return factory;
    }
}
