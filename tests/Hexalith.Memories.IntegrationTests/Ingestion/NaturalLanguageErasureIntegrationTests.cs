#pragma warning disable DAPR_CONVERSATION
namespace Hexalith.Memories.IntegrationTests.Ingestion;

using Dapr.Workflow;
using Dapr.AI.Conversation;
using Dapr.Actors;
using Dapr.Actors.Client;
using Hexalith.Memories.Contracts.V1;
using Hexalith.Memories.Server.Actors;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Hexalith.Memories.IntegrationTests.Fixtures;
using Hexalith.Memories.Server.Activities.Cases;
using Hexalith.Memories.Server.Graph;
using Hexalith.Memories.Server.Infrastructure;
using Hexalith.Memories.Server.Ingestion;
using Hexalith.Memories.Server.Migration;
using Hexalith.Memories.Server.NaturalLanguage;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using Shouldly;
using StackExchange.Redis;
using NRedisStack.RedisStackCommands;

/// <summary>Physical Redis adapter controls for fenced legacy migration, retry queues and bounded exact-unit discovery.</summary>
[Collection("RedisStack")]
[Trait("Category", "Integration")]
public sealed class NaturalLanguageErasureIntegrationTests(RedisStackFixture redis, FalkorDbFixture graph) : IClassFixture<FalkorDbFixture>
{
    [Fact]
    public async Task OverflowMovesValidNoncanonicalJsonUsingItsActualObservedBytes()
    {
        var token = TestContext.Current.CancellationToken;
        string tenant = "formatted-retry-" + Guid.NewGuid().ToString("N");
        IDatabase db = redis.Connection.GetDatabase();
        var registry = new FailedNaturalLanguageEmbeddingRegistry(redis.Connection, NullLogger<FailedNaturalLanguageEmbeddingRegistry>.Instance,
            Options.Create(new NaturalLanguageDescriptionOptions { LiveRetryQueueMaxEntries = 1, DeadRetryQueueMaxEntries = 1 }));
        var victim = Record(tenant, "victim") with { QueuedAtTicks = 1 };
        var survivor = Record(tenant, "survivor") with { QueuedAtTicks = 2 };
        await registry.EnqueueAsync(victim, token);
        string raw = System.Text.Json.JsonSerializer.Serialize(victim, new System.Text.Json.JsonSerializerOptions(System.Text.Json.JsonSerializerDefaults.Web) { WriteIndented = true });
        raw.ShouldNotBe(FailedNaturalLanguageEmbeddingRegistry.SerializeRecord(victim));
        FailedNaturalLanguageEmbeddingRegistry.TryDeserialize(raw).ShouldBe(victim);
        await db.HashSetAsync(FailedNaturalLanguageEmbeddingRegistry.LivePayloadKey(tenant), "victim", raw);
        await registry.EnqueueAsync(survivor, token);
        (await db.SortedSetLengthAsync(FailedNaturalLanguageEmbeddingRegistry.LiveKey(tenant))).ShouldBe(1);
        (await db.HashGetAsync(FailedNaturalLanguageEmbeddingRegistry.LivePayloadKey(tenant), "victim")).HasValue.ShouldBeFalse();
        (await db.HashGetAsync(FailedNaturalLanguageEmbeddingRegistry.DeadPayloadKey(tenant), "victim")).ToString().ShouldBe(raw);
        (await registry.DequeueBatchAsync(tenant, 1, token)).ShouldHaveSingleItem().ShouldBe(survivor);
    }

