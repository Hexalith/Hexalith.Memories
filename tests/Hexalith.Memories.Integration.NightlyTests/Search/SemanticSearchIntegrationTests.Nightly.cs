namespace Hexalith.Memories.IntegrationTests.Search;

using System.Diagnostics;
using System.Runtime.InteropServices;

using Dapr.Client;

using Hexalith.Memories.Contracts.V1;
using Hexalith.Memories.IntegrationTests.Fixtures;
using Hexalith.Memories.Server.Activities.Indexing;
using Hexalith.Memories.Server.Activities.Ingestion;
using Hexalith.Memories.Server.Infrastructure;
using Hexalith.Memories.Server.Ingestion;
using Hexalith.Memories.Server.Search;
using Hexalith.Memories.TestHelpers.Factories;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;

using NRedisStack.RedisStackCommands;

using NSubstitute;

using Shouldly;

using StackExchange.Redis;

using RedisQuery = NRedisStack.Search.Query;
using RedisSearchResult = NRedisStack.Search.SearchResult;

/// <summary>Contains the nightly integration cases for SemanticSearchIntegrationTests.</summary>
public sealed partial class SemanticSearchIntegrationTests
{
    [Fact]
    [Trait("Category", "Performance")]
    [Trait("Category", "IntegrationSlow")]
    public async Task SearchAsync_LatencySmokeTest_10ConcurrentQueries_ShouldBeFast()
    {
        // Arrange — seed 10K indexed memory units to match the story acceptance criteria
        string tenantId = $"tenant-perf-{Guid.NewGuid():N}";
        const int DocumentCount = 10_000;
        await SeedPerformanceDocumentsAsync(tenantId, DocumentCount);
        await WaitForVectorIndexCountAsync(tenantId, DocumentCount);

        SemanticSearchService service = CreateService();

        // Warm up
        await service.SearchAsync(
            new SearchQuery { TenantId = tenantId, Query = "searchable" },
            _embeddingConfig,
            CancellationToken.None);

        // Act — 10 concurrent queries with per-query timings
        Task<(SearchResult Result, long ElapsedMilliseconds)>[] queryTasks =
            new Task<(SearchResult, long)>[10];
        for (int i = 0; i < 10; i++)
        {
            queryTasks[i] = MeasureSearchAsync(service, new SearchQuery
            {
                TenantId = tenantId,
                Query = "performance searchable content",
            });
        }

        (SearchResult Result, long ElapsedMilliseconds)[] measurements = await Task.WhenAll(queryTasks);
        long[] latencies = measurements.Select(m => m.ElapsedMilliseconds).Order().ToArray();
        int p95Index = (int)Math.Ceiling(latencies.Length * 0.95d) - 1;

        // Assert
        measurements.ShouldAllBe(m => m.Result.TotalCount > 0);
        latencies[p95Index].ShouldBeLessThan(500);
    }
}
