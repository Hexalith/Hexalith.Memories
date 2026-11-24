// <copyright file="McpToolExecutor.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Mcp;

using Hexalith.Memories.Client.Rest;
using Hexalith.Memories.Contracts.V1;
using Hexalith.Memories.Mcp.Authentication;

using ModelContextProtocol.Protocol;

/// <summary>
/// Executes MCP tool operations after tool-specific validation and tenant authorization.
/// </summary>
internal sealed partial class McpToolExecutor
{
    private readonly McpErrorMapper _errorMapper;
    private readonly ILogger<McpToolExecutor> _logger;
    private readonly TenantClaimAuthorizationFilter _tenantAuthorization;

    /// <summary>Initializes a new instance of the <see cref="McpToolExecutor"/> class.</summary>
    /// <param name="tenantAuthorization">The tenant-claim authorization filter.</param>
    /// <param name="errorMapper">The MCP error mapper.</param>
    /// <param name="logger">The server logger for sanitized tool diagnostics.</param>
    public McpToolExecutor(
        TenantClaimAuthorizationFilter tenantAuthorization,
        McpErrorMapper errorMapper,
        ILogger<McpToolExecutor> logger)
    {
        ArgumentNullException.ThrowIfNull(tenantAuthorization);
        ArgumentNullException.ThrowIfNull(errorMapper);
        ArgumentNullException.ThrowIfNull(logger);
        _tenantAuthorization = tenantAuthorization;
        _errorMapper = errorMapper;
        _logger = logger;
    }

    /// <summary>Validates, authorizes, and executes an MCP tool operation.</summary>
    /// <param name="requestedTenantId">The tenant identifier requested by the caller.</param>
    /// <param name="toolName">The registered MCP tool name.</param>
    /// <param name="validation">The tool-specific validation callback.</param>
    /// <param name="operation">The operation to invoke with the authorized tenant snapshot.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <param name="caseId">The optional requested case for error diagnostics.</param>
    /// <returns>The MCP tool result.</returns>
    public async Task<CallToolResult> RunAsync(
        string requestedTenantId,
        string toolName,
        Func<McpErrorMapper, CallToolResult?> validation,
        Func<string, CancellationToken, Task<CallToolResult>> operation,
        CancellationToken cancellationToken,
        string? caseId = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(toolName);
        ArgumentNullException.ThrowIfNull(validation);
        ArgumentNullException.ThrowIfNull(operation);

        string diagnosticTenantId = requestedTenantId;
        try
        {
            CallToolResult? validationError = validation(_errorMapper);
            if (validationError is not null)
            {
                if (validationError.IsError != true)
                {
                    throw new InvalidOperationException("An MCP tool validation callback returned a non-error result.");
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

            diagnosticTenantId = authorizedTenant;
            return await operation(authorizedTenant, cancellationToken).ConfigureAwait(false);
        }
        catch (TaskCanceledException timeout) when (!cancellationToken.IsCancellationRequested && timeout.InnerException is TimeoutException)
        {
            return MapUnexpectedFailure(timeout, toolName, McpErrorMapper.CreateRequestScope(diagnosticTenantId, caseId));
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (MemoriesRemoteException exception)
        {
            return _errorMapper.Map(exception, toolName, scope: McpErrorMapper.CreateRequestScope(diagnosticTenantId, caseId));
        }
        catch (Exception exception)
        {
            return MapUnexpectedFailure(exception, toolName, McpErrorMapper.CreateRequestScope(diagnosticTenantId, caseId));
        }
    }

    private CallToolResult MapUnexpectedFailure(Exception exception, string toolName, EvidencePacketScope? scope)
    {
        LogUnexpectedToolFailure(_logger, toolName, exception.GetType().Name, exception.HResult);
        return _errorMapper.MapGeneric(exception, toolName, scope);
    }

    [LoggerMessage(Level = LogLevel.Error, Message = "MCP tool {Tool} failed with {ExceptionType} (HRESULT {HResult}).")]
    private static partial void LogUnexpectedToolFailure(ILogger logger, string tool, string exceptionType, int hResult);
}
