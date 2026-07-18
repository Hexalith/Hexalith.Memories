// <copyright file="AccessTelemetryLifecycleBootstrapService.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Telemetry.AccessTelemetryLifecycle;

using System.Globalization;

using Dapr.Client;

using Hexalith.Memories.AccessTelemetry.Contracts;

/// <summary>Loads authoritative retention and marker-key material without blocking business startup.</summary>
internal sealed class AccessTelemetryLifecycleBootstrapService(
    DaprClient daprClient,
    AccessTelemetryOptions configuredOptions,
    AccessTelemetrySanitizerAccessor sanitizerAccessor,
    AccessTelemetryLifecycleStatus status,
    TimeProvider timeProvider,
    MonotonicRecordIdGenerator recordIds,
    IHostEnvironment environment) : BackgroundService
{
    private const string RetentionConfigurationKey = "retentionSeconds";

    /// <inheritdoc/>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                AccessTelemetryOptions options = await ResolveRetentionAsync(stoppingToken).ConfigureAwait(false);
                AccessTelemetryOptionsValidationResult validation = AccessTelemetryOptionsValidator.Validate(options, environment.EnvironmentName);
                if (!validation.IsValid || validation.EffectiveRetention is null)
                {
                    status.Publish(AccessTelemetryHealthState.Unhealthy, AccessTelemetryReason.ConfigurationInvalid);
                    return;
                }

                Dictionary<string, string> secret = await daprClient.GetSecretAsync(
                    options.SecretStoreName,
                    options.MarkerKeyReference,
                    cancellationToken: stoppingToken).ConfigureAwait(false);
                if (!secret.TryGetValue(options.MarkerKeyReference, out string? encodedMarkerKey) || string.IsNullOrWhiteSpace(encodedMarkerKey))
                {
                    status.Publish(AccessTelemetryHealthState.Unhealthy, AccessTelemetryReason.ConfigurationInvalid);
                    return;
                }

                byte[] markerKey = Convert.FromBase64String(encodedMarkerKey);
                sanitizerAccessor.Publish(new AccessTelemetrySanitizer(
                    markerKey,
                    options.MarkerKeyGeneration,
                    timeProvider,
                    recordIds,
                    validation.EffectiveRetention.Value));
                status.Publish(AccessTelemetryHealthState.Healthy, AccessTelemetryReason.None);
                return;
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception exception) when (exception is FormatException or ArgumentException or OverflowException)
            {
                status.Publish(AccessTelemetryHealthState.Unhealthy, AccessTelemetryReason.ConfigurationInvalid);
                return;
            }
            catch (Exception exception) when (exception is not OutOfMemoryException and not StackOverflowException)
            {
                status.Publish(AccessTelemetryHealthState.Degraded, AccessTelemetryReason.RemoteValidationPending);
                await Task.Delay(TimeSpan.FromSeconds(5), timeProvider, stoppingToken).ConfigureAwait(false);
            }
        }
    }

    private async Task<AccessTelemetryOptions> ResolveRetentionAsync(CancellationToken cancellationToken)
    {
        if (configuredOptions.RetentionSource != RetentionConfigurationSource.DaprConfiguration)
        {
            return configuredOptions;
        }

        GetConfigurationResponse response = await daprClient.GetConfiguration(
            configuredOptions.ConfigurationStoreName,
            [RetentionConfigurationKey],
            new Dictionary<string, string>(StringComparer.Ordinal),
            cancellationToken).ConfigureAwait(false);
        if (!response.Items.TryGetValue(RetentionConfigurationKey, out ConfigurationItem? item) ||
            !int.TryParse(item.Value, NumberStyles.None, CultureInfo.InvariantCulture, out int retentionSeconds))
        {
            return configuredOptions with { Retention = null };
        }

        return configuredOptions with { Retention = TimeSpan.FromSeconds(retentionSeconds) };
    }
}
