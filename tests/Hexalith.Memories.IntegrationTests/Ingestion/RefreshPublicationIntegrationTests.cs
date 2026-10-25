namespace Hexalith.Memories.IntegrationTests.Ingestion;

using System.Text;
using System.Runtime.InteropServices;
using Query = NRedisStack.Search.Query;
using Dapr.Workflow;
using Hexalith.EventStore.Client.Queries;
using Hexalith.Memories.Contracts.V1;
using Hexalith.Memories.IntegrationTests.Fixtures;
using Hexalith.Memories.Server.Activities.Cases;
using Hexalith.Memories.Server.Activities.Indexing;
using Hexalith.Memories.Server.Activities.Ingestion;
using Hexalith.Memories.Server.DerivedStores;
using Hexalith.Memories.Server.Graph;
using Hexalith.Memories.Server.Infrastructure;
using Hexalith.Memories.Server.Ingestion;
using Hexalith.Memories.Server.Search;
using Hexalith.Memories.TestHelpers.Factories;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Logging.Abstractions;
using NRedisStack.RedisStackCommands;
using NSubstitute;
using Shouldly;
using StackExchange.Redis;

/// <summary>Real Redis/FalkorDB adapter controls for current content, both revision orders, retry, restart and physical cleanup.</summary>
[Collection("RedisStack")]
[Trait("Category", "Integration")]
public sealed class RefreshPublicationIntegrationTests(RedisStackFixture redis, FalkorDbFixture graph) : IClassFixture<FalkorDbFixture>
{
    [Fact]
    public async Task InitialPublicationCannotRetireARealUnitNamedNone()
    {
        CancellationToken token = TestContext.Current.CancellationToken;
        string tenant = "absent-prior-" + Guid.NewGuid().ToString("N");
        const string source = "urn:synthetic:new-source";
        var inventory = new SourceInventoryStore(redis.Connection, new QueryCursorCodec(new EphemeralDataProtectionProvider(), "AbsentPriorTests"));
        var publication = new RefreshPublicationStore(redis.Connection, inventory, graph.Connection);
        await IndexAsync(tenant, "__none", "urn:synthetic:unrelated", Encoding.UTF8.GetBytes("unrelated"));
        await SourcePublicationVisibility.WriteAsync(redis.Connection.GetDatabase(), tenant, "__none", tenant + ":mu:__none",
            [new HashEntry("content", "unrelated")], false, "captured-case", "urn:synthetic:unrelated");
        string before = (await redis.Connection.GetDatabase().HashGetAsync(tenant + ":mu:__none", SourcePublicationVisibility.Field)).ToString();
        var input = Input(tenant, "captured-case", source, "new content", "new-token", 1, null);
        input = input with { RefreshIdentityDigest = await publication.PrepareAsync(input, token) };
        await IndexAsync(tenant, "candidate", source, input.ContentBytes!);
        (await publication.PublishAsync(input, "candidate", token)).ShouldBeTrue();
        (await redis.Connection.GetDatabase().HashGetAsync(tenant + ":mu:__none", SourcePublicationVisibility.Field)).ToString().ShouldBe(before);
    }

    [Fact]
    public async Task PermanentSourceFenceRejectsDistinctRefreshTokensWithoutRetainingBindings()
    {
        CancellationToken token = TestContext.Current.CancellationToken;
        string tenant = "closed-bindings-" + Guid.NewGuid().ToString("N");
        const string source = "urn:synthetic:closed-source";
        var inventory = new SourceInventoryStore(redis.Connection, new QueryCursorCodec(new EphemeralDataProtectionProvider(), "ClosedBindingTests"));
        await inventory.FenceAsync(tenant, source, true, token);
        long before = await redis.Connection.GetDatabase().SetLengthAsync(TenantIngestionOwnership.Key(tenant));
        var publication = new RefreshPublicationStore(redis.Connection, inventory, graph.Connection);
        for (int index = 0; index < 20; index++)
        {
            await Should.ThrowAsync<InvalidOperationException>(() => publication.PrepareAsync(Input(tenant, "captured-case", source, "bytes", "token-" + index, index, null), token));
        }
        (await redis.Connection.GetDatabase().SetLengthAsync(TenantIngestionOwnership.Key(tenant))).ShouldBe(before);
    }

