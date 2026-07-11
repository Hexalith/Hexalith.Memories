// <copyright file="HandlerRegistryService.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Handlers;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Hexalith.Memories.Contracts.V1;
using Hexalith.Memories.EventStore;
using Hexalith.Memories.Server.Tenants;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

/// <summary>Story 9.3 — read-only handler registry service backing <c>GET /api/v1/handlers</c>. Returns
/// ONE <see cref="HandlerRegistration"/> per <c>SourceToTenantMap</c> entry whose routed tenant is
/// active (not Deleting/Deleted/NotFound).</summary>
/// <remarks>Graceful per-tenant degradation (Finding S): individual tenant observation-store reads are
/// wrapped in try/catch so ONE failing tenant produces a single row with
/// <c>Error = "OBSERVATION_READ_FAILED"</c> instead of a 500 that hides every healthy row.</remarks>
public sealed class HandlerRegistryService
{
    /// <summary>Hardcoded observation window width. See Story 9.3 deferred-work
    /// <c>Story-9.3-ObservationWindowConfig</c> for the config-extensibility follow-up.</summary>
    public static readonly TimeSpan ObservationWindow = TimeSpan.FromHours(24);

    /// <summary>Startup grace window for the <see cref="HandlerSubscriptionStatus"/> inference: for the
    /// first 2 minutes of process uptime we report <c>Unknown</c> rather than <c>Active</c> when no
    /// traffic has been seen yet.</summary>
    public static readonly TimeSpan StartupGraceWindow = TimeSpan.FromMinutes(2);

    /// <summary>Per-tenant tenant-status probe timeout (R3-9 hardening against a hanging registry).</summary>
    public static readonly TimeSpan TenantStatusProbeTimeout = TimeSpan.FromMilliseconds(500);

    private readonly IOptionsMonitor<TenantEventRoutingOptions> _routingOptions;
    private readonly IObservedEventTypeStore _observedEventTypeStore;
    private readonly TenantRegistryService _tenantRegistry;
    private readonly TimeProvider _timeProvider;
    private readonly ProcessLifetimeClock _processLifetimeClock;
    private readonly ILogger<HandlerRegistryService> _logger;

    public HandlerRegistryService(
        IOptionsMonitor<TenantEventRoutingOptions> routingOptions,
        IObservedEventTypeStore observedEventTypeStore,
        TenantRegistryService tenantRegistry,
        TimeProvider timeProvider,
        ProcessLifetimeClock processLifetimeClock,
        ILogger<HandlerRegistryService> logger)
    {
        ArgumentNullException.ThrowIfNull(routingOptions);
        ArgumentNullException.ThrowIfNull(observedEventTypeStore);
        ArgumentNullException.ThrowIfNull(tenantRegistry);
        ArgumentNullException.ThrowIfNull(timeProvider);
        ArgumentNullException.ThrowIfNull(processLifetimeClock);
        ArgumentNullException.ThrowIfNull(logger);

        _routingOptions = routingOptions;
        _observedEventTypeStore = observedEventTypeStore;
        _tenantRegistry = tenantRegistry;
        _timeProvider = timeProvider;
        _processLifetimeClock = processLifetimeClock;
        _logger = logger;
    }

    /// <summary>Returns a point-in-time <see cref="HandlerRegistrationSnapshot"/>.</summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task<HandlerRegistrationSnapshot> GetSnapshotAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        TenantEventRoutingOptions options = _routingOptions.CurrentValue;
        string asOf = _timeProvider.GetUtcNow().ToString("O");

        if (string.IsNullOrWhiteSpace(options.Topic))
        {
            return new HandlerRegistrationSnapshot
            {
                PubSubName = options.PubSubName,
                Topic = string.Empty,
                AsOf = asOf,
                SubscriptionStatus = HandlerSubscriptionStatus.Disabled,
                Handlers = Array.Empty<HandlerRegistration>(),
            };
        }

        // Group map entries by tenant so we can fan one Redis read per tenant, not per prefix.
        List<IGrouping<string, KeyValuePair<string, string>>> tenantGroups = options.SourceToTenantMap
            .GroupBy(kvp => kvp.Value, StringComparer.Ordinal)
            .ToList();

        Task<IReadOnlyList<HandlerRegistration>>[] tenantTasks = tenantGroups
            .Select(group => BuildRegistrationsForTenantAsync(group, cancellationToken))
            .ToArray();

        IReadOnlyList<HandlerRegistration>[] perTenantResults = await Task.WhenAll(tenantTasks)
            .ConfigureAwait(false);

        List<HandlerRegistration> allHandlers = perTenantResults
            .SelectMany(r => r)
            .ToList();

        bool anyObservationRecorded = allHandlers.Any(h => h.EventsProcessedCount > 0);
        HandlerSubscriptionStatus status = InferSubscriptionStatus(options, anyObservationRecorded);

