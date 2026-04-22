// <copyright file="EventIngestionService.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.EventStore;

using System.Diagnostics;
using System.Text.Json;

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
    private readonly IEventIngestionTelemetry _telemetry;
    private readonly IOptionsMonitor<TenantEventRoutingOptions> _options;
    private readonly ILogger<EventIngestionService> _logger;

    public EventIngestionService(
        ITenantEventRouter router,
        IEventIngestionWorkflowScheduler scheduler,
        IPreflightDedupStore dedupStore,
        IEventIngestionTelemetry telemetry,
        IOptionsMonitor<TenantEventRoutingOptions> options,
        ILogger<EventIngestionService> logger)
    {
        ArgumentNullException.ThrowIfNull(router);
        ArgumentNullException.ThrowIfNull(scheduler);
        ArgumentNullException.ThrowIfNull(dedupStore);
        ArgumentNullException.ThrowIfNull(telemetry);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(logger);
        _router = router;
        _scheduler = scheduler;
        _dedupStore = dedupStore;
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

        CloudEventEnvelope envelope;
        try
        {
            envelope = CloudEventEnvelopeParser.Parse(envelopeJson);
            cloudEventId = envelope.Id;
        }
        catch (InvalidOperationException ex)
        {
            EventStoreIntegrationLog.InvalidEnvelope(_logger, ex.Message, cloudEventId ?? "(unknown)");
            EventIngestionProcessResult invalid = new(
                EventIngestionOutcome.InvalidCloudEvent,
                EventIngestionResponse.Invalid(ex.Message));
            _telemetry.RecordIngestion(
                tenantIdForTelemetry, caseIdForTelemetry, cloudEventId, aggregateTypeForTelemetry,
                invalid.Outcome, ElapsedMs(startTicks));
            return invalid;
        }

        TenantEventRouteResolution resolution = await _router
            .ResolveAsync(envelope, cancellationToken)
            .ConfigureAwait(false);

        if (resolution.TenantId is not null)
        {
            tenantIdForTelemetry = resolution.TenantId;
        }

        EventIngestionProcessResult? drop = MapNonAcceptedResolution(resolution, envelope);
        if (drop is not null)
        {
            _telemetry.RecordIngestion(
                tenantIdForTelemetry, caseIdForTelemetry, cloudEventId, aggregateTypeForTelemetry,
                drop.Outcome, ElapsedMs(startTicks));
            return drop;
        }

        TenantEventRoute route = resolution.Route!;
        caseIdForTelemetry = route.CaseId;
        aggregateTypeForTelemetry = route.AggregateType;

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
                        duplicate.Outcome, ElapsedMs(startTicks));
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
                accepted.Outcome, ElapsedMs(startTicks));
            return accepted;
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
                failed.Outcome, ElapsedMs(startTicks));
            return failed;
        }
    }

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
