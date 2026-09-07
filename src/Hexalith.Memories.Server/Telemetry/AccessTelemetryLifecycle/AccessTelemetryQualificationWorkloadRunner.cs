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

    /// <summary>The shared Crockford golden identity consumed by C# and Python qualification IDs.</summary>
    public const string CrockfordGoldenIdentity = "qualification-0123456789abcdef0123456789abcdef-000";

    /// <summary>The shared Crockford golden record ID for <see cref="CrockfordGoldenIdentity"/>.</summary>
    public const string CrockfordGoldenRecordId = "21HGE3EHP37C6JQJJ8CHZFQ8FC";

    /// <summary>The shared golden run identity used with <see cref="CrockfordGoldenSegmentId"/>.</summary>
    public const string CrockfordGoldenRunId = "run-golden";

    /// <summary>The shared golden segment identity used with <see cref="CrockfordGoldenRunId"/>.</summary>
    public const string CrockfordGoldenSegmentId = "writer-1-segment-0001";

    /// <summary>The shared golden ordinal used with the run/segment identity pair.</summary>
    public const int CrockfordGoldenOrdinal = 0;

    /// <summary>The shared Crockford record ID for the golden run/segment/ordinal triple.</summary>
    public const string CrockfordGoldenRunSegmentRecordId = "1RDFHWEWT5XK6766PQXEJT35XC";

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
    private readonly ConcurrentDictionary<string, SegmentWork> _segments =
        new(StringComparer.Ordinal);
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
    public Task<AccessTelemetryQualificationWorkloadResult> RunAsync(
        string runId,
        string segmentId,
        long emittedUtcMs,
        CancellationToken cancellationToken)
        => RunAsync(runId, segmentId, emittedUtcMs, waitForAcknowledgement: true, cancellationToken);

    /// <summary>Runs one fixed one-second segment, optionally returning after emit and before acknowledgement.</summary>
    /// <param name="waitForAcknowledgement">When <see langword="false"/>, return emit timestamps without waiting for acknowledgement.</param>
    /// <param name="cancellationToken">Stops the request without weakening gate expiry.</param>
    /// <returns>Privacy-safe process-local aggregate accounting.</returns>
    public async Task<AccessTelemetryQualificationWorkloadResult> RunAsync(
        string runId,
        string segmentId,
        long emittedUtcMs,
        bool waitForAcknowledgement,
        CancellationToken cancellationToken)
    {
        ValidateIdentity(runId, nameof(runId));
        ValidateIdentity(segmentId, nameof(segmentId));
        if (!_gate.TryValidate(out string reason))
        {
            throw new InvalidOperationException(reason);
        }

        string key = $"{runId}/{segmentId}";
        SegmentWork work = GetOrAdd(key, runId, segmentId, emittedUtcMs);
        _ = work.Complete.Value;
        try
        {
            if (!waitForAcknowledgement)
            {
                AccessTelemetryQualificationWorkloadResult emitted = await work.Emit
                    .Task
                    .WaitAsync(cancellationToken)
                    .ConfigureAwait(false);
                if (work.Complete.Value.IsCompletedSuccessfully)
                {
                    return await work.Complete.Value.ConfigureAwait(false);
                }

                return emitted;
            }

            return await work.Complete.Value.WaitAsync(cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            EvictIfUnsuccessful(key, work);
        }
    }

    private SegmentWork GetOrAdd(string key, string runId, string segmentId, long emittedUtcMs)
    {
        lock (_segmentGate)
        {
            if (_segments.TryGetValue(key, out SegmentWork? existing))
            {
                if (existing.Complete.IsValueCreated)
                {
                    Task<AccessTelemetryQualificationWorkloadResult> cached = existing.Complete.Value;
                    if (cached.IsFaulted || cached.IsCanceled)
                    {
                        _ = _segments.TryRemove(key, out _);
                    }
                    else if (existing.EmittedUtcMs != emittedUtcMs)
                    {
                        throw new InvalidOperationException("qualification_segment_timestamp_conflict");
                    }
                    else
                    {
                        return existing;
                    }
                }
                else if (existing.EmittedUtcMs != emittedUtcMs)
                {
                    throw new InvalidOperationException("qualification_segment_timestamp_conflict");
                }
                else
                {
                    return existing;
                }
            }

            if (_segments.Count >= MaximumCachedSegments)
            {
                throw new InvalidOperationException("qualification_segment_capacity_exhausted");
            }

            ValidateEmissionTime(emittedUtcMs);
            var created = new SegmentWork(
                emittedUtcMs,
                work => RunSegmentAsync(runId, segmentId, emittedUtcMs, work));
            if (_segments.TryAdd(key, created))
            {
                return created;
            }

            SegmentWork concurrent = _segments[key];
            if (concurrent.EmittedUtcMs != emittedUtcMs)
            {
                throw new InvalidOperationException("qualification_segment_timestamp_conflict");
            }

            return concurrent;
        }
    }

    private void EvictIfUnsuccessful(string key, SegmentWork work)
    {
        Task<AccessTelemetryQualificationWorkloadResult> task = work.Complete.Value;
        if (!task.IsFaulted && !task.IsCanceled)
        {
            return;
        }

        lock (_segmentGate)
        {
            if (_segments.TryGetValue(key, out SegmentWork? current) && ReferenceEquals(current, work))
            {
                _ = _segments.TryRemove(key, out _);
            }
        }
    }

    private async Task<AccessTelemetryQualificationWorkloadResult> RunSegmentAsync(
        string runId,
        string segmentId,
        long emittedUtcMs,
        SegmentWork work)
    {
        try
        {
            if (!_gate.TryValidate(out string reason))
            {
                throw new InvalidOperationException(reason);
            }

            long startedUtcMs = _timeProvider.GetUtcNow().ToUnixTimeMilliseconds();
            long expected = checked((long)_recordsPerSecond * _steadyStateSeconds);
            string correlation = QualificationCorrelation(runId, segmentId);
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
                    string qualificationIdentity = QualificationIdentity(runId, segmentId, eventOrdinal);
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

            long emitFinishedUtcMs = _timeProvider.GetUtcNow().ToUnixTimeMilliseconds();
            string writer = Environment.GetEnvironmentVariable("HEXALITH_QUALIFICATION_WRITER") ?? "unassigned";
            AccessTelemetryQualificationAccountingSnapshot emitDelta = _accounting.ForCorrelation($"qualification-{correlation}").Since(before);
            AccessTelemetryQualificationWorkloadResult emitted = CreateResult(
                runId,
                segmentId,
                writer,
                startedUtcMs,
                emitFinishedUtcMs,
                acknowledgedUtcMs: 0,
                emitDelta,
                recordIds);
            _ = work.Emit.TrySetResult(emitted);

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
            long acknowledgedUtcMs = _timeProvider.GetUtcNow().ToUnixTimeMilliseconds();
            return CreateResult(
                runId,
                segmentId,
                writer,
                startedUtcMs,
                emitFinishedUtcMs,
                acknowledgedUtcMs,
                delta,
                recordIds);
        }
        catch (Exception exception)
        {
            _ = work.Emit.TrySetException(exception);
            throw;
        }
    }

    private static AccessTelemetryQualificationWorkloadResult CreateResult(
        string runId,
        string segmentId,
        string writer,
        long startedUtcMs,
        long emitFinishedUtcMs,
        long acknowledgedUtcMs,
        AccessTelemetryQualificationAccountingSnapshot delta,
        IReadOnlyList<string> recordIds)
        => new(
            runId,
            segmentId,
            writer,
            startedUtcMs,
            emitFinishedUtcMs,
            emitFinishedUtcMs,
            acknowledgedUtcMs,
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

    /// <summary>Derives the bounded SHA-256 correlation shared with the Python producer.</summary>
    /// <param name="runId">The host-assigned qualification run identity.</param>
    /// <param name="segmentId">The host-assigned one-second segment identity.</param>
    /// <returns>The lowercase 32-character hex correlation prefix.</returns>
    internal static string QualificationCorrelation(string runId, string segmentId)
        => Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes($"{runId}/{segmentId}")))[..32];

    /// <summary>Derives the exact qualification identity for one record ordinal.</summary>
    /// <param name="runId">The host-assigned qualification run identity.</param>
    /// <param name="segmentId">The host-assigned one-second segment identity.</param>
    /// <param name="ordinal">The zero-based record ordinal inside the segment.</param>
    /// <returns>The 50-character qualification identity hashed into a Crockford record ID.</returns>
    internal static string QualificationIdentity(string runId, string segmentId, int ordinal)
        => string.Create(
            System.Globalization.CultureInfo.InvariantCulture,
            $"qualification-{QualificationCorrelation(runId, segmentId)}-{ordinal:000}");

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

    private sealed class SegmentWork
    {
        public SegmentWork(
            long emittedUtcMs,
            Func<SegmentWork, Task<AccessTelemetryQualificationWorkloadResult>> factory)
        {
            EmittedUtcMs = emittedUtcMs;
            Complete = new Lazy<Task<AccessTelemetryQualificationWorkloadResult>>(
                () => factory(this),
                LazyThreadSafetyMode.ExecutionAndPublication);
        }

        public long EmittedUtcMs { get; }

        public TaskCompletionSource<AccessTelemetryQualificationWorkloadResult> Emit { get; } =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public Lazy<Task<AccessTelemetryQualificationWorkloadResult>> Complete { get; }
    }
}
