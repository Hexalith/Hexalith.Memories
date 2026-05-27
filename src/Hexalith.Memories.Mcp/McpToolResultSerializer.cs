// <copyright file="McpToolResultSerializer.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Mcp;

using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

using Hexalith.Memories.Contracts.V1;

using ModelContextProtocol.Protocol;

/// <summary>
/// Story 10.1 — single seam where MCP tool methods turn typed Server contracts into protocol-level
/// <see cref="CallToolResult"/> payloads. All serialization runs through
/// <see cref="MemoriesJsonContext.Options"/> so the wire format matches what other Memories surfaces
/// (CLI, REST) emit.
/// </summary>
internal static class McpToolResultSerializer
{
    /// <summary>Serializes <paramref name="value"/> to a JSON string using the shared Memories options.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="value">The value to serialize.</param>
    /// <returns>The JSON-encoded payload.</returns>
    public static string Serialize<T>(T value)
    {
        JsonTypeInfo<T> typeInfo = (JsonTypeInfo<T>)MemoriesJsonContext.Options.GetTypeInfo(typeof(T));
        return JsonSerializer.Serialize(value, typeInfo);
    }

    /// <summary>
    /// Wraps a successful typed contract payload in a protocol-level MCP tool result.
    /// </summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="value">The value to serialize.</param>
    /// <returns>A non-error MCP tool result whose text content carries the shared Memories JSON shape.</returns>
    public static CallToolResult Success<T>(T value)
    {
        JsonTypeInfo<T> typeInfo = (JsonTypeInfo<T>)MemoriesJsonContext.Options.GetTypeInfo(typeof(T));
        JsonElement structured = JsonSerializer.SerializeToElement(value, typeInfo);
        return new CallToolResult
        {
            Content = [new TextContentBlock { Text = JsonSerializer.Serialize(value, typeInfo) }],
            StructuredContent = structured,
            IsError = false,
        };
    }
}
