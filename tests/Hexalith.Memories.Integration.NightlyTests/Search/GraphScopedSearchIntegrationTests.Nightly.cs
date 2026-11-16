namespace Hexalith.Memories.IntegrationTests.Search;

using System.Diagnostics;

using Dapr.Client;

using Hexalith.Memories.Contracts.V1;
using Hexalith.Memories.IntegrationTests.Fixtures;
using Hexalith.Memories.Server.Activities.Indexing;
using Hexalith.Memories.Server.Activities.Ingestion;
using Hexalith.Memories.Server.Graph;
using Hexalith.Memories.Server.Infrastructure;
using Hexalith.Memories.Server.Ingestion;
using Hexalith.Memories.Server.Search;
using Hexalith.Memories.TestHelpers.Factories;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;

using NFalkorDB;

using NRedisStack.RedisStackCommands;

using NSubstitute;

using Shouldly;

using StackExchange.Redis;

/// <summary>Contains the nightly integration cases for GraphScopedSearchIntegrationTests.</summary>
public sealed partial class GraphScopedSearchIntegrationTests
{
    [Fact]
    [Trait("Category", "Performance")]
    [Trait("Category", "IntegrationSlow")]
    public async Task SearchAsync_Latency_ShouldBeUnder2Seconds()
    {
        // Arrange — seed a small graph
        string tenantId = $"tenant-perf-{Guid.NewGuid():N}";
        string caseId = $"case-{Guid.NewGuid():N}";
        await SeedGraphChainAsync(tenantId, caseId, "mu-p1", "mu-p2", "mu-p3");
        await SeedSyntacticHashAsync(tenantId, "mu-p1", "Perf content 1");
        await SeedSyntacticHashAsync(tenantId, "mu-p2", "Perf content 2");
        await SeedSyntacticHashAsync(tenantId, "mu-p3", "Perf content 3");

        GraphScopedSearch service = CreateService();

        // Act
        Stopwatch sw = Stopwatch.StartNew();
        SearchResult result = await service.SearchAsync(
            new SearchQuery { TenantId = tenantId, Query = string.Empty, MaxResults = 10 },
            "mu-p1", depth: 3);
        sw.Stop();

        // Assert
        sw.ElapsedMilliseconds.ShouldBeLessThan(2000);
        result.Results.Count.ShouldBeGreaterThan(0);
    }
}
