// <copyright file="KeyedRedisConnectionRegistrationTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Tests.Hosting;

using System;
using System.Collections.Generic;
using System.Linq;

using Hexalith.Memories.ServiceDefaults;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using Shouldly;

using StackExchange.Redis;

/// <summary>spec-infrastructure-dependency-abstraction (F5, Decision D30) — verifies the keyed
/// <see cref="IConnectionMultiplexer"/> construction relocated to the ServiceDefaults boundary project
/// still registers both keyed connections and preserves the fail-fast "Start the server through AppHost…"
/// guard. The end-to-end "both keyed connections resolve against a live Redis/FalkorDB" is the AppHost
/// boot smoke that requires a container runtime; it is tracked as a blocked-evidence gate for review.</summary>
public sealed class KeyedRedisConnectionRegistrationTests
{
    [Fact]
    public void AddKeyedRedisConnections_RegistersBothKeyedMultiplexers()
    {
        HostApplicationBuilder builder = CreateBuilder();

        _ = builder.AddKeyedRedisConnections();

        bool redisRegistered = builder.Services.Any(d =>
            d.ServiceType == typeof(IConnectionMultiplexer) &&
            string.Equals(d.ServiceKey as string, "redis", StringComparison.Ordinal));
        bool falkorRegistered = builder.Services.Any(d =>
            d.ServiceType == typeof(IConnectionMultiplexer) &&
            string.Equals(d.ServiceKey as string, "falkordb", StringComparison.Ordinal));

        redisRegistered.ShouldBeTrue();
        falkorRegistered.ShouldBeTrue();
    }

    [Theory]
    [InlineData("redis")]
    [InlineData("falkordb")]
    public void ResolvingKeyedMultiplexer_WithoutConnectionString_ThrowsAppHostGuard(string connectionName)
    {
        HostApplicationBuilder builder = CreateBuilder();
        _ = builder.AddKeyedRedisConnections();
        using ServiceProvider provider = builder.Services.BuildServiceProvider();

        InvalidOperationException ex = Should.Throw<InvalidOperationException>(
            () => provider.GetRequiredKeyedService<IConnectionMultiplexer>(connectionName));

        ex.Message.ShouldContain($"Connection string '{connectionName}' is required.");
        ex.Message.ShouldContain("Start the server through AppHost");
        ex.Message.ShouldContain($"ConnectionStrings__{connectionName}");
    }

    [Fact]
    public void ConnectRequiredMultiplexer_WithMissingConnectionString_ThrowsAppHostGuard()
    {
        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>())
            .Build();

        InvalidOperationException ex = Should.Throw<InvalidOperationException>(
            () => Extensions.ConnectRequiredMultiplexer(configuration, "redis"));

        ex.Message.ShouldContain("Start the server through AppHost");
    }

    private static HostApplicationBuilder CreateBuilder()
        => Host.CreateEmptyApplicationBuilder(new HostApplicationBuilderSettings
        {
            Args = [],
            EnvironmentName = "Development",
        });
}
