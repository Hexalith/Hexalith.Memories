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

/// <summary>Contains the fast integration cases for SemanticSearchIntegrationTests.</summary>
public sealed partial class SemanticSearchIntegrationTests
{
    [Fact]
    public async Task SearchAsync_KnnRanking_ShouldReturnClosestVectorFirst()
    {
        // Arrange — seed 3 docs; query with text matching one exactly
        string tenantId = $"tenant-{Guid.NewGuid():N}";
        OverrideEmbeddingClient embeddingClient = new();
        float[] alphaVector = CreateVector(1.0f, 0.0f, 0.0f);
        float[] betaVector = CreateVector(0.0f, 1.0f, 0.0f);
        float[] gammaVector = CreateVector(0.0f, 0.0f, 1.0f);
        embeddingClient.SetVector("alpha document content", alphaVector);
        embeddingClient.SetVector("beta document content", betaVector);
        embeddingClient.SetVector("gamma document content", gammaVector);

        await SeedDocumentAsync(tenantId, "mu-alpha", "alpha document content", embeddingClient: embeddingClient);
        await SeedDocumentAsync(tenantId, "mu-beta", "beta document content", embeddingClient: embeddingClient);
        await SeedDocumentAsync(tenantId, "mu-gamma", "gamma document content", embeddingClient: embeddingClient);

        SemanticSearchService service = new(
            _redis.Connection,
            embeddingClient,
            NullLogger<SemanticSearchService>.Instance);

        // Act — query with "alpha document content" → identical vector → distance 0, similarity 1.0
        SearchResult result = await service.SearchAsync(
            new SearchQuery { TenantId = tenantId, Query = "alpha document content" },
            _embeddingConfig,
            CancellationToken.None);

        // Assert
        result.Results.Count.ShouldBeGreaterThanOrEqualTo(1);
        result.Results[0].MemoryUnitId.ShouldBe("mu-alpha");
        result.Results[0].Score.ShouldBe(1.0, 0.001); // Identical text → similarity 1.0
    }

    [Fact]
    public async Task SearchAsync_EmptyResults_ShouldReturnEmptyNotException()
    {
        // Arrange — query against a tenant that was never seeded
        string tenantId = $"tenant-never-seeded-{Guid.NewGuid():N}";
        SemanticSearchService service = CreateService();

        // Act
        SearchResult result = await service.SearchAsync(
            new SearchQuery { TenantId = tenantId, Query = "anything" },
            _embeddingConfig,
            CancellationToken.None);

        // Assert
        result.Results.ShouldBeEmpty();
        result.TotalCount.ShouldBe(0);
        result.HasIndexedMemoryUnits.ShouldBeFalse();
        result.Query.ShouldBe("anything");
    }

    [Fact]
    public async Task SearchAsync_TenantIsolation_ShouldNotReturnCrossTenantResults()
    {
        // Arrange
        string tenantA = $"tenant-a-{Guid.NewGuid():N}";
        string tenantB = $"tenant-b-{Guid.NewGuid():N}";

        await SeedDocumentAsync(tenantA, "mu-a", "shared content text");
        await SeedDocumentAsync(tenantB, "mu-b", "shared content text");

        SemanticSearchService service = CreateService();

        // Act — query tenant A only
        SearchResult resultA = await service.SearchAsync(
            new SearchQuery { TenantId = tenantA, Query = "shared content text" },
            _embeddingConfig,
            CancellationToken.None);

        // Assert
        resultA.Results.ShouldAllBe(r => r.MemoryUnitId == "mu-a");
        resultA.Results.ShouldNotContain(r => r.MemoryUnitId == "mu-b");
    }

