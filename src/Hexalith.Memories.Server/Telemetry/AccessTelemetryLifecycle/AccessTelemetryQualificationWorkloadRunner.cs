// <copyright file="AccessTelemetryQualificationWorkloadRunner.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Telemetry.AccessTelemetryLifecycle;

using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

using Hexalith.Memories.Contracts.V1;
using Hexalith.Memories.Server.Telemetry;

/// <summary>Emits the one closed two-writer workload through the normal typed logger pipeline.</summary>
internal sealed class AccessTelemetryQualificationWorkloadRunner
{
    /// <summary>The exact per-writer record rate; two writers produce the ADR cluster total of 250/s.</summary>
    public const int RecordsPerSecond = 125;

    /// <summary>The exact steady-state duration.</summary>
    public const int SteadyStateSeconds = 30 * 60;

    /// <summary>The exact duration emitted by one resumable host-controlled segment.</summary>
    public const int SegmentSeconds = 1;

    private const int MaximumCachedSegments = 4096;
    private static readonly Regex BoundedIdentity = new(
        "\\A[a-z0-9][a-z0-9-]{0,63}\\z",
        RegexOptions.CultureInvariant | RegexOptions.NonBacktracking);
    private readonly AccessTelemetryQualificationAccounting _accounting;
    private readonly AccessTelemetryQualificationGate _gate;
    private readonly ILogger<AccessTelemetryCategory> _logger;
    private readonly int _recordsPerSecond;
    private readonly int _steadyStateSeconds;
    private readonly TimeProvider _timeProvider;
    private readonly ConcurrentDictionary<string, Lazy<Task<AccessTelemetryQualificationWorkloadResult>>> _segments =
        new(StringComparer.Ordinal);
    private readonly ConcurrentDictionary<string, long> _segmentEmissionTimes = new(StringComparer.Ordinal);
    private readonly object _segmentGate = new();

    /// <summary>Initializes the exact Production-shaped fixed workload.</summary>
    public AccessTelemetryQualificationWorkloadRunner(
        ILogger<AccessTelemetryCategory> logger,
        AccessTelemetryQualificationAccounting accounting,
        AccessTelemetryQualificationGate gate,
        TimeProvider timeProvider)
        : this(logger, accounting, gate, timeProvider, RecordsPerSecond, SegmentSeconds)
    {
    }

    /// <summary>Initializes a bounded test seam without changing the mapped endpoint contract.</summary>
    internal AccessTelemetryQualificationWorkloadRunner(
        ILogger<AccessTelemetryCategory> logger,
        AccessTelemetryQualificationAccounting accounting,
        AccessTelemetryQualificationGate gate,
        TimeProvider timeProvider,
        int recordsPerSecond,
        int steadyStateSeconds)
    {
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(accounting);
        ArgumentNullException.ThrowIfNull(gate);
        ArgumentNullException.ThrowIfNull(timeProvider);
        ArgumentOutOfRangeException.ThrowIfLessThan(recordsPerSecond, 1);
        ArgumentOutOfRangeException.ThrowIfLessThan(steadyStateSeconds, 1);
        _logger = logger;
        _accounting = accounting;
        _gate = gate;
        _timeProvider = timeProvider;
        _recordsPerSecond = recordsPerSecond;
        _steadyStateSeconds = steadyStateSeconds;
    }

    /// <summary>Runs one fixed one-second segment and waits for its bounded lifecycle accounting.</summary>
    /// <param name="cancellationToken">Stops the request without weakening gate expiry.</param>
    /// <returns>Privacy-safe process-local aggregate accounting.</returns>
    public async Task<AccessTelemetryQualificationWorkloadResult> RunAsync(
        string runId,
        string segmentId,
        long emittedUtcMs,
        CancellationToken cancellationToken)
    {
        ValidateIdentity(runId, nameof(runId));
        ValidateIdentity(segmentId, nameof(segmentId));
        if (!_gate.TryValidate(out string reason))
        {
            throw new InvalidOperationException(reason);
        }

        string key = $"{runId}/{segmentId}";
        Lazy<Task<AccessTelemetryQualificationWorkloadResult>> segment;
        lock (_segmentGate)
        {
            if (!_segments.TryGetValue(key, out segment!))
            {
                if (_segments.Count >= MaximumCachedSegments)
                {
                    throw new InvalidOperationException("qualification_segment_capacity_exhausted");
                }

                ValidateEmissionTime(emittedUtcMs);
                _segmentEmissionTimes[key] = emittedUtcMs;
                segment = new Lazy<Task<AccessTelemetryQualificationWorkloadResult>>(
                    () => RunSegmentAsync(runId, segmentId, emittedUtcMs),
                    LazyThreadSafetyMode.ExecutionAndPublication);
                if (!_segments.TryAdd(key, segment))
                {
                    segment = _segments[key];
                }
            }
            else if (!_segmentEmissionTimes.TryGetValue(key, out long priorEmissionTime) ||
                priorEmissionTime != emittedUtcMs)
            {
                throw new InvalidOperationException("qualification_segment_timestamp_conflict");
            }
        }

        return await segment.Value.WaitAsync(cancellationToken).ConfigureAwait(false);
    }

