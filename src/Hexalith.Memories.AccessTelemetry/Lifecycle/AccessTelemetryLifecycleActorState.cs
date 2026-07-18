// <copyright file="AccessTelemetryLifecycleActorState.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.AccessTelemetry.Lifecycle;

using Hexalith.Memories.AccessTelemetry.Contracts;

/// <summary>Durable control-plane state for the global lifecycle actor.</summary>
internal sealed record AccessTelemetryLifecycleActorState
{
    /// <summary>Gets the active configuration epoch.</summary>
    public LifecycleConfigurationEpoch? Configuration { get; init; }

    /// <summary>Gets dynamic writer membership keyed by service-instance/process epoch.</summary>
    public IReadOnlyDictionary<string, WriterHeartbeat> Writers { get; init; } = new Dictionary<string, WriterHeartbeat>(StringComparer.Ordinal);

    /// <summary>Gets the current minute expiry cursor.</summary>
    public long ExpiryMinuteCursor { get; init; }

    /// <summary>Gets the next shard cursor in the fixed range 0..63.</summary>
    public int ExpiryShardCursor { get; init; }

    /// <summary>Gets the last successful purge time.</summary>
    public long? LastPurgeUnixMilliseconds { get; init; }

    /// <summary>Gets the bounded retained capacity estimate.</summary>
    public long RetainedRecordCount { get; init; }

    /// <summary>Gets the physical-reclamation evidence ID without claiming proof.</summary>
    public string PhysicalReclamationEvidenceId { get; init; } = "pending-story-27-3";

    /// <summary>Gets the staged marker-key generation.</summary>
    public string? StagedMarkerKeyGeneration { get; init; }

    /// <summary>Gets the active marker-key generation.</summary>
    public string? ActiveMarkerKeyGeneration { get; init; }

    /// <summary>Gets reminder progress used for idempotent reactivation.</summary>
    public long ReminderSequence { get; init; }

    /// <summary>Gets the durable staged marker-key rotation protocol.</summary>
    public MarkerKeyRotationState? MarkerKeyRotation { get; init; }

    /// <summary>Gets the durable fail-closed lifecycle health.</summary>
    public AccessTelemetryHealthState Health { get; init; } = AccessTelemetryHealthState.Healthy;

    /// <summary>Gets the durable bounded lifecycle health reason.</summary>
    public AccessTelemetryReason HealthReason { get; init; } = AccessTelemetryReason.None;
}
