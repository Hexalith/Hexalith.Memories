// <copyright file="ServerAccessTelemetryLifecycleMetrics.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Telemetry.AccessTelemetryLifecycle;

using System.Diagnostics.Metrics;

using Hexalith.Memories.AccessTelemetry.Contracts;

/// <summary>Server-side bounded admission/delivery instruments without identity labels.</summary>
internal static class ServerAccessTelemetryLifecycleMetrics
{
    /// <summary>Meter registered by the Server composition root.</summary>
    public const string MeterName = "Hexalith.Memories.AccessTelemetry.Server";

    private static readonly Meter Meter = new(MeterName, "1.0.0");
    private static readonly Counter<long> Records = Meter.CreateCounter<long>("memories.access.telemetry.lifecycle.records");
    private static readonly Histogram<long> QueueBytes = Meter.CreateHistogram<long>("memories.access.telemetry.lifecycle.queue.bytes", "By");
    private static readonly Histogram<double> DaprLatency = Meter.CreateHistogram<double>("memories.access.telemetry.lifecycle.dapr.duration", "ms");
    private static readonly Histogram<double> AttestationLatency = Meter.CreateHistogram<double>("memories.access.telemetry.lifecycle.attestation.duration", "ms");

    /// <summary>Records one bounded transition.</summary>
    public static void Record(AccessTelemetryRecordState state, AccessTelemetryReason reason)
        => Record(1, state, reason);

    /// <summary>Records a bounded number of identical transitions.</summary>
    public static void Record(long count, AccessTelemetryRecordState state, AccessTelemetryReason reason)
        => Records.Add(
            count,
            new KeyValuePair<string, object?>("state", state.ToString().ToLowerInvariant()),
            new KeyValuePair<string, object?>("reason", ToReason(reason)));

    /// <summary>Records current queue bytes without labels.</summary>
    public static void RecordQueueBytes(long bytes) => QueueBytes.Record(bytes);

    /// <summary>Records Dapr invocation latency without labels.</summary>
    public static void RecordDaprLatency(double milliseconds) => DaprLatency.Record(milliseconds);

    /// <summary>Records clock-attestation latency without labels.</summary>
    public static void RecordAttestationLatency(double milliseconds) => AttestationLatency.Record(milliseconds);

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
}
