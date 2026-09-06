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
    private static readonly TimeSpan RefreshInterval = TimeSpan.FromSeconds(10);
    private string? _publishedFingerprint;
    private string? _publishedMarkerKeyHash;
    private string? _publishedMarkerKeyGeneration;
    private bool _validatedOnce;

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
                    if (_validatedOnce)
                    {
                        sanitizerAccessor.Clear();
                        status.PublishTerminal(AccessTelemetryReason.ConfigurationInvalid);
                        return;
                    }

                    status.Publish(AccessTelemetryHealthState.Unhealthy, AccessTelemetryReason.ConfigurationInvalid);
                    await Task.Delay(RefreshInterval, timeProvider, stoppingToken).ConfigureAwait(false);
                    continue;
                }

                Dictionary<string, string> secret = await daprClient.GetSecretAsync(
                    options.SecretStoreName,
                    options.MarkerKeyReference,
                    cancellationToken: stoppingToken).ConfigureAwait(false);
                if (!secret.TryGetValue(options.MarkerKeyReference, out string? encodedMarkerKey) || string.IsNullOrWhiteSpace(encodedMarkerKey))
                {
                    if (_validatedOnce)
                    {
                        sanitizerAccessor.Clear();
                        status.PublishTerminal(AccessTelemetryReason.ConfigurationInvalid);
                        return;
                    }

                    status.Publish(AccessTelemetryHealthState.Unhealthy, AccessTelemetryReason.ConfigurationInvalid);
                    await Task.Delay(RefreshInterval, timeProvider, stoppingToken).ConfigureAwait(false);
                    continue;
                }

                byte[] markerKey = Convert.FromBase64String(encodedMarkerKey);
                AccessTelemetryRuntimeValidationResponse remote = await ValidateRemoteAsync(options, stoppingToken).ConfigureAwait(false);
                if (!remote.AllowsWrites)
                {
                    status.Publish(
                        remote.Reason == AccessTelemetryReason.ConfigurationInvalid
                            ? AccessTelemetryHealthState.Unhealthy
                            : AccessTelemetryHealthState.Degraded,
                        remote.Reason);
                    if (_validatedOnce && remote.Reason == AccessTelemetryReason.ConfigurationInvalid)
                    {
                        sanitizerAccessor.Clear();
                        status.PublishTerminal(AccessTelemetryReason.ConfigurationInvalid);
                        return;
                    }

                    await Task.Delay(RefreshInterval, timeProvider, stoppingToken).ConfigureAwait(false);
                    continue;
                }

                string markerKeyHash = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(markerKey));
                if (_validatedOnce &&
                    !string.Equals(markerKeyHash, _publishedMarkerKeyHash, StringComparison.Ordinal) &&
                    string.Equals(options.MarkerKeyGeneration, _publishedMarkerKeyGeneration, StringComparison.Ordinal))
                {
                    sanitizerAccessor.Clear();
                    status.PublishTerminal(AccessTelemetryReason.ConfigurationInvalid);
                    return;
                }

                string fingerprint = string.Create(
                    CultureInfo.InvariantCulture,
                    $"{options.ConfigurationEpoch}|{options.ComponentProfileHash}|{options.MarkerKeyGeneration}|{validation.EffectiveRetention.Value.Ticks}|{markerKeyHash}");
                if (!string.Equals(fingerprint, _publishedFingerprint, StringComparison.Ordinal))
                {
                    sanitizerAccessor.Publish(new AccessTelemetrySanitizer(
                        markerKey,
                        options.MarkerKeyGeneration,
                        timeProvider,
                        recordIds,
                        validation.EffectiveRetention.Value,
                        environment.IsEnvironment("Qualification")));
                    _publishedFingerprint = fingerprint;
                    _publishedMarkerKeyHash = markerKeyHash;
                    _publishedMarkerKeyGeneration = options.MarkerKeyGeneration;
                }

                _validatedOnce = true;
                status.Publish(AccessTelemetryHealthState.Healthy, AccessTelemetryReason.None);
                await Task.Delay(RefreshInterval, timeProvider, stoppingToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception exception) when (exception is FormatException or ArgumentException or OverflowException)
            {
                status.Publish(AccessTelemetryHealthState.Unhealthy, AccessTelemetryReason.ConfigurationInvalid);
                if (_validatedOnce)
                {
                    sanitizerAccessor.Clear();
                    status.PublishTerminal(AccessTelemetryReason.ConfigurationInvalid);
                    return;
                }

                await Task.Delay(RefreshInterval, timeProvider, stoppingToken).ConfigureAwait(false);
            }
            catch (Exception exception) when (exception is not OutOfMemoryException and not StackOverflowException)
            {
                status.Publish(AccessTelemetryHealthState.Degraded, AccessTelemetryReason.RemoteValidationPending);
                await Task.Delay(TimeSpan.FromSeconds(5), timeProvider, stoppingToken).ConfigureAwait(false);
            }
        }
    }

    private async Task<AccessTelemetryRuntimeValidationResponse> ValidateRemoteAsync(
        AccessTelemetryOptions options,
        CancellationToken cancellationToken)
    {
        var request = new AccessTelemetryRuntimeValidationRequest(
            options.ConfigurationEpoch,
            options.ComponentProfileHash);
#pragma warning disable CS0618 // DaprClient 1.18 typed service invocation is obsolete without a native typed helper.
        return await daprClient.InvokeMethodAsync<AccessTelemetryRuntimeValidationRequest, AccessTelemetryRuntimeValidationResponse>(
            HttpMethod.Post,
            options.LifecycleAppId,
            "v1/access-telemetry/validate",
            request,
            cancellationToken).ConfigureAwait(false);
#pragma warning restore CS0618
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
