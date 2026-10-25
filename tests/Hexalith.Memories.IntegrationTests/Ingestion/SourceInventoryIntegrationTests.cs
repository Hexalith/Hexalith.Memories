namespace Hexalith.Memories.IntegrationTests.Ingestion;

using System.Text.Json;
using Hexalith.EventStore.Client.Queries;
using Hexalith.Memories.Contracts.V1;
using Hexalith.Memories.IntegrationTests.Fixtures;
using Hexalith.Memories.Server.Ingestion;
using Microsoft.AspNetCore.DataProtection;
using Shouldly;
using StackExchange.Redis;
using NSubstitute;

/// <summary>Real Redis inventory persistence, CAS, scope/cursor, fence and restart controls; no mocked absence claims.</summary>
[Collection("RedisStack")]
[Trait("Category", "Integration")]
public sealed class SourceInventoryIntegrationTests(RedisStackFixture redis, FalkorDbFixture graph) : IClassFixture<FalkorDbFixture>
{
    [Fact]
    public async Task FullCapacityMutationReadsExactDeletionReceiptsInOneNativeBatch()
    {
        CancellationToken token = TestContext.Current.CancellationToken;
        string tenant = "mutation-batch-" + Guid.NewGuid().ToString("N");
        const string source = "urn:synthetic:full-mutation";
        IDatabase db = redis.Connection.GetDatabase();
        var snapshot = new SourceInventorySnapshot(tenant, source, "initial", Entries: Enumerable.Range(0, SourceInventoryStore.Capacity)
            .Select(index => new SourceInventoryEntry("case", "workflow-" + index, MemoryUnitId: "unit-" + index, State: "Completed")).ToArray());
        await db.StringSetAsync(SourceInventoryStore.BuildKey(tenant, source), JsonSerializer.Serialize(snapshot, new JsonSerializerOptions(JsonSerializerDefaults.Web)));
        var metricsOptions = ConfigurationOptions.Parse(redis.ConnectionString); metricsOptions.AllowAdmin = true;
        using IConnectionMultiplexer metrics = await ConnectionMultiplexer.ConnectAsync(metricsOptions);
        async Task<long> MultiGets()
        {
            string info = (await metrics.GetDatabase().ExecuteAsync("INFO", "commandstats")).ToString();
            var match = System.Text.RegularExpressions.Regex.Match(info, @"cmdstat_mget:calls=(\d+)");
            return match.Success ? long.Parse(match.Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture) : 0;
        }
        long before = await MultiGets();
        var inventory = new SourceInventoryStore(redis.Connection, new QueryCursorCodec(new EphemeralDataProtectionProvider(), "MutationBatchTests"));
        using var budget = CancellationTokenSource.CreateLinkedTokenSource(token); budget.CancelAfter(TimeSpan.FromSeconds(5));
        await inventory.FenceAsync(tenant, source, true, budget.Token);
        ((await MultiGets()) - before).ShouldBe(1);
        using JsonDocument persisted = JsonDocument.Parse((await db.StringGetAsync(SourceInventoryStore.BuildKey(tenant, source))).ToString());
        persisted.RootElement.GetProperty("admissionClosed").GetBoolean().ShouldBeTrue();
        persisted.RootElement.GetProperty("entries").GetArrayLength().ShouldBe(SourceInventoryStore.Capacity);
    }

