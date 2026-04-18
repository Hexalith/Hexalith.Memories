// <copyright file="AccessTelemetryLog.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Telemetry;

using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;

using Hexalith.Memories.Contracts.V1;

using Microsoft.Extensions.Logging;

/// <summary>
/// Story 7.5 — source-generated emitters for per-tenant audit events (FR67). EventId bank <c>7500-7599</c>
/// is pinned for this story.
/// <para>
/// Operation-type → EventId mapping:
/// <list type="bullet">
///   <item><description>7501 — LogSearchAccess (Information)</description></item>
///   <item><description>7502 — LogIngestAccess (Information)</description></item>
///   <item><description>7503 — LogTraverseAccess (Information)</description></item>
///   <item><description>7504 — LogCaseAccess (Information)</description></item>
///   <item><description>7505 — LogDeleteAccess (Information)</description></item>
///   <item><description>7511-7515 — matching <c>*Error</c> variants at Warning level</description></item>
/// </list>
/// </para>
/// <para>
/// All emitters take the <see cref="AccessTelemetryEvent"/> record as a single structured argument via the
/// <c>{@AuditEvent}</c> destructuring placeholder so <c>AddJsonConsole</c> produces the AC #4 JSON shape.
/// </para>
/// </summary>
internal static partial class AccessTelemetryLog
{
    /// <summary>Operation type constant: search.</summary>
    public const string OperationSearch = "search";

    /// <summary>Operation type constant: ingest.</summary>
    public const string OperationIngest = "ingest";

    /// <summary>Operation type constant: traverse.</summary>
    public const string OperationTraverse = "traverse";

    /// <summary>Operation type constant: case-access (memory-unit read).</summary>
    public const string OperationCaseAccess = "case-access";

    /// <summary>Operation type constant: delete.</summary>
    public const string OperationDelete = "delete";

    /// <summary>Outcome constant: success.</summary>
    public const string OutcomeOk = "ok";

    /// <summary>Outcome constant: partial success (hybrid search with ≥1 degraded axis).</summary>
    public const string OutcomePartial = "partial";

    /// <summary>Outcome constant: failure.</summary>
    public const string OutcomeError = "error";

    /// <summary>User constant for the fallback anonymous identity.</summary>
    public const string UserAnonymous = "anonymous";

    /// <summary>User constant for quickstart-wizard-originated operations.</summary>
    public const string UserQuickstartWizard = "quickstart-wizard";

    /// <summary>
    /// Builds an <see cref="AccessTelemetryEvent"/> populated with the current <see cref="Activity"/> trace/span ids.
    /// Callers pass this record to the corresponding <c>LogXxx</c> emitter.
    /// </summary>
    /// <param name="eventId">Logger event id (7500-7599 bank).</param>
    /// <param name="tenantId">Tenant id (or <c>__rejected__</c> when the tenant guard rejected the request).</param>
    /// <param name="operationType">Operation type constant (see <see cref="OperationSearch"/> etc.).</param>
    /// <param name="caseId">Case id (null if not case-scoped).</param>
    /// <param name="user">User identity per ADR-7.5-004.</param>
    /// <param name="queryParams">Operation-specific params (content-free).</param>
    /// <param name="resultCount">Result count for read operations (null otherwise).</param>
    /// <param name="durationMs">Elapsed operation duration in milliseconds.</param>
    /// <param name="outcome">Outcome constant (see <see cref="OutcomeOk"/> etc.).</param>
    /// <param name="errorCode">Error code (null on success).</param>
    /// <param name="currentActivity">Current activity (typically <see cref="Activity.Current"/>).</param>
    /// <returns>A fully populated <see cref="AccessTelemetryEvent"/>.</returns>
    public static AccessTelemetryEvent CreateEvent(
        int eventId,
        string tenantId,
        string operationType,
        string? caseId,
        string user,
        IReadOnlyDictionary<string, object?> queryParams,
        int? resultCount,
        long durationMs,
        string outcome,
        string? errorCode,
        Activity? currentActivity)
        => new()
        {
            EventId = eventId,
            Timestamp = DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture),
            TenantId = tenantId,
            OperationType = operationType,
            CaseId = caseId,
            User = user,
            QueryParams = queryParams,
            ResultCount = resultCount,
            DurationMs = durationMs,
            Outcome = outcome,
            ErrorCode = errorCode,
            TraceId = currentActivity?.TraceId.ToString(),
            SpanId = currentActivity?.SpanId.ToString(),
        };

    [LoggerMessage(EventId = 7501, Level = LogLevel.Information, Message = "Search access {@AuditEvent}")]
    public static partial void LogSearchAccess(ILogger<AccessTelemetryCategory> logger, AccessTelemetryEvent auditEvent);

    [LoggerMessage(EventId = 7502, Level = LogLevel.Information, Message = "Ingest access {@AuditEvent}")]
    public static partial void LogIngestAccess(ILogger<AccessTelemetryCategory> logger, AccessTelemetryEvent auditEvent);

    [LoggerMessage(EventId = 7503, Level = LogLevel.Information, Message = "Traverse access {@AuditEvent}")]
    public static partial void LogTraverseAccess(ILogger<AccessTelemetryCategory> logger, AccessTelemetryEvent auditEvent);

    [LoggerMessage(EventId = 7504, Level = LogLevel.Information, Message = "Case access {@AuditEvent}")]
    public static partial void LogCaseAccess(ILogger<AccessTelemetryCategory> logger, AccessTelemetryEvent auditEvent);

    [LoggerMessage(EventId = 7505, Level = LogLevel.Information, Message = "Delete access {@AuditEvent}")]
    public static partial void LogDeleteAccess(ILogger<AccessTelemetryCategory> logger, AccessTelemetryEvent auditEvent);

    [LoggerMessage(EventId = 7511, Level = LogLevel.Warning, Message = "Search access error {@AuditEvent}")]
    public static partial void LogSearchAccessError(ILogger<AccessTelemetryCategory> logger, AccessTelemetryEvent auditEvent);

    [LoggerMessage(EventId = 7512, Level = LogLevel.Warning, Message = "Ingest access error {@AuditEvent}")]
    public static partial void LogIngestAccessError(ILogger<AccessTelemetryCategory> logger, AccessTelemetryEvent auditEvent);

    [LoggerMessage(EventId = 7513, Level = LogLevel.Warning, Message = "Traverse access error {@AuditEvent}")]
    public static partial void LogTraverseAccessError(ILogger<AccessTelemetryCategory> logger, AccessTelemetryEvent auditEvent);

    [LoggerMessage(EventId = 7514, Level = LogLevel.Warning, Message = "Case access error {@AuditEvent}")]
    public static partial void LogCaseAccessError(ILogger<AccessTelemetryCategory> logger, AccessTelemetryEvent auditEvent);

    [LoggerMessage(EventId = 7515, Level = LogLevel.Warning, Message = "Delete access error {@AuditEvent}")]
    public static partial void LogDeleteAccessError(ILogger<AccessTelemetryCategory> logger, AccessTelemetryEvent auditEvent);
}
