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

    /// <summary>Initializes the lifecycle processor.</summary>
    public AccessTelemetryLifecycleProcessor(IAccessTelemetryStateStore store, TimeProvider timeProvider)
    {
        _store = store;
        _timeProvider = timeProvider;
    }

    /// <summary>Gets the current fail-closed lifecycle health.</summary>
    public AccessTelemetryHealthState Health { get; private set; } = AccessTelemetryHealthState.Healthy;

    /// <summary>Validates and atomically persists one record/index pair.</summary>
    public async Task<AccessTelemetryPersistenceResult> PersistAsync(
        AccessTelemetryRecord record,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(record);
        try
        {
            _ = AccessTelemetryCanonicalizer.CanonicalizeRecord(record);
        }
        catch (AccessTelemetryContractException)
        {
            return new AccessTelemetryPersistenceResult(
                AccessTelemetryPersistenceStatus.Rejected,
                AccessTelemetryReason.SchemaMismatch);
        }

        DateTimeOffset now = _timeProvider.GetUtcNow();
        DateTimeOffset emittedAt = ParseUtc(record.EmittedAtUtc);
        DateTimeOffset expiresAt = ParseUtc(record.ExpiresAtUtc);
        if (emittedAt - now > AcceptedFutureSkew)
        {
            Health = AccessTelemetryHealthState.Unhealthy;
            AccessTelemetryLifecycleMetrics.Record(AccessTelemetryRecordState.Rejected, AccessTelemetryReason.ClockUntrusted);
            return new AccessTelemetryPersistenceResult(
                AccessTelemetryPersistenceStatus.Rejected,
                AccessTelemetryReason.ClockUntrusted);
        }

        if (expiresAt <= now)
        {
            return new AccessTelemetryPersistenceResult(
                AccessTelemetryPersistenceStatus.Rejected,
                AccessTelemetryReason.Expired);
        }

        int ttlInSeconds = checked((int)Math.Ceiling((expiresAt - now).TotalSeconds));
        var expiryEntry = new AccessTelemetryExpiryEntry(
            record.RecordId,
            AccessTelemetryExpiryIndex.GetExpiryMinute(expiresAt),
            AccessTelemetryExpiryIndex.GetShard(record.RecordId),
            record.EnvelopeHash,
            record.ExpiresAtUtc);
        long stateStarted = Stopwatch.GetTimestamp();
        AccessTelemetryStoreWriteStatus status;
        try
        {
            status = await _store.WriteRecordAndIndexAsync(
                record,
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
            AccessTelemetryLifecycleMetrics.Record(AccessTelemetryRecordState.Failed, AccessTelemetryReason.RecordIdConflict);
            return new AccessTelemetryPersistenceResult(
                AccessTelemetryPersistenceStatus.Conflict,
                AccessTelemetryReason.RecordIdConflict);
        }

        AccessTelemetryLifecycleMetrics.Record(AccessTelemetryRecordState.Persisted, AccessTelemetryReason.None);
        return new AccessTelemetryPersistenceResult(
            status == AccessTelemetryStoreWriteStatus.Inserted
                ? AccessTelemetryPersistenceStatus.Inserted
                : AccessTelemetryPersistenceStatus.Idempotent,
            AccessTelemetryReason.None,
            ttlInSeconds);
    }

    /// <summary>Executes one bounded logical-purge actor turn.</summary>
    public async Task<AccessTelemetryPurgeResult> PurgeAsync(CancellationToken cancellationToken)
    {
        long purgeStarted = Stopwatch.GetTimestamp();
        DateTimeOffset now = _timeProvider.GetUtcNow();
        long dueMinute = AccessTelemetryExpiryIndex.GetExpiryMinute(now);
        IReadOnlyList<AccessTelemetryExpiryEntry> due = await _store.GetDueEntriesAsync(
            dueMinute,
            513,
            cancellationToken).ConfigureAwait(false);
        var stopwatch = Stopwatch.StartNew();
        int processed = 0;
        int purged = 0;
        int verified = 0;
        foreach (AccessTelemetryExpiryEntry entry in due.Take(512))
        {
            if (processed > 0 && stopwatch.Elapsed >= TurnBudget)
            {
                break;
            }

            processed++;
            if (ParseUtc(entry.ExpiresAtUtc) > now)
            {
                continue;
            }

            AccessTelemetryLifecycleMetrics.RecordExpiryLag(
                Math.Max(0, (now - ParseUtc(entry.ExpiresAtUtc)).TotalSeconds));
            long stateStarted = Stopwatch.GetTimestamp();
            bool absent;
            try
            {
                absent = await _store.DeleteAndVerifyAsync(entry, cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                AccessTelemetryLifecycleMetrics.RecordStateLatency(
                    Stopwatch.GetElapsedTime(stateStarted).TotalMilliseconds);
            }
            purged++;
            if (absent)
            {
                verified++;
                AccessTelemetryLifecycleMetrics.Record(AccessTelemetryRecordState.Purged, AccessTelemetryReason.None);
            }
            else
            {
                Health = AccessTelemetryHealthState.Unhealthy;
            }
        }

        AccessTelemetryLifecycleMetrics.RecordPurgeLatency(
            Stopwatch.GetElapsedTime(purgeStarted).TotalMilliseconds);
        return new AccessTelemetryPurgeResult(
            processed,
            purged,
            verified,
            due.Count > processed);
    }

    private static DateTimeOffset ParseUtc(string value)
        => DateTimeOffset.ParseExact(
            value,
            "yyyy-MM-dd'T'HH:mm:ss.fff'Z'",
            CultureInfo.InvariantCulture,
            DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal);
}
