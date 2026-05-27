// <copyright file="EventIngestionTelemetryAdapter.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.EventStoreIntegration;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

using Hexalith.Memories.Contracts.V1;
using Hexalith.Memories.EventStore;
using Hexalith.Memories.Server.Telemetry;
using Hexalith.Memories.Telemetry;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using StackExchange.Redis;

/// <summary>Server-side adapter implementing <see cref="IEventIngestionTelemetry"/> by routing outcomes
/// through the existing <see cref="AccessTelemetryLog"/> infrastructure AND — as of Story 9.3 —
/// fanning out a bounded fire-and-forget observation write to <see cref="IObservedEventTypeStore"/>.
/// </summary>
/// <remarks>Observation write guarantees (Story 9.3 Delta #3 + #14):
/// <list type="bullet">
///   <item><description>ONLY <see cref="EventIngestionOutcome.Accepted"/> triggers an observation write
///   (R3-8: Duplicate-counting inflates <c>EventsProcessedCount</c> by retry volume).</description></item>
///   <item><description>Bounded fire-and-forget: a process-wide <see cref="SemaphoreSlim"/>(256) caps
///   in-flight observation writes; if acquisition fails within 5 ms, the write is dropped.</description></item>
///   <item><description>Hard 2 s timeout on the write itself so a slow Redis cannot tie up threads.</description></item>
///   <item><description>Kill switch: <c>EventStoreIntegration:Observation:Enabled = false</c> disables
///   all observation writes; honoured on the NEXT event via <see cref="IOptionsMonitor{T}"/>.</description></item>
///   <item><description>Drops increment <see cref="MemoriesMeter.ObservationsDropped"/> with tag
///   <c>reason ∈ {backpressure, timeout, redis_error}</c> and emit log event 9144 Warning.</description></item>
/// </list></remarks>
internal sealed class EventIngestionTelemetryAdapter : IEventIngestionTelemetry, IDisposable
{
    private const string OkOutcome = AccessTelemetryLog.OutcomeOk;
    private const string ErrorOutcome = AccessTelemetryLog.OutcomeError;

    /// <summary>Process-wide in-flight cap for fire-and-forget observation writes. See Finding K
    /// in the Story 9.3 Dev Notes for the rationale behind 256.</summary>
    internal const int MaxInFlightObservationWrites = 256;

    /// <summary>Hard timeout on each observation write — so a slow Redis never holds a thread more than 2s.</summary>
    internal static readonly TimeSpan ObservationWriteTimeout = TimeSpan.FromSeconds(2);

    /// <summary>Semaphore acquire-attempt timeout; if the in-flight cap is saturated we drop fast.</summary>
    internal static readonly TimeSpan ObservationSemaphoreAcquireTimeout = TimeSpan.FromMilliseconds(5);

    private static readonly SemaphoreSlim ObservationInFlight =
        new(initialCount: MaxInFlightObservationWrites, maxCount: MaxInFlightObservationWrites);

    private readonly ILogger<AccessTelemetryCategory> _accessLogger;
    private readonly ILogger<EventIngestionTelemetryAdapter> _adapterLogger;
    private readonly IObservedEventTypeStore _observedEventTypeStore;
    private readonly IOptionsMonitor<EventStoreObservationOptions> _observationOptions;
    private readonly IDisposable? _optionsChangeSubscription;
    private bool? _lastKnownEnabled;

    public EventIngestionTelemetryAdapter(
        ILogger<AccessTelemetryCategory> accessLogger,
        ILogger<EventIngestionTelemetryAdapter> adapterLogger,
        IObservedEventTypeStore observedEventTypeStore,
        IOptionsMonitor<EventStoreObservationOptions> observationOptions)
    {
        ArgumentNullException.ThrowIfNull(accessLogger);
        ArgumentNullException.ThrowIfNull(adapterLogger);
        ArgumentNullException.ThrowIfNull(observedEventTypeStore);
        ArgumentNullException.ThrowIfNull(observationOptions);

        _accessLogger = accessLogger;
        _adapterLogger = adapterLogger;
        _observedEventTypeStore = observedEventTypeStore;
        _observationOptions = observationOptions;

        bool startupEnabled = observationOptions.CurrentValue.Enabled;
        _lastKnownEnabled = startupEnabled;

        // AC #21 (a) — emit 9143 once at startup when the kill switch is initially disabled, so the
        // disabled state is auditable even when no later transition occurs. The adapter is registered
        // as a singleton (TryAddSingleton in ServerEventStoreIntegrationExtensions) so the constructor
        // runs exactly once per process — no extra latch is required.
        if (!startupEnabled)
        {
            Hexalith.Memories.EventStore.EventStoreIntegrationLog
                .ObservationWritesConfigChanged(_adapterLogger, enabled: false);
        }

        // R3-7 — compare by VALUE not reference so we don't emit 9143 on spurious filesystem-watcher
        // double-fires that reuse the same reference.
        _optionsChangeSubscription = observationOptions.OnChange(current =>
        {
            if (_lastKnownEnabled == current.Enabled)
            {
                return;
            }

            _lastKnownEnabled = current.Enabled;
            Hexalith.Memories.EventStore.EventStoreIntegrationLog
                .ObservationWritesConfigChanged(_adapterLogger, current.Enabled);
        });
    }

