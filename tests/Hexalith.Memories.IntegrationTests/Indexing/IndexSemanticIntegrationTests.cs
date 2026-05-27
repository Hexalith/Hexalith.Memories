namespace Hexalith.Memories.IntegrationTests.Indexing;

using System.Runtime.InteropServices;

using Hexalith.Memories.Contracts.V1;
using Hexalith.Memories.IntegrationTests.Fixtures;
using Hexalith.Memories.Server.Activities.Indexing;
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
    public async Task RunAsync_ShouldCreateSearchableVectorIndex_InRealRedis()
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

        // Act
        await activityA.RunAsync(context, inputA);
        await activityB.RunAsync(context, inputB);

        // Assert — tenant A's hash key is not visible under tenant B's prefix
        IDatabase db = redis.GetDatabase();
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
}
