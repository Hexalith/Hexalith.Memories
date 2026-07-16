// <copyright file="TraverseRelationsTool.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Mcp.Tools;

using System.ComponentModel;

using Hexalith.Memories.Client.Rest;
using Hexalith.Memories.Contracts.V1;

using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

/// <summary>Story 10.1 — exposes graph traversal as the MCP <c>traverse_relations</c> tool.</summary>
[McpServerToolType]
internal sealed class TraverseRelationsTool
{
    /// <summary>Server-mirroring depth lower bound; mirrors <c>Math.Clamp(depth, 0, 10)</c> in <c>Server/Program.cs</c>.</summary>
    internal const int DepthLowerBound = 0;

    /// <summary>Server-mirroring depth upper bound.</summary>
    internal const int DepthUpperBound = 10;

    private readonly MemoriesClient _client;
    private readonly McpToolExecutor _executor;

    /// <summary>Initializes a new instance of the <see cref="TraverseRelationsTool"/> class.</summary>
    /// <param name="client">The Memories REST client.</param>
    /// <param name="executor">The shared MCP tool executor.</param>
    public TraverseRelationsTool(
        MemoriesClient client,
        McpToolExecutor executor)
    {
        ArgumentNullException.ThrowIfNull(client);
        ArgumentNullException.ThrowIfNull(executor);
        _client = client;
        _executor = executor;
    }

    /// <summary>The MCP tool method.</summary>
    /// <param name="tenantId">Tenant identifier (required).</param>
    /// <param name="from">Memory unit id to start traversal from (required).</param>
    /// <param name="depth">Maximum traversal depth; clamped to <c>[0, 10]</c>.</param>
    /// <param name="edgeType">Optional comma-separated edge type list.</param>
    /// <param name="tokenBudget">Optional output token budget. The server prunes leaves before the primary causal path.</param>
    /// <param name="caseId">Optional graph scope case id.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>An MCP tool result carrying the traversal result.</returns>
    [McpServerTool(Name = "traverse_relations")]
    [Description("Traverses causal and correlational relationships from a starting memory unit and returns ordered nodes plus edges (with gap markers when stub nodes are encountered).")]
    public async Task<CallToolResult> TraverseAsync(
        [Description("The tenant identifier whose graph should be traversed.")]
        string tenantId,
        [Description("The memory unit identifier to start traversal from.")]
        string from,
        [Description("Maximum traversal depth; clamped to the inclusive range 0..10.")]
        int depth = 3,
        [Description("Optional comma-separated edge type filter; valid values: causedBy, correlatedWith, references, contains, annotates.")]
        string? edgeType = null,
        [Description("Maximum output tokens. The server truncates leaves first, preserving the primary causal path.")]
        int? tokenBudget = null,
        [Description("Optional case identifier scoping the traversal to a single case (graph_scope simplification — see docs/dev/mcp-server.md).")]
        string? caseId = null,
        CancellationToken cancellationToken = default)
    {
        const string toolName = "traverse_relations";
        IReadOnlyList<EdgeType>? parsedEdgeTypes = null;

        return await _executor.RunAsync(
            tenantId,
            toolName,
            mapper =>
            {
                if (string.IsNullOrWhiteSpace(tenantId))
                {
                    return mapper.MapValidation(
                        "INVALID_INPUT",
                        "tenantId is required.",
                        "Provide a non-empty tenantId.",
                        toolName);
                }

                if (string.IsNullOrWhiteSpace(from))
                {
                    return mapper.MapValidation(
                        "INVALID_INPUT",
                        "from is required.",
                        "Provide the memory unit id to start traversal from.",
                        toolName);
                }

                if (TryParseEdgeTypes(edgeType, out parsedEdgeTypes, out string? invalidValue))
                {
                    return null;
                }

                string validList = string.Join(
                    ", ",
                    Enum.GetValues<EdgeType>().Select(static et => char.ToLowerInvariant(et.ToString()[0]) + et.ToString()[1..]));
                return mapper.MapValidation(
                    "INVALID_EDGE_TYPE",
                    $"Unknown edge type: '{invalidValue}'.",
                    $"Use comma-separated camelCase edge type names (valid: {validList}).",
                    toolName);
            },
            async (authorizedTenant, token) =>
            {
                int clampedDepth = Math.Clamp(depth, DepthLowerBound, DepthUpperBound);
                int? effectiveTokenBudget = tokenBudget is > 0 ? tokenBudget : null;

                TraversalResult result = await _client.TraverseAsync(
                    authorizedTenant,
                    from,
                    clampedDepth,
                    caseId,
                    parsedEdgeTypes,
                    tokenBudget: effectiveTokenBudget,
                    ct: token).ConfigureAwait(false);
                return McpToolResultSerializer.Success(result);
            },
            cancellationToken).ConfigureAwait(false);
    }

    private static bool TryParseEdgeTypes(string? raw, out IReadOnlyList<EdgeType>? parsed, out string? invalidValue)
    {
        parsed = null;
        invalidValue = null;
        if (string.IsNullOrWhiteSpace(raw))
        {
            return true;
        }

        string[] parts = raw.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        var collected = new List<EdgeType>(parts.Length);
        foreach (string part in parts)
        {
            if (!Enum.TryParse(part, ignoreCase: true, out EdgeType value) || !Enum.IsDefined(value))
            {
                invalidValue = part;
                return false;
            }

            collected.Add(value);
        }

        parsed = collected;
        return true;
    }
}
