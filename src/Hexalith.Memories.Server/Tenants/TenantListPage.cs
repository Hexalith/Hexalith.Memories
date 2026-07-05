// <copyright file="TenantListPage.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Tenants;

using Hexalith.Memories.Contracts.V1;

/// <summary>A bounded page from the tenant registry index.</summary>
/// <param name="Tenants">The tenants in the requested page.</param>
/// <param name="TotalCount">The total number of tenant ids in the registry index.</param>
/// <param name="Offset">The clamped page offset.</param>
/// <param name="Limit">The clamped page limit.</param>
/// <param name="HasMore">Whether a subsequent page exists.</param>
public sealed record TenantListPage(
    IReadOnlyList<TenantInfo> Tenants,
    int TotalCount,
    int Offset,
    int Limit,
    bool HasMore);
