// <copyright file="AccessTelemetryHeartbeatWorker.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Telemetry.AccessTelemetryLifecycle;

using Hexalith.Memories.AccessTelemetry.Contracts;

/// <summary>Maintains a bounded 30-second writer lease at a 10-second cadence.</summary>
internal sealed class AccessTelemetryHeartbeatWorker(
    IAccessTelemetryHeartbeatClient client,
    AccessTelemetryOptions options,
    AccessTelemetryWriterIdentity identity,
    BoundedAccessTelemetryQueue queue,
    AccessTelemetryLifecycleStatus status,
    TimeProvider timeProvider) : BackgroundService
{
    /// <summary>Gets the fixed writer heartbeat cadence.</summary>
    public static readonly TimeSpan HeartbeatInterval = TimeSpan.FromSeconds(10);

    private static readonly TimeSpan LeaseDuration = TimeSpan.FromSeconds(30);
    private string _activeGeneration = options.MarkerKeyGeneration;

    /// <summary>Sends one heartbeat for focused verification.</summary>
    internal async Task SendOnceAsync(CancellationToken cancellationToken)
    {
        DateTimeOffset now = timeProvider.GetUtcNow();
        WriterHeartbeatResponse response = await client.SendAsync(
            new WriterHeartbeat
            {
                DeploymentId = options.DeploymentId,
                ServiceInstanceId = identity.ServiceInstanceId,
                ProcessEpoch = identity.ProcessEpoch,
                MarkerKeyGeneration = options.MarkerKeyGeneration,
                OldKeyQueueCount = string.Equals(_activeGeneration, options.MarkerKeyGeneration, StringComparison.Ordinal)
                    ? 0
                    : queue.CountByMarkerKey(_activeGeneration),
                LeaseExpiresAtUnixMilliseconds = now.Add(LeaseDuration).ToUnixTimeMilliseconds(),
            },
            cancellationToken).ConfigureAwait(false);
        if (!response.Accepted)
        {
            if (response.Reason is AccessTelemetryReason.ConfigurationInvalid or AccessTelemetryReason.RecordIdConflict)
            {
                status.PublishTerminal(response.Reason);
            }
            else
            {
                status.Publish(AccessTelemetryHealthState.Unhealthy, response.Reason);
            }

            return;
        }

        _activeGeneration = response.ActiveGeneration;
    }

    /// <inheritdoc/>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await SendOnceAsync(stoppingToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception exception) when (exception is not OutOfMemoryException and not StackOverflowException)
            {
                status.Publish(AccessTelemetryHealthState.Degraded, AccessTelemetryReason.DependencyUnavailable);
                ServerAccessTelemetryLifecycleMetrics.Record(AccessTelemetryRecordState.Failed, AccessTelemetryReason.DependencyUnavailable);
            }

            await Task.Delay(HeartbeatInterval, timeProvider, stoppingToken).ConfigureAwait(false);
        }
    }
}
