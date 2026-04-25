// <copyright file="GetCaseInfoTool.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Mcp.Tools;

using System.ComponentModel;

using Hexalith.Memories.Client.Rest;
using Hexalith.Memories.Contracts.V1;
using Hexalith.Memories.Mcp.Authentication;

using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

/// <summary>Story 10.1 — exposes case lookup as the MCP <c>get_case_info</c> tool.</summary>
[McpServerToolType]
internal sealed class GetCaseInfoTool
{
    private readonly MemoriesClient _client;
    private readonly McpErrorMapper _mapper;
    private readonly TenantClaimAuthorizationFilter _tenantAuthorization;
    private readonly IAuthorizedTenantAccessor _authorizedTenantAccessor;

    /// <summary>Initializes a new instance of the <see cref="GetCaseInfoTool"/> class.</summary>
    /// <param name="client">The Memories REST client.</param>
    /// <param name="mapper">The error mapper.</param>
    /// <param name="tenantAuthorization">The tenant-claim authorization filter.</param>
    /// <param name="authorizedTenantAccessor">The authorized tenant accessor.</param>
    public GetCaseInfoTool(
        MemoriesClient client,
        McpErrorMapper mapper,
        TenantClaimAuthorizationFilter tenantAuthorization,
        IAuthorizedTenantAccessor authorizedTenantAccessor)
    {
        ArgumentNullException.ThrowIfNull(client);
        ArgumentNullException.ThrowIfNull(mapper);
        ArgumentNullException.ThrowIfNull(tenantAuthorization);
        ArgumentNullException.ThrowIfNull(authorizedTenantAccessor);
        _client = client;
        _mapper = mapper;
        _tenantAuthorization = tenantAuthorization;
        _authorizedTenantAccessor = authorizedTenantAccessor;
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

        if (string.IsNullOrWhiteSpace(tenantId))
        {
            return _mapper.MapValidation(
                "INVALID_INPUT",
                "tenantId is required.",
                "Provide a non-empty tenantId.",
                toolName);
        }

        if (string.IsNullOrWhiteSpace(caseId))
        {
            return _mapper.MapValidation(
                "INVALID_INPUT",
                "caseId is required.",
                "Provide a non-empty caseId.",
                toolName);
        }

        if (!_tenantAuthorization.TryAuthorizeTenant(tenantId, toolName, out _, out CallToolResult? authorizationError))
        {
            return authorizationError!;
        }

        if (!_authorizedTenantAccessor.TryGetAuthorizedTenant(out string authorizedTenant))
        {
            return _mapper.MapAuthorization(tenantId, toolName, McpErrorMapper.TenantForbiddenCode);
        }

        try
        {
            Case caseSummary = await _client.GetCaseAsync(authorizedTenant, caseId, cancellationToken).ConfigureAwait(false);
            return McpToolResultSerializer.Success(caseSummary);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (MemoriesRemoteException ex)
        {
            return _mapper.Map(ex, toolName);
        }
        catch (Exception ex)
        {
            return _mapper.MapGeneric(ex, toolName);
        }
    }
}
