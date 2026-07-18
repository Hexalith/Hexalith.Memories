// <copyright file="AccessTelemetryCapabilityProbeHostedService.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.AccessTelemetry.Capability;

using Hexalith.Memories.AccessTelemetry.Contracts;

/// <summary>Runs exact-profile probes before the first write in each process/configuration epoch.</summary>
internal sealed class AccessTelemetryCapabilityProbeHostedService(
    AccessTelemetryCapabilityProbeRunner runner,
    Hexalith.Memories.AccessTelemetry.Lifecycle.AccessTelemetryRuntimeOptionsProvider optionsProvider,
    AccessTelemetryCapabilityEvidenceOptions evidence,
    IHostEnvironment environment,
    TimeProvider timeProvider) : BackgroundService
{
    /// <inheritdoc/>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            if (optionsProvider.IsReady)
            {
                AccessTelemetryOptions options = optionsProvider.Current;
                DateTimeOffset now = timeProvider.GetUtcNow();
                DateTimeOffset validUntil = evidence.ValidUntilUtc == default && !environment.IsProduction()
                    ? now.AddMinutes(1)
                    : evidence.ValidUntilUtc;
                var context = new AccessTelemetryCapabilityProbeContext
                {
                    ComponentProfileHash = options.ComponentProfileHash,
                    ExactVersionPinned = evidence.ExactVersionPinned || !environment.IsProduction(),
                    DaprOnlyBoundary = true,
                    Production = environment.IsProduction(),
                    AllowAlpha = options.AllowAlphaComponent,
                    IsAlpha = options.ComponentIsAlpha,
                    CapacityEvidenceId = options.CapacityEvidenceId,
                    PhysicalReclamationEvidenceId = options.PhysicalReclamationEvidenceId,
                    ValidUntilUtc = validUntil,
                };
                _ = await runner.RunAsync(context, stoppingToken).ConfigureAwait(false);
            }

            await Task.Delay(TimeSpan.FromSeconds(30), timeProvider, stoppingToken).ConfigureAwait(false);
        }
    }
}
