// <copyright file="TenantStatusEndpointFilter.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Endpoints;

using Hexalith.Memories.Contracts.V1;
using Hexalith.Memories.Server.Tenants;

using Microsoft.AspNetCore.Http;

/// <summary>Endpoint filter that applies tenant active-only or existence-only registry validation.</summary>
internal sealed class TenantStatusEndpointFilter(
    TenantStatusGuard tenantGuard,
    TenantStatusValidationMode mode) : IEndpointFilter
{
    /// <summary>Creates an active-only tenant-status endpoint-filter delegate.</summary>
    /// <returns>The endpoint-filter delegate.</returns>
    public static Func<EndpointFilterInvocationContext, EndpointFilterDelegate, ValueTask<object?>> ActiveOnly()
        => For(TenantStatusValidationMode.ActiveOnly);

    /// <summary>Creates an exists-only tenant-status endpoint-filter delegate.</summary>
    /// <returns>The endpoint-filter delegate.</returns>
    public static Func<EndpointFilterInvocationContext, EndpointFilterDelegate, ValueTask<object?>> ExistsOnly()
        => For(TenantStatusValidationMode.ExistsOnly);

    /// <inheritdoc />
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(next);

        if (!EndpointValidationHelpers.TryResolveTenantId(context, out string? tenantId)
            || tenantId is null)
        {
            return await next(context).ConfigureAwait(false);
        }

        ErrorResponse? validationError = EndpointValidationHelpers.ValidateTenantId(tenantId);
        if (validationError is not null)
        {
            return ErrorResults.InvalidTenantIdResult(validationError.Message);
        }

        ErrorResponse? error = mode == TenantStatusValidationMode.ActiveOnly
            ? await tenantGuard.ValidateTenantActiveAsync(tenantId, context.HttpContext.RequestAborted).ConfigureAwait(false)
            : await tenantGuard.ValidateTenantExistsAsync(tenantId, context.HttpContext.RequestAborted).ConfigureAwait(false);

        return error is null
            ? await next(context).ConfigureAwait(false)
            : ErrorResults.TenantStatusResult(error);
    }

    private static Func<EndpointFilterInvocationContext, EndpointFilterDelegate, ValueTask<object?>> For(
        TenantStatusValidationMode validationMode)
        => async (context, next) =>
        {
            TenantStatusGuard guard = context.HttpContext.RequestServices.GetRequiredService<TenantStatusGuard>();
            var filter = new TenantStatusEndpointFilter(guard, validationMode);
            return await filter.InvokeAsync(context, next).ConfigureAwait(false);
        };
}
