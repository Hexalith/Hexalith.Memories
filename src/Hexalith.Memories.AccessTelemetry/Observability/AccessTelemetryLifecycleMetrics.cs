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
    private static readonly Counter<long> Records = Meter.CreateCounter<long>(AccessTelemetryMetricContract.Records);
    private static readonly Histogram<double> DaprLatency = Meter.CreateHistogram<double>(AccessTelemetryMetricContract.DaprDuration, "ms");
    private static readonly Histogram<double> AttestationLatency = Meter.CreateHistogram<double>(AccessTelemetryMetricContract.AttestationDuration, "ms");
    private static readonly Histogram<double> StateLatency = Meter.CreateHistogram<double>(AccessTelemetryMetricContract.StateDuration, "ms");
    private static readonly Counter<long> StateOperations = Meter.CreateCounter<long>(AccessTelemetryMetricContract.StateOperations);
    private static readonly Histogram<double> ExpiryLag = Meter.CreateHistogram<double>(AccessTelemetryMetricContract.ExpiryLag, "s");
    private static readonly Histogram<double> PurgeLatency = Meter.CreateHistogram<double>(AccessTelemetryMetricContract.PurgeDuration, "ms");
    private static readonly Counter<long> Reminders = Meter.CreateCounter<long>(AccessTelemetryMetricContract.Reminders);
    private static readonly Counter<long> PhysicalEvidence = Meter.CreateCounter<long>(AccessTelemetryMetricContract.PhysicalEvidence);
    private static readonly ObservableGauge<long> Capacity = Meter.CreateObservableGauge(
        AccessTelemetryMetricContract.CapacityRecords,
        () => ObserveNonNegative(Volatile.Read(ref _currentCapacityRecords)),
        "{records}");
    private static readonly ObservableGauge<double> CapacityUtilization = Meter.CreateObservableGauge(
        AccessTelemetryMetricContract.CapacityUtilization,
        () => ObserveNonNegative(Volatile.Read(ref _currentCapacityUtilization)),
        "1");
    private static readonly ObservableGauge<long> ExpiryIndexDepth = Meter.CreateObservableGauge(
        AccessTelemetryMetricContract.ExpiryIndexDepth,
        () => ObserveNonNegative(Volatile.Read(ref _currentExpiryIndexDepth)),
        "{records}");
    private static readonly ObservableGauge<double> ExpiryOldestDueAge = Meter.CreateObservableGauge(
        AccessTelemetryMetricContract.ExpiryOldestDueAge,
        ObserveExpiryOldestDueAge,
        "s");
    private static readonly ObservableGauge<double> PurgeCohortAge = Meter.CreateObservableGauge(
        AccessTelemetryMetricContract.PurgeCohortAge,
        ObservePurgeCohortAge,
        "s");
    private static readonly ObservableGauge<double> AttestationAge = Meter.CreateObservableGauge(
        AccessTelemetryMetricContract.AttestationAge,
        ObserveAttestationAge,
        "s");
    private static readonly ObservableGauge<double> AttestationDelta = Meter.CreateObservableGauge(
        AccessTelemetryMetricContract.AttestationDelta,
        () => ObserveNonNegative(Volatile.Read(ref _currentAttestationDeltaMilliseconds)),
        "ms");
    private static readonly ObservableGauge<double> AttestationUncertainty = Meter.CreateObservableGauge(
        AccessTelemetryMetricContract.AttestationUncertainty,
        () => ObserveNonNegative(Volatile.Read(ref _currentAttestationUncertaintyMilliseconds)),
        "ms");
    private static readonly ObservableGauge<long> Health = Meter.CreateObservableGauge(
        AccessTelemetryMetricContract.Health,
        ObserveHealth,
        "{state}");
    private static readonly ObservableGauge<long> Profile = Meter.CreateObservableGauge(
        AccessTelemetryMetricContract.Profile,
        ObserveProfile,
        "{state}");
    private static readonly ObservableGauge<long> PhysicalEvidenceLastTimestamp = Meter.CreateObservableGauge(
        AccessTelemetryMetricContract.PhysicalEvidenceLastTimestamp,
        ObservePhysicalEvidenceTimestamp,
        "s");
    private static long _currentCapacityRecords = -1;
    private static double _currentCapacityUtilization = -1;
    private static long _currentExpiryIndexDepth = -1;
    private static long _oldestDueUnixMilliseconds;
    private static long _purgeCohortUnixMilliseconds;
    private static long _attestationIssuedUnixMilliseconds;
    private static double _currentAttestationDeltaMilliseconds = -1;
    private static double _currentAttestationUncertaintyMilliseconds = -1;
    private static int _runtimeHealthState = (int)AccessTelemetryHealthState.Unhealthy;
    private static int _runtimeHealthReason = (int)AccessTelemetryReason.CapabilityUnproven;
    private static int _processorHealthState = (int)AccessTelemetryHealthState.Healthy;
    private static int _processorHealthReason = (int)AccessTelemetryReason.None;
    private static long _processorLastActivityUnixMilliseconds;
    private static int _runtimeAllowsWrites;
    private static long _runtimeValidUntilUnixMilliseconds;
    private static long _lastPhysicalEvidenceUnixSeconds;
    private static TimeProvider _timeProvider = TimeProvider.System;

    /// <summary>Records one bounded lifecycle transition.</summary>
    public static void Record(AccessTelemetryRecordState state, AccessTelemetryReason reason)
        => Record(1, state, reason);

    /// <summary>Records a bounded number of identical lifecycle transitions.</summary>
    public static void Record(long count, AccessTelemetryRecordState state, AccessTelemetryReason reason)
        => Records.Add(
            count,
            new KeyValuePair<string, object?>("state", ToState(state)),
            new KeyValuePair<string, object?>("reason", ToReason(reason)));

    /// <summary>Records Dapr invocation latency without identity labels.</summary>
    public static void RecordDaprLatency(double milliseconds) => DaprLatency.Record(milliseconds);

    /// <summary>Records attestation latency without identity labels.</summary>
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

    /// <summary>Records state latency without identity labels.</summary>
    public static void RecordStateLatency(double milliseconds) => StateLatency.Record(milliseconds);

    /// <summary>Records completed target-side state operations without inferred multipliers.</summary>
    public static void RecordStateOperations(long count) => StateOperations.Add(count);

    /// <summary>Records bounded capacity without identity labels.</summary>
    public static void RecordCapacity(long records, long? admittedCapacity = null)
    {
        Volatile.Write(ref _currentCapacityRecords, Math.Max(0, records));
        if (admittedCapacity is > 0)
        {
            Volatile.Write(ref _currentCapacityUtilization, Math.Clamp((double)records / admittedCapacity.Value, 0, 1));
        }
        else
        {
            Volatile.Write(ref _currentCapacityUtilization, -1);
        }
    }

    /// <summary>Records current expiry-index and purge-cohort observations without identity labels.</summary>
    public static void RecordExpiryState(
        long indexDepth,
        DateTimeOffset? oldestDueUtc,
        DateTimeOffset? purgeCohortUtc)
    {
        Volatile.Write(ref _currentExpiryIndexDepth, Math.Max(0, indexDepth));
        Volatile.Write(ref _oldestDueUnixMilliseconds, oldestDueUtc?.ToUnixTimeMilliseconds() ?? 0);
        Volatile.Write(ref _purgeCohortUnixMilliseconds, purgeCohortUtc?.ToUnixTimeMilliseconds() ?? 0);
    }

    /// <summary>Records expiry lag without identity labels.</summary>
    public static void RecordExpiryLag(double seconds)
    {
        double bounded = Math.Max(0, seconds);
        ExpiryLag.Record(bounded);
        Volatile.Write(
            ref _oldestDueUnixMilliseconds,
            _timeProvider.GetUtcNow().AddSeconds(-bounded).ToUnixTimeMilliseconds());
    }

    /// <summary>Records purge latency without identity labels.</summary>
    public static void RecordPurgeLatency(double milliseconds) => PurgeLatency.Record(milliseconds);

    /// <summary>Records a reminder result with one bounded outcome label.</summary>
    public static void RecordReminder(bool succeeded)
        => Reminders.Add(1, new KeyValuePair<string, object?>("outcome", succeeded ? "succeeded" : "failed"));

    /// <summary>Records only whether physical evidence is pending or present.</summary>
    public static void RecordPhysicalEvidence(bool present, DateTimeOffset? evidenceUtc = null)
    {
        if (present && evidenceUtc is null)
        {
            throw new ArgumentException("Present physical evidence requires its actual observation timestamp.", nameof(evidenceUtc));
        }

        PhysicalEvidence.Add(1, new KeyValuePair<string, object?>("state", present ? "present" : "pending"));
        if (present)
        {
            Volatile.Write(ref _lastPhysicalEvidenceUnixSeconds, evidenceUtc.GetValueOrDefault().ToUnixTimeSeconds());
        }
        else
        {
            Volatile.Write(ref _lastPhysicalEvidenceUnixSeconds, 0);
        }
    }

    /// <summary>Records the runtime-gate contribution to centrally aggregated lifecycle health.</summary>
    public static void RecordRuntimeGate(
        bool allowsWrites,
        DateTimeOffset? validUntilUtc,
        AccessTelemetryHealthState state,
        AccessTelemetryReason reason,
        TimeProvider timeProvider)
    {
        ArgumentNullException.ThrowIfNull(timeProvider);
        Volatile.Write(ref _timeProvider, timeProvider);
        Volatile.Write(ref _runtimeAllowsWrites, allowsWrites ? 1 : 0);
        Volatile.Write(ref _runtimeValidUntilUnixMilliseconds, validUntilUtc?.ToUnixTimeMilliseconds() ?? 0);
        Volatile.Write(ref _runtimeHealthState, (int)state);
        Volatile.Write(ref _runtimeHealthReason, (int)reason);
    }

    /// <summary>Records the processor contribution to centrally aggregated lifecycle health.</summary>
    public static void RecordProcessorHealth(
        AccessTelemetryHealthState state,
        AccessTelemetryReason reason,
        DateTimeOffset? lastActivityUtc = null)
    {
        Volatile.Write(ref _processorHealthState, (int)state);
        Volatile.Write(ref _processorHealthReason, (int)reason);
        Volatile.Write(ref _processorLastActivityUnixMilliseconds, lastActivityUtc?.ToUnixTimeMilliseconds() ?? 0);
    }

    private static Measurement<long> ObserveHealth()
    {
        (AccessTelemetryHealthState state, AccessTelemetryReason reason) = EffectiveHealth();
        return new Measurement<long>(
            1,
            new KeyValuePair<string, object?>("state", ToHealth(state)),
            new KeyValuePair<string, object?>("reason", ToReason(reason)));
    }

    private static Measurement<long> ObserveProfile()
        => new(1, new KeyValuePair<string, object?>("state", RuntimeGateIsCurrent() ? "matched" : "unproven"));

    private static IEnumerable<Measurement<double>> ObserveAttestationAge()
        => ObserveAge(Volatile.Read(ref _attestationIssuedUnixMilliseconds));

    private static IEnumerable<Measurement<double>> ObserveExpiryOldestDueAge()
        => ObserveAge(Volatile.Read(ref _oldestDueUnixMilliseconds));

    private static IEnumerable<Measurement<double>> ObservePurgeCohortAge()
        => ObserveAge(Volatile.Read(ref _purgeCohortUnixMilliseconds));

    private static IEnumerable<Measurement<long>> ObservePhysicalEvidenceTimestamp()
    {
        long timestamp = Volatile.Read(ref _lastPhysicalEvidenceUnixSeconds);
        return timestamp > 0 ? [new Measurement<long>(timestamp)] : [];
    }

    private static IEnumerable<Measurement<long>> ObserveNonNegative(long value)
        => value >= 0 ? [new Measurement<long>(value)] : [];

    private static IEnumerable<Measurement<double>> ObserveNonNegative(double value)
        => value >= 0 ? [new Measurement<double>(value)] : [];

    private static IEnumerable<Measurement<double>> ObserveAge(long unixMilliseconds)
        => unixMilliseconds > 0
            ? [new Measurement<double>(Math.Max(0, (_timeProvider.GetUtcNow().ToUnixTimeMilliseconds() - unixMilliseconds) / 1000d))]
            : [];

    private static bool RuntimeGateIsCurrent()
        => Volatile.Read(ref _runtimeAllowsWrites) == 1 &&
            Volatile.Read(ref _runtimeValidUntilUnixMilliseconds) > _timeProvider.GetUtcNow().ToUnixTimeMilliseconds();

    private static (AccessTelemetryHealthState State, AccessTelemetryReason Reason) EffectiveHealth()
    {
        bool expired = Volatile.Read(ref _runtimeAllowsWrites) == 1 && !RuntimeGateIsCurrent();
        AccessTelemetryHealthState runtimeState = expired
            ? AccessTelemetryHealthState.Unhealthy
            : (AccessTelemetryHealthState)Volatile.Read(ref _runtimeHealthState);
        AccessTelemetryReason runtimeReason = expired
            ? AccessTelemetryReason.CapabilityUnproven
            : (AccessTelemetryReason)Volatile.Read(ref _runtimeHealthReason);
        var processorState = (AccessTelemetryHealthState)Volatile.Read(ref _processorHealthState);
        var processorReason = (AccessTelemetryReason)Volatile.Read(ref _processorHealthReason);
        (AccessTelemetryHealthState State, AccessTelemetryReason Reason) effective =
            HealthRank(runtimeState) >= HealthRank(processorState)
            ? (runtimeState, runtimeReason)
            : (processorState, processorReason);
        if (effective.State != AccessTelemetryHealthState.Healthy)
        {
            return effective;
        }

        long lastActivity = Volatile.Read(ref _processorLastActivityUnixMilliseconds);
        bool noData = lastActivity <= 0 ||
            _timeProvider.GetUtcNow().ToUnixTimeMilliseconds() - lastActivity >= TimeSpan.FromMinutes(15).TotalMilliseconds;
        return noData
            ? (AccessTelemetryHealthState.NoData, AccessTelemetryReason.None)
            : effective;
    }

    private static int HealthRank(AccessTelemetryHealthState state)
        => state switch
        {
            AccessTelemetryHealthState.Unhealthy => 4,
            AccessTelemetryHealthState.Degraded => 3,
            AccessTelemetryHealthState.NoData => 2,
            AccessTelemetryHealthState.Healthy => 1,
            _ => 4,
        };

    private static string ToHealth(AccessTelemetryHealthState state)
        => state switch
        {
            AccessTelemetryHealthState.NoData => "no_data",
            AccessTelemetryHealthState.Healthy => "healthy",
            AccessTelemetryHealthState.Degraded => "degraded",
            AccessTelemetryHealthState.Unhealthy => "unhealthy",
            _ => "unhealthy",
        };

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
