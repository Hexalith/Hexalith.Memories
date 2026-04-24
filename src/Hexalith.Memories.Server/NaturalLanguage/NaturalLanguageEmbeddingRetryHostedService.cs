// <copyright file="NaturalLanguageEmbeddingRetryHostedService.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.NaturalLanguage;

using Dapr.Workflow;

using Hexalith.Memories.Contracts.V1;
using Hexalith.Memories.Server.Workflows;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

/// <summary>Story 9.2 Task 8.5 — background service that drains
/// <c>nl-embedding-retry:{tenantId}</c> by scheduling <see cref="NaturalLanguageEmbeddingRetryWorkflow"/>
/// per record. The instance id is tenant-scoped so DAPR Workflow's instance-level dedup prevents
/// double-scheduling under restart without colliding across tenants (Task 8.6 idempotency guard).</summary>
public sealed partial class NaturalLanguageEmbeddingRetryHostedService : BackgroundService
{
    internal static readonly TimeSpan WorkflowCompletionWaitTimeout = TimeSpan.FromSeconds(30);

    private readonly IFailedNaturalLanguageEmbeddingRegistry _registry;
    private readonly DaprWorkflowClient _workflowClient;
    private readonly IOptionsMonitor<NaturalLanguageDescriptionOptions> _options;
    private readonly ILogger<NaturalLanguageEmbeddingRetryHostedService> _logger;

    public NaturalLanguageEmbeddingRetryHostedService(
        IFailedNaturalLanguageEmbeddingRegistry registry,
        DaprWorkflowClient workflowClient,
        IOptionsMonitor<NaturalLanguageDescriptionOptions> options,
        ILogger<NaturalLanguageEmbeddingRetryHostedService> logger)
    {
        ArgumentNullException.ThrowIfNull(registry);
        ArgumentNullException.ThrowIfNull(workflowClient);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(logger);
        _registry = registry;
        _workflowClient = workflowClient;
        _options = options;
        _logger = logger;
    }

