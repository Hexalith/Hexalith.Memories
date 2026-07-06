// <copyright file="EndpointTelemetryDescriptor.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Telemetry;

using System.Diagnostics;

using Hexalith.Memories.Server.Endpoints;

using Microsoft.AspNetCore.Http;

/// <summary>Describes the audit, activity, and metric behavior an endpoint telemetry filter should apply.</summary>
/// <param name="OperationType">The stable audit operation type.</param>
/// <param name="ActivityName">The stable activity name.</param>
/// <param name="SuccessEventId">The audit event id used for successful or partial outcomes.</param>
/// <param name="ErrorEventId">The audit event id used for error outcomes.</param>
internal sealed record EndpointTelemetryDescriptor(
    string OperationType,
    string ActivityName,
    int SuccessEventId,
    int ErrorEventId)
{
    /// <summary>Gets the tenant-id resolver for the endpoint invocation.</summary>
    public Func<EndpointFilterInvocationContext, string?> TenantIdResolver { get; init; } =
        static context => EndpointValidationHelpers.TryResolveTenantId(context, out string? tenantId) ? tenantId : null;

    /// <summary>Gets the optional case-id resolver for the endpoint invocation.</summary>
    public Func<EndpointFilterInvocationContext, string?> CaseIdResolver { get; init; } =
        static context => context.HttpContext.Request.RouteValues.TryGetValue("caseId", out object? value) ? value?.ToString() : null;

    /// <summary>Gets the operation-specific audit parameter factory.</summary>
    public Func<EndpointFilterInvocationContext, IReadOnlyDictionary<string, object?>> QueryParamsFactory { get; init; } =
        static _ => new Dictionary<string, object?>(0);

    /// <summary>Gets the optional metric callback invoked when the telemetry scope is disposed.</summary>
    public Action<EndpointTelemetryScope>? RecordMetricOnDispose { get; init; }

    /// <summary>Gets the optional activity tag configurator.</summary>
    public Action<Activity?, EndpointFilterInvocationContext, EndpointTelemetryScope>? ConfigureActivity { get; init; }
}