        EventStoreIntegrationLog.HandlerRegistrySnapshotServed(_logger, allHandlers.Count);

        return new HandlerRegistrationSnapshot
        {
            PubSubName = options.PubSubName,
            Topic = options.Topic,
            AsOf = asOf,
            SubscriptionStatus = status,
            Handlers = allHandlers,
        };
    }

    private HandlerSubscriptionStatus InferSubscriptionStatus(
        TenantEventRoutingOptions options,
        bool anyObservationRecorded)
    {
        if (options.SourceToTenantMap.Count == 0)
        {
            return HandlerSubscriptionStatus.Unknown;
        }

        TimeSpan uptime = _processLifetimeClock.GetUptime();
        return uptime < StartupGraceWindow && !anyObservationRecorded
            ? HandlerSubscriptionStatus.Unknown
            : HandlerSubscriptionStatus.Active;
    }

    private async Task<IReadOnlyList<HandlerRegistration>> BuildRegistrationsForTenantAsync(
        IGrouping<string, KeyValuePair<string, string>> tenantGroup,
        CancellationToken outerCancellation)
    {
        string tenantId = tenantGroup.Key;

        // Per-tenant try-catch (Finding S) — one bad tenant should NOT 500 the whole endpoint.
        try
        {
            using var probeCts = CancellationTokenSource.CreateLinkedTokenSource(outerCancellation);
            probeCts.CancelAfter(TenantStatusProbeTimeout);

            TenantInfo? tenant;
            try
            {
                tenant = await _tenantRegistry.GetTenantAsync(tenantId, probeCts.Token).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (probeCts.IsCancellationRequested && !outerCancellation.IsCancellationRequested)
            {
                // R3-9 — tenant-registry probe hung; surface a specific error row and skip the Redis read.
                return tenantGroup
                    .Select(entry => BuildErrorRegistration(tenantId, entry.Key, "TENANT_STATUS_CHECK_FAILED"))
                    .ToList();
            }

            if (tenant is null || tenant.Status == TenantStatus.Deleting)
            {
                // Excluded from output — not-found or deleting tenants' handlers should not surface.
                return Array.Empty<HandlerRegistration>();
            }

            IReadOnlyList<ObservedEventType> observedTypes = await _observedEventTypeStore
                .GetAllObservedTypesAsync(tenantId, ObservationWindow, outerCancellation)
                .ConfigureAwait(false);

            List<HandlerRegistration> rows = new(tenantGroup.Count());
            foreach (KeyValuePair<string, string> entry in tenantGroup)
            {
                rows.Add(BuildRegistration(tenantId, entry.Key, observedTypes));
            }

            return rows;
        }
        catch (Exception ex) when (
            ex is StackExchange.Redis.RedisException
                or TimeoutException
                or System.IO.IOException)
        {
            EventStoreIntegrationLog.TenantObservationReadFailed(_logger, tenantId, ex.GetType().Name);
            return tenantGroup
                .Select(entry => BuildErrorRegistration(tenantId, entry.Key, "OBSERVATION_READ_FAILED"))
                .ToList();
        }
    }

    private static HandlerRegistration BuildRegistration(
        string tenantId,
        string sourcePrefix,
        IReadOnlyList<ObservedEventType> observedTypes)
    {
        long totalCount = observedTypes.Sum(o => o.Count);
        DateTimeOffset? lastSeen = observedTypes.Count == 0
            ? null
            : observedTypes.Max(o => o.LastSeenAt);

        List<string> distinctAggregateTypes = observedTypes
            .Select(o => o.AggregateType)
            .Distinct(StringComparer.Ordinal)
            .ToList();

        List<ObservedEventTypeSummary> summaries = observedTypes
            .Select(o => new ObservedEventTypeSummary
            {
                AggregateType = o.AggregateType,
                EventType = o.EventType,
                Count = o.Count,
                LastSeenAt = o.LastSeenAt.ToString("O"),
            })
            .ToList();

        return new HandlerRegistration
        {
            TenantId = tenantId,
            SourcePrefix = sourcePrefix,
            EventTypePatterns = distinctAggregateTypes, // [] when empty — Fix #3 data purity
            EventsProcessedCount = totalCount,
            LastEventAt = lastSeen?.ToString("O"),
            ObservedEventTypes = summaries,
            Error = null,
        };
    }

    private static HandlerRegistration BuildErrorRegistration(
        string tenantId,
        string sourcePrefix,
        string errorCode) => new()
        {
            TenantId = tenantId,
            SourcePrefix = sourcePrefix,
            EventTypePatterns = Array.Empty<string>(),
            EventsProcessedCount = 0,
            LastEventAt = null,
            ObservedEventTypes = Array.Empty<ObservedEventTypeSummary>(),
            Error = errorCode,
        };
}
