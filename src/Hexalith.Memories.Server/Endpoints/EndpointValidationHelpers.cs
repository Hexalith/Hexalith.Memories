// <copyright file="EndpointValidationHelpers.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Endpoints;

using Hexalith.Memories.Contracts.V1;
using Hexalith.Memories.Server.Activities.Indexing;
using Hexalith.Memories.Server.Tenants;

/// <summary>Shared validation helpers for the decomposed endpoint mappings.</summary>
internal static class EndpointValidationHelpers
{
    internal static ErrorResponse? ValidateTenantId(string tenantId)
    {
        try
        {
            TenantIdGuard.Validate(tenantId);
            return null;
        }
        catch (ArgumentException ex)
        {
            return new ErrorResponse(
                "INVALID_TENANT_ID",
                ex.Message,
                "Use only alphanumeric characters and hyphens for tenant identifiers.");
        }
    }
}
