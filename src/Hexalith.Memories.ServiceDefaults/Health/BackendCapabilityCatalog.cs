// <copyright file="BackendCapabilityCatalog.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.ServiceDefaults.Health;

/// <summary>Static mapping from health-check name to the operator-facing capabilities
/// impacted when the check fails. Kept in <c>ServiceDefaults</c> so the response writer
/// can consume it without forcing a <c>ServiceDefaults → Server</c> reference inversion.
/// Extend by adding an entry here whenever a new backend check is registered.</summary>
public static class BackendCapabilityCatalog
{
    /// <summary>Gets the immutable mapping from check name to affected capability ids.</summary>
    public static IReadOnlyDictionary<string, IReadOnlyList<string>> Map { get; } =
        new Dictionary<string, IReadOnlyList<string>>(StringComparer.Ordinal)
        {
            ["redisearch"] = ["syntactic-search", "hybrid-search-syntactic-axis"],
            ["redis-vector"] = ["semantic-search", "hybrid-search-semantic-axis"],
            ["falkordb"] = ["graph-traversal", "graph-scoped-search"],
            ["dapr-sidecar"] = ["all-service-invocation", "workflow-orchestration", "actor-runtime"],
            ["dapr-statestore"] = ["workflow-state-persistence", "actor-state-persistence"],
        };

    /// <summary>Returns the affected capability ids for <paramref name="checkName"/>, or
    /// an empty list when no mapping is registered (unknown check names are tolerated).</summary>
    /// <param name="checkName">Registered health-check name (e.g. <c>redisearch</c>).</param>
    /// <returns>Non-null read-only list of capability ids.</returns>
    public static IReadOnlyList<string> GetCapabilities(string checkName)
    {
        ArgumentNullException.ThrowIfNull(checkName);
        return Map.TryGetValue(checkName, out IReadOnlyList<string>? capabilities)
            ? capabilities
            : [];
    }
}
