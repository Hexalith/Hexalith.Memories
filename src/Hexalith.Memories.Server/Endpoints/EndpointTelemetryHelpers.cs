// <copyright file="EndpointTelemetryHelpers.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Endpoints;

using System.Diagnostics;
using System.Globalization;

using Hexalith.Memories.Contracts.V1;
using Hexalith.Memories.Server.Telemetry;
using Hexalith.Memories.Telemetry;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.Logging;

/// <summary>Shared audit and telemetry helpers for the decomposed endpoint mappings.</summary>
internal static class EndpointTelemetryHelpers
{
    internal const string EndpointAuditEmittedItemKey = "__Hexalith.Memories.EndpointAuditEmitted";

    internal const string EndpointTelemetryErrorCodeItemKey = "__Hexalith.Memories.EndpointTelemetryErrorCode";

    internal static IReadOnlyDictionary<string, object?> CreateIngestAuditQueryParams(
        SourceType sourceType,
        string? contentType,
        long? bytes)
        => new Dictionary<string, object?>(System.StringComparer.Ordinal)
        {
            ["sourceType"] = sourceType.ToString(),
            ["contentType"] = string.IsNullOrWhiteSpace(contentType) ? null : contentType,
            ["bytes"] = bytes ?? 0,
        };

    internal static string ResolvePrincipalAuditUser(HttpContext httpContext, System.Diagnostics.Activity? activity)
        => AuditPrincipalResolver.Resolve(httpContext, activity);

    internal static EndpointTelemetryScope CreateEndpointAuditScope(
        ILogger<AccessTelemetryCategory> auditLogger,
        HttpContext httpContext,
        string activityName,
        string operationType,
        int successEventId,
        int errorEventId,
        string? tenantId,
        string? caseId,
        IReadOnlyDictionary<string, object?> queryParams,
        Action<EndpointTelemetryScope>? recordMetricOnDispose = null)
    {
        Activity? activity = MemoriesActivitySource.Instance.StartActivity(activityName);
        return CreateEndpointAuditScope(
            auditLogger,
            httpContext,
            activity,
            operationType,
            successEventId,
            errorEventId,
            tenantId,
            caseId,
            queryParams,
            recordMetricOnDispose,
            disposeActivityOnDispose: true);
    }

    internal static EndpointTelemetryScope CreateEndpointAuditScope(
        ILogger<AccessTelemetryCategory> auditLogger,
        HttpContext httpContext,
        System.Diagnostics.Activity? activity,
        string operationType,
        int successEventId,
        int errorEventId,
        string? tenantId,
        string? caseId,
        IReadOnlyDictionary<string, object?> queryParams,
        Action<EndpointTelemetryScope>? recordMetricOnDispose = null,
        bool disposeActivityOnDispose = false)
    {
        string tenantIdTag = string.IsNullOrWhiteSpace(tenantId) ? MemoriesMeter.RejectedTenantTag : tenantId;
        activity?.SetTag(MemoriesActivitySource.TagOperation, operationType);
        activity?.SetTag(MemoriesActivitySource.TagTenantId, tenantIdTag);
        if (!string.IsNullOrWhiteSpace(caseId))
        {
            activity?.SetTag(MemoriesActivitySource.TagCaseId, caseId);
        }

        var scope = new EndpointTelemetryScope(
            auditLogger,
            activity,
            operationType,
            successEventId,
            errorEventId,
            tenantIdTag,
            recordMetricOnDispose,
            _ => MarkEndpointAuditEmitted(httpContext),
            disposeActivityOnDispose);
        scope.CaseId = caseId;
        scope.User = ResolvePrincipalAuditUser(httpContext, activity);
        scope.QueryParams = queryParams;
        return scope;
    }

    internal static Dictionary<string, object?> CreateAuditQueryParams(string operation)
        => new(StringComparer.Ordinal)
        {
            ["operation"] = operation,
        };

    internal static Dictionary<string, object?> CreateWorkflowStatusAuditQueryParams(string operation, string instanceId)
        => new(StringComparer.Ordinal)
        {
            ["operation"] = operation,
            ["workflowInstanceIdPrefix"] = PrefixIdentifier(instanceId, 32),
        };

    internal static void MarkAuditFromHttpResult(EndpointTelemetryScope scope, IResult result, HttpContext? httpContext = null)
    {
        if (result is IStatusCodeHttpResult statusCodeResult
            && statusCodeResult.StatusCode is int statusCode
            && statusCode >= StatusCodes.Status400BadRequest)
        {
            if (httpContext is not null
                && httpContext.Items.TryGetValue(EndpointTelemetryErrorCodeItemKey, out object? errorCode)
                && errorCode is string explicitErrorCode
                && !string.IsNullOrWhiteSpace(explicitErrorCode))
            {
                scope.MarkValidationError(explicitErrorCode);
                return;
            }

            if (result is IValueHttpResult valueResult && valueResult.Value is ErrorResponse errorResponse)
            {
                scope.MarkValidationError(errorResponse.Code);
                return;
            }

            scope.MarkValidationError(statusCode switch
            {
                StatusCodes.Status404NotFound => "NOT_FOUND",
                StatusCodes.Status409Conflict => "CONFLICT",
                StatusCodes.Status503ServiceUnavailable => "DAPR_UNAVAILABLE",
                _ => "HTTP_" + statusCode.ToString(CultureInfo.InvariantCulture),
            });
        }
    }

    internal static bool HasEndpointAuditEmitted(HttpContext httpContext)
    {
        ArgumentNullException.ThrowIfNull(httpContext);

        return httpContext.Items.TryGetValue(EndpointAuditEmittedItemKey, out object? emitted)
            && emitted is true;
    }

    internal static void MarkEndpointTelemetryErrorCode(HttpContext httpContext, string errorCode)
    {
        ArgumentNullException.ThrowIfNull(httpContext);
        ArgumentException.ThrowIfNullOrWhiteSpace(errorCode);

        httpContext.Items[EndpointTelemetryErrorCodeItemKey] = errorCode;
    }

    internal static void MarkEndpointAuditEmitted(HttpContext httpContext)
    {
        ArgumentNullException.ThrowIfNull(httpContext);

        httpContext.Items[EndpointAuditEmittedItemKey] = true;
    }

    internal static string PrefixIdentifier(string value, int maxLength)
        => string.IsNullOrWhiteSpace(value)
            ? string.Empty
            : value[..Math.Min(value.Length, maxLength)];
}
