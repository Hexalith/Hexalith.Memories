// <copyright file="FalkorDbCompatibilityExtensions.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

// Intentionally declared in the global namespace.
//
// Story 15.6 code review flagged "global-namespace pollution" as a smell, and the standard fix is to
// declare `namespace Hexalith.Memories.Redis;` here. That would break ~15 call sites in
// `Hexalith.Memories.Server.Activities.*` that invoke `falkor.QueryAsync(graphId, …)` without an
// explicit `using Hexalith.Memories.Redis;` and rely on global-namespace extension resolution to bridge
// the pre-`NFalkorDB` 1.0.6 graph-id-bound API shape.
//
// Touching those 15 caller files would breach Story 15.6's File Scope (only `Hexalith.Memories.Redis/`,
// `Hexalith.Memories.Server/Ingestion/ContentExtractionClient.cs`, `Hexalith.Memories.AppHost/`,
// `Hexalith.Memories.ServiceDefaults/`, and the test trees are allowed). The compatibility shim's
// class name (`FalkorDbCompatibilityExtensions`) is sufficiently distinctive that the practical
// collision risk against downstream `Hexalith.Memories.Redis` consumers is near-zero.
//
// Re-open trigger for re-scoping: a downstream consumer reports a name collision against this class
// in the global namespace, OR server code migrates to the post-1.0.6 `SelectGraph().QueryAsync()`
// shape and this shim becomes unused (delete the file rather than rehome it).
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