    public void RecordIngestion(
        string tenantId,
        string? caseId,
        string? cloudEventId,
        string? aggregateType,
        string? cloudEventType,
        EventIngestionOutcome outcome,
        long durationMs)
    {
        (string outcomeTag, string? errorCode, bool isError) = MapOutcome(outcome);
        Dictionary<string, object?> queryParams = new(StringComparer.Ordinal)
        {
            ["sourceType"] = "event",
            ["cloudEventId"] = cloudEventId,
            ["aggregateType"] = aggregateType,
            ["cloudEventType"] = cloudEventType,
            ["eventOutcome"] = outcome.ToString(),
        };

        AccessTelemetryEvent record = AccessTelemetryLog.CreateEvent(
            eventId: isError ? 7512 : 7502,
            tenantId: tenantId,
            operationType: AccessTelemetryLog.OperationIngest,
            caseId: caseId,
            user: "events",
            queryParams: queryParams,
            resultCount: null,
            durationMs: durationMs,
            outcome: outcomeTag,
            errorCode: errorCode,
            currentActivity: Activity.Current);

        if (isError)
        {
            AccessTelemetryLog.LogIngestAccessError(_accessLogger, record);
        }
        else
        {
            AccessTelemetryLog.LogIngestAccess(_accessLogger, record);
        }

        // Delta #14 kill switch.
        if (!_observationOptions.CurrentValue.Enabled)
        {
            return;
        }

        // R3-8 semantic fix + whitespace-strict gates + __rejected__ guard.
        if (outcome != EventIngestionOutcome.Accepted
            || string.IsNullOrWhiteSpace(tenantId)
            || string.Equals(tenantId, MemoriesMeter.RejectedTenantTag, StringComparison.Ordinal)
            || string.IsNullOrWhiteSpace(aggregateType)
            || string.IsNullOrWhiteSpace(cloudEventType))
        {
            _adapterLogger.LogDebug(
                "Observation write skipped (outcome={Outcome}, hasTenantId={HasTenantId}, hasAggregate={HasAggregate}, hasEventType={HasEventType}).",
                outcome,
                !string.IsNullOrWhiteSpace(tenantId) && !string.Equals(tenantId, MemoriesMeter.RejectedTenantTag, StringComparison.Ordinal),
                !string.IsNullOrWhiteSpace(aggregateType),
                !string.IsNullOrWhiteSpace(cloudEventType));
            return;
        }

        // Delta #3 — bounded fire-and-forget.
        string tenantIdCopy = tenantId;
        string aggregateTypeCopy = aggregateType!;
        string cloudEventTypeCopy = cloudEventType!;
        DateTimeOffset observedAt = DateTimeOffset.UtcNow;

        _ = Task.Run(async () =>
            await WriteObservationAsync(tenantIdCopy, aggregateTypeCopy, cloudEventTypeCopy, observedAt)
                .ConfigureAwait(false));
    }

    public void Dispose()
    {
        _optionsChangeSubscription?.Dispose();
    }

    private async Task WriteObservationAsync(
        string tenantId,
        string aggregateType,
        string cloudEventType,
        DateTimeOffset observedAt)
    {
        bool acquired = false;
        try
        {
            acquired = await ObservationInFlight
                .WaitAsync(ObservationSemaphoreAcquireTimeout)
                .ConfigureAwait(false);
            if (!acquired)
            {
                MemoriesMeter.ObservationsDropped.Add(
                    1, new KeyValuePair<string, object?>("reason", "backpressure"));
                Hexalith.Memories.EventStore.EventStoreIntegrationLog
                    .ObservationDropped(_adapterLogger, tenantId, "backpressure");
                return;
            }

            using var writeCts = new CancellationTokenSource(ObservationWriteTimeout);
            try
            {
                await _observedEventTypeStore
                    .RecordObservationAsync(
                        tenantId, aggregateType, cloudEventType, observedAt, writeCts.Token)
                    .ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (writeCts.IsCancellationRequested)
            {
                MemoriesMeter.ObservationsDropped.Add(
                    1, new KeyValuePair<string, object?>("reason", "timeout"));
                Hexalith.Memories.EventStore.EventStoreIntegrationLog
                    .ObservationDropped(_adapterLogger, tenantId, "timeout");
            }
            catch (RedisException)
            {
                MemoriesMeter.ObservationsDropped.Add(
                    1, new KeyValuePair<string, object?>("reason", "redis_error"));
                Hexalith.Memories.EventStore.EventStoreIntegrationLog
                    .ObservationDropped(_adapterLogger, tenantId, "redis_error");

                // The store's own LoggerMessage emits 9140 at Warning for the underlying failure.
            }
        }
        finally
        {
            if (acquired)
            {
                ObservationInFlight.Release();
            }
        }
    }

    private static (string Outcome, string? ErrorCode, bool IsError) MapOutcome(EventIngestionOutcome outcome)
        => outcome switch
        {
            EventIngestionOutcome.Accepted => (OkOutcome, null, false),
            EventIngestionOutcome.Duplicate => (OkOutcome, null, false),
            EventIngestionOutcome.UnknownSource => (ErrorOutcome, "UNKNOWN_SOURCE", true),
            EventIngestionOutcome.TenantNotFound => (ErrorOutcome, "TENANT_NOT_FOUND", true),
            EventIngestionOutcome.TenantProvisioning => (ErrorOutcome, "TENANT_PROVISIONING", true),
            EventIngestionOutcome.TenantDeleting => (ErrorOutcome, "TENANT_DELETING", true),
            EventIngestionOutcome.AutoCreateDisabled => (ErrorOutcome, "AUTO_CREATE_DISABLED", true),
            EventIngestionOutcome.CaseCapExceeded => (ErrorOutcome, "CASE_CAP_EXCEEDED", true),
            EventIngestionOutcome.InvalidCloudEvent => (ErrorOutcome, "INVALID_CLOUDEVENT", true),
            EventIngestionOutcome.ScheduleFailed => (ErrorOutcome, "SCHEDULE_FAILED", true),
            _ => (ErrorOutcome, "UNKNOWN", true),
        };
}
