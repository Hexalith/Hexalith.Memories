// <copyright file="DaprIngestionWorkflowScheduler.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Ingestion;

using Dapr.Workflow;

using Hexalith.Memories.Contracts.V1;
using Hexalith.Memories.Server.Workflows;

using Microsoft.Extensions.Logging;

/// <summary>DAPR-backed implementation of <see cref="IIngestionWorkflowScheduler"/>.</summary>
internal sealed partial class DaprIngestionWorkflowScheduler : IIngestionWorkflowScheduler
{
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
        ILogger<DaprIngestionWorkflowScheduler> logger)
    {
        ArgumentNullException.ThrowIfNull(workflowClient);
        ArgumentNullException.ThrowIfNull(payloadStore);
        ArgumentNullException.ThrowIfNull(workflowConfigurationCapture);
        ArgumentNullException.ThrowIfNull(workflowTraceContextCapture);
        ArgumentNullException.ThrowIfNull(inFlightRegistry);
        ArgumentNullException.ThrowIfNull(timeProvider);
        ArgumentNullException.ThrowIfNull(logger);
        _workflowClient = workflowClient;
        _payloadStore = payloadStore;
        _workflowConfigurationCapture = workflowConfigurationCapture;
        _workflowTraceContextCapture = workflowTraceContextCapture;
        _inFlightRegistry = inFlightRegistry;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    public async Task<string> ScheduleAsync(string instanceId, IngestionInput input, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(instanceId);
        ArgumentNullException.ThrowIfNull(input);

        IngestionInput slimInput = await PrepareInputAsync(
                _payloadStore,
                instanceId,
                input,
                _workflowConfigurationCapture,
                _workflowTraceContextCapture,
                cancellationToken)
            .ConfigureAwait(false);

        IngestionWorkflowInFlightEntry trackedEntry = new(input.TenantId, instanceId, _timeProvider.GetUtcNow());
        await _inFlightRegistry
            .TrackAsync(trackedEntry, cancellationToken)
            .ConfigureAwait(false);

        try
        {
            return await _workflowClient
                .ScheduleNewWorkflowAsync(nameof(IngestionWorkflow), instanceId, slimInput, null, cancellationToken)
                .ConfigureAwait(false);
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
