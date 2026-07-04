// <copyright file="IAuthorizedTenantAccessor.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Authentication;

/// <summary>Reads the request-scoped tenant snapshot approved by Memories Server authorization.</summary>
public interface IAuthorizedTenantAccessor
{
    /// <summary>Tries to read the authorized tenant identifier from the current HTTP context.</summary>
    /// <param name="tenantId">The authorized tenant identifier.</param>
    /// <returns><c>true</c> when an authorized tenant was captured for the request.</returns>
    bool TryGetAuthorizedTenant(out string tenantId);
}
