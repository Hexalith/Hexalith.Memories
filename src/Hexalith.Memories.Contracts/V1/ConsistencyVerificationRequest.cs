// <copyright file="ConsistencyVerificationRequest.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Contracts.V1;

/// <summary>
/// Request payload for <c>POST /api/v1/tenants/{tenantId}/consistency/verify</c>.
/// Body is optional — an empty body is accepted and defaults <see cref="BatchSize"/>
/// to <c>null</c> (the workflow then uses the default of 500).
/// </summary>
/// <param name="TenantId">The tenant to audit.</param>
/// <param name="BatchSize">
/// Optional per-batch size for fan-out. Must be in [10, 5000] when provided; the endpoint
/// returns 400 for out-of-range values.
/// </param>
public sealed record ConsistencyVerificationRequest(string TenantId, int? BatchSize = null);
