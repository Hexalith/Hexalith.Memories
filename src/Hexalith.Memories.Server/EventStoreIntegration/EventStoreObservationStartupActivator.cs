// <copyright file="EventStoreObservationStartupActivator.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.EventStoreIntegration;

using Hexalith.Memories.EventStore;

using Microsoft.Extensions.Hosting;

/// <summary>One-shot startup activator that eagerly resolves <see cref="IEventIngestionTelemetry"/> so
/// constructor-time startup probes in the server-owned adapter run during actual host startup rather than
/// being deferred until the first event-ingestion request.</summary>
internal sealed class EventStoreObservationStartupActivator : IHostedService
{
    private readonly IEventIngestionTelemetry _telemetry;

    public EventStoreObservationStartupActivator(IEventIngestionTelemetry telemetry)
    {
        ArgumentNullException.ThrowIfNull(telemetry);
        _telemetry = telemetry;
    }

    /// <inheritdoc/>
    public Task StartAsync(CancellationToken cancellationToken)
    {
        _ = _telemetry;
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}