    /// <summary>Historical cases, pending admissions and deleted canonical provenance all survive a serialized restart.</summary>
    [Fact]
    public async Task HistoricalCasesAndPendingOutputsRemainDurableAcrossRestart()
    {
        CancellationToken token = TestContext.Current.CancellationToken;
        string tenant = "inventory-" + Guid.NewGuid().ToString("N");
        const string source = "urn:synthetic:inventory";
        var codec = new QueryCursorCodec(new EphemeralDataProtectionProvider(), "SourceInventoryTests");
        var store = new SourceInventoryStore(redis.Connection, codec);
        (await store.ReadAsync(tenant, source, null, 1, token)).CoverageComplete.ShouldBeFalse();
        await store.AdmitAsync("workflow-old-case", Input(tenant, "historical", source), token);
        await store.AdmitAsync("workflow-current-case", Input(tenant, "current", source), token);
        await store.CompleteAsync(tenant, source, "workflow-old-case", "historical", "canonical-old", token);
        await store.FenceAsync(tenant, source, true, token);
        SourceInventoryPage first = await store.ReadAsync(tenant, source, null, 1, token);
        first.CoverageComplete.ShouldBeFalse("New inventory keys never certify unknown historical work.");
        first.Entries.Single().CaseId.ShouldBe("historical");
        first.NextCursor.ShouldNotBeNull();
        using IConnectionMultiplexer restartedConnection = await ConnectionMultiplexer.ConnectAsync(redis.ConnectionString);
        var restarted = new SourceInventoryStore(restartedConnection, codec);
        SourceInventoryPage second = await restarted.ReadAsync(tenant, source, first.NextCursor, 1, token);
        second.Version.ShouldBe(first.Version);
        second.Entries.Single().WorkflowInstanceId.ShouldBe("workflow-current-case");
        second.Entries.Single().MemoryUnitId.ShouldBeNull();
        second.NextCursor.ShouldBeNull();
        second.AdmissionClosed.ShouldBeTrue();
        second.Permanent.ShouldBeTrue();
        await restarted.MarkDeletedAsync(tenant, "canonical-old", token);
        SourceInventoryPage deleted = await restarted.ReadAsync(tenant, source, null, 100, token);
        deleted.Entries.Single(entry => entry.MemoryUnitId == "canonical-old").State.ShouldBe("Deleted");
        deleted.Entries.Count.ShouldBe(2);
        string raw = (await restartedConnection.GetDatabase().StringGetAsync(SourceInventoryStore.BuildKey(tenant, source))).ToString();
        raw.ShouldContain("canonical-old");
        raw.ShouldContain("workflow-current-case");
        raw.ShouldNotContain("ContentBytes");
        await restarted.ReconcileAsync(tenant, source, deleted.Version, [], "synthetic-owned-legacy-reconciliation", token);
        (await restarted.ReadAsync(tenant, source, null, 100, token)).CoverageComplete.ShouldBeTrue();
    }

    /// <summary>Concurrent source changes and different tenant/source scopes cannot reuse a cursor.</summary>
    [Fact]
    public async Task CursorsAreProtectedAndInvalidatedByConcurrentAdmission()
    {
        CancellationToken token = TestContext.Current.CancellationToken;
        string tenant = "cursor-" + Guid.NewGuid().ToString("N");
        var store = new SourceInventoryStore(redis.Connection, new QueryCursorCodec(new EphemeralDataProtectionProvider(), "CursorTests"));
        const string source = "urn:synthetic:cursor";
        await store.AdmitAsync("one", Input(tenant, "case", source), token);
        await store.AdmitAsync("two", Input(tenant, "case", source), token);
        SourceInventoryPage page = await store.ReadAsync(tenant, source, null, 1, token);
        await Should.ThrowAsync<InvalidOperationException>(() => store.ReadAsync(tenant + "-other", source, page.NextCursor, 1, token));
        await Should.ThrowAsync<InvalidOperationException>(() => store.ReadAsync(tenant, source + ":other", page.NextCursor, 1, token));
        await Should.ThrowAsync<InvalidOperationException>(() => store.ReadAsync(tenant, source, page.NextCursor + "tamper", 1, token));
        await store.AdmitAsync("three", Input(tenant, "historical-case", source), token);
        await Should.ThrowAsync<InvalidOperationException>(() => store.ReadAsync(tenant, source, page.NextCursor, 1, token));
        await store.FenceAsync(tenant, source, true, token);
        await Should.ThrowAsync<InvalidOperationException>(() => store.AdmitAsync("four", Input(tenant, "case", source), token));
        await Should.ThrowAsync<InvalidOperationException>(() => store.AdmitAsync("one", Input(tenant, "case", source), token));
        (await store.ReadAsync(tenant, source, null, 100, token)).Entries.Count.ShouldBe(3);
    }

    /// <summary>Malformed durable records and cancellation never become empty complete inventories.</summary>
    [Fact]
    public async Task CorruptionAndCancellationFailClosedWithoutDiscardingObligations()
    {
        CancellationToken token = TestContext.Current.CancellationToken;
        string tenant = "corrupt-" + Guid.NewGuid().ToString("N");
        const string source = "urn:synthetic:corrupt";
        var store = new SourceInventoryStore(redis.Connection, new QueryCursorCodec(new EphemeralDataProtectionProvider(), "CorruptTests"));
        string key = SourceInventoryStore.BuildKey(tenant, source);
        const string corrupt = "{broken";
        await redis.Connection.GetDatabase().StringSetAsync(key, corrupt);
        await Should.ThrowAsync<JsonException>(() => store.ReadAsync(tenant, source, null, 100, token));
        (await redis.Connection.GetDatabase().StringGetAsync(key)).ToString().ShouldBe(corrupt);
        using var canceled = new CancellationTokenSource();
        canceled.Cancel();
        await Should.ThrowAsync<OperationCanceledException>(() => store.AdmitAsync("canceled", Input(tenant, "case", "urn:cancel"), canceled.Token));
        (await store.ReadAsync(tenant, "urn:cancel", null, 100, token)).Entries.ShouldBeEmpty();
        (await store.ReadAsync(tenant, "urn:cancel", null, 100, token)).CoverageComplete.ShouldBeFalse();
    }

