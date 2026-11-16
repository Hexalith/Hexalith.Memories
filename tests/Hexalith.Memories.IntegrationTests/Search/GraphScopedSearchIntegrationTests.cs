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

/// <summary>
/// Integration tests verifying GraphScopedSearch against real FalkorDB + Redis Stack.
/// Validates traversal, depth limiting, tenant isolation, enrichment, and latency.
/// </summary>
[Collection("GraphSearch")]
[Trait("Category", "Integration")]
public partial class GraphScopedSearchIntegrationTests
{
    private readonly CompositeSearchFixture _fixture;
    private readonly GraphQueryBuilder _builder = new();

    public GraphScopedSearchIntegrationTests(CompositeSearchFixture fixture) => _fixture = fixture;





















    private GraphScopedSearch CreateService()
    {
        return new GraphScopedSearch(
            _fixture.FalkorDbConnection,
            _fixture.RedisConnection,
            _builder,
            NullLogger<GraphScopedSearch>.Instance);
    }

    /// <summary>Seeds a chain of memory units with CAUSED_BY edges: A→B→C...</summary>
    private async Task SeedGraphChainAsync(string tenantId, string caseId, params string[] nodeIds)
    {
        FalkorDB falkor = new(_fixture.FalkorDbConnection.GetDatabase());

        // Create case node
        (string caseQuery, IDictionary<string, object> caseParams) = _builder.BuildMergeCaseNode(caseId);
        await falkor.SelectGraph(tenantId).QueryAsync(caseQuery, caseParams);

        for (int i = 0; i < nodeIds.Length; i++)
        {
            await CreateMemoryUnitNodeAsync(falkor, tenantId, nodeIds[i], caseId);

            // CONTAINS edge: case → MU
            (string containsQ, IDictionary<string, object> containsP) = _builder.BuildMergeEdge(
                caseId, nodeIds[i], EdgeType.Contains, EdgeTypeDefaults.Contains, EdgeOrigin.Explicit);
            await falkor.SelectGraph(tenantId).QueryAsync(containsQ, containsP);

            // CAUSED_BY edge to previous node
            if (i > 0)
            {
                (string edgeQ, IDictionary<string, object> edgeP) = _builder.BuildMergeEdge(
                    nodeIds[i - 1], nodeIds[i], EdgeType.CausedBy, EdgeTypeDefaults.CausedBy, EdgeOrigin.Explicit);
                await falkor.SelectGraph(tenantId).QueryAsync(edgeQ, edgeP);
            }
        }
    }

    private async Task CreateMemoryUnitNodeAsync(FalkorDB falkor, string tenantId, string muId, string caseId)
    {
        (string q, IDictionary<string, object> p) = _builder.BuildMergeMemoryUnitNode(
            muId, caseId, $"Content for {muId}", $"hash-{muId}",
            $"file:///{muId}.txt", SourceType.File, "provider", 3,
            "test@example.com", DateTimeOffset.UtcNow, "{}");
        await falkor.SelectGraph(tenantId).QueryAsync(q, p);
    }

    private async Task CreateCausedByEdgeAsync(FalkorDB falkor, string tenantId, string sourceId, string targetId)
    {
        (string q, IDictionary<string, object> p) = _builder.BuildMergeEdge(
            sourceId, targetId, EdgeType.CausedBy, EdgeTypeDefaults.CausedBy, EdgeOrigin.Explicit);
        await falkor.SelectGraph(tenantId).QueryAsync(q, p);
    }

    private async Task SeedSyntacticHashAsync(
        string tenantId,
        string memoryUnitId,
        string content,
        string? sourceUri = null,
        SourceType sourceType = SourceType.File,
        string? caseId = null)
    {
        IDatabase db = _fixture.RedisConnection.GetDatabase();
        string key = $"{tenantId}:mu:{memoryUnitId}";
        List<HashEntry> entries =
        [
            new("content", content),
            new("sourceUri", sourceUri ?? $"file:///{memoryUnitId}.txt"),
            new("sourceType", sourceType.ToString().ToLowerInvariant()),
        ];
        if (!string.IsNullOrWhiteSpace(caseId))
        {
            entries.Add(new HashEntry("caseId", caseId));
        }

        await db.HashSetAsync(key, [.. entries]);
    }

    private static ScoredResult CreateScoredResult(string memoryUnitId, double score, string axis) => new()
    {
        MemoryUnitId = memoryUnitId,
        Score = score,
        ContentSnippet = $"Snippet for {memoryUnitId}",
        SourceUri = $"file:///{memoryUnitId}.txt",
        SourceType = SourceType.File,
        Axis = axis,
    };

    private const int TestDimensions = 768;

    private static float[] CreateVector(params float[] leadingValues)
    {
        float[] vector = new float[TestDimensions];
        Array.Copy(leadingValues, vector, leadingValues.Length);
        return vector;
    }

    /// <summary>Seeds a memory unit's syntactic hash (content/source/metadata for enrichment) and its raw
    /// semantic vector hash (for KNN) on the fixture's Redis Stack connection.</summary>
    // Story 23.7 (A34): indexing activities no longer create indexes on demand (creation is owned by
    // TenantProvisioningWorkflow), so raw-Redis integration seeds must provision the tenant indexes explicitly
    // before writing — mirroring the schema TenantProvisioningWorkflow creates.
    private void ProvisionTenantIndexes(string tenantId, int dimensions)
    {
        IDatabase db = _fixture.RedisConnection.GetDatabase();
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

    private async Task SeedSemanticDocumentAsync(
        string tenantId,
        string memoryUnitId,
        string content,
        string caseId,
        float[] vector,
        Dictionary<string, MetadataField> metadata,
        SourceType sourceType = SourceType.File)
    {
        IndexInput input = IndexInputFactory.Create(
            tenantId: tenantId,
            memoryUnitId: memoryUnitId,
            content: content,
            caseId: caseId,
            sourceType: sourceType,
            embeddingVector: vector,
            embeddingDimensions: TestDimensions)
            with
        {
            Metadata = metadata,
        };

        var context = Substitute.For<Dapr.Workflow.WorkflowActivityContext>();
        ProvisionTenantIndexes(tenantId, TestDimensions);
        await new IndexSyntacticActivity(_fixture.RedisConnection, NullLogger<IndexSyntacticActivity>.Instance)
            .RunAsync(context, input);
        await new IndexSemanticActivity(_fixture.RedisConnection, NullLogger<IndexSemanticActivity>.Instance)
            .RunAsync(context, input);
    }

    private async Task SeedChunkedSemanticDocumentAsync(
        string tenantId,
        string memoryUnitId,
        string content,
        string caseId,
        IReadOnlyList<ChunkEmbeddingResult> chunks,
        Dictionary<string, MetadataField>? metadata = null,
        SourceType sourceType = SourceType.File)
    {
        IndexInput input = IndexInputFactory.Create(
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
        ProvisionTenantIndexes(tenantId, TestDimensions);
        await new IndexSyntacticActivity(_fixture.RedisConnection, NullLogger<IndexSyntacticActivity>.Instance)
            .RunAsync(context, input);

        await new IndexSemanticChunksActivity(_fixture.RedisConnection, NullLogger<IndexSemanticChunksActivity>.Instance)
            .RunAsync(
                context,
                new SemanticChunkIndexInput
                {
                    TenantId = tenantId,
                    MemoryUnitId = memoryUnitId,
                    CaseId = caseId,
                    Chunks = chunks,
                    EmbeddingProvider = "google",
                    EmbeddingModel = "gemini-embedding-001",
                    EmbeddingDimensions = TestDimensions,
                    Metadata = metadata ?? [],
                });
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
