namespace Hexalith.Memories.IntegrationTests.Ingestion;

using Dapr.Workflow;
using Hexalith.EventStore.Client.Queries;
using Hexalith.Memories.Contracts.V1;
using Hexalith.Memories.IntegrationTests.Fixtures;
using Hexalith.Memories.Server.Activities.Cases;
using Hexalith.Memories.Server.Activities.Ingestion;
using Hexalith.Memories.Server.Graph;
using Hexalith.Memories.Server.Infrastructure;
using Hexalith.Memories.Server.Ingestion;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Options;
using NSubstitute;
using Shouldly;
using StackExchange.Redis;

/// <summary>Actual Redis issuance, exact historical reconciliation and physical erasure across connection recreation.</summary>
[Collection("RedisStack")]
[Trait("Category", "Integration")]
public sealed class UnitOwnershipAuthorityIntegrationTests(RedisStackFixture redis, FalkorDbFixture graph) : IClassFixture<FalkorDbFixture>
{
    /// <summary>Fresh annotation admission reserves authority before graph/content writes; deletion cannot free its identity.</summary>
    [Fact]
    public async Task IssuedAnnotationRetainsAuthorityAcrossDeletionPurgeAndSourceChange()
    {
        CancellationToken token = TestContext.Current.CancellationToken;
        string tenant = "issued-" + Guid.NewGuid().ToString("N");
        var settings = Settings();
        string unit = UnitOwnershipAuthority.Qualify("annotation", settings.Value.OwnershipGeneration);
        var store = Store(redis.Connection, settings);
        IngestionInput input = Input(tenant);
        var admitted = await new AdmitSourceInventoryActivity(store, Substitute.For<IDaprWorkflowClient>()).RunAsync(null!, new(unit, input));
        admitted.SourceInventoryTracked.ShouldBeTrue();
        IDatabase db = redis.Connection.GetDatabase();
        RedisValue authority = await db.StringGetAsync(UnitOwnershipAuthority.Key(tenant, unit));
        authority.HasValue.ShouldBeTrue();
        (await db.KeyExistsAsync(SourcePublicationVisibility.KeyProvenance(tenant, unit))).ShouldBeFalse();
        await store.RecordCandidateAsync(tenant, input.SourceUri, unit, unit, token);
        string chunk = IndexSchemaDefinitions.BuildSemanticChunkKey(tenant, unit, 7);
        await SourcePublicationVisibility.WriteAsync(db, tenant, unit, chunk, Hash(tenant, unit), true, input.CaseId, input.SourceUri);
        string survivor = IndexSchemaDefinitions.BuildSemanticChunkKey(tenant, "foreign", 7);
        await db.HashSetAsync(survivor, Hash(tenant, "foreign"));
        HashEntry[] survivorBytes = await db.HashGetAllAsync(survivor);
        await DeleteAsync(redis.Connection, tenant, unit);
        (await db.KeyExistsAsync(chunk)).ShouldBeFalse();
        (await db.StringGetAsync(UnitOwnershipAuthority.Key(tenant, unit))).ShouldBe(authority);
        using IConnectionMultiplexer recreated = await ConnectionMultiplexer.ConnectAsync(redis.ConnectionString);
        var after = Store(recreated, settings);
        await Should.ThrowAsync<RedisServerException>(() => after.AdmitAsync(unit, input with { SourceUri = input.SourceUri + "/other" }, token));
        await Should.ThrowAsync<RedisServerException>(() => Store(recreated).AdmitAsync(unit, input with { SourceUri = input.SourceUri + "/unconfigured" }, token));
        (await recreated.GetDatabase().StringGetAsync(UnitOwnershipAuthority.Key(tenant, unit))).ShouldBe(authority);
        AssertBytes(await db.HashGetAllAsync(survivor), survivorBytes);
        await TenantIngestionOwnership.PurgeAsync(recreated, tenant, null, token);
        (await db.StringGetAsync(UnitOwnershipAuthority.Key(tenant, unit))).ShouldBe(authority);
        (await db.KeyExistsAsync(UnitOwnershipAuthority.GenerationKey(settings.Value.OwnershipGeneration!))).ShouldBeTrue();
        AssertBytes(await db.HashGetAllAsync(survivor), survivorBytes);
    }

