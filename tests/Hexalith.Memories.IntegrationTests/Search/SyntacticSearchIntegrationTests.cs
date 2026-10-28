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

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public async Task SearchAsync_TiedScores_ShouldKeepOrderedUniqueAdjacentAndRepeatedWindows(bool graphScoped, bool resp3)
    {
        string tenantId = $"tenant-{Guid.NewGuid():N}";
        const int documentCount = 23;
        for (int i = documentCount - 1; i >= 0; i--)
        {
            await SeedDocumentAsync(tenantId, $"mu-{i:D3}", RankedContent(i));
        }

        using ConnectionMultiplexer connection = await ConnectAsync(resp3);
        SyntacticSearchService service = new(connection, NullLogger<SyntacticSearchService>.Instance);
        int[] eligible = Enumerable.Range(0, documentCount).Where(i => !graphScoped || i % 5 != 1).ToArray();
        RedisKey[]? graphKeys = graphScoped
            ? eligible.Select(i => (RedisKey)IndexSchemaDefinitions.BuildSyntacticKey(tenantId, $"mu-{i:D3}")).ToArray()
            : null;
        string[] expected = eligible.OrderByDescending(i => i % 3).ThenBy(i => i)
            .Select(i => $"mu-{i:D3}").ToArray();
        var query = new SearchQuery { TenantId = tenantId, Query = "boundaryrank", MaxResults = documentCount };
        SearchResult all = await service.SearchAsync(query, graphKeys);
        all.Results.Select(result => result.MemoryUnitId).ShouldBe(expected);
        all.Results.Select(result => result.Score).Distinct().Count().ShouldBe(3);

        List<string> walked = [];
        const int pageSize = 5;
        // Include a fully exhausted window for both graph-scoped and plain searches.
        for (int offset = 0; offset <= expected.Length + pageSize; offset += pageSize)
        {
            SearchQuery pageQuery = query with { Offset = offset, MaxResults = pageSize };
            SearchResult page = await service.SearchAsync(pageQuery, graphKeys);
            SearchResult repeated = await service.SearchAsync(pageQuery, graphKeys);
            string[] expectedPage = expected.Skip(offset).Take(pageSize).ToArray();
            page.Results.Select(result => result.MemoryUnitId).ShouldBe(expectedPage);
            repeated.Results.Select(result => result.MemoryUnitId).ShouldBe(expectedPage);
            page.Results.Select(result => result.Score).ShouldBe(page.Results.Select(result => result.Score).OrderDescending());
            page.Results.ShouldAllBe(result => result.ContentSnippet.Contains("<b>boundaryrank</b>", StringComparison.Ordinal));
            new[] { page, repeated }.ShouldAllBe(result => result.TotalCount == eligible.Length && result.HasIndexedMemoryUnits);
            walked.AddRange(page.Results.Select(result => result.MemoryUnitId));
        }

        walked.ShouldBe(expected);
        walked.Distinct(StringComparer.Ordinal).Count().ShouldBe(expected.Length);
    }

    [Theory]
    [InlineData("sourceUri", false, false)]
    [InlineData("sourceUri", false, true)]
    [InlineData("sourceUri", true, false)]
    [InlineData("sourceUri", true, true)]
    [InlineData("sourceType", false, false)]
    [InlineData("sourceType", false, true)]
    [InlineData("sourceType", true, false)]
    [InlineData("sourceType", true, true)]
    public async Task SearchAsync_InvalidDocuments_ShouldConsumeRawPagePositionsAndPreserveTotal(string missingField, bool graphScoped, bool resp3)
    {
        string tenantId = $"tenant-{Guid.NewGuid():N}";
        const int documentCount = 6;
        const int pageSize = 2;
        for (int i = documentCount - 1; i >= 0; i--)
        {
            await SeedDocumentAsync(tenantId, $"mu-{i}", "rawpagingrank identical tied content");
        }

        using ConnectionMultiplexer connection = await ConnectAsync(resp3);
        SyntacticSearchService service = new(connection, NullLogger<SyntacticSearchService>.Instance);
        RedisKey[]? graphKeys = graphScoped
            ? Enumerable.Range(0, documentCount).Select(i => (RedisKey)IndexSchemaDefinitions.BuildSyntacticKey(tenantId, $"mu-{i}")).ToArray()
            : null;
        var query = new SearchQuery { TenantId = tenantId, Query = "rawpagingrank", MaxResults = documentCount };
        SearchResult baseline = await service.SearchAsync(query, graphKeys);
        baseline.Results.Select(result => result.MemoryUnitId).ShouldBe(Enumerable.Range(0, documentCount).Select(i => $"mu-{i}"));
        baseline.Results.Select(result => result.Score).Distinct().Count().ShouldBe(1);

        IDatabase database = connection.GetDatabase();
        foreach (ScoredResult invalidDocument in baseline.Results.Take(pageSize))
        {
            RedisKey key = IndexSchemaDefinitions.BuildSyntacticKey(tenantId, invalidDocument.MemoryUnitId);
            (await database.HashDeleteAsync(key, missingField)).ShouldBeTrue();
            (await database.HashExistsAsync(key, missingField)).ShouldBeFalse();
            (await database.HashExistsAsync(key, "content")).ShouldBeTrue();
        }

        SearchResult first = await service.SearchAsync(query with { MaxResults = pageSize }, graphKeys);
        SearchResult second = await service.SearchAsync(query with { MaxResults = pageSize, Offset = pageSize }, graphKeys);
        SearchResult third = await service.SearchAsync(query with { MaxResults = pageSize, Offset = pageSize * 2 }, graphKeys);
        SearchResult exhausted = await service.SearchAsync(query with { MaxResults = pageSize, Offset = documentCount }, graphKeys);

        first.Results.ShouldBeEmpty();
        second.Results.Select(result => result.MemoryUnitId).ShouldBe(["mu-2", "mu-3"]);
        third.Results.Select(result => result.MemoryUnitId).ShouldBe(["mu-4", "mu-5"]);
        exhausted.Results.ShouldBeEmpty();
        new[] { first, second, third, exhausted }.ShouldAllBe(result => result.TotalCount == documentCount);
        new[] { first, second, third, exhausted }.ShouldAllBe(result => result.HasIndexedMemoryUnits);
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public async Task SearchAsync_ProfileWarningTerm_ShouldRemainSearchable(bool graphScoped, bool resp3)
    {
        string tenantId = $"tenant-{Guid.NewGuid():N}";
        await SeedDocumentAsync(tenantId, "mu-warning", "warning searchable content");
        using ConnectionMultiplexer connection = await ConnectAsync(resp3);
        SyntacticSearchService service = new(connection, NullLogger<SyntacticSearchService>.Instance);
        RedisKey key = IndexSchemaDefinitions.BuildSyntacticKey(tenantId, "mu-warning");

        SearchResult result = await service.SearchAsync(
            new SearchQuery { TenantId = tenantId, Query = "warning" }, graphScoped ? [key] : null);

        result.Results.ShouldHaveSingleItem().MemoryUnitId.ShouldBe("mu-warning");
        result.TotalCount.ShouldBe(1);
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public async Task SearchAsync_WhitespaceCaseId_ShouldNormalizeToNull(bool graphScoped, bool resp3)
    {
        string tenantId = $"tenant-{Guid.NewGuid():N}";
        await SeedDocumentAsync(tenantId, "mu-whitespace", "whitespace case compatibility");
        using ConnectionMultiplexer connection = await ConnectAsync(resp3);
        RedisKey key = IndexSchemaDefinitions.BuildSyntacticKey(tenantId, "mu-whitespace");
        await connection.GetDatabase().HashSetAsync(key, "caseId", " \t ");
        (await connection.GetDatabase().HashGetAsync(key, "caseId")).ToString().ShouldBe(" \t ");
        SyntacticSearchService service = new(connection, NullLogger<SyntacticSearchService>.Instance);

        SearchResult result = await service.SearchAsync(
            new SearchQuery { TenantId = tenantId, Query = "compatibility" }, graphScoped ? [key] : null);

        result.Results.ShouldHaveSingleItem().CaseId.ShouldBeNull();
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public async Task SearchAsync_CuratedUpserts_ShouldPageEachTenantIdentityAndPreserveSourceUri(bool graphScoped, bool resp3)
    {
        string tenantId = $"tenant-{Guid.NewGuid():N}";
        using ConnectionMultiplexer connection = await ConnectAsync(resp3);
        IDatabase db = connection.GetDatabase();
        ProvisionSyntacticIndex(db, tenantId);
        RedisSearchIndexMaintenanceAdapter adapter = new(connection, NullLogger<RedisSearchIndexMaintenanceAdapter>.Instance);
        const int documentCount = 13;
        for (int i = documentCount - 1; i >= 0; i--)
        {
            string identity = $"tenant-{i:D3}";
            var entry = new SearchIndexEntryChanged { TenantId = tenantId, AggregateId = identity, Text = "oldcuratedtext" };
            await adapter.ApplyEntryChangedAsync(tenantId, $"tenant:{identity}", entry, null, CancellationToken.None);
            await adapter.ApplyEntryChangedAsync(tenantId, $"tenant:{identity}", entry with { Text = "curatedpaging current snapshot" }, null, CancellationToken.None);
            (await db.HashGetAsync(IndexSchemaDefinitions.BuildSyntacticKey(tenantId, identity), "content")).ToString()
                .ShouldBe("curatedpaging current snapshot");
        }

        SyntacticSearchService service = new(connection, NullLogger<SyntacticSearchService>.Instance);
        RedisKey[]? graphKeys = graphScoped
            ? Enumerable.Range(0, documentCount).Select(i => (RedisKey)IndexSchemaDefinitions.BuildSyntacticKey(tenantId, $"tenant-{i:D3}")).ToArray()
            : null;
        List<string> sourceUris = [];
        for (int offset = 0; offset <= documentCount + 4; offset += 4)
        {
            SearchResult page = await service.SearchAsync(
                new SearchQuery { TenantId = tenantId, Query = "curatedpaging", Offset = offset, MaxResults = 4 }, graphKeys);
            page.TotalCount.ShouldBe(documentCount);
            page.Results.ShouldAllBe(result => result.SourceUri == $"tenant:{result.MemoryUnitId}" && result.SourceType == SourceType.Event);
            sourceUris.AddRange(page.Results.Select(result => result.SourceUri));
        }

        sourceUris.ShouldBe(Enumerable.Range(0, documentCount).Select(i => $"tenant:tenant-{i:D3}"));
        sourceUris.Distinct(StringComparer.Ordinal).Count().ShouldBe(documentCount);
        (await service.SearchAsync(new SearchQuery { TenantId = tenantId, Query = "oldcuratedtext" })).TotalCount.ShouldBe(0);
    }

    [Fact]
    public async Task SearchAsync_BroadMatch_ShouldRespectMaxResultsCap()
    {
        // Arrange — seed more than the direct service candidate window with a common word.
        string tenantId = $"tenant-{Guid.NewGuid():N}";
        int documentCount = SearchPaginationOptions.MaxCandidateWindow + 20;
        for (int i = 0; i < documentCount; i++)
        {
            await SeedDocumentAsync(tenantId, $"mu-{i:D3}", $"common word appears in document {i}");
        }

        SyntacticSearchService service = CreateService();

        // Act
        SearchResult result = await service.SearchAsync(new SearchQuery
        {
            TenantId = tenantId,
            Query = "common word document",
            MaxResults = SearchPaginationOptions.MaxCandidateWindow + 50,
        });

        // Assert
        result.Results.Count.ShouldBe(SearchPaginationOptions.MaxCandidateWindow);
        result.TotalCount.ShouldBe(documentCount);
        result.HasIndexedMemoryUnits.ShouldBeTrue();
    }

    [Fact]
    public async Task SearchAsync_OutOfRangePaginationInputs_ShouldClampToSafeDefaults()
    {
        // Arrange
        string tenantId = $"tenant-{Guid.NewGuid():N}";
        int documentCount = SearchPaginationOptions.MaxCandidateWindow + 20;
        for (int i = 0; i < documentCount; i++)
        {
            await SeedDocumentAsync(tenantId, $"mu-{i:D3}", $"common word appears in document {i}");
        }

        SyntacticSearchService service = CreateService();

        // Act
        SearchResult clamped = await service.SearchAsync(new SearchQuery
        {
            TenantId = tenantId,
            Query = "common word document",
            MaxResults = SearchPaginationOptions.MaxCandidateWindow + 50,
            Offset = -50,
        });

        SearchResult baseline = await service.SearchAsync(new SearchQuery
        {
            TenantId = tenantId,
            Query = "common word document",
            MaxResults = SearchPaginationOptions.MaxCandidateWindow,
            Offset = 0,
        });

        // Assert
        clamped.Results.Count.ShouldBe(SearchPaginationOptions.MaxCandidateWindow);
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

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public async Task SearchAsync_DeepWindowBeyondAggregateCap_ShouldRejectPartialSuccess(bool graphScoped, bool resp3)
    {
        // Startup configuration belongs only to this test's disposable server. Do not mutate
        // the shared fixture or rely on equal search/aggregate caps in supported deployments.
        await using var container = new ContainerBuilder(RedisStackFixture.RedisStackImage)
            .WithEnvironment("REDISEARCH_ARGS", "MAXAGGREGATERESULTS 7 MAXSEARCHRESULTS 1000")
            .WithPortBinding(0, 6379)
            .WithWaitStrategy(Wait.ForUnixContainer()
                .UntilInternalTcpPortIsAvailable(6379)
                .UntilExternalTcpPortIsAvailable(6379)
                .UntilCommandIsCompleted("redis-cli", "PING"))
            .Build();
        await container.StartAsync();
        ConfigurationOptions options = ConfigurationOptions.Parse(RedisStackFixture.OwnedLoopbackConnectionString(container.GetMappedPublicPort(6379)));
        options.Protocol = resp3 ? RedisProtocol.Resp3 : RedisProtocol.Resp2;
        using ConnectionMultiplexer connection = await ConnectionMultiplexer.ConnectAsync(options);
        IDatabase db = connection.GetDatabase();
        string tenantId = $"tenant-{Guid.NewGuid():N}";
        const int documentCount = 23;
        ProvisionSyntacticIndex(db, tenantId);
        for (int i = documentCount - 1; i >= 0; i--)
        {
            await SeedRawDocumentAsync(db, tenantId, $"mu-{i:D3}", "capqualification tied content");
        }

        RedisKey[]? graphKeys = graphScoped
            ? Enumerable.Range(0, documentCount).Select(i => (RedisKey)IndexSchemaDefinitions.BuildSyntacticKey(tenantId, $"mu-{i:D3}")).ToArray()
            : null;
        string indexName = IndexSchemaDefinitions.GetSyntacticIndexName(tenantId);
        List<object> rawArguments = SyntacticSearchService.BuildOrderedWindowArguments(indexName, "capqualification", graphKeys, 10, 5);
        RedisResult raw = await db.ExecuteAsync("FT.AGGREGATE", rawArguments, CommandFlags.None);
        // Negative control: the old aggregate strategy silently called this valid page empty.
        ReadRawTotal(raw).ShouldBe(documentCount);
        ReadRawRows(raw).ShouldBe(0);
        TestContext.Current.SendDiagnosticMessage($"cap mismatch negative control RESP{(resp3 ? 3 : 2)} graph={graphScoped}: total {ReadRawTotal(raw)}, rows {ReadRawRows(raw)}");
        SyntacticSearchService service = new(connection, NullLogger<SyntacticSearchService>.Instance);
        var query = new SearchQuery { TenantId = tenantId, Query = "capqualification", Offset = 10, MaxResults = 5 };
        RedisConnectionException error = await Should.ThrowAsync<RedisConnectionException>(() => service.SearchAsync(query, graphKeys));
        error.FailureType.ShouldBe(ConnectionFailureType.ProtocolFailure);
        error.Message.ShouldContain("Incomplete RediSearch ordered window");

        // A complete prefix still succeeds, preserving relevance/identity order and raw totals.
        SearchResult first = await service.SearchAsync(query with { Offset = 0 }, graphKeys);
        first.Results.Select(result => result.MemoryUnitId).ShouldBe(Enumerable.Range(0, 5).Select(i => $"mu-{i:D3}"));
        first.TotalCount.ShouldBe(documentCount);
        SearchResult exhausted = await service.SearchAsync(query with { Offset = documentCount }, graphKeys);
        exhausted.Results.ShouldBeEmpty();
        exhausted.TotalCount.ShouldBe(documentCount);
        (await db.HashGetAsync(IndexSchemaDefinitions.BuildSyntacticKey(tenantId, "mu-010"), "sourceUri")).ToString()
            .ShouldBe("tenant:mu-010");
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task SearchAsync_TimeoutReturnPolicy_ShouldRejectIncompleteExecutionInBothProtocols(bool resp3)
    {
        // The general fixture intentionally uses FAIL for its legacy tests. This disposable
        // server keeps Redis's default RETURN policy independently of native fixture settings.
        await using var container = new ContainerBuilder(RedisStackFixture.RedisStackImage)
            .WithPortBinding(0, 6379)
            .WithWaitStrategy(Wait.ForUnixContainer()
                .UntilInternalTcpPortIsAvailable(6379)
                .UntilExternalTcpPortIsAvailable(6379)
                .UntilCommandIsCompleted("redis-cli", "PING"))
            .Build();
        await container.StartAsync();
        using ConnectionMultiplexer connection = await ConnectAsync(resp3,
            RedisStackFixture.OwnedLoopbackConnectionString(container.GetMappedPublicPort(6379)));
        IDatabase db = connection.GetDatabase();
        string tenantId = $"tenant-{Guid.NewGuid():N}";
        ProvisionSyntacticIndex(db, tenantId);
        RedisResult policy = await db.ExecuteAsync("FT.CONFIG", "GET", "ON_TIMEOUT");
        RedisResult[] policyEntries = (RedisResult[])policy!;
        policyEntries.SelectMany(entry => entry.Resp2Type == ResultType.Array ? (RedisResult[])entry! : [entry])
            .Select(entry => entry.ToString()).ShouldContain("return");
        string indexName = IndexSchemaDefinitions.GetSyntacticIndexName(tenantId);
        var query = new SearchQuery { TenantId = tenantId, Query = "timeoutqualification", MaxResults = 5 };
        SyntacticSearchService timedService = CreateForwardedService(connection, (command, args) =>
        {
            args.AddRange(["TIMEOUT", 1]);
            return db.ExecuteAsync(command, args, CommandFlags.None);
        });
        int documentCount = 0;
        long partialCount = -1;
        RedisTimeoutException? observedTimeout = null;
        // Bound both corpus growth and attempts. A fast worker escalates workload rather than
        // relying on a single machine-dependent 50,000-document/one-millisecond observation.
        foreach (int targetCount in new[] { 5_000, 20_000, 50_000, 100_000, 200_000 })
        {
            for (int offset = documentCount; offset < targetCount; offset += 1_000)
            {
                await Task.WhenAll(Enumerable.Range(offset, Math.Min(1_000, targetCount - offset)).Select(i =>
                    SeedRawDocumentAsync(db, tenantId, $"mu-{i:D6}", "timeoutqualification common corpus")));
            }

            documentCount = targetCount;
            for (int attempt = 0; attempt < 3 && observedTimeout is null; attempt++)
            {
                List<object> originalArguments = SyntacticSearchService.BuildOrderedWindowArguments(indexName, query.Query, null, 0, 5);
                originalArguments.AddRange(["TIMEOUT", 1]);
                partialCount = ReadRawTotal(await db.ExecuteAsync("FT.AGGREGATE", originalArguments, CommandFlags.None));
                if (partialCount >= documentCount)
                {
                    continue;
                }

                try
                {
                    _ = await timedService.SearchAsync(query);
                }
                catch (RedisTimeoutException timeout)
                {
                    observedTimeout = timeout;
                }
            }

            if (observedTimeout is not null)
            {
                break;
            }
        }

        observedTimeout.ShouldNotBeNull("bounded workload escalation must establish a real incomplete timeout response");
        observedTimeout.Message.ShouldContain("Incomplete RediSearch execution");
        partialCount.ShouldBeGreaterThanOrEqualTo(0);
        partialCount.ShouldBeLessThan(documentCount);
        ReadRawTotal(await db.ExecuteAsync("FT.SEARCH", indexName, query.Query, "LIMIT", 0, 0)).ShouldBe(documentCount);

        // Qualify ordinary bounded execution on exactly the corpus that demonstrated timeout,
        // retaining the existing separate 200ms concurrent-query latency contract as well.
        SyntacticSearchService normalService = new(connection, NullLogger<SyntacticSearchService>.Instance);
        Stopwatch normalWatch = Stopwatch.StartNew();
        SearchResult normal = await normalService.SearchAsync(query)
            .WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
        normalWatch.Stop();
        normal.TotalCount.ShouldBe(documentCount);
        normal.Results.Select(result => result.MemoryUnitId).ShouldBe(Enumerable.Range(0, 5).Select(i => $"mu-{i:D6}"));
        normal.HasIndexedMemoryUnits.ShouldBeTrue();
        normalWatch.ElapsedMilliseconds.ShouldBeLessThan(5_000);
        TestContext.Current.SendDiagnosticMessage($"timeout RETURN control RESP{(resp3 ? 3 : 2)}: partial {partialCount}/{documentCount}; normal bounded search {normalWatch.ElapsedMilliseconds}ms, total {normal.TotalCount}, rows {normal.Results.Count}");
        (await db.HashGetAsync(IndexSchemaDefinitions.BuildSyntacticKey(tenantId, "mu-000001"), "content")).ToString()
            .ShouldBe("timeoutqualification common corpus");
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public async Task SearchAsync_SelectedHashDeletedBeforeHydration_ShouldRefuseChangedRawPositions(bool graphScoped, bool resp3)
    {
        using ConnectionMultiplexer connection = await ConnectAsync(resp3);
        IDatabase db = connection.GetDatabase();
        string tenantId = $"tenant-{Guid.NewGuid():N}";
        ProvisionSyntacticIndex(db, tenantId);
        RedisKey[] keys = Enumerable.Range(0, 3).Select(i => (RedisKey)IndexSchemaDefinitions.BuildSyntacticKey(tenantId, $"mu-{i:D3}")).ToArray();
        for (int i = 0; i < keys.Length; i++)
        {
            await SeedRawDocumentAsync(db, tenantId, $"mu-{i:D3}", "hydrationrace equal content");
        }

        bool deleted = false;
        SyntacticSearchService service = CreateForwardedService(connection, async (command, args) =>
        {
            if (args[1].ToString() == "SEARCH")
            {
                deleted = await db.KeyDeleteAsync(keys[0]);
                (await db.KeyExistsAsync(keys[0])).ShouldBeFalse();
            }

            return await db.ExecuteAsync(command, args, CommandFlags.None);
        });
        RedisConnectionException error = await Should.ThrowAsync<RedisConnectionException>(() => service.SearchAsync(
            new SearchQuery { TenantId = tenantId, Query = "hydrationrace", MaxResults = 3 }, graphScoped ? keys : null));
        error.FailureType.ShouldBe(ConnectionFailureType.ProtocolFailure);
        error.Message.ShouldContain("hydration");
        deleted.ShouldBeTrue();
        (await db.KeyExistsAsync(keys[0])).ShouldBeFalse();
        ReadRawTotal(await db.ExecuteAsync("FT.SEARCH", IndexSchemaDefinitions.GetSyntacticIndexName(tenantId), "hydrationrace", "LIMIT", 0, 0)).ShouldBe(2);
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public async Task SearchAsync_HydrationReturnsFewerIdentitiesThanAdvertised_ShouldRefusePartialWindow(bool graphScoped, bool resp3)
    {
        using ConnectionMultiplexer connection = await ConnectAsync(resp3);
        IDatabase db = connection.GetDatabase();
        string tenantId = $"tenant-{Guid.NewGuid():N}";
        ProvisionSyntacticIndex(db, tenantId);
        RedisKey[] keys = Enumerable.Range(0, 3).Select(i => (RedisKey)IndexSchemaDefinitions.BuildSyntacticKey(tenantId, $"mu-{i:D3}")).ToArray();
        for (int i = 0; i < keys.Length; i++)
        {
            await SeedRawDocumentAsync(db, tenantId, $"mu-{i:D3}", "hydrationtruncation equal content");
        }

        bool truncated = false;
        SyntacticSearchService service = CreateForwardedService(connection, async (command, args) =>
        {
            if (args[1].ToString() == "SEARCH")
            {
                args[args.IndexOf("LIMIT") + 2] = 1;
                RedisResult actual = await db.ExecuteAsync(command, args, CommandFlags.None);
                RedisResult result = ProfileQueryResult(actual, resp3);
                ReadRawTotal(result).ShouldBe(3);
                ReadRawRows(result, withScores: true).ShouldBe(1);
                truncated = true;
                return actual;
            }

            return await db.ExecuteAsync(command, args, CommandFlags.None);
        });
        RedisConnectionException error = await Should.ThrowAsync<RedisConnectionException>(() => service.SearchAsync(
            new SearchQuery { TenantId = tenantId, Query = "hydrationtruncation", MaxResults = 3 }, graphScoped ? keys : null));
        error.FailureType.ShouldBe(ConnectionFailureType.ProtocolFailure);
        error.Message.ShouldContain("returned identities");
        truncated.ShouldBeTrue();
        (await db.KeyExistsAsync(keys[0])).ShouldBeTrue();
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public async Task SearchAsync_HydrationOnlyCompletionWarning_ShouldRefuseSuccessfulSelection(bool graphScoped, bool resp3)
    {
        using ConnectionMultiplexer connection = await ConnectAsync(resp3);
        IDatabase db = connection.GetDatabase();
        string tenantId = $"tenant-{Guid.NewGuid():N}";
        await SeedDocumentAsync(tenantId, "mu-hydration", "hydrationwarning matching content");
        RedisKey key = IndexSchemaDefinitions.BuildSyntacticKey(tenantId, "mu-hydration");
        bool selectionCompleted = false;
        bool hydrationWarned = false;
        SyntacticSearchService service = CreateForwardedService(connection, async (command, args) =>
        {
            RedisResult actual = await db.ExecuteAsync(command, args, CommandFlags.None);
            if (args[1].ToString() == "AGGREGATE")
            {
                ReadRawTotal(ProfileQueryResult(actual, resp3)).ShouldBe(1);
                selectionCompleted = true;
                return actual;
            }

            selectionCompleted.ShouldBeTrue();
            hydrationWarned = true;
            return WithTimeoutWarning(actual, resp3);
        });
        RedisTimeoutException error = await Should.ThrowAsync<RedisTimeoutException>(() => service.SearchAsync(
            new SearchQuery { TenantId = tenantId, Query = "hydrationwarning" }, graphScoped ? [key] : null));
        error.Message.ShouldContain("Incomplete RediSearch execution");
        selectionCompleted.ShouldBeTrue();
        hydrationWarned.ShouldBeTrue();
        (await db.HashGetAsync(key, "content")).ToString().ShouldBe("hydrationwarning matching content");
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public async Task SearchAsync_Resp3CountOnly_ShouldDistinguishEmptyIndexAndNoMatches(bool emptyGraph, bool seedDocument)
    {
        using ConnectionMultiplexer connection = await ConnectAsync(resp3: true);
        IDatabase db = connection.GetDatabase();
        string tenantId = $"tenant-{Guid.NewGuid():N}";
        ProvisionSyntacticIndex(db, tenantId);
        if (seedDocument)
        {
            await SeedRawDocumentAsync(db, tenantId, "mu-count", "unrelated document content");
        }

        SyntacticSearchService service = new(connection, NullLogger<SyntacticSearchService>.Instance);
        SearchResult result = await service.SearchAsync(
            new SearchQuery { TenantId = tenantId, Query = "nomatchcountqualification" }, emptyGraph ? [] : null);
        result.Results.ShouldBeEmpty();
        result.TotalCount.ShouldBe(0);
        result.HasIndexedMemoryUnits.ShouldBe(seedDocument);
    }

    private static SyntacticSearchService CreateForwardedService(
        IConnectionMultiplexer connection,
        Func<string, List<object>, Task<RedisResult>> execute)
    {
        IDatabase db = connection.GetDatabase();
        IDatabase forwardedDb = Substitute.For<IDatabase>();
        forwardedDb.StringGetAsync(Arg.Any<RedisKey>(), Arg.Any<CommandFlags>())
            .Returns(call => db.StringGetAsync(call.Arg<RedisKey>(), call.Arg<CommandFlags>()));
        forwardedDb.ExecuteAsync(Arg.Any<string>(), Arg.Any<ICollection<object>>(), Arg.Any<CommandFlags>())
            .Returns(call => execute(call.Arg<string>(), [.. call.Arg<ICollection<object>>() ]));
        IConnectionMultiplexer forwarded = Substitute.For<IConnectionMultiplexer>();
        forwarded.GetDatabase(Arg.Any<int>(), Arg.Any<object>()).Returns(forwardedDb);
        return new SyntacticSearchService(forwarded, NullLogger<SyntacticSearchService>.Instance);
    }

    private static RedisResult ProfileQueryResult(RedisResult profile, bool resp3)
        => resp3 ? profile : ((RedisResult[])profile!)[0];

    private static RedisResult WithTimeoutWarning(RedisResult profile, bool resp3)
    {
        RedisResult[] values = (RedisResult[])profile!;
        if (!resp3)
        {
            return RedisResult.Create([values[0], RedisResult.Create([
                RedisResult.Create([RedisResult.Create((RedisValue)"Warning"), RedisResult.Create((RedisValue)"Timeout limit was reached")])])]);
        }

        int profileIndex = Array.FindIndex(values, value => value.ToString() == "profile");
        RedisResult[] details = (RedisResult[])values[profileIndex + 1]!;
        int warningIndex = Array.FindIndex(details, value => value.ToString() == "Warning");
        details[warningIndex + 1] = RedisResult.Create((RedisValue)"Timeout limit was reached");
        values[profileIndex + 1] = RedisResult.Create(details);
        return RedisResult.Create(values);
    }

    private static int ReadRawRows(RedisResult result, bool withScores = false)
    {
        RedisResult[] entries = (RedisResult[]?)result ?? [];
        if (long.TryParse(entries[0].ToString(), out _))
        {
            return withScores ? (entries.Length - 1) / 3 : entries.Length - 1;
        }

        for (int i = 0; i + 1 < entries.Length; i += 2)
        {
            if (entries[i].ToString() == "results")
            {
                return ((RedisResult[]?)entries[i + 1] ?? []).Length;
            }
        }

        throw new InvalidOperationException("Missing raw Redis rows.");
    }

    private static long ReadRawTotal(RedisResult result)
    {
        RedisResult[] entries = (RedisResult[]?)result ?? [];
        if (long.TryParse(entries[0].ToString(), out long total))
        {
            return total;
        }

        for (int i = 0; i + 1 < entries.Length; i += 2)
        {
            if (entries[i].ToString() == "total_results")
            {
                return long.Parse(entries[i + 1].ToString(), System.Globalization.CultureInfo.InvariantCulture);
            }
        }

        throw new InvalidOperationException("Missing raw Redis total.");
    }

    private static Task SeedRawDocumentAsync(IDatabase db, string tenantId, string memoryUnitId, string content)
        => db.HashSetAsync(IndexSchemaDefinitions.BuildSyntacticKey(tenantId, memoryUnitId),
        [
            new HashEntry("content", content),
            new HashEntry("sourceUri", $"tenant:{memoryUnitId}"),
            new HashEntry("sourceType", "file"),
        ]);

    private async Task<ConnectionMultiplexer> ConnectAsync(bool resp3, string? connectionString = null)
    {
        ConfigurationOptions options = ConfigurationOptions.Parse(connectionString ?? _redis.ConnectionString);
        options.Protocol = resp3 ? RedisProtocol.Resp3 : RedisProtocol.Resp2;
        return await ConnectionMultiplexer.ConnectAsync(options);
    }

    private static string RankedContent(int index)
        => string.Join(' ', Enumerable.Repeat("boundaryrank", index % 3 + 1)
            .Concat(Enumerable.Repeat("filler", 2 - index % 3)));

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
        ProvisionSyntacticIndex(_redis.Connection.GetDatabase(), tenantId);
        await activity.RunAsync(context, input);
    }

    private static void ProvisionSyntacticIndex(IDatabase db, string tenantId)
        => TryCreateIndex(() => db.FT().Create(
            IndexSchemaDefinitions.GetSyntacticIndexName(tenantId),
            IndexSchemaDefinitions.CreateSyntacticParams(tenantId),
            IndexSchemaDefinitions.CreateSyntacticSchema()));

    private static void TryCreateIndex(Action create)
    {
        try
        {
            create();
        }
        catch (RedisServerException ex) when (ex.Message.Contains("Index already exists", StringComparison.OrdinalIgnoreCase))
        {
        }
    }
}
