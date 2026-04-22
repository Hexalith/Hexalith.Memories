// <copyright file="EventStoreIntegrationBuilder.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.EventStore;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

/// <summary>Fluent configuration surface for host-specific EventStore integration overrides.
/// The package registers sane defaults for the workflow scheduler, telemetry, Redis-backed preflight dedup,
/// and Redis-backed aggregate-to-case mapping. Hosts may replace any adapter that needs local knowledge
/// (for example tenant-status or case-creation accessors) while still composing everything through a single
/// <see cref="EventStoreIntegrationServiceCollectionExtensions.AddMemoriesEventStoreIntegration(IServiceCollection, Microsoft.Extensions.Configuration.IConfiguration, Action{EventStoreIntegrationBuilder}?)"/> call.</summary>
public sealed class EventStoreIntegrationBuilder
{
    internal EventStoreIntegrationBuilder(IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        Services = services;
    }

    internal IServiceCollection Services { get; }

    /// <summary>Replaces the default tenant-status accessor.</summary>
    /// <typeparam name="TImplementation">The implementation type.</typeparam>
    /// <returns>The current builder.</returns>
    public EventStoreIntegrationBuilder AddTenantStatusAccessor<TImplementation>()
        where TImplementation : class, ITenantStatusAccessor
    {
        Services.Replace(ServiceDescriptor.Singleton<ITenantStatusAccessor, TImplementation>());
        return this;
    }

    /// <summary>Replaces the default case-creation adapter.</summary>
    /// <typeparam name="TImplementation">The implementation type.</typeparam>
    /// <returns>The current builder.</returns>
    public EventStoreIntegrationBuilder AddCaseCreationService<TImplementation>()
        where TImplementation : class, ICaseCreationService
    {
        Services.Replace(ServiceDescriptor.Singleton<ICaseCreationService, TImplementation>());
        return this;
    }

    /// <summary>Replaces the default workflow scheduler.</summary>
    /// <typeparam name="TImplementation">The implementation type.</typeparam>
    /// <returns>The current builder.</returns>
    public EventStoreIntegrationBuilder AddWorkflowScheduler<TImplementation>()
        where TImplementation : class, IEventIngestionWorkflowScheduler
    {
        Services.Replace(ServiceDescriptor.Singleton<IEventIngestionWorkflowScheduler, TImplementation>());
        return this;
    }

    /// <summary>Replaces the default telemetry adapter.</summary>
    /// <typeparam name="TImplementation">The implementation type.</typeparam>
    /// <returns>The current builder.</returns>
    public EventStoreIntegrationBuilder AddTelemetry<TImplementation>()
        where TImplementation : class, IEventIngestionTelemetry
    {
        Services.Replace(ServiceDescriptor.Singleton<IEventIngestionTelemetry, TImplementation>());
        return this;
    }

    /// <summary>Replaces the default preflight dedup store.</summary>
    /// <typeparam name="TImplementation">The implementation type.</typeparam>
    /// <returns>The current builder.</returns>
    public EventStoreIntegrationBuilder AddPreflightDedupStore<TImplementation>()
        where TImplementation : class, IPreflightDedupStore
    {
        Services.Replace(ServiceDescriptor.Singleton<IPreflightDedupStore, TImplementation>());
        return this;
    }

    /// <summary>Replaces the default aggregate-to-case mapping store.</summary>
    /// <typeparam name="TImplementation">The implementation type.</typeparam>
    /// <returns>The current builder.</returns>
    public EventStoreIntegrationBuilder AddAggregateCaseMappingStore<TImplementation>()
        where TImplementation : class, IAggregateCaseMappingStore
    {
        Services.Replace(ServiceDescriptor.Singleton<IAggregateCaseMappingStore, TImplementation>());
        return this;
    }

    /// <summary>Adds a host-owned startup validator or initialization service.</summary>
    /// <typeparam name="TImplementation">The hosted service type.</typeparam>
    /// <returns>The current builder.</returns>
    public EventStoreIntegrationBuilder AddHostedValidation<TImplementation>()
        where TImplementation : class, IHostedService
    {
        Services.AddHostedService<TImplementation>();
        return this;
    }
}