    [Fact]
    public async Task SearchAsync_CaseScoping_ShouldFilterByCase()
    {
        // Arrange
        string tenantId = $"tenant-{Guid.NewGuid():N}";
        await SeedDocumentAsync(tenantId, "mu-case1", "important financial data", caseId: "case-alpha");
        await SeedDocumentAsync(tenantId, "mu-case2", "important financial data", caseId: "case-beta");

        SemanticSearchService service = CreateService();

        // Act — query with case filter
        SearchResult result = await service.SearchAsync(
            new SearchQuery { TenantId = tenantId, Query = "important financial data", CaseId = "case-alpha" },
            _embeddingConfig,
            CancellationToken.None);

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

        SemanticSearchService service = CreateService();

        SearchResult result = await service.SearchAsync(
            new SearchQuery
            {
                TenantId = tenantId,
                Query = "claim event",
                CloudEventSubject = "claim-42",
            },
            _embeddingConfig,
            CancellationToken.None);

        result.Results.Select(r => r.MemoryUnitId).ShouldBe(["mu-subject-a"]);
    }

    [Fact]
    public async Task SearchAsync_CosineSimilarityRange_AllScoresShouldBeInRange()
    {
        // Arrange
        string tenantId = $"tenant-{Guid.NewGuid():N}";
        await SeedDocumentAsync(tenantId, "mu-1", "first document content");
        await SeedDocumentAsync(tenantId, "mu-2", "second document content");
        await SeedDocumentAsync(tenantId, "mu-3", "third document content");

        SemanticSearchService service = CreateService();

        // Act
        SearchResult result = await service.SearchAsync(
            new SearchQuery { TenantId = tenantId, Query = "document content", MaxResults = 10 },
            _embeddingConfig,
            CancellationToken.None);

        // Assert — all scores must be in [0.0, 1.0]
        result.Results.ShouldAllBe(r => r.Score >= 0.0 && r.Score <= 1.0);
    }

    [Fact]
    public async Task SearchAsync_ContentEnrichment_ShouldIncludeContentAndSourceFields()
    {
        // Arrange
        string tenantId = $"tenant-{Guid.NewGuid():N}";
        await SeedDocumentAsync(tenantId, "mu-enrich", "enrichment test content",
            sourceUri: "file:///enrichment-test.pdf", sourceType: SourceType.Url);

        SemanticSearchService service = CreateService();

        // Act
        SearchResult result = await service.SearchAsync(
            new SearchQuery { TenantId = tenantId, Query = "enrichment test content" },
            _embeddingConfig,
            CancellationToken.None);

        // Assert
        result.Results.Count.ShouldBeGreaterThanOrEqualTo(1);
        ScoredResult first = result.Results[0];
        first.ContentSnippet.ShouldContain("enrichment test content");
        first.SourceUri.ShouldBe("file:///enrichment-test.pdf");
        first.SourceType.ShouldBe(SourceType.Url);
        first.Axis.ShouldBe("semantic");
    }

    [Fact]
    public async Task SearchAsync_MissingSourceUri_ShouldSkipGracefully()
    {
        string tenantId = $"tenant-{Guid.NewGuid():N}";
        await SeedDocumentAsync(tenantId, "mu-missing-source", "content with missing source uri");

        IDatabase db = _redis.Connection.GetDatabase();
        await db.HashDeleteAsync($"{tenantId}:mu:mu-missing-source", "sourceUri");

        SemanticSearchService service = CreateService();

        SearchResult result = await service.SearchAsync(
            new SearchQuery { TenantId = tenantId, Query = "content with missing source uri" },
            _embeddingConfig,
            CancellationToken.None);

        result.Results.ShouldBeEmpty();
    }