    private async Task<AccessTelemetryQualificationWorkloadResult> RunSegmentAsync(
        string runId,
        string segmentId,
        long emittedUtcMs)
    {
        if (!_gate.TryValidate(out string reason))
        {
            throw new InvalidOperationException(reason);
        }

        long startedUtcMs = _timeProvider.GetUtcNow().ToUnixTimeMilliseconds();
        long expected = checked((long)_recordsPerSecond * _steadyStateSeconds);
        string correlation = Convert.ToHexStringLower(
            SHA256.HashData(Encoding.UTF8.GetBytes($"{runId}/{segmentId}")))[..32];
        AccessTelemetryQualificationAccountingSnapshot before = _accounting.ForCorrelation($"qualification-{correlation}");
        int eventOrdinal = 0;
        List<string> recordIds = new(checked(_recordsPerSecond * _steadyStateSeconds));
        for (int second = 0; second < _steadyStateSeconds; second++)
        {
            if (!_gate.TryValidate(out reason))
            {
                throw new InvalidOperationException(reason);
            }

            DateTimeOffset intervalStart = _timeProvider.GetUtcNow();
            for (int record = 0; record < _recordsPerSecond; record++)
            {
                string qualificationIdentity = $"qualification-{correlation}-{eventOrdinal:000}";
                recordIds.Add(AccessTelemetrySanitizer.CreateQualificationRecordId(qualificationIdentity));
                IReadOnlyDictionary<string, object?> queryParameters =
                    new Dictionary<string, object?>(StringComparer.Ordinal)
                    {
                        ["operation"] = "tenant-create",
                        ["state"] = "completed",
                        ["workflowInstanceIdPrefix"] = qualificationIdentity,
                    };
                AccessTelemetryEvent auditEvent = AccessTelemetryLog.CreateEvent(
                    7506,
                    "qualification-tenant",
                    AccessTelemetryLog.OperationTenantLifecycle,
                    caseId: null,
                    user: "qualification-runner",
                    queryParameters,
                    resultCount: null,
                    durationMs: 0,
                    AccessTelemetryLog.OutcomeOk,
                    errorCode: null,
                    currentActivity: null) with
                {
                    Timestamp = DateTimeOffset.FromUnixTimeMilliseconds(emittedUtcMs + eventOrdinal)
                        .UtcDateTime.ToString("O", System.Globalization.CultureInfo.InvariantCulture),
                };
                AccessTelemetryLog.LogTenantLifecycleAccess(_logger, auditEvent);
                eventOrdinal++;
            }

            DateTimeOffset nextInterval = intervalStart.AddSeconds(1);
            TimeSpan remaining = nextInterval - _timeProvider.GetUtcNow();
            if (remaining > TimeSpan.Zero)
            {
                await Task.Delay(remaining, _timeProvider, CancellationToken.None).ConfigureAwait(false);
            }
        }

        DateTimeOffset acknowledgementDeadline = _timeProvider.GetUtcNow().AddMinutes(5);
        AccessTelemetryQualificationAccountingSnapshot delta;
        do
        {
            delta = _accounting.ForCorrelation($"qualification-{correlation}").Since(before);
            if (delta.Persisted + delta.Rejected + delta.Dropped >= expected)
            {
                break;
            }

            if (!_gate.TryValidate(out reason))
            {
                throw new InvalidOperationException(reason);
            }

            await Task.Delay(TimeSpan.FromMilliseconds(100), _timeProvider, CancellationToken.None).ConfigureAwait(false);
            }
        while (_timeProvider.GetUtcNow() < acknowledgementDeadline);

        delta = _accounting.ForCorrelation($"qualification-{correlation}").Since(before);
        long finishedUtcMs = _timeProvider.GetUtcNow().ToUnixTimeMilliseconds();
        string writer = Environment.GetEnvironmentVariable("HEXALITH_QUALIFICATION_WRITER") ?? "unassigned";
        return new AccessTelemetryQualificationWorkloadResult(
            runId,
            segmentId,
            writer,
            startedUtcMs,
            finishedUtcMs,
            delta.Attempted,
            delta.Enqueued,
            delta.Persisted,
            delta.Persisted,
            delta.Conflicted,
            delta.Persisted,
            delta.Dropped,
            Math.Max(0, delta.Rejected - delta.Conflicted),
            recordIds,
            Math.Max(1, delta.Attempted + delta.Enqueued + delta.Persisted + delta.Rejected + delta.Dropped));
    }

    private static void ValidateIdentity(string value, string name)
    {
        if (string.IsNullOrWhiteSpace(value) || BoundedIdentity.IsMatch(value) is false)
        {
            throw new InvalidOperationException($"qualification_{name}_invalid");
        }
    }

    private void ValidateEmissionTime(long emittedUtcMs)
    {
        DateTimeOffset now = _timeProvider.GetUtcNow();
        DateTimeOffset emitted;
        try
        {
            emitted = DateTimeOffset.FromUnixTimeMilliseconds(emittedUtcMs);
        }
        catch (ArgumentOutOfRangeException exception)
        {
            throw new InvalidOperationException("qualification_segment_timestamp_invalid", exception);
        }

        if (emitted > now.AddSeconds(1) || emitted < now.Subtract(TimeSpan.FromMinutes(15)))
        {
            throw new InvalidOperationException("qualification_segment_timestamp_invalid");
        }
    }
}
