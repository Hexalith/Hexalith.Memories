// <copyright file="TenantSummaryCacheTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Tests.Tenants;

using Hexalith.Memories.Contracts.V1;
using Hexalith.Memories.Server.Tenants;

using Microsoft.Extensions.Options;

using Shouldly;

public sealed class TenantSummaryCacheTests
{
    [Theory]
    [InlineData(IndexHealth.Unknown, 1)]
    [InlineData(IndexHealth.Degraded, 1)]
    [InlineData(IndexHealth.Ready, null)]
    public async Task IncompleteMetrics_ReturnedButRefreshedOnNextRead(IndexHealth indexHealth, int? unitCount)
    {
        TenantSummaryCache cache = CreateCache();
        TenantSummary healthy = CreateSummary("tenant-a");
        TenantSummary incomplete = healthy with
        {
            IndexStatus = healthy.IndexStatus with { Graph = indexHealth },
            IndexSizes = healthy.IndexSizes with { GraphNodeCount = indexHealth == IndexHealth.Ready ? 10 : null },
            MemoryUnitCount = unitCount,
        };
        TenantSummary neighbor = CreateSummary("tenant-b") with { MemoryUnitCount = 30 };
        _ = await cache.GetOrCreateAsync(neighbor.Id, () => Task.FromResult(neighbor));

        (await cache.GetOrCreateAsync(healthy.Id, () => Task.FromResult(incomplete))).ShouldBe(incomplete);
        (await cache.GetOrCreateAsync(healthy.Id, () => Task.FromResult(healthy))).ShouldBe(healthy);

        (await cache.GetOrCreateAsync(healthy.Id, UnexpectedRead)).ShouldBe(healthy);
        (await cache.GetOrCreateAsync(neighbor.Id, UnexpectedRead)).ShouldBe(neighbor);
    }

    [Fact]
    public async Task UnavailableEnrichmentWithHealthyCounts_IsNotCached()
    {
        TenantSummaryCache cache = CreateCache();
        TenantSummary summary = CreateSummary("tenant-a");

        _ = await cache.GetOrCreateAsync(summary.Id, () => Task.FromResult((summary, false)));

        TenantSummary recovered = summary with { LastActivityAt = DateTimeOffset.UnixEpoch };
        (await cache.GetOrCreateAsync(summary.Id, () => Task.FromResult((recovered, true)))).ShouldBe(recovered);
        (await cache.GetOrCreateAsync(summary.Id, UnexpectedRead)).ShouldBe(recovered);
    }

    [Fact]
    public async Task MissingIndexAndNoPriorActivity_CanBeCached()
    {
        TenantSummaryCache cache = CreateCache();
        TenantSummary healthy = CreateSummary("empty-tenant") with
        {
            IndexStatus = new TenantIndexStatus(IndexHealth.Missing, IndexHealth.Missing, IndexHealth.Missing),
            IndexSizes = new TenantIndexSizes(null, null, null),
            MemoryUnitCount = 0,
            LastActivityAt = null,
        };

        _ = await cache.GetOrCreateAsync(healthy.Id, () => Task.FromResult(healthy));
        (await cache.GetOrCreateAsync(healthy.Id, UnexpectedRead)).ShouldBe(healthy);
    }

    private static TenantSummaryCache CreateCache()
        => new(Options.Create(new TenantReadCacheOptions { TenantSummaryTtlSeconds = 120 }), TimeProvider.System);

    private static TenantSummary CreateSummary(string tenantId) => new()
    {
        Id = tenantId,
        DisplayName = tenantId,
        Status = TenantStatus.Active,
        CreatedAt = DateTimeOffset.UnixEpoch,
        IndexStatus = new TenantIndexStatus(IndexHealth.Ready, IndexHealth.Ready, IndexHealth.Ready),
        IndexSizes = new TenantIndexSizes(10, 10, 10),
        MemoryUnitCount = 1,
        ReindexRequired = false,
    };

    private static Task<TenantSummary> UnexpectedRead()
        => throw new InvalidOperationException("Expected the tenant's own cached summary.");
}
