// <copyright file="EvidencePacketPermissionsContext.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Contracts.V1;

/// <summary>Wire values used to identify the permission context of an evidence packet.</summary>
public static class EvidencePacketPermissionsContext
{
    /// <summary>A tenant-scoped request.</summary>
    public const string Tenant = "tenant";

    /// <summary>A request scoped to a case within a tenant.</summary>
    public const string TenantCase = "tenant-case";

    /// <summary>A request rejected by the MCP tenant authorization boundary.</summary>
    public const string McpAuthorization = "mcp-auth";

    /// <summary>An MCP tool error without a known request scope.</summary>
    public const string McpError = "mcp-error";

    /// <summary>Gets the permission context for a requested case or tenant scope.</summary>
    /// <param name="caseId">The optional case identifier.</param>
    /// <returns>The case or tenant context value.</returns>
    public static string ForScope(string? caseId)
        => string.IsNullOrWhiteSpace(caseId) ? Tenant : TenantCase;
}