    [Fact]
    public async Task SearchAsync_MissingSyntacticHash_ShouldSkipGracefully()
    {
        // Arrange — seed vector only (no syntactic hash)
        string tenantId = $"tenant-{Guid.NewGuid():N}";

        // Seed semantic index only (no syntactic)
        float[] vector = await _embeddingClient.GenerateAsync(
            "vector only content", tenantId, _embeddingConfig, CancellationToken.None);

        IndexInput input = IndexInputFactory.Create(
            tenantId: tenantId,
            memoryUnitId: "mu-vector-only",
            content: "vector only content",
            caseId: "default-case",
            embeddingVector: vector,
            embeddingDimensions: TestDimensions);

        ProvisionTenantIndexes(_redis.Connection.GetDatabase(), tenantId, TestDimensions);
        var semanticActivity = new IndexSemanticActivity(
            _redis.Connection,
            NullLogger<IndexSemanticActivity>.Instance);
        var context = Substitute.For<Dapr.Workflow.WorkflowActivityContext>();
        await semanticActivity.RunAsync(context, input);

        // Do NOT run IndexSyntacticActivity — simulate missing syntactic hash

        SemanticSearchService service = CreateService();

        // Act
        SearchResult result = await service.SearchAsync(
            new SearchQuery { TenantId = tenantId, Query = "vector only content" },
            _embeddingConfig,
            CancellationToken.None);

        // Assert — result should be empty because enrichment skips missing syntactic hashes
        result.Results.ShouldBeEmpty();
    }

    [Fact]
    public async Task SearchAsync_SyntacticOnlyDocument_ShouldNotAppearInSemanticResults()
    {
        // Arrange — seed only syntactic hash, no vector index
        string tenantId = $"tenant-{Guid.NewGuid():N}";

        // Seed one doc with both indexes, one with syntactic only
        await SeedDocumentAsync(tenantId, "mu-both", "both indexes content");

        // Seed syntactic only (no semantic)
        IndexInput syntacticOnly = IndexInputFactory.Create(
            tenantId: tenantId,
            memoryUnitId: "mu-syntactic-only",
            content: "syntactic only content",
            caseId: "default-case",
            embeddingVector: IndexInputFactory.CreateRealisticVector(TestDimensions),
            embeddingDimensions: TestDimensions);

        ProvisionTenantIndexes(_redis.Connection.GetDatabase(), tenantId, TestDimensions);
        var syntacticActivity = new IndexSyntacticActivity(
            _redis.Connection,
            NullLogger<IndexSyntacticActivity>.Instance);
        var context = Substitute.For<Dapr.Workflow.WorkflowActivityContext>();
        await syntacticActivity.RunAsync(context, syntacticOnly);

        SemanticSearchService service = CreateService();

        // Act
        SearchResult result = await service.SearchAsync(
            new SearchQuery { TenantId = tenantId, Query = "content", MaxResults = 10 },
            _embeddingConfig,
            CancellationToken.None);

        // Assert — syntactic-only doc should not appear
        result.Results.ShouldNotContain(r => r.MemoryUnitId == "mu-syntactic-only");
    }

    [Fact]
    public async Task SearchAsync_SemanticMatchWithoutKeywordOverlap_ShouldReturnRelatedResult()
    {
        // Arrange — use explicit vector overrides so the query is semantically aligned with the claim document
        string tenantId = $"tenant-{Guid.NewGuid():N}";
        OverrideEmbeddingClient embeddingClient = new();
        float[] claimVector = CreateVector(1.0f, 0.0f, 0.0f);
        float[] unrelatedVector = CreateVector(0.0f, 1.0f, 0.0f);

        embeddingClient.SetVector("claim denied due to policy expiration", claimVector);
        embeddingClient.SetVector("payment rejection", claimVector);
        embeddingClient.SetVector("sunny weather forecast for today", unrelatedVector);

        await SeedDocumentAsync(
            tenantId,
            "mu-claim",
            "claim denied due to policy expiration",
            embeddingClient: embeddingClient);
        await SeedDocumentAsync(
            tenantId,
            "mu-weather",
            "sunny weather forecast for today",
            embeddingClient: embeddingClient);

        SemanticSearchService service = new(
            _redis.Connection,
            embeddingClient,
            NullLogger<SemanticSearchService>.Instance);

        // Act — query has no keyword overlap with the claim document
        SearchResult result = await service.SearchAsync(
            new SearchQuery { TenantId = tenantId, Query = "payment rejection", MaxResults = 10 },
            _embeddingConfig,
            CancellationToken.None);

        // Assert — the semantically aligned claim document should rank first
        result.Results.Count.ShouldBeGreaterThanOrEqualTo(1);
        result.Results[0].MemoryUnitId.ShouldBe("mu-claim");
        result.Results[0].Score.ShouldBe(1.0, 0.001);
        result.Results.ShouldNotContain(r => r.MemoryUnitId == "mu-weather" && r.Score > result.Results[0].Score);
    }

