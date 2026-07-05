namespace Hexalith.Memories.IntegrationTests.Indexing;

using System.Runtime.InteropServices;

using Hexalith.Memories.Contracts.V1;
using Hexalith.Memories.IntegrationTests.Fixtures;
using Hexalith.Memories.Server.Activities.Indexing;
using Hexalith.Memories.Server.Activities.Ingestion;
using Hexalith.Memories.Server.Infrastructure;
using Hexalith.Memories.TestHelpers.Factories;

using Microsoft.Extensions.Logging.Abstractions;

using NRedisStack.RedisStackCommands;
using NRedisStack.Search;

using NSubstitute;

using Shouldly;

using StackExchange.Redis;

/// <summary>
/// Integration tests verifying IndexSemanticActivity against a real Redis Stack instance.
/// Validates vector storage, KNN retrieval, and tenant isolation for Redis Vector Search.
/// </summary>
[Collection("RedisStack")]
[Trait("Category", "Integration")]
public class IndexSemanticIntegrationTests
{
    private readonly RedisStackFixture _redis;

    public IndexSemanticIntegrationTests(RedisStackFixture redis) => _redis = redis;

    [Fact]
    public async Task RunAsync_ShouldStoreVectorAndRetrieveViaKnn_InRealRedis()
    {
        // Arrange
        string tenantId = $"tenant-sem-{Guid.NewGuid():N}";
        IndexInput input = IndexInputFactory.Create(tenantId: tenantId);

        IConnectionMultiplexer redis = _redis.Connection;
        IndexSemanticActivity activity = new(redis, NullLogger<IndexSemanticActivity>.Instance);

        var context = Substitute.For<Dapr.Workflow.WorkflowActivityContext>();
        ProvisionSemanticIndex(redis.GetDatabase(), tenantId, input.EmbeddingDimensions);

        // Act
        IndexResult result = await activity.RunAsync(context, input);

        // Assert — result
        result.Backend.ShouldBe("semantic");
        result.MemoryUnitId.ShouldBe(input.MemoryUnitId);
        result.TenantId.ShouldBe(tenantId);

        // Assert — HASH exists in Redis with correct vector bytes
        IDatabase db = redis.GetDatabase();
        RedisValue storedVector = await db.HashGetAsync(
            $"{tenantId}:vec:{input.MemoryUnitId}",
            "embedding");

        storedVector.IsNull.ShouldBeFalse("Embedding vector should be stored in Redis HASH");

        byte[] expectedBytes = MemoryMarshal.AsBytes(input.EmbeddingVector.AsSpan()).ToArray();
        ((byte[])storedVector!).ShouldBe(expectedBytes);
    }

    [Fact]
    public async Task RunAsync_WithProvisionedIndex_ShouldBeSearchableViaKnn_InRealRedis()
    {
        // Arrange
        string tenantId = $"tenant-sem-{Guid.NewGuid():N}";
        float[] queryVector = [0.1f, 0.2f, 0.3f];
        IndexInput input = IndexInputFactory.Create(
            tenantId: tenantId,
            embeddingVector: queryVector,
            embeddingDimensions: 3);

        IConnectionMultiplexer redis = _redis.Connection;
        IndexSemanticActivity activity = new(redis, NullLogger<IndexSemanticActivity>.Instance);

        var context = Substitute.For<Dapr.Workflow.WorkflowActivityContext>();
        ProvisionSemanticIndex(redis.GetDatabase(), tenantId, input.EmbeddingDimensions);

        // Act
        await activity.RunAsync(context, input);

        // Assert — FT.SEARCH with KNN finds the vector
        IDatabase db = redis.GetDatabase();
        var ft = db.FT();

        byte[] queryBytes = MemoryMarshal.AsBytes(queryVector.AsSpan()).ToArray();

        var searchResult = ft.Search(
            $"{tenantId}:memories:vec",
            new Query("*=>[KNN 1 @embedding $vec AS score]")
                .AddParam("vec", queryBytes)
                .SetSortBy("score")
                .Limit(0, 1)
                .Dialect(2));

        searchResult.TotalResults.ShouldBeGreaterThan(0, "KNN search should find the indexed vector");
    }

