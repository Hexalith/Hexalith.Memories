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
        using var deadline = new CancellationTokenSource(TotalTimeout, _timeProvider);
        using var startup = CancellationTokenSource.CreateLinkedTokenSource(deadline.Token, cancellationToken);
        int? lastCount = null;
        try
        {
            while (!startup.IsCancellationRequested)
            {
                (int? inFlight, string? failureReason, Exception? failure) =
                    await TryCountInFlightAsync(startup.Token).ConfigureAwait(false);
                if (deadline.IsCancellationRequested)
                {
                    break;
                }

                if (inFlight is null)
                {
                    // Fail open when the sidecar or registry is unavailable. Operators still quiesce intake.
                    LogGateFailedOpen(_logger, failureReason ?? "count-unavailable", failure);
                    return;
                }

                startup.Token.ThrowIfCancellationRequested();
                if (inFlight.Value == 0)
                {
                    LogDrained(_logger);
                    return;
                }

                lastCount = inFlight.Value;
                LogDraining(_logger, inFlight.Value);
                await Task.Delay(PollInterval, _timeProvider, startup.Token).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException) when (startup.IsCancellationRequested)
        {
            // Classify the end of startup using the original cancellation sources.
        }

        if (deadline.IsCancellationRequested && !cancellationToken.IsCancellationRequested)
        {
            LogDrainTimeout(_logger, lastCount);
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

    /// <summary>Counts active workflows or returns the reason the gate could not obtain a count.</summary>
    internal async Task<(int? Count, string? FailureReason, Exception? Failure)> TryCountInFlightAsync(CancellationToken cancellationToken)
    {
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            int count = 0;
            IReadOnlyList<IngestionWorkflowInFlightEntry> entries = await _inFlightRegistry
                .ListAsync(cancellationToken)
                .WaitAsync(cancellationToken)
                .ConfigureAwait(false);
            cancellationToken.ThrowIfCancellationRequested();

            if (entries.Count == 0
                && !await _inFlightRegistry.IsInitializedAsync(cancellationToken)
                    .WaitAsync(cancellationToken).ConfigureAwait(false))
            {
                int fallbackCount = await CountInFlightByEnumerationFallbackAsync(cancellationToken).ConfigureAwait(false);
                if (fallbackCount == 0)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    await _inFlightRegistry.MarkInitializedAsync(cancellationToken)
                        .WaitAsync(cancellationToken).ConfigureAwait(false);
                }

                return (fallbackCount, null, null);
            }

            foreach (IngestionWorkflowInFlightEntry entry in entries)
            {
                WorkflowState? state = await QuerySidecarAsync(
                    token => _workflowClient.GetWorkflowStateAsync(entry.InstanceId, false, token),
                    cancellationToken).ConfigureAwait(false);

                if (state is null || !state.Exists || !IsActive(state.RuntimeStatus))
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    await _inFlightRegistry.RemoveAsync(entry.InstanceId, cancellationToken)
                        .WaitAsync(cancellationToken).ConfigureAwait(false);
                    continue;
                }

                count++;
            }

            return (count, null, null);
        }
        catch (ReplaySafetyQueryTimeoutException)
        {
            return (null, "sidecar-query-timeout", null);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            return (null, "sidecar-query-exception", ex);
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
            WorkflowInstancePage page = await QuerySidecarAsync(
                token => _workflowClient.ListInstanceIdsAsync(continuationToken, EnumerationFallbackPageSize, token),
                cancellationToken).ConfigureAwait(false);

            foreach (string instanceId in page.InstanceIds)
            {
                cancellationToken.ThrowIfCancellationRequested();
                WorkflowState? state = await QuerySidecarAsync(
                    token => _workflowClient.GetWorkflowStateAsync(instanceId, false, token),
                    cancellationToken).ConfigureAwait(false);

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

    private async Task<T> QuerySidecarAsync<T>(Func<CancellationToken, Task<T>> query, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        using var queryTimeout = new CancellationTokenSource(PerQueryTimeout, _timeProvider);
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(queryTimeout.Token, cancellationToken);
        try
        {
            return await query(linked.Token).WaitAsync(linked.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException exception) when (queryTimeout.IsCancellationRequested)
        {
            throw new ReplaySafetyQueryTimeoutException(exception);
        }
    }

    [LoggerMessage(EventId = 9171, Level = LogLevel.Warning, Message = "Replay-safety gate: {InFlightCount} in-flight IngestionWorkflow instance(s) detected — delaying startup (event 9171).")]
    private static partial void LogDraining(ILogger logger, int inFlightCount);

    [LoggerMessage(Level = LogLevel.Information, Message = "Replay-safety gate: no in-flight IngestionWorkflow instances — proceeding.")]
    private static partial void LogDrained(ILogger logger);

    [LoggerMessage(EventId = 9172, Level = LogLevel.Critical, Message = "Replay-safety gate: timed out — proceeding anyway (event 9172). Last completed active count: {RemainingCount}.")]
    private static partial void LogDrainTimeout(ILogger logger, int? remainingCount);

    [LoggerMessage(EventId = 9173, Level = LogLevel.Critical, Message = "Replay-safety gate failing open (event 9173): reason={Reason}. Operator action: verify Redis registry connectivity, DAPR sidecar health, and Dapr workflow state reads.")]
    private static partial void LogGateFailedOpen(ILogger logger, string reason, Exception? exception);

}