    [Fact]
    public async Task SearchAsync_WithOffset_ShouldReturnDisjointStableSemanticPages()
    {
        string tenantId = $"tenant-{Guid.NewGuid():N}";
        const string CaseId = "case-alpha";
        OverrideEmbeddingClient embeddingClient = new();

        embeddingClient.SetVector("pagination query", CreateVector(1.0f, 0.0f, 0.0f));
        embeddingClient.SetVector("pagination rank 1", CreateVector(1.0f, 0.0f, 0.0f));
        embeddingClient.SetVector("pagination rank 2", CreateVector(0.9f, 0.1f, 0.0f));
        embeddingClient.SetVector("pagination rank 3", CreateVector(0.8f, 0.2f, 0.0f));
        embeddingClient.SetVector("pagination rank 4", CreateVector(0.8f, 0.2f, 0.0f));
        embeddingClient.SetVector("pagination other case", CreateVector(1.0f, 0.0f, 0.0f));

        await SeedDocumentAsync(tenantId, "mu-page-01", "pagination rank 1", caseId: CaseId, embeddingClient: embeddingClient);
        await SeedDocumentAsync(tenantId, "mu-page-02", "pagination rank 2", caseId: CaseId, embeddingClient: embeddingClient);
        await SeedDocumentAsync(tenantId, "mu-page-03", "pagination rank 3", caseId: CaseId, embeddingClient: embeddingClient);
        await SeedDocumentAsync(tenantId, "mu-page-04", "pagination rank 4", caseId: CaseId, embeddingClient: embeddingClient);
        await SeedDocumentAsync(tenantId, "mu-page-other-case", "pagination other case", caseId: "case-beta", embeddingClient: embeddingClient);

        SemanticSearchService service = new(
            _redis.Connection,
            embeddingClient,
            NullLogger<SemanticSearchService>.Instance);

        SearchQuery firstPageQuery = new()
        {
            TenantId = tenantId,
            Query = "pagination query",
            CaseId = CaseId,
            MaxResults = 2,
            Offset = 0,
        };

        SearchResult firstPage = await service.SearchAsync(
            firstPageQuery,
            _embeddingConfig,
            CancellationToken.None);
        SearchResult secondPage = await service.SearchAsync(
            firstPageQuery with { Offset = 2 },
            _embeddingConfig,
            CancellationToken.None);

        firstPage.Results.Select(static r => r.MemoryUnitId).ShouldBe(["mu-page-01", "mu-page-02"]);
        secondPage.Results.Select(static r => r.MemoryUnitId).ShouldBe(["mu-page-03", "mu-page-04"]);
        firstPage.Results.Select(static r => r.MemoryUnitId).Intersect(secondPage.Results.Select(static r => r.MemoryUnitId)).ShouldBeEmpty();
        secondPage.Results.ShouldAllBe(static r => r.CaseId == CaseId);
        firstPage.Results.Concat(secondPage.Results).ShouldNotContain(static r => r.MemoryUnitId == "mu-page-other-case");
        AssertDescendingScoreThenId(firstPage.Results);
        AssertDescendingScoreThenId(secondPage.Results);
    }

