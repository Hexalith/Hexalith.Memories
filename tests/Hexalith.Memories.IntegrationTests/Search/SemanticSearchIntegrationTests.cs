namespace Hexalith.Memories.IntegrationTests.Search;

using System.Diagnostics;
using System.Runtime.InteropServices;

using Dapr.Client;

using Hexalith.Memories.Contracts.V1;
using Hexalith.Memories.IntegrationTests.Fixtures;
using Hexalith.Memories.Server.Activities.Indexing;
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

/// <summary>
/// Integration tests verifying SemanticSearchService against a real Redis Stack instance.
/// Validates KNN ranking, tenant isolation, case scoping, cosine similarity, and latency.
/// </summary>
[Collection("RedisStack")]
[Trait("Category", "Integration")]
public class SemanticSearchIntegrationTests
{
    private const int TestDimensions = 768;

    private readonly RedisStackFixture _redis;
    private readonly EmbeddingClient _embeddingClient;
    private readonly TenantEmbeddingConfig _embeddingConfig;

    public SemanticSearchIntegrationTests(RedisStackFixture redis)
    {
        _redis = redis;

        _embeddingClient = new EmbeddingClient(
            Substitute.For<IHttpClientFactory>(),
            Substitute.For<DaprClient>(),
            CreateFakeEmbeddingConfiguration(),
            CreateDevelopmentHostEnvironment());

        _embeddingConfig = EmbeddingProviderDefaults.Google();

        // Safety guard: fail fast if fake embedding mode is not active
        Assert.True(
            CreateFakeEmbeddingConfiguration().GetValue<bool>("Memories:Testing:UseFakeEmbedding"),
            "Integration tests must use fake embeddings. Set Memories:Testing:UseFakeEmbedding=true.");
    }

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
    [Trait("Category", "Performance")]
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

    private SemanticSearchService CreateService()
        => new(
            _redis.Connection,
            _embeddingClient,
            NullLogger<SemanticSearchService>.Instance);

    private async Task SeedPerformanceDocumentsAsync(string tenantId, int documentCount)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(documentCount, 1);

        const int BatchSize = 250;
        const string CaseId = "default-case";

        IDatabase db = _redis.Connection.GetDatabase();
        float[] sharedVector = CreateVector(1.0f, 0.0f, 0.0f);
        byte[] vectorBytes = MemoryMarshal.AsBytes(sharedVector.AsSpan()).ToArray();

        var context = Substitute.For<Dapr.Workflow.WorkflowActivityContext>();
        IndexSemanticActivity semanticActivity = new(
            _redis.Connection,
            NullLogger<IndexSemanticActivity>.Instance);

        IndexInput firstInput = IndexInputFactory.Create(
            tenantId: tenantId,
            memoryUnitId: "mu-00000",
            content: "performance test document 0 with searchable content",
            caseId: CaseId,
            sourceUri: "file:///perf-0.txt",
            sourceType: SourceType.File,
            embeddingVector: sharedVector,
            embeddingDimensions: TestDimensions);

        await semanticActivity.RunAsync(context, firstInput);
        await SetSyntacticHashAsync(db, tenantId, firstInput.MemoryUnitId, firstInput.Content, firstInput.SourceUri, "file");

