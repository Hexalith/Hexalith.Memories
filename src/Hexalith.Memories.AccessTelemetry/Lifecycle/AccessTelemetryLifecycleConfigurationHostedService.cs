// <copyright file="AccessTelemetryLifecycleConfigurationHostedService.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.AccessTelemetry.Lifecycle;

using System.Globalization;

using Dapr.Client;

using Hexalith.Memories.AccessTelemetry.Capability;
using Hexalith.Memories.AccessTelemetry.Contracts;

/// <summary>Polls authoritative Dapr retention and publishes only validated runtime snapshots.</summary>
internal sealed class AccessTelemetryLifecycleConfigurationHostedService(
    DaprClient daprClient,
    AccessTelemetryOptions configured,
    AccessTelemetryRuntimeOptionsProvider optionsProvider,
    AccessTelemetryRuntimeGate runtimeGate,
    IHostEnvironment environment,
    TimeProvider timeProvider) : BackgroundService
{
    private const string RetentionConfigurationKey = "retentionSeconds";
    private static readonly TimeSpan RefreshInterval = TimeSpan.FromSeconds(10);

    /// <inheritdoc/>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                AccessTelemetryOptions resolved = await ResolveAsync(stoppingToken).ConfigureAwait(false);
                AccessTelemetryOptionsValidationResult validation = AccessTelemetryOptionsValidator.Validate(resolved, environment.EnvironmentName);
                if (!validation.IsValid || validation.EffectiveRetention is null)
                {
                    optionsProvider.FailClosed();
                    runtimeGate.Publish(new AccessTelemetryCapabilityGateResult(
                        false,
                        true,
                        AccessTelemetryHealthState.Unhealthy,
                        AccessTelemetryReason.ConfigurationInvalid));
                }
                else
                {
                    optionsProvider.Publish(resolved with { Retention = validation.EffectiveRetention });
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception exception) when (exception is not OutOfMemoryException and not StackOverflowException)
            {
                optionsProvider.FailClosed();
                runtimeGate.Publish(new AccessTelemetryCapabilityGateResult(
                    false,
                    false,
                    AccessTelemetryHealthState.Degraded,
                    AccessTelemetryReason.RemoteValidationPending));
            }

            await Task.Delay(RefreshInterval, timeProvider, stoppingToken).ConfigureAwait(false);
        }
    }

    private async Task<AccessTelemetryOptions> ResolveAsync(CancellationToken cancellationToken)
    {
        if (configured.RetentionSource != RetentionConfigurationSource.DaprConfiguration)
        {
            return configured;
        }

        GetConfigurationResponse response = await daprClient.GetConfiguration(
            configured.ConfigurationStoreName,
            [RetentionConfigurationKey],
            new Dictionary<string, string>(StringComparer.Ordinal),
            cancellationToken).ConfigureAwait(false);
        if (!response.Items.TryGetValue(RetentionConfigurationKey, out ConfigurationItem? item) ||
            !int.TryParse(item.Value, NumberStyles.None, CultureInfo.InvariantCulture, out int seconds))
        {
            return configured with { Retention = null };
        }

        return configured with { Retention = TimeSpan.FromSeconds(seconds) };
    }
}
