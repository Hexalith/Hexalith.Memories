// <copyright file="GetCaseInfoTool.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Mcp.Tools;

using System.ComponentModel;

using Hexalith.Memories.Client.Rest;
using Hexalith.Memories.Contracts.V1;

using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

/// <summary>Story 10.1 — exposes case lookup as the MCP <c>get_case_info</c> tool.</summary>
[McpServerToolType]
internal sealed class GetCaseInfoTool
{
    private readonly MemoriesClient _client;
    private readonly McpToolExecutor _executor;

    /// <summary>Initializes a new instance of the <see cref="GetCaseInfoTool"/> class.</summary>
    /// <param name="client">The Memories REST client.</param>
    /// <param name="executor">The shared MCP tool executor.</param>
    public GetCaseInfoTool(
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
    /// <param name="caseId">Case identifier (required).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>An MCP tool result carrying the case summary.</returns>
    [McpServerTool(Name = "get_case_info")]
    [Description("Fetches summary information for a case (status, member count, memory unit count, recent activity timestamps).")]
    public async Task<CallToolResult> GetCaseAsync(
        [Description("The tenant identifier owning the case.")]
        string tenantId,
        [Description("The case identifier to look up.")]
        string caseId,
        CancellationToken cancellationToken = default)
    {
        const string toolName = "get_case_info";

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

                return string.IsNullOrWhiteSpace(caseId)
                    ? mapper.MapValidation(
                        "INVALID_INPUT",
                        "caseId is required.",
                        "Provide a non-empty caseId.",
                        toolName)
                    : null;
            },
            async (authorizedTenant, token) =>
            {
                Case caseSummary = await _client.GetCaseAsync(authorizedTenant, caseId, token).ConfigureAwait(false);
                return McpToolResultSerializer.Success(caseSummary);
            },
            cancellationToken).ConfigureAwait(false);
    }
}
