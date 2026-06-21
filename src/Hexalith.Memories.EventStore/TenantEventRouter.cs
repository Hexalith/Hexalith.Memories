// <copyright file="TenantEventRouter.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.EventStore;

using System.Collections.Concurrent;

using Microsoft.Extensions.Options;

/// <summary>Default <see cref="ITenantEventRouter"/>: resolves a CloudEvents envelope to a concrete
/// tenant + case + aggregate-type outcome via the configured <see cref="TenantEventRoutingOptions"/>.
///
/// <para>Routing pipeline (AC #7, #10, #11, #14a, #14b):</para>
/// <list type="number">
///   <item>Case-insensitive longest-prefix match of <c>source</c> against <see cref="TenantEventRoutingOptions.SourceToTenantMap"/>.</item>
///   <item>Tenant-status lookup via <see cref="ITenantStatusAccessor"/>.</item>
///   <item>Aggregate-type extraction via <see cref="AggregateTypeExtractor"/>.</item>
///   <item>Case resolution: per-tenant cache lookup → else auto-create via <see cref="ICaseCreationService"/>
///       (when enabled and under cap) → else typed drop outcome.</item>
/// </list>
///
/// <para>Concurrency: the <c>(tenantId, aggregateType) → caseId</c> cache is a lazy per-tenant
/// <see cref="ConcurrentDictionary{TKey, TValue}"/> with <see cref="Lazy{T}"/> values, guaranteeing that
/// concurrent first-time events for the same aggregate-type converge on a single case creation call.</para>
/// </summary>
internal sealed class TenantEventRouter : ITenantEventRouter
{
    private static readonly TimeSpan SharedCaseCreationLeaseTtl = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan SharedCaseCreationWaitTimeout = TimeSpan.FromSeconds(5);
    private static readonly TimeSpan SharedCaseCreationPollInterval = TimeSpan.FromMilliseconds(50);

    private readonly IOptionsMonitor<TenantEventRoutingOptions> _options;
    private readonly ITenantStatusAccessor _tenantStatus;
    private readonly ICaseCreationService _caseCreationService;
    private readonly IAggregateCaseMappingStore _caseMapStore;

    // tenantId → (aggregateType → caseId)
    private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, string>> _caseCache
        = new(StringComparer.Ordinal);

