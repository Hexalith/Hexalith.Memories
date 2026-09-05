// <copyright file="AccessTelemetryQualificationWorkloadRunner.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Telemetry.AccessTelemetryLifecycle;

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

    private static readonly IReadOnlyDictionary<string, object?> QueryParameters =
        new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["operation"] = "tenant-create",
            ["state"] = "completed",
            ["workflowInstanceIdPrefix"] = "qualification",
        };
    private readonly AccessTelemetryQualificationAccounting _accounting;
    private readonly AccessTelemetryQualificationGate _gate;
    private readonly ILogger<AccessTelemetryCategory> _logger;
    private readonly int _recordsPerSecond;
    private readonly int _steadyStateSeconds;
    private readonly TimeProvider _timeProvider;
    private readonly SemaphoreSlim _singleRun = new(1, 1);

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
    public async Task<AccessTelemetryQualificationWorkloadResult> RunAsync(CancellationToken cancellationToken)
    {
        if (!await _singleRun.WaitAsync(0, cancellationToken).ConfigureAwait(false))
        {
            throw new InvalidOperationException("qualification_workload_already_running");
        }

        try
        {
            if (!_gate.TryValidate(out string reason))
            {
                throw new InvalidOperationException(reason);
            }

            AccessTelemetryQualificationAccountingSnapshot before = _accounting.Current;
            long expected = checked((long)_recordsPerSecond * _steadyStateSeconds);
            for (int second = 0; second < _steadyStateSeconds; second++)
            {
                if (!_gate.TryValidate(out reason))
                {
                    throw new InvalidOperationException(reason);
                }

                DateTimeOffset intervalStart = _timeProvider.GetUtcNow();
                for (int record = 0; record < _recordsPerSecond; record++)
                {
                    AccessTelemetryEvent auditEvent = AccessTelemetryLog.CreateEvent(
                        7506,
                        "qualification-tenant",
                        AccessTelemetryLog.OperationTenantLifecycle,
                        caseId: null,
                        user: "qualification-runner",
                        QueryParameters,
                        resultCount: null,
                        durationMs: 0,
                        AccessTelemetryLog.OutcomeOk,
                        errorCode: null,
                        currentActivity: null);
                    AccessTelemetryLog.LogTenantLifecycleAccess(_logger, auditEvent);
                }

                DateTimeOffset nextInterval = intervalStart.AddSeconds(1);
                TimeSpan remaining = nextInterval - _timeProvider.GetUtcNow();
                if (remaining > TimeSpan.Zero)
                {
                    await Task.Delay(remaining, _timeProvider, cancellationToken).ConfigureAwait(false);
                }
            }

            DateTimeOffset acknowledgementDeadline = _timeProvider.GetUtcNow().AddMinutes(5);
            AccessTelemetryQualificationAccountingSnapshot delta;
            do
            {
                delta = _accounting.Current.Since(before);
                if (delta.Persisted + delta.Rejected + delta.Dropped >= expected)
                {
                    break;
                }

                if (!_gate.TryValidate(out reason))
                {
                    throw new InvalidOperationException(reason);
                }

                await Task.Delay(TimeSpan.FromMilliseconds(100), _timeProvider, cancellationToken).ConfigureAwait(false);
            }
            while (_timeProvider.GetUtcNow() < acknowledgementDeadline);

            delta = _accounting.Current.Since(before);
            string writer = Environment.GetEnvironmentVariable("HEXALITH_QUALIFICATION_WRITER") ?? "unassigned";
            return new AccessTelemetryQualificationWorkloadResult(
                writer,
                expected,
                delta.Persisted,
                delta.Persisted,
                delta.Conflicted,
                delta.Persisted,
                delta.Dropped,
                Math.Max(0, delta.Rejected - delta.Conflicted),
                Math.Max(1, delta.Attempted + delta.Enqueued + delta.Persisted + delta.Rejected + delta.Dropped));
        }
        finally
        {
            _ = _singleRun.Release();
        }
    }
}
