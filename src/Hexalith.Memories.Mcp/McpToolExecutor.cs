// <copyright file="McpToolExecutor.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Mcp;

using Hexalith.Memories.Client.Rest;
using Hexalith.Memories.Mcp.Authentication;

using ModelContextProtocol.Protocol;

/// <summary>
/// Executes MCP tool operations after tool-specific validation and tenant authorization.
/// </summary>
internal sealed class McpToolExecutor
{
    private readonly McpErrorMapper _errorMapper;
    private readonly TenantClaimAuthorizationFilter _tenantAuthorization;

    /// <summary>Initializes a new instance of the <see cref="McpToolExecutor"/> class.</summary>
    /// <param name="tenantAuthorization">The tenant-claim authorization filter.</param>
    /// <param name="errorMapper">The MCP error mapper.</param>
    public McpToolExecutor(
        TenantClaimAuthorizationFilter tenantAuthorization,
        McpErrorMapper errorMapper)
    {
        ArgumentNullException.ThrowIfNull(tenantAuthorization);
        ArgumentNullException.ThrowIfNull(errorMapper);
        _tenantAuthorization = tenantAuthorization;
        _errorMapper = errorMapper;
    }

    /// <summary>Validates, authorizes, and executes an MCP tool operation.</summary>
    /// <param name="requestedTenantId">The tenant identifier requested by the caller.</param>
    /// <param name="toolName">The registered MCP tool name.</param>
    /// <param name="validation">The tool-specific validation callback.</param>
    /// <param name="operation">The operation to invoke with the authorized tenant snapshot.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The MCP tool result.</returns>
    public async Task<CallToolResult> RunAsync(
        string requestedTenantId,
        string toolName,
        Func<McpErrorMapper, CallToolResult?> validation,
        Func<string, CancellationToken, Task<CallToolResult>> operation,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(toolName);
        ArgumentNullException.ThrowIfNull(validation);
        ArgumentNullException.ThrowIfNull(operation);

        CallToolResult? validationError = validation(_errorMapper);
        if (validationError is not null)
        {
            if (validationError.IsError != true)
            {
                return _errorMapper.MapGeneric(
                    new InvalidOperationException("An MCP tool validation callback returned a non-error result."),
                    toolName);
            }

            return validationError;
        }

        if (!_tenantAuthorization.TryAuthorizeTenant(
                requestedTenantId,
                toolName,
                out string authorizedTenant,
                out CallToolResult? authorizationError))
        {
            return authorizationError!;
        }

        try
        {
            return await operation(authorizedTenant, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (MemoriesRemoteException exception)
        {
            return _errorMapper.Map(exception, toolName);
        }
        catch (Exception exception)
        {
            return _errorMapper.MapGeneric(exception, toolName);
        }
    }
}