    [Fact]
    public async Task RunAsync_TenantIsolation_ShouldNotLeakAcrossTenants()
    {
        // Arrange — two different tenants
        string tenantA = $"tenant-sem-a-{Guid.NewGuid():N}";
        string tenantB = $"tenant-sem-b-{Guid.NewGuid():N}";

        IndexInput inputA = IndexInputFactory.Create(tenantId: tenantA);
        IndexInput inputB = IndexInputFactory.Create(tenantId: tenantB);

        IConnectionMultiplexer redis = _redis.Connection;
        var context = Substitute.For<Dapr.Workflow.WorkflowActivityContext>();

        IndexSemanticActivity activityA = new(redis, NullLogger<IndexSemanticActivity>.Instance);
        IndexSemanticActivity activityB = new(redis, NullLogger<IndexSemanticActivity>.Instance);
        IDatabase db = redis.GetDatabase();
        ProvisionSemanticIndex(db, tenantA, inputA.EmbeddingDimensions);
        ProvisionSemanticIndex(db, tenantB, inputB.EmbeddingDimensions);

        // Act
        await activityA.RunAsync(context, inputA);
        await activityB.RunAsync(context, inputB);

        // Assert — tenant A's hash key is not visible under tenant B's prefix
        RedisValue crossTenantCheck = await db.HashGetAsync(
            $"{tenantB}:vec:{inputA.MemoryUnitId}",
            "embedding");

        crossTenantCheck.IsNull.ShouldBeTrue("Tenant A's vector should not be accessible under tenant B's namespace");
    }

    [Fact]
    public async Task RunAsync_IdempotentReindex_ShouldOverwriteExistingVector()
    {
        // Arrange
        string tenantId = $"tenant-sem-{Guid.NewGuid():N}";
        string memoryUnitId = $"mu-idem-{Guid.NewGuid():N}";

        float[] vectorV1 = [1.0f, 0.0f, 0.0f];
        float[] vectorV2 = [0.0f, 1.0f, 0.0f];

        IndexInput inputV1 = IndexInputFactory.Create(
            tenantId: tenantId,
            memoryUnitId: memoryUnitId,
            embeddingVector: vectorV1,
            embeddingDimensions: 3);

        IndexInput inputV2 = IndexInputFactory.Create(
            tenantId: tenantId,
            memoryUnitId: memoryUnitId,
            embeddingVector: vectorV2,
            embeddingDimensions: 3);

        IConnectionMultiplexer redis = _redis.Connection;
        var context = Substitute.For<Dapr.Workflow.WorkflowActivityContext>();

        IndexSemanticActivity activity = new(redis, NullLogger<IndexSemanticActivity>.Instance);
        ProvisionSemanticIndex(redis.GetDatabase(), tenantId, inputV1.EmbeddingDimensions);

        // Act — index twice with different vectors
        await activity.RunAsync(context, inputV1);
        await activity.RunAsync(context, inputV2);

        // Assert — stored vector is V2 (overwritten)
        IDatabase db = redis.GetDatabase();
        RedisValue storedVector = await db.HashGetAsync(
            $"{tenantId}:vec:{memoryUnitId}",
            "embedding");

        byte[] expectedV2 = MemoryMarshal.AsBytes(vectorV2.AsSpan()).ToArray();
        ((byte[])storedVector!).ShouldBe(expectedV2, "Re-indexing should overwrite with the latest vector");
    }

    [Fact]
    public async Task RunAsync_ChunkedInput_ShouldStoreChunkHashesAndRetrieveViaKnn_InRealRedis()
    {
        string tenantId = $"tenant-sem-chunk-{Guid.NewGuid():N}";
        const string MemoryUnitId = "mu-chunked";
        const string CaseId = "case-chunked";
        float[] firstVector = [1.0f, 0.0f, 0.0f];
        float[] secondVector = [0.0f, 1.0f, 0.0f];

        SemanticChunkIndexInput input = new()
        {
            TenantId = tenantId,
            MemoryUnitId = MemoryUnitId,
            CaseId = CaseId,
            EmbeddingProvider = "google",
            EmbeddingModel = "gemini-embedding-001",
            EmbeddingDimensions = 3,
            Metadata =
            {
                ["cloudevent.subject"] = new("claim-42", MetadataOrigin.Ai, 1.0f),
            },
            Chunks =
            [
                CreateChunk(sequence: 0, text: "first semantic chunk", startOffset: 0, endOffset: 20, vector: firstVector),
                CreateChunk(sequence: 1, text: "second semantic chunk", startOffset: 20, endOffset: 41, vector: secondVector),
            ],
        };

        IConnectionMultiplexer redis = _redis.Connection;
        IndexSemanticChunksActivity activity = new(redis, NullLogger<IndexSemanticChunksActivity>.Instance);
        var context = Substitute.For<Dapr.Workflow.WorkflowActivityContext>();
        ProvisionSemanticIndex(redis.GetDatabase(), tenantId, input.EmbeddingDimensions);

        IndexResult result = await activity.RunAsync(context, input);

        result.Backend.ShouldBe("semantic");
        result.MemoryUnitId.ShouldBe(MemoryUnitId);
        result.TenantId.ShouldBe(tenantId);

        IDatabase db = redis.GetDatabase();
        string firstKey = IndexSchemaDefinitions.BuildSemanticChunkKey(tenantId, MemoryUnitId, 0);
        string secondKey = IndexSchemaDefinitions.BuildSemanticChunkKey(tenantId, MemoryUnitId, 1);

        (await db.KeyExistsAsync(firstKey)).ShouldBeTrue();
        (await db.KeyExistsAsync(secondKey)).ShouldBeTrue();
        (await db.KeyExistsAsync(IndexSchemaDefinitions.BuildSemanticKey(tenantId, MemoryUnitId))).ShouldBeFalse();

        RedisValue storedMemoryUnitId = await db.HashGetAsync(firstKey, "memoryUnitId");
        RedisValue storedSequence = await db.HashGetAsync(secondKey, "chunkSequence");
        RedisValue storedSubject = await db.HashGetAsync(firstKey, "cloudeventSubject");
        RedisValue storedVector = await db.HashGetAsync(secondKey, "embedding");

        storedMemoryUnitId.ToString().ShouldBe(MemoryUnitId);
        ((int)storedSequence).ShouldBe(1);
        storedSubject.ToString().ShouldBe("claim-42");
        ((byte[])storedVector!).ShouldBe(MemoryMarshal.AsBytes(secondVector.AsSpan()).ToArray());

        byte[] queryBytes = MemoryMarshal.AsBytes(firstVector.AsSpan()).ToArray();
        var searchResult = db.FT().Search(
            IndexSchemaDefinitions.GetSemanticIndexName(tenantId),
            new Query("*=>[KNN 2 @embedding $vec AS score]")
                .AddParam("vec", queryBytes)
                .SetSortBy("score")
                .Limit(0, 2)
                .Dialect(2));

        searchResult.TotalResults.ShouldBeGreaterThanOrEqualTo(2);
    }

