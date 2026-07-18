// <copyright file="AccessTelemetryOptionsValidator.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.AccessTelemetry.Contracts;

using System.Text.RegularExpressions;

/// <summary>Strict validator for the bounded lifecycle configuration contract.</summary>
public static partial class AccessTelemetryOptionsValidator
{
    /// <summary>Validates options for the named host environment.</summary>
    /// <param name="options">Lifecycle options.</param>
    /// <param name="environmentName">Host environment.</param>
    /// <returns>A fail-closed validation result.</returns>
    public static AccessTelemetryOptionsValidationResult Validate(
        AccessTelemetryOptions options,
        string environmentName)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentException.ThrowIfNullOrWhiteSpace(environmentName);

        if (!options.Enabled)
        {
            return new AccessTelemetryOptionsValidationResult
            {
                IsValid = true,
                AllowsLifecycleWrites = false,
                Reason = AccessTelemetryReason.None,
            };
        }

        bool production = string.Equals(environmentName, "Production", StringComparison.OrdinalIgnoreCase);
        List<string> errors = [];
        if (!Enum.IsDefined(options.RetentionSource))
        {
            errors.Add("RetentionSource is invalid.");
        }

        TimeSpan? retention = options.Retention;
        if (!production && retention is null && options.RetentionSource == RetentionConfigurationSource.DevelopmentDefault)
        {
            retention = AccessTelemetryOptions.DefaultRetention;
        }

        if (production && options.RetentionSource != RetentionConfigurationSource.DaprConfiguration)
        {
            errors.Add("Production retention must be supplied by Dapr configuration.");
        }

        if (retention is null || retention < AccessTelemetryOptions.MinimumRetention || retention > AccessTelemetryOptions.MaximumRetention)
        {
            errors.Add("Retention must be finite and between one hour and seven days inclusive.");
        }

        RequireExact(options.LifecycleAppId, AccessTelemetryOptions.RequiredLifecycleAppId, nameof(options.LifecycleAppId), errors);
        RequireExact(options.ClockAppId, AccessTelemetryOptions.RequiredClockAppId, nameof(options.ClockAppId), errors);
        RequireExact(options.StateStoreName, AccessTelemetryOptions.RequiredStateStoreName, nameof(options.StateStoreName), errors);
        RequireExact(options.SecretStoreName, AccessTelemetryOptions.RequiredSecretStoreName, nameof(options.SecretStoreName), errors);
        RequireExact(options.ConfigurationStoreName, AccessTelemetryOptions.RequiredConfigurationStoreName, nameof(options.ConfigurationStoreName), errors);

        if (options.SchemaVersion != 1)
        {
            errors.Add("SchemaVersion must be exactly 1.");
        }

        RequireNonblank(options.DeploymentId, nameof(options.DeploymentId), errors);
        RequireMatch(options.ConfigurationEpoch, UlidRegex(), nameof(options.ConfigurationEpoch), errors);
        RequireMatch(options.ComponentProfileHash, LowerHex64Regex(), nameof(options.ComponentProfileHash), errors);
        RequireNonblank(options.AttestationVerificationKey, nameof(options.AttestationVerificationKey), errors);
        RequireMatch(options.ClockSignerKeyEpoch, KeyIdRegex(), nameof(options.ClockSignerKeyEpoch), errors);
        RequireNonblank(options.MarkerKeyReference, nameof(options.MarkerKeyReference), errors);
        RequireMatch(options.MarkerKeyGeneration, KeyIdRegex(), nameof(options.MarkerKeyGeneration), errors);
        RequireNonblank(options.CapacityEvidenceId, nameof(options.CapacityEvidenceId), errors);
        RequireNonblank(options.PhysicalReclamationEvidenceId, nameof(options.PhysicalReclamationEvidenceId), errors);

        if (production && options.ComponentIsAlpha && !options.AllowAlphaComponent)
        {
            errors.Add("Production alpha components require explicit allowAlphaComponent opt-in.");
        }

        RequireRange(options.QueueRecordLimit, 1, AccessTelemetryOptions.MaximumQueueRecords, nameof(options.QueueRecordLimit), errors);
        RequireRange(options.QueueByteLimit, AccessTelemetryOptions.MaximumRecordBytes, AccessTelemetryOptions.MaximumQueueBytes, nameof(options.QueueByteLimit), errors);
        RequireRange(options.BatchRecordLimit, 1, AccessTelemetryOptions.MaximumBatchRecords, nameof(options.BatchRecordLimit), errors);
        RequireRange(options.BatchByteLimit, AccessTelemetryOptions.MaximumRecordBytes, AccessTelemetryOptions.MaximumBatchBytes, nameof(options.BatchByteLimit), errors);
        RequireDuration(options.RetryInitialDelay, TimeSpan.FromMilliseconds(100), TimeSpan.FromSeconds(5), nameof(options.RetryInitialDelay), errors);
        RequireDuration(options.RetryMaximumDelay, options.RetryInitialDelay, TimeSpan.FromSeconds(5), nameof(options.RetryMaximumDelay), errors);
        RequireDuration(options.ClockRefreshInterval, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(10), nameof(options.ClockRefreshInterval), errors);
        RequireDuration(options.ClockEvidenceLifetime, options.ClockRefreshInterval, TimeSpan.FromSeconds(30), nameof(options.ClockEvidenceLifetime), errors);
        RequireDuration(options.PurgeInterval, TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(5), nameof(options.PurgeInterval), errors);
        RequireRange(options.PurgeRecordLimit, 1, 512, nameof(options.PurgeRecordLimit), errors);

        return new AccessTelemetryOptionsValidationResult
        {
            IsValid = errors.Count == 0,
            AllowsLifecycleWrites = errors.Count == 0,
            Reason = errors.Count == 0 ? AccessTelemetryReason.None : AccessTelemetryReason.ConfigurationInvalid,
            EffectiveRetention = errors.Count == 0 ? retention : null,
            Errors = errors,
        };
    }

    private static void RequireDuration(TimeSpan value, TimeSpan minimum, TimeSpan maximum, string name, List<string> errors)
    {
        if (value < minimum || value > maximum)
        {
            errors.Add($"{name} is outside its bounded range.");
        }
    }

    private static void RequireExact(string value, string expected, string name, List<string> errors)
    {
        if (!string.Equals(value, expected, StringComparison.Ordinal))
        {
            errors.Add($"{name} must be exactly {expected}.");
        }
    }

    private static void RequireMatch(string value, Regex pattern, string name, List<string> errors)
    {
        if (!pattern.IsMatch(value))
        {
            errors.Add($"{name} has an invalid bounded format.");
        }
    }

    private static void RequireNonblank(string value, string name, List<string> errors)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Length > 256)
        {
            errors.Add($"{name} is required and must be at most 256 characters.");
        }
    }

    private static void RequireRange(int value, int minimum, int maximum, string name, List<string> errors)
    {
        if (value < minimum || value > maximum)
        {
            errors.Add($"{name} is outside its bounded range.");
        }
    }

    [GeneratedRegex("^[a-z0-9][a-z0-9-]{0,31}$", RegexOptions.CultureInvariant)]
    private static partial Regex KeyIdRegex();

    [GeneratedRegex("^[0-9a-f]{64}$", RegexOptions.CultureInvariant)]
    private static partial Regex LowerHex64Regex();

    [GeneratedRegex("^[0-9A-HJKMNP-TV-Z]{26}$", RegexOptions.CultureInvariant)]
    private static partial Regex UlidRegex();
}
