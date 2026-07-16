// <copyright file="IsStubBackfillMigrationHostedService.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Hosting;

using Hexalith.Memories.Contracts.V1;
using Hexalith.Memories.Server.Migrations;
using Hexalith.Memories.Server.Tenants;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

/// <summary>Story 9.2 Review D3 — background runner that invokes <see cref="IsStubBackfillMigration"/>
/// once per tenant at startup. The migration itself is gated by a per-graph
/// <c>(:SchemaMigration {id: "9.2-isStub-backfill"})</c> node so repeated runs are no-ops.
///
/// Without this wrapper the migration exists but never executes, leaving the
/// <see cref="Graph.GraphTraversalService"/> content-absent fallback permanently load-bearing
/// (Review Finding D3 in the Story 9.2 adversarial code review).</summary>
public sealed partial class IsStubBackfillMigrationHostedService : BackgroundService
{
    private static readonly TimeSpan StartupRetryDelay = TimeSpan.FromSeconds(30);

    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<IsStubBackfillMigrationHostedService> _logger;

    public IsStubBackfillMigrationHostedService(
        IServiceProvider serviceProvider,
        ILogger<IsStubBackfillMigrationHostedService> logger)
    {
        ArgumentNullException.ThrowIfNull(serviceProvider);
        ArgumentNullException.ThrowIfNull(logger);
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    /// <inheritdoc/>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await using AsyncServiceScope scope = _serviceProvider.CreateAsyncScope();
                TenantRegistryService registry = scope.ServiceProvider.GetRequiredService<TenantRegistryService>();
                IsStubBackfillMigration migration = scope.ServiceProvider.GetRequiredService<IsStubBackfillMigration>();

                IReadOnlyList<TenantInfo> tenants = await registry.ListTenantsAsync(stoppingToken).ConfigureAwait(false);
                LogStarting(_logger, tenants.Count);

                long total = 0;
                int successTenants = 0;
                foreach (TenantInfo tenant in tenants)
                {
                    stoppingToken.ThrowIfCancellationRequested();
                    try
                    {
                        long backfilled = await migration.RunAsync(tenant.Id, stoppingToken).ConfigureAwait(false);
                        total += backfilled;
                        successTenants++;
                    }
                    catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                    {
                        throw;
                    }
                    catch (Exception ex) when (ex is not OutOfMemoryException and not StackOverflowException)
                    {
                        LogTenantFailed(_logger, tenant.Id, ex);
                    }
                }

                LogCompleted(_logger, successTenants, tenants.Count, total);
                return;
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                // Service shutdown during enumeration — exit cleanly.
                return;
            }
            catch (Exception ex) when (ex is not OutOfMemoryException and not StackOverflowException)
            {
                // Migration is best-effort at startup; do not crash the host if tenant registry is
                // momentarily unreachable. Orphan-stub promotion failures are captured by
                // GraphTraversalService's content-absent fallback until the next successful retry.
                LogStartupFailed(_logger, ex, (int)StartupRetryDelay.TotalSeconds);
                await Task.Delay(StartupRetryDelay, stoppingToken).ConfigureAwait(false);
            }
        }
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "IsStubBackfillMigration starting — sweeping {TenantCount} tenant graph(s).")]
    private static partial void LogStarting(ILogger logger, int tenantCount);

    [LoggerMessage(Level = LogLevel.Information, Message = "IsStubBackfillMigration completed — {SuccessCount}/{TenantCount} tenants processed, {TotalBackfilled} nodes backfilled.")]
    private static partial void LogCompleted(ILogger logger, int successCount, int tenantCount, long totalBackfilled);

    [LoggerMessage(Level = LogLevel.Warning, Message = "IsStubBackfillMigration failed for tenant {TenantId} — fallback gap-marker detection remains active for this graph.")]
    private static partial void LogTenantFailed(ILogger logger, string tenantId, Exception exception);

    [LoggerMessage(Level = LogLevel.Error, Message = "IsStubBackfillMigration startup enumeration failed — retrying in {RetryDelaySeconds}s.")]
    private static partial void LogStartupFailed(ILogger logger, Exception exception, int retryDelaySeconds);
}
