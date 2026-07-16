// <copyright file="TenantIndexStatus.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Contracts.V1;

using System.Text.Json.Serialization;

/// <summary>
/// Per-retrieval-axis health state for a tenant (Story 5.5 AC1/AC2).
/// Paired with <see cref="TenantIndexSizes"/> for operator listing;
/// <see cref="IndexHealth.Unknown"/> on an axis implies the corresponding
/// count in <see cref="TenantIndexSizes"/> is <see langword="null"/>.
/// </summary>
/// <param name="Syntactic">Health of the syntactic index.</param>
/// <param name="Semantic">Health of the semantic index.</param>
/// <param name="Graph">Health of the graph index.</param>
public sealed record TenantIndexStatus(
    [property: JsonPropertyName("rediSearch")] IndexHealth Syntactic,
    [property: JsonPropertyName("redisVector")] IndexHealth Semantic,
    [property: JsonPropertyName("falkorDb")] IndexHealth Graph);
