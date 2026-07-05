// <copyright file="EventStoreWebAppFactory.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Tests.EventStoreIntegration;

using Dapr.Client;

using Hexalith.Memories.EventStore;
using Hexalith.Memories.Server.EventStoreIntegration;
using Hexalith.Memories.Server.Ingestion;
using Hexalith.Memories.Server.Tests.Infrastructure;
using Hexalith.Memories.Server.Tests.Telemetry.Infrastructure;

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
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

    public IIngestionWorkflowStateReader IngestionWorkflowStateReader { get; } = Substitute.For<IIngestionWorkflowStateReader>();

    internal IMemoriesCommandStore MemoriesCommandStore { get; set; } = new InMemoryMemoriesCommandStore();

    internal ICaseProjectionWorkflowScheduler CaseProjectionWorkflowScheduler { get; set; } = new InMemoryCaseProjectionWorkflowScheduler();

    internal IFailedUnitsRegistry? FailedUnitsRegistryOverride { get; set; }

    internal IWorkflowPayloadStore? WorkflowPayloadStoreOverride { get; set; }

    internal IIngestionWorkflowScheduler? IngestionWorkflowSchedulerOverride { get; set; }

    public IConnectionMultiplexer RedisMultiplexer { get; } = Substitute.For<IConnectionMultiplexer>();

    public IConnectionMultiplexer FalkorDbMultiplexer { get; } = Substitute.For<IConnectionMultiplexer>();

    public IDatabase RedisDatabase { get; } = Substitute.For<IDatabase>();

    public IDatabase FalkorDbDatabase { get; } = Substitute.For<IDatabase>();

    public DaprClient DaprClient { get; } = Substitute.For<DaprClient>();

    public Dapr.Actors.Client.IActorProxyFactory ActorProxyFactory { get; } = Substitute.For<Dapr.Actors.Client.IActorProxyFactory>();

    public CapturingAuditLoggerProvider AuditLogs { get; } = new();

    public CapturingEventStoreLogProvider EventStoreLogs { get; } = new();

    public EventStoreWebAppFactory()
    {
        RedisMultiplexer.GetDatabase(Arg.Any<int>(), Arg.Any<object>()).Returns(RedisDatabase);
        FalkorDbMultiplexer.GetDatabase(Arg.Any<int>(), Arg.Any<object>()).Returns(FalkorDbDatabase);
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");

        // Sentinel connection strings so the production keyed-multiplexer factory does not throw before
        // the ConfigureTestServices override replaces the registrations.
        builder.UseSetting("ConnectionStrings:redis", "localhost:0,abortConnect=false,connectTimeout=1");
        builder.UseSetting("ConnectionStrings:falkordb", "localhost:0,abortConnect=false,connectTimeout=1");
        builder.ConfigureAppConfiguration((context, configuration) =>
        {
            Dictionary<string, string?> settings = new(StringComparer.OrdinalIgnoreCase)
            {
                ["Authentication:JwtBearer:Issuer"] = "hexalith-memories-test",
                ["Authentication:JwtBearer:Audience"] = "hexalith-memories-server",
                ["Authentication:JwtBearer:SigningKey"] = "hexalith-memories-test-signing-key-32b",
                ["Authentication:JwtBearer:RequireHttpsMetadata"] = "false",
            };

            _ = configuration.AddInMemoryCollection(settings);
            _ = context;
        });

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
            services.RemoveAll<IIngestionWorkflowStateReader>();
            services.RemoveAll<IMemoriesCommandStore>();
            services.RemoveAll<ICaseProjectionWorkflowScheduler>();
            services.AddSingleton(Router);
            services.AddSingleton(Scheduler);
            services.AddSingleton(TenantStatus);
            services.AddSingleton(CaseCreator);
            services.AddSingleton(Telemetry);
            services.AddSingleton(PreflightDedup);
            services.AddSingleton(IngestionWorkflowStateReader);
            services.AddSingleton(MemoriesCommandStore);
            services.AddSingleton(CaseProjectionWorkflowScheduler);

            if (FailedUnitsRegistryOverride is not null)
            {
                services.RemoveAll<IFailedUnitsRegistry>();
                services.AddSingleton(FailedUnitsRegistryOverride);
            }

            if (WorkflowPayloadStoreOverride is not null)
            {
                services.RemoveAll<IWorkflowPayloadStore>();
                services.AddSingleton(WorkflowPayloadStoreOverride);
            }

            if (IngestionWorkflowSchedulerOverride is not null)
            {
                services.RemoveAll<IIngestionWorkflowScheduler>();
                services.AddSingleton(IngestionWorkflowSchedulerOverride);
            }

            // Endpoint tests drive adapters directly; infrastructure services would resolve DAPR/backends at startup.
            services.RemoveInfrastructureHostedServices();

            services.AddSingleton<ILoggerProvider>(AuditLogs);
            services.AddSingleton<ILoggerProvider>(EventStoreLogs);
        });
    }
}
