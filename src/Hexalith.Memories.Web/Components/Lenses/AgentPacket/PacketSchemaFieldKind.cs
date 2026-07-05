// <copyright file="PacketSchemaFieldKind.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Web.Components.Lenses.AgentPacket;

/// <summary>A field of the readable Agent Packet Inspector schema view.</summary>
/// <remarks>
/// Story 17.4 — each field is a sanitized projection of a named Evidence Packet member.
/// </remarks>
public enum PacketSchemaFieldKind
{
    /// <summary>Scope tenant identifier.</summary>
    ScopeTenant = 0,

    /// <summary>Scope case identifier.</summary>
    ScopeCase,

    /// <summary>Scope isolation status.</summary>
    ScopeIsolation,

    /// <summary>Request query string.</summary>
    ResultQuery,

    /// <summary>Returned/total result counts.</summary>
    ResultCounts,

    /// <summary>Evidence strength.</summary>
    EvidenceStrength,

    /// <summary>Count of retrieval axes used.</summary>
    EvidenceAxes,

    /// <summary>Packet trust state.</summary>
    State,

    /// <summary>Omission reason.</summary>
    OmissionReason,

    /// <summary>Estimated token budget.</summary>
    TokenBudget,

    /// <summary>MCP tool/resource name.</summary>
    ToolName,

    /// <summary>MCP structured-content schema name and version.</summary>
    McpSchema,
}
