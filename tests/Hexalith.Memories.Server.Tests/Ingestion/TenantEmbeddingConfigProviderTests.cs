// <copyright file="TenantEmbeddingConfigProviderTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Tests.Ingestion;

using Dapr.Actors;
using Dapr.Actors.Client;

using Hexalith.Memories.Contracts.V1;
using Hexalith.Memories.Server.Actors;
using Hexalith.Memories.Server.Ingestion;

using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;

using NSubstitute;

using Shouldly;

public sealed class TenantEmbeddingConfigProviderTests
{
    [Fact]
    public async Task GetAsync_DifferentTenants_DoNotShareCachedConfig()
    {
        TenantEmbeddingConfig tenantAConfig = EmbeddingProviderDefaults.Google() with { RateLimitPerMinute = 500 };
        TenantEmbeddingConfig tenantBConfig = EmbeddingProviderDefaults.Ollama() with { RateLimitPerMinute = 6000 };
        ITenantConfigurationActor tenantAActor = Substitute.For<ITenantConfigurationActor>();
        ITenantConfigurationActor tenantBActor = Substitute.For<ITenantConfigurationActor>();
        tenantAActor.GetEmbeddingConfigAsync().Returns(tenantAConfig);
        tenantBActor.GetEmbeddingConfigAsync().Returns(tenantBConfig);

        IActorProxyFactory actorProxyFactory = Substitute.For<IActorProxyFactory>();
        actorProxyFactory.CreateActorProxy<ITenantConfigurationActor>(
                Arg.Is<ActorId>(id => id.ToString() == "tenant-a"),
                nameof(TenantConfigurationActor))
            .Returns(tenantAActor);
        actorProxyFactory.CreateActorProxy<ITenantConfigurationActor>(
                Arg.Is<ActorId>(id => id.ToString() == "tenant-b"),
                nameof(TenantConfigurationActor))
            .Returns(tenantBActor);

        TenantEmbeddingConfigProvider provider = new(
            actorProxyFactory,
            Options.Create(new TenantEmbeddingConfigCacheOptions { CacheTtlSeconds = 30 }),
            new FakeTimeProvider(new DateTimeOffset(2026, 7, 5, 12, 0, 0, TimeSpan.Zero)));

        TenantEmbeddingConfig firstA = await provider.GetAsync("tenant-a");
        TenantEmbeddingConfig firstB = await provider.GetAsync("tenant-b");
        TenantEmbeddingConfig secondA = await provider.GetAsync("tenant-a");
        TenantEmbeddingConfig secondB = await provider.GetAsync("tenant-b");

        firstA.RateLimitPerMinute.ShouldBe(500);
        secondA.RateLimitPerMinute.ShouldBe(500);
        firstB.Provider.ShouldBe(EmbeddingProviderDefaults.OllamaProviderName);
        secondB.Provider.ShouldBe(EmbeddingProviderDefaults.OllamaProviderName);
        await tenantAActor.Received(1).GetEmbeddingConfigAsync();
        await tenantBActor.Received(1).GetEmbeddingConfigAsync();
    }

    [Fact]
    public async Task GetAsync_WhenCacheExpires_ReadsActorAgain()
    {
        TenantEmbeddingConfig firstConfig = EmbeddingProviderDefaults.Google() with { RateLimitPerMinute = 500 };
        TenantEmbeddingConfig secondConfig = EmbeddingProviderDefaults.Google() with { RateLimitPerMinute = 750 };
        ITenantConfigurationActor actor = Substitute.For<ITenantConfigurationActor>();
        actor.GetEmbeddingConfigAsync().Returns(firstConfig, secondConfig);
        IActorProxyFactory actorProxyFactory = CreateActorProxyFactory("tenant-a", actor);
        FakeTimeProvider timeProvider = new(new DateTimeOffset(2026, 7, 5, 12, 0, 0, TimeSpan.Zero));
        TenantEmbeddingConfigProvider provider = new(
            actorProxyFactory,
            Options.Create(new TenantEmbeddingConfigCacheOptions { CacheTtlSeconds = 1 }),
            timeProvider);

        TenantEmbeddingConfig first = await provider.GetAsync("tenant-a");
        timeProvider.Advance(TimeSpan.FromSeconds(2));
        TenantEmbeddingConfig second = await provider.GetAsync("tenant-a");

        first.RateLimitPerMinute.ShouldBe(500);
        second.RateLimitPerMinute.ShouldBe(750);
        await actor.Received(2).GetEmbeddingConfigAsync();
    }

