namespace Hexalith.Memories.IntegrationTests.Indexing;

using Hexalith.Memories.Contracts.V1;
using Hexalith.Memories.IntegrationTests.Fixtures;
using Hexalith.Memories.Server.Activities.Indexing;
using Hexalith.Memories.Server.Infrastructure;
using Hexalith.Memories.TestHelpers.Factories;

using Microsoft.Extensions.Logging.Abstractions;

using NRedisStack.RedisStackCommands;

using NSubstitute;

using Shouldly;

using StackExchange.Redis;

/// <summary>
/// Integration tests verifying IndexSyntacticActivity against a real Redis Stack instance.
/// Validates provisioned RediSearch readiness, HASH storage, and tenant isolation.
/// </summary>
[Collection("RedisStack")]
[Trait("Category", "Integration")]
public class IndexSyntacticIntegrationTests
{
    private readonly RedisStackFixture _redis;

    public IndexSyntacticIntegrationTests(RedisStackFixture redis) => _redis = redis;

    [Fact]
    public async Task RunAsync_WithProvisionedIndex_ShouldStoreHash_InRealRedis()
    {
        // Arrange
        string tenantId = $"tenant-{Guid.NewGuid():N}";
        IndexInput input = IndexInputFactory.Create(tenantId: tenantId);

        // Use the real connection but wrap in a keyed-services-compatible way
        IConnectionMultiplexer redis = _redis.Connection;
        IndexSyntacticActivity activity = new(redis, NullLogger<IndexSyntacticActivity>.Instance);

        var context = Substitute.For<Dapr.Workflow.WorkflowActivityContext>();
        ProvisionSyntacticIndex(redis.GetDatabase(), tenantId);

        // Act
        IndexResult result = await activity.RunAsync(context, input);

        // Assert — result
        result.Backend.ShouldBe("syntactic");
        result.MemoryUnitId.ShouldBe(input.MemoryUnitId);

        // Assert — HASH exists in Redis with correct fields
        IDatabase db = redis.GetDatabase();
        RedisValue content = await db.HashGetAsync($"{tenantId}:mu:{input.MemoryUnitId}", "content");
        content.ToString().ShouldBe(input.Content);

        RedisValue caseId = await db.HashGetAsync($"{tenantId}:mu:{input.MemoryUnitId}", "caseId");
        caseId.ToString().ShouldBe(input.CaseId);
    }

    [Fact]
    public async Task RunAsync_WithProvisionedIndex_ShouldBeSearchable_InRealRedis()
    {
        // Arrange
        string tenantId = $"tenant-{Guid.NewGuid():N}";
        IndexInput input = IndexInputFactory.Create(tenantId: tenantId, content: "unique searchable content xyz123");

        IConnectionMultiplexer redis = _redis.Connection;
        IndexSyntacticActivity activity = new(redis, NullLogger<IndexSyntacticActivity>.Instance);

        var context = Substitute.For<Dapr.Workflow.WorkflowActivityContext>();
        ProvisionSyntacticIndex(redis.GetDatabase(), tenantId);

        // Act
        await activity.RunAsync(context, input);

        // Assert — FT.SEARCH finds the document
        IDatabase db = redis.GetDatabase();
        var ft = db.FT();
        var searchResult = ft.Search($"{tenantId}:memories:idx", new NRedisStack.Search.Query("xyz123"));

        searchResult.TotalResults.ShouldBeGreaterThan(0);
    }

    [Fact]
    public async Task RunAsync_TenantIsolation_ShouldNotLeakAcrossTenants()
    {
        // Arrange — two different tenants
        string tenantA = $"tenant-a-{Guid.NewGuid():N}";
        string tenantB = $"tenant-b-{Guid.NewGuid():N}";

        IndexInput inputA = IndexInputFactory.Create(tenantId: tenantA, content: "tenant A secret data");
        IndexInput inputB = IndexInputFactory.Create(tenantId: tenantB, content: "tenant B public data");

        IConnectionMultiplexer redis = _redis.Connection;
        var context = Substitute.For<Dapr.Workflow.WorkflowActivityContext>();

        IndexSyntacticActivity activityA = new(redis, NullLogger<IndexSyntacticActivity>.Instance);
        IndexSyntacticActivity activityB = new(redis, NullLogger<IndexSyntacticActivity>.Instance);
        IDatabase db = redis.GetDatabase();
        ProvisionSyntacticIndex(db, tenantA);
        ProvisionSyntacticIndex(db, tenantB);

        // Act
        await activityA.RunAsync(context, inputA);
        await activityB.RunAsync(context, inputB);

        // Assert — tenant A's index does NOT contain tenant B's data
        var ft = db.FT();
        var searchResultA = ft.Search($"{tenantA}:memories:idx", new NRedisStack.Search.Query("*"));
        var searchResultB = ft.Search($"{tenantB}:memories:idx", new NRedisStack.Search.Query("*"));

        searchResultA.TotalResults.ShouldBe(1);
        searchResultB.TotalResults.ShouldBe(1);
    }

    [Fact]
    public async Task RunAsync_ShouldIndexMetadataAndSourceFields_ForFullTextSearch()
    {
        // Arrange
        string tenantId = $"tenant-{Guid.NewGuid():N}";
        IndexInput input = IndexInputFactory.Create(
            tenantId: tenantId,
            content: "base content only",
            sourceUri: "https://docs.example.com/blorbo/reference-note",
            sourceType: SourceType.Url) with
        {
            Metadata = new Dictionary<string, MetadataField>
            {
                ["priority"] = new("metakeyword42", MetadataOrigin.Human, 1.0f),
            },
        };

        IConnectionMultiplexer redis = _redis.Connection;
        IndexSyntacticActivity activity = new(redis, NullLogger<IndexSyntacticActivity>.Instance);
        var context = Substitute.For<Dapr.Workflow.WorkflowActivityContext>();
        ProvisionSyntacticIndex(redis.GetDatabase(), tenantId);

        // Act
        await activity.RunAsync(context, input);

        // Assert
        IDatabase db = redis.GetDatabase();
        var ft = db.FT();

        ft.Search($"{tenantId}:memories:idx", new NRedisStack.Search.Query("blorbo")).TotalResults.ShouldBe(1);
        ft.Search($"{tenantId}:memories:idx", new NRedisStack.Search.Query("metakeyword42")).TotalResults.ShouldBe(1);
        ft.Search($"{tenantId}:memories:idx", new NRedisStack.Search.Query("url")).TotalResults.ShouldBe(1);
    }

    [Fact]
    public async Task RunAsync_MissingProvisionedIndex_ShouldFailWithoutWritingHash_InRealRedis()
    {
        string tenantId = $"tenant-missing-{Guid.NewGuid():N}";
        IndexInput input = IndexInputFactory.Create(tenantId: tenantId);

        IConnectionMultiplexer redis = _redis.Connection;
        IndexSyntacticActivity activity = new(redis, NullLogger<IndexSyntacticActivity>.Instance);

        await Should.ThrowAsync<TenantIndexNotProvisionedException>(
            () => activity.RunAsync(Substitute.For<Dapr.Workflow.WorkflowActivityContext>(), input));

        IDatabase db = redis.GetDatabase();
        bool hashExists = await db.KeyExistsAsync(IndexSchemaDefinitions.BuildSyntacticKey(tenantId, input.MemoryUnitId));
        hashExists.ShouldBeFalse("ingestion must not write hashes when the tenant index was not provisioned");
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
