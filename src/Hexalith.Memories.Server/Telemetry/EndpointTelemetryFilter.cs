// <copyright file="EndpointTelemetryFilter.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Telemetry;

using Hexalith.Memories.Server.Endpoints;
using Hexalith.Memories.Telemetry;

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

/// <summary>Endpoint filter that centralizes activity creation, audit scope lifetime, result marking, and exception marking.</summary>
internal sealed class EndpointTelemetryFilter(
    EndpointTelemetryDescriptor descriptor,
    ILogger<AccessTelemetryCategory> auditLogger) : IEndpointFilter
{
    /// <summary>Creates a reusable endpoint-filter delegate for the supplied descriptor.</summary>
    /// <param name="descriptor">The endpoint telemetry descriptor.</param>
    /// <returns>The endpoint-filter delegate.</returns>
    public static Func<EndpointFilterInvocationContext, EndpointFilterDelegate, ValueTask<object?>> For(
        EndpointTelemetryDescriptor descriptor)
    {
        ArgumentNullException.ThrowIfNull(descriptor);

        return async (context, next) =>
        {
            ILogger<AccessTelemetryCategory> logger =
                context.HttpContext.RequestServices.GetRequiredService<ILogger<AccessTelemetryCategory>>();
            var filter = new EndpointTelemetryFilter(descriptor, logger);
            return await filter.InvokeAsync(context, next).ConfigureAwait(false);
        };
    }

    /// <inheritdoc />
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(next);

        string? tenantId = descriptor.TenantIdResolver(context);
        string? caseId = descriptor.CaseIdResolver(context);
        IReadOnlyDictionary<string, object?> queryParams = descriptor.QueryParamsFactory(context);

        using EndpointTelemetryScope scope = EndpointTelemetryHelpers.CreateEndpointAuditScope(
            auditLogger,
            context.HttpContext,
            descriptor.ActivityName,
            descriptor.OperationType,
            descriptor.SuccessEventId,
            descriptor.ErrorEventId,
            tenantId,
            caseId,
            queryParams,
            descriptor.RecordMetricOnDispose);

        try
        {
            descriptor.ConfigureActivity?.Invoke(scope.Activity, context, scope);
            scope.Activity?.SetTag(MemoriesActivitySource.TagOperation, descriptor.OperationType);

            object? result = await next(context).ConfigureAwait(false);
            if (result is IResult httpResult)
            {
                EndpointTelemetryHelpers.MarkAuditFromHttpResult(scope, httpResult, context.HttpContext);
            }

            return result;
        }
        catch (Exception ex)
        {
            scope.MarkUnhandledException(ex);
            throw;
        }
    }
}
