// <copyright file="DefaultRedisReadyHealthCheckTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Tests.HealthChecks;

using Hexalith.Memories.ServiceDefaults;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;

using NSubstitute;

using Shouldly;

using StackExchange.Redis;

/// <summary>Regression tests for Story 15.6's default Redis readiness gate.</summary>
public sealed class DefaultRedisReadyHealthCheckTests
{
    [Fact]
    public async Task AddDefaultHealthChecks_WhenRedisKeyedServiceMissing_ReadyCheckIsUnhealthy()
    {
        HostApplicationBuilder builder = CreateBuilder();
        builder.AddDefaultHealthChecks();

        using ServiceProvider provider = builder.Services.BuildServiceProvider();
        HealthCheckService health = provider.GetRequiredService<HealthCheckService>();

        HealthReport report = await health.CheckHealthAsync(
            registration => registration.Tags.Contains("ready"));

        report.Status.ShouldBe(HealthStatus.Unhealthy);
        report.Entries.Keys.ShouldContain("redis-ping");
        (report.Entries["redis-ping"].Description ?? string.Empty).ShouldContain("not registered");
    }

    [Fact]
    public async Task AddDefaultHealthChecks_WhenRedisPingSucceeds_ReadyCheckIsHealthy()
    {
        HostApplicationBuilder builder = CreateBuilder();
        IConnectionMultiplexer redis = Substitute.For<IConnectionMultiplexer>();
        IDatabase database = Substitute.For<IDatabase>();
        redis.GetDatabase(Arg.Any<int>(), Arg.Any<object>()).Returns(database);
        database.PingAsync(Arg.Any<CommandFlags>()).Returns(Task.FromResult(TimeSpan.FromMilliseconds(1)));

        builder.Services.AddKeyedSingleton<IConnectionMultiplexer>(
            Extensions.RedisConnectionKey,
            (_, _) => redis);
        builder.AddDefaultHealthChecks();

        using ServiceProvider provider = builder.Services.BuildServiceProvider();
        HealthCheckService health = provider.GetRequiredService<HealthCheckService>();

        HealthReport report = await health.CheckHealthAsync(
            registration => registration.Name == "redis-ping");

        report.Status.ShouldBe(HealthStatus.Healthy);
        (report.Entries["redis-ping"].Description ?? string.Empty).ShouldContain("Redis PING succeeded");
    }

    [Fact]
    public void AddDefaultHealthChecks_WhenRedisReadyCheckOptedOut_DoesNotRegisterRedisPing()
    {
        HostApplicationBuilder builder = CreateBuilder();

        builder.AddDefaultHealthChecks(configureRedisReadyCheck: false);

        using ServiceProvider provider = builder.Services.BuildServiceProvider();
        HealthCheckServiceOptions options = provider
            .GetRequiredService<Microsoft.Extensions.Options.IOptions<HealthCheckServiceOptions>>()
            .Value;
        options.Registrations.Select(registration => registration.Name).ShouldNotContain("redis-ping");
    }

    private static HostApplicationBuilder CreateBuilder()
        => Host.CreateEmptyApplicationBuilder(new HostApplicationBuilderSettings
        {
            Args = [],
            EnvironmentName = "Development",
        });
}
