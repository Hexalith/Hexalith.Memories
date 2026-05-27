// <copyright file="EventStoreObservationStartupActivatorTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Tests.EventStoreIntegration;

using Hexalith.Memories.EventStore;
using Hexalith.Memories.Server.EventStoreIntegration;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using NSubstitute;

using Shouldly;

/// <summary>Guard tests for the one-shot startup activator that eagerly resolves the server-side
/// EventStore telemetry adapter so constructor-time startup probes execute during host startup.</summary>
public sealed class EventStoreObservationStartupActivatorTests
{
    [Fact]
    public async Task StartAsync_WithTelemetryDependency_ShouldCompleteWithoutError()
    {
        EventStoreObservationStartupActivator service =
            new(Substitute.For<IEventIngestionTelemetry>());

        await Should.NotThrowAsync(() => service.StartAsync(CancellationToken.None));
        await Should.NotThrowAsync(() => service.StopAsync(CancellationToken.None));
    }

    [Fact]
    public void AddServerEventStoreIntegration_RegistersObservationStartupActivator()
    {
        ServiceCollection services = [];
        IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection(
            new Dictionary<string, string?>
            {
                ["EventStoreIntegration:Routing:Topic"] = "memories-events",
            }).Build();

        _ = services.AddServerEventStoreIntegration(configuration);

        services.ShouldContain(descriptor =>
            descriptor.ServiceType == typeof(IHostedService)
            && descriptor.ImplementationType == typeof(EventStoreObservationStartupActivator));
    }
}