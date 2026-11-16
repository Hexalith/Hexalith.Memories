namespace Hexalith.Memories.IntegrationTests.Search;

using System.Diagnostics;

using DotNet.Testcontainers.Builders;

using Hexalith.Memories.Contracts.V1;
using Hexalith.Memories.IntegrationTests.Fixtures;
using Hexalith.Memories.Server.Activities.Indexing;
using Hexalith.Memories.Server.Infrastructure;
using Hexalith.Memories.Server.EventStoreIntegration;
using Hexalith.Memories.Server.Search;
using Hexalith.Memories.TestHelpers.Factories;

using Microsoft.Extensions.Logging.Abstractions;

using NRedisStack.RedisStackCommands;

using NSubstitute;

using Shouldly;

using StackExchange.Redis;

/// <summary>Contains the nightly integration cases for SyntacticSearchIntegrationTests.</summary>
public sealed partial class SyntacticSearchIntegrationTests
{
    [Fact]
    [Trait("Category", "Performance")]
    [Trait("Category", "IntegrationSlow")]
    public async Task SearchAsync_LatencySmokeTest_10ConcurrentQueries_ShouldBeFast()
    {
        // Arrange — seed 100+ documents
        string tenantId = $"tenant-perf-{Guid.NewGuid():N}";
        Task[] seedTasks = new Task[120];
        for (int i = 0; i < 120; i++)
        {
            int idx = i;
            seedTasks[i] = SeedDocumentAsync(tenantId, $"mu-{idx:D4}", $"performance test document {idx} with searchable content keywords terms");
        }

        await Task.WhenAll(seedTasks);

        SyntacticSearchService service = CreateService();

        // Warm up
        await service.SearchAsync(new SearchQuery { TenantId = tenantId, Query = "searchable" });

        // Act — 10 concurrent queries with per-query timings
        Task<(SearchResult Result, long ElapsedMilliseconds)>[] queryTasks = new Task<(SearchResult Result, long ElapsedMilliseconds)>[10];
        for (int i = 0; i < 10; i++)
        {
            queryTasks[i] = MeasureSearchAsync(service, new SearchQuery
            {
                TenantId = tenantId,
                Query = "performance searchable content",
            });
        }

        (SearchResult Result, long ElapsedMilliseconds)[] measurements = await Task.WhenAll(queryTasks);
        SearchResult[] results = measurements.Select(m => m.Result).ToArray();
        long[] latencies = measurements.Select(m => m.ElapsedMilliseconds).Order().ToArray();
        int p95Index = (int)Math.Ceiling(latencies.Length * 0.95d) - 1;

        // Assert
        results.ShouldAllBe(r => r.TotalCount > 0);
        results.ShouldAllBe(r => r.HasIndexedMemoryUnits);
        latencies[p95Index].ShouldBeLessThan(200);
    }
}
