// <copyright file="EventIngestionTelemetryAdapter.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.EventStoreIntegration;

using System.Collections.Generic;
using System.Diagnostics;

using Hexalith.Memories.Contracts.V1;
using Hexalith.Memories.EventStore;
using Hexalith.Memories.Server.Telemetry;

using Microsoft.Extensions.Logging;

/// <summary>Server-side adapter implementing <see cref="IEventIngestionTelemetry"/> by routing outcomes
/// through the existing <see cref="AccessTelemetryLog"/> infrastructure. Every event-ingestion request
/// emits a single AccessTelemetryLog entry using <c>OperationIngest</c> so event-sourced ingestion is
/// observable alongside URL / file / directory ingestion.</summary>
internal sealed class EventIngestionTelemetryAdapter : IEventIngestionTelemetry
{
    private const string OkOutcome = AccessTelemetryLog.OutcomeOk;
    private const string ErrorOutcome = AccessTelemetryLog.OutcomeError;

    private readonly ILogger<AccessTelemetryCategory> _logger;

    public EventIngestionTelemetryAdapter(ILogger<AccessTelemetryCategory> logger)
    {
        ArgumentNullException.ThrowIfNull(logger);
        _logger = logger;
    }

    public void RecordIngestion(
        string tenantId,
        string? caseId,
        string? cloudEventId,
        string? aggregateType,
        EventIngestionOutcome outcome,
        long durationMs)
    {
        (string outcomeTag, string? errorCode, bool isError) = MapOutcome(outcome);
        Dictionary<string, object?> queryParams = new(StringComparer.Ordinal)
        {
            ["sourceType"] = "event",
            ["cloudEventId"] = cloudEventId,
            ["aggregateType"] = aggregateType,
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
            AccessTelemetryLog.LogIngestAccessError(_logger, record);
        }
        else
        {
            AccessTelemetryLog.LogIngestAccess(_logger, record);
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
