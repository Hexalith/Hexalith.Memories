// <copyright file="TenantIndexSizes.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Contracts.V1;

/// <summary>
/// Per-backend document / node counts for a tenant (Story 5.5 AC1).
/// Nullable values indicate the backend was unavailable when the count was computed;
/// availability is signalled in parallel by <see cref="IndexHealth.Unknown"/> on the
/// corresponding axis of <see cref="TenantIndexStatus"/>.
/// </summary>
/// <param name="RediSearchKeyCount">Number of documents indexed in the tenant's RediSearch syntactic index, or null if unavailable.</param>
/// <param name="RedisVectorKeyCount">Number of documents indexed in the tenant's Redis Vector semantic index, or null if unavailable.</param>
/// <param name="FalkorDbNodeCount">Number of nodes in the tenant's FalkorDB graph, or null if unavailable.</param>
public sealed record TenantIndexSizes(
    long? RediSearchKeyCount,
    long? RedisVectorKeyCount,
    long? FalkorDbNodeCount);