    [Fact]
    public async Task UntrackedMaintenanceKeysJoinModernProvenanceBeforeRefreshAndNativeTopK()
    {
        CancellationToken token = TestContext.Current.CancellationToken;
        string tenant = "maintenance-" + Guid.NewGuid().ToString("N");
        const string source = "urn:synthetic:maintenance", caseId = "captured-case";
        var inventory = new SourceInventoryStore(redis.Connection, new QueryCursorCodec(new EphemeralDataProtectionProvider(), "MaintenanceTests"));
        var publication = new RefreshPublicationStore(redis.Connection, inventory, graph.Connection);
        var first = Input(tenant, caseId, source, "initial modern content", "initial", 1, null);
        first = first with { RefreshIdentityDigest = await publication.PrepareAsync(first, token) };
        await IndexAsync(tenant, "modern", source, first.ContentBytes!);
        (await publication.PublishAsync(first, "modern", token)).ShouldBeTrue();
        IDatabase db = redis.Connection.GetDatabase();
        db.FT().Create(IndexSchemaDefinitions.GetNaturalLanguageSemanticIndexName(tenant),
            IndexSchemaDefinitions.CreateNaturalLanguageSemanticParams(tenant), IndexSchemaDefinitions.CreateNaturalLanguageSemanticSchema(3));
        var context = Substitute.For<WorkflowActivityContext>();
        var nl = new IndexNaturalLanguageSemanticActivity(redis.Connection, NullLogger<IndexNaturalLanguageSemanticActivity>.Instance);
        await nl.RunAsync(context, new NaturalLanguageIndexInput { TenantId = tenant, CaseId = caseId, MemoryUnitId = "modern",
            EmbeddingProvider = "google", EmbeddingModel = "synthetic", EmbeddingDimensions = 3, EmbeddingVector = [1, 0, 0],
            NaturalLanguageDescription = "late NL retry", ConfidenceSource = ConfidenceSource.Constant });
        string lateNl = IndexSchemaDefinitions.BuildNaturalLanguageSemanticKey(tenant, "modern");
        (await db.SetContainsAsync(SourcePublicationVisibility.Keys(tenant, "modern"), lateNl)).ShouldBeTrue();
        (await db.HashGetAsync(lateNl, SourcePublicationVisibility.Field)).ToString().ShouldBe("1");
        await new IndexSemanticChunksActivity(redis.Connection, NullLogger<IndexSemanticChunksActivity>.Instance).RunAsync(context,
            new SemanticChunkIndexInput { TenantId = tenant, CaseId = caseId, MemoryUnitId = "modern", EmbeddingProvider = "google",
                EmbeddingModel = "synthetic", EmbeddingDimensions = 3, Chunks = [new ChunkEmbeddingResult { Sequence = 7, Text = "late restore chunk",
                    StartOffset = 0, EndOffset = 18, EstimatedTokens = 4, Vector = [1, 0, 0] }] });
        string lateChunk = IndexSchemaDefinitions.BuildSemanticChunkKey(tenant, "modern", 7);
        (await db.SetContainsAsync(SourcePublicationVisibility.Keys(tenant, "modern"), lateChunk)).ShouldBeTrue();
        var next = Input(tenant, caseId, source, "successor modern content", "next", 2, "modern");
        next = next with { RefreshIdentityDigest = await publication.PrepareAsync(next, token) };
        await IndexAsync(tenant, "successor", source, next.ContentBytes!);
        await nl.RunAsync(context, new NaturalLanguageIndexInput { TenantId = tenant, CaseId = caseId, MemoryUnitId = "successor",
            EmbeddingProvider = "google", EmbeddingModel = "synthetic", EmbeddingDimensions = 3, EmbeddingVector = [0.8f, 0.2f, 0],
            NaturalLanguageDescription = "current NL", ConfidenceSource = ConfidenceSource.Constant });
        (await publication.PublishAsync(next, "successor", token)).ShouldBeTrue();
        (await db.HashGetAsync(lateNl, SourcePublicationVisibility.Field)).ToString().ShouldBe("0");
        (await db.HashGetAsync(lateChunk, SourcePublicationVisibility.Field)).ToString().ShouldBe("0");
        var embedding = Substitute.For<EmbeddingClient>(Substitute.For<IHttpClientFactory>(), Substitute.For<Dapr.Client.DaprClient>(),
            new Microsoft.Extensions.Configuration.ConfigurationBuilder().Build(), Substitute.For<Microsoft.Extensions.Hosting.IHostEnvironment>());
        embedding.GenerateAsync(Arg.Any<string>(), tenant, Arg.Any<TenantEmbeddingConfig>(), Arg.Any<CancellationToken>()).Returns(Task.FromResult(new float[] { 1, 0, 0 }));
        var search = new NaturalLanguageSemanticSearchService(redis.Connection, embedding, NullLogger<NaturalLanguageSemanticSearchService>.Instance);
        (await search.SearchAsync(new SearchQuery { TenantId = tenant, CaseId = caseId, Query = "late NL", MaxResults = 1 },
            new TenantEmbeddingConfig("google", "synthetic", 3, 60, "unused"), token)).Results.ShouldHaveSingleItem().MemoryUnitId.ShouldBe("successor");
        using IConnectionMultiplexer restarted = await ConnectionMultiplexer.ConnectAsync(redis.ConnectionString);
        (await restarted.GetDatabase().SetContainsAsync(SourcePublicationVisibility.Keys(tenant, "modern"), lateNl)).ShouldBeTrue();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task BothCompletionOrdersPreserveCurrentContentAndRetryableNewerRevision(bool newerFirst)
    {
        CancellationToken token = TestContext.Current.CancellationToken;
        string tenant = "refresh-" + Guid.NewGuid().ToString("N");
        const string source = "urn:synthetic:refresh";
        const string caseId = "captured-case";
        var codec = new QueryCursorCodec(new EphemeralDataProtectionProvider(), "RefreshTests");
        var inventory = new SourceInventoryStore(redis.Connection, codec);
        var publication = new RefreshPublicationStore(redis.Connection, inventory, graph.Connection);
        WorkflowActivityContext context = Substitute.For<WorkflowActivityContext>();
        var old = Input(tenant, caseId, source, "oldblue shared shared shared", "initial", 0, null);
        await inventory.AdmitAsync("old", old, token);
        await IndexAsync(tenant, "old", source, old.ContentBytes!);
        await new SaveDedupKeyActivity(redis.Connection).RunAsync(context,
            new(DedupKeyBuilder.BuildKey(tenant, caseId, source), "old"));
        await inventory.CompleteAsync(tenant, source, "old", caseId, "old", token);
        await IndexAsync(tenant, "unrelated", "urn:synthetic:other-source", Encoding.UTF8.GetBytes("unrelated synthetic control"));

        // These are the unchanged pre-feature schemas, not new schemas populated
        // with legacy-shaped rows. Refresh installs its optional field idempotently.
        IndexSchemaDefinitions.GetAttributeIdentifiers(await redis.Connection.GetDatabase().ExecuteAsync("FT.INFO", IndexSchemaDefinitions.GetSyntacticIndexName(tenant)))
            .ShouldNotContain(SourcePublicationVisibility.Field);
        IndexSchemaDefinitions.GetAttributeIdentifiers(await redis.Connection.GetDatabase().ExecuteAsync("FT.INFO", IndexSchemaDefinitions.GetSemanticIndexName(tenant)))
            .ShouldNotContain(SourcePublicationVisibility.Field);

        (string obsoleteEdge, IDictionary<string, object> obsoleteEdgeParameters) = new GraphQueryBuilder().BuildMergeEdge(
            "old", "unrelated", EdgeType.References, EdgeTypeDefaults.References, EdgeOrigin.Explicit);
        await new NFalkorDB.FalkorDB(graph.Connection.GetDatabase()).SelectGraph(tenant).QueryAsync(obsoleteEdge, obsoleteEdgeParameters);
        var legacyIndex = new IndexSyntacticActivity(redis.Connection, NullLogger<IndexSyntacticActivity>.Instance);
        for (int index = 0; index < 1005; index++)
        {
            IndexInput legacy = IndexInputFactory.Create(memoryUnitId: "legacy-" + index, tenantId: tenant, caseId: caseId,
                sourceUri: "urn:synthetic:legacy:" + index, content: "legacygreen ordinary unchanged");
            (await legacyIndex.RunAsync(context, legacy)).Backend.ShouldBe("syntactic");
        }
        var legacySearch = new SyntacticSearchService(redis.Connection, NullLogger<SyntacticSearchService>.Instance);
        SearchResult beforeUpgrade = await legacySearch.SearchAsync(new SearchQuery { TenantId = tenant, CaseId = caseId, Query = "legacygreen", MaxResults = 1, Offset = 1001 });
        beforeUpgrade.TotalCount.ShouldBe(1005);
        beforeUpgrade.Results.ShouldHaveSingleItem();

        // Model interruption after the old implementation registered only one legacy key.
        // A nonempty set without complete provenance must still discover the exact unit's chunks.
        await redis.Connection.GetDatabase().SetAddAsync(SourcePublicationVisibility.Keys(tenant, "old"),
            IndexSchemaDefinitions.BuildSyntacticKey(tenant, "old"));
        (await redis.Connection.GetDatabase().KeyExistsAsync(SourcePublicationVisibility.KeyProvenance(tenant, "old"))).ShouldBeFalse();
        IngestionInput older = Input(tenant, caseId, source, "olderred shared shared shared", "revision-one", 1, "old");
        IngestionInput newer = Input(tenant, caseId, source, "newyellow shared " + new string('x', 200), "revision-two", 2, "old");
        older = older with { RefreshIdentityDigest = await publication.PrepareAsync(older, token) };
        (await redis.Connection.GetDatabase().StringGetAsync(SourcePublicationVisibility.KeyProvenance(tenant, "old"))).ToString().ShouldBe("legacy-complete");
        (await redis.Connection.GetDatabase().SetContainsAsync(SourcePublicationVisibility.Keys(tenant, "old"),
            IndexSchemaDefinitions.BuildSemanticChunkKey(tenant, "old", 0))).ShouldBeTrue();
        newer = newer with { RefreshIdentityDigest = await publication.PrepareAsync(newer, token) };
        await inventory.AdmitAsync("older", older, token);
        await inventory.AdmitAsync("newer", newer, token);
        await IndexAsync(tenant, "older", source, older.ContentBytes!);
        await IndexAsync(tenant, "newer", source, newer.ContentBytes!);
        var publish = new PublishRefreshActivity(publication, inventory);
        var delete = new DeleteMemoryUnitProjectionActivity(redis.Connection, graph.Connection, new GraphQueryBuilder(), inventory);
        string current;
        if (newerFirst)
        {
            (await publish.RunAsync(context, new(newer, "newer"))).ShouldBeTrue();
            (await publish.RunAsync(context, new(older, "older"))).ShouldBeFalse();
            await delete.RunAsync(context, new(tenant, caseId, "older", []));
            current = "newer";
        }
        else
        {
            (await publish.RunAsync(context, new(older, "older"))).ShouldBeTrue();
            (await publish.RunAsync(context, new(newer, "newer"))).ShouldBeFalse();
            await delete.RunAsync(context, new(tenant, caseId, "newer", []));
            // The same logical revision token can rebase after a definitive compensated
            // CAS loss. An older revision is never allowed to rebase over the newer one.
            newer = newer with { ExpectedMemoryUnitId = "older" };
            newer = newer with { RefreshIdentityDigest = await publication.PrepareAsync(newer, token) };
            await inventory.AdmitAsync("newer-retry", newer, token);
            await IndexAsync(tenant, "newer-retry", source, newer.ContentBytes!);
            (await publish.RunAsync(context, new(newer, "newer-retry"))).ShouldBeTrue();
            current = "newer-retry";
        }
        await Should.ThrowAsync<InvalidOperationException>(() => publication.PrepareAsync(older with { ExpectedMemoryUnitId = current }, token));
        newer = newer with { RefreshIdentityDigest = await publication.PrepareAsync(newer, token) };
        (await publication.PublishAsync(newer, current, token)).ShouldBeTrue();
        (await redis.Connection.GetDatabase().StringGetAsync(DedupKeyBuilder.BuildKey(tenant, caseId, source))).ToString().ShouldBe(current);
        (await redis.Connection.GetDatabase().StringGetAsync(DedupKeyBuilder.BuildTokenKey(tenant, caseId, "revision-two"))).ToString().ShouldBe(current);
        await Should.ThrowAsync<InvalidOperationException>(() => publication.PrepareAsync(newer with { ContentBytes = Encoding.UTF8.GetBytes("changed retry body") }, token));

        // Use actual producer indexing and search, with obsolete matches ahead of a
        // small requested TopK. Filtering precedes public pagination and exact counts.
        for (int index = 0; index < 12; index++)
        {
            string id = "superseded-" + index;
            await inventory.AdmitAsync(id, old, token);
            await IndexAsync(tenant, id, source, Encoding.UTF8.GetBytes("shared shared shared supersededblue"));
            await inventory.CompleteAsync(tenant, source, id, caseId, id, token);
        }
        // An untracked replay/restore cannot import a stale visibility flag or
        // reveal a superseded canonical output under the current source pointer.
        await SourcePublicationVisibility.WriteAsync(redis.Connection.GetDatabase(), tenant, "old",
            IndexSchemaDefinitions.BuildSyntacticKey(tenant, "old"),
            [new("content", "shared stale replay"), new(SourcePublicationVisibility.Field, "1")], false, caseId, source);
        (await redis.Connection.GetDatabase().HashGetAsync(IndexSchemaDefinitions.BuildSyntacticKey(tenant, "old"), SourcePublicationVisibility.Field))
            .ToString().ShouldBe("0");

        string[] sourceUnits = ["old", "older", "newer", "newer-retry", .. Enumerable.Range(0, 12).Select(index => "superseded-" + index)];
        string vectorPredicate = "@memoryUnitId:{" + string.Join("|", sourceUnits.Select(RediSearchQueryEscaper.EscapeTag)) + "}=>[KNN 1 @embedding $query_vec AS __vector_score]";
        string vectorQuery = await SourcePublicationVisibility.FilterQueryAsync(redis.Connection.GetDatabase(), tenant, vectorPredicate,
            IndexSchemaDefinitions.GetSemanticIndexName(tenant));
        float[] queryVector = [0.1f, 0.2f, 0.3f];
        NRedisStack.Search.SearchResult vectorResults = await redis.Connection.GetDatabase().FT().SearchAsync(IndexSchemaDefinitions.GetSemanticIndexName(tenant),
            new Query(vectorQuery).AddParam("query_vec", MemoryMarshal.AsBytes(queryVector.AsSpan()).ToArray()).Dialect(2).Limit(0, 1));
        vectorResults.Documents.ShouldHaveSingleItem()["memoryUnitId"].ToString().ShouldBe(current);

        // Exercise the actual semantic caller: a direct helper invocation cannot qualify native TopK integration.
        float[] fartherCurrent = [0.3f, 0.2f, 0.1f];
        byte[] fartherBytes = MemoryMarshal.AsBytes(fartherCurrent.AsSpan()).ToArray();
        await redis.Connection.GetDatabase().HashSetAsync(IndexSchemaDefinitions.BuildSemanticKey(tenant, current), "embedding", fartherBytes);
        await redis.Connection.GetDatabase().HashSetAsync(IndexSchemaDefinitions.BuildSemanticChunkKey(tenant, current, 0), "embedding", fartherBytes);
        EmbeddingClient embeddingClient = Substitute.For<EmbeddingClient>(Substitute.For<IHttpClientFactory>(), Substitute.For<Dapr.Client.DaprClient>(),
            new Microsoft.Extensions.Configuration.ConfigurationBuilder().Build(), Substitute.For<Microsoft.Extensions.Hosting.IHostEnvironment>());
        embeddingClient.GenerateAsync("semantic publication control", tenant, Arg.Any<TenantEmbeddingConfig>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(queryVector));
        var semantic = new SemanticSearchService(redis.Connection, embeddingClient, NullLogger<SemanticSearchService>.Instance);
        SearchResult semanticCurrent = await semantic.SearchAsync(new SearchQuery { TenantId = tenant, CaseId = caseId,
            Query = "semantic publication control", MaxResults = 1 }, new TenantEmbeddingConfig("google", "synthetic", 3, 60, "unused"),
            sourceUnits.Select(unit => (RedisKey)IndexSchemaDefinitions.BuildSemanticKey(tenant, unit)).ToArray(), token);
        semanticCurrent.Results.ShouldHaveSingleItem().MemoryUnitId.ShouldBe(current);

        // Exercise the NL service's real native TopK. Retired candidates are deliberately closer.
        IDatabase nlDatabase = redis.Connection.GetDatabase();
        nlDatabase.FT().Create(IndexSchemaDefinitions.GetNaturalLanguageSemanticIndexName(tenant),
            IndexSchemaDefinitions.CreateNaturalLanguageSemanticParams(tenant), IndexSchemaDefinitions.CreateNaturalLanguageSemanticSchema(3));
        await SourcePublicationVisibility.EnsureSchemaAsync(nlDatabase, [IndexSchemaDefinitions.GetNaturalLanguageSemanticIndexName(tenant)]);
        foreach (string unitId in new[] { "old", current })
        {
            await new IndexNaturalLanguageSemanticActivity(redis.Connection, NullLogger<IndexNaturalLanguageSemanticActivity>.Instance)
                .RunAsync(context, new NaturalLanguageIndexInput
                {
                    MemoryUnitId = unitId, TenantId = tenant, CaseId = caseId, EmbeddingProvider = "google",
                    EmbeddingModel = "synthetic", EmbeddingDimensions = 3, EmbeddingVector = unitId == current ? fartherCurrent : queryVector,
                    NaturalLanguageDescription = "synthetic business event", ConfidenceSource = ConfidenceSource.Constant,
                });
        }
        (await nlDatabase.HashGetAsync(IndexSchemaDefinitions.BuildNaturalLanguageSemanticKey(tenant, "old"), SourcePublicationVisibility.Field)).ToString().ShouldBe("0");
        (await nlDatabase.HashGetAsync(IndexSchemaDefinitions.BuildNaturalLanguageSemanticKey(tenant, current), SourcePublicationVisibility.Field)).ToString().ShouldBe("1");
        embeddingClient.GenerateAsync("NL publication control", tenant, Arg.Any<TenantEmbeddingConfig>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(queryVector));
        var nlService = new NaturalLanguageSemanticSearchService(redis.Connection, embeddingClient, NullLogger<NaturalLanguageSemanticSearchService>.Instance);
        SearchResult nlCurrent = await nlService.SearchAsync(new SearchQuery { TenantId = tenant, CaseId = caseId,
            Query = "NL publication control", MaxResults = 1 }, new TenantEmbeddingConfig("google", "synthetic", 3, 60, "unused"), token);
        nlCurrent.Results.ShouldHaveSingleItem().MemoryUnitId.ShouldBe(current);

        var search = new SyntacticSearchService(redis.Connection, NullLogger<SyntacticSearchService>.Instance);
        SearchResult candidates = await search.SearchAsync(new SearchQuery { TenantId = tenant, CaseId = caseId, Query = "shared", MaxResults = 1000 });
        candidates.Results.ShouldHaveSingleItem().MemoryUnitId.ShouldBe(current);
        SearchResult visible = await publication.FilterResultAsync(tenant, candidates, 1, 0, null, token);
        visible.Results.ShouldHaveSingleItem().MemoryUnitId.ShouldBe(current);
        (await redis.Connection.GetDatabase().HashGetAsync(IndexSchemaDefinitions.BuildSyntacticKey(tenant, current), "content")).ToString().ShouldContain("newyellow");
        visible.TotalCount.ShouldBe(1);
        (string nativeQuery, IDictionary<string, object> nativeParameters) = new GraphQueryBuilder().BuildTraverseFromNode(current, 0, caseId, 1);
        NFalkorDB.ResultSet currentGraph = await new NFalkorDB.FalkorDB(graph.Connection.GetDatabase()).SelectGraph(tenant).QueryAsync(nativeQuery, nativeParameters);
        currentGraph.ShouldHaveSingleItem().Values[0]?.ToString().ShouldBe(current);
        (string staleQuery, IDictionary<string, object> staleParameters) = new GraphQueryBuilder().BuildTraverseFromNode("old", 0, caseId, 1);
        (await new NFalkorDB.FalkorDB(graph.Connection.GetDatabase()).SelectGraph(tenant).QueryAsync(staleQuery, staleParameters)).ShouldBeEmpty();

        (string retiredStart, IDictionary<string, object> retiredStartParameters) = new GraphQueryBuilder().BuildTraverseFromNode("old", 1, caseId, 10);
        (await new NFalkorDB.FalkorDB(graph.Connection.GetDatabase()).SelectGraph(tenant).QueryAsync(retiredStart, retiredStartParameters)).ShouldBeEmpty("A superseded start/path cannot expose neighbors through retired content.");

        await new NFalkorDB.FalkorDB(graph.Connection.GetDatabase()).SelectGraph(tenant).QueryAsync(
            "MATCH (a:MemoryUnit {id:'unrelated'}),(b:MemoryUnit {id:'old'}) MERGE (a)-[:REFERENCES]->(b)");
        var traversal = new GraphTraversalService(graph.Connection, redis.Connection, new GraphQueryBuilder(), NullLogger<GraphTraversalService>.Instance);
        TraversalResult unrelatedTraversal = await traversal.TraverseAsync(tenant, "unrelated", 0, caseId, null, token);
        unrelatedTraversal.Nodes.ShouldHaveSingleItem().Edges.ShouldNotContain(edge => edge.ConnectedNodeId == "old");

        SearchResult legacyPage = await search.SearchAsync(new SearchQuery { TenantId = tenant, CaseId = caseId, Query = "legacygreen", MaxResults = 1, Offset = 1001 });
        legacyPage.Results.ShouldHaveSingleItem();
        legacyPage.TotalCount.ShouldBe(1005);
        (await publication.FilterResultAsync(tenant, legacyPage, 1, 1001, null, token)).TotalCount.ShouldBe(1005);
        SearchResult second = await search.SearchAsync(new SearchQuery { TenantId = tenant, CaseId = caseId, Query = "shared", MaxResults = 1, Offset = 1 });
        (await publication.FilterResultAsync(tenant, second, 1, 1, null, token)).Results.ShouldBeEmpty();

        // An explicitly deleted predecessor may be recreated at the same logical
        // revision with a fresh retry identity. Lower revisions remain inadmissible.
        string erasedCurrent = current;
        await delete.RunAsync(context, new(tenant, caseId, erasedCurrent, []));
        IngestionInput recreated = newer with { ExpectedMemoryUnitId = erasedCurrent, IdempotencyToken = "recreation-revision-two" };
        recreated = recreated with { RefreshIdentityDigest = await publication.PrepareAsync(recreated, token) };
        await inventory.AdmitAsync("recreated-current", recreated, token);
        await IndexAsync(tenant, "recreated-current", source, recreated.ContentBytes!);
        (await publish.RunAsync(context, new(recreated, "recreated-current"))).ShouldBeTrue();
        (await publication.PublishAsync(newer, erasedCurrent, token)).ShouldBeFalse("A delayed tombstoned output cannot republish after recreation.");
        current = "recreated-current";
        candidates = await search.SearchAsync(new SearchQuery { TenantId = tenant, CaseId = caseId, Query = "shared", MaxResults = 1 });
        candidates.Results.ShouldHaveSingleItem().MemoryUnitId.ShouldBe(current);
        (string recreatedQuery, IDictionary<string, object> recreatedParameters) = new GraphQueryBuilder().BuildTraverseFromNode(current, 0, caseId, 1);
        (await new NFalkorDB.FalkorDB(graph.Connection.GetDatabase()).SelectGraph(tenant).QueryAsync(recreatedQuery, recreatedParameters))
            .ShouldHaveSingleItem().Values[0]?.ToString().ShouldBe(current);
        await Should.ThrowAsync<InvalidOperationException>(() => publication.PrepareAsync(older with { ExpectedMemoryUnitId = current }, token));

        using IConnectionMultiplexer restarted = await ConnectionMultiplexer.ConnectAsync(redis.ConnectionString);
        var restartedInventory = new SourceInventoryStore(restarted, codec);
        var restartedPublication = new RefreshPublicationStore(restarted, restartedInventory, graph.Connection);
        (await restartedPublication.FilterResultAsync(tenant, candidates, 1, 0, null, token)).Results.Single().MemoryUnitId.ShouldBe(current);
        SourceInventoryPage outputs = await restartedInventory.ReadAsync(tenant, source, null, 100, token);
        outputs.CoverageComplete.ShouldBeFalse("The test has not attested legacy inventory coverage.");
        outputs.Entries.ShouldContain(entry => entry.MemoryUnitId == "old");
        outputs.Entries.ShouldContain(entry => entry.MemoryUnitId == current && !entry.TerminalVerified);
        await restartedInventory.FenceAsync(tenant, source, true, token);
        await Should.ThrowAsync<InvalidOperationException>(() => restartedInventory.AdmitAsync("erased-retry", newer, token));
        await Should.ThrowAsync<InvalidOperationException>(() => SourcePublicationVisibility.WriteAsync(redis.Connection.GetDatabase(), tenant, "unadmitted-new-output",
            IndexSchemaDefinitions.BuildSyntacticKey(tenant, "unadmitted-new-output"), [new("content", "must remain absent")], false, caseId, source));
        (await redis.Connection.GetDatabase().KeyExistsAsync(IndexSchemaDefinitions.BuildSyntacticKey(tenant, "unadmitted-new-output"))).ShouldBeFalse();
        foreach (string unit in outputs.Entries.Select(entry => entry.MemoryUnitId).OfType<string>().Distinct())
        {
            await delete.RunAsync(context, new(tenant, caseId, unit, []));
            (await redis.Connection.GetDatabase().KeyExistsAsync(IndexSchemaDefinitions.BuildSyntacticKey(tenant, unit))).ShouldBeFalse();
            (await redis.Connection.GetDatabase().KeyExistsAsync(IndexSchemaDefinitions.BuildSemanticKey(tenant, unit))).ShouldBeFalse();
            (await redis.Connection.GetDatabase().KeyExistsAsync(IndexSchemaDefinitions.BuildSemanticChunkKey(tenant, unit, 0))).ShouldBeFalse();
            (await redis.Connection.GetDatabase().KeyExistsAsync(RedisDerivedStoreService.BuildSourceArtifactKey(tenant, unit))).ShouldBeFalse();
            NFalkorDB.ResultSet count = await new NFalkorDB.FalkorDB(graph.Connection.GetDatabase()).SelectGraph(tenant)
                .QueryAsync("MATCH (m:MemoryUnit {id:$id}) RETURN count(m)", new Dictionary<string, object> { ["id"] = unit });
            Convert.ToInt64(count.First().Values[0], System.Globalization.CultureInfo.InvariantCulture).ShouldBe(0);
        }
        (await redis.Connection.GetDatabase().KeyExistsAsync(IndexSchemaDefinitions.BuildSyntacticKey(tenant, "unrelated"))).ShouldBeTrue();
        (await redis.Connection.GetDatabase().KeyExistsAsync(IndexSchemaDefinitions.BuildSemanticKey(tenant, "unrelated"))).ShouldBeTrue();
        (await redis.Connection.GetDatabase().KeyExistsAsync(RedisDerivedStoreService.BuildSourceArtifactKey(tenant, "unrelated"))).ShouldBeTrue();
        (await new NFalkorDB.FalkorDB(graph.Connection.GetDatabase()).SelectGraph(tenant)
            .QueryAsync("MATCH (m:MemoryUnit {id:$id}) RETURN m.content", new Dictionary<string, object> { ["id"] = "unrelated" }))
            .ShouldHaveSingleItem().Values[0]!.ToString()!.ShouldContain("unrelated synthetic control");
        // An admitted worker delayed until after permanent erasure cannot recreate
        // any actual backend or artifact for the deleted canonical identity.
        (await Should.ThrowAsync<InvalidOperationException>(() => IndexAsync(tenant, current, source, newer.ContentBytes!)))
            .Message.ShouldBe("source-publication-write-fenced-or-full");
        await Should.ThrowAsync<InvalidOperationException>(() => SourcePublicationVisibility.WriteAsync(redis.Connection.GetDatabase(), tenant, current,
            IndexSchemaDefinitions.BuildSemanticChunkKey(tenant, current, 0), [new("memoryUnitId", current)], false, caseId, source));
        IndexInput delayed = IndexInputFactory.Create(memoryUnitId: current, tenantId: tenant, caseId: caseId, sourceUri: source,
            content: "delayed erased output") with { SourceInventoryTracked = true };
        await Should.ThrowAsync<InvalidOperationException>(() => new IndexSemanticActivity(redis.Connection, NullLogger<IndexSemanticActivity>.Instance).RunAsync(context, delayed));
        await new IndexGraphActivity(graph.Connection, new GraphQueryBuilder(), NullLogger<IndexGraphActivity>.Instance).RunAsync(context, delayed);
        (await new NFalkorDB.FalkorDB(graph.Connection.GetDatabase()).SelectGraph(tenant)
            .QueryAsync("MATCH (m:MemoryUnit {id:$id}) RETURN m.id", new Dictionary<string, object> { ["id"] = current })).ShouldBeEmpty();
        (await redis.Connection.GetDatabase().KeyExistsAsync(IndexSchemaDefinitions.BuildSyntacticKey(tenant, current))).ShouldBeFalse();
        (await redis.Connection.GetDatabase().KeyExistsAsync(IndexSchemaDefinitions.BuildSemanticKey(tenant, current))).ShouldBeFalse();
    }

    [Fact]
    public async Task GraphFailureRetainsDurableRepairAndAllGraphCallersFailClosedUntilRestartRepair()
    {
        CancellationToken token = TestContext.Current.CancellationToken;
        string tenant = "graph-divergence-" + Guid.NewGuid().ToString("N");
        const string source = "urn:synthetic:graph-divergence";
        const string caseId = "captured-case";
        var inventory = new SourceInventoryStore(redis.Connection, new QueryCursorCodec(new EphemeralDataProtectionProvider(), "GraphRepair"));
        var publication = new RefreshPublicationStore(redis.Connection, inventory, graph.Connection);
        IngestionInput old = Input(tenant, caseId, source, "old source", "initial", 0, null);
        await inventory.AdmitAsync("old", old, token);
        await IndexAsync(tenant, "old", source, old.ContentBytes!);
        await new SaveDedupKeyActivity(redis.Connection).RunAsync(null!, new(DedupKeyBuilder.BuildKey(tenant, caseId, source), "old"));
        IngestionInput next = Input(tenant, caseId, source, "new source", "refresh-one", 1, "old");
        next = next with { RefreshIdentityDigest = await publication.PrepareAsync(next, token) };
        await inventory.AdmitAsync("current", next, token);
        await IndexAsync(tenant, "current", source, next.ContentBytes!);
        string user = "failgraph" + Guid.NewGuid().ToString("N");
        const string password = "synthetic-owned-test-password";
        await graph.Connection.GetDatabase().ExecuteAsync("ACL", "SETUSER", user, "on", ">" + password, "~*", "+@all", "-graph.query");
        using IConnectionMultiplexer denied = await ConnectionMultiplexer.ConnectAsync(graph.ConnectionString + ",user=" + user + ",password=" + password);
        var failingPublication = new RefreshPublicationStore(redis.Connection, inventory, denied);
        await Should.ThrowAsync<RedisServerException>(() => failingPublication.PublishAsync(next, "current", token));
        (await redis.Connection.GetDatabase().StringGetAsync(DedupKeyBuilder.BuildKey(tenant, caseId, source))).ToString().ShouldBe("current");
        (await redis.Connection.GetDatabase().HashLengthAsync(GraphPublicationReadGate.JournalKey(tenant))).ShouldBe(1);
        var deniedTraversal = new GraphTraversalService(denied, redis.Connection, new GraphQueryBuilder(), NullLogger<GraphTraversalService>.Instance);
        await Should.ThrowAsync<RedisServerException>(() => deniedTraversal.TraverseAsync(tenant, "old", 0, caseId, null, token));
        var deniedSearch = new GraphScopedSearch(denied, redis.Connection, new GraphQueryBuilder(), NullLogger<GraphScopedSearch>.Instance);
        await Should.ThrowAsync<RedisServerException>(() => deniedSearch.SearchAsync(new SearchQuery { TenantId = tenant, Query = "old" }, "old", 0, cancellationToken: token));
        using IConnectionMultiplexer restartedRedis = await ConnectionMultiplexer.ConnectAsync(redis.ConnectionString);
        var restartedTraversal = new GraphTraversalService(graph.Connection, restartedRedis, new GraphQueryBuilder(), NullLogger<GraphTraversalService>.Instance);
        (await restartedTraversal.TraverseAsync(tenant, "old", 0, caseId, null, token)).Nodes.ShouldBeEmpty();
        (await restartedTraversal.TraverseAsync(tenant, "current", 0, caseId, null, token)).Nodes.ShouldHaveSingleItem();
        (await restartedRedis.GetDatabase().HashLengthAsync(GraphPublicationReadGate.JournalKey(tenant))).ShouldBe(0);
        var restartedPublication = new RefreshPublicationStore(restartedRedis, inventory, graph.Connection);
        (await restartedPublication.PublishAsync(next, "current", token)).ShouldBeTrue();
        await graph.Connection.GetDatabase().ExecuteAsync("ACL", "DELUSER", user);
        (await restartedRedis.GetDatabase().SetLengthAsync(TenantIngestionOwnership.Operations(tenant))).ShouldBe(0,
            "Explicit ACL non-execution must not leave an immortal write obligation after actual repair.");
        await TenantIngestionOwnership.PurgeAsync(restartedRedis, tenant, null, token);
        (await restartedRedis.GetDatabase().KeyExistsAsync(TenantIngestionOwnership.Key(tenant))).ShouldBeFalse();
    }

    [Fact]
    public async Task DirectDeletionBeforeReverseAliasRecoveryRepairsEveryOutputAfterSerializedRestart()
    {
        CancellationToken token = TestContext.Current.CancellationToken;
        string tenant = "alias-interruption-" + Guid.NewGuid().ToString("N");
        const string source = "urn:synthetic:alias-interruption";
        var codec = new QueryCursorCodec(new EphemeralDataProtectionProvider(), "AliasInterruption");
        var inventory = new SourceInventoryStore(redis.Connection, codec);
        IngestionInput input = Input(tenant, "captured-case", source, "synthetic alias content", "alias-retry", 0, null);
        await inventory.AdmitAsync("old", input, token);
        await IndexAsync(tenant, "old", source, input.ContentBytes!);
        await inventory.CompleteAsync(tenant, source, "old", "captured-case", "old", token);
        // Recreate the historical interrupted ordering: output persisted, reverse alias not yet retained.
        string aliasKey = "source-inventory-output:" + Convert.ToHexStringLower(System.Security.Cryptography.SHA256.HashData(
            System.Text.Json.JsonSerializer.SerializeToUtf8Bytes(new[] { tenant, "old" })));
        await redis.Connection.GetDatabase().KeyDeleteAsync(aliasKey);
        await new DeleteMemoryUnitProjectionActivity(redis.Connection, graph.Connection, new GraphQueryBuilder(), inventory)
            .RunAsync(null!, new(tenant, "captured-case", "old", []));
        using IConnectionMultiplexer connection = await ConnectionMultiplexer.ConnectAsync(redis.ConnectionString);
        var restarted = new SourceInventoryStore(connection, codec);
        SourceInventoryPage repaired = await restarted.ReadAsync(tenant, source, null, 100, token);
        repaired.Entries.ShouldHaveSingleItem().State.ShouldBe("Deleted");
        repaired.Entries.Single().TerminalVerified.ShouldBeFalse();
        await restarted.CompleteAsync(tenant, source, "old", "captured-case", "old", token);
        (await restarted.ReadAsync(tenant, source, null, 100, token)).Entries.ShouldHaveSingleItem().State.ShouldBe("Deleted");
        (await connection.GetDatabase().SetMembersAsync(aliasKey)).Select(value => value.ToString()).ShouldContain(source);
        (await connection.GetDatabase().KeyExistsAsync(IndexSchemaDefinitions.BuildSyntacticKey(tenant, "old"))).ShouldBeFalse();
    }

    private async Task IndexAsync(string tenant, string id, string source, byte[] content)
    {
        IDatabase db = redis.Connection.GetDatabase();
        try { db.FT().Create(IndexSchemaDefinitions.GetSyntacticIndexName(tenant), IndexSchemaDefinitions.CreateSyntacticParams(tenant), IndexSchemaDefinitions.CreateSyntacticSchema()); }
        catch (RedisServerException ex) when (ex.Message.Contains("Index already exists", StringComparison.OrdinalIgnoreCase)) { }
        try { db.FT().Create(IndexSchemaDefinitions.GetSemanticIndexName(tenant), IndexSchemaDefinitions.CreateSemanticParams(tenant), IndexSchemaDefinitions.CreateSemanticSchema(3)); }
        catch (RedisServerException ex) when (ex.Message.Contains("Index already exists", StringComparison.OrdinalIgnoreCase)) { }
        IndexInput input = IndexInputFactory.Create(memoryUnitId: id, tenantId: tenant, caseId: "captured-case", sourceUri: source,
            content: Encoding.UTF8.GetString(content)) with { SourceInventoryTracked = id != "old" && id != "unrelated" };
        WorkflowActivityContext context = Substitute.For<WorkflowActivityContext>();
        (await new IndexSyntacticActivity(redis.Connection, NullLogger<IndexSyntacticActivity>.Instance).RunAsync(context, input))
            .ShouldBe(new IndexResult("syntactic", id, tenant));
        (await new IndexSemanticActivity(redis.Connection, NullLogger<IndexSemanticActivity>.Instance).RunAsync(context, input))
            .ShouldBe(new IndexResult("semantic", id, tenant));
        (await new IndexSemanticChunksActivity(redis.Connection, NullLogger<IndexSemanticChunksActivity>.Instance).RunAsync(context, new SemanticChunkIndexInput
        {
            TenantId = tenant, CaseId = input.CaseId, MemoryUnitId = id, SourceInventoryTracked = input.SourceInventoryTracked,
            EmbeddingProvider = input.EmbeddingProvider, EmbeddingModel = input.EmbeddingModel, EmbeddingDimensions = 3,
            Chunks = [new ChunkEmbeddingResult { Sequence = 0, Text = input.Content, StartOffset = 0, EndOffset = input.Content.Length,
                EstimatedTokens = 4, Vector = [0.1f, 0.2f, 0.3f] }],
        })).ShouldBe(new IndexResult("semantic", id, tenant));
        (await new IndexGraphActivity(graph.Connection, new GraphQueryBuilder(), NullLogger<IndexGraphActivity>.Instance).RunAsync(context, input))
            .ShouldBe(new IndexResult("graph", id, tenant));
        await new RedisDerivedStoreService(redis.Connection, graph.Connection, TimeProvider.System).SaveSourceArtifactAsync(new(
            tenant, id, input.CaseId, source, SourceType.File, "text/plain", content, input.EmbeddingProvider, input.EmbeddingModel,
            input.EmbeddingDimensions, input.Metadata, input.IngestedBy, null, null, "{}", DateTimeOffset.UtcNow), TestContext.Current.CancellationToken);
        (await db.HashGetAsync(IndexSchemaDefinitions.BuildSyntacticKey(tenant, id), "content")).ToString().ShouldBe(input.Content);
        (await db.HashGetAsync(IndexSchemaDefinitions.BuildSemanticKey(tenant, id), "embedding")).HasValue.ShouldBeTrue();
        (await db.HashGetAsync(IndexSchemaDefinitions.BuildSemanticChunkKey(tenant, id, 0), "embedding")).HasValue.ShouldBeTrue();
        (await db.KeyExistsAsync(RedisDerivedStoreService.BuildSourceArtifactKey(tenant, id))).ShouldBeTrue();
        NFalkorDB.ResultSet populated = await new NFalkorDB.FalkorDB(graph.Connection.GetDatabase()).SelectGraph(tenant)
            .QueryAsync("MATCH (m:MemoryUnit {id:$id}) RETURN m.content", new Dictionary<string, object> { ["id"] = id });
        populated.ShouldHaveSingleItem().Values[0]?.ToString().ShouldBe(input.Content);
    }

    [Fact]
    public async Task LostAdmissionAcknowledgementReplaysTheSameReopenButCannotCrossANewFence()
    {
        CancellationToken token = TestContext.Current.CancellationToken;
        string tenant = "reopen-retry-" + Guid.NewGuid().ToString("N");
        const string source = "urn:synthetic:reopen-refresh";
        const string caseId = "captured-case";
        var codec = new QueryCursorCodec(new EphemeralDataProtectionProvider(), "ReopenRefreshTests");
        var inventory = new SourceInventoryStore(redis.Connection, codec);
        var publication = new RefreshPublicationStore(redis.Connection, inventory, graph.Connection);
        IngestionInput old = Input(tenant, caseId, source, "old content", "initial", 0, null);
        await inventory.AdmitAsync("old", old, token);
        await IndexAsync(tenant, "old", source, old.ContentBytes!);
        await new SaveDedupKeyActivity(redis.Connection).RunAsync(Substitute.For<WorkflowActivityContext>(),
            new(DedupKeyBuilder.BuildKey(tenant, caseId, source), "old"));
        await inventory.CompleteAsync(tenant, source, "old", caseId, "old", token);
        await inventory.MarkDeletedAsync(tenant, "old", token);
        await inventory.FenceAsync(tenant, source, false, token);
        SourceInventoryPage page = await inventory.ReadAsync(tenant, source, null, 100, token);
        await inventory.RecordTerminalEvidenceAsync(tenant, source, page.Entries.Single(), token);
        page = await inventory.ReadAsync(tenant, source, null, 100, token);
        await inventory.ReconcileAsync(tenant, source, page.Version, [], "owned-test-settled-reopen", token);
        page = await inventory.ReadAsync(tenant, source, null, 100, token);
        IngestionInput refresh = Input(tenant, caseId, source, "new content", "reopened-retry", 1, "old") with { ReopenSourceVersion = page.Version };
        string digest = await publication.PrepareAsync(refresh, token);
        await inventory.AdmitAsync("accepted-before-lost-ack", refresh, token);
        using IConnectionMultiplexer connection = await ConnectionMultiplexer.ConnectAsync(redis.ConnectionString);
        var restartedInventory = new SourceInventoryStore(connection, codec);
        var restarted = new RefreshPublicationStore(connection, restartedInventory, graph.Connection);
        (await restarted.PrepareAsync(refresh, token)).ShouldBe(digest);
        await Should.ThrowAsync<InvalidOperationException>(() => restarted.PrepareAsync(refresh with { ContentBytes = Encoding.UTF8.GetBytes("altered in-flight retry") }, token));
        await restartedInventory.FenceAsync(tenant, source, false, token);
        await Should.ThrowAsync<InvalidOperationException>(() => restarted.PrepareAsync(refresh, token));
        await restartedInventory.FenceAsync(tenant, source, true, token);
        await Should.ThrowAsync<InvalidOperationException>(() => restarted.PrepareAsync(refresh, token));
        (await restartedInventory.ReadAsync(tenant, source, null, 100, token)).Entries.ShouldContain(entry => entry.WorkflowInstanceId == "accepted-before-lost-ack");
    }

    private static IngestionInput Input(string tenant, string caseId, string source, string content, string retry, long revision, string? predecessor)
        => new() { TenantId = tenant, CaseId = caseId, SourceUri = source, ContentBytes = Encoding.UTF8.GetBytes(content), ContentType = "text/plain",
            SourceType = SourceType.File, IngestedBy = "synthetic-control", IdempotencyToken = retry, SourceRevisionNumber = revision,
            RefreshExistingSource = true, ExpectedMemoryUnitId = predecessor };
}