    [Theory]
    [InlineData("enqueue")]
    [InlineData("requeue")]
    [InlineData("dead")]
    [InlineData("overflow")]
    public async Task WrongTypeDestinationCannotDeleteOrOverwriteAdmittedLivePayload(string mode)
    {
        var token = TestContext.Current.CancellationToken;
        string tenant = "queue-wrongtype-" + Guid.NewGuid().ToString("N");
        IDatabase db = redis.Connection.GetDatabase();
        var registry = new FailedNaturalLanguageEmbeddingRegistry(redis.Connection, NullLogger<FailedNaturalLanguageEmbeddingRegistry>.Instance,
            Options.Create(new NaturalLanguageDescriptionOptions { LiveRetryQueueMaxEntries = 1 }));
        var original = Record(tenant, "victim");
        await registry.EnqueueAsync(original, token);
        RedisValue bytes = await db.HashGetAsync(FailedNaturalLanguageEmbeddingRegistry.LivePayloadKey(tenant), "victim");
        await db.StringSetAsync(FailedNaturalLanguageEmbeddingRegistry.DeadPayloadKey(tenant), "malformed-destination");
        await Should.ThrowAsync<RedisServerException>(async () =>
        {
            if (mode is "enqueue" or "overflow") await registry.EnqueueAsync(Record(tenant, mode == "overflow" ? "next" : "victim"), token);
            else await registry.IncrementAttemptsAsync(original, mode == "dead" ? 1 : 10, token);
        });
        (await db.HashGetAsync(FailedNaturalLanguageEmbeddingRegistry.LivePayloadKey(tenant), "victim")).ShouldBe(bytes);
        (await db.SortedSetScoreAsync(FailedNaturalLanguageEmbeddingRegistry.LiveKey(tenant), "victim")).ShouldNotBeNull();
        (await db.StringGetAsync(FailedNaturalLanguageEmbeddingRegistry.DeadPayloadKey(tenant))).ToString().ShouldBe("malformed-destination");
    }

    [Fact]
    public async Task MixedLegacyAliasRetiresWithoutReplacingModernPayloadAndBatchSizeOneMakesProgress()
    {
        var token = TestContext.Current.CancellationToken;
        string tenant = "mixed-alias-" + Guid.NewGuid().ToString("N");
        IDatabase db = redis.Connection.GetDatabase();
        var registry = new FailedNaturalLanguageEmbeddingRegistry(redis.Connection, NullLogger<FailedNaturalLanguageEmbeddingRegistry>.Instance);
        var modern = Record(tenant, "victim") with { QueuedAtTicks = 20, TruncatedRawJsonPayload = "modern-content" };
        await registry.EnqueueAsync(modern, token);
        string alias = FailedNaturalLanguageEmbeddingRegistry.SerializeRecord(modern with { QueuedAtTicks = 1, TruncatedRawJsonPayload = "old-content" });
        await db.SortedSetAddAsync(FailedNaturalLanguageEmbeddingRegistry.LiveKey(tenant), alias, 1);
        (await registry.DequeueBatchAsync(tenant, 1, token)).ShouldBeEmpty();
        (await registry.DequeueBatchAsync(tenant, 1, token)).ShouldHaveSingleItem().ShouldBe(modern);
        (await db.HashGetAsync(FailedNaturalLanguageEmbeddingRegistry.LivePayloadKey(tenant), "victim")).ToString()
            .ShouldBe(FailedNaturalLanguageEmbeddingRegistry.SerializeRecord(modern));
        await db.SortedSetAddAsync(FailedNaturalLanguageEmbeddingRegistry.LiveKey(tenant), alias, 1);
        await Task.WhenAll(registry.DequeueBatchAsync(tenant, 1, token), FailedNaturalLanguageEmbeddingRegistry.PurgeUnitAsync(db, tenant, "victim", token));
        (await registry.DequeueBatchAsync(tenant, 1, token)).ShouldBeEmpty();
        (await db.HashGetAsync(FailedNaturalLanguageEmbeddingRegistry.LivePayloadKey(tenant), "victim")).HasValue.ShouldBeFalse();
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(true, true)]
    public async Task UnitAndCaseDeletionPreserveForeignRawSemanticHashAtOverlappingLegacyName(bool caseDeletion, bool reverse)
    {
        var token = TestContext.Current.CancellationToken;
        string tenant = "namespace-survivor-" + Guid.NewGuid().ToString("N");
        IDatabase db = redis.Connection.GetDatabase();
        string erased = reverse ? "nl:victim" : "victim";
        string survivor = reverse ? "victim" : "nl:victim";
        await db.StringSetAsync(SourcePublicationVisibility.KeyProvenance(tenant, erased), "modern");
        string key = IndexSchemaDefinitions.BuildSemanticKey(tenant, "nl:victim");
        key.ShouldBe(IndexSchemaDefinitions.BuildLegacyNaturalLanguageSemanticKey(tenant, "victim"));
        await db.HashSetAsync(key, Fields(survivor, "unrelated-private-content"));
        HashEntry[] before = await db.HashGetAllAsync(key);
        if (caseDeletion) await new DeleteCaseProjectionActivity(redis.Connection, graph.Connection, new GraphQueryBuilder(), workflowClient: Substitute.For<IDaprWorkflowClient>())
            .RunAsync(null!, new(tenant, "case", [erased]));
        else await new DeleteMemoryUnitProjectionActivity(redis.Connection, graph.Connection, new GraphQueryBuilder(), workflowClient: Substitute.For<IDaprWorkflowClient>())
            .RunAsync(null!, new(tenant, "case", erased, []));
        (await db.HashGetAllAsync(key)).OrderBy(field => field.Name.ToString()).ShouldBe(before.OrderBy(field => field.Name.ToString()));
    }