    /// <summary>Source-B coverage and old bare provenance cannot certify source-A historical chunks or natural-language output.</summary>
    /// <param name="bareMarker">The historical unscoped marker, if any.</param>
    [Theory]
    [InlineData(null)]
    [InlineData("modern")]
    [InlineData("legacy-owner-reconciled-v1")]
    public async Task HistoricalUnitRequiresExactEvidenceRegardlessOfSourceOrBareMarker(string? bareMarker)
    {
        CancellationToken token = TestContext.Current.CancellationToken;
        string tenant = "historical-authority-" + Guid.NewGuid().ToString("N");
        const string unit = "purged-producer";
        var settings = Settings();
        var store = Store(redis.Connection, settings);
        IngestionInput sourceB = Input(tenant) with { SourceUri = "https://synthetic.invalid/source-b" };
        IDatabase db = redis.Connection.GetDatabase();
        string[] keys = [IndexSchemaDefinitions.BuildSemanticChunkKey(tenant, unit, 7),
            IndexSchemaDefinitions.BuildLegacyNaturalLanguageSemanticKey(tenant, unit)];
        foreach (string key in keys) { await db.HashSetAsync(key, Hash(tenant, unit)); }
        HashEntry[][] original = await Task.WhenAll(keys.Select(key => db.HashGetAllAsync(key)));
        string foreign = IndexSchemaDefinitions.BuildSemanticChunkKey(tenant, "foreign", 9);
        await db.HashSetAsync(foreign, Hash(tenant, "foreign"));
        HashEntry[] foreignBytes = await db.HashGetAllAsync(foreign);
        if (bareMarker is not null) { await db.StringSetAsync(SourcePublicationVisibility.KeyProvenance(tenant, unit), bareMarker); }
        await store.FenceAsync(tenant, sourceB.SourceUri, false, token);
        SourceInventoryPage fenced = await store.ReadAsync(tenant, sourceB.SourceUri, null, 100, token);
        await store.ReconcileAsync(tenant, sourceB.SourceUri, fenced.Version, [], "owned-source-b-only", token);
        SourceInventoryPage covered = await store.ReadAsync(tenant, sourceB.SourceUri, null, 100, token);
        await store.ReopenAsync(tenant, sourceB.SourceUri, covered.Version, token);
        await store.AdmitAsync(unit, sourceB, token);
        if (bareMarker is null)
            await Should.ThrowAsync<RedisServerException>(() => store.RecordCandidateAsync(tenant, sourceB.SourceUri, unit, unit, token));
        else
            await store.RecordCandidateAsync(tenant, sourceB.SourceUri, unit, unit, token);
        using IConnectionMultiplexer recreated = await ConnectionMultiplexer.ConnectAsync(redis.ConnectionString);
        var refused = await Should.ThrowAsync<InvalidOperationException>(() => DeleteAsync(recreated, tenant, unit));
        refused.Message.ShouldBe("source-publication-legacy-owner-reconciliation-required");
        for (int i = 0; i < keys.Length; i++) { AssertBytes(await db.HashGetAllAsync(keys[i]), original[i]); }
        AssertBytes(await db.HashGetAllAsync(foreign), foreignBytes);
        // Only this fixture's complete known unit inventory and excluded writers authorize repair.
        await Should.ThrowAsync<InvalidOperationException>(() => Store(recreated).ReconcileUnitAsync(tenant, unit, keys, "unit-proof", null, token));
        await Store(recreated, settings).ReconcileUnitAsync(tenant, unit, keys, "owned-unit-proof:" + Guid.NewGuid().ToString("N"), null, token);
        (await SemanticKeyDiscovery.FindAsync(db, tenant, unit, token)).ShouldContain(keys[0]);
        await DeleteAsync(recreated, tenant, unit);
        foreach (string key in keys) { (await db.KeyExistsAsync(key)).ShouldBeFalse(); }
        (await db.KeyExistsAsync(UnitOwnershipAuthority.Key(tenant, unit))).ShouldBeTrue();
        AssertBytes(await db.HashGetAllAsync(foreign), foreignBytes);
    }

