// <copyright file="WorkflowReplaySafetyHostedService.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Hosting;

using Dapr.Workflow;
using Dapr.Workflow.Client;

using Hexalith.Memories.Server.Ingestion;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

/// <summary>Story 9.2 Task 5.9 — startup gate that delays workflow-host startup until any
/// version-mismatched in-flight <see cref="IngestionWorkflow"/> instances drain (or a 5-min timeout
/// expires). Implements <see cref="IHostedLifecycleService"/> so <see cref="StartingAsync"/> runs
/// before any other hosted service (per Spike 0.4 — ordering is DI-registration-order-independent).
///
/// Story 24.5 replaced recurring broad workflow enumeration with an app-owned in-flight registry. The
/// gate reads instance ids that the ingestion scheduler tracked, prunes terminal or missing entries, and
/// delays startup while any tracked workflow remains active. During first rollout, an uninitialized empty
/// registry uses a one-time public SDK enumeration fallback before marking the registry initialized.
/// Spike 0.4 chose <see cref="IHostedLifecycleService"/> over
/// <c>IStartupFilter</c> so the gate does NOT depend on DAPR's own hosted-service registration
/// order.</summary>
internal sealed partial class WorkflowReplaySafetyHostedService : IHostedLifecycleService
{
    private const int EnumerationFallbackPageSize = 100;

    internal static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(5);
    internal static readonly TimeSpan TotalTimeout = TimeSpan.FromMinutes(5);
    internal static readonly TimeSpan PerQueryTimeout = TimeSpan.FromSeconds(10);

    private readonly IDaprWorkflowClient _workflowClient;
    private readonly IIngestionWorkflowInFlightRegistry _inFlightRegistry;
    private readonly ILogger<WorkflowReplaySafetyHostedService> _logger;
    private readonly TimeProvider _timeProvider;

    public WorkflowReplaySafetyHostedService(
        IDaprWorkflowClient workflowClient,
        IIngestionWorkflowInFlightRegistry inFlightRegistry,
        ILogger<WorkflowReplaySafetyHostedService> logger,
        TimeProvider? timeProvider = null)
    {
        ArgumentNullException.ThrowIfNull(workflowClient);
        ArgumentNullException.ThrowIfNull(inFlightRegistry);
        ArgumentNullException.ThrowIfNull(logger);
        _workflowClient = workflowClient;
        _inFlightRegistry = inFlightRegistry;
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
                // (Note: TryCountInFlightAsync already logged the specific sidecar or registry
                // failure reason via LogGateFailedOpen. No duplicate log here.)
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
            IReadOnlyList<IngestionWorkflowInFlightEntry> entries = await _inFlightRegistry
                .ListAsync(cancellationToken)
                .ConfigureAwait(false);

            if (entries.Count == 0
                && !await _inFlightRegistry.IsInitializedAsync(cancellationToken).ConfigureAwait(false))
            {
                int fallbackCount = await CountInFlightByEnumerationFallbackAsync(cancellationToken).ConfigureAwait(false);
                if (fallbackCount == 0)
                {
                    await _inFlightRegistry.MarkInitializedAsync(cancellationToken).ConfigureAwait(false);
                }

                return fallbackCount;
            }

            foreach (IngestionWorkflowInFlightEntry entry in entries)
            {
                WorkflowState? state;
                using (CancellationTokenSource stateCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken))
                {
                    stateCts.CancelAfter(PerQueryTimeout);
                    state = await _workflowClient
                        .GetWorkflowStateAsync(entry.InstanceId, false, stateCts.Token)
                        .ConfigureAwait(false);
                }

                if (state is null || !state.Exists || !IsActive(state.RuntimeStatus))
                {
                    await _inFlightRegistry.RemoveAsync(entry.InstanceId, cancellationToken).ConfigureAwait(false);
                    continue;
                }

                count++;
            }

            return count;
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            // Per-query timeout elapsed — sidecar unreachable.
            LogGateFailedOpen(_logger, "sidecar-query-timeout", null);
            return null;
        }
        catch (Exception ex)
        {
            LogGateFailedOpen(_logger, "sidecar-query-exception", ex);
            return null;
        }
    }

    internal static bool IsActive(WorkflowRuntimeStatus status)
        => status != WorkflowRuntimeStatus.Completed
            && status != WorkflowRuntimeStatus.Failed
            && status != WorkflowRuntimeStatus.Canceled
            && status != WorkflowRuntimeStatus.Terminated;

    internal static bool ShouldCountWorkflow(string? workflowName, bool exists, WorkflowRuntimeStatus status)
        => exists
            && string.Equals(workflowName, "IngestionWorkflow", StringComparison.Ordinal)
            && IsActive(status);

    internal static bool ShouldBlockForUnreadableWorkflowName(bool exists, WorkflowRuntimeStatus status)
        => exists && IsActive(status);

    private async Task<int> CountInFlightByEnumerationFallbackAsync(CancellationToken cancellationToken)
    {
        string? continuationToken = null;
        int count = 0;

        do
        {
            WorkflowInstancePage page;
            using (CancellationTokenSource listCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken))
            {
                listCts.CancelAfter(PerQueryTimeout);
                page = await _workflowClient
                    .ListInstanceIdsAsync(continuationToken, EnumerationFallbackPageSize, listCts.Token)
                    .ConfigureAwait(false);
            }

            foreach (string instanceId in page.InstanceIds)
            {
                cancellationToken.ThrowIfCancellationRequested();
                WorkflowState? state;
                using (CancellationTokenSource stateCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken))
                {
                    stateCts.CancelAfter(PerQueryTimeout);
                    state = await _workflowClient
                        .GetWorkflowStateAsync(instanceId, false, stateCts.Token)
                        .ConfigureAwait(false);
                }

                if (state is null)
                {
                    continue;
                }

                if (ShouldCountWorkflow(state.WorkflowName, state.Exists, state.RuntimeStatus)
                    || (string.IsNullOrWhiteSpace(state.WorkflowName)
                        && ShouldBlockForUnreadableWorkflowName(state.Exists, state.RuntimeStatus)))
                {
                    count++;
                }
            }

            continuationToken = page.ContinuationToken;
        }
        while (!string.IsNullOrEmpty(continuationToken));

        return count;
    }

    [LoggerMessage(EventId = 9171, Level = LogLevel.Warning, Message = "Replay-safety gate: {InFlightCount} in-flight IngestionWorkflow instance(s) detected — delaying startup (event 9171).")]
    private static partial void LogDraining(ILogger logger, int inFlightCount);

    [LoggerMessage(Level = LogLevel.Information, Message = "Replay-safety gate: no in-flight IngestionWorkflow instances — proceeding.")]
    private static partial void LogDrained(ILogger logger);

    [LoggerMessage(EventId = 9172, Level = LogLevel.Critical, Message = "Replay-safety gate: timed out with {RemainingCount} instance(s) still active — proceeding anyway (event 9172).")]
    private static partial void LogDrainTimeout(ILogger logger, int remainingCount);

    [LoggerMessage(EventId = 9173, Level = LogLevel.Critical, Message = "Replay-safety gate failing open (event 9173): reason={Reason}. Operator action: verify Redis registry connectivity, DAPR sidecar health, and Dapr workflow state reads.")]
    private static partial void LogGateFailedOpen(ILogger logger, string reason, Exception? exception);

}
