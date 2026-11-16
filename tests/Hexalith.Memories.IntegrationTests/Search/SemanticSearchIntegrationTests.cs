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

/// <summary>
/// Integration tests verifying SemanticSearchService against a real Redis Stack instance.
/// Validates KNN ranking, tenant isolation, case scoping, cosine similarity, and latency.
/// </summary>
[Collection("RedisStack")]
[Trait("Category", "Integration")]
public partial class SemanticSearchIntegrationTests
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
        ProvisionTenantIndexes(db, tenantId, TestDimensions);

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

    // Story 23.7 (A34): indexing activities no longer create indexes on demand (creation is owned by
    // TenantProvisioningWorkflow), so raw-Redis integration seeds must provision the tenant indexes explicitly
    // before writing — mirroring the schema TenantProvisioningWorkflow creates.
    private static void ProvisionTenantIndexes(IDatabase db, string tenantId, int dimensions)
    {
        TryCreateIndex(() => db.FT().Create(
            IndexSchemaDefinitions.GetSyntacticIndexName(tenantId),
            IndexSchemaDefinitions.CreateSyntacticParams(tenantId),
            IndexSchemaDefinitions.CreateSyntacticSchema()));
        TryCreateIndex(() => db.FT().Create(
            IndexSchemaDefinitions.GetSemanticIndexName(tenantId),
            IndexSchemaDefinitions.CreateSemanticParams(tenantId),
            IndexSchemaDefinitions.CreateSemanticSchema(dimensions)));
        TryCreateIndex(() => db.FT().Create(
            IndexSchemaDefinitions.GetNaturalLanguageSemanticIndexName(tenantId),
            IndexSchemaDefinitions.CreateNaturalLanguageSemanticParams(tenantId),
            IndexSchemaDefinitions.CreateNaturalLanguageSemanticSchema(dimensions)));
    }

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
        ProvisionTenantIndexes(_redis.Connection.GetDatabase(), tenantId, TestDimensions);

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

    private async Task SeedChunkedDocumentAsync(
        string tenantId,
        string memoryUnitId,
        string content,
        string caseId,
        IReadOnlyList<ChunkEmbeddingResult> chunks,
        SourceType sourceType = SourceType.File,
        Dictionary<string, MetadataField>? metadata = null)
    {
        IndexInput syntacticInput = IndexInputFactory.Create(
            tenantId: tenantId,
            memoryUnitId: memoryUnitId,
            content: content,
            caseId: caseId,
            sourceType: sourceType,
            embeddingVector: chunks[0].Vector,
            embeddingDimensions: TestDimensions)
            with
        {
            Metadata = metadata ?? [],
        };

        var context = Substitute.For<Dapr.Workflow.WorkflowActivityContext>();
        ProvisionTenantIndexes(_redis.Connection.GetDatabase(), tenantId, TestDimensions);
        await new IndexSyntacticActivity(_redis.Connection, NullLogger<IndexSyntacticActivity>.Instance)
            .RunAsync(context, syntacticInput);

        SemanticChunkIndexInput semanticInput = new()
        {
            TenantId = tenantId,
            MemoryUnitId = memoryUnitId,
            CaseId = caseId,
            Chunks = chunks,
            EmbeddingProvider = "google",
            EmbeddingModel = "gemini-embedding-001",
            EmbeddingDimensions = TestDimensions,
            Metadata = metadata ?? [],
        };

        await new IndexSemanticChunksActivity(_redis.Connection, NullLogger<IndexSemanticChunksActivity>.Instance)
            .RunAsync(context, semanticInput);
    }

    private static ChunkEmbeddingResult CreateChunk(
        int sequence,
        string text,
        int startOffset,
        int endOffset,
        float[] vector)
        => new()
        {
            Sequence = sequence,
            Text = text,
            StartOffset = startOffset,
            EndOffset = endOffset,
            EstimatedTokens = Math.Max(1, text.Length / 4),
            Vector = vector,
        };

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

    private static void AssertDescendingScoreThenId(IReadOnlyList<ScoredResult> results)
    {
        results.ShouldBe(
            [.. results
                .OrderByDescending(static r => r.Score)
                .ThenBy(static r => r.MemoryUnitId, StringComparer.Ordinal)]);
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
