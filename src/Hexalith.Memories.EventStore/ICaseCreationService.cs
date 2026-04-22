// <copyright file="ICaseCreationService.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.EventStore;

/// <summary>Adapter over the Server-side case-management service. Delegates to <c>CaseService.CreateCaseAsync</c>
/// so this package does not take a compile-time reference on Server case types (ADR 9.1-D).</summary>
public interface ICaseCreationService
{
    /// <summary>Creates a new case in the given tenant with the specified name and returns the new case id.</summary>
    /// <param name="tenantId">The tenant that owns the case.</param>
    /// <param name="caseName">The human-readable case name. The router supplies a rendered name from
    /// <see cref="TenantEventRoutingOptions.CaseNameTemplate"/>.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The newly-created case id.</returns>
    Task<string> CreateCaseAsync(string tenantId, string caseName, CancellationToken cancellationToken);
}
