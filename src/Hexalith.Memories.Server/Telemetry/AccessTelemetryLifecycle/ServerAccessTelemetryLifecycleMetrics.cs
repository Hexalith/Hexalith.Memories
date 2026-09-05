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
    private static readonly Counter<long> Records = Meter.CreateCounter<long>(AccessTelemetryMetricContract.Records);
    private static readonly ObservableGauge<long> QueueBytes = Meter.CreateObservableGauge(
        AccessTelemetryMetricContract.QueueBytes,
        () => Volatile.Read(ref _currentQueueBytes),
        "By");
    private static readonly ObservableGauge<long> QueueRecords = Meter.CreateObservableGauge(
        AccessTelemetryMetricContract.QueueRecords,
        () => Volatile.Read(ref _currentQueueRecords),
        "{records}");
    private static readonly ObservableGauge<double> QueueOldestAge = Meter.CreateObservableGauge(
        AccessTelemetryMetricContract.QueueOldestAge,
        ObserveQueueOldestAge,
        "s");
    private static readonly Histogram<double> DaprLatency = Meter.CreateHistogram<double>(AccessTelemetryMetricContract.DaprDuration, "ms");
    private static readonly Histogram<double> AttestationLatency = Meter.CreateHistogram<double>(AccessTelemetryMetricContract.AttestationDuration, "ms");
    private static readonly ObservableGauge<double> AttestationAge = Meter.CreateObservableGauge(
        AccessTelemetryMetricContract.AttestationAge,
        ObserveAttestationAge,
        "s");
    private static readonly ObservableGauge<double> AttestationDelta = Meter.CreateObservableGauge(
        AccessTelemetryMetricContract.AttestationDelta,
        () => Volatile.Read(ref _currentAttestationDeltaMilliseconds),
        "ms");
    private static readonly ObservableGauge<double> AttestationUncertainty = Meter.CreateObservableGauge(
        AccessTelemetryMetricContract.AttestationUncertainty,
        () => Volatile.Read(ref _currentAttestationUncertaintyMilliseconds),
        "ms");
    private static long _currentQueueBytes;
    private static long _currentQueueRecords;
    private static long _oldestQueuedUnixMilliseconds;
    private static long _attestationIssuedUnixMilliseconds;
    private static double _currentAttestationDeltaMilliseconds;
    private static double _currentAttestationUncertaintyMilliseconds;
    private static TimeProvider _timeProvider = TimeProvider.System;

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
    public static void RecordQueue(
        long records,
        long bytes,
        DateTimeOffset? oldestEmittedUtc,
        TimeProvider timeProvider)
    {
        ArgumentNullException.ThrowIfNull(timeProvider);
        Volatile.Write(ref _timeProvider, timeProvider);
        Volatile.Write(ref _currentQueueRecords, Math.Max(0, records));
        Volatile.Write(ref _currentQueueBytes, Math.Max(0, bytes));
        Volatile.Write(ref _oldestQueuedUnixMilliseconds, oldestEmittedUtc?.ToUnixTimeMilliseconds() ?? 0);
    }

    /// <summary>Records Dapr invocation latency without labels.</summary>
    public static void RecordDaprLatency(double milliseconds) => DaprLatency.Record(milliseconds);

    /// <summary>Records clock-attestation latency without labels.</summary>
    public static void RecordAttestationLatency(double milliseconds) => AttestationLatency.Record(milliseconds);

    /// <summary>Records sanitized current evidence-age, delta, and uncertainty values.</summary>
    public static void RecordAttestation(SignedClockAttestation attestation, DateTimeOffset observedAt)
    {
        ArgumentNullException.ThrowIfNull(attestation);
        long observedMilliseconds = observedAt.ToUnixTimeMilliseconds();
        double midpoint = (attestation.NotBeforeUnixMilliseconds / 2d) + (attestation.NotAfterUnixMilliseconds / 2d);
        Volatile.Write(ref _attestationIssuedUnixMilliseconds, attestation.IssuedAtUnixMilliseconds);
        Volatile.Write(ref _currentAttestationDeltaMilliseconds, Math.Abs(observedMilliseconds - midpoint));
        Volatile.Write(
            ref _currentAttestationUncertaintyMilliseconds,
            Math.Max(0, (attestation.NotAfterUnixMilliseconds / 2d) - (attestation.NotBeforeUnixMilliseconds / 2d)));
    }

    /// <summary>Records an attestation with its testable collection clock.</summary>
    public static void RecordAttestation(
        SignedClockAttestation attestation,
        DateTimeOffset observedAt,
        TimeProvider timeProvider)
    {
        ArgumentNullException.ThrowIfNull(timeProvider);
        Volatile.Write(ref _timeProvider, timeProvider);
        RecordAttestation(attestation, observedAt);
    }

    private static IEnumerable<Measurement<double>> ObserveQueueOldestAge()
        => ObserveAge(Volatile.Read(ref _oldestQueuedUnixMilliseconds), emitZeroWhenAbsent: true);

    private static IEnumerable<Measurement<double>> ObserveAttestationAge()
        => ObserveAge(Volatile.Read(ref _attestationIssuedUnixMilliseconds), emitZeroWhenAbsent: false);

    private static IEnumerable<Measurement<double>> ObserveAge(long unixMilliseconds, bool emitZeroWhenAbsent)
    {
        if (unixMilliseconds <= 0)
        {
            return emitZeroWhenAbsent ? [new Measurement<double>(0)] : [];
        }

        return
        [
            new Measurement<double>(
                Math.Max(0, (_timeProvider.GetUtcNow().ToUnixTimeMilliseconds() - unixMilliseconds) / 1000d)),
        ];
    }

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
