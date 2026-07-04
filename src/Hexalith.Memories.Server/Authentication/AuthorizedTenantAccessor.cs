// <copyright file="AuthorizedTenantAccessor.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Authentication;

using Microsoft.AspNetCore.Http;

/// <summary>Default <see cref="IAuthorizedTenantAccessor"/> implementation.</summary>
public sealed class AuthorizedTenantAccessor(IHttpContextAccessor httpContextAccessor) : IAuthorizedTenantAccessor
{
    /// <summary>HTTP context item key containing the authorized tenant id.</summary>
    public const string HttpContextItemKey = "Server.AuthorizedTenant";

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
