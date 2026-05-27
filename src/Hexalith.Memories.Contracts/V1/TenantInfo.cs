// <copyright file="TenantInfo.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Contracts.V1;

using System.Text.Json.Serialization;

/// <summary>Public-facing tenant representation.</summary>
public sealed record TenantInfo(
    string Id,
    string DisplayName,
    TenantStatus Status,
    DateTimeOffset CreatedAt)
{
    /// <summary>Gets the embedding provider name, if configured.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? EmbeddingProvider { get; init; }

    /// <summary>Gets the embedding model name, if configured.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? EmbeddingModel { get; init; }
}
