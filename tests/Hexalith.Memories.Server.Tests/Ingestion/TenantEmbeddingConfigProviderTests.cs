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
}
