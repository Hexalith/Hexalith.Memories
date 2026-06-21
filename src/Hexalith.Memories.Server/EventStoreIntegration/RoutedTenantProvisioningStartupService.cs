// <copyright file="RoutedTenantProvisioningStartupService.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.EventStoreIntegration;

using Dapr;
using Dapr.Workflow;

using Hexalith.Memories.Contracts.V1;
using Hexalith.Memories.EventStore;
using Hexalith.Memories.Server.Tenants;
using Hexalith.Memories.Server.Workflows;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

/// <summary>Opt-in background service that provisions the routed (index) tenants in
/// <see cref="TenantEventRoutingOptions.SourceToTenantMap"/> when
/// <see cref="TenantEventRoutingOptions.AutoProvisionRoutedTenants"/> is set.
///
/// <para>A routing target (e.g. <c>tenants-index</c>) must be a provisioned/Active tenant or the router
/// drops its events as <c>TenantNotFound</c>. Production registers index tenants explicitly, but a
/// single-process host that owns a well-known curated index (the Tenants AppHost) opts in so the index
/// becomes Active automatically.</para>
///
/// <para>Runs as a <see cref="BackgroundService"/> so the provisioning loop executes AFTER the host has
/// started — never on the startup critical path. The per-tenant work waits (with timeouts) for DAPR
/// readiness and for the tenant to reach Active, which can take minutes; doing that in
/// <c>IHostedService.StartAsync</c> would block <c>ApplicationStarted</c>, later hosted services, and any
/// consumer gated on this host's readiness. Failures are logged and non-fatal — DAPR redelivery plus the
/// producer's upsert republish recover any events that arrive during the provisioning window.</para>
/// </summary>
internal sealed partial class RoutedTenantProvisioningStartupService : BackgroundService
{
    private static readonly TimeSpan DaprReadinessRetryDelay = TimeSpan.FromSeconds(5);
    private static readonly TimeSpan DaprReadinessTimeout = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan ActivePollInterval = TimeSpan.FromSeconds(2);
    private static readonly TimeSpan ActiveTimeout = TimeSpan.FromMinutes(3);

    private readonly IOptionsMonitor<TenantEventRoutingOptions> _options;
    private readonly TenantRegistryService _registry;
    private readonly DaprWorkflowClient _workflowClient;
    private readonly ILogger<RoutedTenantProvisioningStartupService> _logger;

    public RoutedTenantProvisioningStartupService(
        IOptionsMonitor<TenantEventRoutingOptions> options,
        TenantRegistryService registry,
        DaprWorkflowClient workflowClient,
        ILogger<RoutedTenantProvisioningStartupService> logger)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(registry);
        ArgumentNullException.ThrowIfNull(workflowClient);
        ArgumentNullException.ThrowIfNull(logger);
        _options = options;
        _registry = registry;
        _workflowClient = workflowClient;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        TenantEventRoutingOptions options = _options.CurrentValue;
        if (!options.AutoProvisionRoutedTenants || options.SourceToTenantMap.Count == 0)
        {
            return;
        }

        foreach (string tenantId in options.SourceToTenantMap.Values.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            try
            {
                await EnsureProvisionedAsync(tenantId, stoppingToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                return;
            }
            catch (Exception ex)
            {
                LogSeedFailed(_logger, tenantId, ex.GetType().Name, ex.Message);
            }
        }
    }

    private async Task EnsureProvisionedAsync(string tenantId, CancellationToken cancellationToken)
    {
        if (await _registry.TenantExistsAsync(tenantId, cancellationToken).ConfigureAwait(false))
        {
            LogAlreadyRegistered(_logger, tenantId);
            await WaitForActiveAsync(tenantId, cancellationToken).ConfigureAwait(false);
            return;
        }

        string instanceId = $"provision-{tenantId}-{Guid.NewGuid():N}";
        TenantProvisioningInput input = new(tenantId, tenantId);
        await ScheduleWithDaprReadinessAsync(instanceId, input, cancellationToken).ConfigureAwait(false);
        LogSeedScheduled(_logger, tenantId, instanceId);
        await WaitForActiveAsync(tenantId, cancellationToken).ConfigureAwait(false);
    }

    private async Task ScheduleWithDaprReadinessAsync(
        string instanceId,
        TenantProvisioningInput input,
        CancellationToken cancellationToken)
    {
        DateTimeOffset deadline = DateTimeOffset.UtcNow.Add(DaprReadinessTimeout);
        while (true)
        {
            try
            {
                await _workflowClient
                    .ScheduleNewWorkflowAsync(nameof(TenantProvisioningWorkflow), instanceId, input)
                    .ConfigureAwait(false);
                return;
            }
            catch (DaprException) when (!cancellationToken.IsCancellationRequested && DateTimeOffset.UtcNow < deadline)
            {
                LogDaprNotReady(_logger, (int)DaprReadinessRetryDelay.TotalSeconds);
                await Task.Delay(DaprReadinessRetryDelay, cancellationToken).ConfigureAwait(false);
            }
        }
    }

    private async Task WaitForActiveAsync(string tenantId, CancellationToken cancellationToken)
    {
        DateTimeOffset deadline = DateTimeOffset.UtcNow.Add(ActiveTimeout);
        while (DateTimeOffset.UtcNow < deadline)
        {
            cancellationToken.ThrowIfCancellationRequested();
            TenantInfo? tenant = await _registry.GetTenantAsync(tenantId, cancellationToken).ConfigureAwait(false);
            switch (tenant?.Status)
            {
                case TenantStatus.Active:
                    LogActive(_logger, tenantId);
                    return;
                case TenantStatus.Failed:
                case TenantStatus.CompensationFailed:
                    LogSeedFailed(_logger, tenantId, "ProvisioningState", tenant.Status.ToString());
                    return;
                default:
                    await Task.Delay(ActivePollInterval, cancellationToken).ConfigureAwait(false);
                    break;
            }
        }

        LogActiveTimeout(_logger, tenantId, (int)ActiveTimeout.TotalSeconds);
    }

    [LoggerMessage(EventId = 9192, Level = LogLevel.Information, Message = "Routed-tenant seed: tenant {TenantId} already registered.")]
    private static partial void LogAlreadyRegistered(ILogger logger, string tenantId);

    [LoggerMessage(EventId = 9193, Level = LogLevel.Information, Message = "Routed-tenant seed: provisioning scheduled for tenant {TenantId} (instance {InstanceId}).")]
    private static partial void LogSeedScheduled(ILogger logger, string tenantId, string instanceId);

    [LoggerMessage(EventId = 9194, Level = LogLevel.Information, Message = "Routed-tenant seed: tenant {TenantId} is Active.")]
    private static partial void LogActive(ILogger logger, string tenantId);

    [LoggerMessage(EventId = 9195, Level = LogLevel.Warning, Message = "Routed-tenant seed: tenant {TenantId} did not reach Active within {TimeoutSeconds}s; events may be dropped until it does.")]
    private static partial void LogActiveTimeout(ILogger logger, string tenantId, int timeoutSeconds);

    [LoggerMessage(EventId = 9196, Level = LogLevel.Warning, Message = "Routed-tenant seed: provisioning for tenant {TenantId} failed ({Reason}): {Detail}.")]
    private static partial void LogSeedFailed(ILogger logger, string tenantId, string reason, string detail);

    [LoggerMessage(EventId = 9197, Level = LogLevel.Information, Message = "Routed-tenant seed: DAPR not ready, retrying workflow scheduling in {RetryDelaySeconds}s.")]
    private static partial void LogDaprNotReady(ILogger logger, int retryDelaySeconds);
}
