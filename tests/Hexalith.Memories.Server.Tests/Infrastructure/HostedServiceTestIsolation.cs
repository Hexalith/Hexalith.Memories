// <copyright file="HostedServiceTestIsolation.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Tests.Infrastructure;

using Hexalith.Memories.Server.Telemetry;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

/// <summary>
/// Removes infrastructure background services from in-memory endpoint test hosts.
/// </summary>
internal static class HostedServiceTestIsolation
{
    /// <summary>
    /// Removes external-infrastructure <see cref="IHostedService"/> registrations from a test service collection.
    /// </summary>
    /// <param name="services">The service collection to update.</param>
    public static void RemoveInfrastructureHostedServices(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        List<ServiceDescriptor> descriptors = [.. services.Where(IsInfrastructureHostedService)];

        foreach (ServiceDescriptor descriptor in descriptors)
        {
            services.Remove(descriptor);
        }
    }

    private static bool IsInfrastructureHostedService(ServiceDescriptor descriptor)
    {
        if (descriptor.ServiceType != typeof(IHostedService) ||
            descriptor.ImplementationType is null ||
            descriptor.ImplementationType == typeof(RollingCounterStore))
        {
            return false;
        }

        string? assemblyName = descriptor.ImplementationType.Assembly.GetName().Name;
        if (assemblyName is not null && assemblyName.StartsWith("Dapr.", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        string? typeName = descriptor.ImplementationType.FullName;
        return typeName is
            "Hexalith.Memories.Server.NaturalLanguage.NaturalLanguageEmbeddingRetryHostedService" or
            "Hexalith.Memories.Server.Hosting.OrphanSemanticIndexReconciler" or
            "Hexalith.Memories.Server.Hosting.IsStubBackfillMigrationHostedService" or
            "Hexalith.Memories.Server.Hosting.WorkflowReplaySafetyHostedService" or
            "Hexalith.Memories.Server.EventStoreIntegration.EventStoreRoutingConfigValidator" or
            "Hexalith.Memories.Server.EventStoreIntegration.EventStoreObservationStartupActivator" or
            "Hexalith.Memories.Server.EventStoreIntegration.RoutedTenantProvisioningStartupService";
    }
}
