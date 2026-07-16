// <copyright file="EventIngestionService.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.EventStore;

using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

using Hexalith.Memories.Contracts.V1;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

/// <summary>Default <see cref="IEventIngestionService"/>. Drives one CloudEvents subscription request through:
/// <list type="number">
///   <item>Envelope parse + validation (<see cref="CloudEventEnvelopeParser"/>).</item>
///   <item>Tenant + case routing (<see cref="ITenantEventRouter"/>).</item>
///   <item>Preflight dedup reservation (<see cref="IPreflightDedupStore"/>), when enabled.</item>
///   <item>Workflow scheduling (<see cref="IEventIngestionWorkflowScheduler"/>).</item>
///   <item>Compensation — releases the reservation if scheduling throws so DAPR retries land on a clean key (AC #9).</item>
/// </list>
/// </summary>
internal sealed class EventIngestionService : IEventIngestionService
{
    private readonly ITenantEventRouter _router;
    private readonly IEventIngestionWorkflowScheduler _scheduler;
    private readonly IPreflightDedupStore _dedupStore;
    private readonly ISearchIndexMaintenance _searchIndexMaintenance;
    private readonly IEventIngestionTelemetry _telemetry;
    private readonly IOptionsMonitor<TenantEventRoutingOptions> _options;
    private readonly ILogger<EventIngestionService> _logger;

    public EventIngestionService(
        ITenantEventRouter router,
        IEventIngestionWorkflowScheduler scheduler,
        IPreflightDedupStore dedupStore,
        ISearchIndexMaintenance searchIndexMaintenance,
        IEventIngestionTelemetry telemetry,
        IOptionsMonitor<TenantEventRoutingOptions> options,
        ILogger<EventIngestionService> logger)
    {
        ArgumentNullException.ThrowIfNull(router);
        ArgumentNullException.ThrowIfNull(scheduler);
        ArgumentNullException.ThrowIfNull(dedupStore);
        ArgumentNullException.ThrowIfNull(searchIndexMaintenance);
        ArgumentNullException.ThrowIfNull(telemetry);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(logger);
        _router = router;
        _scheduler = scheduler;
        _dedupStore = dedupStore;
        _searchIndexMaintenance = searchIndexMaintenance;
        _telemetry = telemetry;
        _options = options;
        _logger = logger;
    }

