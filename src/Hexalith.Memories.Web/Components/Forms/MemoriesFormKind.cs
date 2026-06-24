// <copyright file="MemoriesFormKind.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Web.Components.Forms;

/// <summary>
/// The configuration surface a Story 17.3 contract-aware form changes.
/// </summary>
/// <remarks>
/// Story 17.3 (AC1) — forms that change these surfaces must place tenant and case scope first, validate
/// against typed contracts, and gate dangerous or inconsistent changes behind an explicit acknowledgement.
/// The kind is consumed only to classify dangerous changes; it never invents new scope, command, or
/// contract semantics.
/// </remarks>
public enum MemoriesFormKind
{
    /// <summary>Search / retrieval configuration (query, axes, max results).</summary>
    Search = 0,

    /// <summary>Ingestion configuration (sources, re-ingestion).</summary>
    Ingestion,

    /// <summary>Source filter configuration.</summary>
    SourceFilter,

    /// <summary>Graph traversal configuration (depth, edge types).</summary>
    Graph,

    /// <summary>Token-budget configuration.</summary>
    TokenBudget,

    /// <summary>Repair / consistency configuration. Repair forms are dangerous by nature.</summary>
    Repair,

    /// <summary>Benchmark configuration.</summary>
    Benchmark,

    /// <summary>MCP request configuration.</summary>
    McpRequest,
}
