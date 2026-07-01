// <copyright file="HealthCheckWebAppFactory.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Tests.HealthChecks;

using Dapr.Client;

using Hexalith.Memories.Server.Tests.Infrastructure;

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Diagnostics.HealthChecks;

using NSubstitute;

using StackExchange.Redis;

/// <summary>
/// Story 8.1 — in-memory <see cref="WebApplicationFactory{TEntryPoint}"/> used by both
/// <see cref="ProgramHealthCheckRegistrationTests"/> (Task 4.4) and
/// <see cref="ReadyEndpointAggregationTests"/> (Task AC #9). Replaces DAPR + Redis +
/// FalkorDB dependencies with NSubstitute fakes and strips infrastructure services
/// that are unrelated to health endpoint behavior.
/// </summary>
internal sealed class HealthCheckWebAppFactory : WebApplicationFactory<Program>
{
    private readonly Action<IServiceCollection>? _extraConfiguration;

    public HealthCheckWebAppFactory(Action<IServiceCollection>? extraConfiguration = null)
    {
        _extraConfiguration = extraConfiguration;
    }

    public DaprClient DaprClient { get; } = Substitute.For<DaprClient>();

    public IConnectionMultiplexer RedisMultiplexer { get; } = Substitute.For<IConnectionMultiplexer>();

    public IConnectionMultiplexer FalkorDbMultiplexer { get; } = Substitute.For<IConnectionMultiplexer>();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");

        builder.UseSetting("ConnectionStrings:redis", "localhost:0,abortConnect=false,connectTimeout=1");
        builder.UseSetting("ConnectionStrings:falkordb", "localhost:0,abortConnect=false,connectTimeout=1");

        builder.ConfigureTestServices(services =>
        {
            services.AddKeyedSingleton<IConnectionMultiplexer>("redis", (_, _) => RedisMultiplexer);
            services.AddKeyedSingleton<IConnectionMultiplexer>("falkordb", (_, _) => FalkorDbMultiplexer);

            services.RemoveAll<DaprClient>();
            services.AddSingleton<DaprClient>(DaprClient);

            services.RemoveInfrastructureHostedServices();

            _extraConfiguration?.Invoke(services);
        });
    }
}
