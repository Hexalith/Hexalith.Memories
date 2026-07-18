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
    TimeProvider timeProvider) : BackgroundService
{
    /// <summary>Gets the fixed writer heartbeat cadence.</summary>
    public static readonly TimeSpan HeartbeatInterval = TimeSpan.FromSeconds(10);

    private static readonly TimeSpan LeaseDuration = TimeSpan.FromSeconds(30);

    /// <summary>Sends one heartbeat for focused verification.</summary>
    internal Task SendOnceAsync(CancellationToken cancellationToken)
    {
        DateTimeOffset now = timeProvider.GetUtcNow();
        return client.SendAsync(
            new WriterHeartbeat
            {
                DeploymentId = options.DeploymentId,
                ServiceInstanceId = identity.ServiceInstanceId,
                ProcessEpoch = identity.ProcessEpoch,
                MarkerKeyGeneration = options.MarkerKeyGeneration,
                OldKeyQueueCount = 0,
                LeaseExpiresAtUnixMilliseconds = now.Add(LeaseDuration).ToUnixTimeMilliseconds(),
            },
            cancellationToken);
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
                ServerAccessTelemetryLifecycleMetrics.Record(AccessTelemetryRecordState.Failed, AccessTelemetryReason.DependencyUnavailable);
            }

            await Task.Delay(HeartbeatInterval, timeProvider, stoppingToken).ConfigureAwait(false);
        }
    }
}