    [Fact]
    public async Task CanonicalAliasesAndReconciledHistoricalOutputsRetainIndependentDeletionAndTerminalEvidence()
    {
        CancellationToken token = TestContext.Current.CancellationToken;
        string tenant = "aliases-" + Guid.NewGuid().ToString("N");
        var store = new SourceInventoryStore(redis.Connection, new QueryCursorCodec(new EphemeralDataProtectionProvider(), "AliasTests"));
        const string first = "urn:synthetic:alias:first";
        const string second = "urn:synthetic:alias:second";
        await store.AdmitAsync("one", Input(tenant, "old-case", first), token);
        await store.AdmitAsync("two", Input(tenant, "old-case", second), token);
        await store.CompleteAsync(tenant, first, "one", "old-case", "shared-canonical", token);
        await store.CompleteAsync(tenant, second, "two", "old-case", "shared-canonical", token);
        await store.FenceAsync(tenant, first, true, token);
        SourceInventoryPage page = await store.ReadAsync(tenant, first, null, 100, token);
        await store.ReconcileAsync(tenant, first, page.Version,
            [new("historical-case", null, MemoryUnitId: "historical-canonical", State: "Completed")], "owned-test-owner-reconciliation", token);
        await store.MarkDeletedAsync(tenant, "shared-canonical", token);
        await store.MarkDeletedAsync(tenant, "historical-canonical", token);
        SourceInventoryPage deleted = await store.ReadAsync(tenant, first, null, 100, token);
        deleted.Entries.ShouldAllBe(entry => entry.State == "Deleted");
        deleted.Entries.Single(entry => entry.WorkflowInstanceId == "one").TerminalVerified.ShouldBeFalse("Output or deletion alone never proves workflow terminality.");
        (await store.ReadAsync(tenant, second, null, 100, token)).Entries.ShouldHaveSingleItem().State.ShouldBe("Deleted");
        await store.RecordTerminalEvidenceAsync(tenant, first, deleted.Entries.Single(entry => entry.WorkflowInstanceId == "one"), token);
        (await store.ReadAsync(tenant, first, null, 100, token)).Entries.Single(entry => entry.WorkflowInstanceId == "one").TerminalVerified.ShouldBeTrue();
        // The producer identity remains permanently fenced after its output was deleted.
        await Should.ThrowAsync<InvalidOperationException>(() => store.AdmitAsync("one", Input(tenant, "old-case", first), token));
    }

    [Fact]
    public async Task TransientReopenRequiresIndependentTerminalProofAfterOutputDeletion()
    {
        CancellationToken token = TestContext.Current.CancellationToken;
        string tenant = "reopen-" + Guid.NewGuid().ToString("N");
        const string source = "urn:synthetic:reopen";
        var store = new SourceInventoryStore(redis.Connection, new QueryCursorCodec(new EphemeralDataProtectionProvider(), "ReopenTests"));
        await store.AdmitAsync("producer", Input(tenant, "case", source), token);
        await store.CompleteAsync(tenant, source, "producer", "case", "unit", token);
        await store.MarkDeletedAsync(tenant, "unit", token);
        await store.FenceAsync(tenant, source, false, token);
        SourceInventoryPage page = await store.ReadAsync(tenant, source, null, 100, token);
        await store.ReconcileAsync(tenant, source, page.Version, [], "owned-test-reconciled-historical-scope", token);
        page = await store.ReadAsync(tenant, source, null, 100, token);
        await Should.ThrowAsync<InvalidOperationException>(() => store.ReopenAsync(tenant, source, page.Version, token));
        await store.RecordTerminalEvidenceAsync(tenant, source, page.Entries.Single(), token);
        page = await store.ReadAsync(tenant, source, null, 100, token);
        string reopenedFrom = page.Version;
        await store.ReopenAsync(tenant, source, reopenedFrom, token);
        await store.AdmitAsync("newly-admitted", Input(tenant, "case", source), token);
        using IConnectionMultiplexer restartedConnection = await ConnectionMultiplexer.ConnectAsync(redis.ConnectionString);
        var restarted = new SourceInventoryStore(restartedConnection, new QueryCursorCodec(new EphemeralDataProtectionProvider(), "ReopenReplay"));
        await restarted.ReopenAsync(tenant, source, reopenedFrom, token);
        (await restarted.ReadAsync(tenant, source, null, 100, token)).AdmissionClosed.ShouldBeFalse();
        await restarted.FenceAsync(tenant, source, false, token);
        await Should.ThrowAsync<InvalidOperationException>(() => restarted.ReopenAsync(tenant, source, reopenedFrom, token));
        await restarted.FenceAsync(tenant, source, true, token);
        await Should.ThrowAsync<InvalidOperationException>(() => restarted.ReopenAsync(tenant, source, reopenedFrom, token));
    }