    public async Task<EventIngestionProcessResult> ProcessAsync(
        JsonElement envelopeJson,
        CancellationToken cancellationToken)
    {
        long startTicks = Stopwatch.GetTimestamp();
        string? cloudEventId = null;
        string tenantIdForTelemetry = "__rejected__";
        string? caseIdForTelemetry = null;
        string? aggregateTypeForTelemetry = null;
        string? cloudEventTypeForTelemetry = null;

        CloudEventEnvelope envelope;
        try
        {
            envelope = CloudEventEnvelopeParser.Parse(envelopeJson);
            cloudEventId = envelope.Id;
            cloudEventTypeForTelemetry = envelope.Type;
        }
        catch (InvalidOperationException ex)
        {
            EventStoreIntegrationLog.InvalidEnvelope(_logger, ex.Message, cloudEventId ?? "(unknown)");
            EventIngestionProcessResult invalid = new(
                EventIngestionOutcome.InvalidCloudEvent,
                EventIngestionResponse.Invalid(ex.Message));
            _telemetry.RecordIngestion(
                tenantIdForTelemetry, caseIdForTelemetry, cloudEventId, aggregateTypeForTelemetry,
                cloudEventTypeForTelemetry, invalid.Outcome, ElapsedMs(startTicks));
            return invalid;
        }

        TenantEventRouteResolution resolution;
        try
        {
            resolution = await _router
                .ResolveAsync(envelope, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            EventStoreIntegrationLog.RouteResolutionFailed(_logger, envelope.Id, ex.GetType().Name);
            EventIngestionProcessResult failed = new(
                EventIngestionOutcome.ScheduleFailed,
                EventIngestionResponse.Drop("routing-failed", ex.Message));
            _telemetry.RecordIngestion(
                tenantIdForTelemetry, caseIdForTelemetry, cloudEventId, aggregateTypeForTelemetry,
                cloudEventTypeForTelemetry, failed.Outcome, ElapsedMs(startTicks));
            return failed;
        }

        if (resolution.TenantId is not null)
        {
            tenantIdForTelemetry = resolution.TenantId;
        }

        EventIngestionProcessResult? drop = MapNonAcceptedResolution(resolution, envelope);
        if (drop is not null)
        {
            _telemetry.RecordIngestion(
                tenantIdForTelemetry, caseIdForTelemetry, cloudEventId, aggregateTypeForTelemetry,
                cloudEventTypeForTelemetry, drop.Outcome, ElapsedMs(startTicks));
            return drop;
        }

        TenantEventRoute route = resolution.Route!;
        caseIdForTelemetry = route.CaseId;
        aggregateTypeForTelemetry = route.AggregateType;

        // Curated search-index maintenance events bypass the generic raw-event ingestion workflow AND the
        // preflight dedup. Their CloudEvent id is a stable source identity (e.g. "tenant:{id}") that is
        // re-published verbatim on every revision (a rename keeps the same id), so routing them through the
        // cloudevent.id dedup would silently drop legitimate updates. They are upsert-by-(index, aggregate)
        // snapshots, so applying them directly is naturally idempotent and needs no dedup reservation.
        if (CuratedSearchIndexEventTypes.IsCuratedType(envelope.Type))
        {
            EventIngestionProcessResult curated = await ApplyCuratedSearchIndexEventAsync(envelope, route, cancellationToken)
                .ConfigureAwait(false);
            _telemetry.RecordIngestion(
                tenantIdForTelemetry, caseIdForTelemetry, cloudEventId, aggregateTypeForTelemetry,
                cloudEventTypeForTelemetry, curated.Outcome, ElapsedMs(startTicks));
            return curated;
        }

        IngestionInput input = CloudEventToIngestionInputMapper.Map(envelope, route);
        string dedupKey = EventStoreDedupKey.Build(route.TenantId, route.CaseId, envelope.Id);

        TenantEventRoutingOptions options = _options.CurrentValue;
        bool reservationHeld = false;
        if (options.PreflightDedupEnabled)
        {
            PreflightReservationResult reservation = await _dedupStore
                .TryReserveAsync(dedupKey, options.PreflightDedupTtl, cancellationToken)
                .ConfigureAwait(false);

            switch (reservation)
            {
                case PreflightReservationResult.Duplicate:
                    EventIngestionProcessResult duplicate = new(
                        EventIngestionOutcome.Duplicate,
                        EventIngestionResponse.Duplicate());
                    _telemetry.RecordIngestion(
                        tenantIdForTelemetry, caseIdForTelemetry, cloudEventId, aggregateTypeForTelemetry,
                        cloudEventTypeForTelemetry, duplicate.Outcome, ElapsedMs(startTicks));
                    return duplicate;
                case PreflightReservationResult.Reserved:
                    reservationHeld = true;
                    break;
                case PreflightReservationResult.FailOpen:
                    reservationHeld = false;
                    break;
                default:
                    reservationHeld = false;
                    break;
            }
        }

        string instanceId = dedupKey;
        try
        {
            string scheduled = await _scheduler
                .ScheduleAsync(instanceId, input, cancellationToken)
                .ConfigureAwait(false);

            EventIngestionProcessResult accepted = new(
                EventIngestionOutcome.Accepted,
                EventIngestionResponse.Accepted(scheduled));
            _telemetry.RecordIngestion(
                tenantIdForTelemetry, caseIdForTelemetry, cloudEventId, aggregateTypeForTelemetry,
                cloudEventTypeForTelemetry, accepted.Outcome, ElapsedMs(startTicks));
            return accepted;
        }
        catch (DuplicateWorkflowInstanceException)
        {
            EventIngestionProcessResult duplicate = new(
                EventIngestionOutcome.Duplicate,
                EventIngestionResponse.Duplicate());
            _telemetry.RecordIngestion(
                tenantIdForTelemetry, caseIdForTelemetry, cloudEventId, aggregateTypeForTelemetry,
                cloudEventTypeForTelemetry, duplicate.Outcome, ElapsedMs(startTicks));
            return duplicate;
        }
        catch (Exception ex)
        {
            EventStoreIntegrationLog.WorkflowScheduleFailed(_logger, envelope.Id, ex.GetType().Name);

            if (reservationHeld)
            {
                try
                {
                    await _dedupStore.ReleaseAsync(dedupKey, cancellationToken).ConfigureAwait(false);
                }
                catch (Exception releaseEx)
                {
                    EventStoreIntegrationLog.PreflightReleaseFailed(_logger, envelope.Id, releaseEx.GetType().Name);
                }
            }

            EventIngestionProcessResult failed = new(
                EventIngestionOutcome.ScheduleFailed,
                EventIngestionResponse.Drop("schedule-failed", ex.Message));
            _telemetry.RecordIngestion(
                tenantIdForTelemetry, caseIdForTelemetry, cloudEventId, aggregateTypeForTelemetry,
                cloudEventTypeForTelemetry, failed.Outcome, ElapsedMs(startTicks));
            return failed;
        }
    }

    private async Task<EventIngestionProcessResult> ApplyCuratedSearchIndexEventAsync(
        CloudEventEnvelope envelope,
        TenantEventRoute route,
        CancellationToken cancellationToken)
    {
        try
        {
            if (string.Equals(envelope.Type, CuratedSearchIndexEventTypes.Changed, StringComparison.Ordinal))
            {
                SearchIndexEntryChanged? entry = DeserializeData<SearchIndexEntryChanged>(envelope.Data);
                if (entry is null || string.IsNullOrWhiteSpace(entry.AggregateId) || string.IsNullOrWhiteSpace(entry.Text))
                {
                    const string reason = "SearchIndexEntryChanged requires a non-empty AggregateId and Text";
                    EventStoreIntegrationLog.CuratedSearchIndexEventInvalid(_logger, reason, envelope.Id);
                    return new EventIngestionProcessResult(
                        EventIngestionOutcome.InvalidCloudEvent,
                        EventIngestionResponse.Invalid(reason));
                }

                await _searchIndexMaintenance
                    .ApplyEntryChangedAsync(route.TenantId, envelope.Id, entry, route.CaseId, cancellationToken)
                    .ConfigureAwait(false);
                EventStoreIntegrationLog.CuratedSearchIndexEntryApplied(_logger, "upserted", route.TenantId, entry.AggregateId, envelope.Id);
            }
            else
            {
                SearchIndexEntryRemoved? entry = DeserializeData<SearchIndexEntryRemoved>(envelope.Data);
                if (entry is null || string.IsNullOrWhiteSpace(entry.AggregateId))
                {
                    const string reason = "SearchIndexEntryRemoved requires a non-empty AggregateId";
                    EventStoreIntegrationLog.CuratedSearchIndexEventInvalid(_logger, reason, envelope.Id);
                    return new EventIngestionProcessResult(
                        EventIngestionOutcome.InvalidCloudEvent,
                        EventIngestionResponse.Invalid(reason));
                }

                await _searchIndexMaintenance
                    .ApplyEntryRemovedAsync(route.TenantId, entry, cancellationToken)
                    .ConfigureAwait(false);
                EventStoreIntegrationLog.CuratedSearchIndexEntryApplied(_logger, "removed", route.TenantId, entry.AggregateId, envelope.Id);
            }

            return new EventIngestionProcessResult(
                EventIngestionOutcome.Accepted,
                EventIngestionResponse.Accepted(envelope.Id));
        }
        catch (JsonException ex)
        {
            EventStoreIntegrationLog.CuratedSearchIndexEventInvalid(_logger, ex.Message, envelope.Id);
            return new EventIngestionProcessResult(
                EventIngestionOutcome.InvalidCloudEvent,
                EventIngestionResponse.Invalid(ex.Message));
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // Maintenance failed (e.g. transient Redis fault). Return a retryable outcome so DAPR redelivers;
            // the upsert is idempotent so a retry is safe.
            EventStoreIntegrationLog.CuratedSearchIndexMaintenanceFailed(_logger, envelope.Id, ex.GetType().Name);
            return new EventIngestionProcessResult(
                EventIngestionOutcome.ScheduleFailed,
                EventIngestionResponse.Drop("search-index-maintenance-failed", ex.Message));
        }
    }

    private static T? DeserializeData<T>(JsonElement data)
        where T : class
        => data.ValueKind == JsonValueKind.Object
            ? data.Deserialize((JsonTypeInfo<T>)MemoriesJsonContext.Options.GetTypeInfo(typeof(T)))
            : null;

    private EventIngestionProcessResult? MapNonAcceptedResolution(
        TenantEventRouteResolution resolution,
        CloudEventEnvelope envelope)
    {
        switch (resolution.Status)
        {
            case TenantEventRouteResolutionStatus.Accepted:
                return null;
            case TenantEventRouteResolutionStatus.UnknownSource:
                EventStoreIntegrationLog.UnknownSource(_logger, envelope.Source, envelope.Id);
                return new EventIngestionProcessResult(
                    EventIngestionOutcome.UnknownSource,
                    EventIngestionResponse.Drop(EventIngestionResponse.StatusUnknownSource, "No tenant mapping for source"));
            case TenantEventRouteResolutionStatus.TenantNotFound:
                EventStoreIntegrationLog.TenantNotFound(_logger, resolution.TenantId ?? "(null)", envelope.Id);
                return new EventIngestionProcessResult(
                    EventIngestionOutcome.TenantNotFound,
                    EventIngestionResponse.Drop(EventIngestionResponse.StatusTenantNotFound, "Resolved tenant does not exist"));
            case TenantEventRouteResolutionStatus.TenantProvisioning:
                EventStoreIntegrationLog.TenantProvisioning(_logger, resolution.TenantId ?? "(null)", envelope.Id);
                return new EventIngestionProcessResult(
                    EventIngestionOutcome.TenantProvisioning,
                    EventIngestionResponse.Drop("tenant-provisioning", "Tenant is provisioning"));
            case TenantEventRouteResolutionStatus.TenantDeleting:
                EventStoreIntegrationLog.TenantDeleting(_logger, resolution.TenantId ?? "(null)", envelope.Id);
                return new EventIngestionProcessResult(
                    EventIngestionOutcome.TenantDeleting,
                    EventIngestionResponse.Drop(EventIngestionResponse.StatusTenantDeleting, "Tenant is deleting"));
            case TenantEventRouteResolutionStatus.AutoCreateDisabled:
                EventStoreIntegrationLog.AutoCreateDisabled(_logger, resolution.TenantId ?? "(null)", envelope.Id);
                return new EventIngestionProcessResult(
                    EventIngestionOutcome.AutoCreateDisabled,
                    EventIngestionResponse.Drop(EventIngestionResponse.StatusAutoCreateDisabled, "Auto-create disabled"));
            case TenantEventRouteResolutionStatus.CaseCapExceeded:
                EventStoreIntegrationLog.CaseCapExceeded(_logger, resolution.TenantId ?? "(null)", envelope.Id);
                return new EventIngestionProcessResult(
                    EventIngestionOutcome.CaseCapExceeded,
                    EventIngestionResponse.Drop(EventIngestionResponse.StatusCaseCapExceeded, "Case cap exceeded"));
            default:
                return new EventIngestionProcessResult(
                    EventIngestionOutcome.ScheduleFailed,
                    EventIngestionResponse.Drop("unknown-resolution", resolution.Status.ToString()));
        }
    }

    private static long ElapsedMs(long startTicks)
        => (long)Stopwatch.GetElapsedTime(startTicks).TotalMilliseconds;
}
