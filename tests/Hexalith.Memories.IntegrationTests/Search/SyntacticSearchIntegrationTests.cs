namespace Hexalith.Memories.IntegrationTests.Search;

using System.Diagnostics;

using Hexalith.Memories.Contracts.V1;
using Hexalith.Memories.IntegrationTests.Fixtures;
using Hexalith.Memories.Server.Activities.Indexing;
using Hexalith.Memories.Server.Infrastructure;
using Hexalith.Memories.Server.Search;
using Hexalith.Memories.TestHelpers.Factories;

using Microsoft.Extensions.Logging.Abstractions;

using NSubstitute;

using Shouldly;

using StackExchange.Redis;

/// <summary>
/// Integration tests verifying SyntacticSearchService against a real Redis Stack instance.
/// Validates BM25 ranking, tenant isolation, case scoping, pagination, and latency.
/// </summary>
[Collection("RedisStack")]
[Trait("Category", "Integration")]
public class SyntacticSearchIntegrationTests
{
    private readonly RedisStackFixture _redis;

    public SyntacticSearchIntegrationTests(RedisStackFixture redis) => _redis = redis;

    [Fact]
    public async Task SearchAsync_Bm25Ranking_ShouldReturnResultsOrderedByRelevance()
    {
        // Arrange
        string tenantId = $"tenant-{Guid.NewGuid():N}";
        await SeedDocumentAsync(tenantId, "mu-1", "insurance claim denied due to policy expiration claim rejected");
        await SeedDocumentAsync(tenantId, "mu-2", "the weather today is sunny and warm outside");
        await SeedDocumentAsync(tenantId, "mu-3", "claim denied because of incomplete documentation");

        SyntacticSearchService service = CreateService();

        // Act
        SearchResult result = await service.SearchAsync(new SearchQuery
        {
            TenantId = tenantId,
            Query = "claim denied",
        });

        // Assert
        result.TotalCount.ShouldBeGreaterThanOrEqualTo(2);
        result.Results.Count.ShouldBeGreaterThanOrEqualTo(2);

        // "mu-1" has "claim" twice and "denied" once — should rank higher
        result.Results[0].Score.ShouldBeGreaterThanOrEqualTo(result.Results[1].Score);

        // Weather doc should not appear for "claim denied"
        result.Results.ShouldAllBe(r => r.MemoryUnitId != "mu-2");
    }

    [Fact]
    public async Task SearchAsync_PinnedBm25StdScorer_ShouldBeAcceptedByRedisStack()
    {
        string tenantId = $"tenant-{Guid.NewGuid():N}";
        await SeedDocumentAsync(tenantId, "mu-1", "explicit scorer acceptance document");

        SyntacticSearchService service = CreateService();

        SearchResult result = await service.SearchAsync(new SearchQuery
        {
            TenantId = tenantId,
            Query = "explicit scorer",
        });

        result.Results.ShouldContain(r => r.MemoryUnitId == "mu-1");
        result.HasIndexedMemoryUnits.ShouldBeTrue();
    }

    [Fact]
    public async Task SearchAsync_ShouldReturnRedisHighlightedBoundedSnippet()
    {
        string tenantId = $"tenant-{Guid.NewGuid():N}";
        string memoryUnitId = "mu-highlight";
        await SeedDocumentAsync(
            tenantId,
            memoryUnitId,
            "opening context before the match. payment outage traced to database timeout and queue backpressure. trailing context after the match.");

        SyntacticSearchService service = CreateService();
        var query = new SearchQuery
        {
            TenantId = tenantId,
            Query = "payment outage",
        };

        SearchResult unscoped = await service.SearchAsync(query);
        SearchResult graphScoped = await service.SearchAsync(
            query,
            [IndexSchemaDefinitions.BuildSyntacticKey(tenantId, memoryUnitId)]);

        AssertHighlightedSnippet(unscoped.Results.Single(r => r.MemoryUnitId == memoryUnitId));
        AssertHighlightedSnippet(graphScoped.Results.Single(r => r.MemoryUnitId == memoryUnitId));
    }

