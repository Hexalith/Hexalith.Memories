// <copyright file="GenerateEmbeddingActivityConfigTests.cs" company="ITANEO">
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

using NSubstitute;

using Shouldly;

using StackExchange.Redis;

public class GenerateEmbeddingActivityConfigTests
{
    private const string TenantId = "test-tenant";
    private const string TestText = "Hello world";

    [Fact]
    public async Task RunAsync_ShouldReadConfigFromTenantConfigurationActor()
    {
        // Arrange
        TenantEmbeddingConfig config = EmbeddingProviderDefaults.Google();
        ITenantConfigurationActor tenantConfigActor = Substitute.For<ITenantConfigurationActor>();
        tenantConfigActor.GetEmbeddingConfigAsync().Returns(config);

        (GenerateEmbeddingActivity activity, _, _) = CreateActivity(tenantConfigActor);
        WorkflowActivityContext context = Substitute.For<WorkflowActivityContext>();
        EmbeddingInput input = new(TenantId, TestText);

        // Act
        await activity.RunAsync(context, input);

        // Assert
        await tenantConfigActor.Received(1).GetEmbeddingConfigAsync();
    }

    [Fact]
    public async Task RunAsync_ShouldPassConfigToEmbeddingClient()
    {
        // Arrange
        TenantEmbeddingConfig config = EmbeddingProviderDefaults.Google();
        ITenantConfigurationActor tenantConfigActor = Substitute.For<ITenantConfigurationActor>();
        tenantConfigActor.GetEmbeddingConfigAsync().Returns(config);

        (GenerateEmbeddingActivity activity, EmbeddingClient embeddingClient, _) = CreateActivity(tenantConfigActor);
        WorkflowActivityContext context = Substitute.For<WorkflowActivityContext>();
        EmbeddingInput input = new(TenantId, TestText);

        // Act
        await activity.RunAsync(context, input);

        // Assert
        await embeddingClient.Received(1).GenerateAsync(
            TestText, TenantId,
            Arg.Is<TenantEmbeddingConfig>(c => c.Model == "gemini-embedding-001"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RunAsync_ShouldSetRateLimiterCeilingFromConfig()
    {
        // Arrange
        TenantEmbeddingConfig config = EmbeddingProviderDefaults.Google() with { RateLimitPerMinute = 500 };
        ITenantConfigurationActor tenantConfigActor = Substitute.For<ITenantConfigurationActor>();
        tenantConfigActor.GetEmbeddingConfigAsync().Returns(config);

        (GenerateEmbeddingActivity activity, _, IEmbeddingRateLimiterActor rateLimiter) = CreateActivity(tenantConfigActor);
        WorkflowActivityContext context = Substitute.For<WorkflowActivityContext>();
        EmbeddingInput input = new(TenantId, TestText);

        // Act
        await activity.RunAsync(context, input);

        // Assert — SetCeilingAsync called with config value before TryConsumeAsync
        Received.InOrder(() =>
        {
            rateLimiter.SetCeilingAsync(500);
            rateLimiter.TryConsumeAsync();
        });
    }

    [Fact]
    public async Task RunAsync_ShouldReturnDynamicProviderAndDimensions()
    {
        // Arrange
        TenantEmbeddingConfig config = EmbeddingProviderDefaults.Google();
        ITenantConfigurationActor tenantConfigActor = Substitute.For<ITenantConfigurationActor>();
        tenantConfigActor.GetEmbeddingConfigAsync().Returns(config);

        (GenerateEmbeddingActivity activity, _, _) = CreateActivity(tenantConfigActor);
        WorkflowActivityContext context = Substitute.For<WorkflowActivityContext>();
        EmbeddingInput input = new(TenantId, TestText);

        // Act
        EmbeddingResult result = await activity.RunAsync(context, input);

        // Assert — result uses config values, not "google:text-embedding-004"/768 constants
        result.Provider.ShouldBe("google:gemini-embedding-001");
        result.Dimensions.ShouldBe(768);
    }

    private static (GenerateEmbeddingActivity Activity, EmbeddingClient Client, IEmbeddingRateLimiterActor RateLimiter) CreateActivity(
        ITenantConfigurationActor tenantConfigActor)
    {
        EmbeddingClient embeddingClient = Substitute.For<EmbeddingClient>(
            Substitute.For<IHttpClientFactory>(),
            Substitute.For<Dapr.Client.DaprClient>(),
            CreateConfiguration(),
            CreateHostEnvironment());
        embeddingClient.PrimeApiKeyAsync(Arg.Any<string>(), Arg.Any<TenantEmbeddingConfig>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);
        embeddingClient.GenerateAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<TenantEmbeddingConfig>(), Arg.Any<CancellationToken>())
            .Returns(new float[768]);

        IEmbeddingRateLimiterActor rateLimiter = Substitute.For<IEmbeddingRateLimiterActor>();
        rateLimiter.TryConsumeAsync().Returns(true);

        IActorProxyFactory actorProxyFactory = Substitute.For<IActorProxyFactory>();
        actorProxyFactory.CreateActorProxy<ITenantConfigurationActor>(Arg.Any<ActorId>(), Arg.Any<string>())
            .Returns(tenantConfigActor);
        actorProxyFactory.CreateActorProxy<IEmbeddingRateLimiterActor>(Arg.Any<ActorId>(), Arg.Any<string>())
            .Returns(rateLimiter);

        GenerateEmbeddingActivity activity = new(
            embeddingClient,
            actorProxyFactory,
            new ZeroJitterSource(),
            NullLogger<GenerateEmbeddingActivity>.Instance,
            CreateRedisWithoutMarker());
        return (activity, embeddingClient, rateLimiter);
    }

    private static IConnectionMultiplexer CreateRedisWithoutMarker()
    {
        IDatabase db = Substitute.For<IDatabase>();
        db.HashGetAllAsync(Arg.Any<RedisKey>(), Arg.Any<CommandFlags>())
            .Returns(Task.FromResult(Array.Empty<HashEntry>()));
        IConnectionMultiplexer redis = Substitute.For<IConnectionMultiplexer>();
        redis.GetDatabase(Arg.Any<int>(), Arg.Any<object>()).Returns(db);
        return redis;
    }

    private sealed class ZeroJitterSource : IJitterSource
    {
        public int NextMilliseconds(int maxExclusive = 500) => 0;
    }

    private static IConfiguration CreateConfiguration()
        => new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Memories:Testing:UseFakeEmbedding"] = "false",
            })
            .Build();

    private static IHostEnvironment CreateHostEnvironment()
    {
        IHostEnvironment env = Substitute.For<IHostEnvironment>();
        env.EnvironmentName.Returns("Development");
        return env;
    }
}
