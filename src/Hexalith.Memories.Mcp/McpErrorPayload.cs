// <copyright file="McpErrorPayload.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Mcp;

using System.Text.Json.Serialization;

using Hexalith.Memories.Contracts.V1;

/// <summary>Structured diagnostic returned in an MCP tool error.</summary>
/// <param name="Code">The stable error code.</param>
/// <param name="Service">The service that reported the error.</param>
/// <param name="Tool">The tool that observed the error.</param>
/// <param name="Message">The client-facing diagnostic message.</param>
/// <param name="Suggestion">The recovery guidance.</param>
/// <param name="EvidencePacket">The optional evidence packet for the error.</param>
internal sealed record McpErrorPayload(
    string Code,
    string Service,
    string Tool,
    string Message,
    string Suggestion,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] EvidencePacket? EvidencePacket);