    /// <summary>Reconciliation is bound to exact tenant/unit ownership, evidence identity and immutable exclusion custody.</summary>
    [Fact]
    public async Task HistoricalReconciliationRefusesForeignKeysChangedEvidenceAndReusedGeneration()
    {
        CancellationToken token = TestContext.Current.CancellationToken;
        string tenant = "proof-controls-" + Guid.NewGuid().ToString("N");
        var settings = Settings();
        var store = Store(redis.Connection, settings);
        string key = IndexSchemaDefinitions.BuildSemanticChunkKey(tenant, "unit", 7);
        IDatabase db = redis.Connection.GetDatabase();
        await db.HashSetAsync(key, Hash(tenant, "foreign"));
        await Should.ThrowAsync<RedisServerException>(() => store.ReconcileUnitAsync(tenant, "unit", [key], "exact-unit-proof", null, token));
        (await db.KeyExistsAsync(UnitOwnershipAuthority.Key(tenant, "unit"))).ShouldBeFalse();
        await db.HashSetAsync(key, Hash(tenant, "unit"));
        await store.ReconcileUnitAsync(tenant, "unit", [key], "exact-unit-proof", null, token);
        RedisValue proof = await db.StringGetAsync(UnitOwnershipAuthority.Key(tenant, "unit"));
        HashEntry[] original = await db.HashGetAllAsync(key);
        using IConnectionMultiplexer recreated = await ConnectionMultiplexer.ConnectAsync(redis.ConnectionString);
        var recovered = Store(recreated, settings);
        // A committed reconciliation whose acknowledgement was lost must accept the identical desired retry.
        await recovered.ReconcileUnitAsync(tenant, "unit", [key], "exact-unit-proof", null, token);
        (await db.StringGetAsync(SourcePublicationVisibility.KeyProvenance(tenant, "unit"))).ShouldBe(proof);
        RedisValue previousProof = proof;
        await recovered.ReconcileUnitAsync(tenant, "unit", [key], "replacement-proof", proof, token);
        proof = await db.StringGetAsync(UnitOwnershipAuthority.Key(tenant, "unit"));
        proof.ShouldNotBe(previousProof);
        (await db.StringGetAsync(SourcePublicationVisibility.KeyProvenance(tenant, "unit"))).ShouldBe(proof);
        await recovered.ReconcileUnitAsync(tenant, "unit", [key], "replacement-proof", previousProof, token);
        await Should.ThrowAsync<RedisServerException>(() => recovered.ReconcileUnitAsync(tenant, "unit", [key], "stale-replacement", previousProof, token));
        var omitted = await Should.ThrowAsync<RedisServerException>(() => recovered.ReconcileUnitAsync(tenant, "unit", [], "omitted-obligation", proof, token));
        omitted.Message.ShouldContain("unit-ownership-reconciliation-incomplete");
        (await db.StringGetAsync(UnitOwnershipAuthority.Key(tenant, "unit"))).ShouldBe(proof);
        (await db.StringGetAsync(SourcePublicationVisibility.KeyProvenance(tenant, "unit"))).ShouldBe(proof);
        AssertBytes(await db.HashGetAllAsync(key), original);
        await Should.ThrowAsync<RedisServerException>(() => store.ReconcileUnitAsync(tenant, "unit", [key], "different-proof", null, token));
        var conflicting = Options.Create(new IngestionSettings { OwnershipGeneration = settings.Value.OwnershipGeneration,
            OwnershipWriterExclusionEvidenceId = "another-cutover" });
        await Should.ThrowAsync<RedisServerException>(() => Store(redis.Connection, conflicting).ReconcileUnitAsync(tenant, "unit", [key], "same-unit", proof, token));
        await db.StringSetAsync(SourcePublicationVisibility.KeyProvenance(tenant, "other"), proof);
        await db.StringSetAsync(UnitOwnershipAuthority.Key(tenant, "other"), proof);
        await Should.ThrowAsync<InvalidOperationException>(() => SemanticKeyDiscovery.FindAsync(db, tenant, "other", token));
        (await db.StringGetAsync(UnitOwnershipAuthority.Key(tenant, "unit"))).ShouldBe(proof);
    }

    /// <summary>Removing issuer configuration cannot let another source borrow a retained issuance receipt.</summary>
    [Fact]
    public async Task UnconfiguredProducerCannotBorrowIssuedAuthorityForUnscheduledCleanup()
    {
        CancellationToken token = TestContext.Current.CancellationToken;
        string tenant = "authority-binding-" + Guid.NewGuid().ToString("N");
        var settings = Settings();
        string unit = UnitOwnershipAuthority.Qualify("unit", settings.Value.OwnershipGeneration);
        IngestionInput input = Input(tenant);
        await Store(redis.Connection, settings).AdmitAsync(unit, input, token);
        RedisValue authority = await redis.Connection.GetDatabase().StringGetAsync(UnitOwnershipAuthority.Key(tenant, unit));
        using IConnectionMultiplexer recreated = await ConnectionMultiplexer.ConnectAsync(redis.ConnectionString);
        var other = Store(recreated);
        var refused = await Should.ThrowAsync<RedisServerException>(() => other.AdmitAsync(unit,
            input with { SourceUri = input.SourceUri + "/other" }, token));
        refused.Message.ShouldContain("unit-ownership-producer-conflict");
        (await other.ReadAsync(tenant, input.SourceUri + "/other", null, 100, token)).Entries.ShouldBeEmpty();
        UnitOwnershipAuthority.IsProducer(authority, unit, input.SourceUri + "/other", input.CaseId).ShouldBeFalse();
        (await recreated.GetDatabase().StringGetAsync(UnitOwnershipAuthority.Key(tenant, unit))).ShouldBe(authority);
    }

