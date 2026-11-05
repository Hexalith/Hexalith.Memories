// <copyright file="DaprIngestionWorkflowScheduler.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Ingestion;

using Dapr.Workflow;

using Hexalith.Memories.Contracts.V1;
using Hexalith.Memories.Server.Activities.Ingestion;
using Hexalith.Memories.Server.Workflows;

using Microsoft.Extensions.Logging;

/// <summary>DAPR-backed implementation of <see cref="IIngestionWorkflowScheduler"/>.</summary>
internal sealed partial class DaprIngestionWorkflowScheduler : IIngestionWorkflowScheduler
{
    private readonly SourceInventoryStore? _sourceInventory;
    private readonly RefreshPublicationStore? _refreshPublication;
    private readonly IDaprWorkflowClient _workflowClient;
    private readonly IWorkflowPayloadStore _payloadStore;
    private readonly IngestionWorkflowConfigurationCapture _workflowConfigurationCapture;
    private readonly WorkflowTraceContextCapture _workflowTraceContextCapture;
    private readonly IIngestionWorkflowInFlightRegistry _inFlightRegistry;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<DaprIngestionWorkflowScheduler> _logger;

    public DaprIngestionWorkflowScheduler(
        IDaprWorkflowClient workflowClient,
        IWorkflowPayloadStore payloadStore,
        IngestionWorkflowConfigurationCapture workflowConfigurationCapture,
        WorkflowTraceContextCapture workflowTraceContextCapture,
        IIngestionWorkflowInFlightRegistry inFlightRegistry,
        TimeProvider timeProvider,
        ILogger<DaprIngestionWorkflowScheduler> logger,
        SourceInventoryStore? sourceInventory = null,
        RefreshPublicationStore? refreshPublication = null)
    {
        ArgumentNullException.ThrowIfNull(workflowClient);
        ArgumentNullException.ThrowIfNull(payloadStore);
        ArgumentNullException.ThrowIfNull(workflowConfigurationCapture);
        ArgumentNullException.ThrowIfNull(workflowTraceContextCapture);
        ArgumentNullException.ThrowIfNull(inFlightRegistry);
        ArgumentNullException.ThrowIfNull(timeProvider);
        ArgumentNullException.ThrowIfNull(logger);
        _sourceInventory = sourceInventory;
        _refreshPublication = refreshPublication;
        _workflowClient = workflowClient;
        _payloadStore = payloadStore;
        _workflowConfigurationCapture = workflowConfigurationCapture;
        _workflowTraceContextCapture = workflowTraceContextCapture;
        _inFlightRegistry = inFlightRegistry;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    public Task<string> ScheduleAsync(string instanceId, IngestionInput input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        if (input.FailedRetry is not null) { throw new ArgumentException("failed-retry-context-is-scheduler-owned", nameof(input)); }
        return ScheduleCoreAsync(instanceId, input, null, cancellationToken);
    }

    public async Task<string> ScheduleFailedRetryAsync(FailedUnitRecord record, IngestionInput input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(record);
        ArgumentNullException.ThrowIfNull(input);
        if (_sourceInventory is null || input.FailedRetry is not null || input.RefreshExistingSource
            || input.TenantId != record.TenantId || input.CaseId != record.CaseId || input.SourceUri != record.SourceUri
            || input.SourceType != record.SourceType || input.PayloadReference != record.SourcePayloadReference)
        { throw new InvalidOperationException("failed-retry-source-mismatch"); }
        SourceInventoryEntry previous = await _sourceInventory.FindFailedRetryAsync(record, cancellationToken).ConfigureAwait(false);
        WorkflowState? state = await _workflowClient.GetWorkflowStateAsync(previous.WorkflowInstanceId!, true, cancellationToken).ConfigureAwait(false);
        IngestionInput? stored = state?.Exists == true ? state.ReadInputAs<IngestionInput>() : null;
        if (state?.RuntimeStatus != WorkflowRuntimeStatus.Failed || state.WorkflowName != nameof(IngestionWorkflow) || stored is null
            || stored.TenantId != record.TenantId || stored.CaseId != record.CaseId || stored.SourceUri != record.SourceUri
            || stored.RefreshExistingSource || stored.SourceType != record.SourceType || stored.PayloadReference != record.SourcePayloadReference
            || (stored.FailedRetry is { } retry && retry.MemoryUnitId != record.MemoryUnitId))
        { throw new InvalidOperationException("failed-retry-workflow-proof-mismatch"); }
        string attemptId = "retry-" + Guid.NewGuid().ToString("N");
        return await ScheduleCoreAsync(attemptId, input with
        { FailedRetry = new(previous.WorkflowInstanceId!, record.MemoryUnitId) }, previous, cancellationToken).ConfigureAwait(false);
    }

    private async Task<string> ScheduleCoreAsync(string instanceId, IngestionInput input, SourceInventoryEntry? previous, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(instanceId);
        ArgumentNullException.ThrowIfNull(input);

        IngestionIdentityGuard.Input(input, instanceId);
        input = await IngestionPayloadClaimCheck.NormalizeSourceAsync(_payloadStore, input, cancellationToken).ConfigureAwait(false);
        if (input.RefreshExistingSource)
        {
            IngestionInputValidator.Validate(input, requireInlineRefreshContent: true);
            if (_refreshPublication is null) { throw new InvalidOperationException("source-refresh-unavailable"); }
            input = input with { RefreshIdentityDigest = await _refreshPublication.PrepareAsync(input, cancellationToken).ConfigureAwait(false) };
        }
        if (_sourceInventory is not null)
        {
            if (previous is null) { await _sourceInventory.AdmitAsync(instanceId, input, cancellationToken).ConfigureAwait(false); }
            else { await _sourceInventory.AdmitFailedRetryAsync(instanceId, input, previous, cancellationToken).ConfigureAwait(false); }
            input = input with { SourceInventoryTracked = true };
        }
        IngestionInput slimInput;
        try
        {
            slimInput = await PrepareInputAsync(
                    _payloadStore,
                    input.FailedRetry?.MemoryUnitId ?? instanceId,
                    input,
                    _workflowConfigurationCapture,
                    _workflowTraceContextCapture,
                    cancellationToken)
                .ConfigureAwait(false);

            IngestionWorkflowInFlightEntry trackedEntry = new(input.TenantId, instanceId, _timeProvider.GetUtcNow());
            await _inFlightRegistry
                .TrackAsync(trackedEntry, cancellationToken)
                .ConfigureAwait(false);

        }
        catch
        {
            if (_sourceInventory is not null)
            {
                using var proofBudget = new CancellationTokenSource(TimeSpan.FromSeconds(5));
                await _sourceInventory.SetSchedulingAsync(input.TenantId, input.SourceUri, instanceId, "Unscheduled", proofBudget.Token).ConfigureAwait(false);
            }
            throw;
        }
        if (_sourceInventory is not null)
        {
            await _sourceInventory.SetSchedulingAsync(input.TenantId, input.SourceUri, instanceId, "Scheduling", cancellationToken).ConfigureAwait(false);
        }
        try
        {
            string accepted = await _workflowClient.ScheduleNewWorkflowAsync(nameof(IngestionWorkflow), instanceId, slimInput, null, cancellationToken).ConfigureAwait(false);
            if (accepted != instanceId) { throw new InvalidOperationException("source-inventory-scheduler-identity-mismatch"); }
            if (_sourceInventory is not null)
            {
                await _sourceInventory.SetSchedulingAsync(input.TenantId, input.SourceUri, instanceId, "Scheduled", cancellationToken).ConfigureAwait(false);
            }
            return accepted;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            try
            {
                await _inFlightRegistry.RemoveAsync(instanceId, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception removeEx) when (removeEx is not OperationCanceledException)
            {
                LogInFlightTrackingCleanupFailed(_logger, removeEx, input.TenantId, instanceId);
            }

            LogWorkflowSchedulingFailed(_logger, ex, input.TenantId, instanceId);
            throw;
        }
    }

    internal static Task<IngestionInput> PrepareInputAsync(
        IWorkflowPayloadStore payloadStore,
        string instanceId,
        IngestionInput input,
        IngestionWorkflowConfigurationCapture workflowConfigurationCapture,
        WorkflowTraceContextCapture workflowTraceContextCapture,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(workflowConfigurationCapture);
        ArgumentNullException.ThrowIfNull(workflowTraceContextCapture);
        IngestionInput configuredInput = workflowTraceContextCapture.Apply(workflowConfigurationCapture.Apply(input));
        return IngestionPayloadClaimCheck.PrepareAsync(payloadStore, instanceId, configuredInput, cancellationToken);
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "Failed to remove unscheduled ingestion workflow {InstanceId} for tenant {TenantId} from the in-flight registry after schedule failure.")]
    private static partial void LogInFlightTrackingCleanupFailed(ILogger logger, Exception exception, string tenantId, string instanceId);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Failed to schedule tracked ingestion workflow {InstanceId} for tenant {TenantId}.")]
    private static partial void LogWorkflowSchedulingFailed(ILogger logger, Exception exception, string tenantId, string instanceId);
}
