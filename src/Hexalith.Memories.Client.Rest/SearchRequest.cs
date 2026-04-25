// <copyright file="SearchRequest.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Client.Rest;

/// <summary>
/// Request DTO for a single-axis search (<c>axis ∈ {syntactic, semantic, graph}</c>). Consumed by
/// <see cref="MemoriesClient.SearchAsync"/>; translated into a query string that omits server-default values.
/// </summary>
/// <param name="TenantId">The tenant identifier. Required.</param>
/// <param name="Axis">The axis name — <c>syntactic</c>, <c>semantic</c>, or <c>graph</c>.</param>
/// <param name="Query">The free-text query; nullable for <c>graph</c>-only searches.</param>
/// <param name="CaseId">The case identifier, or null to search the tenant globally.</param>
/// <param name="MaxResults">Max rows to return. Server default is 10; omit when equal to avoid wire noise.</param>
/// <param name="Explain">When <see langword="true"/>, ask the server for explain metadata.</param>
/// <param name="TokenBudget">Optional maximum output tokens; null means no server-side budget truncation.</param>
public sealed record SearchRequest(
    string TenantId,
    string Axis,
    string? Query,
    string? CaseId = null,
    int MaxResults = 10,
    bool Explain = false,
    int? TokenBudget = null);