    /// <summary>Directory batches reserve exact unit authority before their early claim-check writes.</summary>
    [Fact]
    public async Task DirectoryReservesIssuanceBeforeSavingAnySourcePayload()
    {
        CancellationToken token = TestContext.Current.CancellationToken;
        string tenant = "directory-issuance-" + Guid.NewGuid().ToString("N");
        string directory = Directory.CreateTempSubdirectory("owned-unit-directory-").FullName;
        try
        {
            await File.WriteAllTextAsync(Path.Combine(directory, "source.txt"), "owned synthetic bytes", token);
            var settings = Settings();
            settings.Value.AllowedDirectoryRoots = [directory];
            var inventory = Store(redis.Connection, settings);
            var payloads = Substitute.For<IWorkflowPayloadStore>();
            bool reservedBeforeWrite = false;
            payloads.SaveAsync(tenant, Arg.Any<string>(), WorkflowPayloadKind.SourceBytes, Arg.Any<ReadOnlyMemory<byte>>(), "source", Arg.Any<CancellationToken>())
                .Returns(async call =>
                {
                    string unit = call.ArgAt<string>(1);
                    RedisValue authority = await redis.Connection.GetDatabase().StringGetAsync(UnitOwnershipAuthority.Key(tenant, unit));
                    reservedBeforeWrite = UnitOwnershipAuthority.IsComplete(authority, authority, tenant, unit);
                    reservedBeforeWrite.ShouldBeTrue();
                    ReadOnlyMemory<byte> bytes = call.ArgAt<ReadOnlyMemory<byte>>(3);
                    string hash = Convert.ToHexStringLower(System.Security.Cryptography.SHA256.HashData(bytes.Span));
                    return new WorkflowPayloadReference(unit + ":sourcebytes:" + hash + ":source", hash, bytes.Length,
                        WorkflowPayloadKind.SourceBytes, tenant, unit);
                });
            var scheduler = Substitute.For<IIngestionWorkflowScheduler>();
            scheduler.ScheduleAsync(Arg.Any<string>(), Arg.Any<IngestionInput>(), Arg.Any<CancellationToken>())
                .Returns(call => call.ArgAt<string>(0));
            var service = new DirectoryIngestionService(settings, scheduler, Substitute.For<Dapr.Client.DaprClient>(),
                Microsoft.Extensions.Logging.Abstractions.NullLogger<DirectoryIngestionService>.Instance, payloads, sourceInventory: inventory);
            DirectoryIngestionResult result = await service.IngestAsync(new DirectoryIngestionRequest { TenantId = tenant,
                CaseId = "case", DirectoryPath = directory, IngestedBy = "owned-control" }, token);
            result.ErrorCode.ShouldBeNull();
            reservedBeforeWrite.ShouldBeTrue();
            result.Outcome!.InstanceIds.ShouldHaveSingleItem().ShouldStartWith("owned-" + settings.Value.OwnershipGeneration + "-");
        }
        finally { Directory.Delete(directory, recursive: true); }
    }

