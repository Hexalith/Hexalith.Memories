// <copyright file="KeyedRedisConnectionsLiveSmokeTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.IntegrationTests.Hosting;

using Hexalith.Memories.IntegrationTests.Fixtures;
using Hexalith.Memories.ServiceDefaults;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using Shouldly;

using StackExchange.Redis;

/// <summary>spec-infrastructure-dependency-abstraction A3 / F5 — live container smoke that the
/// ServiceDefaults boundary <see cref="Extensions.AddKeyedRedisConnections{TBuilder}"/> resolves both
/// keyed multiplexers when Aspire-shaped <c>ConnectionStrings__redis</c> /
/// <c>ConnectionStrings__falkordb</c> are injected (CompositeSearch fixture = Redis Stack + FalkorDB).</summary>
[Collection("GraphSearch")]
[Trait("Category", "Integration")]
[Trait("Tier", "2")]
public sealed class KeyedRedisConnectionsLiveSmokeTests
{
    private readonly CompositeSearchFixture _fixture;

    public KeyedRedisConnectionsLiveSmokeTests(CompositeSearchFixture fixture) => _fixture = fixture;

    [Fact]
    public async Task AddKeyedRedisConnections_WithLiveConnectionStrings_ResolvesAndPingsBothKeys()
    {
        HostApplicationBuilder builder = Host.CreateEmptyApplicationBuilder(new HostApplicationBuilderSettings
        {
            Args = [],
            EnvironmentName = "Development",
        });
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ConnectionStrings:redis"] = _fixture.RedisConnectionString,
            ["ConnectionStrings:falkordb"] = _fixture.FalkorDbConnectionString,
        });

        _ = builder.AddKeyedRedisConnections();
        await using ServiceProvider provider = builder.Services.BuildServiceProvider();

        IConnectionMultiplexer redis = provider.GetRequiredKeyedService<IConnectionMultiplexer>("redis");
        IConnectionMultiplexer falkor = provider.GetRequiredKeyedService<IConnectionMultiplexer>("falkordb");

        redis.IsConnected.ShouldBeTrue();
        falkor.IsConnected.ShouldBeTrue();
        (await redis.GetDatabase().PingAsync()).ShouldBeGreaterThan(TimeSpan.Zero);
        (await falkor.GetDatabase().PingAsync()).ShouldBeGreaterThan(TimeSpan.Zero);

        // Distinct backends — not two handles onto the same container.
        redis.GetEndPoints().Select(static e => e.ToString()).ShouldNotBe(
            falkor.GetEndPoints().Select(static e => e.ToString()));
    }
}