    [Fact]
    public async Task SearchAsync_TenantIsolation_ShouldNotReturnCrossTenantResults()
    {
        // Arrange
        string tenantA = $"tenant-a-{Guid.NewGuid():N}";
        string tenantB = $"tenant-b-{Guid.NewGuid():N}";

        await SeedDocumentAsync(tenantA, "mu-a", "secret tenant A data alpha");
        await SeedDocumentAsync(tenantB, "mu-b", "secret tenant B data beta");

        SyntacticSearchService service = CreateService();

        // Act
        SearchResult resultA = await service.SearchAsync(new SearchQuery
        {
            TenantId = tenantA,
            Query = "secret data",
        });

        // Assert
        resultA.Results.ShouldAllBe(r => r.MemoryUnitId == "mu-a");
        resultA.Results.ShouldNotContain(r => r.MemoryUnitId == "mu-b");
    }

    [Fact]
    public async Task SearchAsync_CaseScoping_ShouldFilterByCase()
    {
        // Arrange
        string tenantId = $"tenant-{Guid.NewGuid():N}";
        await SeedDocumentAsync(tenantId, "mu-case1", "important document about finances", caseId: "case-alpha");
        await SeedDocumentAsync(tenantId, "mu-case2", "important document about finances", caseId: "case-beta");

        SyntacticSearchService service = CreateService();

        // Act
        SearchResult result = await service.SearchAsync(new SearchQuery
        {
            TenantId = tenantId,
            Query = "important finances",
            CaseId = "case-alpha",
        });

        // Assert
        result.Results.ShouldAllBe(r => r.MemoryUnitId == "mu-case1");
    }

    [Fact]
    public async Task SearchAsync_CloudEventSubjectFilter_ShouldUseExactMatchTagFiltering()
    {
        string tenantId = $"tenant-{Guid.NewGuid():N}";

        await SeedDocumentAsync(
            tenantId,
            "mu-subject-a",
            "claim event content",
            metadata: new Dictionary<string, MetadataField>
            {
                ["cloudevent.subject"] = new("claim-42", MetadataOrigin.Ai, 1.0f),
            });
        await SeedDocumentAsync(
            tenantId,
            "mu-subject-b",
            "claim event content",
            metadata: new Dictionary<string, MetadataField>
            {
                ["cloudevent.subject"] = new("claim-99", MetadataOrigin.Ai, 1.0f),
            });

        SyntacticSearchService service = CreateService();

        SearchResult result = await service.SearchAsync(new SearchQuery
        {
            TenantId = tenantId,
            Query = "claim event",
            CloudEventSubject = "claim-42",
        });

        result.Results.Select(r => r.MemoryUnitId).ShouldBe(["mu-subject-a"]);
    }

    [Fact]
    public async Task SearchAsync_EmptyResults_ShouldReturnEmptySet()
    {
        // Arrange
        string tenantId = $"tenant-{Guid.NewGuid():N}";
        await SeedDocumentAsync(tenantId, "mu-1", "actual content about finance");

        SyntacticSearchService service = CreateService();

        // Act
        SearchResult result = await service.SearchAsync(new SearchQuery
        {
            TenantId = tenantId,
            Query = "xyznonexistent987",
        });

        // Assert
        result.Results.ShouldBeEmpty();
        result.TotalCount.ShouldBe(0);
        result.HasIndexedMemoryUnits.ShouldBeTrue();
    }

    [Fact]
    public async Task SearchAsync_MissingIndex_ShouldReturnEmptyNotException()
    {
        // Arrange
        string tenantId = $"tenant-never-seeded-{Guid.NewGuid():N}";
        SyntacticSearchService service = CreateService();

        // Act
        SearchResult result = await service.SearchAsync(new SearchQuery
        {
            TenantId = tenantId,
            Query = "anything",
        });

        // Assert
        result.Results.ShouldBeEmpty();
        result.TotalCount.ShouldBe(0);
        result.HasIndexedMemoryUnits.ShouldBeFalse();
        result.Query.ShouldBe("anything");
    }

    [Fact]
    public async Task SearchAsync_SpecialCharacters_ShouldNotThrowParseError()
    {
        // Arrange
        string tenantId = $"tenant-{Guid.NewGuid():N}";
        await SeedDocumentAsync(tenantId, "mu-1", "some content");

        SyntacticSearchService service = CreateService();

        // Act & Assert — should not throw
        SearchResult result = await service.SearchAsync(new SearchQuery
        {
            TenantId = tenantId,
            Query = "claim-denied (policy) [section]",
        });

        result.ShouldNotBeNull();
    }