    [Fact]
    public async Task RunAsync_MissingProvisionedIndex_ShouldFailWithoutWritingVectorHash_InRealRedis()
    {
        string tenantId = $"tenant-sem-missing-{Guid.NewGuid():N}";
        IndexInput input = IndexInputFactory.Create(tenantId: tenantId);

        IConnectionMultiplexer redis = _redis.Connection;
        IndexSemanticActivity activity = new(redis, NullLogger<IndexSemanticActivity>.Instance);

        await Should.ThrowAsync<TenantIndexNotProvisionedException>(
            () => activity.RunAsync(Substitute.For<Dapr.Workflow.WorkflowActivityContext>(), input));

        IDatabase db = redis.GetDatabase();
        bool hashExists = await db.KeyExistsAsync(IndexSchemaDefinitions.BuildSemanticKey(tenantId, input.MemoryUnitId));
        hashExists.ShouldBeFalse("ingestion must not write vectors when the tenant semantic index was not provisioned");
    }

    [Fact]
    public async Task RunAsync_ChunkedInputMissingProvisionedIndex_ShouldFailWithoutWritingChunkHashes_InRealRedis()
    {
        string tenantId = $"tenant-sem-chunk-missing-{Guid.NewGuid():N}";
        const string MemoryUnitId = "mu-chunked-missing";

        SemanticChunkIndexInput input = new()
        {
            TenantId = tenantId,
            MemoryUnitId = MemoryUnitId,
            CaseId = "case-chunked",
            EmbeddingProvider = "google",
            EmbeddingModel = "gemini-embedding-001",
            EmbeddingDimensions = 3,
            Chunks =
            [
                CreateChunk(sequence: 0, text: "first semantic chunk", startOffset: 0, endOffset: 20, vector: [1.0f, 0.0f, 0.0f]),
            ],
        };

        IConnectionMultiplexer redis = _redis.Connection;
        IndexSemanticChunksActivity activity = new(redis, NullLogger<IndexSemanticChunksActivity>.Instance);

        await Should.ThrowAsync<TenantIndexNotProvisionedException>(
            () => activity.RunAsync(Substitute.For<Dapr.Workflow.WorkflowActivityContext>(), input));

        IDatabase db = redis.GetDatabase();
        bool chunkHashExists = await db.KeyExistsAsync(IndexSchemaDefinitions.BuildSemanticChunkKey(tenantId, MemoryUnitId, 0));
        chunkHashExists.ShouldBeFalse("chunked ingestion must not write vectors when the tenant semantic index was not provisioned");
    }

    private static void ProvisionSemanticIndex(IDatabase db, string tenantId, int dimensions)
        => TryCreateIndex(() => db.FT().Create(
            IndexSchemaDefinitions.GetSemanticIndexName(tenantId),
            IndexSchemaDefinitions.CreateSemanticParams(tenantId),
            IndexSchemaDefinitions.CreateSemanticSchema(dimensions)));

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
            EstimatedTokens = 8,
            Vector = vector,
        };
}
