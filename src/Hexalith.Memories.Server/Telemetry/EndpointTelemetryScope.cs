// <copyright file="EndpointTelemetryScope.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Telemetry;

using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Metrics;

using Hexalith.Memories.Contracts.V1;
using Hexalith.Memories.Telemetry;

using Microsoft.Extensions.Logging;

/// <summary>
/// Story 7.5 — uniform try/finally helper that guarantees one audit event emission per endpoint invocation
/// regardless of exit path (success, validation-rejection, tenant-guard rejection, exception). Callers
/// configure <see cref="TenantIdTag"/>, <see cref="ResultCount"/>, <see cref="Outcome"/>, and
/// <see cref="ErrorCode"/> as information becomes available, then <c>Dispose</c> emits the audit event +
/// records the outcome metric.
/// </summary>
internal sealed class EndpointTelemetryScope : IDisposable
{
    private static readonly IReadOnlyDictionary<string, object?> EmptyParams =
        new Dictionary<string, object?>(0);

    private static readonly HashSet<string> RejectedTenantErrorCodes = new(StringComparer.Ordinal)
    {
        "INVALID_TENANT_ID",
        "TENANT_NOT_FOUND",
        "TENANT_DELETING",
        "TENANT_PROVISIONING",
        "TENANT_FAILED",
        "TENANT_UNAVAILABLE",
    };

    private readonly ILogger<AccessTelemetryCategory> _logger;
    private readonly Activity? _activity;
    private readonly Stopwatch _stopwatch;
    private readonly string _operationType;
    private readonly int _successEventId;
    private readonly int _errorEventId;
    private readonly Action<EndpointTelemetryScope>? _recordMetricOnDispose;
    private bool _disposed;

    public EndpointTelemetryScope(
        ILogger<AccessTelemetryCategory> logger,
        Activity? activity,
        string operationType,
        int successEventId,
        int errorEventId,
        string tenantIdTag,
        Action<EndpointTelemetryScope>? recordMetricOnDispose = null)
    {
        _logger = logger;
        _activity = activity;
        _operationType = operationType;
        _successEventId = successEventId;
        _errorEventId = errorEventId;
        _stopwatch = Stopwatch.StartNew();
        _recordMetricOnDispose = recordMetricOnDispose;
        TenantIdTag = tenantIdTag;
        User = AccessTelemetryLog.UserAnonymous;
        Outcome = AccessTelemetryLog.OutcomeOk;
        QueryParams = EmptyParams;
    }

    /// <summary>Gets or sets the tenant id that will be recorded on the audit event AND as a metric tag.</summary>
    public string TenantIdTag { get; set; }

    /// <summary>Gets or sets the case id (null when the operation is not case-scoped).</summary>
    public string? CaseId { get; set; }

    /// <summary>Gets or sets the user identity (ADR-7.5-004).</summary>
    public string User { get; set; }

    /// <summary>Gets or sets the operation-specific param dictionary.</summary>
    public IReadOnlyDictionary<string, object?> QueryParams { get; set; }

    /// <summary>Gets or sets the number of results returned (null for write/schedule operations).</summary>
    public int? ResultCount { get; set; }

    /// <summary>Gets or sets the outcome constant (<see cref="AccessTelemetryLog.OutcomeOk"/> etc.).</summary>
    public string Outcome { get; set; }

    /// <summary>Gets or sets the error code (catalog or synthetic) when outcome is error/partial.</summary>
    public string? ErrorCode { get; set; }

    /// <summary>Gets the activity started for this scope (may be null if no listener attached).</summary>
    public Activity? Activity => _activity;

    /// <summary>Gets the elapsed milliseconds captured at dispose time.</summary>
    public long ElapsedMs { get; private set; }

    /// <summary>Mark the scope as having been rejected by the tenant guard — tenant tag becomes synthetic __rejected__.</summary>
    public void MarkTenantRejected(string errorCode)
    {
        TenantIdTag = MemoriesMeter.RejectedTenantTag;
        Outcome = AccessTelemetryLog.OutcomeError;
        ErrorCode = errorCode;
    }

