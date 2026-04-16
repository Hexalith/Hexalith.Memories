// <copyright file="HybridSearchRequest.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Client.Rest;

/// <summary>
/// Request DTO for a hybrid (multi-axis) search. Consumed by <see cref="MemoriesClient.HybridSearchAsync"/>.
/// Same wire conventions as <see cref="SearchRequest"/> — server defaults are omitted from the query string.
/// </summary>
/// <param name="TenantId">The tenant identifier. Required.</param>
/// <param name="Query">The free-text query. Required for hybrid search.</param>
/// <param name="CaseId">The case identifier, or null to search the tenant globally.</param>
/// <param name="MaxResults">Max rows to return. Server default is 10; omit when equal to avoid wire noise.</param>
/// <param name="Explain">When <see langword="true"/>, ask the server for explain metadata.</param>
public sealed record HybridSearchRequest(
    string TenantId,
    string Query,
    string? CaseId = null,
    int MaxResults = 10,
    bool Explain = false);
