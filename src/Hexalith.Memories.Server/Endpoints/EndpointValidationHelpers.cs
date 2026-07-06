// <copyright file="EndpointValidationHelpers.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Endpoints;

using Hexalith.Memories.Contracts.V1;
using Hexalith.Memories.Server.Activities.Indexing;

using Microsoft.AspNetCore.Http;

/// <summary>Shared validation helpers for the decomposed endpoint mappings.</summary>
internal static class EndpointValidationHelpers
{
    /// <summary>Resolves a tenant id from route values, known body-bound endpoint contracts, or query string.</summary>
    /// <param name="context">The endpoint filter invocation context.</param>
    /// <param name="tenantId">The resolved tenant id, if one is present.</param>
    /// <returns><c>true</c> when a tenant value was found.</returns>
    internal static bool TryResolveTenantId(EndpointFilterInvocationContext context, out string? tenantId)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (context.HttpContext.Request.RouteValues.TryGetValue("tenantId", out object? routeValue)
            && routeValue is not null)
        {
            tenantId = routeValue.ToString();
            return true;
        }

        foreach (object? argument in context.Arguments)
        {
            switch (argument)
            {
                case IngestionInput input:
                    tenantId = input.TenantId;
                    return true;
                case UrlIngestionRequest request:
                    tenantId = request.TenantId;
                    return true;
                case DirectoryIngestionRequest request:
                    tenantId = request.TenantId;
                    return true;
                case TenantProvisioningInput input:
                    tenantId = input.TenantId;
                    return true;
                case TenantDeletionInput input:
                    tenantId = input.TenantId;
                    return true;
                case CreateCaseInput input:
                    tenantId = input.TenantId;
                    return true;
            }
        }

        if (context.HttpContext.Request.Query.TryGetValue("tenantId", out Microsoft.Extensions.Primitives.StringValues queryValue))
        {
            tenantId = queryValue.ToString();
            return true;
        }

        tenantId = null;
        return false;
    }

    /// <summary>Validates a tenant id through the canonical tenant-id guard.</summary>
    /// <param name="tenantId">The tenant identifier.</param>
    /// <returns>An error envelope when validation fails; otherwise <c>null</c>.</returns>
    internal static ErrorResponse? ValidateTenantId(string? tenantId)
    {
        if (string.IsNullOrWhiteSpace(tenantId))
        {
            return ErrorResults.InvalidTenantId("TenantId is required.");
        }

        try
        {
            TenantIdGuard.Validate(tenantId);
            return null;
        }
        catch (ArgumentException ex)
        {
            return ErrorResults.InvalidTenantId(ex.Message);
        }
    }
}
