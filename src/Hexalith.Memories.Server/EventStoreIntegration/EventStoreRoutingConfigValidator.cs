// <copyright file="EventStoreRoutingConfigValidator.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.EventStoreIntegration;

using Dapr;

using Hexalith.Memories.EventStore;
using Hexalith.Memories.Contracts.V1;
using Hexalith.Memories.Server.Tenants;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

/// <summary>Story 9.1 Task 3.6 — startup fail-fast validation for the EventStore routing configuration.
///
/// <para>Verifies every tenant id referenced by <see cref="TenantEventRoutingOptions.SourceToTenantMap"/>
/// actually exists in the tenant registry so obvious mis-configurations are caught at boot instead of at
/// first event arrival. Per the story Risk #4, this catches non-existent tenants but cannot prove routing
/// correctness (a <c>source</c> mapped to the <i>wrong-but-existing</i> tenant still passes validation);
/// it is intentionally one tool in the safety chain, not the whole chain.</para>
///
/// <para>If <see cref="TenantEventRoutingOptions.Topic"/> is empty or unset the validator is a no-op —
/// EventStore integration is opt-in and must not block startup for deployments that do not use it.</para>
/// </summary>
internal sealed partial class EventStoreRoutingConfigValidator : IHostedService
{
    private static readonly TimeSpan DaprReadinessRetryDelay = TimeSpan.FromSeconds(5);
    private static readonly TimeSpan DaprReadinessTimeout = TimeSpan.FromMinutes(5);

    private readonly IOptionsMonitor<TenantEventRoutingOptions> _options;
    private readonly TenantRegistryService _registry;
    private readonly IHostEnvironment _hostEnvironment;
    private readonly ILogger<EventStoreRoutingConfigValidator> _logger;

    public EventStoreRoutingConfigValidator(
        IOptionsMonitor<TenantEventRoutingOptions> options,
        TenantRegistryService registry,
        IHostEnvironment hostEnvironment,
        ILogger<EventStoreRoutingConfigValidator> logger)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(registry);
        ArgumentNullException.ThrowIfNull(hostEnvironment);
        ArgumentNullException.ThrowIfNull(logger);
        _options = options;
        _registry = registry;
        _hostEnvironment = hostEnvironment;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        DateTimeOffset deadline = DateTimeOffset.UtcNow.Add(DaprReadinessTimeout);

        while (true)
        {
            try
            {
                await ValidateAsync(cancellationToken).ConfigureAwait(false);
                return;
            }
            catch (DaprException ex) when (!cancellationToken.IsCancellationRequested && DateTimeOffset.UtcNow < deadline)
            {
                LogDaprNotReady(_logger, ex, (int)DaprReadinessRetryDelay.TotalSeconds);
                await Task.Delay(DaprReadinessRetryDelay, cancellationToken).ConfigureAwait(false);
            }
            catch (DaprException ex) when (!cancellationToken.IsCancellationRequested)
            {
                LogDaprNotReadyTimeout(_logger, ex, (int)DaprReadinessTimeout.TotalSeconds);
                throw;
            }
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    private async Task ValidateAsync(CancellationToken cancellationToken)
    {
        TenantEventRoutingOptions options = _options.CurrentValue;
        if (string.IsNullOrWhiteSpace(options.Topic))
        {
            LogNotConfigured(_logger);
            return;
        }

        if (options.SourceToTenantMap.Count == 0)
        {
            LogEmptyMap(_logger);
            return;
        }

        List<string> missing = [];
        foreach (KeyValuePair<string, string> mapping in options.SourceToTenantMap)
        {
            bool exists = await _registry.TenantExistsAsync(mapping.Value, cancellationToken).ConfigureAwait(false);
            if (!exists)
            {
                missing.Add($"{mapping.Key} -> {mapping.Value}");
            }
        }

        if (missing.Count > 0)
        {
            string joined = string.Join(", ", missing);

            // When auto-provisioning routed tenants is enabled, missing routed tenants are expected at
            // validation time — the RoutedTenantProvisioningStartupService creates them after startup — so
            // defer rather than fail-fast regardless of environment.
            if (options.AutoProvisionRoutedTenants || _hostEnvironment.IsDevelopment())
            {
                LogUnknownTenantsDeferred(_logger, joined);
                return;
            }

            LogUnknownTenants(_logger, joined);
            throw new InvalidOperationException(
                $"EventStore routing configuration references {missing.Count} unknown tenant(s): {joined}. "
                + $"Register the tenants with POST {MemoriesRoutes.Tenants} or remove them from EventStoreIntegration:Routing:SourceToTenantMap.");
        }

        LogValidated(_logger, options.SourceToTenantMap.Count);
    }

    [LoggerMessage(
        EventId = 9104,
        Level = LogLevel.Information,
        Message = "EventStore routing configuration validated: {MappingCount} source→tenant mapping(s) resolved.")]
    private static partial void LogValidated(ILogger logger, int mappingCount);

    [LoggerMessage(
        EventId = 9105,
        Level = LogLevel.Critical,
        Message = "EventStore routing configuration references unknown tenant(s): {MissingMappings}. Fail-fast.")]
    private static partial void LogUnknownTenants(ILogger logger, string missingMappings);

    [LoggerMessage(
        EventId = 9108,
        Level = LogLevel.Warning,
        Message = "EventStore routing configuration references unknown tenant(s): {MissingMappings}. Development mode will continue startup so the tenants can be provisioned later.")]
    private static partial void LogUnknownTenantsDeferred(ILogger logger, string missingMappings);

    [LoggerMessage(
        EventId = 9106,
        Level = LogLevel.Information,
        Message = "EventStore routing is not configured (EventStoreIntegration:Routing:Topic is empty). Subscription disabled.")]
    private static partial void LogNotConfigured(ILogger logger);

    [LoggerMessage(
        EventId = 9107,
        Level = LogLevel.Warning,
        Message = "EventStore routing configuration has no source-to-tenant mappings; all events will be dropped as unknown-source.")]
    private static partial void LogEmptyMap(ILogger logger);

    [LoggerMessage(
        EventId = 9109,
        Level = LogLevel.Warning,
        Message = "EventStore routing validation is waiting for DAPR state store readiness; retrying in {RetryDelaySeconds}s.")]
    private static partial void LogDaprNotReady(ILogger logger, Exception exception, int retryDelaySeconds);

    [LoggerMessage(
        EventId = 9110,
        Level = LogLevel.Critical,
        Message = "EventStore routing validation could not reach the DAPR state store within {TimeoutSeconds}s. Fail-fast.")]
    private static partial void LogDaprNotReadyTimeout(ILogger logger, Exception exception, int timeoutSeconds);
}