    [Fact]
    public async Task RestoreInvalidatesPriorCoverageBeforeWritingAndCannotCrossASourceFence()
    {
        CancellationToken token = TestContext.Current.CancellationToken;
        string tenant = "restore-" + Guid.NewGuid().ToString("N");
        const string source = "urn:synthetic:restore";
        var store = new SourceInventoryStore(redis.Connection, new QueryCursorCodec(new EphemeralDataProtectionProvider(), "RestoreTests"));
        await store.FenceAsync(tenant, source, false, token);
        SourceInventoryPage page = await store.ReadAsync(tenant, source, null, 100, token);
        await store.ReconcileAsync(tenant, source, page.Version, [], "owned-empty-source-before-restore", token);
        page = await store.ReadAsync(tenant, source, null, 100, token);
        await store.ReopenAsync(tenant, source, page.Version, token);
        await store.InvalidateForRestoreAsync(tenant, source, "case", "restored-output", token);
        page = await store.ReadAsync(tenant, source, null, 100, token);
        page.CoverageComplete.ShouldBeFalse();
        page.IncompleteReason.ShouldBe("restore-reconciliation-required");
        page.Entries.ShouldHaveSingleItem().MemoryUnitId.ShouldBe("restored-output");
        await store.FenceAsync(tenant, source, true, token);
        await Should.ThrowAsync<InvalidOperationException>(() => store.InvalidateForRestoreAsync(tenant, source, "case", "new-output", token));
        (await store.ReadAsync(tenant, source, null, 100, token)).Entries.ShouldHaveSingleItem().MemoryUnitId.ShouldBe("restored-output");
    }

    [Fact]
    public async Task ActualRestoreActivityHonorsSourceInventoryBeforeAnyRestoredOutputWrite()
    {
        CancellationToken token = TestContext.Current.CancellationToken;
        string tenant = "restore-caller-" + Guid.NewGuid().ToString("N");
        const string source = "urn:synthetic:restore-caller";
        var inventory = new SourceInventoryStore(redis.Connection, new QueryCursorCodec(new EphemeralDataProtectionProvider(), "RestoreCaller"));
        await inventory.FenceAsync(tenant, source, true, token);
        string envelope = """
        {"manifest":{"schemaVersion":1,"scope":"tenant","tenantId":"TENANT","exportedAt":"2026-07-13T00:00:00Z","snapshotAt":"2026-07-13T00:00:00Z"},
        "tenant":{},"cases":[],"memoryUnits":[{"unit":{"id":"restored","tenantId":"TENANT","caseId":"case","content":"synthetic restored content","contentHash":"hash","sourceUri":"urn:synthetic:restore-caller","sourceType":"file","ingestedBy":"test","ingestedAt":"2026-07-01T00:00:00Z","lastUpdated":"2026-07-01T00:00:00Z","status":"indexed","metadata":{},"embeddingProvider":"google","embeddingModel":"test","embeddingDimensions":3},"annotationTargets":[]}],"edges":[],"statistics":{"memoryUnitCount":1,"edgeCount":0,"caseCount":0}}
        """.Replace("TENANT", tenant, StringComparison.Ordinal);
        var staging = NSubstitute.Substitute.For<Hexalith.Memories.Server.Import.IImportStagingStore>();
        staging.OpenReadAsync(NSubstitute.Arg.Any<string>(), NSubstitute.Arg.Any<CancellationToken>())
            .Returns(_ => new MemoryStream(System.Text.Encoding.UTF8.GetBytes(envelope)));
        var config = NSubstitute.Substitute.For<Hexalith.Memories.Server.Ingestion.ITenantEmbeddingConfigProvider>();
        config.GetAsync(tenant, NSubstitute.Arg.Any<CancellationToken>()).Returns(new TenantEmbeddingConfig("google", "test", 3, 60, "unused"));
        staging.OwnsRestoreLeaseAsync(NSubstitute.Arg.Any<string>(), NSubstitute.Arg.Any<CancellationToken>()).Returns(true);
        var activity = new Hexalith.Memories.Server.Activities.Restore.RestoreDataPlaneActivity(staging, redis.Connection, graph.Connection,
            new Hexalith.Memories.Server.Graph.GraphQueryBuilder(), config, NSubstitute.Substitute.For<Hexalith.Memories.Server.Import.IRestoreTargetGuard>(),
            Microsoft.Extensions.Logging.Abstractions.NullLogger<Hexalith.Memories.Server.Activities.Restore.RestoreDataPlaneActivity>.Instance,
            NSubstitute.Substitute.For<Hexalith.Memories.Server.Infrastructure.ITenantIndexReadinessVerifier>(), inventory);
        (await Should.ThrowAsync<InvalidOperationException>(() => activity.RunAsync(null!, new(tenant, null, "owned-staging", "test"))))
            .Message.ShouldBe("source-inventory-admission-closed");
        (await redis.Connection.GetDatabase().KeyExistsAsync(Hexalith.Memories.Server.Infrastructure.IndexSchemaDefinitions.BuildSyntacticKey(tenant, "restored"))).ShouldBeFalse();
        (await inventory.ReadAsync(tenant, source, null, 100, token)).Entries.ShouldBeEmpty();
    }