    /// <summary>The production EventStore adapter issues generation-qualified units through the same atomic scheduler boundary.</summary>
    [Fact]
    public async Task EventAdapterReservesGenerationAuthorityBeforeActualSchedulingBoundary()
    {
        CancellationToken token = TestContext.Current.CancellationToken;
        string tenant = "event-issuance-" + Guid.NewGuid().ToString("N");
        var settings = Settings();
        var inventory = Store(redis.Connection, settings);
        var capture = new IngestionWorkflowConfigurationCapture(settings,
            Options.Create(new Hexalith.Memories.Server.NaturalLanguage.NaturalLanguageDescriptionOptions()));
        var client = Substitute.For<IDaprWorkflowClient>();
        bool authorityBeforeScheduling = false;
        client.ScheduleNewWorkflowAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<IngestionInput>(), null, Arg.Any<CancellationToken>())
            .Returns(async call =>
            {
                string unit = call.ArgAt<string>(1);
                RedisValue authority = await redis.Connection.GetDatabase().StringGetAsync(UnitOwnershipAuthority.Key(tenant, unit));
                authorityBeforeScheduling = UnitOwnershipAuthority.IsComplete(authority, authority, tenant, unit);
                return unit;
            });
        var scheduler = new DaprIngestionWorkflowScheduler(client, Substitute.For<IWorkflowPayloadStore>(), capture,
            new WorkflowTraceContextCapture(), Substitute.For<IIngestionWorkflowInFlightRegistry>(), TimeProvider.System,
            Microsoft.Extensions.Logging.Abstractions.NullLogger<DaprIngestionWorkflowScheduler>.Instance, inventory);
        var adapter = new Hexalith.Memories.Server.EventStoreIntegration.EventIngestionWorkflowSchedulerAdapter(scheduler, capture);
        IngestionInput input = Input(tenant) with { SourceType = SourceType.Event, ContentBytes = [1, 2, 3] };
        string unit = await adapter.ScheduleAsync("dedup:owned-event", input, token);
        unit.ShouldBe(UnitOwnershipAuthority.Qualify("dedup:owned-event", settings.Value.OwnershipGeneration));
        authorityBeforeScheduling.ShouldBeTrue();
        await inventory.RecordCandidateAsync(tenant, input.SourceUri, unit, unit, token);
        RedisValue proof = await redis.Connection.GetDatabase().StringGetAsync(UnitOwnershipAuthority.Key(tenant, unit));
        UnitOwnershipAuthority.IsComplete(await redis.Connection.GetDatabase().StringGetAsync(SourcePublicationVisibility.KeyProvenance(tenant, unit)), proof, tenant, unit).ShouldBeTrue();
    }

    /// <summary>A configured directory issuer refuses before writing if its authority dependency is absent.</summary>
    [Fact]
    public async Task ConfiguredDirectoryIssuerRequiresAuthorityStoreBeforePayloadWrites()
    {
        CancellationToken token = TestContext.Current.CancellationToken;
        var payloads = Substitute.For<IWorkflowPayloadStore>();
        var scheduler = Substitute.For<IIngestionWorkflowScheduler>();
        var service = new DirectoryIngestionService(Settings(), scheduler, Substitute.For<Dapr.Client.DaprClient>(),
            Microsoft.Extensions.Logging.Abstractions.NullLogger<DirectoryIngestionService>.Instance, payloads);
        var error = await Should.ThrowAsync<InvalidOperationException>(() => service.IngestAsync(new DirectoryIngestionRequest {
            TenantId = "owned-configured-missing-store", CaseId = "case", DirectoryPath = Path.GetTempPath(), IngestedBy = "owned-control" }, token));
        error.Message.ShouldBe("unit-ownership-inventory-required");
        await payloads.DidNotReceiveWithAnyArgs().SaveAsync(default!, default!, default, default, default, default);
        await scheduler.DidNotReceiveWithAnyArgs().ScheduleAsync(default!, default!, default);
    }

    /// <summary>Writer and reader share one serialized limit; bounded guards refuse before creating authority.</summary>
    [Fact]
    public async Task OversizedUnicodeAuthorityAndInventoriesRefuseBeforeMutation()
    {
        CancellationToken token = TestContext.Current.CancellationToken;
        string tenant = "authority-capacity-" + Guid.NewGuid().ToString("N");
        var settings = Settings();
        settings.Value.OwnershipWriterExclusionEvidenceId = new string('漢', 512);
        var store = Store(redis.Connection, settings);
        var error = await Should.ThrowAsync<InvalidOperationException>(() => store.ReconcileUnitAsync(tenant, "unit", [], new string('漢', 512), null, token));
        error.Message.ShouldBe("unit-ownership-authority-capacity");
        string unicodeTenant = tenant + "-issued";
        string unit = UnitOwnershipAuthority.Qualify(new string('漢', 400), settings.Value.OwnershipGeneration);
        (await Should.ThrowAsync<InvalidOperationException>(() => store.AdmitAsync(unit, Input(unicodeTenant), token))).Message.ShouldBe("unit-ownership-authority-capacity");
        IDatabase db = redis.Connection.GetDatabase();
        (await db.KeyExistsAsync(UnitOwnershipAuthority.GenerationKey(settings.Value.OwnershipGeneration!))).ShouldBeFalse();
        (await db.KeyExistsAsync(UnitOwnershipAuthority.Key(tenant, "unit"))).ShouldBeFalse();
        (await store.ReadAsync(unicodeTenant, Input(unicodeTenant).SourceUri, null, 100, token)).Entries.ShouldBeEmpty();
        (await Should.ThrowAsync<InvalidOperationException>(() => store.ReconcileUnitAsync(tenant, "unit",
            Enumerable.Repeat("unused", SourcePublicationVisibility.KeyCapacity + 1).ToArray(), "bounded-proof", null, token)))
            .Message.ShouldBe("source-publication-key-capacity");
        await db.SetAddAsync(SourcePublicationVisibility.Keys(tenant, "unit"),
            Enumerable.Range(0, SourcePublicationVisibility.KeyCapacity + 1).Select(index => (RedisValue)("retained-" + index)).ToArray());
        var retained = await Should.ThrowAsync<RedisServerException>(() => Store(redis.Connection, Settings()).ReconcileUnitAsync(tenant, "unit", [], "bounded-proof", null, token));
        retained.Message.ShouldContain("source-publication-key-capacity");
        (await db.KeyExistsAsync(UnitOwnershipAuthority.Key(tenant, "unit"))).ShouldBeFalse();
        (await db.SetLengthAsync(SourcePublicationVisibility.Keys(tenant, "unit"))).ShouldBe(SourcePublicationVisibility.KeyCapacity + 1);
    }

    /// <summary>Concurrent tenants cannot reserve one global generation under competing custody identities.</summary>
    /// <param name="reconciliation">Whether the second contender is historical reconciliation rather than issuance.</param>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task CompetingTenantIssuanceAndReconciliationHaveOneGenerationCustodyWinner(bool reconciliation)
    {
        CancellationToken token = TestContext.Current.CancellationToken;
        string generation = Guid.NewGuid().ToString("N");
        string firstTenant = "custody-a-" + Guid.NewGuid().ToString("N"), secondTenant = "custody-b-" + Guid.NewGuid().ToString("N");
        var firstSettings = Options.Create(new IngestionSettings { OwnershipGeneration = generation, OwnershipWriterExclusionEvidenceId = "custody-a" });
        var secondSettings = Options.Create(new IngestionSettings { OwnershipGeneration = generation, OwnershipWriterExclusionEvidenceId = "custody-b" });
        using IConnectionMultiplexer secondConnection = await ConnectionMultiplexer.ConnectAsync(redis.ConnectionString);
        var first = Store(redis.Connection, firstSettings);
        var second = Store(secondConnection, secondSettings);
        string firstUnit = UnitOwnershipAuthority.Qualify("first", generation), secondUnit = UnitOwnershipAuthority.Qualify("second", generation);
        string historicalKey = IndexSchemaDefinitions.BuildSemanticChunkKey(secondTenant, "historical", 7);
        IDatabase db = redis.Connection.GetDatabase();
        await db.HashSetAsync(historicalKey, Hash(secondTenant, "historical"));
        HashEntry[] original = await db.HashGetAllAsync(historicalKey);
        async Task<bool> Attempt(Func<Task> operation)
        {
            try { await operation(); return true; }
            catch (RedisServerException failure) when (failure.Message.Contains("unit-ownership-generation-conflict", StringComparison.Ordinal)) { return false; }
        }
        bool[] winners = await Task.WhenAll(Attempt(() => first.AdmitAsync(firstUnit, Input(firstTenant), token)),
            Attempt(() => reconciliation ? second.ReconcileUnitAsync(secondTenant, "historical", [historicalKey], "historical-evidence", null, token)
                : second.AdmitAsync(secondUnit, Input(secondTenant), token)));
        winners.Count(winner => winner).ShouldBe(1);
        (await db.StringGetAsync(UnitOwnershipAuthority.GenerationKey(generation))).ToString().ShouldBe(winners[0] ? "custody-a" : "custody-b");
        string losingTenant = winners[0] ? secondTenant : firstTenant;
        string losingUnit = winners[0] ? secondUnit : firstUnit;
        SourceInventoryStore losingStore = winners[0] ? second : first;
        await Should.ThrowAsync<RedisServerException>(() => losingStore.AdmitAsync(losingUnit, Input(losingTenant), token));
        (await losingStore.ReadAsync(losingTenant, Input(losingTenant).SourceUri, null, 100, token)).Entries.ShouldBeEmpty();
        (await db.KeyExistsAsync(UnitOwnershipAuthority.Key(losingTenant, losingUnit))).ShouldBeFalse();
        AssertBytes(await db.HashGetAllAsync(historicalKey), original);
    }

    /// <summary>CaseService's returned configured annotation identity becomes the exact durable unit issuer identity.</summary>
    [Fact]
    public async Task CaseServiceReturnedAnnotationIdentityBindsActualRedisAuthority()
    {
        CancellationToken token = TestContext.Current.CancellationToken;
        string tenant = "case-entry-" + Guid.NewGuid().ToString("N");
        var settings = Settings();
        IDatabase db = redis.Connection.GetDatabase();
        await db.HashSetAsync(IndexSchemaDefinitions.BuildSyntacticKey(tenant, "target"), [.. Hash(tenant, "target"), new("id", "target")]);
        await db.HashSetAsync(IndexSchemaDefinitions.BuildSemanticKey(tenant, "target"), Hash(tenant, "target"));
        await new NFalkorDB.FalkorDB(graph.Connection.GetDatabase()).SelectGraph(tenant).QueryAsync("CREATE (:MemoryUnit {id:'target'})");
        var projectionScheduler = Substitute.For<Hexalith.Memories.Server.EventStoreIntegration.ICaseProjectionWorkflowScheduler>();
        AnnotationProjectionInput? scheduled = null;
        projectionScheduler.ScheduleAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<object>(), Arg.Any<CancellationToken>())
            .Returns(call => { scheduled = (AnnotationProjectionInput)call.ArgAt<object>(2); return call.ArgAt<string>(1); });
        var capture = new IngestionWorkflowConfigurationCapture(settings, Options.Create(new Hexalith.Memories.Server.NaturalLanguage.NaturalLanguageDescriptionOptions()));
        var service = new Hexalith.Memories.Server.Cases.CaseService(redis.Connection, graph.Connection, new GraphQueryBuilder(),
            new Hexalith.Memories.Server.Cases.CaseActivityService(redis.Connection, Microsoft.Extensions.Logging.Abstractions.NullLogger<Hexalith.Memories.Server.Cases.CaseActivityService>.Instance),
            null!, Substitute.For<Dapr.Actors.Client.IActorProxyFactory>(),
            Microsoft.Extensions.Logging.Abstractions.NullLogger<Hexalith.Memories.Server.Cases.CaseService>.Instance,
            projectionWorkflowScheduler: projectionScheduler, workflowConfigurationCapture: capture);
        var result = (await service.CreateAnnotationAsync(new CreateAnnotationInput(tenant, "case", "target", "owned annotation", "owned-control", null), token)).ShouldNotBeNull();
        string returnedId = result.Annotation.Id;
        returnedId.ShouldStartWith("owned-" + settings.Value.OwnershipGeneration + "-");
        scheduled.ShouldNotBeNull().AnnotationMemoryUnitId.ShouldBe(returnedId);
        IngestionInput child = Input(tenant) with { SourceUri = scheduled!.SourceUri, ContentBytes = System.Text.Encoding.UTF8.GetBytes(scheduled.Content) };
        await new AdmitSourceInventoryActivity(Store(redis.Connection, settings), Substitute.For<IDaprWorkflowClient>()).RunAsync(null!, new(returnedId, child));
        using IConnectionMultiplexer recreated = await ConnectionMultiplexer.ConnectAsync(redis.ConnectionString);
        RedisValue authority = await recreated.GetDatabase().StringGetAsync(UnitOwnershipAuthority.Key(tenant, returnedId));
        UnitOwnershipAuthority.IsComplete(authority, authority, tenant, returnedId).ShouldBeTrue();
        (await Store(recreated, settings).ReadAsync(tenant, child.SourceUri, null, 100, token)).Entries.ShouldHaveSingleItem().WorkflowInstanceId.ShouldBe(returnedId);
    }

    /// <summary>Preflight failure before admission creates no inventory, issuance or tenant ownership mutation.</summary>
    [Fact]
    public async Task UnadmittedLookupFailureCreatesNoDurableInventory()
    {
        CancellationToken token = TestContext.Current.CancellationToken;
        string tenant = "unadmitted-lookup-" + Guid.NewGuid().ToString("N");
        var settings = Settings();
        var capture = new IngestionWorkflowConfigurationCapture(settings, Options.Create(new Hexalith.Memories.Server.NaturalLanguage.NaturalLanguageDescriptionOptions()));
        string unit = capture.NewUnitId();
        var client = Substitute.For<IDaprWorkflowClient>();
        client.GetWorkflowStateAsync(unit, true, Arg.Any<CancellationToken>()).Returns(Task.FromException<WorkflowState>(new IOException("owned initial lookup failure")));
        var scheduler = new DaprIngestionWorkflowScheduler(client, Substitute.For<IWorkflowPayloadStore>(), capture,
            new WorkflowTraceContextCapture(), Substitute.For<IIngestionWorkflowInFlightRegistry>(), TimeProvider.System,
            Microsoft.Extensions.Logging.Abstractions.NullLogger<DaprIngestionWorkflowScheduler>.Instance, Store(redis.Connection, settings));
        IngestionInput input = Input(tenant);
        await Should.ThrowAsync<IOException>(() => scheduler.ScheduleAsync(unit, input, token));
        using IConnectionMultiplexer recreated = await ConnectionMultiplexer.ConnectAsync(redis.ConnectionString);
        IDatabase db = recreated.GetDatabase();
        (await db.KeyExistsAsync(SourceInventoryStore.BuildKey(tenant, input.SourceUri))).ShouldBeFalse();
        (await db.KeyExistsAsync(TenantIngestionOwnership.Key(tenant))).ShouldBeFalse();
        (await db.KeyExistsAsync(UnitOwnershipAuthority.Key(tenant, unit))).ShouldBeFalse();
        (await db.KeyExistsAsync(UnitOwnershipAuthority.GenerationKey(settings.Value.OwnershipGeneration!))).ShouldBeFalse();
        await client.DidNotReceiveWithAnyArgs().ScheduleNewWorkflowAsync(default!, default!, default!, default, default);
    }

    /// <summary>A rejected or preflight-failed competing request cannot settle another body's Preparing admission.</summary>
    /// <param name="mismatch">The admission binding that differs from the retained producer.</param>
    /// <param name="lookupFailure">Whether failure precedes admission rather than being an admission rejection.</param>
    [Theory]
    [InlineData("body", false)]
    [InlineData("body", true)]
    [InlineData("same", true)]
    [InlineData("token", true)]
    [InlineData("case", true)]
    [InlineData("refresh", true)]
    [InlineData("body-with-reference", true)]
    public async Task CompetingRequestCannotSettleAnotherProducersPreparingAdmission(string mismatch, bool lookupFailure)
    {
        CancellationToken token = TestContext.Current.CancellationToken;
        string tenant = "settlement-binding-" + Guid.NewGuid().ToString("N");
        var settings = Settings();
        var capture = new IngestionWorkflowConfigurationCapture(settings, Options.Create(new Hexalith.Memories.Server.NaturalLanguage.NaturalLanguageDescriptionOptions()));
        string unit = capture.NewUnitId();
        var inventory = Store(redis.Connection, settings);
        IngestionInput first = Input(tenant) with { SourceType = SourceType.File, ContentBytes = [1, 2, 3], IdempotencyToken = "producer-a" };
        if (mismatch == "refresh") { first = first with { RefreshExistingSource = true, SourceRevisionNumber = 7, RefreshIdentityDigest = "digest-a" }; }
        await inventory.AdmitAsync(unit, first, token);
        IDatabase db = redis.Connection.GetDatabase();
        RedisValue original = await db.StringGetAsync(SourceInventoryStore.BuildKey(tenant, first.SourceUri));
        IngestionInput competing = mismatch switch
        {
            "body" => first with { ContentBytes = [9, 8, 7] },
            "token" => first with { IdempotencyToken = "producer-b" },
            "case" => first with { CaseId = "other-case" },
            "refresh" => first with { RefreshIdentityDigest = "digest-b" },
            "body-with-reference" => first with { ContentBytes = [9, 8, 7], PayloadReference = IngestionPayloadClaimCheck.GetSourceReference(unit, first) },
            _ => first,
        };
        var client = Substitute.For<IDaprWorkflowClient>();
        if (lookupFailure) { client.GetWorkflowStateAsync(unit, true, Arg.Any<CancellationToken>()).Returns(Task.FromException<WorkflowState>(new IOException("owned preflight failure"))); }
        var scheduler = new DaprIngestionWorkflowScheduler(client, Substitute.For<IWorkflowPayloadStore>(), capture,
            new WorkflowTraceContextCapture(), Substitute.For<IIngestionWorkflowInFlightRegistry>(), TimeProvider.System,
            Microsoft.Extensions.Logging.Abstractions.NullLogger<DaprIngestionWorkflowScheduler>.Instance, inventory);
        if (lookupFailure) { await Should.ThrowAsync<IOException>(() => scheduler.ScheduleAsync(unit, competing, token)); }
        else { (await Should.ThrowAsync<InvalidOperationException>(() => scheduler.ScheduleAsync(unit, competing, token))).Message.ShouldBe("source-inventory-admission-mismatch"); }
        using IConnectionMultiplexer recreated = await ConnectionMultiplexer.ConnectAsync(redis.ConnectionString);
        (await recreated.GetDatabase().StringGetAsync(SourceInventoryStore.BuildKey(tenant, first.SourceUri))).ShouldBe(original);
        var recovered = Store(recreated, settings);
        if (mismatch != "same") { await recovered.SetUnscheduledIfPreparingAsync(unit, competing, token); }
        (await recreated.GetDatabase().StringGetAsync(SourceInventoryStore.BuildKey(tenant, first.SourceUri))).ShouldBe(original);
        (await recovered.ReadAsync(tenant, first.SourceUri, null, 100, token)).Entries.ShouldHaveSingleItem().SchedulingState.ShouldBe("Preparing");
        await recovered.SetSchedulingAsync(tenant, first.SourceUri, unit, "Scheduling", token);
        (await recovered.ReadAsync(tenant, first.SourceUri, null, 100, token)).Entries.ShouldHaveSingleItem().SchedulingState.ShouldBe("Scheduling");
        await client.DidNotReceiveWithAnyArgs().ScheduleNewWorkflowAsync(default!, default!, default!, default, default);
    }

    private SourceInventoryStore Store(IConnectionMultiplexer connection, IOptions<IngestionSettings>? settings = null)
        => new(connection, new QueryCursorCodec(new EphemeralDataProtectionProvider(), "OwnedAuthorityControls"), settings);

    private static IOptions<IngestionSettings> Settings() => Options.Create(new IngestionSettings {
        OwnershipGeneration = Guid.NewGuid().ToString("N"), OwnershipWriterExclusionEvidenceId = "owned-disposable-writers-excluded" });

    private static IngestionInput Input(string tenant) => new() { TenantId = tenant, CaseId = "case", SourceUri = "https://synthetic.invalid/source-a",
        SourceType = SourceType.Annotation, ContentType = "text/plain", IngestedBy = "owned-fixture" };

    private static HashEntry[] Hash(string tenant, string unit) => [new("tenantId", tenant), new("memoryUnitId", unit),
        new("caseId", "case"), new("sourceUri", "https://synthetic.invalid/source-a"), new("text", new byte[] { 0, 255, 128, 13, 10 }), new("vector", new byte[] { 0, 128, 255, 0 })];

    private static void AssertBytes(HashEntry[] actual, HashEntry[] expected)
        => actual.OrderBy(item => item.Name.ToString(), StringComparer.Ordinal).ShouldBe(expected.OrderBy(item => item.Name.ToString(), StringComparer.Ordinal));

    private Task<bool> DeleteAsync(IConnectionMultiplexer connection, string tenant, string unit)
        => new DeleteMemoryUnitProjectionActivity(connection, graph.Connection, new GraphQueryBuilder(),
            workflowClient: Substitute.For<IDaprWorkflowClient>()).RunAsync(null!, new(tenant, "case", unit, []));
}
