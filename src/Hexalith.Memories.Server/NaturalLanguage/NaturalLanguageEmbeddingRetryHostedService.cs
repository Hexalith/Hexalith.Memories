// <copyright file="NaturalLanguageEmbeddingRetryHostedService.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.NaturalLanguage;

using System.Security.Cryptography;
using System.Text.Json;

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
    private readonly StackExchange.Redis.IDatabase? _history;
    private readonly IDaprWorkflowClient _workflowClient;
    private readonly IOptionsMonitor<NaturalLanguageDescriptionOptions> _options;
    private readonly ILogger<NaturalLanguageEmbeddingRetryHostedService> _logger;

    public NaturalLanguageEmbeddingRetryHostedService(
        IFailedNaturalLanguageEmbeddingRegistry registry,
        IDaprWorkflowClient workflowClient,
        IOptionsMonitor<NaturalLanguageDescriptionOptions> options,
        ILogger<NaturalLanguageEmbeddingRetryHostedService> logger)
        : this(registry, workflowClient, options, logger, null)
    {
    }

    /// <summary>Initializes the worker with durable discovery for registered adapters.</summary>
    /// <param name="registry">The retry registry.</param>
    /// <param name="workflowClient">The Dapr workflow client.</param>
    /// <param name="options">The retry configuration.</param>
    /// <param name="logger">The worker logger.</param>
    /// <param name="redis">The registered retry store.</param>
    [ActivatorUtilitiesConstructor]
    public NaturalLanguageEmbeddingRetryHostedService(IFailedNaturalLanguageEmbeddingRegistry registry,
        IDaprWorkflowClient workflowClient, IOptionsMonitor<NaturalLanguageDescriptionOptions> options,
        ILogger<NaturalLanguageEmbeddingRetryHostedService> logger,
        [FromKeyedServices("redis")] StackExchange.Redis.IConnectionMultiplexer? redis)
    {
        ArgumentNullException.ThrowIfNull(registry);
        ArgumentNullException.ThrowIfNull(workflowClient);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(logger);
        _registry = registry;
        _history = redis?.GetDatabase() ?? (registry as FailedNaturalLanguageEmbeddingRegistry)?.Database;
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
        if (!_options.CurrentValue.RetryWorkerEnabled)
        {
            return;
        }

        await foreach (string tenantId in _registry.ListTenantsWithBacklogAsync(cancellationToken).ConfigureAwait(false))
        {
            try
            {
                if (!_options.CurrentValue.RetryWorkerEnabled)
                {
                    return;
                }

                long backlog = await _registry.GetBacklogCountAsync(tenantId, cancellationToken).ConfigureAwait(false);
                if (!_options.CurrentValue.RetryWorkerEnabled)
                {
                    return;
                }
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
                    if (!_options.CurrentValue.RetryWorkerEnabled)
                    {
                        return;
                    }

                    await ScheduleRetryAsync(record, opts, cancellationToken).ConfigureAwait(false);
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
            catch (Exception error) when (error is InvalidOperationException or StackExchange.Redis.RedisServerException
                && error.Message.Contains("nl-retry-", StringComparison.Ordinal))
            {
                // The queue/lease remains intact; retain a content-free reconciliation obligation for this tenant.
                if (_history is not null)
                    await _history.StringSetAsync(RetryWorkflowHistory.Unresolved(tenantId), "1").WaitAsync(cancellationToken).ConfigureAwait(false);
                LogTenantReconciliationFailed(_logger, error, tenantId);
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
            string.Empty,
            record.EventType,
            record.AggregateType,
            record.CaseId,
            record.EmbeddingProvider,
            record.EmbeddingModel,
            record.EmbeddingDimensions) { PayloadVersion = FailedNaturalLanguageEmbeddingRegistry.PayloadVersion(record) };

        string instanceId = GetRetryWorkflowInstanceId(record.TenantId, record.MemoryUnitId);

        StackExchange.Redis.IDatabase? history = _history;
        string? authority = history is null ? null : await RetryWorkflowHistory.BeginAsync(history, record.TenantId, record.MemoryUnitId, cancellationToken).ConfigureAwait(false);
        if (history is not null && authority is null) return;
        try
        {
            if (history is not null && await FailedNaturalLanguageEmbeddingRegistry.ReadCurrentAsync(history, record.TenantId, record.MemoryUnitId).ConfigureAwait(false) != record)
            {
                await RetryWorkflowHistory.AcknowledgeAsync(history, record.TenantId, record.MemoryUnitId, authority!, cancellationToken).ConfigureAwait(false);
                return;
            }
            WorkflowState? prior = await TryGetWorkflowStateAsync(instanceId, cancellationToken).ConfigureAwait(false);
            if (prior is { Exists: true })
            {
                var scope = RetryWorkflowErasure.ReadScope(prior);
                if (scope.Tenant != record.TenantId || scope.Unit != record.MemoryUnitId) throw new InvalidOperationException("nl-retry-workflow-scope-mismatch");
                var priorInput = prior.ReadInputAs<NaturalLanguageEmbeddingRetryInput>();
                if (priorInput?.PayloadVersion != input.PayloadVersion)
                {
                    if (!IsTerminalStatus(prior.RuntimeStatus))
                    {
                        if (history is not null) await RetryWorkflowHistory.AcknowledgeAsync(history, record.TenantId, record.MemoryUnitId, authority!, cancellationToken).ConfigureAwait(false);
                        return;
                    }
                    // Registered schedulers serialize this address with a non-expiring per-attempt lease.
                    if (history is null) return;
                    if (await FailedNaturalLanguageEmbeddingRegistry.ReadCurrentAsync(history, record.TenantId, record.MemoryUnitId).ConfigureAwait(false) != record)
                    {
                        await RetryWorkflowHistory.AcknowledgeAsync(history, record.TenantId, record.MemoryUnitId, authority!, cancellationToken).ConfigureAwait(false);
                        return;
                    }
                    await RetryWorkflowErasure.PurgeScopedAsync(_workflowClient, record.TenantId, record.MemoryUnitId, prior, cancellationToken).ConfigureAwait(false);
                }
                else
                {
                    if (history is not null) await RetryWorkflowHistory.AcknowledgeAsync(history, record.TenantId, record.MemoryUnitId, authority!, cancellationToken).ConfigureAwait(false);
                    _ = await TryFinalizeRetryAsync(record, opts, prior, cancellationToken).ConfigureAwait(false);
                    return;
                }
            }
            string scheduledId = await _workflowClient.ScheduleNewWorkflowAsync(
                nameof(NaturalLanguageEmbeddingRetryWorkflow),
                instanceId,
                input).ConfigureAwait(false);
            if (scheduledId != instanceId) throw new InvalidOperationException("nl-retry-workflow-address-mismatch");
            if (history is not null) await RetryWorkflowHistory.AcknowledgeAsync(history, record.TenantId, record.MemoryUnitId, authority!, cancellationToken).ConfigureAwait(false);

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
            if (existingState is { Exists: true })
            {
                var scope = RetryWorkflowErasure.ReadScope(existingState);
                if (scope.Tenant != record.TenantId || scope.Unit != record.MemoryUnitId) throw new InvalidOperationException("nl-retry-workflow-scope-mismatch");
                if (history is not null) await RetryWorkflowHistory.AcknowledgeAsync(history, record.TenantId, record.MemoryUnitId, authority!, cancellationToken).ConfigureAwait(false);
            }
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
        // A JSON tuple preserves the tenant/unit boundary; the prefix is disjoint from legacy IDs.
        byte[] scope = JsonSerializer.SerializeToUtf8Bytes(new[] { tenantId, memoryUnitId });
        return "retry-nlv2-" + Convert.ToHexStringLower(SHA256.HashData(scope));
    }

    internal static string GetLegacyRetryWorkflowInstanceId(string tenantId, string memoryUnitId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        ArgumentException.ThrowIfNullOrWhiteSpace(memoryUnitId);
        return $"retry-nl-{tenantId}-{memoryUnitId}";
    }

    internal static bool IsTerminalStatus(WorkflowRuntimeStatus status)
        => status == WorkflowRuntimeStatus.Completed
            || status == WorkflowRuntimeStatus.Failed
            || status == WorkflowRuntimeStatus.Canceled
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

        var scope = RetryWorkflowErasure.ReadScope(state);
        if (scope.Tenant != record.TenantId || scope.Unit != record.MemoryUnitId)
            throw new InvalidOperationException("nl-retry-workflow-scope-mismatch");
        NaturalLanguageEmbeddingRetryInput? scheduled = state.ReadInputAs<NaturalLanguageEmbeddingRetryInput>();
        if (scheduled?.PayloadVersion != FailedNaturalLanguageEmbeddingRegistry.PayloadVersion(record)) return false;
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

    [LoggerMessage(Level = LogLevel.Warning, Message = "NL retry tenant {TenantId} requires reconciliation; other tenants remain eligible.")]
    private static partial void LogTenantReconciliationFailed(ILogger logger, Exception exception, string tenantId);

    [LoggerMessage(Level = LogLevel.Information, Message = "NaturalLanguageEmbeddingRetryHostedService started.")]
    private static partial void LogStarted(ILogger logger);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Retry tick failed.")]
    private static partial void LogTickFailed(ILogger logger, Exception exception);

    [LoggerMessage(EventId = 9170, Level = LogLevel.Warning, Message = "NL retry queue backlog for tenant {TenantId} is {Backlog} (event 9170).")]
    private static partial void LogBacklogWarning(ILogger logger, string tenantId, long backlog);

    [LoggerMessage(EventId = 9179, Level = LogLevel.Error, Message = "NL retry queue backlog for tenant {TenantId} is {Backlog} (event 9179, Error threshold).")]
    private static partial void LogBacklogError(ILogger logger, string tenantId, long backlog);

    [LoggerMessage(EventId = 9180, Level = LogLevel.Error, Message = "NL retry reached dead-letter for {MemoryUnitId} (tenant {TenantId}, attempts={Attempts}) (event 9180).")]
    private static partial void LogPermanentlyFailed(ILogger logger, string tenantId, string memoryUnitId, int attempts);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Failed to schedule NL retry workflow for {MemoryUnitId} (tenant {TenantId}).")]
    private static partial void LogScheduleFailed(ILogger logger, Exception exception, string tenantId, string memoryUnitId);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Failed to read NL retry workflow state for instance {InstanceId}.")]
    private static partial void LogStateReadFailed(ILogger logger, Exception exception, string instanceId);
}
