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
    IConfiguration configuration,
    TimeProvider timeProvider) : BackgroundService
{
    /// <inheritdoc/>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            _ = await ProbeCurrentAsync(stoppingToken).ConfigureAwait(false);

            await Task.Delay(TimeSpan.FromSeconds(30), timeProvider, stoppingToken).ConfigureAwait(false);
        }
    }
    /// <summary>Runs the same bounded context construction used by each hosted iteration.</summary>
    internal async Task<AccessTelemetryCapabilityGateResult?> ProbeCurrentAsync(CancellationToken cancellationToken)
    {
        if (!optionsProvider.IsReady) return null;
        AccessTelemetryOptions options = optionsProvider.Current;
        DateTimeOffset now = timeProvider.GetUtcNow();
        bool localIntent = !string.IsNullOrEmpty(options.LocalFunctionalRunId) || !string.IsNullOrEmpty(options.LocalFunctionalProfileJson) || !string.IsNullOrEmpty(options.LocalFunctionalSourceMappingHash);
        bool bound = environment.IsDevelopment() && !string.IsNullOrEmpty(options.LocalFunctionalRunId) && options.LocalFunctionalRunId == options.DeploymentId && options.LocalFunctionalRunId == configuration["HEXALITH_G4_RUN_ID"] && options.LocalFunctionalSourceMappingHash == configuration["HEXALITH_SOURCE_MAPPING_HASH"];
        DateTimeOffset validUntil = evidence.ValidUntilUtc == default && !environment.IsProduction() && !localIntent ? now.AddMinutes(1) : evidence.ValidUntilUtc;
        var context = new AccessTelemetryCapabilityProbeContext
        {
            LocalFunctional = localIntent,
            CurrentRunId = bound ? configuration["HEXALITH_G4_RUN_ID"]! : string.Empty,
            CurrentSourceMappingHash = configuration["HEXALITH_SOURCE_MAPPING_HASH"] ?? string.Empty,
            LocalFunctionalProfileJson = options.LocalFunctionalProfileJson,
            ComponentProfileHash = options.ComponentProfileHash,
            ExactVersionPinned = evidence.ExactVersionPinned || !environment.IsProduction(),
            DaprOnlyBoundary = true, Production = environment.IsProduction(), AllowAlpha = options.AllowAlphaComponent, IsAlpha = options.ComponentIsAlpha,
            CapacityEvidenceId = options.CapacityEvidenceId, PhysicalReclamationEvidenceId = options.PhysicalReclamationEvidenceId, ValidUntilUtc = validUntil,
        };
        return await runner.RunAsync(context, cancellationToken).ConfigureAwait(false);
    }

}