    [Fact]
    public async Task Invalidate_RemovesEmbeddingAndFusionWeightsEntries()
    {
        TenantEmbeddingConfig firstConfig = EmbeddingProviderDefaults.Google() with { RateLimitPerMinute = 500 };
        TenantEmbeddingConfig secondConfig = EmbeddingProviderDefaults.Google() with { RateLimitPerMinute = 900 };
        FusionWeights firstWeights = new() { SyntacticWeight = 0.7, SemanticWeight = 0.2, NlWeight = 0.05, GraphWeight = 0.05 };
        FusionWeights secondWeights = new() { SyntacticWeight = 0.4, SemanticWeight = 0.4, NlWeight = 0.1, GraphWeight = 0.1 };
        ITenantConfigurationActor actor = Substitute.For<ITenantConfigurationActor>();
        actor.GetEmbeddingConfigAsync().Returns(firstConfig, secondConfig);
        actor.GetFusionWeightsAsync().Returns(firstWeights, secondWeights);
        IActorProxyFactory actorProxyFactory = CreateActorProxyFactory("tenant-a", actor);
        TenantEmbeddingConfigProvider provider = new(
            actorProxyFactory,
            Options.Create(new TenantEmbeddingConfigCacheOptions { CacheTtlSeconds = 30 }),
            new FakeTimeProvider(new DateTimeOffset(2026, 7, 5, 12, 0, 0, TimeSpan.Zero)));

        _ = await provider.GetAsync("tenant-a");
        _ = await provider.GetFusionWeightsAsync("tenant-a");
        provider.Invalidate("tenant-a");
        TenantEmbeddingConfig refreshedConfig = await provider.GetAsync("tenant-a");
        FusionWeights refreshedWeights = await provider.GetFusionWeightsAsync("tenant-a");

        refreshedConfig.RateLimitPerMinute.ShouldBe(900);
        refreshedWeights.SyntacticWeight.ShouldBe(0.4);
        await actor.Received(2).GetEmbeddingConfigAsync();
        await actor.Received(2).GetFusionWeightsAsync();
    }

    [Fact]
    public async Task GetFusionWeightsAsync_WhenCacheWarm_DoesNotCallActorAgain()
    {
        FusionWeights weights = new() { SyntacticWeight = 0.6, SemanticWeight = 0.3, NlWeight = 0.05, GraphWeight = 0.05 };
        ITenantConfigurationActor actor = Substitute.For<ITenantConfigurationActor>();
        actor.GetFusionWeightsAsync().Returns(weights);
        IActorProxyFactory actorProxyFactory = CreateActorProxyFactory("tenant-a", actor);
        TenantEmbeddingConfigProvider provider = new(
            actorProxyFactory,
            Options.Create(new TenantEmbeddingConfigCacheOptions { CacheTtlSeconds = 30 }),
            new FakeTimeProvider(new DateTimeOffset(2026, 7, 5, 12, 0, 0, TimeSpan.Zero)));

        FusionWeights first = await provider.GetFusionWeightsAsync("tenant-a");
        FusionWeights second = await provider.GetFusionWeightsAsync("tenant-a");

        first.ShouldBe(weights);
        second.ShouldBe(weights);
        await actor.Received(1).GetFusionWeightsAsync();
    }

    private static IActorProxyFactory CreateActorProxyFactory(string tenantId, ITenantConfigurationActor actor)
    {
        IActorProxyFactory actorProxyFactory = Substitute.For<IActorProxyFactory>();
        actorProxyFactory.CreateActorProxy<ITenantConfigurationActor>(
                Arg.Is<ActorId>(id => id.ToString() == tenantId),
                nameof(TenantConfigurationActor))
            .Returns(actor);
        return actorProxyFactory;
    }
}
