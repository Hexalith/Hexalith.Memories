// <copyright file="MemoriesServerExceptionHandler.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Diagnostics;

using System.Diagnostics;

using Hexalith.Memories.Contracts.V1;
using Hexalith.Memories.Server.Activities.Indexing;
using Hexalith.Memories.Server.Endpoints;
using Hexalith.Memories.Server.Telemetry;
using Hexalith.Memories.Telemetry;

using Microsoft.AspNetCore.Diagnostics;

/// <summary>Maps unhandled server exceptions to the public sanitized <see cref="ErrorResponse"/> envelope.</summary>
internal sealed partial class MemoriesServerExceptionHandler(
    ILogger<MemoriesServerExceptionHandler> logger,
    ILogger<AccessTelemetryCategory> auditLogger) : IExceptionHandler
{
    private static readonly IReadOnlyDictionary<string, object?> EmptyAuditParams = new Dictionary<string, object?>(0);

    /// <inheritdoc />
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(httpContext);
        ArgumentNullException.ThrowIfNull(exception);

        if (httpContext.Response.HasStarted)
        {
            LogResponseAlreadyStarted(logger, exception);
            return false;
        }

        LogUnhandledException(logger, exception);
        EmitFallbackAuditIfNeeded(httpContext);
        httpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;
        await httpContext.Response.WriteAsJsonAsync(
            ErrorResults.UnhandledException(),
            cancellationToken).ConfigureAwait(false);
        return true;
    }

    private void EmitFallbackAuditIfNeeded(HttpContext httpContext)
    {
        if (EndpointTelemetryHelpers.HasEndpointAuditEmitted(httpContext))
        {
            return;
        }

        string tenantId = ResolveTenantTag(httpContext);
        string? caseId = httpContext.Request.RouteValues.TryGetValue("caseId", out object? caseValue)
            ? caseValue?.ToString()
            : null;
        Activity? activity = Activity.Current;
        string operation = activity?.GetTagItem(MemoriesActivitySource.TagOperation)?.ToString()
            ?? "unhandled-exception";
        string user = AuditPrincipalResolver.Resolve(httpContext, activity);
        AccessTelemetryEvent auditEvent = AccessTelemetryLog.CreateEvent(
            ResolveErrorEventId(operation),
            tenantId,
            operation,
            caseId,
            user,
            EmptyAuditParams,
            resultCount: null,
            ResolveDurationMs(activity),
            AccessTelemetryLog.OutcomeError,
            "UNHANDLED_EXCEPTION",
            activity);

        LogFallbackAudit(operation, auditEvent);
        EndpointTelemetryHelpers.MarkEndpointAuditEmitted(httpContext);
    }

    private void LogFallbackAudit(string operation, AccessTelemetryEvent auditEvent)
    {
        switch (operation)
        {
            case AccessTelemetryLog.OperationIngest:
                AccessTelemetryLog.LogIngestAccessError(auditLogger, auditEvent);
                break;
            case AccessTelemetryLog.OperationTraverse:
                AccessTelemetryLog.LogTraverseAccessError(auditLogger, auditEvent);
                break;
            case AccessTelemetryLog.OperationCaseAccess:
                AccessTelemetryLog.LogCaseAccessError(auditLogger, auditEvent);
                break;
            case AccessTelemetryLog.OperationDelete:
                AccessTelemetryLog.LogDeleteAccessError(auditLogger, auditEvent);
                break;
            case AccessTelemetryLog.OperationTenantLifecycle:
                AccessTelemetryLog.LogTenantLifecycleAccessError(auditLogger, auditEvent);
                break;
            case AccessTelemetryLog.OperationTenantConfig:
                AccessTelemetryLog.LogTenantConfigAccessError(auditLogger, auditEvent);
                break;
            case AccessTelemetryLog.OperationCaseMember:
                AccessTelemetryLog.LogCaseMemberAccessError(auditLogger, auditEvent);
                break;
            case AccessTelemetryLog.OperationAnnotation:
                AccessTelemetryLog.LogAnnotationAccessError(auditLogger, auditEvent);
                break;
            default:
                AccessTelemetryLog.LogSearchAccessError(auditLogger, auditEvent);
                break;
        }
    }

    private static int ResolveErrorEventId(string operation)
        => operation switch
        {
            AccessTelemetryLog.OperationIngest => 7512,
            AccessTelemetryLog.OperationTraverse => 7513,
            AccessTelemetryLog.OperationCaseAccess => 7514,
            AccessTelemetryLog.OperationDelete => 7515,
            AccessTelemetryLog.OperationTenantLifecycle => 7516,
            AccessTelemetryLog.OperationTenantConfig => 7517,
            AccessTelemetryLog.OperationCaseMember => 7518,
            AccessTelemetryLog.OperationAnnotation => 7519,
            _ => 7511,
        };

    private static long ResolveDurationMs(Activity? activity)
    {
        if (activity is null || activity.StartTimeUtc == default)
        {
            return 0;
        }

        double elapsed = (DateTime.UtcNow - activity.StartTimeUtc).TotalMilliseconds;
        return elapsed <= 0 ? 0 : (long)elapsed;
    }

    private static string ResolveTenantTag(HttpContext httpContext)
    {
        string? tenantId = null;
        if (httpContext.Request.RouteValues.TryGetValue("tenantId", out object? routeValue)
            && routeValue is not null
            && !string.IsNullOrWhiteSpace(routeValue.ToString()))
        {
            tenantId = routeValue.ToString();
        }
        else if (httpContext.Request.Query.TryGetValue("tenantId", out Microsoft.Extensions.Primitives.StringValues queryValue)
            && !string.IsNullOrWhiteSpace(queryValue.ToString()))
        {
            tenantId = queryValue.ToString();
        }

        if (string.IsNullOrWhiteSpace(tenantId))
        {
            return MemoriesMeter.RejectedTenantTag;
        }

        try
        {
            TenantIdGuard.Validate(tenantId);
            return tenantId;
        }
        catch (ArgumentException)
        {
            return MemoriesMeter.RejectedTenantTag;
        }
    }

    [LoggerMessage(
        EventId = 7520,
        Level = LogLevel.Error,
        Message = "Unhandled Memories Server exception.")]
    private static partial void LogUnhandledException(ILogger logger, Exception exception);

    [LoggerMessage(
        EventId = 7521,
        Level = LogLevel.Error,
        Message = "Unhandled Memories Server exception occurred after the response started.")]
    private static partial void LogResponseAlreadyStarted(ILogger logger, Exception exception);
}