    public TenantEventRouter(
        IOptionsMonitor<TenantEventRoutingOptions> options,
        ITenantStatusAccessor tenantStatus,
        ICaseCreationService caseCreationService,
        IAggregateCaseMappingStore caseMapStore)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(tenantStatus);
        ArgumentNullException.ThrowIfNull(caseCreationService);
        ArgumentNullException.ThrowIfNull(caseMapStore);
        _options = options;
        _tenantStatus = tenantStatus;
        _caseCreationService = caseCreationService;
        _caseMapStore = caseMapStore;
    }

    public async Task<TenantEventRouteResolution> ResolveAsync(
        CloudEventEnvelope envelope,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(envelope);

        TenantEventRoutingOptions options = _options.CurrentValue;

        string? tenantId = MatchTenant(envelope.Source, options.SourceToTenantMap);
        if (tenantId is null)
        {
            return TenantEventRouteResolution.UnknownSource();
        }

        EventStoreTenantStatus? status = await _tenantStatus
            .GetStatusAsync(tenantId, cancellationToken)
            .ConfigureAwait(false);

        switch (status)
        {
            case null:
                return TenantEventRouteResolution.TenantNotFound(tenantId);
            case EventStoreTenantStatus.Provisioning:
                return TenantEventRouteResolution.TenantProvisioning(tenantId);
            case EventStoreTenantStatus.Deleting:
            case EventStoreTenantStatus.Unavailable:
                return TenantEventRouteResolution.TenantDeleting(tenantId);
            case EventStoreTenantStatus.Active:
                break;
            default:
                return TenantEventRouteResolution.TenantDeleting(tenantId);
        }

        string aggregateType = AggregateTypeExtractor.Extract(envelope.Type);

        // Curated search-index events write to a deterministic {tenantId}:mu:{aggregateId} key and never use a
        // case. Short-circuit here — after the authoritative tenant + status check — so they do not trigger
        // case auto-creation (which would create a spurious "events:SearchIndexEntryChanged" case, consume a
        // MaxAutoCreatedCasesPerTenant slot, and add a workflow + Redis round-trip on the first event).
        if (CuratedSearchIndexEventTypes.IsCuratedType(envelope.Type))
        {
            return TenantEventRouteResolution.Accepted(
                new TenantEventRoute(tenantId, string.Empty, aggregateType));
        }

        ConcurrentDictionary<string, string> tenantCache = _caseCache
            .GetOrAdd(tenantId, _ => new ConcurrentDictionary<string, string>(StringComparer.Ordinal));

        if (tenantCache.TryGetValue(aggregateType, out string? cachedCaseId))
        {
            return TenantEventRouteResolution.Accepted(
                new TenantEventRoute(tenantId, cachedCaseId, aggregateType));
        }

        string? persistedCaseId = await _caseMapStore
            .GetCaseIdAsync(tenantId, aggregateType, cancellationToken)
            .ConfigureAwait(false);
        if (!string.IsNullOrWhiteSpace(persistedCaseId))
        {
            tenantCache[aggregateType] = persistedCaseId;
            return TenantEventRouteResolution.Accepted(
                new TenantEventRoute(tenantId, persistedCaseId, aggregateType));
        }

        if (!options.AutoCreateCases)
        {
            return TenantEventRouteResolution.AutoCreateDisabled(tenantId);
        }

        if (await _caseMapStore.GetAggregateCountAsync(tenantId, cancellationToken).ConfigureAwait(false)
            >= options.MaxAutoCreatedCasesPerTenant)
        {
            return TenantEventRouteResolution.CaseCapExceeded(tenantId);
        }

        bool leaseAcquired = await _caseMapStore
            .TryAcquireCreationLockAsync(tenantId, aggregateType, SharedCaseCreationLeaseTtl, cancellationToken)
            .ConfigureAwait(false);
        if (!leaseAcquired)
        {
            (leaseAcquired, persistedCaseId) = await WaitForSharedCaseOrLeaseAsync(tenantId, aggregateType, cancellationToken)
                .ConfigureAwait(false);

            if (!string.IsNullOrWhiteSpace(persistedCaseId))
            {
                tenantCache[aggregateType] = persistedCaseId;
                return TenantEventRouteResolution.Accepted(
                    new TenantEventRoute(tenantId, persistedCaseId, aggregateType));
            }
        }

        if (!leaseAcquired)
        {
            throw new InvalidOperationException(
                $"Timed out waiting for a shared case mapping lease for tenant '{tenantId}' and aggregate '{aggregateType}'.");
        }

        try
        {
            persistedCaseId = await _caseMapStore
                .GetCaseIdAsync(tenantId, aggregateType, cancellationToken)
                .ConfigureAwait(false);
            if (!string.IsNullOrWhiteSpace(persistedCaseId))
            {
                tenantCache[aggregateType] = persistedCaseId;
                return TenantEventRouteResolution.Accepted(
                    new TenantEventRoute(tenantId, persistedCaseId, aggregateType));
            }

            if (await _caseMapStore.GetAggregateCountAsync(tenantId, cancellationToken).ConfigureAwait(false)
                >= options.MaxAutoCreatedCasesPerTenant)
            {
                return TenantEventRouteResolution.CaseCapExceeded(tenantId);
            }

            string caseName = CaseNameTemplateRenderer.Render(options.CaseNameTemplate, tenantId, aggregateType);
            string createdCaseId = await _caseCreationService
                .CreateCaseAsync(tenantId, caseName, cancellationToken)
                .ConfigureAwait(false);

            bool stored = await _caseMapStore
                .TryStoreCaseIdAsync(tenantId, aggregateType, createdCaseId, cancellationToken)
                .ConfigureAwait(false);

            string caseId = createdCaseId;
            if (!stored)
            {
                string? winnerCaseId = await _caseMapStore
                    .GetCaseIdAsync(tenantId, aggregateType, cancellationToken)
                    .ConfigureAwait(false);
                if (!string.IsNullOrWhiteSpace(winnerCaseId))
                {
                    caseId = winnerCaseId;
                }
            }

            tenantCache[aggregateType] = caseId;
            return TenantEventRouteResolution.Accepted(
                new TenantEventRoute(tenantId, caseId, aggregateType));
        }
        finally
        {
            await _caseMapStore
                .ReleaseCreationLockAsync(tenantId, aggregateType, cancellationToken)
                .ConfigureAwait(false);
        }
    }

    private async Task<(bool LeaseAcquired, string? CaseId)> WaitForSharedCaseOrLeaseAsync(
        string tenantId,
        string aggregateType,
        CancellationToken cancellationToken)
    {
        DateTimeOffset deadline = DateTimeOffset.UtcNow.Add(SharedCaseCreationWaitTimeout);
        while (DateTimeOffset.UtcNow < deadline)
        {
            cancellationToken.ThrowIfCancellationRequested();

            string? existingCaseId = await _caseMapStore
                .GetCaseIdAsync(tenantId, aggregateType, cancellationToken)
                .ConfigureAwait(false);
            if (!string.IsNullOrWhiteSpace(existingCaseId))
            {
                return (false, existingCaseId);
            }

            bool leaseAcquired = await _caseMapStore
                .TryAcquireCreationLockAsync(tenantId, aggregateType, SharedCaseCreationLeaseTtl, cancellationToken)
                .ConfigureAwait(false);
            if (leaseAcquired)
            {
                return (true, null);
            }

            await Task.Delay(SharedCaseCreationPollInterval, cancellationToken).ConfigureAwait(false);
        }

        return (false, null);
    }

    private static string? MatchTenant(string source, IReadOnlyDictionary<string, string> map)
    {
        string? bestPrefix = null;
        string? bestTenant = null;
        foreach (KeyValuePair<string, string> entry in map)
        {
            if (source.StartsWith(entry.Key, StringComparison.OrdinalIgnoreCase)
                && (bestPrefix is null || entry.Key.Length > bestPrefix.Length))
            {
                bestPrefix = entry.Key;
                bestTenant = entry.Value;
            }
        }

        return bestTenant;
    }
}
