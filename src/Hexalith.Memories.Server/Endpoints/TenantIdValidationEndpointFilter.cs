// <copyright file="TenantIdValidationEndpointFilter.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Endpoints;

using Hexalith.Memories.Contracts.V1;

using Microsoft.AspNetCore.Http;

/// <summary>Endpoint filter that rejects resolved tenant ids failing <c>TenantIdGuard</c> validation.</summary>
internal sealed class TenantIdValidationEndpointFilter : IEndpointFilter
{
    /// <summary>Creates a reusable endpoint-filter delegate.</summary>
    /// <returns>The endpoint-filter delegate.</returns>
    public static Func<EndpointFilterInvocationContext, EndpointFilterDelegate, ValueTask<object?>> For()
        => static async (context, next) =>
        {
            var filter = new TenantIdValidationEndpointFilter();
            return await filter.InvokeAsync(context, next).ConfigureAwait(false);
        };

    /// <inheritdoc />
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(next);

        if (EndpointValidationHelpers.TryResolveTenantId(context, out string? tenantId))
        {
            ErrorResponse? error = EndpointValidationHelpers.ValidateTenantId(tenantId);
            if (error is not null)
            {
                return ErrorResults.InvalidTenantIdResult(error.Message);
            }
        }

        return await next(context).ConfigureAwait(false);
    }
}
