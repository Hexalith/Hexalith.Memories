// <copyright file="AccessTelemetryCapabilityProbeHostedService.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.AccessTelemetry.Capability;

using Hexalith.Memories.AccessTelemetry.Contracts;

/// <summary>Runs exact-profile probes before the first write in each process/configuration epoch.</summary>
internal sealed class AccessTelemetryCapabilityProbeHostedService(
    AccessTelemetryCapabilityProbeRunner runner,
    AccessTelemetryOptions options,
    AccessTelemetryCapabilityEvidenceOptions evidence,
    IHostEnvironment environment) : IHostedService
{
    /// <inheritdoc/>
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var context = new AccessTelemetryCapabilityProbeContext
        {
            ComponentProfileHash = options.ComponentProfileHash,
            ExactVersionPinned = evidence.ExactVersionPinned,
            DaprOnlyBoundary = true,
            Production = environment.IsProduction(),
            AllowAlpha = options.AllowAlphaComponent,
            IsAlpha = options.ComponentIsAlpha,
            CapacityEvidenceId = options.CapacityEvidenceId,
            PhysicalReclamationEvidenceId = options.PhysicalReclamationEvidenceId,
            ValidUntilUtc = evidence.ValidUntilUtc,
        };
        _ = await runner.RunAsync(context, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
