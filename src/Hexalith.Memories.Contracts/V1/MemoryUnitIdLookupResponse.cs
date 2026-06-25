// <copyright file="MemoryUnitIdLookupResponse.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Contracts.V1;

/// <summary>
/// Story 18.5 — additive response body for the exact source-URI-keyed memory-unit lookup
/// (<c>GET /api/tenants/{tenantId}/cases/{caseId}/memory-units/by-source-uri</c>). Carries the canonical
/// <see cref="MemoryUnitId"/> resolved by exact key from the permanent dedup record. A genuine miss is
/// signalled by a structured <c>404</c> (<c>MEMORY_UNIT_NOT_FOUND</c>), never by a populated body with an
/// empty id.
/// </summary>
public sealed record MemoryUnitIdLookupResponse
{
    /// <summary>Gets the canonical memory-unit identifier mapped to the requested source URI.</summary>
    public required string MemoryUnitId { get; init; }
}
