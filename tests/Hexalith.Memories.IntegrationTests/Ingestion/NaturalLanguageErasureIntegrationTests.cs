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
    public async Task LargeAccumulatedDeadQueueTrimUsesBoundedLuaExpansionAndPreservesNewestPayloads()
    {
        var token = TestContext.Current.CancellationToken;
        string tenant = "large-trim-" + Guid.NewGuid().ToString("N");
        IDatabase db = redis.Connection.GetDatabase();
        const int count = 20_000;
        await db.SortedSetAddAsync(FailedNaturalLanguageEmbeddingRegistry.DeadKey(tenant),
            Enumerable.Range(0, count).Select(i => new SortedSetEntry("old-" + i, i)).ToArray());
        await db.HashSetAsync(FailedNaturalLanguageEmbeddingRegistry.DeadPayloadKey(tenant),
            Enumerable.Range(0, count).Select(i => new HashEntry("old-" + i, "old-payload-" + i)).ToArray());
        var registry = new FailedNaturalLanguageEmbeddingRegistry(redis.Connection, NullLogger<FailedNaturalLanguageEmbeddingRegistry>.Instance,
            Options.Create(new NaturalLanguageDescriptionOptions { DeadRetryQueueMaxEntries = 2 }));
        var record = Record(tenant, "newest") with { QueuedAtTicks = count + 1 };
        await registry.EnqueueAsync(record, token);
        (await registry.IncrementAttemptsAsync(record, 1, token)).ShouldBeTrue();
        (await db.SortedSetRangeByRankAsync(FailedNaturalLanguageEmbeddingRegistry.DeadKey(tenant))).Select(value => value.ToString())
            .ShouldBe(new[] { "old-19999", "newest" });
        (await db.HashLengthAsync(FailedNaturalLanguageEmbeddingRegistry.DeadPayloadKey(tenant))).ShouldBe(2);
        (await db.HashGetAsync(FailedNaturalLanguageEmbeddingRegistry.DeadPayloadKey(tenant), "old-19999")).ToString().ShouldBe("old-payload-19999");
        FailedNaturalLanguageEmbeddingRegistry.TryDeserialize((await db.HashGetAsync(FailedNaturalLanguageEmbeddingRegistry.DeadPayloadKey(tenant), "newest")).ToString())
            .ShouldBe(record with { Attempts = 1 });
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ExactRetryAdmissionRejectsRecordReplacementDuringDescriptionOrEmbedding(bool duringEmbedding)
    {
        string tenant = "retry-replaced-" + Guid.NewGuid().ToString("N");
        var token = TestContext.Current.CancellationToken;
        IDatabase db = redis.Connection.GetDatabase();
        const string unit = "victim";
        await db.HashSetAsync(IndexSchemaDefinitions.BuildSyntacticKey(tenant, unit), [new("memoryUnitId", unit), new("content", "canonical")]);
        string output = IndexSchemaDefinitions.BuildNaturalLanguageSemanticKey(tenant, unit);
        await db.HashSetAsync(output, Fields(unit, "prior-canonical-description"));
        byte[] before = (byte[])(await db.ExecuteAsync("DUMP", output))!;
        var config = EmbeddingProviderDefaults.Google();
        db.FT().Create(IndexSchemaDefinitions.GetNaturalLanguageSemanticIndexName(tenant),
            IndexSchemaDefinitions.CreateNaturalLanguageSemanticParams(tenant), IndexSchemaDefinitions.CreateNaturalLanguageSemanticSchema(config.Dimensions));
        var registry = new FailedNaturalLanguageEmbeddingRegistry(redis.Connection, NullLogger<FailedNaturalLanguageEmbeddingRegistry>.Instance);
        var record = Record(tenant, unit) with { EmbeddingDimensions = config.Dimensions, EmbeddingModel = config.Model };
        var replacement = record with { TruncatedRawJsonPayload = "newer-raw-payload", QueuedAtTicks = 2 };
        await registry.EnqueueAsync(record, token);
        var conversation = Substitute.For<DaprConversationClient>(null!, new HttpClient(), null!);
        conversation.ConverseAsync(Arg.Any<IReadOnlyList<ConversationInput>>(), Arg.Any<ConversationOptions>(), Arg.Any<CancellationToken>()).Returns(async _ =>
        {
            if (!duringEmbedding) await registry.EnqueueAsync(replacement, token);
            return new ConversationResponse([new ConversationResponseResult([new ConversationResultChoice(null, 0, new ResultMessage("A stale action occurred."))])], string.Empty);
        });
        var environment = Substitute.For<IHostEnvironment>(); environment.EnvironmentName.Returns("Development");
        var embedding = Substitute.For<EmbeddingClient>(Substitute.For<IHttpClientFactory>(), Substitute.For<Dapr.Client.DaprClient>(), new ConfigurationBuilder().Build(), environment);
        embedding.PrimeApiKeyAsync(Arg.Any<string>(), Arg.Any<TenantEmbeddingConfig>(), Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);
        embedding.GenerateAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<TenantEmbeddingConfig>(), Arg.Any<CancellationToken>()).Returns(async _ =>
        {
            if (duringEmbedding) await registry.EnqueueAsync(replacement, token);
            return new float[config.Dimensions];
        });
        var actors = Substitute.For<IActorProxyFactory>(); var limiter = Substitute.For<IEmbeddingRateLimiterActor>();
        limiter.TryConsumeWithCeilingAsync(Arg.Any<int>()).Returns(true);
        actors.CreateActorProxy<IEmbeddingRateLimiterActor>(Arg.Any<ActorId>(), Arg.Any<string>()).Returns(limiter);
        var provider = Substitute.For<ITenantEmbeddingConfigProvider>(); provider.GetAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(config);
        using var services = new ServiceCollection().AddLogging().AddKeyedSingleton<IConnectionMultiplexer>("redis", redis.Connection)
            .AddSingleton(conversation).AddSingleton(Options.Create(new NaturalLanguageDescriptionOptions())).AddSingleton(embedding)
            .AddSingleton(actors).AddSingleton(provider).AddSingleton(Substitute.For<IJitterSource>()).BuildServiceProvider();
        var result = await new RetryNaturalLanguageEmbeddingActivity(redis.Connection, services).RunAsync(Substitute.For<WorkflowActivityContext>(),
            new(tenant, unit, string.Empty, record.EventType, null, "case", config.Provider, config.Model, config.Dimensions));
        result.Indexed.ShouldBeFalse(); result.Reason.ShouldBe("retry-payload-retired");
        ((byte[])(await db.ExecuteAsync("DUMP", output))!).ShouldBe(before);
        (await FailedNaturalLanguageEmbeddingRegistry.ReadCurrentAsync(db, tenant, unit)).ShouldBe(replacement);
        if (!duringEmbedding) _ = embedding.DidNotReceiveWithAnyArgs().GenerateAsync(default!, default!, default!, default);
    }

    [Fact]
    public void OwnedContainerConnectionStringUsesExplicitLoopbackIpAcceptedByWorkflowFixture()
    {
        string connection = RedisStackFixture.OwnedLoopbackConnectionString(16388);
        var endpoint = ConfigurationOptions.Parse(connection).EndPoints.Single().ShouldBeOfType<System.Net.IPEndPoint>();
        endpoint.Address.ShouldBe(System.Net.IPAddress.Loopback);
        endpoint.Port.ShouldBe(16388);
    }

    [Fact]
    public async Task RefusedUnknownLegacyDiscoveryNeverMutatesForeignRawHashOrInventory()
    {
        string tenant = "foreign-discovery-" + Guid.NewGuid().ToString("N");
        IDatabase db = redis.Connection.GetDatabase();
        db.FT().Create(IndexSchemaDefinitions.GetSemanticIndexName(tenant),
            IndexSchemaDefinitions.CreateSemanticParams(tenant), IndexSchemaDefinitions.CreateSemanticSchema(1));
        string key = IndexSchemaDefinitions.BuildLegacyNaturalLanguageSemanticKey(tenant, "victim");
        key.ShouldBe(IndexSchemaDefinitions.BuildSemanticKey(tenant, "nl:victim"));
        await db.HashSetAsync(key, [.. Fields("nl:victim", "foreign-private-content"), new("tenantId", tenant)]);
        byte[] before = (byte[])(await db.ExecuteAsync("DUMP", key))!;
        var error = await Should.ThrowAsync<InvalidOperationException>(() => SemanticKeyDiscovery.RegisterLegacyKeysAsync(db, tenant, "victim", TestContext.Current.CancellationToken));
        error.Message.ShouldBe("source-publication-legacy-owner-reconciliation-required");
        ((byte[])(await db.ExecuteAsync("DUMP", key))!).ShouldBe(before);
        (await db.HashGetAsync(key, "publicationVisible")).HasValue.ShouldBeFalse();
        (await db.SetContainsAsync(SourcePublicationVisibility.Keys(tenant, "victim"), key)).ShouldBeFalse();
        (await db.StringGetAsync(SourcePublicationVisibility.KeyProvenance(tenant, "victim"))).HasValue.ShouldBeFalse();
    }

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

    /// <summary>
    /// Verifies mixed Distinct Legacy Alias Preserves Modern Progress And Remains For Reconciliation.
    /// </summary>
    [Fact]
    public async Task MixedDistinctLegacyAliasPreservesModernProgressAndRemainsForReconciliation()
    {
        var token = TestContext.Current.CancellationToken;
        string tenant = "mixed-alias-" + Guid.NewGuid().ToString("N");
        IDatabase db = redis.Connection.GetDatabase();
        var registry = new FailedNaturalLanguageEmbeddingRegistry(redis.Connection, NullLogger<FailedNaturalLanguageEmbeddingRegistry>.Instance);
        var modern = Record(tenant, "victim") with { QueuedAtTicks = 20, TruncatedRawJsonPayload = "modern-content" };
        await registry.EnqueueAsync(modern, token);
        string alias = FailedNaturalLanguageEmbeddingRegistry.SerializeRecord(modern with { QueuedAtTicks = 1, TruncatedRawJsonPayload = "old-content" });
        await db.SortedSetAddAsync(FailedNaturalLanguageEmbeddingRegistry.LiveKey(tenant), alias, 1);
        (await registry.DequeueBatchAsync(tenant, 1, token)).ShouldHaveSingleItem().ShouldBe(modern);
        (await db.SortedSetScoreAsync(FailedNaturalLanguageEmbeddingRegistry.LiveKey(tenant), alias)).ShouldBe(1);
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

    /// <summary>
    /// Verifies admitted Modern And Json Only Legacy Retries Persist Actual Natural Language Output.
    /// </summary>
    [Theory]
    [InlineData(false, false, false, false, false)]
    [InlineData(true, false, false, false, false)]
    [InlineData(false, true, false, false, false)]
    [InlineData(false, false, true, false, false)]
    [InlineData(false, false, false, true, false)]
    [InlineData(false, false, false, false, true)]
    public async Task AdmittedModernAndJsonOnlyLegacyRetriesPersistActualNaturalLanguageOutput(bool legacy, bool removeDuringDescription, bool replaceAtReadiness, bool eraseAtReadiness, bool noncanonical)
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
        if (noncanonical)
            await db.HashSetAsync(FailedNaturalLanguageEmbeddingRegistry.LivePayloadKey(tenant), unit,
                FailedNaturalLanguageEmbeddingRegistry.SerializeRecord(record).Replace(",", ",\n  ", StringComparison.Ordinal));
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
        var readiness = Substitute.For<ITenantIndexReadinessVerifier>();
        readiness.EnsureReadyAsync(Arg.Any<IDatabase>(), tenant, TenantIndexFamily.NaturalLanguageSemantic, config.Dimensions, Arg.Any<CancellationToken>())
            .Returns(async _ =>
            {
                await new TenantIndexReadinessVerifier(NullLogger<TenantIndexReadinessVerifier>.Instance).EnsureReadyAsync(db, tenant, TenantIndexFamily.NaturalLanguageSemantic, config.Dimensions, token);
                if (replaceAtReadiness)
                {
                    await registry.EnqueueAsync(record with { TruncatedRawJsonPayload = "newer-payload", QueuedAtTicks = 2 }, token);
                    await db.HashSetAsync(IndexSchemaDefinitions.BuildNaturalLanguageSemanticKey(tenant, unit), "naturalLanguageDescription", "newer-output");
                }
                if (eraseAtReadiness) await FailedNaturalLanguageEmbeddingRegistry.PurgeUnitAsync(db, tenant, unit, token);
            });
        using var services = new ServiceCollection().AddLogging().AddKeyedSingleton<IConnectionMultiplexer>("redis", redis.Connection)
            .AddSingleton(readiness)
            .AddSingleton(conversation).AddSingleton(Options.Create(new NaturalLanguageDescriptionOptions()))
            .AddSingleton(embedding).AddSingleton(actors).AddSingleton(provider).AddSingleton(Substitute.For<IJitterSource>()).BuildServiceProvider();
        var activity = new RetryNaturalLanguageEmbeddingActivity(redis.Connection, services);
        var input = new NaturalLanguageEmbeddingRetryInput(tenant, unit, string.Empty, record.EventType, null, "case", config.Provider, config.Model, config.Dimensions);
        (await activity.RunAsync(Substitute.For<WorkflowActivityContext>(), input)).Indexed.ShouldBe(!removeDuringDescription && !replaceAtReadiness && !eraseAtReadiness);
        if (replaceAtReadiness || eraseAtReadiness)
        {
            if (replaceAtReadiness)
            {
                (await db.HashGetAsync(IndexSchemaDefinitions.BuildNaturalLanguageSemanticKey(tenant, unit), "naturalLanguageDescription")).ToString().ShouldBe("newer-output");
                (await db.HashGetAsync(FailedNaturalLanguageEmbeddingRegistry.LivePayloadKey(tenant), unit)).ToString().ShouldContain("newer-payload");
            }
            else (await db.KeyExistsAsync(IndexSchemaDefinitions.BuildNaturalLanguageSemanticKey(tenant, unit))).ShouldBeFalse();
            (await db.HashGetAsync(unrelated, "naturalLanguageDescription")).ToString().ShouldBe("unrelated-content");
            return;
        }
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
        if (noncanonical)
        {
            await registry.CompleteAsync(record, token);
            (await db.HashGetAsync(FailedNaturalLanguageEmbeddingRegistry.LivePayloadKey(tenant), unit)).HasValue.ShouldBeFalse();
        }
        await db.KeyDeleteAsync(syntactic);
        (await activity.RunAsync(Substitute.For<WorkflowActivityContext>(), input)).Indexed.ShouldBeFalse();
    }

    /// <summary>
    /// Verifies observed Pruning Preserves Concurrent Live And Dead Replacement.
    /// </summary>
    [Theory]
    [InlineData(false, "payload")]
    [InlineData(false, "score")]
    [InlineData(false, "membership")]
    [InlineData(true, "payload")]
    [InlineData(true, "score")]
    [InlineData(true, "membership")]
    public async Task ObservedPruningPreservesConcurrentLiveAndDeadReplacement(bool dead, string changed)
    {
        var token = TestContext.Current.CancellationToken;
        string tenant = "observed-prune-" + Guid.NewGuid().ToString("N");
        IDatabase db = redis.Connection.GetDatabase();
        string queue = dead ? FailedNaturalLanguageEmbeddingRegistry.DeadKey(tenant) : FailedNaturalLanguageEmbeddingRegistry.LiveKey(tenant);
        string payloads = dead ? FailedNaturalLanguageEmbeddingRegistry.DeadPayloadKey(tenant) : FailedNaturalLanguageEmbeddingRegistry.LivePayloadKey(tenant);
        var record = Record(tenant, "victim");
        string observed = FailedNaturalLanguageEmbeddingRegistry.SerializeRecord(record);
        await db.SortedSetAddAsync(queue, "victim", 1);
        await db.HashSetAsync(payloads, "victim", observed);
        if (changed == "payload") await db.HashSetAsync(payloads, "victim", FailedNaturalLanguageEmbeddingRegistry.SerializeRecord(record with { TruncatedRawJsonPayload = "newer" }));
        if (changed == "score") await db.SortedSetAddAsync(queue, "victim", 2);
        if (changed == "membership") await db.SortedSetRemoveAsync(queue, "victim");
        RedisValue before = await db.HashGetAsync(payloads, "victim");
        (await FailedNaturalLanguageEmbeddingRegistry.RemoveObservedAsync(db, tenant, queue, payloads, "victim", observed, 1, token)).ShouldBeFalse();
        using var restarted = await ConnectionMultiplexer.ConnectAsync(redis.ConnectionString);
        (await restarted.GetDatabase().HashGetAsync(payloads, "victim")).ShouldBe(before);
        (await db.SortedSetScoreAsync(queue, "victim")).ShouldBe(changed == "membership" ? null : changed == "score" ? 2d : 1d);
    }

    /// <summary>
    /// Verifies misaddressed Modern Payloads Cannot Retry Or Overflow Another Unit.
    /// </summary>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task MisaddressedModernPayloadsCannotRetryOrOverflowAnotherUnit(bool foreignTenant)
    {
        var token = TestContext.Current.CancellationToken;
        string tenant = "misaddressed-" + Guid.NewGuid().ToString("N");
        IDatabase db = redis.Connection.GetDatabase();
        var payload = Record(foreignTenant ? tenant + "-foreign" : tenant, "other-unit");
        string bytes = FailedNaturalLanguageEmbeddingRegistry.SerializeRecord(payload);
        await db.SortedSetAddAsync(FailedNaturalLanguageEmbeddingRegistry.LiveKey(tenant), "selected-unit", 1);
        await db.HashSetAsync(FailedNaturalLanguageEmbeddingRegistry.LivePayloadKey(tenant), "selected-unit", bytes);
        var registry = new FailedNaturalLanguageEmbeddingRegistry(redis.Connection, NullLogger<FailedNaturalLanguageEmbeddingRegistry>.Instance,
            Options.Create(new NaturalLanguageDescriptionOptions { LiveRetryQueueMaxEntries = 1 }));
        (await Should.ThrowAsync<InvalidOperationException>(() => registry.DequeueBatchAsync(tenant, 10, token))).Message.ShouldBe("nl-retry-payload-scope");
        (await Should.ThrowAsync<InvalidOperationException>(() => registry.EnqueueAsync(Record(tenant, "survivor") with { QueuedAtTicks = 2 }, token))).Message.ShouldBe("nl-retry-payload-scope");
        (await db.HashGetAsync(FailedNaturalLanguageEmbeddingRegistry.LivePayloadKey(tenant), "selected-unit")).ToString().ShouldBe(bytes);
        (await db.HashGetAsync(FailedNaturalLanguageEmbeddingRegistry.DeadPayloadKey(tenant), "other-unit")).HasValue.ShouldBeFalse();
        (await db.SortedSetScoreAsync(FailedNaturalLanguageEmbeddingRegistry.LiveKey(tenant), "selected-unit")).ShouldBe(1);
        (await db.HashGetAsync(FailedNaturalLanguageEmbeddingRegistry.LivePayloadKey(tenant), "survivor")).HasValue.ShouldBeTrue();
    }

    /// <summary>
    /// Verifies modern First Completion Retires Verified Aliases But Distinct Legacy Work Remains Blocked After Restart.
    /// </summary>
    [Fact]
    public async Task ModernFirstCompletionRetiresVerifiedAliasesButDistinctLegacyWorkRemainsBlockedAfterRestart()
    {
        var token = TestContext.Current.CancellationToken;
        string tenant = "modern-first-" + Guid.NewGuid().ToString("N");
        IDatabase db = redis.Connection.GetDatabase();
        var record = Record(tenant, "0-victim");
        var registry = new FailedNaturalLanguageEmbeddingRegistry(redis.Connection, NullLogger<FailedNaturalLanguageEmbeddingRegistry>.Instance);
        await registry.EnqueueAsync(record, token);
        string alias = FailedNaturalLanguageEmbeddingRegistry.SerializeRecord(record with { Attempts = 3 });
        string distinct = FailedNaturalLanguageEmbeddingRegistry.SerializeRecord(record with { TruncatedRawJsonPayload = "distinct-payload" });
        await db.SortedSetAddAsync(FailedNaturalLanguageEmbeddingRegistry.LiveKey(tenant), [new(alias, 1), new(distinct, 1)]);
        (await registry.DequeueBatchAsync(tenant, 1, token)).ShouldHaveSingleItem().ShouldBe(record);
        await registry.CompleteAsync(record, token);
        (await db.HashGetAsync(FailedNaturalLanguageEmbeddingRegistry.LivePayloadKey(tenant), record.MemoryUnitId)).HasValue.ShouldBeFalse();
        (await db.SortedSetScoreAsync(FailedNaturalLanguageEmbeddingRegistry.LiveKey(tenant), alias)).ShouldBeNull();
        (await db.SortedSetScoreAsync(FailedNaturalLanguageEmbeddingRegistry.LiveKey(tenant), distinct)).ShouldBe(1);
        using var restarted = await ConnectionMultiplexer.ConnectAsync(redis.ConnectionString);
        var restored = new FailedNaturalLanguageEmbeddingRegistry(restarted, NullLogger<FailedNaturalLanguageEmbeddingRegistry>.Instance);
        (await Should.ThrowAsync<RedisServerException>(() => restored.DequeueBatchAsync(tenant, 10, token))).Message.ShouldContain("nl-retry-legacy-lineage-reconciliation-required");
        (await db.HashGetAsync(FailedNaturalLanguageEmbeddingRegistry.LivePayloadKey(tenant), record.MemoryUnitId)).HasValue.ShouldBeFalse();
        (await db.SortedSetScoreAsync(FailedNaturalLanguageEmbeddingRegistry.LiveKey(tenant), distinct)).ShouldBe(1);
        (await db.SetContainsAsync(RetryWorkflowHistory.Units(tenant), record.MemoryUnitId)).ShouldBeTrue();
    }

    /// <summary>
    /// Verifies same Unit Scheduler Lease Serializes Attempts And Stale Acknowledgement Cannot Clear New Authority.
    /// </summary>
    [Fact]
    public async Task SameUnitSchedulerLeaseSerializesAttemptsAndStaleAcknowledgementCannotClearNewAuthority()
    {
        var token = TestContext.Current.CancellationToken;
        string tenant = "schedule-lease-" + Guid.NewGuid().ToString("N");
        IDatabase db = redis.Connection.GetDatabase();
        string first = (await RetryWorkflowHistory.BeginAsync(db, tenant, "victim", token)).ShouldNotBeNull();
        (await RetryWorkflowHistory.BeginAsync(db, tenant, "victim", token)).ShouldBeNull();
        (await db.HashLengthAsync(RetryWorkflowHistory.Scheduling(tenant))).ShouldBe(1);
        using var restarted = await ConnectionMultiplexer.ConnectAsync(redis.ConnectionString);
        (await RetryWorkflowHistory.BeginAsync(restarted.GetDatabase(), tenant, "victim", token)).ShouldBeNull();
        await RetryWorkflowHistory.AcknowledgeAsync(db, tenant, "victim", "foreign-authority", token);
        (await db.HashGetAsync(RetryWorkflowHistory.Scheduling(tenant), first)).ToString().ShouldBe("victim");
        await RetryWorkflowHistory.AcknowledgeAsync(db, tenant, "victim", first, token);
        string second = (await RetryWorkflowHistory.BeginAsync(db, tenant, "victim", token)).ShouldNotBeNull();
        second.ShouldNotBe(first);
        await RetryWorkflowHistory.AcknowledgeAsync(db, tenant, "victim", first, token);
        (await db.HashGetAsync(RetryWorkflowHistory.Scheduling(tenant), second)).ToString().ShouldBe("victim");
        (await Should.ThrowAsync<InvalidOperationException>(() => FailedNaturalLanguageEmbeddingRegistry.PurgeUnitAsync(db, tenant, "victim", token)))
            .Message.ShouldBe("nl-retry-scheduling-unresolved");
        (await Should.ThrowAsync<InvalidOperationException>(() => RetryWorkflowHistory.PurgeTenantAsync(db, null, tenant, false, token)))
            .Message.ShouldBe("nl-retry-scheduling-unresolved");
        await RetryWorkflowHistory.AcknowledgeAsync(db, tenant, "victim", second, token);
        await FailedNaturalLanguageEmbeddingRegistry.PurgeUnitAsync(db, tenant, "victim", token);
        (await db.HashLengthAsync(RetryWorkflowHistory.Scheduling(tenant))).ShouldBe(0);
        (await db.KeyExistsAsync(RetryWorkflowHistory.Lease(tenant, "victim"))).ShouldBeFalse();
        (await RetryWorkflowHistory.BeginAsync(db, tenant, "victim", token)).ShouldBeNull();
    }

    /// <summary>
    /// Verifies actual Queue Maintenance Preserves ASelection Changed At Its Final Redis Command.
    /// </summary>
    [Theory]
    [InlineData("overflow", "payload")]
    [InlineData("overflow", "score")]
    [InlineData("overflow", "membership")]
    [InlineData("corrupt", "payload")]
    [InlineData("corrupt", "score")]
    [InlineData("corrupt", "membership")]
    [InlineData("dead", "payload")]
    [InlineData("dead", "score")]
    [InlineData("dead", "membership")]
    public async Task ActualQueueMaintenancePreservesASelectionChangedAtItsFinalRedisCommand(string operation, string changed)
    {
        var token = TestContext.Current.CancellationToken;
        string tenant = "maintenance-race-" + Guid.NewGuid().ToString("N");
        IDatabase db = redis.Connection.GetDatabase();
        bool dead = operation == "dead";
        string queue = dead ? FailedNaturalLanguageEmbeddingRegistry.DeadKey(tenant) : FailedNaturalLanguageEmbeddingRegistry.LiveKey(tenant);
        string payloads = dead ? FailedNaturalLanguageEmbeddingRegistry.DeadPayloadKey(tenant) : FailedNaturalLanguageEmbeddingRegistry.LivePayloadKey(tenant);
        var old = Record(tenant, "old");
        string bytes = operation == "corrupt" ? "malformed-json" : FailedNaturalLanguageEmbeddingRegistry.SerializeRecord(old);
        await db.SortedSetAddAsync(queue, "old", 1);
        await db.HashSetAsync(payloads, "old", bytes);
        bool entered = false;
        RedisValue retained = RedisValue.Null;
        IDatabase intercepted = RetryDatabaseInterception.Create(db,
            (script, values) => !entered && (operation == "overflow" ? script.Contains("nl-retry-atomic-move-v3", StringComparison.Ordinal) && values[2] == "overflow"
                : script.Contains("nl-retry-observed-prune-v1", StringComparison.Ordinal)),
            async () =>
            {
                entered = true;
                if (changed == "payload") await db.HashSetAsync(payloads, "old", FailedNaturalLanguageEmbeddingRegistry.SerializeRecord(old with { TruncatedRawJsonPayload = "replacement" }));
                if (changed == "score") await db.SortedSetAddAsync(queue, "old", 999);
                if (changed == "membership") await db.SortedSetRemoveAsync(queue, "old");
                retained = await db.HashGetAsync(payloads, "old");
            });
        var connection = Substitute.For<IConnectionMultiplexer>();
        connection.GetDatabase(Arg.Any<int>(), Arg.Any<object>()).Returns(intercepted);
        var registry = new FailedNaturalLanguageEmbeddingRegistry(connection, NullLogger<FailedNaturalLanguageEmbeddingRegistry>.Instance,
            Options.Create(new NaturalLanguageDescriptionOptions { LiveRetryQueueMaxEntries = 1, DeadRetryQueueMaxEntries = 1 }));
        var fresh = Record(tenant, "fresh") with { QueuedAtTicks = 2 };
        await registry.EnqueueAsync(fresh, token);
        if (dead) await registry.IncrementAttemptsAsync(fresh, 1, token);
        entered.ShouldBeTrue();
        using var restarted = await ConnectionMultiplexer.ConnectAsync(redis.ConnectionString);
        (await restarted.GetDatabase().HashGetAsync(payloads, "old")).ShouldBe(retained);
        (await db.SortedSetScoreAsync(queue, "old")).ShouldBe(changed == "membership" ? null : changed == "score" ? 999d : 1d);
        if (!dead) (await db.HashGetAsync(FailedNaturalLanguageEmbeddingRegistry.DeadPayloadKey(tenant), "old")).HasValue.ShouldBeFalse();
    }

    /// <summary>
    /// Verifies malformed Legacy Pruning Retains Only AContent Free Reconciliation Obligation.
    /// </summary>
    [Fact]
    public async Task MalformedLegacyPruningRetainsOnlyAContentFreeReconciliationObligation()
    {
        var token = TestContext.Current.CancellationToken;
        string tenant = "malformed-legacy-" + Guid.NewGuid().ToString("N");
        IDatabase db = redis.Connection.GetDatabase();
        string malformed = "{\"tenantId\":\"" + tenant + "\",\"truncatedRawJsonPayload\":\"synthetic-private-content\"}";
        await db.SortedSetAddAsync(FailedNaturalLanguageEmbeddingRegistry.LiveKey(tenant), malformed, 1);
        var registry = new FailedNaturalLanguageEmbeddingRegistry(redis.Connection, NullLogger<FailedNaturalLanguageEmbeddingRegistry>.Instance);
        (await registry.DequeueBatchAsync(tenant, 10, token)).ShouldBeEmpty();
        (await db.SortedSetLengthAsync(FailedNaturalLanguageEmbeddingRegistry.LiveKey(tenant))).ShouldBe(0);
        (await db.SetMembersAsync(RetryWorkflowHistory.Units(tenant))).ShouldBeEmpty();
        (await db.StringGetAsync(RetryWorkflowHistory.Unresolved(tenant))).ToString().ShouldBe("1");
        await db.StringSetAsync(RetryWorkflowHistory.Coverage(tenant), RetryWorkflowHistory.ReconciledCoverage);
        (await Should.ThrowAsync<InvalidOperationException>(() => RetryWorkflowHistory.PurgeTenantAsync(db, null, tenant, false, token)))
            .Message.ShouldBe("nl-retry-history-owner-reconciliation-required");
    }

    /// <summary>
    /// Verifies atomic Selection Protects Refresh Before The Former Payload And Score Reads.
    /// </summary>
    [Theory]
    [InlineData("overflow")]
    [InlineData("corrupt")]
    [InlineData("dead")]
    public async Task AtomicSelectionProtectsRefreshBeforeTheFormerPayloadAndScoreReads(string operation)
    {
        var token = TestContext.Current.CancellationToken;
        string tenant = "selection-race-" + Guid.NewGuid().ToString("N");
        IDatabase db = redis.Connection.GetDatabase();
        bool dead = operation == "dead";
        string queue = dead ? FailedNaturalLanguageEmbeddingRegistry.DeadKey(tenant) : FailedNaturalLanguageEmbeddingRegistry.LiveKey(tenant);
        string payloads = dead ? FailedNaturalLanguageEmbeddingRegistry.DeadPayloadKey(tenant) : FailedNaturalLanguageEmbeddingRegistry.LivePayloadKey(tenant);
        var old = Record(tenant, "old");
        await db.SortedSetAddAsync(queue, "old", 1);
        await db.HashSetAsync(payloads, "old", operation == "corrupt" ? "invalid-json" : FailedNaturalLanguageEmbeddingRegistry.SerializeRecord(old));
        string replacement = FailedNaturalLanguageEmbeddingRegistry.SerializeRecord(old with { TruncatedRawJsonPayload = "newer", QueuedAtTicks = 999 });
        bool refreshed = false;
        IDatabase intercepted = RetryDatabaseInterception.Create(db,
            (script, _) => !refreshed && script.Contains("nl-retry-observed-selection-v1", StringComparison.Ordinal),
            () => Task.CompletedTask,
            after: async () =>
            {
                refreshed = true;
                await db.HashSetAsync(payloads, "old", replacement);
                await db.SortedSetAddAsync(queue, "old", 999);
            });
        var connection = Substitute.For<IConnectionMultiplexer>();
        connection.GetDatabase(Arg.Any<int>(), Arg.Any<object>()).Returns(intercepted);
        var registry = new FailedNaturalLanguageEmbeddingRegistry(connection, NullLogger<FailedNaturalLanguageEmbeddingRegistry>.Instance,
            Options.Create(new NaturalLanguageDescriptionOptions { LiveRetryQueueMaxEntries = 1, DeadRetryQueueMaxEntries = 1 }));
        var fresh = Record(tenant, "fresh") with { QueuedAtTicks = 2 };
        await registry.EnqueueAsync(fresh, token);
        if (dead) await registry.IncrementAttemptsAsync(fresh, 1, token);
        refreshed.ShouldBeTrue();
        (await db.HashGetAsync(payloads, "old")).ToString().ShouldBe(replacement);
        (await db.SortedSetScoreAsync(queue, "old")).ShouldBe(999);
        (await db.KeyExistsAsync(RetryWorkflowHistory.Unresolved(tenant))).ShouldBeFalse();
    }

    /// <summary>
    /// Verifies rejected Corrupt Prune Does Not Install An Unresolved History Marker.
    /// </summary>
    [Fact]
    public async Task RejectedCorruptPruneDoesNotInstallAnUnresolvedHistoryMarker()
    {
        var token = TestContext.Current.CancellationToken;
        string tenant = "rejected-prune-" + Guid.NewGuid().ToString("N");
        IDatabase db = redis.Connection.GetDatabase();
        var record = Record(tenant, "victim");
        string replacement = FailedNaturalLanguageEmbeddingRegistry.SerializeRecord(record);
        await db.SortedSetAddAsync(FailedNaturalLanguageEmbeddingRegistry.LiveKey(tenant), "victim", 1);
        await db.HashSetAsync(FailedNaturalLanguageEmbeddingRegistry.LivePayloadKey(tenant), "victim", replacement);
        (await FailedNaturalLanguageEmbeddingRegistry.RemoveObservedAsync(db, tenant, FailedNaturalLanguageEmbeddingRegistry.LiveKey(tenant),
            FailedNaturalLanguageEmbeddingRegistry.LivePayloadKey(tenant), "victim", "old-corrupt-json", 1, token)).ShouldBeFalse();
        (await db.KeyExistsAsync(RetryWorkflowHistory.Unresolved(tenant))).ShouldBeFalse();
        (await db.HashGetAsync(FailedNaturalLanguageEmbeddingRegistry.LivePayloadKey(tenant), "victim")).ToString().ShouldBe(replacement);
    }

    /// <summary>
    /// Verifies unit Purge Capacity Is Atomic And Existing Units Remain Idempotent Without Losing Fences.
    /// </summary>
    [Fact]
    public async Task UnitPurgeCapacityIsAtomicAndExistingUnitsRemainIdempotentWithoutLosingFences()
    {
        var token = TestContext.Current.CancellationToken;
        string tenant = "history-capacity-" + Guid.NewGuid().ToString("N");
        IDatabase db = redis.Connection.GetDatabase();
        await db.SetAddAsync(RetryWorkflowHistory.Units(tenant), Enumerable.Range(0, TenantIngestionOwnership.Capacity - 1).Select(i => (RedisValue)("unit-" + i)).ToArray());
        await FailedNaturalLanguageEmbeddingRegistry.PurgeUnitAsync(db, tenant, "boundary", token);
        (await db.SetLengthAsync(RetryWorkflowHistory.Units(tenant))).ShouldBe(TenantIngestionOwnership.Capacity);
        await FailedNaturalLanguageEmbeddingRegistry.PurgeUnitAsync(db, tenant, "boundary", token);
        string bytes = FailedNaturalLanguageEmbeddingRegistry.SerializeRecord(Record(tenant, "excess"));
        await db.SortedSetAddAsync(FailedNaturalLanguageEmbeddingRegistry.LiveKey(tenant), "excess", 1);
        await db.HashSetAsync(FailedNaturalLanguageEmbeddingRegistry.LivePayloadKey(tenant), "excess", bytes);
        (await Should.ThrowAsync<RedisServerException>(() => FailedNaturalLanguageEmbeddingRegistry.PurgeUnitAsync(db, tenant, "excess", token)))
            .Message.ShouldContain("nl-retry-history-capacity");
        (await db.KeyExistsAsync(SourcePublicationVisibility.Tombstone(tenant, "excess"))).ShouldBeTrue();
        (await db.HashGetAsync(FailedNaturalLanguageEmbeddingRegistry.LivePayloadKey(tenant), "excess")).ToString().ShouldBe(bytes);
        (await db.SetLengthAsync(RetryWorkflowHistory.Units(tenant))).ShouldBe(TenantIngestionOwnership.Capacity);
        (await db.SetContainsAsync(RetryWorkflowHistory.Units(tenant), "excess")).ShouldBeFalse();
    }

    /// <summary>
    /// Verifies tenant Purge Validates Many Units With Only One Scan Of Each Queue And Hash.
    /// </summary>
    [Fact]
    public async Task TenantPurgeValidatesManyUnitsWithOnlyOneScanOfEachQueueAndHash()
    {
        var token = TestContext.Current.CancellationToken;
        string tenant = "linear-tenant-purge-" + Guid.NewGuid().ToString("N");
        IDatabase db = redis.Connection.GetDatabase();
        await db.StringSetAsync(RetryWorkflowHistory.Coverage(tenant), RetryWorkflowHistory.ReconciledCoverage);
        for (int i = 0; i < 128; i++)
        {
            string unit = "unit-" + i;
            await db.SortedSetAddAsync(FailedNaturalLanguageEmbeddingRegistry.LiveKey(tenant), unit, i);
            await db.HashSetAsync(FailedNaturalLanguageEmbeddingRegistry.LivePayloadKey(tenant), unit, FailedNaturalLanguageEmbeddingRegistry.SerializeRecord(Record(tenant, unit)));
        }
        int queueScans = 0, hashScans = 0;
        var intercepted = RetryDatabaseInterception.Create(db, (_, _) => false, () => Task.CompletedTask,
            observe: method => { if (method == nameof(IDatabase.SortedSetScanAsync)) queueScans++; if (method == nameof(IDatabase.HashScanAsync)) hashScans++; });
        await RetryWorkflowHistory.PurgeTenantAsync(intercepted, Substitute.For<IDaprWorkflowClient>(), tenant, false, token);
        queueScans.ShouldBe(2);
        hashScans.ShouldBe(2);
        (await db.SetLengthAsync(RetryWorkflowHistory.Units(tenant))).ShouldBe(128);
        (await db.KeyExistsAsync(FailedNaturalLanguageEmbeddingRegistry.LiveKey(tenant))).ShouldBeFalse();
        (await db.KeyExistsAsync(FailedNaturalLanguageEmbeddingRegistry.LivePayloadKey(tenant))).ShouldBeFalse();
        (await db.KeyExistsAsync(SourcePublicationVisibility.Tombstone(tenant, "unit-127"))).ShouldBeTrue();
    }

    /// <summary>
    /// Verifies completion Preserves Historical Aliases With Unknown Or Duplicate Admission Metadata.
    /// </summary>
    [Theory]
    [InlineData("unknown")]
    [InlineData("duplicate")]
    [InlineData("conflicting")]
    public async Task CompletionPreservesHistoricalAliasesWithUnknownOrDuplicateAdmissionMetadata(string shape)
    {
        var token = TestContext.Current.CancellationToken;
        string tenant = "ambiguous-alias-" + Guid.NewGuid().ToString("N");
        IDatabase db = redis.Connection.GetDatabase();
        var record = Record(tenant, "0-victim");
        var registry = new FailedNaturalLanguageEmbeddingRegistry(redis.Connection, NullLogger<FailedNaturalLanguageEmbeddingRegistry>.Instance);
        await registry.EnqueueAsync(record, token);
        string alias = FailedNaturalLanguageEmbeddingRegistry.SerializeRecord(record with { Attempts = 3 });
        alias = shape switch
        {
            "unknown" => alias[..^1] + ",\"futureAdmissionVersion\":\"unrecognized\"}",
            "duplicate" => alias[..^1] + ",\"memoryUnitId\":\"0-victim\"}",
            _ => "{\"memoryUnitId\":\"other-unit\"," + alias[1..],
        };
        await db.SortedSetAddAsync(FailedNaturalLanguageEmbeddingRegistry.LiveKey(tenant), alias, 1);
        await registry.CompleteAsync(record, token);
        (await db.SortedSetScoreAsync(FailedNaturalLanguageEmbeddingRegistry.LiveKey(tenant), alias)).ShouldBe(1);
        (await db.SortedSetRangeByRankAsync(FailedNaturalLanguageEmbeddingRegistry.LiveKey(tenant))).ShouldContain((RedisValue)alias);
        (await db.HashGetAsync(FailedNaturalLanguageEmbeddingRegistry.LivePayloadKey(tenant), record.MemoryUnitId)).HasValue.ShouldBeFalse();
        await Should.ThrowAsync<InvalidOperationException>(() => registry.DequeueBatchAsync(tenant, 10, token));
        (await db.SortedSetScoreAsync(FailedNaturalLanguageEmbeddingRegistry.LiveKey(tenant), alias)).ShouldBe(1);
    }

    private static HashEntry[] Fields(string unit, string content) => [new("memoryUnitId", unit), new("caseId", "case"), new("embedding", new byte[] { 1, 0, 0, 0 }), new("naturalLanguageDescription", content)];
    private static FailedNaturalLanguageEmbeddingRecord Record(string tenant, string unit)
        => new(tenant, unit, "private-raw-" + unit, "TestEvent", null, "case", "test", "test", 1, 1, 0);
}
