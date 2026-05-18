// <copyright file="FalkorDbCompatibilityExtensions.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

using NFalkorDB;

using StackExchange.Redis;

/// <summary>Compatibility helpers for the graph-id query API used by earlier NFalkorDB versions.</summary>
public static class FalkorDbCompatibilityExtensions
{
    /// <summary>Executes a query against the selected FalkorDB graph.</summary>
    /// <param name="falkorDb">The FalkorDB client.</param>
    /// <param name="graphId">The graph identifier.</param>
    /// <param name="query">The Cypher query.</param>
    /// <param name="parameters">The query parameters.</param>
    /// <param name="flags">The Redis command flags.</param>
    /// <param name="timeout">The query timeout.</param>
    /// <returns>The FalkorDB result set.</returns>
    public static Task<ResultSet> QueryAsync(
        this FalkorDB falkorDb,
        string graphId,
        string query,
        IDictionary<string, object>? parameters = null,
        CommandFlags flags = CommandFlags.None,
        long timeout = 0)
    {
        ArgumentNullException.ThrowIfNull(falkorDb);
        ArgumentException.ThrowIfNullOrWhiteSpace(graphId);
        ArgumentNullException.ThrowIfNull(query);

        return falkorDb.SelectGraph(graphId).QueryAsync(query, parameters ?? new Dictionary<string, object>(), flags, timeout);
    }
}
