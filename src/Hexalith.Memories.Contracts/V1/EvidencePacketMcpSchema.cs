// <copyright file="EvidencePacketMcpSchema.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Contracts.V1;

using System.Text.Json.Serialization;

/// <summary>MCP tool and structured-content schema metadata attached to an Evidence Packet.</summary>
/// <param name="ToolName">MCP tool name, when the packet was produced for a tool response.</param>
/// <param name="SchemaName">Structured-content schema name.</param>
/// <param name="SchemaVersion">Structured-content schema version.</param>
/// <param name="Transport">MCP transport or host surface, when known.</param>
public sealed record EvidencePacketMcpSchema(
    string ToolName,
    string SchemaName,
    string SchemaVersion,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? Transport = null);