    [Fact]
    public async Task SearchAsync_ChunkedMemoryUnit_ShouldDeduplicateBestChunkBeforePagination()
    {
        string tenantId = $"tenant-{Guid.NewGuid():N}";
        OverrideEmbeddingClient embeddingClient = new();
        embeddingClient.SetVector("chunk best probe", CreateVector(1.0f, 0.0f, 0.0f));

        await SeedChunkedDocumentAsync(
            tenantId,
            "mu-chunked",
            "full content from two semantic chunks",
            "default-case",
            [
                CreateChunk(sequence: 0, text: "weaker chunk", startOffset: 0, endOffset: 12, vector: CreateVector(0.70f, 0.30f, 0.0f)),
                CreateChunk(sequence: 1, text: "best matching chunk", startOffset: 12, endOffset: 31, vector: CreateVector(1.0f, 0.0f, 0.0f)),
            ]);
        await SeedDocumentAsync(
            tenantId,
            "mu-other",
            "single semantic vector",
            embeddingVector: CreateVector(0.90f, 0.10f, 0.0f));

        SemanticSearchService service = new(
            _redis.Connection,
            embeddingClient,
            NullLogger<SemanticSearchService>.Instance);

        SearchQuery firstPageQuery = new()
        {
            TenantId = tenantId,
            Query = "chunk best probe",
            MaxResults = 2,
        };

        SearchResult firstPage = await service.SearchAsync(firstPageQuery, _embeddingConfig, CancellationToken.None);
        SearchResult secondPage = await service.SearchAsync(
            firstPageQuery with { MaxResults = 1, Offset = 1 },
            _embeddingConfig,
            CancellationToken.None);

        firstPage.TotalCount.ShouldBe(2);
        firstPage.Results.Select(r => r.MemoryUnitId).ShouldBe(["mu-chunked", "mu-other"]);
        firstPage.Results.Count(r => r.MemoryUnitId == "mu-chunked").ShouldBe(1);
        firstPage.Results[0].ContentSnippet.ShouldContain("full content from two semantic chunks");
        secondPage.TotalCount.ShouldBe(2);
        secondPage.Results.Select(r => r.MemoryUnitId).ShouldBe(["mu-other"]);
    }

    [Fact]
    public async Task SearchAsync_AxisParameterRouting_ShouldReturnCorrectAxisResults()
    {
        // Arrange — seed document with both indexes
        string tenantId = $"tenant-{Guid.NewGuid():N}";
        await SeedDocumentAsync(tenantId, "mu-routing", "routing test content");

        // Verify via semantic search
        SemanticSearchService semanticService = CreateService();
        SearchResult semanticResult = await semanticService.SearchAsync(
            new SearchQuery { TenantId = tenantId, Query = "routing test content" },
            _embeddingConfig,
            CancellationToken.None);

        // Verify via syntactic search
        SyntacticSearchService syntacticService = new(
            _redis.Connection,
            NullLogger<SyntacticSearchService>.Instance);
        SearchResult syntacticResult = await syntacticService.SearchAsync(
            new SearchQuery { TenantId = tenantId, Query = "routing test content" });

        // Assert
        semanticResult.Results.Count.ShouldBeGreaterThanOrEqualTo(1);
        semanticResult.Results[0].Axis.ShouldBe("semantic");

        syntacticResult.Results.Count.ShouldBeGreaterThanOrEqualTo(1);
        syntacticResult.Results[0].Axis.ShouldBe("syntactic");
    }

