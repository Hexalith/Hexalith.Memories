// <copyright file="AccessTelemetryOptions.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.AccessTelemetry.Contracts;

/// <summary>Strict bounded lifecycle configuration shared by writers and services.</summary>
public sealed record AccessTelemetryOptions
{
    /// <summary>Configuration section name.</summary>
    public const string SectionName = "AccessTelemetryLifecycle";

    /// <summary>Fixed lifecycle app identity.</summary>
    public const string RequiredLifecycleAppId = "memories-access-telemetry";

    /// <summary>Fixed clock app identity.</summary>
    public const string RequiredClockAppId = "memories-access-telemetry-clock";

    /// <summary>Fixed state component identity.</summary>
    public const string RequiredStateStoreName = "access-telemetry-store";

    /// <summary>Fixed secret component identity.</summary>
    public const string RequiredSecretStoreName = "access-telemetry-secrets";

    /// <summary>Fixed configuration component identity.</summary>
    public const string RequiredConfigurationStoreName = "access-telemetry-config";

    /// <summary>Default Development retention.</summary>
    public static readonly TimeSpan DefaultRetention = TimeSpan.FromHours(24);

    /// <summary>Minimum deployable retention.</summary>
    public static readonly TimeSpan MinimumRetention = TimeSpan.FromHours(1);

    /// <summary>Maximum deployable retention.</summary>
    public static readonly TimeSpan MaximumRetention = TimeSpan.FromDays(7);

    /// <summary>Maximum canonical record size.</summary>
    public const int MaximumRecordBytes = 1024;

    /// <summary>Maximum records in one invocation.</summary>
    public const int MaximumBatchRecords = 256;

    /// <summary>Maximum bytes in one invocation.</summary>
    public const int MaximumBatchBytes = 1024 * 1024;

    /// <summary>Maximum queued records per Server process.</summary>
    public const int MaximumQueueRecords = 8192;

    /// <summary>Maximum queued bytes per Server process.</summary>
    public const int MaximumQueueBytes = 64 * 1024 * 1024;

    /// <summary>Maximum shutdown flush duration.</summary>
    public static readonly TimeSpan ShutdownFlushTimeout = TimeSpan.FromSeconds(5);

    /// <summary>Maximum retry age measured from source emission.</summary>
    public static readonly TimeSpan MaximumRetryAge = TimeSpan.FromMinutes(5);

    /// <summary>Gets whether the lifecycle copy is enabled.</summary>
    public bool Enabled { get; init; }

    /// <summary>Gets the fixed lifecycle app ID.</summary>
    public string LifecycleAppId { get; init; } = RequiredLifecycleAppId;

    /// <summary>Gets the fixed clock app ID.</summary>
    public string ClockAppId { get; init; } = RequiredClockAppId;

    /// <summary>Gets the fixed state component name.</summary>
    public string StateStoreName { get; init; } = RequiredStateStoreName;

    /// <summary>Gets the fixed secret component name.</summary>
    public string SecretStoreName { get; init; } = RequiredSecretStoreName;

    /// <summary>Gets the fixed configuration component name.</summary>
    public string ConfigurationStoreName { get; init; } = RequiredConfigurationStoreName;

    /// <summary>Gets the exact persisted schema version.</summary>
    public int SchemaVersion { get; init; } = 1;

    /// <summary>Gets the configured retention or null when absent.</summary>
    public TimeSpan? Retention { get; init; }

    /// <summary>Gets the retention configuration authority.</summary>
    public RetentionConfigurationSource RetentionSource { get; init; }

    /// <summary>Gets the deployment identity bound into clock evidence.</summary>
    public string DeploymentId { get; init; } = string.Empty;

    /// <summary>Gets the configuration epoch ULID.</summary>
    public string ConfigurationEpoch { get; init; } = string.Empty;

    /// <summary>Gets the lowercase SHA-256 of the exact component profile.</summary>
    public string ComponentProfileHash { get; init; } = string.Empty;

    /// <summary>Gets whether the selected component is alpha.</summary>
    public bool ComponentIsAlpha { get; init; }

    /// <summary>Gets explicit Production alpha opt-in.</summary>
    public bool AllowAlphaComponent { get; init; }

    /// <summary>Gets the attestation verification public key.</summary>
    public string AttestationVerificationKey { get; init; } = string.Empty;

    /// <summary>Gets the expected bounded clock signer-key epoch.</summary>
    public string ClockSignerKeyEpoch { get; init; } = "development-clock-key";

    /// <summary>Gets the Dapr marker-secret reference.</summary>
    public string MarkerKeyReference { get; init; } = string.Empty;

    /// <summary>Gets the active bounded marker-key generation.</summary>
    public string MarkerKeyGeneration { get; init; } = string.Empty;

    /// <summary>Gets the capacity evidence identity.</summary>
    public string CapacityEvidenceId { get; init; } = string.Empty;

    /// <summary>Gets the physical reclamation evidence identity or pending marker.</summary>
    public string PhysicalReclamationEvidenceId { get; init; } = string.Empty;

    /// <summary>Gets the configured queue record limit.</summary>
    public int QueueRecordLimit { get; init; } = MaximumQueueRecords;

    /// <summary>Gets the configured queue byte limit.</summary>
    public int QueueByteLimit { get; init; } = MaximumQueueBytes;

    /// <summary>Gets the configured batch record limit.</summary>
    public int BatchRecordLimit { get; init; } = MaximumBatchRecords;

    /// <summary>Gets the configured batch byte limit.</summary>
    public int BatchByteLimit { get; init; } = MaximumBatchBytes;

    /// <summary>Gets the initial retry delay.</summary>
    public TimeSpan RetryInitialDelay { get; init; } = TimeSpan.FromMilliseconds(100);

    /// <summary>Gets the maximum retry delay.</summary>
    public TimeSpan RetryMaximumDelay { get; init; } = TimeSpan.FromSeconds(5);

    /// <summary>Gets the clock refresh interval.</summary>
    public TimeSpan ClockRefreshInterval { get; init; } = TimeSpan.FromSeconds(10);

    /// <summary>Gets the clock evidence lifetime.</summary>
    public TimeSpan ClockEvidenceLifetime { get; init; } = TimeSpan.FromSeconds(30);

    /// <summary>Gets the purge reminder interval.</summary>
    public TimeSpan PurgeInterval { get; init; } = TimeSpan.FromMinutes(5);

    /// <summary>Gets the maximum purge records per actor turn.</summary>
    public int PurgeRecordLimit { get; init; } = 512;
}
