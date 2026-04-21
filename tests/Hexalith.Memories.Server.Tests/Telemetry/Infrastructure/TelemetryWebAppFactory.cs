// <copyright file="TelemetryWebAppFactory.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Tests.Telemetry.Infrastructure;

using System.Collections.Generic;
using System.Linq;

using Dapr.Client;

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

using NSubstitute;

using StackExchange.Redis;

/// <summary>
/// Story 7.5 Rev 1.3 (Tasks 11.1 + 11.2) — Tier-2 <see cref="WebApplicationFactory{TEntryPoint}"/> that boots
/// the Memories Server without DAPR sidecar / Redis / FalkorDB infrastructure. Used exclusively to drive
/// telemetry instrumentation assertions (trace-id propagation + audit-log emission) through the
/// validation-fail branches of the four instrumented endpoints — search / ingest / traverse / case-access.
/// <para>
/// The factory:
/// <list type="number">
///   <item><description>Supplies sentinel connection strings so the <c>ConnectRequiredMultiplexer</c> factory
///     registered in <c>Program.cs</c> doesn't throw on startup before the ConfigureTestServices override
///     replaces the registration.</description></item>
///   <item><description>Overrides the keyed <see cref="IConnectionMultiplexer"/> registrations with
///     <see cref="NSubstitute"/> fakes — later-registered descriptors win in DI so production-side
///     services see the fakes on first resolution.</description></item>
///   <item><description>Replaces <see cref="DaprClient"/> with an NSubstitute fake so DI resolution does
///     not connect to the DAPR sidecar.</description></item>
///   <item><description>Removes the DAPR workflow + actor hosted services that otherwise would try to open
///     gRPC channels to a non-existent sidecar on <see cref="IHostedService.StartAsync"/>.</description></item>
/// </list>
/// </para>
/// <para>
/// IMPORTANT: this fixture exercises the instrumentation wrapper (<c>EndpointTelemetryScope</c>) and
/// activity emission. It does NOT exercise the downstream search / ingest / traverse / case-access
/// execution paths (those require Redis + FalkorDB + DAPR — covered by the Tier-3 Aspire fixture).
/// Callers MUST hit validation-fail paths that exit the endpoint BEFORE any downstream service call.
/// </para>
/// </summary>
internal sealed class TelemetryWebAppFactory : WebApplicationFactory<Program>
{
    /// <summary>Audit-log captures collected during the factory's lifetime — populated by the registered
    /// <see cref="CapturingAuditLoggerProvider"/>.</summary>
    public CapturingAuditLoggerProvider AuditLogs { get; } = new();

    /// <summary>Fake <see cref="DaprClient"/> exposed to tests. Tests that need specific behavior
    /// (e.g. a known tenant in the registry) configure it directly via NSubstitute before building a client.</summary>
    public DaprClient DaprClient { get; } = Substitute.For<DaprClient>();

    /// <summary>Fake actor-proxy factory exposed to tests so endpoint paths can inject actor failures.</summary>
    public Dapr.Actors.Client.IActorProxyFactory ActorProxyFactory { get; } = Substitute.For<Dapr.Actors.Client.IActorProxyFactory>();

    /// <summary>Fake Redis multiplexer exposed to tests for path-specific backend behavior.</summary>
    public IConnectionMultiplexer RedisMultiplexer { get; } = Substitute.For<IConnectionMultiplexer>();

    /// <summary>Fake FalkorDB multiplexer exposed to tests for traversal-specific backend behavior.</summary>
    public IConnectionMultiplexer FalkorDbMultiplexer { get; } = Substitute.For<IConnectionMultiplexer>();

    /// <summary>Fake Redis database surfaced for tests that need to script Redis-path calls.</summary>
    public IDatabase RedisDatabase { get; } = Substitute.For<IDatabase>();

    /// <summary>Fake FalkorDB database surfaced for tests that need to script graph-path calls.</summary>
    public IDatabase FalkorDbDatabase { get; } = Substitute.For<IDatabase>();

    public TelemetryWebAppFactory()
    {
        RedisMultiplexer.GetDatabase(Arg.Any<int>(), Arg.Any<object>()).Returns(RedisDatabase);
        FalkorDbMultiplexer.GetDatabase(Arg.Any<int>(), Arg.Any<object>()).Returns(FalkorDbDatabase);
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");

        // Sentinel connection strings so the production keyed-multiplexer factory does not throw
        // "Connection string '{name}' is required" BEFORE our ConfigureTestServices override takes effect.
        // These strings are never actually dialed because the factory registration is overridden below.
        builder.UseSetting("ConnectionStrings:redis", "localhost:0,abortConnect=false,connectTimeout=1");
        builder.UseSetting("ConnectionStrings:falkordb", "localhost:0,abortConnect=false,connectTimeout=1");

        builder.ConfigureTestServices(services =>
        {
            // 1. Replace keyed IConnectionMultiplexer registrations with NSubstitute fakes. The last-registered
            //    keyed descriptor wins, so later AddKeyedSingleton calls override the production factory.
            services.AddKeyedSingleton<IConnectionMultiplexer>(
                "redis",
                (_, _) => RedisMultiplexer);
            services.AddKeyedSingleton<IConnectionMultiplexer>(
                "falkordb",
                (_, _) => FalkorDbMultiplexer);

            services.RemoveAll<Dapr.Actors.Client.IActorProxyFactory>();
            services.AddSingleton(ActorProxyFactory);

            // 2. Replace DaprClient with the shared fake so DI resolution and health-probe wiring does not
            //    attempt to connect to a non-existent sidecar, and tests can stub registry calls via
            //    the factory's DaprClient property.
            services.RemoveAll<DaprClient>();
            services.AddSingleton<DaprClient>(DaprClient);

            // 3. Remove DAPR-specific hosted services (workflow runtime + actor registration) — both try to
            //    open gRPC channels to the sidecar on StartAsync. Filter by implementation assembly so we do
            //    not touch the RollingCounterStore hosted service or other legitimate test-compatible ones.
            List<ServiceDescriptor> hostedToRemove = [.. services.Where(s =>
                s.ServiceType == typeof(IHostedService) &&
                s.ImplementationType is not null &&
                IsDaprAssembly(s.ImplementationType.Assembly.GetName().Name))];
            foreach (ServiceDescriptor descriptor in hostedToRemove)
            {
                services.Remove(descriptor);
            }

            // 4. Register the capturing audit-log provider. AddSingleton<ILoggerProvider> composes with the
            //    existing logging pipeline — AddJsonConsole keeps running; our provider captures in parallel.
            services.AddSingleton<ILoggerProvider>(AuditLogs);
        });
    }

    private static bool IsDaprAssembly(string? assemblyName)
        => assemblyName is not null && assemblyName.StartsWith("Dapr.", System.StringComparison.OrdinalIgnoreCase);
}
