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
/// version-tag backfill. The gate reads the workflow family name reflectively through
/// <see cref="WorkflowState"/>'s private <c>_metadata</c> field — see <see cref="TryGetWorkflowName"/>.
/// Committed-branch review (2026-04-24) decision D2: every active workflow observed by the gate
/// must produce a readable workflow name. If the private-field path has drifted in a newer
/// Dapr.Workflow SDK, the gate emits Critical <c>9173</c> and fails open rather than silently
/// counting zero (which previously made the gate a no-op).
/// Spike 0.4 chose <see cref="IHostedLifecycleService"/> over
/// <c>IStartupFilter</c> so the gate does NOT depend on DAPR's own hosted-service registration
/// order.</summary>
public sealed partial class WorkflowReplaySafetyHostedService : IHostedLifecycleService
{
    internal static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(5);
    internal static readonly TimeSpan TotalTimeout = TimeSpan.FromMinutes(5);
    internal static readonly TimeSpan PerQueryTimeout = TimeSpan.FromSeconds(10);

    // Cached private-field accessor for WorkflowState._metadata (Dapr.Workflow 1.17.6 layout — verified
    // via reflection probe on 2026-04-25). Dapr.Workflow does not expose Name publicly on
    // WorkflowState, so the gate must drill into the private metadata instance to filter by workflow
    // family. If the SDK renames the field in a future release, the startup probe (D2) surfaces the
    // drift as Critical 9173 rather than silently degrading to "count everything".
    private static readonly FieldInfo? MetadataField = typeof(WorkflowState)
        .GetField("_metadata", BindingFlags.Instance | BindingFlags.NonPublic);

    // S6-P10 (re-review 2026-04-25): cache the metadata-Type → Name PropertyInfo so TryGetWorkflowName
    // does not re-resolve the property on every call. The metadata Type is stable across all
    // WorkflowState instances within a process, so a single Lazy<PropertyInfo?> covers all calls.
    private static readonly Lazy<PropertyInfo?> NamePropertyAccessor = new(ResolveNameProperty);

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
        // S6-P3 + follow-up review (2026-04-25): eager startup probe — emit Critical 9173
        // immediately if the cached private-field accessor OR the cached workflow-name accessor is
        // null. Without this, a fresh deploy with no in-flight workflows AND a drifted SDK would
        // silently pass the gate; the next deploy with active workflows would then fail open. The
        // probe makes SDK drift loud at deploy time.
        if (MetadataField is null)
        {
            LogGateFailedOpen(_logger, "metadata-field-missing", null);
            return;
        }

        if (NamePropertyAccessor.Value is null)
        {
            LogGateFailedOpen(_logger, "workflow-name-property-missing", null);
            return;
        }

        DateTimeOffset deadline = _timeProvider.GetUtcNow() + TotalTimeout;

        while (!cancellationToken.IsCancellationRequested)
        {
            int? inFlight = await TryCountInFlightAsync(cancellationToken).ConfigureAwait(false);
            if (inFlight is null)
            {
                // Improvement Z: sidecar unreachable — fail open. A stuck pod is worse than a missing
                // gate. The runbook quiesce still applies as operator-side discipline.
                // (Note: TryCountInFlightAsync already logged the specific reason, including the D2
                // reflection-probe-failure path via LogGateFailedOpen. No duplicate log here.)
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
            // S6-D2 follow-up (code review 2026-04-25): conservative name resolution. An active
            // workflow with an unreadable family name is treated as potentially unsafe and delays
            // startup until it clears or the existing 5-minute timeout fires. That avoids a false
            // "drained" result when the unreadable instance is actually an IngestionWorkflow.
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

                    if (state is null || !state.Exists || !IsActive(state.RuntimeStatus))
                    {
                        continue;
                    }

                    string? workflowName = TryGetWorkflowName(state);

                    if (string.IsNullOrWhiteSpace(workflowName))
                    {
                        LogWorkflowNameUnreadable(_logger, instanceId);
                        if (ShouldBlockForUnreadableWorkflowName(state.Exists, state.RuntimeStatus))
                        {
                            count++;
                        }

                        continue;
                    }

                    if (ShouldCountWorkflow(workflowName, state.Exists, state.RuntimeStatus))
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
            && status != WorkflowRuntimeStatus.Terminated;

    internal static bool ShouldCountWorkflow(string? workflowName, bool exists, WorkflowRuntimeStatus status)
        => exists
            && string.Equals(workflowName, nameof(IngestionWorkflow), StringComparison.Ordinal)
            && IsActive(status);

    internal static bool ShouldBlockForUnreadableWorkflowName(bool exists, WorkflowRuntimeStatus status)
        => exists && IsActive(status);

    // Walks WorkflowState → private `_metadata` field → public WorkflowMetadata.Name. The top-level
    // `Name` property does not exist on WorkflowState (Dapr.Workflow 1.17.6 — verified via reflection
    // probe), so the earlier public-property lookup always returned null and the gate silently
    // counted zero workflows. This private-field walk is fragile but the D2 startup probe surfaces
    // drift as Critical 9173.
    // S6-P10 (re-review 2026-04-25): Name PropertyInfo is cached lazily via NamePropertyAccessor.
    internal static string? TryGetWorkflowName(WorkflowState state)
    {
        object? metadata = MetadataField?.GetValue(state);
        if (metadata is null)
        {
            return null;
        }

        return NamePropertyAccessor.Value?.GetValue(metadata) as string;
    }

    private static PropertyInfo? ResolveNameProperty()
    {
        // The metadata Type is determined by the SDK once the field is non-null on a real state
        // instance. We probe via the FieldInfo's FieldType, which the SDK publishes statically.
        Type? metadataType = MetadataField?.FieldType;
        return metadataType?.GetProperty("Name", BindingFlags.Instance | BindingFlags.Public);
    }

    [LoggerMessage(EventId = 9171, Level = LogLevel.Warning, Message = "Replay-safety gate: {InFlightCount} in-flight IngestionWorkflow instance(s) detected — delaying startup (event 9171).")]
    private static partial void LogDraining(ILogger logger, int inFlightCount);

    [LoggerMessage(Level = LogLevel.Information, Message = "Replay-safety gate: no in-flight IngestionWorkflow instances — proceeding.")]
    private static partial void LogDrained(ILogger logger);

    [LoggerMessage(EventId = 9172, Level = LogLevel.Critical, Message = "Replay-safety gate: timed out with {RemainingCount} instance(s) still active — proceeding anyway (event 9172).")]
    private static partial void LogDrainTimeout(ILogger logger, int remainingCount);

    [LoggerMessage(EventId = 9173, Level = LogLevel.Critical, Message = "Replay-safety gate failing open (event 9173): reason={Reason}. Operator action: verify DAPR sidecar health and that the Dapr.Workflow SDK still exposes WorkflowMetadata.Name via WorkflowState._metadata.")]
    private static partial void LogGateFailedOpen(ILogger logger, string reason, Exception? exception);

    // S6-D2 follow-up (code review 2026-04-25): per-instance Warning when reflection cannot read
    // the workflow name. The instance is counted as in-flight so the gate cannot false-drain while
    // a potentially version-sensitive IngestionWorkflow is active.
    [LoggerMessage(EventId = 9175, Level = LogLevel.Warning, Message = "Replay-safety gate: workflow instance '{InstanceId}' has unreadable name; treating it as in-flight until it clears or the gate times out.")]
    private static partial void LogWorkflowNameUnreadable(ILogger logger, string instanceId);
}
