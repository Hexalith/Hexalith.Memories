// <copyright file="ServerEventStoreIntegrationExtensions.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.EventStoreIntegration;

using Hexalith.Memories.EventStore;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

/// <summary>Server-side composition root that wires every adapter needed by
/// <see cref="EventStoreIntegrationServiceCollectionExtensions.AddMemoriesEventStoreIntegration"/>.
/// Call from <c>Program.cs</c> after DI registrations so the EventStore package resolves its
/// adapter dependencies at runtime.</summary>
internal static class ServerEventStoreIntegrationExtensions
{
    /// <summary>Registers the EventStore package and the Server-owned adapter implementations.</summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">Host configuration.</param>
    /// <returns>The service collection for chaining.</returns>
    internal static IServiceCollection AddServerEventStoreIntegration(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddMemoriesEventStoreIntegration(
            configuration,
            builder => builder
                .AddWorkflowScheduler<EventIngestionWorkflowSchedulerAdapter>()
                .AddTenantStatusAccessor<TenantStatusAccessorAdapter>()
                .AddCaseCreationService<CaseCreationServiceAdapter>()
                .AddTelemetry<EventIngestionTelemetryAdapter>());

        services.TryAddSingleton<IEventIngestionWorkflowScheduler, EventIngestionWorkflowSchedulerAdapter>();
        services.TryAddSingleton<ITenantStatusAccessor, TenantStatusAccessorAdapter>();
        services.TryAddSingleton<ICaseCreationService, CaseCreationServiceAdapter>();
        services.TryAddSingleton<IEventIngestionTelemetry, EventIngestionTelemetryAdapter>();

        services.AddHostedService<EventStoreRoutingConfigValidator>();
        services.AddHostedService<EventStoreObservationStartupActivator>();

        return services;
    }
}