    [Fact]
    public async Task SearchAsync_NaturalLanguageQuestion_ShouldReturnKeywordMatches()
    {
        // Arrange
        string tenantId = $"tenant-{Guid.NewGuid():N}";
        await SeedDocumentAsync(
            tenantId,
            "mu-1",
            "Payment outage in March traced to database timeout and connection pool exhaustion.");
        await SeedDocumentAsync(
            tenantId,
            "mu-2",
            "Invoice batch completed successfully overnight with no alerting issues.");

        SyntacticSearchService service = CreateService();

        // Act
        SearchResult result = await service.SearchAsync(new SearchQuery
        {
            TenantId = tenantId,
            Query = "What caused the payment outage in March?",
        });

        // Assert
        result.Results.ShouldContain(r => r.MemoryUnitId == "mu-1");
        result.Results.ShouldNotContain(r => r.MemoryUnitId == "mu-2");
    }

    [Fact]
    public async Task SearchAsync_KeywordPhrase_ShouldPreserveAndStyleMatching()
    {
        // Arrange
        string tenantId = $"tenant-{Guid.NewGuid():N}";
        await SeedDocumentAsync(
            tenantId,
            "mu-1",
            "Payment outage in March traced to database timeout and connection pool exhaustion.");
        await SeedDocumentAsync(
            tenantId,
            "mu-2",
            "Outage analysis focused on overnight invoice jobs without any customer impact.");

        SyntacticSearchService service = CreateService();

        // Act
        SearchResult result = await service.SearchAsync(new SearchQuery
        {
            TenantId = tenantId,
            Query = "payment outage",
        });

        // Assert
        result.Results.ShouldContain(r => r.MemoryUnitId == "mu-1");
        result.Results.ShouldNotContain(r => r.MemoryUnitId == "mu-2");
    }

    [Fact]
    public async Task SearchAsync_OffsetPagination_ShouldSkipResults()
    {
        // Arrange — seed 15 documents
        string tenantId = $"tenant-{Guid.NewGuid():N}";
        for (int i = 0; i < 15; i++)
        {
            await SeedDocumentAsync(tenantId, $"mu-{i:D3}", $"searchable document number {i} with common keyword");
        }

        SyntacticSearchService service = CreateService();

        // Act — get the full ordered result set
        SearchResult allResults = await service.SearchAsync(new SearchQuery
        {
            TenantId = tenantId,
            Query = "searchable document keyword",
            MaxResults = 15,
        });

        // Get the page that should start at the 11th document
        SearchResult page = await service.SearchAsync(new SearchQuery
        {
            TenantId = tenantId,
            Query = "searchable document keyword",
            MaxResults = 5,
            Offset = 10,
        });

        // Assert
        allResults.Results.Count.ShouldBe(15);
        page.Results.Count.ShouldBe(5);
        page.HasIndexedMemoryUnits.ShouldBeTrue();

        string[] expectedIds = allResults.Results.Skip(10).Take(5).Select(r => r.MemoryUnitId).ToArray();
        string[] actualIds = page.Results.Select(r => r.MemoryUnitId).ToArray();
        actualIds.ShouldBe(expectedIds);
    }

    [Fact]
    public async Task SearchAsync_BroadMatch_ShouldRespectMaxResultsCap()
    {
        // Arrange — seed many docs with a common word
        string tenantId = $"tenant-{Guid.NewGuid():N}";
        for (int i = 0; i < 120; i++)
        {
            await SeedDocumentAsync(tenantId, $"mu-{i:D3}", $"common word appears in document {i}");
        }

        SyntacticSearchService service = CreateService();

        // Act
        SearchResult result = await service.SearchAsync(new SearchQuery
        {
            TenantId = tenantId,
            Query = "common word document",
            MaxResults = 500,
        });

        // Assert
        result.Results.Count.ShouldBe(100);
        result.TotalCount.ShouldBeGreaterThanOrEqualTo(100);
        result.HasIndexedMemoryUnits.ShouldBeTrue();
    }