    [Fact]
    public async Task SearchAsync_MetadataFilterBeyondInitialWindow_ShouldReturnLaterFilteredMatches()
    {
        // Story 22.6 (A49): the nearest KNN neighbours fail the metadata filter while later neighbours
        // match. A fixed offset+max candidate window would return empty/undersized; the bounded
        // over-fetch must recover the later matches and report an honest filtered TotalCount.
        string tenantId = $"tenant-{Guid.NewGuid():N}";
        OverrideEmbeddingClient embeddingClient = new();
        embeddingClient.SetVector("recall probe", CreateVector(1.0f, 0.0f, 0.0f));
        embeddingClient.SetVector("nearest one", CreateVector(1.0f, 0.0f, 0.0f));
        embeddingClient.SetVector("nearest two", CreateVector(0.95f, 0.05f, 0.0f));
        embeddingClient.SetVector("farther one", CreateVector(0.80f, 0.20f, 0.0f));
        embeddingClient.SetVector("farther two", CreateVector(0.70f, 0.30f, 0.0f));

        Dictionary<string, MetadataField> nonMatching = new() { ["customer"] = new("globex", MetadataOrigin.Ai, 1.0f) };
        Dictionary<string, MetadataField> matching = new() { ["customer"] = new("acme", MetadataOrigin.Ai, 1.0f) };

        await SeedDocumentAsync(tenantId, "mu-near-1", "nearest one", metadata: nonMatching, embeddingClient: embeddingClient);
        await SeedDocumentAsync(tenantId, "mu-near-2", "nearest two", metadata: nonMatching, embeddingClient: embeddingClient);
        await SeedDocumentAsync(tenantId, "mu-far-1", "farther one", metadata: matching, embeddingClient: embeddingClient);
        await SeedDocumentAsync(tenantId, "mu-far-2", "farther two", metadata: matching, embeddingClient: embeddingClient);

        SemanticSearchService service = new(
            _redis.Connection, embeddingClient, NullLogger<SemanticSearchService>.Instance);

        // Small page size — matching docs live at ranks 3-4, beyond the initial offset+max window.
        SearchResult result = await service.SearchAsync(
            new SearchQuery
            {
                TenantId = tenantId,
                Query = "recall probe",
                MetadataQuery = "acme",
                MaxResults = 2,
            },
            _embeddingConfig,
            CancellationToken.None);

        result.Results.Select(r => r.MemoryUnitId).ShouldBe(["mu-far-1", "mu-far-2"]);
        result.TotalCount.ShouldBe(2);
        result.Results.ShouldNotContain(r => r.MemoryUnitId == "mu-near-1" || r.MemoryUnitId == "mu-near-2");
    }

    [Fact]
    public async Task SearchAsync_SourceTypeFilterBeyondInitialWindow_ShouldReturnLaterFilteredMatches()
    {
        // Story 22.6 (A49): sourceType is applied as a bounded service-side post-filter, not a fake KNN
        // @sourceType pre-filter (sourceType is not indexed on the semantic vector hash). The nearest
        // neighbours are the wrong source type; later neighbours match and must be recovered.
        string tenantId = $"tenant-{Guid.NewGuid():N}";
        OverrideEmbeddingClient embeddingClient = new();
        embeddingClient.SetVector("source probe", CreateVector(1.0f, 0.0f, 0.0f));
        embeddingClient.SetVector("file near 1", CreateVector(1.0f, 0.0f, 0.0f));
        embeddingClient.SetVector("file near 2", CreateVector(0.95f, 0.05f, 0.0f));
        embeddingClient.SetVector("url far 1", CreateVector(0.80f, 0.20f, 0.0f));
        embeddingClient.SetVector("url far 2", CreateVector(0.70f, 0.30f, 0.0f));

        await SeedDocumentAsync(tenantId, "mu-file-1", "file near 1", sourceType: SourceType.File, embeddingClient: embeddingClient);
        await SeedDocumentAsync(tenantId, "mu-file-2", "file near 2", sourceType: SourceType.File, embeddingClient: embeddingClient);
        await SeedDocumentAsync(tenantId, "mu-url-1", "url far 1", sourceType: SourceType.Url, embeddingClient: embeddingClient);
        await SeedDocumentAsync(tenantId, "mu-url-2", "url far 2", sourceType: SourceType.Url, embeddingClient: embeddingClient);

        SemanticSearchService service = new(
            _redis.Connection, embeddingClient, NullLogger<SemanticSearchService>.Instance);

        SearchResult result = await service.SearchAsync(
            new SearchQuery
            {
                TenantId = tenantId,
                Query = "source probe",
                SourceTypeFilter = "url",
                MaxResults = 2,
            },
            _embeddingConfig,
            CancellationToken.None);

        result.Results.Select(r => r.MemoryUnitId).ShouldBe(["mu-url-1", "mu-url-2"]);
        result.Results.ShouldAllBe(r => r.SourceType == SourceType.Url);
        result.TotalCount.ShouldBe(2);
    }
}
