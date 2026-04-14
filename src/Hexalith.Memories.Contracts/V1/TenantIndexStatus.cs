// <copyright file="TenantIndexStatus.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Contracts.V1;

/// <summary>
/// Per-backend health state for a tenant (Story 5.5 AC1/AC2).
/// Paired with <see cref="TenantIndexSizes"/> for operator listing;
/// <see cref="IndexHealth.Unknown"/> on an axis implies the corresponding
/// count in <see cref="TenantIndexSizes"/> is <see langword="null"/>.
/// </summary>
/// <param name="RediSearch">Health of the RediSearch syntactic index.</param>
/// <param name="RedisVector">Health of the Redis Vector semantic index.</param>
/// <param name="FalkorDb">Health of the FalkorDB graph.</param>
public sealed record TenantIndexStatus(
    IndexHealth RediSearch,
    IndexHealth RedisVector,
    IndexHealth FalkorDb);