    [Fact]
    public async Task SearchAsync_OutOfRangePaginationInputs_ShouldClampToSafeDefaults()
    {
        // Arrange
        string tenantId = $"tenant-{Guid.NewGuid():N}";
        for (int i = 0; i < 120; i++)
        {
            await SeedDocumentAsync(tenantId, $"mu-{i:D3}", $"common word appears in document {i}");
        }

        SyntacticSearchService service = CreateService();

        // Act
        SearchResult clamped = await service.SearchAsync(new SearchQuery
        {
            TenantId = tenantId,
            Query = "common word document",
            MaxResults = 500,
            Offset = -50,
        });

        SearchResult baseline = await service.SearchAsync(new SearchQuery
        {
            TenantId = tenantId,
            Query = "common word document",
            MaxResults = 100,
            Offset = 0,
        });

        // Assert
        clamped.Results.Count.ShouldBe(100);
        clamped.TotalCount.ShouldBe(baseline.TotalCount);
        clamped.HasIndexedMemoryUnits.ShouldBeTrue();
        clamped.Results.Select(r => r.MemoryUnitId).ToArray()
            .ShouldBe(baseline.Results.Select(r => r.MemoryUnitId).ToArray());
    }

    [Fact]
    public async Task SearchAsync_QueryInjection_ShouldNotActAsFieldFilter()
    {
        // Arrange
        string tenantId = $"tenant-{Guid.NewGuid():N}";
        await SeedDocumentAsync(tenantId, "mu-1", "normal document content", sourceType: SourceType.File);
        await SeedDocumentAsync(tenantId, "mu-2", "another document content", sourceType: SourceType.Url);

        SyntacticSearchService service = CreateService();

        // Act — inject a field filter as the query
        SearchResult result = await service.SearchAsync(new SearchQuery
        {
            TenantId = tenantId,
            Query = "document @sourceType:{file}",
        });

        // Assert — should NOT filter by sourceType; the @ should be escaped.
        // Safe behavior is either no matches or matches spanning both source types.
        result.HasIndexedMemoryUnits.ShouldBeTrue();
        if (result.Results.Count > 0)
        {
            SourceType[] sourceTypes = result.Results.Select(r => r.SourceType).Distinct().ToArray();
            sourceTypes.ShouldBe([SourceType.File, SourceType.Url], ignoreOrder: true);
        }
    }

    [Fact]
    public async Task SearchAsync_CaseIdInjection_ShouldNotInjectContentFilter()
    {
        // Arrange
        string tenantId = $"tenant-{Guid.NewGuid():N}";
        await SeedDocumentAsync(tenantId, "mu-1", "secret confidential data", caseId: "safe-case");
        await SeedDocumentAsync(tenantId, "mu-2", "public normal data", caseId: "safe-case");

        SyntacticSearchService service = CreateService();

        // Act — inject content filter via caseId
        SearchResult result = await service.SearchAsync(new SearchQuery
        {
            TenantId = tenantId,
            Query = "data",
            CaseId = "} @content:{secret",
        });

        // Assert — the escaped malicious caseId should not match any case tag.
        result.Results.ShouldBeEmpty();
        result.HasIndexedMemoryUnits.ShouldBeTrue();
    }

    [Fact]
    [Trait("Category", "Performance")]
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

    private SyntacticSearchService CreateService()
        => new(_redis.Connection, NullLogger<SyntacticSearchService>.Instance);

    private static void AssertHighlightedSnippet(ScoredResult result)
    {
        result.ContentSnippet.ShouldContain("<b>payment</b>");
        result.ContentSnippet.ShouldContain("<b>outage</b>");
        result.ContentSnippet.Length.ShouldBeLessThanOrEqualTo(SearchSnippetBuilder.MaxSnippetLength + 3);
    }

    private static async Task<(SearchResult Result, long ElapsedMilliseconds)> MeasureSearchAsync(
        SyntacticSearchService service,
        SearchQuery query)
    {
        Stopwatch sw = Stopwatch.StartNew();
        SearchResult result = await service.SearchAsync(query);
        sw.Stop();
        return (result, sw.ElapsedMilliseconds);
    }

    private async Task SeedDocumentAsync(
        string tenantId,
        string memoryUnitId,
        string content,
        string? caseId = null,
        SourceType sourceType = SourceType.File,
        Dictionary<string, MetadataField>? metadata = null)
    {
        IndexInput input = IndexInputFactory.Create(
            tenantId: tenantId,
            memoryUnitId: memoryUnitId,
            content: content,
            caseId: caseId ?? "default-case",
            sourceType: sourceType)
            with
        {
            Metadata = metadata ?? [],
        };

        IndexSyntacticActivity activity = new(
            _redis.Connection,
            NullLogger<IndexSyntacticActivity>.Instance);

        var context = Substitute.For<Dapr.Workflow.WorkflowActivityContext>();
        await activity.RunAsync(context, input);
    }
}