        for (int batchStart = 1; batchStart < documentCount; batchStart += BatchSize)
        {
            int batchEnd = Math.Min(batchStart + BatchSize, documentCount);
            IBatch batch = db.CreateBatch();
            List<Task> writes = [];

            for (int i = batchStart; i < batchEnd; i++)
            {
                string memoryUnitId = $"mu-{i:D5}";
                string content = $"performance test document {i} with searchable content";
                string sourceUri = $"file:///perf-{i}.txt";

                writes.Add(batch.HashSetAsync(
                    $"{tenantId}:vec:{memoryUnitId}",
                    [
                        new HashEntry("embedding", vectorBytes),
                        new HashEntry("memoryUnitId", memoryUnitId),
                        new HashEntry("caseId", CaseId),
                    ]));

                writes.Add(batch.HashSetAsync(
                    $"{tenantId}:mu:{memoryUnitId}",
                    [
                        new HashEntry("content", content),
                        new HashEntry("sourceUri", sourceUri),
                        new HashEntry("sourceType", "file"),
                    ]));
            }

            batch.Execute();
            await Task.WhenAll(writes);
        }
    }

    private async Task WaitForVectorIndexCountAsync(string tenantId, long expectedCount)
    {
        IDatabase db = _redis.Connection.GetDatabase();
        DateTimeOffset deadline = DateTimeOffset.UtcNow.AddMinutes(2);

        while (DateTimeOffset.UtcNow < deadline)
        {
            RedisSearchResult countResult = await db.FT()
                .SearchAsync($"{tenantId}:memories:vec", new RedisQuery("*").Limit(0, 0).Dialect(2));

            if (countResult.TotalResults >= expectedCount)
            {
                return;
            }

            await Task.Delay(250);
        }

        throw new TimeoutException(
            $"Redis Vector index for tenant '{tenantId}' did not reach {expectedCount} documents within the timeout.");
    }

    private static async Task<(SearchResult Result, long ElapsedMilliseconds)> MeasureSearchAsync(
        SemanticSearchService service,
        SearchQuery query)
    {
        Stopwatch sw = Stopwatch.StartNew();
        SearchResult result = await service.SearchAsync(
            query,
            EmbeddingProviderDefaults.Google(),
            CancellationToken.None);
        sw.Stop();
        return (result, sw.ElapsedMilliseconds);
    }

    private async Task SeedDocumentAsync(
        string tenantId,
        string memoryUnitId,
        string content,
        string? caseId = null,
        string? sourceUri = null,
        SourceType sourceType = SourceType.File,
        EmbeddingClient? embeddingClient = null,
        float[]? embeddingVector = null,
        Dictionary<string, MetadataField>? metadata = null)
    {
        // Generate deterministic vector from content text (fake embedding mode)
        EmbeddingClient client = embeddingClient ?? _embeddingClient;
        float[] vector = embeddingVector ?? await client.GenerateAsync(
            content, tenantId, _embeddingConfig, CancellationToken.None);

        IndexInput input = IndexInputFactory.Create(
            tenantId: tenantId,
            memoryUnitId: memoryUnitId,
            content: content,
            caseId: caseId ?? "default-case",
            sourceUri: sourceUri,
            sourceType: sourceType,
            embeddingVector: vector,
            embeddingDimensions: TestDimensions)
            with
        {
            Metadata = metadata ?? [],
        };

        var context = Substitute.For<Dapr.Workflow.WorkflowActivityContext>();

        // Seed syntactic index (content for enrichment)
        var syntacticActivity = new IndexSyntacticActivity(
            _redis.Connection,
            NullLogger<IndexSyntacticActivity>.Instance);
        await syntacticActivity.RunAsync(context, input);

        // Seed semantic index (vector for KNN)
        var semanticActivity = new IndexSemanticActivity(
            _redis.Connection,
            NullLogger<IndexSemanticActivity>.Instance);
        await semanticActivity.RunAsync(context, input);
    }

    private static IConfiguration CreateFakeEmbeddingConfiguration()
        => new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Memories:Testing:UseFakeEmbedding"] = "true",
            })
            .Build();

    private static IHostEnvironment CreateDevelopmentHostEnvironment()
    {
        IHostEnvironment hostEnv = Substitute.For<IHostEnvironment>();
        hostEnv.EnvironmentName.Returns("Development");
        return hostEnv;
    }

    private static float[] CreateVector(params float[] leadingValues)
    {
        float[] vector = new float[TestDimensions];
        Array.Copy(leadingValues, vector, leadingValues.Length);
        return vector;
    }

    private static Task SetSyntacticHashAsync(
        IDatabase db,
        string tenantId,
        string memoryUnitId,
        string content,
        string sourceUri,
        string sourceType)
        => db.HashSetAsync(
            $"{tenantId}:mu:{memoryUnitId}",
            [
                new HashEntry("content", content),
                new HashEntry("sourceUri", sourceUri),
                new HashEntry("sourceType", sourceType),
            ]);

    private sealed class OverrideEmbeddingClient : EmbeddingClient
    {
        private readonly Dictionary<string, float[]> _overrides = new(StringComparer.Ordinal);

        public OverrideEmbeddingClient()
            : base(
                Substitute.For<IHttpClientFactory>(),
                Substitute.For<DaprClient>(),
                CreateFakeEmbeddingConfiguration(),
                CreateDevelopmentHostEnvironment())
        {
        }

        public void SetVector(string text, float[] vector)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(text);
            ArgumentNullException.ThrowIfNull(vector);
            _overrides[text] = vector;
        }

        public override Task<float[]> GenerateAsync(
            string text,
            string tenantId,
            TenantEmbeddingConfig config,
            CancellationToken ct)
            => _overrides.TryGetValue(text, out float[]? vector)
                ? Task.FromResult(vector)
                : base.GenerateAsync(text, tenantId, config, ct);
    }
}
