// <copyright file="AccessTelemetryLifecycleProcessor.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.AccessTelemetry.Lifecycle;

using System.Diagnostics;
using System.Globalization;

using Hexalith.Memories.AccessTelemetry.Contracts;
using Hexalith.Memories.AccessTelemetry.Observability;

/// <summary>Portable serialized lifecycle logic used only by the fixed actor.</summary>
internal sealed class AccessTelemetryLifecycleProcessor
{
    private static readonly TimeSpan AcceptedFutureSkew = TimeSpan.FromSeconds(1);
    private static readonly TimeSpan TurnBudget = TimeSpan.FromMilliseconds(100);
    private readonly IAccessTelemetryStateStore _store;
    private readonly TimeProvider _timeProvider;
    private readonly AccessTelemetryRuntimeOptionsProvider? _optionsProvider;
    private readonly AccessTelemetryOptions? _testOptions;
    private readonly AccessTelemetryProcessorStatus _status;

    /// <summary>Initializes the lifecycle processor.</summary>
    public AccessTelemetryLifecycleProcessor(
        IAccessTelemetryStateStore store,
        TimeProvider timeProvider,
        AccessTelemetryRuntimeOptionsProvider optionsProvider,
        AccessTelemetryProcessorStatus status)
    {
        _store = store;
        _timeProvider = timeProvider;
        _optionsProvider = optionsProvider;
        _status = status;
    }

    /// <summary>Initializes deterministic processor logic for focused tests.</summary>
    internal AccessTelemetryLifecycleProcessor(
        IAccessTelemetryStateStore store,
        TimeProvider timeProvider,
        AccessTelemetryOptions? options = null)
    {
        _store = store;
        _timeProvider = timeProvider;
        _testOptions = options ?? new AccessTelemetryOptions { Retention = AccessTelemetryOptions.DefaultRetention };
        _status = new AccessTelemetryProcessorStatus();
    }

    /// <summary>Gets the current fail-closed lifecycle health.</summary>
    public AccessTelemetryHealthState Health { get; private set; } = AccessTelemetryHealthState.Healthy;

    /// <summary>Gets the bounded reason for the current processor health.</summary>
    public AccessTelemetryReason HealthReason { get; private set; } = AccessTelemetryReason.None;

    /// <summary>Validates and atomically persists one record/index pair.</summary>
    public async Task<AccessTelemetryPersistenceResult> PersistAsync(
        AccessTelemetryRecord record,
        CancellationToken cancellationToken)
        => await PersistAsync(record, _timeProvider.GetUtcNow(), cancellationToken).ConfigureAwait(false);

