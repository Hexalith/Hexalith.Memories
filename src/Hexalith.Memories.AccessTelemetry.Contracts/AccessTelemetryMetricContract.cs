// <copyright file="AccessTelemetryMetricContract.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.AccessTelemetry.Contracts;

/// <summary>Defines the bounded lifecycle metric names and their permitted labels.</summary>
public static class AccessTelemetryMetricContract
{
    /// <summary>Lifecycle state transition counter.</summary>
    public const string Records = "memories.access.telemetry.lifecycle.records";

    /// <summary>Current queue record count.</summary>
    public const string QueueRecords = "memories.access.telemetry.lifecycle.queue.records";

    /// <summary>Current queue byte count.</summary>
    public const string QueueBytes = "memories.access.telemetry.lifecycle.queue.bytes";

    /// <summary>Oldest queued record age in seconds.</summary>
    public const string QueueOldestAge = "memories.access.telemetry.lifecycle.queue.oldest.age";

    /// <summary>Dapr invocation latency in milliseconds.</summary>
    public const string DaprDuration = "memories.access.telemetry.lifecycle.dapr.duration";

    /// <summary>Clock-attestation request latency in milliseconds.</summary>
    public const string AttestationDuration = "memories.access.telemetry.lifecycle.attestation.duration";

    /// <summary>Current clock-attestation age in seconds.</summary>
    public const string AttestationAge = "memories.access.telemetry.lifecycle.attestation.age";

    /// <summary>Current absolute clock/reference delta in milliseconds.</summary>
    public const string AttestationDelta = "memories.access.telemetry.lifecycle.attestation.delta";

    /// <summary>Current attestation uncertainty in milliseconds.</summary>
    public const string AttestationUncertainty = "memories.access.telemetry.lifecycle.attestation.uncertainty";

    /// <summary>Dapr state operation latency in milliseconds.</summary>
    public const string StateDuration = "memories.access.telemetry.lifecycle.state.duration";

    /// <summary>Completed lifecycle state operations, used for target-side throughput qualification.</summary>
    public const string StateOperations = "memories.access.telemetry.lifecycle.state.operations";

    /// <summary>Current retained-record count, distinct from queue depth.</summary>
    public const string CapacityRecords = "memories.access.telemetry.lifecycle.capacity.records";

    /// <summary>Current capacity utilization as a ratio from zero through one.</summary>
    public const string CapacityUtilization = "memories.access.telemetry.lifecycle.capacity.utilization";

    /// <summary>Current expiry-index entry count.</summary>
    public const string ExpiryIndexDepth = "memories.access.telemetry.lifecycle.expiry.index.depth";

    /// <summary>Oldest due record age in seconds.</summary>
    public const string ExpiryOldestDueAge = "memories.access.telemetry.lifecycle.expiry.oldest.due.age";

    /// <summary>Observed expiry lag in seconds.</summary>
    public const string ExpiryLag = "memories.access.telemetry.lifecycle.expiry.lag";

    /// <summary>Purge operation latency in milliseconds.</summary>
    public const string PurgeDuration = "memories.access.telemetry.lifecycle.purge.duration";

    /// <summary>Current purge-cohort age in seconds.</summary>
    public const string PurgeCohortAge = "memories.access.telemetry.lifecycle.purge.cohort.age";

    /// <summary>Reminder outcome counter.</summary>
    public const string Reminders = "memories.access.telemetry.lifecycle.reminders";

    /// <summary>Physical-reclamation evidence state counter.</summary>
    public const string PhysicalEvidence = "memories.access.telemetry.lifecycle.physical.evidence";

    /// <summary>UTC Unix seconds of the latest physical-reclamation evidence.</summary>
    public const string PhysicalEvidenceLastTimestamp = "memories.access.telemetry.lifecycle.physical.evidence.last.timestamp";

    /// <summary>Current lifecycle health state.</summary>
    public const string Health = "memories.access.telemetry.lifecycle.health";

    /// <summary>Current immutable-profile comparison state.</summary>
    public const string Profile = "memories.access.telemetry.lifecycle.profile";

    /// <summary>Gets the exact bounded label policy consumed by dashboard guards.</summary>
    public static IReadOnlyDictionary<string, IReadOnlyList<string>> MetricTagKeyPolicy { get; } =
        new Dictionary<string, IReadOnlyList<string>>(StringComparer.Ordinal)
        {
            [Records] = new[] { "state", "reason" },
            [QueueRecords] = Array.Empty<string>(),
            [QueueBytes] = Array.Empty<string>(),
            [QueueOldestAge] = Array.Empty<string>(),
            [DaprDuration] = Array.Empty<string>(),
            [AttestationDuration] = Array.Empty<string>(),
            [AttestationAge] = Array.Empty<string>(),
            [AttestationDelta] = Array.Empty<string>(),
            [AttestationUncertainty] = Array.Empty<string>(),
            [StateDuration] = Array.Empty<string>(),
            [StateOperations] = Array.Empty<string>(),
            [CapacityRecords] = Array.Empty<string>(),
            [CapacityUtilization] = Array.Empty<string>(),
            [ExpiryIndexDepth] = Array.Empty<string>(),
            [ExpiryOldestDueAge] = Array.Empty<string>(),
            [ExpiryLag] = Array.Empty<string>(),
            [PurgeDuration] = Array.Empty<string>(),
            [PurgeCohortAge] = Array.Empty<string>(),
            [Reminders] = new[] { "outcome" },
            [PhysicalEvidence] = new[] { "state" },
            [PhysicalEvidenceLastTimestamp] = Array.Empty<string>(),
            [Health] = new[] { "state", "reason" },
            [Profile] = new[] { "state" },
        };

    /// <summary>Gets the metrics exported with the Prometheus counter suffix.</summary>
    public static IReadOnlySet<string> CounterMetricNames { get; } = new HashSet<string>(StringComparer.Ordinal)
    {
        Records,
        StateOperations,
        Reminders,
        PhysicalEvidence,
    };

    /// <summary>Gets the Prometheus unit suffix for each runtime histogram.</summary>
    public static IReadOnlyDictionary<string, string> HistogramUnitSuffixes { get; } =
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            [DaprDuration] = "milliseconds",
            [AttestationDuration] = "milliseconds",
            [StateDuration] = "milliseconds",
            [ExpiryLag] = "seconds",
            [PurgeDuration] = "milliseconds",
        };

    /// <summary>Gets the Prometheus unit suffix for non-histogram runtime instruments.</summary>
    public static IReadOnlyDictionary<string, string> MetricUnitSuffixes { get; } =
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            [QueueOldestAge] = "seconds",
            [AttestationAge] = "seconds",
            [AttestationDelta] = "milliseconds",
            [AttestationUncertainty] = "milliseconds",
            [ExpiryOldestDueAge] = "seconds",
            [PurgeCohortAge] = "seconds",
            [PhysicalEvidenceLastTimestamp] = "seconds",
        };
}