    /// <summary>Mark the scope as having encountered a validation error.</summary>
    public void MarkValidationError(string errorCode)
    {
        if (RejectedTenantErrorCodes.Contains(errorCode))
        {
            TenantIdTag = MemoriesMeter.RejectedTenantTag;
        }

        Outcome = AccessTelemetryLog.OutcomeError;
        ErrorCode = errorCode;
    }

    /// <summary>Mark the scope as having partially succeeded (hybrid degradation path).</summary>
    public void MarkPartial(string? errorCode = null)
    {
        Outcome = AccessTelemetryLog.OutcomePartial;
        ErrorCode = errorCode;
    }

    /// <summary>Mark the scope as having failed with an unhandled exception.</summary>
    public void MarkUnhandledException(Exception exception, string? errorCode = null)
    {
        ArgumentNullException.ThrowIfNull(exception);

        if (Outcome == AccessTelemetryLog.OutcomeError && !string.IsNullOrWhiteSpace(ErrorCode))
        {
            return;
        }

        Outcome = AccessTelemetryLog.OutcomeError;
        ErrorCode = errorCode
            ?? (exception is OperationCanceledException ? "REQUEST_CANCELLED" : "UNHANDLED_EXCEPTION");
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _stopwatch.Stop();
        ElapsedMs = _stopwatch.ElapsedMilliseconds;

        _activity?.SetTag(MemoriesActivitySource.TagOutcome, Outcome);
        if (ErrorCode is not null)
        {
            _activity?.SetTag(MemoriesActivitySource.TagErrorCode, ErrorCode);
        }

        bool isError = Outcome != AccessTelemetryLog.OutcomeOk && Outcome != AccessTelemetryLog.OutcomePartial;
        _activity?.SetStatus(isError ? ActivityStatusCode.Error : ActivityStatusCode.Ok, isError ? ErrorCode : null);

        try
        {
            _recordMetricOnDispose?.Invoke(this);
        }
        catch
        {
            // Metric emission must never propagate — telemetry is additive.
        }

        int eventId = isError ? _errorEventId : _successEventId;

        AccessTelemetryEvent auditEvent = AccessTelemetryLog.CreateEvent(
            eventId,
            TenantIdTag,
            _operationType,
            CaseId,
            User,
            QueryParams,
            ResultCount,
            ElapsedMs,
            Outcome,
            ErrorCode,
            _activity);

        switch (_operationType)
        {
            case AccessTelemetryLog.OperationSearch:
                if (isError)
                {
                    AccessTelemetryLog.LogSearchAccessError(_logger, auditEvent);
                }
                else
                {
                    AccessTelemetryLog.LogSearchAccess(_logger, auditEvent);
                }

                break;
            case AccessTelemetryLog.OperationIngest:
                if (isError)
                {
                    AccessTelemetryLog.LogIngestAccessError(_logger, auditEvent);
                }
                else
                {
                    AccessTelemetryLog.LogIngestAccess(_logger, auditEvent);
                }

                break;
            case AccessTelemetryLog.OperationTraverse:
                if (isError)
                {
                    AccessTelemetryLog.LogTraverseAccessError(_logger, auditEvent);
                }
                else
                {
                    AccessTelemetryLog.LogTraverseAccess(_logger, auditEvent);
                }

                break;
            case AccessTelemetryLog.OperationCaseAccess:
                if (isError)
                {
                    AccessTelemetryLog.LogCaseAccessError(_logger, auditEvent);
                }
                else
                {
                    AccessTelemetryLog.LogCaseAccess(_logger, auditEvent);
                }

                break;
            case AccessTelemetryLog.OperationDelete:
                if (isError)
                {
                    AccessTelemetryLog.LogDeleteAccessError(_logger, auditEvent);
                }
                else
                {
                    AccessTelemetryLog.LogDeleteAccess(_logger, auditEvent);
                }

                break;
            default:
                // Unknown operation type — should never happen; emit via search channel as a safe default.
                AccessTelemetryLog.LogSearchAccess(_logger, auditEvent);
                break;
        }
    }
}
