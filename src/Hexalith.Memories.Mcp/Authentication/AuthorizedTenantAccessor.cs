// <copyright file="AuthorizedTenantAccessor.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Mcp.Authentication;

using Microsoft.AspNetCore.Http;

/// <summary>Reads the request-scoped tenant snapshot approved by MCP authorization.</summary>
internal interface IAuthorizedTenantAccessor
{
    /// <summary>Tries to read the authorized tenant identifier from the current HTTP context.</summary>
    /// <param name="tenantId">The authorized tenant identifier.</param>
    /// <returns><c>true</c> when an authorized tenant was captured for the request.</returns>
    bool TryGetAuthorizedTenant(out string tenantId);
}

/// <summary>Default <see cref="IAuthorizedTenantAccessor"/> implementation.</summary>
internal sealed class AuthorizedTenantAccessor(IHttpContextAccessor httpContextAccessor) : IAuthorizedTenantAccessor
{
    /// <summary>HTTP context item key containing the authorized tenant id.</summary>
    internal const string HttpContextItemKey = "Mcp.AuthorizedTenant";

    /// <inheritdoc />
    public bool TryGetAuthorizedTenant(out string tenantId)
    {
        object? value = httpContextAccessor.HttpContext?.Items[HttpContextItemKey];
        if (value is string captured && !string.IsNullOrWhiteSpace(captured))
        {
            tenantId = captured;
            return true;
        }

        tenantId = string.Empty;
        return false;
    }
}
