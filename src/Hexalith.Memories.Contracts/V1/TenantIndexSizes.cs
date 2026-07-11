// <copyright file="TenantIndexSizes.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Contracts.V1;

using System.Text.Json.Serialization;

/// <summary>
/// Per-retrieval-axis document / node counts for a tenant (Story 5.5 AC1).
/// Nullable values indicate the axis was unavailable when the count was computed;
/// availability is signalled in parallel by <see cref="IndexHealth.Unknown"/> on the
/// corresponding axis of <see cref="TenantIndexStatus"/>.
/// </summary>
/// <param name="SyntacticKeyCount">Number of documents indexed on the tenant's syntactic axis, or null if unavailable.</param>
/// <param name="SemanticKeyCount">Number of documents indexed on the tenant's semantic axis, or null if unavailable.</param>
/// <param name="GraphNodeCount">Number of nodes indexed on the tenant's graph axis, or null if unavailable.</param>
public sealed record TenantIndexSizes(
    [property: JsonPropertyName("rediSearchKeyCount")] long? SyntacticKeyCount,
    [property: JsonPropertyName("redisVectorKeyCount")] long? SemanticKeyCount,
    [property: JsonPropertyName("falkorDbNodeCount")] long? GraphNodeCount);