    /// <summary>Validates and persists one record using actor-verified trusted acceptance time.</summary>
    public async Task<AccessTelemetryPersistenceResult> PersistAsync(
        AccessTelemetryRecord record,
        DateTimeOffset acceptedAt,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(record);
        DateTimeOffset emittedAt;
        try
        {
            _ = AccessTelemetryCanonicalizer.CanonicalizeRecord(record);
            emittedAt = ParseUtc(record.EmittedAtUtc);
        }
        catch (Exception exception) when (exception is AccessTelemetryContractException or FormatException)
        {
            return new AccessTelemetryPersistenceResult(
                AccessTelemetryPersistenceStatus.Rejected,
                AccessTelemetryReason.SchemaMismatch);
        }

        AccessTelemetryOptions options = _optionsProvider?.Current ?? _testOptions!;
        TimeSpan retention = options.Retention ?? AccessTelemetryOptions.DefaultRetention;
        DateTimeOffset expiresAt;
        try
        {
            expiresAt = emittedAt.Add(retention);
        }
        catch (ArgumentOutOfRangeException)
        {
            return new AccessTelemetryPersistenceResult(
                AccessTelemetryPersistenceStatus.Rejected,
                AccessTelemetryReason.SchemaMismatch);
        }
        AccessTelemetryRecord normalized = record with
        {
            AcceptedAtUtc = FormatUtc(acceptedAt),
            ExpiresAtUtc = FormatUtc(expiresAt),
            EnvelopeHash = string.Empty,
        };
        normalized = normalized with { EnvelopeHash = AccessTelemetryCanonicalizer.CalculateEnvelopeHash(normalized) };
        if (emittedAt - acceptedAt > AcceptedFutureSkew)
        {
            Health = AccessTelemetryHealthState.Unhealthy;
            HealthReason = AccessTelemetryReason.ClockUntrusted;
            _status.Publish(Health, HealthReason, acceptedAt);
            AccessTelemetryLifecycleMetrics.Record(AccessTelemetryRecordState.Rejected, AccessTelemetryReason.ClockUntrusted);
            return new AccessTelemetryPersistenceResult(
                AccessTelemetryPersistenceStatus.Rejected,
                AccessTelemetryReason.ClockUntrusted);
        }

        if (expiresAt <= acceptedAt)
        {
            return new AccessTelemetryPersistenceResult(
                AccessTelemetryPersistenceStatus.Rejected,
                AccessTelemetryReason.Expired);
        }

        int ttlInSeconds;
        try
        {
            ttlInSeconds = checked((int)Math.Ceiling((expiresAt - acceptedAt).TotalSeconds));
        }
        catch (OverflowException)
        {
            return new AccessTelemetryPersistenceResult(
                AccessTelemetryPersistenceStatus.Rejected,
                AccessTelemetryReason.SchemaMismatch);
        }
        var expiryEntry = new AccessTelemetryExpiryEntry(
            normalized.RecordId,
            AccessTelemetryExpiryIndex.GetExpiryMinute(expiresAt),
            AccessTelemetryExpiryIndex.GetShard(normalized.RecordId),
            normalized.EnvelopeHash,
            normalized.ExpiresAtUtc);
        long stateStarted = Stopwatch.GetTimestamp();
        AccessTelemetryStoreWriteStatus status;
        try
        {
            status = await _store.WriteRecordAndIndexAsync(
                normalized,
                expiryEntry,
                ttlInSeconds,
                cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            AccessTelemetryLifecycleMetrics.RecordStateLatency(
                Stopwatch.GetElapsedTime(stateStarted).TotalMilliseconds);
        }
        if (status == AccessTelemetryStoreWriteStatus.Conflict)
        {
            Health = AccessTelemetryHealthState.Unhealthy;
            HealthReason = AccessTelemetryReason.RecordIdConflict;
            _status.Publish(Health, HealthReason, acceptedAt);
            AccessTelemetryLifecycleMetrics.Record(AccessTelemetryRecordState.Failed, AccessTelemetryReason.RecordIdConflict);
            return new AccessTelemetryPersistenceResult(
                AccessTelemetryPersistenceStatus.Conflict,
                AccessTelemetryReason.RecordIdConflict);
        }

        AccessTelemetryLifecycleMetrics.Record(AccessTelemetryRecordState.Persisted, AccessTelemetryReason.None);
        _status.Publish(AccessTelemetryHealthState.Healthy, AccessTelemetryReason.None, acceptedAt);
        return new AccessTelemetryPersistenceResult(
            status == AccessTelemetryStoreWriteStatus.Inserted
                ? AccessTelemetryPersistenceStatus.Inserted
                : AccessTelemetryPersistenceStatus.Idempotent,
            AccessTelemetryReason.None,
            ttlInSeconds);
    }

    /// <summary>Executes one bounded logical-purge actor turn.</summary>
    public Task<AccessTelemetryPurgeResult> PurgeAsync(CancellationToken cancellationToken)
        => PurgeAsync(_timeProvider.GetUtcNow(), cancellationToken);

    /// <summary>Executes one bounded logical-purge turn using actor-verified trusted time.</summary>
    public async Task<AccessTelemetryPurgeResult> PurgeAsync(DateTimeOffset now, CancellationToken cancellationToken)
    {
        long purgeStarted = Stopwatch.GetTimestamp();
        long dueMinute = AccessTelemetryExpiryIndex.GetExpiryMinute(now);
        IReadOnlyList<AccessTelemetryExpiryEntry> due = await _store.GetDueEntriesAsync(
            dueMinute,
            (_optionsProvider?.Current ?? _testOptions!).PurgeRecordLimit + 1,
            cancellationToken).ConfigureAwait(false);
        var stopwatch = Stopwatch.StartNew();
        int processed = 0;
        int purged = 0;
        int verified = 0;
        AccessTelemetryExpiryEntry? last = null;
        foreach (AccessTelemetryExpiryEntry entry in due.Take((_optionsProvider?.Current ?? _testOptions!).PurgeRecordLimit))
        {
            if (processed > 0 && stopwatch.Elapsed >= TurnBudget)
            {
                break;
            }

            processed++;
            last = entry;
            if (ParseUtc(entry.ExpiresAtUtc) > now)
            {
                continue;
            }

            AccessTelemetryLifecycleMetrics.RecordExpiryLag(
                Math.Max(0, (now - ParseUtc(entry.ExpiresAtUtc)).TotalSeconds));
            long stateStarted = Stopwatch.GetTimestamp();
            AccessTelemetryDeleteStatus deleteStatus;
            try
            {
                deleteStatus = await _store.DeleteAndVerifyAsync(entry, cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                AccessTelemetryLifecycleMetrics.RecordStateLatency(
                    Stopwatch.GetElapsedTime(stateStarted).TotalMilliseconds);
            }
            if (deleteStatus is AccessTelemetryDeleteStatus.Deleted or AccessTelemetryDeleteStatus.AlreadyAbsent)
            {
                purged++;
                verified++;
                AccessTelemetryLifecycleMetrics.Record(AccessTelemetryRecordState.Purged, AccessTelemetryReason.None);
            }
            else if (deleteStatus == AccessTelemetryDeleteStatus.VerificationFailed)
            {
                Health = AccessTelemetryHealthState.Unhealthy;
                HealthReason = AccessTelemetryReason.DependencyUnavailable;
                _status.Publish(Health, HealthReason, now);
            }
        }

        AccessTelemetryLifecycleMetrics.RecordPurgeLatency(
            Stopwatch.GetElapsedTime(purgeStarted).TotalMilliseconds);
        return new AccessTelemetryPurgeResult(
            processed,
            purged,
            verified,
            due.Count > processed,
            last?.ExpiryMinute,
            last?.Shard);
    }

    private static DateTimeOffset ParseUtc(string value)
        => DateTimeOffset.ParseExact(
            value,
            "yyyy-MM-dd'T'HH:mm:ss.fff'Z'",
            CultureInfo.InvariantCulture,
            DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal);

    private static string FormatUtc(DateTimeOffset value)
        => DateTimeOffset.FromUnixTimeMilliseconds(value.ToUnixTimeMilliseconds()).UtcDateTime.ToString(
            "yyyy-MM-dd'T'HH:mm:ss.fff'Z'",
            CultureInfo.InvariantCulture);
}
