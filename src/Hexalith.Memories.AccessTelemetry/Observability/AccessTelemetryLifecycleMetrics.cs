// <copyright file="AccessTelemetryLifecycleMetrics.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.AccessTelemetry.Observability;

using System.Diagnostics.Metrics;

using Hexalith.Memories.AccessTelemetry.Contracts;

/// <summary>Low-cardinality lifecycle instruments with no identity-bearing labels.</summary>
internal static class AccessTelemetryLifecycleMetrics
{
    /// <summary>Meter registered by the lifecycle composition root.</summary>
    public const string MeterName = "Hexalith.Memories.AccessTelemetry";

    private static readonly Meter Meter = new(MeterName, "1.0.0");
    private static readonly Counter<long> Records = Meter.CreateCounter<long>("memories.access.telemetry.lifecycle.records");
    private static readonly Histogram<double> QueueBytes = Meter.CreateHistogram<double>("memories.access.telemetry.lifecycle.queue.bytes", "By");
    private static readonly Histogram<double> DaprLatency = Meter.CreateHistogram<double>("memories.access.telemetry.lifecycle.dapr.duration", "ms");
    private static readonly Histogram<double> AttestationLatency = Meter.CreateHistogram<double>("memories.access.telemetry.lifecycle.attestation.duration", "ms");
    private static readonly Histogram<double> StateLatency = Meter.CreateHistogram<double>("memories.access.telemetry.lifecycle.state.duration", "ms");
    private static readonly Histogram<long> Capacity = Meter.CreateHistogram<long>("memories.access.telemetry.lifecycle.capacity.records");
    private static readonly Histogram<double> ExpiryLag = Meter.CreateHistogram<double>("memories.access.telemetry.lifecycle.expiry.lag", "s");
    private static readonly Histogram<double> PurgeLatency = Meter.CreateHistogram<double>("memories.access.telemetry.lifecycle.purge.duration", "ms");
    private static readonly Counter<long> Reminders = Meter.CreateCounter<long>("memories.access.telemetry.lifecycle.reminders");
    private static readonly Counter<long> PhysicalEvidence = Meter.CreateCounter<long>("memories.access.telemetry.lifecycle.physical_evidence");

    /// <summary>Records one bounded lifecycle transition.</summary>
    public static void Record(AccessTelemetryRecordState state, AccessTelemetryReason reason)
        => Records.Add(
            1,
            new KeyValuePair<string, object?>("state", ToState(state)),
            new KeyValuePair<string, object?>("reason", ToReason(reason)));

    /// <summary>Records queue bytes without labels.</summary>
    public static void RecordQueueBytes(long value) => QueueBytes.Record(value);

    /// <summary>Records Dapr invocation latency without identity labels.</summary>
    public static void RecordDaprLatency(double milliseconds) => DaprLatency.Record(milliseconds);

    /// <summary>Records attestation latency without identity labels.</summary>
    public static void RecordAttestationLatency(double milliseconds) => AttestationLatency.Record(milliseconds);

    /// <summary>Records state latency without identity labels.</summary>
    public static void RecordStateLatency(double milliseconds) => StateLatency.Record(milliseconds);

    /// <summary>Records bounded capacity without identity labels.</summary>
    public static void RecordCapacity(long records) => Capacity.Record(records);

    /// <summary>Records expiry lag without identity labels.</summary>
    public static void RecordExpiryLag(double seconds) => ExpiryLag.Record(seconds);

    /// <summary>Records purge latency without identity labels.</summary>
    public static void RecordPurgeLatency(double milliseconds) => PurgeLatency.Record(milliseconds);

    /// <summary>Records a reminder result with one bounded outcome label.</summary>
    public static void RecordReminder(bool succeeded)
        => Reminders.Add(1, new KeyValuePair<string, object?>("outcome", succeeded ? "succeeded" : "failed"));

    /// <summary>Records only whether physical evidence is pending or present.</summary>
    public static void RecordPhysicalEvidence(bool present)
        => PhysicalEvidence.Add(1, new KeyValuePair<string, object?>("state", present ? "present" : "pending"));

    private static string ToReason(AccessTelemetryReason reason)
        => reason switch
        {
            AccessTelemetryReason.None => "none",
            AccessTelemetryReason.ConfigurationInvalid => "configuration_invalid",
            AccessTelemetryReason.RemoteValidationPending => "remote_validation_pending",
            AccessTelemetryReason.QueueFull => "queue_full",
            AccessTelemetryReason.SchemaMismatch => "schema_mismatch",
            AccessTelemetryReason.Expired => "expired",
            AccessTelemetryReason.RecordIdConflict => "record_id_conflict",
            AccessTelemetryReason.ClockUntrusted => "clock_untrusted",
            AccessTelemetryReason.DependencyUnavailable => "dependency_unavailable",
            AccessTelemetryReason.CapabilityUnproven => "capability_unproven",
            _ => "configuration_invalid",
        };

    private static string ToState(AccessTelemetryRecordState state)
        => state switch
        {
            AccessTelemetryRecordState.Accepted => "accepted",
            AccessTelemetryRecordState.Rejected => "rejected",
            AccessTelemetryRecordState.Enqueued => "enqueued",
            AccessTelemetryRecordState.Persisted => "persisted",
            AccessTelemetryRecordState.Retried => "retried",
            AccessTelemetryRecordState.Failed => "failed",
            AccessTelemetryRecordState.Dropped => "dropped",
            AccessTelemetryRecordState.Expired => "expired",
            AccessTelemetryRecordState.Purged => "purged",
            _ => "failed",
        };
}
