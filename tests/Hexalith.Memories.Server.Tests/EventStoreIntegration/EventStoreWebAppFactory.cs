// <copyright file="EventStoreWebAppFactory.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Tests.EventStoreIntegration;

using System.Collections.Generic;
using System.Linq;

using Dapr.Client;

using Hexalith.Memories.EventStore;
using Hexalith.Memories.Server.Tests.Telemetry.Infrastructure;

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

using NSubstitute;

using StackExchange.Redis;

/// <summary>Story 9.1 Tier-2 <see cref="WebApplicationFactory{TEntryPoint}"/> that boots the Memories Server
/// with the EventStore subscription pipeline wired in but with every external adapter replaced by an
/// NSubstitute fake. Exercises the controller + middleware order + outcome-to-HTTP mapping end-to-end
/// through ASP.NET Core without requiring a running Redis / FalkorDB / DAPR sidecar.</summary>
internal sealed class EventStoreWebAppFactory : WebApplicationFactory<Program>
{
    public ITenantEventRouter Router { get; } = Substitute.For<ITenantEventRouter>();

    public IEventIngestionWorkflowScheduler Scheduler { get; } = Substitute.For<IEventIngestionWorkflowScheduler>();

    public ITenantStatusAccessor TenantStatus { get; } = Substitute.For<ITenantStatusAccessor>();

    public ICaseCreationService CaseCreator { get; } = Substitute.For<ICaseCreationService>();

    public IEventIngestionTelemetry Telemetry { get; } = Substitute.For<IEventIngestionTelemetry>();

    public IPreflightDedupStore PreflightDedup { get; } = Substitute.For<IPreflightDedupStore>();

    public IConnectionMultiplexer RedisMultiplexer { get; } = Substitute.For<IConnectionMultiplexer>();

    public IConnectionMultiplexer FalkorDbMultiplexer { get; } = Substitute.For<IConnectionMultiplexer>();

    public DaprClient DaprClient { get; } = Substitute.For<DaprClient>();

    public Dapr.Actors.Client.IActorProxyFactory ActorProxyFactory { get; } = Substitute.For<Dapr.Actors.Client.IActorProxyFactory>();

    public CapturingAuditLoggerProvider AuditLogs { get; } = new();

    public CapturingEventStoreLogProvider EventStoreLogs { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");

        // Sentinel connection strings so the production keyed-multiplexer factory does not throw before
        // the ConfigureTestServices override replaces the registrations.
        builder.UseSetting("ConnectionStrings:redis", "localhost:0,abortConnect=false,connectTimeout=1");
        builder.UseSetting("ConnectionStrings:falkordb", "localhost:0,abortConnect=false,connectTimeout=1");

        builder.ConfigureTestServices(services =>
        {
            services.AddKeyedSingleton<IConnectionMultiplexer>("redis", (_, _) => RedisMultiplexer);
            services.AddKeyedSingleton<IConnectionMultiplexer>("falkordb", (_, _) => FalkorDbMultiplexer);

            services.RemoveAll<Dapr.Actors.Client.IActorProxyFactory>();
            services.AddSingleton(ActorProxyFactory);

            services.RemoveAll<DaprClient>();
            services.AddSingleton(DaprClient);

            // Replace EventStore adapters with NSubstitute fakes so tests can drive each outcome branch.
            services.RemoveAll<ITenantEventRouter>();
            services.RemoveAll<IEventIngestionWorkflowScheduler>();
            services.RemoveAll<ITenantStatusAccessor>();
            services.RemoveAll<ICaseCreationService>();
            services.RemoveAll<IEventIngestionTelemetry>();
            services.RemoveAll<IPreflightDedupStore>();
            services.AddSingleton(Router);
            services.AddSingleton(Scheduler);
            services.AddSingleton(TenantStatus);
            services.AddSingleton(CaseCreator);
            services.AddSingleton(Telemetry);
            services.AddSingleton(PreflightDedup);

            List<ServiceDescriptor> hostedToRemove = [.. services.Where(s =>
                s.ServiceType == typeof(IHostedService)
                && s.ImplementationType is not null
                && IsDaprAssembly(s.ImplementationType.Assembly.GetName().Name))];
            foreach (ServiceDescriptor descriptor in hostedToRemove)
            {
                services.Remove(descriptor);
            }

            // Also remove the EventStore routing-config validator — it would try to enumerate the tenant
            // registry through the fake DaprClient and either hang or throw. Tests wire the router fake
            // directly and do not need startup validation.
            List<ServiceDescriptor> validatorToRemove = [.. services.Where(s =>
                s.ServiceType == typeof(IHostedService)
                && s.ImplementationType?.Name == "EventStoreRoutingConfigValidator")];
            foreach (ServiceDescriptor descriptor in validatorToRemove)
            {
                services.Remove(descriptor);
            }

            services.AddSingleton<ILoggerProvider>(AuditLogs);
            services.AddSingleton<ILoggerProvider>(EventStoreLogs);
        });
    }

    private static bool IsDaprAssembly(string? assemblyName)
        => assemblyName is not null && assemblyName.StartsWith("Dapr.", System.StringComparison.OrdinalIgnoreCase);
}
