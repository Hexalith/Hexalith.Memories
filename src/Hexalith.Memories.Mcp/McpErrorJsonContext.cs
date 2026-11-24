// <copyright file="McpErrorJsonContext.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Mcp;

using System.Text.Json.Serialization;

/// <summary>Source-generated metadata for the MCP tool error envelope.</summary>
[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(McpErrorPayload))]
internal sealed partial class McpErrorJsonContext : JsonSerializerContext;
