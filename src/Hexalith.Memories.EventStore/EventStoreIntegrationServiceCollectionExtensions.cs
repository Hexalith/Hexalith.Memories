// <copyright file="EventStoreIntegrationServiceCollectionExtensions.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.EventStore;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

/// <summary>Registers the EventStore pub/sub integration services in an ASP.NET Core application.
///
/// <para>Host responsibilities before calling this extension:</para>
/// <list type="bullet">
///   <item><description>Call <c>AddControllers().AddApplicationPart(typeof(EventIngestionController).Assembly)</c>
///       so the MVC controller discovery picks up <see cref="EventIngestionController"/>.</description></item>
///   <item><description>Register Server-specific adapter implementations for the package-owned abstractions:
///       <see cref="IEventIngestionWorkflowScheduler"/>, <see cref="ITenantStatusAccessor"/>,
///       <see cref="ICaseCreationService"/>, <see cref="IEventIngestionTelemetry"/>,
///       <see cref="IPreflightDedupStore"/>.</description></item>
/// </list>
/// </summary>
public static class EventStoreIntegrationServiceCollectionExtensions
{
    /// <summary>Registers EventStore integration services. Adapter interfaces (workflow scheduler, tenant
    /// status, case creation, telemetry, preflight dedup) must be registered by the host.</summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">Host configuration; the <c>EventStoreIntegration:Routing</c> section is bound
    /// to <see cref="TenantEventRoutingOptions"/>.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddMemoriesEventStoreIntegration(
        this IServiceCollection services,
        IConfiguration configuration,
        Action<EventStoreIntegrationBuilder>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        EventStoreIntegrationBuilder builder = new(services);
        string? resolvedTopic = ResolveConfiguredTopic(configuration);

        services.AddOptions<TenantEventRoutingOptions>()
            .Bind(configuration.GetSection("EventStoreIntegration:Routing"))
            .ValidateOnStart();

        services.TryAddEnumerable(
            ServiceDescriptor.Singleton<IValidateOptions<TenantEventRoutingOptions>, TenantEventRoutingOptionsValidator>());

        services.PostConfigure<TenantEventRoutingOptions>(options =>
        {
            options.PubSubName = string.IsNullOrWhiteSpace(options.PubSubName)
                ? EventIngestionController.PubSubName
                : options.PubSubName.Trim();

            if (!string.IsNullOrWhiteSpace(options.Topic))
            {
                options.Topic = options.Topic.Trim();
            }
        });

        if (!string.IsNullOrWhiteSpace(resolvedTopic))
        {
            services.PostConfigure<TenantEventRoutingOptions>(options =>
            {
                if (string.IsNullOrWhiteSpace(options.Topic))
                {
                    options.Topic = resolvedTopic;
                }
            });

            Environment.SetEnvironmentVariable(EventIngestionController.TopicEnvVar, resolvedTopic);
        }

        services
            .AddControllers()
            .AddApplicationPart(typeof(EventIngestionController).Assembly);

        services.TryAddSingleton<IEventIngestionWorkflowScheduler, DaprEventIngestionWorkflowScheduler>();
        services.TryAddSingleton<ITenantStatusAccessor, MissingTenantStatusAccessor>();
        services.TryAddSingleton<ICaseCreationService, MissingCaseCreationService>();
        services.TryAddSingleton<ISearchIndexMaintenance, MissingSearchIndexMaintenance>();
        services.TryAddSingleton<IEventIngestionTelemetry, NoOpEventIngestionTelemetry>();
        // spec-infrastructure-dependency-abstraction (F6, Decision D30, ADR-IDA-001): the atomic-reserve
        // dedup store stays on direct Redis; the KV/set mapping and observation stores are on Dapr state.
        services.TryAddSingleton(TimeProvider.System);
        services.AddOptions<EventStoreStateStoreOptions>()
            .Bind(configuration.GetSection(EventStoreStateStoreOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();
        services.TryAddSingleton<IPreflightDedupStore, RedisPreflightDedupStore>();
        services.TryAddSingleton<IAggregateCaseMappingStore, DaprAggregateCaseMappingStore>();
        services.TryAddSingleton<ITenantEventRouter, TenantEventRouter>();
        services.TryAddSingleton<ITenantEventRouteCacheInvalidator>(
            sp => (ITenantEventRouteCacheInvalidator)sp.GetRequiredService<ITenantEventRouter>());
        services.TryAddSingleton<IEventIngestionService, EventIngestionService>();

        // Story 9.3 — observation store + kill-switch options.
        services.TryAddSingleton<IObservedEventTypeStore, DaprObservedEventTypeStore>();
        services.TryAddSingleton<IProjectionBindingProvider, DefaultProjectionBindingProvider>();
        services.AddOptions<EventStoreObservationOptions>()
            .Bind(configuration.GetSection("EventStoreIntegration:Observation"));

        configure?.Invoke(builder);

        return services;
    }

    internal static string? ResolveConfiguredTopic(IConfiguration configuration)
    {
        string? configuredTopic = configuration[$"EventStoreIntegration:Routing:{nameof(TenantEventRoutingOptions.Topic)}"];
        if (!string.IsNullOrWhiteSpace(configuredTopic))
        {
            return configuredTopic.Trim();
        }

        // spec-infrastructure-dependency-abstraction (F8, Decision D30; review P7): the AppHost-injected
        // topic env var is read through IConfiguration. Hosts MUST include the environment-variables
        // configuration provider (the default host builders do) so AppHost-injected values are visible;
        // a configuration root built without that provider will miss the env override.
        string? environmentTopic = configuration[EventIngestionController.TopicEnvVar];
        return string.IsNullOrWhiteSpace(environmentTopic) ? null : environmentTopic.Trim();
    }
}
