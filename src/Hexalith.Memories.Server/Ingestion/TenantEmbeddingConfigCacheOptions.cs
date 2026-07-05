// <copyright file="TenantEmbeddingConfigCacheOptions.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Ingestion;

/// <summary>Options for short-lived per-tenant embedding configuration caching.</summary>
public sealed class TenantEmbeddingConfigCacheOptions
{
    /// <summary>Configuration section name.</summary>
    public const string SectionName = "Ingestion:EmbeddingConfigCache";

    /// <summary>Gets or sets the per-process cache time-to-live in seconds.</summary>
    public int CacheTtlSeconds { get; set; } = 30;
}