    /// <inheritdoc/>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        LogStarted(_logger);
        while (!stoppingToken.IsCancellationRequested)
        {
            NaturalLanguageDescriptionOptions opts = _options.CurrentValue;
            TimeSpan interval = TimeSpan.FromSeconds(Math.Max(1, opts.RetryIntervalSeconds));

            try
            {
                await TickAsync(opts, stoppingToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                LogTickFailed(_logger, ex);
            }

            try
            {
                await Task.Delay(interval, stoppingToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }

    internal async Task TickAsync(NaturalLanguageDescriptionOptions opts, CancellationToken cancellationToken)
    {
        await foreach (string tenantId in _registry.ListTenantsWithBacklogAsync(cancellationToken).ConfigureAwait(false))
        {
            long backlog = await _registry.GetBacklogCountAsync(tenantId, cancellationToken).ConfigureAwait(false);
            if (backlog == 0)
            {
                continue;
            }

            if (backlog > 1000)
            {
                LogBacklogError(_logger, tenantId, backlog);
            }
            else if (backlog > 100)
            {
                LogBacklogWarning(_logger, tenantId, backlog);
            }

            IReadOnlyList<FailedNaturalLanguageEmbeddingRecord> batch = await _registry
                .DequeueBatchAsync(tenantId, opts.BatchSize, cancellationToken)
                .ConfigureAwait(false);

            foreach (FailedNaturalLanguageEmbeddingRecord record in batch)
            {
                await ScheduleRetryAsync(record, opts, cancellationToken).ConfigureAwait(false);
            }
        }
    }

    private async Task ScheduleRetryAsync(
        FailedNaturalLanguageEmbeddingRecord record,
        NaturalLanguageDescriptionOptions opts,
        CancellationToken cancellationToken)
    {
        NaturalLanguageEmbeddingRetryInput input = new(
            record.TenantId,
            record.MemoryUnitId,
            record.TruncatedRawJsonPayload,
            record.EventType,
            record.AggregateType,
            record.CaseId,
            record.EmbeddingProvider,
            record.EmbeddingModel,
            record.EmbeddingDimensions);

        string instanceId = GetRetryWorkflowInstanceId(record.TenantId, record.MemoryUnitId);

        try
        {
            string scheduledId = await _workflowClient.ScheduleNewWorkflowAsync(
                nameof(NaturalLanguageEmbeddingRetryWorkflow),
                instanceId,
                input).ConfigureAwait(false);

            WorkflowState? state;
            try
            {
                using CancellationTokenSource waitCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                waitCts.CancelAfter(WorkflowCompletionWaitTimeout);
                state = await _workflowClient
                    .WaitForWorkflowCompletionAsync(scheduledId, true, waitCts.Token)
                    .ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                state = await TryGetWorkflowStateAsync(instanceId, cancellationToken).ConfigureAwait(false);
            }

            _ = await TryFinalizeRetryAsync(record, opts, state, cancellationToken).ConfigureAwait(false);
        }
        catch (InvalidOperationException)
        {
            WorkflowState? existingState = await TryGetWorkflowStateAsync(instanceId, cancellationToken).ConfigureAwait(false);
            _ = await TryFinalizeRetryAsync(record, opts, existingState, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            LogScheduleFailed(_logger, ex, record.TenantId, record.MemoryUnitId);
        }
    }

    internal static string GetRetryWorkflowInstanceId(string tenantId, string memoryUnitId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        ArgumentException.ThrowIfNullOrWhiteSpace(memoryUnitId);
        return $"retry-nl-{tenantId}-{memoryUnitId}";
    }

    internal static bool IsTerminalStatus(WorkflowRuntimeStatus status)
        => status == WorkflowRuntimeStatus.Completed
            || status == WorkflowRuntimeStatus.Failed
            || status == WorkflowRuntimeStatus.Terminated;

    private async Task<WorkflowState?> TryGetWorkflowStateAsync(string instanceId, CancellationToken cancellationToken)
    {
        try
        {
            return await _workflowClient.GetWorkflowStateAsync(instanceId, true, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            LogStateReadFailed(_logger, ex, instanceId);
            return null;
        }
    }

    private async Task<bool> TryFinalizeRetryAsync(
        FailedNaturalLanguageEmbeddingRecord record,
        NaturalLanguageDescriptionOptions opts,
        WorkflowState? state,
        CancellationToken cancellationToken)
    {
        if (state is null || !state.Exists || !IsTerminalStatus(state.RuntimeStatus))
        {
            return false;
        }

        NaturalLanguageEmbeddingRetryResult? result = TryReadRetryResult(state);
        if (result is { Indexed: true })
        {
            await _registry.CompleteAsync(record, cancellationToken).ConfigureAwait(false);
            NaturalLanguageIntegrationLog.NaturalLanguageEmbeddingRetrySucceeded(
                _logger,
                record.TenantId,
                record.MemoryUnitId,
                record.Attempts + 1);
            return true;
        }

        bool dead = await _registry
            .IncrementAttemptsAsync(record, opts.MaxRetryAttempts, cancellationToken)
            .ConfigureAwait(false);

        if (dead)
        {
            LogPermanentlyFailed(_logger, record.TenantId, record.MemoryUnitId, record.Attempts + 1);
        }

        return true;
    }

    private static NaturalLanguageEmbeddingRetryResult? TryReadRetryResult(WorkflowState state)
    {
        try
        {
            return state.ReadOutputAs<NaturalLanguageEmbeddingRetryResult>();
        }
        catch (InvalidOperationException)
        {
            return null;
        }
        catch (FormatException)
        {
            return null;
        }
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "NaturalLanguageEmbeddingRetryHostedService started.")]
    private static partial void LogStarted(ILogger logger);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Retry tick failed.")]
    private static partial void LogTickFailed(ILogger logger, Exception exception);

    [LoggerMessage(EventId = 9170, Level = LogLevel.Warning, Message = "NL retry queue backlog for tenant {TenantId} is {Backlog} (event 9170).")]
    private static partial void LogBacklogWarning(ILogger logger, string tenantId, long backlog);

    [LoggerMessage(EventId = 9179, Level = LogLevel.Error, Message = "NL retry queue backlog for tenant {TenantId} is {Backlog} (event 9179, Error threshold).")]
    private static partial void LogBacklogError(ILogger logger, string tenantId, long backlog);

    [LoggerMessage(EventId = 9153, Level = LogLevel.Information, Message = "NL retry succeeded for {MemoryUnitId} (tenant {TenantId}, attempts={Attempts}) (event 9153).")]
    private static partial void LogRetrySucceeded(ILogger logger, string tenantId, string memoryUnitId, int attempts);

    [LoggerMessage(EventId = 9180, Level = LogLevel.Error, Message = "NL retry reached dead-letter for {MemoryUnitId} (tenant {TenantId}, attempts={Attempts}) (event 9180).")]
    private static partial void LogPermanentlyFailed(ILogger logger, string tenantId, string memoryUnitId, int attempts);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Failed to schedule NL retry workflow for {MemoryUnitId} (tenant {TenantId}).")]
    private static partial void LogScheduleFailed(ILogger logger, Exception exception, string tenantId, string memoryUnitId);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Failed to read NL retry workflow state for instance {InstanceId}.")]
    private static partial void LogStateReadFailed(ILogger logger, Exception exception, string instanceId);
}
