// <copyright file="ITenantEventRouteCacheInvalidator.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.EventStore;

/// <summary>Invalidates process-local tenant event routes after projection cleanup removes a case.</summary>
public interface ITenantEventRouteCacheInvalidator
{
    /// <summary>Removes cached aggregate routes for the deleted case in one server process.</summary>
    /// <param name="tenantId">The tenant identifier.</param>
    /// <param name="caseId">The deleted case identifier.</param>
    void InvalidateCaseRoutes(string tenantId, string caseId);
}
