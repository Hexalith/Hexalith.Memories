// <copyright file="WorkflowReplaySafetyHostedService.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Hosting;

using System.Reflection;

using Dapr.Workflow;

using Hexalith.Memories.Server.Workflows;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

/// <summary>Story 9.2 Task 5.9 — startup gate that delays workflow-host startup until any
/// version-mismatched in-flight <see cref="IngestionWorkflow"/> instances drain (or a 5-min timeout
/// expires). Implements <see cref="IHostedLifecycleService"/> so <see cref="StartingAsync"/> runs
/// before any other hosted service (per Spike 0.4 — ordering is DI-registration-order-independent).
///
/// Spike 0.2 chose the simple in-flight-<see cref="IngestionWorkflow"/> heuristic to avoid retroactive
/// version-tag backfill. The gate now reads workflow-family metadata reflectively when the SDK exposes
/// it, so startup only waits on <see cref="IngestionWorkflow"/> instances instead of unrelated
/// short-lived workflows.
/// Spike 0.4 chose <see cref="IHostedLifecycleService"/> over
/// <c>IStartupFilter</c> so the gate does NOT depend on DAPR's own hosted-service registration
/// order.</summary>
public sealed partial class WorkflowReplaySafetyHostedService : IHostedLifecycleService
{
    internal static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(5);
    internal static readonly TimeSpan TotalTimeout = TimeSpan.FromMinutes(5);
    internal static readonly TimeSpan PerQueryTimeout = TimeSpan.FromSeconds(10);

    private readonly DaprWorkflowClient _workflowClient;
    private readonly ILogger<WorkflowReplaySafetyHostedService> _logger;
    private readonly TimeProvider _timeProvider;

    public WorkflowReplaySafetyHostedService(
        DaprWorkflowClient workflowClient,
        ILogger<WorkflowReplaySafetyHostedService> logger,
        TimeProvider? timeProvider = null)
    {
        ArgumentNullException.ThrowIfNull(workflowClient);
        ArgumentNullException.ThrowIfNull(logger);
        _workflowClient = workflowClient;
        _logger = logger;
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    /// <inheritdoc/>
    public async Task StartingAsync(CancellationToken cancellationToken)
    {
        DateTimeOffset deadline = _timeProvider.GetUtcNow() + TotalTimeout;

        while (!cancellationToken.IsCancellationRequested)
        {
            int? inFlight = await TryCountInFlightAsync(cancellationToken).ConfigureAwait(false);
            if (inFlight is null)
            {
                // Improvement Z: sidecar unreachable — fail open. A stuck pod is worse than a missing
                // gate. The runbook quiesce still applies as operator-side discipline.
                LogSidecarUnreachable(_logger);
                return;
            }

            if (inFlight.Value == 0)
            {
                LogDrained(_logger);
                return;
            }

            if (_timeProvider.GetUtcNow() >= deadline)
            {
                // Improvement X split: Critical single-shot at timeout — proceed rather than infinite-block.
                LogDrainTimeout(_logger, inFlight.Value);
                return;
            }

            LogDraining(_logger, inFlight.Value);
            try
            {
                await Task.Delay(PollInterval, _timeProvider, cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                return;
            }
        }
    }

    /// <inheritdoc/>
    public Task StartAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    /// <inheritdoc/>
    public Task StartedAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    /// <inheritdoc/>
    public Task StoppingAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    /// <inheritdoc/>
    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    /// <inheritdoc/>
    public Task StoppedAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    internal async Task<int?> TryCountInFlightAsync(CancellationToken cancellationToken)
    {
        try
        {
            int count = 0;
            string? continuation = null;
            do
            {
                Dapr.Workflow.Client.WorkflowInstancePage page;
                using (CancellationTokenSource listCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken))
                {
                    listCts.CancelAfter(PerQueryTimeout);
                    page = await _workflowClient
                        .ListInstanceIdsAsync(continuation, 100, listCts.Token)
                        .ConfigureAwait(false);
                }

                foreach (string instanceId in page.InstanceIds)
                {
                    WorkflowState? state;
                    using (CancellationTokenSource stateCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken))
                    {
                        stateCts.CancelAfter(PerQueryTimeout);
                        state = await _workflowClient
                            .GetWorkflowStateAsync(instanceId, false, stateCts.Token)
                            .ConfigureAwait(false);
                    }

                    if (state is not null && ShouldCountWorkflow(TryGetWorkflowName(state), state.Exists, state.RuntimeStatus))
                    {
                        count++;
                    }
                }

                continuation = page.ContinuationToken;
            }
            while (!string.IsNullOrEmpty(continuation));

            return count;
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            // Per-query timeout elapsed — sidecar unreachable.
            return null;
        }
        catch (Exception ex)
        {
            LogGateQueryFailed(_logger, ex);
            return null;
        }
    }

    internal static bool IsActive(WorkflowRuntimeStatus status)
        => status != WorkflowRuntimeStatus.Completed
            && status != WorkflowRuntimeStatus.Failed
            && status != WorkflowRuntimeStatus.Terminated;

    internal static bool ShouldCountWorkflow(string? workflowName, bool exists, WorkflowRuntimeStatus status)
        => exists
            && string.Equals(workflowName, nameof(IngestionWorkflow), StringComparison.Ordinal)
            && IsActive(status);

    private static string? TryGetWorkflowName(WorkflowState state)
        => state.GetType()
            .GetProperty("Name", BindingFlags.Instance | BindingFlags.Public)
            ?.GetValue(state) as string;

    [LoggerMessage(Level = LogLevel.Warning, Message = "Replay-safety gate: {InFlightCount} in-flight IngestionWorkflow instance(s) detected — delaying startup (event 9171).")]
    private static partial void LogDraining(ILogger logger, int inFlightCount);

    [LoggerMessage(Level = LogLevel.Information, Message = "Replay-safety gate: no in-flight IngestionWorkflow instances — proceeding.")]
    private static partial void LogDrained(ILogger logger);

    [LoggerMessage(Level = LogLevel.Critical, Message = "Replay-safety gate: timed out with {RemainingCount} instance(s) still active — proceeding anyway (event 9172).")]
    private static partial void LogDrainTimeout(ILogger logger, int remainingCount);

    [LoggerMessage(Level = LogLevel.Critical, Message = "Replay-safety gate: DAPR sidecar unreachable — failing open (event 9173).")]
    private static partial void LogSidecarUnreachable(ILogger logger);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Replay-safety gate: workflow enumeration query failed — failing open.")]
    private static partial void LogGateQueryFailed(ILogger logger, Exception exception);
}
