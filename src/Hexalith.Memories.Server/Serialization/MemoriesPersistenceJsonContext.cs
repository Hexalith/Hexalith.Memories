// <copyright file="MemoriesPersistenceJsonContext.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Serialization;

using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

/// <summary>Provides JSON options for server-owned workflow and durable payloads.</summary>
internal static class MemoriesPersistenceJsonContext
{
    /// <summary>Gets the server persistence serializer options.</summary>
    internal static JsonSerializerOptions Options { get; } = CreateOptions();

    private static JsonSerializerOptions CreateOptions()
        => new(JsonSerializerDefaults.Web)
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            TypeInfoResolver = JsonTypeInfoResolver.Combine(
                MemoriesPersistenceJsonSourceGenerationContext.Default,
                new DefaultJsonTypeInfoResolver()),
        };
}
