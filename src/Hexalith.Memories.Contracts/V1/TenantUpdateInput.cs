// <copyright file="TenantUpdateInput.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Contracts.V1;

/// <summary>
/// Input body for <c>PATCH /api/tenants/{tenantId}</c> (Story 5.5 AC3 / FR42).
/// Currently carries only <see cref="DisplayName"/> (Amendment Q: rate-limit updates flow through
/// the existing <c>PUT /api/tenants/{tenantId}/embedding-config</c> endpoint — keeping the two
/// persistence targets separate avoids a cross-store partial-failure mode for zero user benefit).
/// </summary>
/// <param name="DisplayName">The new display name for the tenant.</param>
public sealed record TenantUpdateInput(string DisplayName);
