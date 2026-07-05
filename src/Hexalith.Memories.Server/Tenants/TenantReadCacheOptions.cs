// <copyright file="TenantReadCacheOptions.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Tenants;

/// <summary>Options for short-lived per-process tenant read caches and bounded tenant-list fan-out.</summary>
public sealed class TenantReadCacheOptions
{
    /// <summary>Configuration section name.</summary>
    public const string SectionName = "Tenants:ReadCache";

    /// <summary>Gets or sets the tenant status cache time-to-live in seconds.</summary>
    public int TenantStatusTtlSeconds { get; set; } = 10;

    /// <summary>Gets or sets the missing-tenant status cache time-to-live in seconds.</summary>
    public int MissingTenantStatusTtlSeconds { get; set; } = 2;

    /// <summary>Gets or sets the tenant summary cache time-to-live in seconds.</summary>
    public int TenantSummaryTtlSeconds { get; set; } = 15;

    /// <summary>Gets or sets the default tenant-list page size.</summary>
    public int DefaultTenantListLimit { get; set; } = 50;

    /// <summary>Gets or sets the maximum tenant-list page size.</summary>
    public int MaxTenantListLimit { get; set; } = 100;

    /// <summary>Gets or sets the maximum tenant-list summary enrichment concurrency.</summary>
    public int MaxTenantListConcurrency { get; set; } = 8;

    /// <summary>Gets or sets the maximum number of entries retained in each per-process tenant read cache
    /// (status and summary). Bounds memory growth from distinct/negative-key probing (Story 24.2 review P4).</summary>
    public int MaxCacheEntries { get; set; } = 10000;

    /// <summary>Gets the clamped tenant status cache TTL.</summary>
    public TimeSpan GetTenantStatusTtl()
        => TimeSpan.FromSeconds(Math.Clamp(TenantStatusTtlSeconds, 1, 60));

    /// <summary>Gets the clamped missing-tenant status cache TTL.</summary>
    public TimeSpan GetMissingTenantStatusTtl()
        => TimeSpan.FromSeconds(Math.Clamp(MissingTenantStatusTtlSeconds, 1, 10));

    /// <summary>Gets the clamped tenant summary cache TTL.</summary>
    public TimeSpan GetTenantSummaryTtl()
        => TimeSpan.FromSeconds(Math.Clamp(TenantSummaryTtlSeconds, 1, 120));

    /// <summary>Gets the clamped default tenant-list limit.</summary>
    public int GetDefaultTenantListLimit()
        => Math.Clamp(DefaultTenantListLimit, 1, GetMaxTenantListLimit());

    /// <summary>Gets the clamped maximum tenant-list limit.</summary>
    public int GetMaxTenantListLimit()
        => Math.Clamp(MaxTenantListLimit, 1, 500);

    /// <summary>Gets the clamped tenant-list concurrency limit.</summary>
    public int GetMaxTenantListConcurrency()
        => Math.Clamp(MaxTenantListConcurrency, 1, 32);

    /// <summary>Gets the clamped maximum per-cache entry count.</summary>
    public int GetMaxCacheEntries()
        => Math.Clamp(MaxCacheEntries, 100, 1_000_000);
}
