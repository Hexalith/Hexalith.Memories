// <copyright file="AccessTelemetryLog.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Telemetry;

using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;

using Hexalith.Memories.Contracts.V1;
using Hexalith.Memories.Server.Telemetry.AccessTelemetryLifecycle;

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
///   <item><description>7506-7509 — tenant lifecycle/config, case-member, and annotation access</description></item>
///   <item><description>7511-7519 — matching <c>*Error</c> variants at Warning level</description></item>
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

    /// <summary>Operation type constant: tenant-lifecycle.</summary>
    public const string OperationTenantLifecycle = "tenant-lifecycle";

    /// <summary>Operation type constant: tenant-config.</summary>
    public const string OperationTenantConfig = "tenant-config";

    /// <summary>Operation type constant: case-member.</summary>
    public const string OperationCaseMember = "case-member";

    /// <summary>Operation type constant: annotation.</summary>
    public const string OperationAnnotation = "annotation";

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

    public static void LogSearchAccess(ILogger<AccessTelemetryCategory> logger, AccessTelemetryEvent auditEvent)
        => Emit(logger, LogLevel.Information, auditEvent, LogSearchAccessCore);

    [LoggerMessage(EventId = 7501, Level = LogLevel.Information, Message = "Search access {@AuditEvent}", SkipEnabledCheck = true, EventName = nameof(LogSearchAccess))]
    private static partial void LogSearchAccessCore(ILogger<AccessTelemetryCategory> logger, AccessTelemetryEvent auditEvent);

    public static void LogIngestAccess(ILogger<AccessTelemetryCategory> logger, AccessTelemetryEvent auditEvent)
        => Emit(logger, LogLevel.Information, auditEvent, LogIngestAccessCore);

    [LoggerMessage(EventId = 7502, Level = LogLevel.Information, Message = "Ingest access {@AuditEvent}", SkipEnabledCheck = true, EventName = nameof(LogIngestAccess))]
    private static partial void LogIngestAccessCore(ILogger<AccessTelemetryCategory> logger, AccessTelemetryEvent auditEvent);

    public static void LogTraverseAccess(ILogger<AccessTelemetryCategory> logger, AccessTelemetryEvent auditEvent)
        => Emit(logger, LogLevel.Information, auditEvent, LogTraverseAccessCore);

    [LoggerMessage(EventId = 7503, Level = LogLevel.Information, Message = "Traverse access {@AuditEvent}", SkipEnabledCheck = true, EventName = nameof(LogTraverseAccess))]
    private static partial void LogTraverseAccessCore(ILogger<AccessTelemetryCategory> logger, AccessTelemetryEvent auditEvent);

    public static void LogCaseAccess(ILogger<AccessTelemetryCategory> logger, AccessTelemetryEvent auditEvent)
        => Emit(logger, LogLevel.Information, auditEvent, LogCaseAccessCore);

    [LoggerMessage(EventId = 7504, Level = LogLevel.Information, Message = "Case access {@AuditEvent}", SkipEnabledCheck = true, EventName = nameof(LogCaseAccess))]
    private static partial void LogCaseAccessCore(ILogger<AccessTelemetryCategory> logger, AccessTelemetryEvent auditEvent);

    public static void LogDeleteAccess(ILogger<AccessTelemetryCategory> logger, AccessTelemetryEvent auditEvent)
        => Emit(logger, LogLevel.Information, auditEvent, LogDeleteAccessCore);

    [LoggerMessage(EventId = 7505, Level = LogLevel.Information, Message = "Delete access {@AuditEvent}", SkipEnabledCheck = true, EventName = nameof(LogDeleteAccess))]
    private static partial void LogDeleteAccessCore(ILogger<AccessTelemetryCategory> logger, AccessTelemetryEvent auditEvent);

    public static void LogTenantLifecycleAccess(ILogger<AccessTelemetryCategory> logger, AccessTelemetryEvent auditEvent)
        => Emit(logger, LogLevel.Information, auditEvent, LogTenantLifecycleAccessCore);

    [LoggerMessage(EventId = 7506, Level = LogLevel.Information, Message = "Tenant lifecycle access {@AuditEvent}", SkipEnabledCheck = true, EventName = nameof(LogTenantLifecycleAccess))]
    private static partial void LogTenantLifecycleAccessCore(ILogger<AccessTelemetryCategory> logger, AccessTelemetryEvent auditEvent);

    public static void LogTenantConfigAccess(ILogger<AccessTelemetryCategory> logger, AccessTelemetryEvent auditEvent)
        => Emit(logger, LogLevel.Information, auditEvent, LogTenantConfigAccessCore);

    [LoggerMessage(EventId = 7507, Level = LogLevel.Information, Message = "Tenant config access {@AuditEvent}", SkipEnabledCheck = true, EventName = nameof(LogTenantConfigAccess))]
    private static partial void LogTenantConfigAccessCore(ILogger<AccessTelemetryCategory> logger, AccessTelemetryEvent auditEvent);

    public static void LogCaseMemberAccess(ILogger<AccessTelemetryCategory> logger, AccessTelemetryEvent auditEvent)
        => Emit(logger, LogLevel.Information, auditEvent, LogCaseMemberAccessCore);

    [LoggerMessage(EventId = 7508, Level = LogLevel.Information, Message = "Case member access {@AuditEvent}", SkipEnabledCheck = true, EventName = nameof(LogCaseMemberAccess))]
    private static partial void LogCaseMemberAccessCore(ILogger<AccessTelemetryCategory> logger, AccessTelemetryEvent auditEvent);

    public static void LogAnnotationAccess(ILogger<AccessTelemetryCategory> logger, AccessTelemetryEvent auditEvent)
        => Emit(logger, LogLevel.Information, auditEvent, LogAnnotationAccessCore);

    [LoggerMessage(EventId = 7509, Level = LogLevel.Information, Message = "Annotation access {@AuditEvent}", SkipEnabledCheck = true, EventName = nameof(LogAnnotationAccess))]
    private static partial void LogAnnotationAccessCore(ILogger<AccessTelemetryCategory> logger, AccessTelemetryEvent auditEvent);

    public static void LogSearchAccessError(ILogger<AccessTelemetryCategory> logger, AccessTelemetryEvent auditEvent)
        => Emit(logger, LogLevel.Warning, auditEvent, LogSearchAccessErrorCore);

    [LoggerMessage(EventId = 7511, Level = LogLevel.Warning, Message = "Search access error {@AuditEvent}", SkipEnabledCheck = true, EventName = nameof(LogSearchAccessError))]
    private static partial void LogSearchAccessErrorCore(ILogger<AccessTelemetryCategory> logger, AccessTelemetryEvent auditEvent);

    public static void LogIngestAccessError(ILogger<AccessTelemetryCategory> logger, AccessTelemetryEvent auditEvent)
        => Emit(logger, LogLevel.Warning, auditEvent, LogIngestAccessErrorCore);

    [LoggerMessage(EventId = 7512, Level = LogLevel.Warning, Message = "Ingest access error {@AuditEvent}", SkipEnabledCheck = true, EventName = nameof(LogIngestAccessError))]
    private static partial void LogIngestAccessErrorCore(ILogger<AccessTelemetryCategory> logger, AccessTelemetryEvent auditEvent);

    public static void LogTraverseAccessError(ILogger<AccessTelemetryCategory> logger, AccessTelemetryEvent auditEvent)
        => Emit(logger, LogLevel.Warning, auditEvent, LogTraverseAccessErrorCore);

    [LoggerMessage(EventId = 7513, Level = LogLevel.Warning, Message = "Traverse access error {@AuditEvent}", SkipEnabledCheck = true, EventName = nameof(LogTraverseAccessError))]
    private static partial void LogTraverseAccessErrorCore(ILogger<AccessTelemetryCategory> logger, AccessTelemetryEvent auditEvent);

    public static void LogCaseAccessError(ILogger<AccessTelemetryCategory> logger, AccessTelemetryEvent auditEvent)
        => Emit(logger, LogLevel.Warning, auditEvent, LogCaseAccessErrorCore);

    [LoggerMessage(EventId = 7514, Level = LogLevel.Warning, Message = "Case access error {@AuditEvent}", SkipEnabledCheck = true, EventName = nameof(LogCaseAccessError))]
    private static partial void LogCaseAccessErrorCore(ILogger<AccessTelemetryCategory> logger, AccessTelemetryEvent auditEvent);

    public static void LogDeleteAccessError(ILogger<AccessTelemetryCategory> logger, AccessTelemetryEvent auditEvent)
        => Emit(logger, LogLevel.Warning, auditEvent, LogDeleteAccessErrorCore);

    [LoggerMessage(EventId = 7515, Level = LogLevel.Warning, Message = "Delete access error {@AuditEvent}", SkipEnabledCheck = true, EventName = nameof(LogDeleteAccessError))]
    private static partial void LogDeleteAccessErrorCore(ILogger<AccessTelemetryCategory> logger, AccessTelemetryEvent auditEvent);

    public static void LogTenantLifecycleAccessError(ILogger<AccessTelemetryCategory> logger, AccessTelemetryEvent auditEvent)
        => Emit(logger, LogLevel.Warning, auditEvent, LogTenantLifecycleAccessErrorCore);

    [LoggerMessage(EventId = 7516, Level = LogLevel.Warning, Message = "Tenant lifecycle access error {@AuditEvent}", SkipEnabledCheck = true, EventName = nameof(LogTenantLifecycleAccessError))]
    private static partial void LogTenantLifecycleAccessErrorCore(ILogger<AccessTelemetryCategory> logger, AccessTelemetryEvent auditEvent);

    public static void LogTenantConfigAccessError(ILogger<AccessTelemetryCategory> logger, AccessTelemetryEvent auditEvent)
        => Emit(logger, LogLevel.Warning, auditEvent, LogTenantConfigAccessErrorCore);

    [LoggerMessage(EventId = 7517, Level = LogLevel.Warning, Message = "Tenant config access error {@AuditEvent}", SkipEnabledCheck = true, EventName = nameof(LogTenantConfigAccessError))]
    private static partial void LogTenantConfigAccessErrorCore(ILogger<AccessTelemetryCategory> logger, AccessTelemetryEvent auditEvent);

    public static void LogCaseMemberAccessError(ILogger<AccessTelemetryCategory> logger, AccessTelemetryEvent auditEvent)
        => Emit(logger, LogLevel.Warning, auditEvent, LogCaseMemberAccessErrorCore);

    [LoggerMessage(EventId = 7518, Level = LogLevel.Warning, Message = "Case member access error {@AuditEvent}", SkipEnabledCheck = true, EventName = nameof(LogCaseMemberAccessError))]
    private static partial void LogCaseMemberAccessErrorCore(ILogger<AccessTelemetryCategory> logger, AccessTelemetryEvent auditEvent);

    public static void LogAnnotationAccessError(ILogger<AccessTelemetryCategory> logger, AccessTelemetryEvent auditEvent)
        => Emit(logger, LogLevel.Warning, auditEvent, LogAnnotationAccessErrorCore);

    [LoggerMessage(EventId = 7519, Level = LogLevel.Warning, Message = "Annotation access error {@AuditEvent}", SkipEnabledCheck = true, EventName = nameof(LogAnnotationAccessError))]
    private static partial void LogAnnotationAccessErrorCore(ILogger<AccessTelemetryCategory> logger, AccessTelemetryEvent auditEvent);

    private static void Emit(
        ILogger<AccessTelemetryCategory> logger,
        LogLevel level,
        AccessTelemetryEvent auditEvent,
        Action<ILogger<AccessTelemetryCategory>, AccessTelemetryEvent> log)
    {
        if (logger.IsEnabled(level))
        {
            log(logger, AccessTelemetrySanitizer.SanitizeForProviders(auditEvent));
        }
    }
}