    [Fact]
    public async Task NamespaceMigrationInventoryCapacityRefusesExplicitlyAndPreservesLegacyBytes()
    {
        var token = TestContext.Current.CancellationToken;
        string tenant = "namespace-capacity-" + Guid.NewGuid().ToString("N");
        IDatabase db = redis.Connection.GetDatabase();
        string legacy = IndexSchemaDefinitions.BuildLegacyNaturalLanguageSemanticKey(tenant, "victim");
        await db.HashSetAsync(legacy, Fields("victim", "retained-content"));
        await db.SetAddAsync(SourcePublicationVisibility.Keys(tenant, "victim"), Enumerable.Range(0, 4096).Select(i => (RedisValue)("retained-" + i)).ToArray());
        var failure = await Should.ThrowAsync<InvalidOperationException>(() => RedisNaturalLanguageNamespaceMigrator.MigrateOneAsync(db, tenant, "victim", token));
        failure.Message.ShouldBe("nl-namespace-inventory-capacity-owner-reconciliation-required");
        (await db.HashGetAsync(legacy, "naturalLanguageDescription")).ToString().ShouldBe("retained-content");
        (await db.KeyExistsAsync(IndexSchemaDefinitions.BuildNaturalLanguageSemanticKey(tenant, "victim"))).ShouldBeFalse();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task MigrationRacesErasureAcrossRecreationWithoutRetainingEitherNamespace(bool caseDeletion)
    {
        string tenant = "nl-race-" + Guid.NewGuid().ToString("N");
        IDatabase db = redis.Connection.GetDatabase();
        var token = TestContext.Current.CancellationToken;
        for (int index = 0; index < 24; index++)
        {
            string unit = "unit-" + index;
            await db.HashSetAsync(IndexSchemaDefinitions.BuildLegacyNaturalLanguageSemanticKey(tenant, unit), Fields(unit, "private-content"));
            await db.StringSetAsync(SourcePublicationVisibility.KeyProvenance(tenant, unit), "modern");
            Task migration = RedisNaturalLanguageNamespaceMigrator.MigrateOneAsync(db, tenant, unit, token);
            Task<bool> deletion = caseDeletion
                ? new DeleteCaseProjectionActivity(redis.Connection, graph.Connection, new GraphQueryBuilder(), workflowClient: Substitute.For<IDaprWorkflowClient>())
                    .RunAsync(Substitute.For<WorkflowActivityContext>(), new(tenant, "case", [unit]))
                : new DeleteMemoryUnitProjectionActivity(redis.Connection, graph.Connection, new GraphQueryBuilder(), workflowClient: Substitute.For<IDaprWorkflowClient>())
                    .RunAsync(Substitute.For<WorkflowActivityContext>(), new(tenant, "case", unit, []));
            await Task.WhenAll(migration, deletion);
            using var restarted = await ConnectionMultiplexer.ConnectAsync(redis.ConnectionString);
            await RedisNaturalLanguageNamespaceMigrator.MigrateOneAsync(restarted.GetDatabase(), tenant, unit, token);
            (await db.KeyExistsAsync(IndexSchemaDefinitions.BuildLegacyNaturalLanguageSemanticKey(tenant, unit))).ShouldBeFalse();
            (await db.KeyExistsAsync(IndexSchemaDefinitions.BuildNaturalLanguageSemanticKey(tenant, unit))).ShouldBeFalse();
        }
        string survivor = IndexSchemaDefinitions.BuildLegacyNaturalLanguageSemanticKey(tenant, "survivor");
        await db.HashSetAsync(survivor, Fields("survivor", "unrelated"));
        await RedisNaturalLanguageNamespaceMigrator.MigrateOneAsync(db, tenant, "survivor", token);
        (await db.HashGetAsync(IndexSchemaDefinitions.BuildNaturalLanguageSemanticKey(tenant, "survivor"), "naturalLanguageDescription")).ToString().ShouldBe("unrelated");
    }

    [Fact]
    public async Task MigrationPreservesNewerCanonicalContentAndHonorsActualSourceFence()
    {
        var token = TestContext.Current.CancellationToken;
        string tenant = "nl-source-fence-" + Guid.NewGuid().ToString("N");
        IDatabase db = redis.Connection.GetDatabase();
        await db.HashSetAsync(IndexSchemaDefinitions.BuildLegacyNaturalLanguageSemanticKey(tenant, "newer"), Fields("newer", "older-content"));
        await db.HashSetAsync(IndexSchemaDefinitions.BuildNaturalLanguageSemanticKey(tenant, "newer"), Fields("newer", "newer-content"));
        await RedisNaturalLanguageNamespaceMigrator.MigrateOneAsync(db, tenant, "newer", token);
        (await db.HashGetAsync(IndexSchemaDefinitions.BuildNaturalLanguageSemanticKey(tenant, "newer"), "naturalLanguageDescription")).ToString().ShouldBe("newer-content");
        (await db.KeyExistsAsync(IndexSchemaDefinitions.BuildLegacyNaturalLanguageSemanticKey(tenant, "newer"))).ShouldBeFalse();
        await db.HashSetAsync(IndexSchemaDefinitions.BuildSyntacticKey(tenant, "fenced"), [new("sourceUri", "urn:synthetic:closed")]);
        await db.StringSetAsync(SourceInventoryStore.BuildKey(tenant, "urn:synthetic:closed"), """{"admissionClosed":true}""");
        await db.HashSetAsync(IndexSchemaDefinitions.BuildLegacyNaturalLanguageSemanticKey(tenant, "fenced"), Fields("fenced", "closed-content"));
        await RedisNaturalLanguageNamespaceMigrator.MigrateOneAsync(db, tenant, "fenced", token);
        (await db.KeyExistsAsync(IndexSchemaDefinitions.BuildNaturalLanguageSemanticKey(tenant, "fenced"))).ShouldBeFalse();
        (await db.KeyExistsAsync(IndexSchemaDefinitions.BuildLegacyNaturalLanguageSemanticKey(tenant, "fenced"))).ShouldBeTrue();
    }

    [Fact]
    public async Task RetryAdmissionDeadLetterTrimmingAndLegacyPayloadsRemainAbsentAfterErasureAndRestart()
    {
        var token = TestContext.Current.CancellationToken;
        string tenant = "nl-queue-" + Guid.NewGuid().ToString("N");
        IDatabase db = redis.Connection.GetDatabase();
        var registry = new FailedNaturalLanguageEmbeddingRegistry(redis.Connection, NullLogger<FailedNaturalLanguageEmbeddingRegistry>.Instance,
            Options.Create(new NaturalLanguageDescriptionOptions { LiveRetryQueueMaxEntries = 1, DeadRetryQueueMaxEntries = 20 }));
        var victim = Record(tenant, "victim");
        var survivor = Record(tenant, "survivor");
        await registry.EnqueueAsync(victim, token);
        await db.SortedSetAddAsync(FailedNaturalLanguageEmbeddingRegistry.LiveKey(tenant), FailedNaturalLanguageEmbeddingRegistry.SerializeRecord(victim with
        {
            Attempts = 9
        }), 1);
        await db.SortedSetAddAsync(FailedNaturalLanguageEmbeddingRegistry.DeadKey(tenant), FailedNaturalLanguageEmbeddingRegistry.SerializeRecord(victim with
        {
            Attempts = 10
        }), 1);
        await Task.WhenAll(registry.EnqueueAsync(survivor, token), registry.IncrementAttemptsAsync(victim, 1, token),
            FailedNaturalLanguageEmbeddingRegistry.PurgeUnitAsync(db, tenant, "victim", token));
        using var restarted = await ConnectionMultiplexer.ConnectAsync(redis.ConnectionString);
        var worker = new FailedNaturalLanguageEmbeddingRegistry(restarted, NullLogger<FailedNaturalLanguageEmbeddingRegistry>.Instance);
        await worker.EnqueueAsync(victim, token);
        await worker.IncrementAttemptsAsync(victim, 1, token);
        await worker.EnqueueAsync(survivor, token);
        foreach (string payloads in new[] { FailedNaturalLanguageEmbeddingRegistry.LivePayloadKey(tenant), FailedNaturalLanguageEmbeddingRegistry.DeadPayloadKey(tenant) })
        {
            (await db.HashGetAsync(payloads, "victim")).HasValue.ShouldBeFalse();
        }
        foreach (string queue in new[] { FailedNaturalLanguageEmbeddingRegistry.LiveKey(tenant), FailedNaturalLanguageEmbeddingRegistry.DeadKey(tenant) })
        {
            foreach (SortedSetEntry entry in await db.SortedSetRangeByRankWithScoresAsync(queue))
            {
                entry.Element.ToString().ShouldNotContain("victim");
            }
        }
        (await worker.DequeueBatchAsync(tenant, 100, token)).ShouldContain(record => record.MemoryUnitId == "survivor");
    }

    [Fact]
    public async Task ModernDiscoveryUsesExactOwnershipAndMissingLegacyIndexRequiresReconciliation()
    {
        string tenant = "nl-discovery-" + Guid.NewGuid().ToString("N");
        IDatabase db = redis.Connection.GetDatabase();
        var token = TestContext.Current.CancellationToken;
        for (int index = 0; index < 1000; index++)
        {
            await db.StringSetAsync(tenant + ":unrelated:" + index, "survivor");
        }
        await db.StringSetAsync(SourcePublicationVisibility.KeyProvenance(tenant, "modern"), "modern");
        string chunk = IndexSchemaDefinitions.BuildSemanticChunkKey(tenant, "modern", 7);
        await db.SetAddAsync(SourcePublicationVisibility.Keys(tenant, "modern"), chunk);
        (await SemanticKeyDiscovery.FindAsync(db, tenant, "modern", token)).ShouldContain((RedisKey)chunk);
        var error = await Should.ThrowAsync<InvalidOperationException>(() => SemanticKeyDiscovery.FindAsync(db, tenant, "legacy", token));
        error.Message.ShouldBe("source-publication-legacy-owner-reconciliation-required");
        await db.SetAddAsync(SourcePublicationVisibility.Keys(tenant, "modern"), IndexSchemaDefinitions.BuildSemanticKey(tenant, "survivor"));
        (await Should.ThrowAsync<InvalidOperationException>(() => SemanticKeyDiscovery.FindAsync(db, tenant, "modern", token))).Message.ShouldBe("source-publication-legacy-key-scope");
        (await db.StringGetAsync(tenant + ":unrelated:999")).ToString().ShouldBe("survivor");
        using var canceled = new CancellationTokenSource();
        canceled.Cancel();
        await Should.ThrowAsync<OperationCanceledException>(() => SemanticKeyDiscovery.FindAsync(db, tenant, "legacy", canceled.Token));
    }

    [Fact]
    public async Task LegacyNativeDiscoveryCoversExactUnitChunksAndBothNamespacesWhileOtherUnitsSurvive()
    {
        string tenant = "legacy-native-" + Guid.NewGuid().ToString("N");
        IDatabase db = redis.Connection.GetDatabase();
        var token = TestContext.Current.CancellationToken;
        db.FT().Create(IndexSchemaDefinitions.GetSemanticIndexName(tenant),
            IndexSchemaDefinitions.CreateSemanticParams(tenant), IndexSchemaDefinitions.CreateSemanticSchema(1));
        string chunk = IndexSchemaDefinitions.BuildSemanticChunkKey(tenant, "victim", 7);
        string survivor = IndexSchemaDefinitions.BuildSemanticChunkKey(tenant, "unrelated", 7);
        foreach (string key in new[] { IndexSchemaDefinitions.BuildSemanticKey(tenant, "victim"), chunk,
            IndexSchemaDefinitions.BuildLegacyNaturalLanguageSemanticKey(tenant, "victim") })
        {
            await db.HashSetAsync(key, Fields("victim", "private-content"));
        }
        await db.HashSetAsync(survivor, Fields("unrelated", "survivor"));
        (await Should.ThrowAsync<InvalidOperationException>(() => SemanticKeyDiscovery.FindAsync(db, tenant, "victim", token)))
            .Message.ShouldBe("source-publication-legacy-owner-reconciliation-required");
        (await db.KeyExistsAsync(SourcePublicationVisibility.KeyProvenance(tenant, "victim"))).ShouldBeFalse();
        // The fixture owner knows every historical key it created and explicitly reconciles that inventory.
        await db.SetAddAsync(SourcePublicationVisibility.Keys(tenant, "victim"), [(RedisValue)chunk,
            (RedisValue)IndexSchemaDefinitions.BuildSemanticKey(tenant, "victim"),
            (RedisValue)IndexSchemaDefinitions.BuildLegacyNaturalLanguageSemanticKey(tenant, "victim")]);
        await db.StringSetAsync(SourcePublicationVisibility.KeyProvenance(tenant, "victim"), "legacy-owner-reconciled-v1");
        var discovered = await SemanticKeyDiscovery.FindAsync(db, tenant, "victim", token);
        discovered.ShouldContain((RedisKey)chunk);
        discovered.ShouldNotContain((RedisKey)survivor);
        var activity = new DeleteMemoryUnitProjectionActivity(redis.Connection, graph.Connection, new GraphQueryBuilder(), workflowClient: NSubstitute.Substitute.For<Dapr.Workflow.IDaprWorkflowClient>());
        await activity.RunAsync(Substitute.For<WorkflowActivityContext>(), new MemoryUnitDeletionProjectionInput(tenant, "case", "victim", []));
        foreach (RedisKey key in discovered)
        {
            (await db.KeyExistsAsync(key)).ShouldBeFalse();
        }
        (await db.HashGetAsync(survivor, "naturalLanguageDescription")).ToString().ShouldBe("survivor");
    }

    [Fact]
    public async Task LegacyDiscoveryRefusesPartialTimeoutPolicyWithoutSealingCompleteness()
    {
        string tenant = "legacy-partial-" + Guid.NewGuid().ToString("N");
        IDatabase db = redis.Connection.GetDatabase();
        var token = TestContext.Current.CancellationToken;
        db.FT().Create(IndexSchemaDefinitions.GetSemanticIndexName(tenant),
            IndexSchemaDefinitions.CreateSemanticParams(tenant), IndexSchemaDefinitions.CreateSemanticSchema(1));
        try
        {
            await db.ExecuteAsync("FT.CONFIG", "SET", "ON_TIMEOUT", "RETURN");
            (await Should.ThrowAsync<InvalidOperationException>(() => SemanticKeyDiscovery.FindAsync(db, tenant, "victim", token)))
                .Message.ShouldBe("source-publication-legacy-owner-reconciliation-required");
            (await db.KeyExistsAsync(SourcePublicationVisibility.KeyProvenance(tenant, "victim"))).ShouldBeFalse();
        }
        finally { await db.ExecuteAsync("FT.CONFIG", "SET", "ON_TIMEOUT", "FAIL"); }
    }

    [Fact]
    public async Task MissingOptionalLegacyTagRemainsUnqualifiedAndPreservesTheObligation()
    {
        string tenant = "legacy-missing-tag-" + Guid.NewGuid().ToString("N");
        var token = TestContext.Current.CancellationToken;
        IDatabase db = redis.Connection.GetDatabase();
        db.FT().Create(IndexSchemaDefinitions.GetSemanticIndexName(tenant),
            IndexSchemaDefinitions.CreateSemanticParams(tenant), IndexSchemaDefinitions.CreateSemanticSchema(1));
        string key = IndexSchemaDefinitions.BuildSemanticChunkKey(tenant, "victim", 9);
        await db.HashSetAsync(key, Fields("victim", "legacy-raw").Where(field => field.Name != "memoryUnitId").ToArray());
        (await Should.ThrowAsync<InvalidOperationException>(() => SemanticKeyDiscovery.FindAsync(db, tenant, "victim", token)))
            .Message.ShouldBe("source-publication-legacy-owner-reconciliation-required");
        (await db.KeyExistsAsync(SourcePublicationVisibility.KeyProvenance(tenant, "victim"))).ShouldBeFalse();
        (await db.HashGetAsync(key, "naturalLanguageDescription")).ToString().ShouldBe("legacy-raw");
    }

    [Fact]
    public async Task IncompleteCanonicalTargetBlocksMigrationAndRetainsValidLegacyContent()
    {
        string tenant = "nl-incomplete-" + Guid.NewGuid().ToString("N");
        IDatabase db = redis.Connection.GetDatabase();
        await db.HashSetAsync(IndexSchemaDefinitions.BuildLegacyNaturalLanguageSemanticKey(tenant, "victim"), Fields("victim", "valid-legacy"));
        await db.HashSetAsync(IndexSchemaDefinitions.BuildNaturalLanguageSemanticKey(tenant, "victim"), "memoryUnitId", "victim");
        (await Should.ThrowAsync<InvalidOperationException>(() => RedisNaturalLanguageNamespaceMigrator.MigrateOneAsync(db, tenant, "victim", TestContext.Current.CancellationToken)))
            .Message.ShouldBe("nl-namespace-canonical-owner-reconciliation-required");
        (await db.HashGetAsync(IndexSchemaDefinitions.BuildLegacyNaturalLanguageSemanticKey(tenant, "victim"), "naturalLanguageDescription"))
            .ToString().ShouldBe("valid-legacy");
    }

    [Fact]
    public async Task ValidOnlyOverflowEnforcesTheDeadPayloadCap()
    {
        string tenant = "nl-dead-cap-" + Guid.NewGuid().ToString("N");
        IDatabase db = redis.Connection.GetDatabase();
        var registry = new FailedNaturalLanguageEmbeddingRegistry(redis.Connection, NullLogger<FailedNaturalLanguageEmbeddingRegistry>.Instance,
            Options.Create(new NaturalLanguageDescriptionOptions { LiveRetryQueueMaxEntries = 1, DeadRetryQueueMaxEntries = 2 }));
        for (int index = 0; index < 10; index++)
            await registry.EnqueueAsync(Record(tenant, "unit-" + index) with { QueuedAtTicks = index + 1 }, TestContext.Current.CancellationToken);
        (await db.SortedSetLengthAsync(FailedNaturalLanguageEmbeddingRegistry.DeadKey(tenant))).ShouldBe(2);
        (await db.HashLengthAsync(FailedNaturalLanguageEmbeddingRegistry.DeadPayloadKey(tenant))).ShouldBe(2);
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    public async Task AdmittedModernAndJsonOnlyLegacyRetriesPersistActualNaturalLanguageOutput(bool legacy, bool removeDuringDescription)
    {
        string tenant = "nl-admitted-" + Guid.NewGuid().ToString("N");
        var token = TestContext.Current.CancellationToken;
        IDatabase db = redis.Connection.GetDatabase();
        const string unit = "victim";
        string syntactic = IndexSchemaDefinitions.BuildSyntacticKey(tenant, unit);
        await db.HashSetAsync(syntactic, [new("memoryUnitId", unit), new("caseId", "case"), new("content", "canonical")]);
        string unrelated = IndexSchemaDefinitions.BuildNaturalLanguageSemanticKey(tenant, "survivor");
        await db.HashSetAsync(unrelated, Fields("survivor", "unrelated-content"));
        var config = EmbeddingProviderDefaults.Google();
        db.FT().Create(IndexSchemaDefinitions.GetNaturalLanguageSemanticIndexName(tenant),
            IndexSchemaDefinitions.CreateNaturalLanguageSemanticParams(tenant), IndexSchemaDefinitions.CreateNaturalLanguageSemanticSchema(config.Dimensions));
        var registry = new FailedNaturalLanguageEmbeddingRegistry(redis.Connection, NullLogger<FailedNaturalLanguageEmbeddingRegistry>.Instance);
        var record = Record(tenant, unit) with { EmbeddingDimensions = config.Dimensions, EmbeddingModel = config.Model };
        if (legacy) await db.SortedSetAddAsync(FailedNaturalLanguageEmbeddingRegistry.LiveKey(tenant), FailedNaturalLanguageEmbeddingRegistry.SerializeRecord(record), 1);
        else await registry.EnqueueAsync(record, token);
        (await registry.DequeueBatchAsync(tenant, 10, token)).ShouldHaveSingleItem().ShouldBe(record);
        (await db.HashGetAsync(FailedNaturalLanguageEmbeddingRegistry.LivePayloadKey(tenant), unit)).HasValue.ShouldBeTrue();
        var conversation = Substitute.For<DaprConversationClient>(null!, new HttpClient(), null!);
        var response = new ConversationResponse([new ConversationResponseResult([new ConversationResultChoice(null, 0, new ResultMessage("A retained business action occurred."))])], string.Empty);
        conversation.ConverseAsync(Arg.Any<IReadOnlyList<ConversationInput>>(), Arg.Any<ConversationOptions>(), Arg.Any<CancellationToken>()).Returns(async _ =>
            { if (removeDuringDescription) await db.KeyDeleteAsync(syntactic); return response; });
        var environment = Substitute.For<IHostEnvironment>();
        environment.EnvironmentName.Returns("Development");
        var embedding = Substitute.For<EmbeddingClient>(Substitute.For<IHttpClientFactory>(), Substitute.For<Dapr.Client.DaprClient>(), new ConfigurationBuilder().Build(), environment);
        embedding.PrimeApiKeyAsync(Arg.Any<string>(), Arg.Any<TenantEmbeddingConfig>(), Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);
        float[] vector = new float[config.Dimensions]; vector[0] = 1;
        embedding.GenerateAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<TenantEmbeddingConfig>(), Arg.Any<CancellationToken>()).Returns(vector);
        var actors = Substitute.For<IActorProxyFactory>();
        var limiter = Substitute.For<IEmbeddingRateLimiterActor>();
        limiter.TryConsumeWithCeilingAsync(Arg.Any<int>()).Returns(true);
        actors.CreateActorProxy<IEmbeddingRateLimiterActor>(Arg.Any<ActorId>(), Arg.Any<string>()).Returns(limiter);
        var provider = Substitute.For<ITenantEmbeddingConfigProvider>();
        provider.GetAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(config);
        using var services = new ServiceCollection().AddLogging().AddKeyedSingleton<IConnectionMultiplexer>("redis", redis.Connection)
            .AddSingleton(conversation).AddSingleton(Options.Create(new NaturalLanguageDescriptionOptions()))
            .AddSingleton(embedding).AddSingleton(actors).AddSingleton(provider).AddSingleton(Substitute.For<IJitterSource>()).BuildServiceProvider();
        var activity = new RetryNaturalLanguageEmbeddingActivity(redis.Connection, services);
        var input = new NaturalLanguageEmbeddingRetryInput(tenant, unit, string.Empty, record.EventType, null, "case", config.Provider, config.Model, config.Dimensions);
        (await activity.RunAsync(Substitute.For<WorkflowActivityContext>(), input)).Indexed.ShouldBe(!removeDuringDescription);
        if (removeDuringDescription)
        {
            _ = embedding.DidNotReceiveWithAnyArgs().GenerateAsync(default!, default!, default!, default);
            (await db.KeyExistsAsync(IndexSchemaDefinitions.BuildNaturalLanguageSemanticKey(tenant, unit))).ShouldBeFalse();
            (await db.HashGetAsync(unrelated, "naturalLanguageDescription")).ToString().ShouldBe("unrelated-content");
            return;
        }
        (await db.HashGetAsync(IndexSchemaDefinitions.BuildNaturalLanguageSemanticKey(tenant, unit), "naturalLanguageDescription"))
            .ToString().ShouldBe("A retained business action occurred.");
        ((byte[]?)(await db.HashGetAsync(IndexSchemaDefinitions.BuildNaturalLanguageSemanticKey(tenant, unit), "embedding")))!.Length.ShouldBe(config.Dimensions * sizeof(float));
        (await db.HashGetAsync(unrelated, "naturalLanguageDescription")).ToString().ShouldBe("unrelated-content");
        await db.KeyDeleteAsync(syntactic);
        (await activity.RunAsync(Substitute.For<WorkflowActivityContext>(), input)).Indexed.ShouldBeFalse();
    }

    private static HashEntry[] Fields(string unit, string content) => [new("memoryUnitId", unit), new("caseId", "case"), new("embedding", new byte[] { 1, 0, 0, 0 }), new("naturalLanguageDescription", content)];
    private static FailedNaturalLanguageEmbeddingRecord Record(string tenant, string unit)
        => new(tenant, unit, "private-raw-" + unit, "TestEvent", null, "case", "test", "test", 1, 1, 0);
}