    [Fact]
    public async Task WorkflowAndHistoricalUnitIdentitiesCannotCollideAcrossRoundTrip()
    {
        CancellationToken token = TestContext.Current.CancellationToken;
        string tenant = "discriminated-" + Guid.NewGuid().ToString("N");
        const string source = "urn:synthetic:discriminated";
        var codec = new QueryCursorCodec(new EphemeralDataProtectionProvider(), "DiscriminatedTests");
        var store = new SourceInventoryStore(redis.Connection, codec);
        await store.AdmitAsync("unit:x", Input(tenant, "case", source), token);
        await store.RetainCanonicalAsync(tenant, source, "case", "x", token);
        using IConnectionMultiplexer restarted = await ConnectionMultiplexer.ConnectAsync(redis.ConnectionString);
        var recreated = new SourceInventoryStore(restarted, codec);
        (await recreated.ReadAsync(tenant, source, null, 100, token)).Entries.Count.ShouldBe(2);
        await recreated.FenceAsync(tenant, source, false, token);
        SourceInventoryPage page = await recreated.ReadAsync(tenant, source, null, 100, token);
        await recreated.ReconcileAsync(tenant, source, page.Version,
            [new("another-case", null, MemoryUnitId: "x", State: "Completed")], "exact-owner-test", token);
        (await recreated.ReadAsync(tenant, source, null, 100, token)).Entries.Count.ShouldBe(3);
    }

    [Theory]
    [InlineData(63)]
    [InlineData(65)]
    [InlineData(1000000)]
    public async Task InvalidPayloadHashCannotBeAdmittedOrReadFromDurableInventory(int length)
    {
        CancellationToken token = TestContext.Current.CancellationToken;
        string tenant = "bad-hash-" + Guid.NewGuid().ToString("N");
        const string source = "urn:synthetic:hash";
        var store = new SourceInventoryStore(redis.Connection, new QueryCursorCodec(new EphemeralDataProtectionProvider(), "HashTests"));
        var reference = new WorkflowPayloadReference("valid-id", new string('a', length), 1, WorkflowPayloadKind.SourceBytes, tenant, "prior");
        await Should.ThrowAsync<ArgumentException>(() => store.AdmitAsync("new", Input(tenant, "case", source) with { PayloadReference = reference }, token));
        (await redis.Connection.GetDatabase().KeyExistsAsync(SourceInventoryStore.BuildKey(tenant, source))).ShouldBeFalse();
        var snapshot = new SourceInventorySnapshot(tenant, source, "corrupt", Entries: [new("case", "new", SourcePayloadReference: reference)]);
        await redis.Connection.GetDatabase().StringSetAsync(SourceInventoryStore.BuildKey(tenant, source), JsonSerializer.Serialize(snapshot, new JsonSerializerOptions(JsonSerializerDefaults.Web)));
        await Should.ThrowAsync<ArgumentException>(() => store.ReadAsync(tenant, source, null, 100, token));
    }

    private static IngestionInput Input(string tenant, string caseId, string source) => new()
    {
        TenantId = tenant, CaseId = caseId, SourceUri = source, ContentType = "text/plain", SourceType = SourceType.File,
        ContentBytes = [1], IngestedBy = "synthetic-control",
    };
}
