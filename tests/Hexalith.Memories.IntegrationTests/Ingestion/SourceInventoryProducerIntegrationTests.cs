namespace Hexalith.Memories.IntegrationTests.Ingestion;

using System.Net.Http.Json;
using System.Reflection;
using System.Security.Claims;
using System.Text.Json;
using Dapr.Common.Serialization;
using Dapr.Workflow;
using Hexalith.EventStore.Client.Queries;
using Hexalith.Memories.Contracts.V1;
using Hexalith.Memories.IntegrationTests.Fixtures;
using Hexalith.Memories.Server.Activities.Ingestion;
using Hexalith.Memories.Server.Activities.Indexing;
using Hexalith.Memories.Server.Authentication;
using Hexalith.Memories.Server.Endpoints;
using Hexalith.Memories.Server.Graph;
using Hexalith.Memories.Server.Ingestion;
using Hexalith.Memories.Server.NaturalLanguage;
using Hexalith.Memories.Server.Workflows;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using Shouldly;
using StackExchange.Redis;
using NRedisStack.RedisStackCommands;
using Hexalith.Memories.Server.Infrastructure;

/// <summary>Executes the scheduler, producer activities and authenticated inventory endpoint against persisted Redis/graph state.</summary>
[Collection("RedisStack")]
public sealed class SourceInventoryProducerIntegrationTests(RedisStackFixture redis, FalkorDbFixture graph) : IClassFixture<FalkorDbFixture>
{
    /// <summary>Preserves lost-acknowledgement recovery only with the original matching durable admission.</summary>
    /// <param name="existingAdmission">Whether this control retained the original admission before losing the scheduling acknowledgement.</param>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ExistingTrackedWorkflowRequiresItsMatchingDurableAdmissionAndPreservesRedelivery(bool existingAdmission)
    {
        CancellationToken token = TestContext.Current.CancellationToken;
        string tenant = "existing-admission-" + Guid.NewGuid().ToString("N");
        IngestionInput input = Input(tenant);
        var inventory = new SourceInventoryStore(redis.Connection, new QueryCursorCodec(new EphemeralDataProtectionProvider(), "ExistingAdmission"));
        var client = Substitute.For<IDaprWorkflowClient>();
        IngestionInput? captured = null;
        bool loseAcknowledgement = true;
        client.ScheduleNewWorkflowAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<IngestionInput>(), null, Arg.Any<CancellationToken>())
            .Returns(call => { captured = call.Arg<IngestionInput>(); return loseAcknowledgement
                ? Task.FromException<string>(new IOException("lost schedule acknowledgement")) : Task.FromResult(call.ArgAt<string>(1)); });
        var scheduler = new DaprIngestionWorkflowScheduler(client, Payloads(new Dictionary<string, string>(), false),
            new IngestionWorkflowConfigurationCapture(Options.Create(new IngestionSettings()), Options.Create(new NaturalLanguageDescriptionOptions())),
            new WorkflowTraceContextCapture(), Substitute.For<IIngestionWorkflowInFlightRegistry>(), TimeProvider.System,
            NullLogger<DaprIngestionWorkflowScheduler>.Instance, inventory);
        if (existingAdmission) { await Should.ThrowAsync<IOException>(() => scheduler.ScheduleAsync("producer", input, token)); }
        loseAcknowledgement = false;
        client.GetWorkflowStateAsync("producer", true, Arg.Any<CancellationToken>())
            .Returns(State(captured ?? input with { SourceInventoryTracked = true }, WorkflowRuntimeStatus.Running, instanceId: "producer"));
        if (existingAdmission)
        {
            await scheduler.ScheduleAsync("producer", input, token);
            (await inventory.ReadAsync(tenant, input.SourceUri, null, 100, token)).Entries.ShouldHaveSingleItem();
            await new AdmitSourceInventoryActivity(inventory, client).RunAsync(null!, new("producer", input));
        }
        else
        {
            await Should.ThrowAsync<InvalidOperationException>(() => scheduler.ScheduleAsync("producer", input, token));
            await Should.ThrowAsync<InvalidOperationException>(() => new AdmitSourceInventoryActivity(inventory, client).RunAsync(null!, new("producer", input)));
        }
        (await redis.Connection.GetDatabase().KeyExistsAsync(SourcePublicationVisibility.KeyProvenance(tenant, "producer"))).ShouldBeFalse();
    }

    [Fact]
    public async Task AnnotationChildAdmissionEstablishesFreshCoverageBeforeItsFirstContentWrite()
    {
        CancellationToken token = TestContext.Current.CancellationToken;
        string tenant = "annotation-fresh-" + Guid.NewGuid().ToString("N");
        IngestionInput input = Input(tenant) with { SourceType = SourceType.Annotation };
        var inventory = new SourceInventoryStore(redis.Connection, new QueryCursorCodec(new EphemeralDataProtectionProvider(), "AnnotationFreshTests"));
        IngestionInput admitted = await new AdmitSourceInventoryActivity(inventory, Substitute.For<IDaprWorkflowClient>()).RunAsync(null!, new("annotation", input));
        admitted.SourceInventoryTracked.ShouldBeTrue();
        await new RecordSourceInventoryCandidateActivity(inventory).RunAsync(null!, new(tenant, input.SourceUri, "annotation", input.CaseId, "annotation"));
        (await redis.Connection.GetDatabase().StringGetAsync(SourcePublicationVisibility.KeyProvenance(tenant, "annotation"))).ToString().ShouldBe("modern");
        (await SemanticKeyDiscovery.FindAsync(redis.Connection.GetDatabase(), tenant, "annotation", token)).ShouldBe([IndexSchemaDefinitions.BuildSemanticKey(tenant, "annotation")]);
    }

    [Theory]
    [InlineData("pre-upgrade")]
    [InlineData("nl")]
    [InlineData("legacy-nl")]
    [InlineData("tombstone")]
    public async Task FreshCoverageRefusesPreUpgradeAdmissionOrExistingArtifacts(string scenario)
    {
        CancellationToken token = TestContext.Current.CancellationToken;
        string tenant = "fresh-candidate-" + Guid.NewGuid().ToString("N");
        IngestionInput input = Input(tenant);
        var inventory = new SourceInventoryStore(redis.Connection, new QueryCursorCodec(new EphemeralDataProtectionProvider(), "FreshCandidateTests"));
        IDatabase database = redis.Connection.GetDatabase();
        await inventory.AdmitAsync("producer", input, token);
        if (scenario == "pre-upgrade")
        {
            string key = SourceInventoryStore.BuildKey(tenant, input.SourceUri);
            var document = System.Text.Json.Nodes.JsonNode.Parse((string)(await database.StringGetAsync(key))!)!;
            document.AsObject().Remove("freshCandidateWorkflows");
            await database.StringSetAsync(key, document.ToJsonString());
            await inventory.RecordCandidateAsync(tenant, input.SourceUri, "producer", "producer", token);
        }
        else
        {
            if (scenario == "nl") { await database.HashSetAsync(IndexSchemaDefinitions.BuildNaturalLanguageSemanticKey(tenant, "producer"), "memoryUnitId", "producer"); }
            else if (scenario == "legacy-nl")
            {
                await database.HashSetAsync(IndexSchemaDefinitions.BuildLegacyNaturalLanguageSemanticKey(tenant, "producer"),
                    [new("memoryUnitId", "producer"), new("tenantId", tenant), new("description", "owned legacy NL"), new("vector", new byte[] { 0, 255, 128, 0 })]);
            }
            else { await database.StringSetAsync(SourcePublicationVisibility.Tombstone(tenant, "producer"), "1"); }
            string legacyKey = IndexSchemaDefinitions.BuildLegacyNaturalLanguageSemanticKey(tenant, "producer");
            HashEntry[] before = await database.HashGetAllAsync(legacyKey);
            var error = await Should.ThrowAsync<RedisServerException>(() => inventory.RecordCandidateAsync(tenant, input.SourceUri, "producer", "producer", token));
            error.Message.ShouldContain("source-publication-existing-candidate");
            (await database.HashGetAllAsync(legacyKey)).OrderBy(item => item.Name.ToString(), StringComparer.Ordinal)
                .ShouldBe(before.OrderBy(item => item.Name.ToString(), StringComparer.Ordinal));
            (await inventory.ReadAsync(tenant, input.SourceUri, null, 100, token)).Entries.ShouldHaveSingleItem().CandidateMemoryUnitId.ShouldBeNull();
        }
        (await database.KeyExistsAsync(SourcePublicationVisibility.KeyProvenance(tenant, "producer"))).ShouldBeFalse();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task CandidateRedeliveryAndFailedRetryCannotCertifyUnknownLegacyCoverage(bool failedRetry)
    {
        CancellationToken token = TestContext.Current.CancellationToken;
        string tenant = "legacy-candidate-" + Guid.NewGuid().ToString("N");
        IngestionInput input = Input(tenant);
        var inventory = new SourceInventoryStore(redis.Connection, new QueryCursorCodec(new EphemeralDataProtectionProvider(), "LegacyCandidateTests"));
        IDatabase database = redis.Connection.GetDatabase();
        if (failedRetry)
        {
            await inventory.AdmitFailedRetryAsync("retry", input with { FailedRetry = new("old", "producer") }, null, token);
            await inventory.RecordCandidateAsync(tenant, input.SourceUri, "retry", "producer", token);
        }
        else
        {
            await inventory.AdmitAsync("producer", input, token);
            await inventory.RecordCandidateAsync(tenant, input.SourceUri, "producer", "producer", token);
            (await database.StringGetAsync(SourcePublicationVisibility.KeyProvenance(tenant, "producer"))).ToString().ShouldBe("modern");
            // An old retained candidate without coverage cannot regain it through activity redelivery.
            await database.KeyDeleteAsync(SourcePublicationVisibility.KeyProvenance(tenant, "producer"));
            await inventory.RecordCandidateAsync(tenant, input.SourceUri, "producer", "producer", token);
        }
        (await database.KeyExistsAsync(SourcePublicationVisibility.KeyProvenance(tenant, "producer"))).ShouldBeFalse();
        await SourcePublicationVisibility.WriteAsync(database, tenant, "producer", IndexSchemaDefinitions.BuildSemanticKey(tenant, "producer"),
            [new("tenantId", tenant), new("memoryUnitId", "producer")], true, input.CaseId, input.SourceUri);
        (await database.KeyExistsAsync(SourcePublicationVisibility.KeyProvenance(tenant, "producer"))).ShouldBeFalse("A tracked retry write retains keys without certifying unknown legacy coverage.");
        await Should.ThrowAsync<InvalidOperationException>(() => SemanticKeyDiscovery.FindAsync(database, tenant, "producer", token));
    }

    [Theory]
    [InlineData("prepare")]
    [InlineData("track")]
    [InlineData("schedule")]
    public async Task OnlyProvenPreSchedulingFailuresCanSettleAfterSerializedRecreation(string stage)
    {
        CancellationToken token = TestContext.Current.CancellationToken;
        string tenant = "producer-" + Guid.NewGuid().ToString("N");
        IngestionInput input = Input(tenant);
        var codec = new QueryCursorCodec(new EphemeralDataProtectionProvider(), "ProducerTests");
        var inventory = new SourceInventoryStore(redis.Connection, codec);
        Dictionary<string, string> bytes = new(StringComparer.Ordinal);
        IWorkflowPayloadStore payloads = Payloads(bytes, stage == "prepare");
        var registry = Substitute.For<IIngestionWorkflowInFlightRegistry>();
        if (stage == "track") { registry.TrackAsync(Arg.Any<IngestionWorkflowInFlightEntry>(), Arg.Any<CancellationToken>()).Returns(Task.FromException(new InvalidOperationException("injected tracking failure"))); }
        var schedulerClient = Substitute.For<IDaprWorkflowClient>();
        schedulerClient.ScheduleNewWorkflowAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<IngestionInput>(), null, Arg.Any<CancellationToken>())
            .Returns(Task.FromException<string>(new InvalidOperationException("ambiguous remote scheduling")));
        var scheduler = new DaprIngestionWorkflowScheduler(schedulerClient, payloads,
            new IngestionWorkflowConfigurationCapture(Options.Create(new IngestionSettings()), Options.Create(new NaturalLanguageDescriptionOptions())),
            new WorkflowTraceContextCapture(), registry, TimeProvider.System, NullLogger<DaprIngestionWorkflowScheduler>.Instance, inventory);
        await Should.ThrowAsync<InvalidOperationException>(() => scheduler.ScheduleAsync("producer", input, token));
        SourceInventoryEntry admitted = (await inventory.ReadAsync(tenant, input.SourceUri, null, 100, token)).Entries.ShouldHaveSingleItem();
        admitted.SchedulingState.ShouldBe(stage == "schedule" ? "Scheduling" : "Unscheduled");
        admitted.SourcePayloadReference.ShouldNotBeNull();
        admitted.TerminalVerified.ShouldBeFalse();
        using IConnectionMultiplexer restartedConnection = await ConnectionMultiplexer.ConnectAsync(redis.ConnectionString);
        var restarted = new SourceInventoryStore(restartedConnection, codec);
        await restarted.FenceAsync(tenant, input.SourceUri, true, token);
        var (app, client) = await StartAsync(tenant, restarted, payloads, Substitute.For<IIngestionWorkflowStateReader>(), token);
        await using (app)
        using (client)
        {
            SourceInventoryPage observed = await client.GetFromJsonAsync<SourceInventoryPage>(Route(tenant, input.SourceUri), token) ?? throw new InvalidOperationException();
            SourceInventoryEntry entry = observed.Entries.ShouldHaveSingleItem();
            entry.TerminalVerified.ShouldBe(stage != "schedule");
            entry.State.ShouldBe(stage == "schedule" ? "Admitted" : "Deleted");
            bytes.Count.ShouldBe(stage == "schedule" ? 1 : 0);
            if (stage != "schedule") { await schedulerClient.DidNotReceiveWithAnyArgs().ScheduleNewWorkflowAsync(default!, default!, default!, default, default); }
        }
    }

    [Theory]
    [InlineData(WorkflowRuntimeStatus.Failed, false)]
    [InlineData(WorkflowRuntimeStatus.Failed, true)]
    [InlineData(WorkflowRuntimeStatus.Terminated, true)]
    public async Task FailedProducerArtifactsAreReconciledButTerminatedWorkRetainsItsFence(WorkflowRuntimeStatus status, bool materialized)
    {
        CancellationToken token = TestContext.Current.CancellationToken;
        string tenant = "failed-" + Guid.NewGuid().ToString("N");
        IngestionInput input = Input(tenant);
        var codec = new QueryCursorCodec(new EphemeralDataProtectionProvider(), "FailedProducerTests");
        var inventory = new SourceInventoryStore(redis.Connection, codec);
        Dictionary<string, string> bytes = new(StringComparer.Ordinal);
        IWorkflowPayloadStore payloads = Payloads(bytes);
        var schedulerClient = Substitute.For<IDaprWorkflowClient>();
        IngestionInput? captured = null;
        schedulerClient.ScheduleNewWorkflowAsync(Arg.Any<string>(), "producer", Arg.Any<IngestionInput>(), null, Arg.Any<CancellationToken>())
            .Returns(call => { captured = call.Arg<IngestionInput>(); return "producer"; });
        var scheduler = new DaprIngestionWorkflowScheduler(schedulerClient, payloads,
            new IngestionWorkflowConfigurationCapture(Options.Create(new IngestionSettings()), Options.Create(new NaturalLanguageDescriptionOptions())),
            new WorkflowTraceContextCapture(), Substitute.For<IIngestionWorkflowInFlightRegistry>(), TimeProvider.System,
            NullLogger<DaprIngestionWorkflowScheduler>.Instance, inventory);
        await scheduler.ScheduleAsync("producer", input, token);
        await new RecordSourceInventoryCandidateActivity(inventory).RunAsync(null!, new(tenant, input.SourceUri, "producer", "case", "producer"));
        if (materialized)
        {
            await redis.Connection.GetDatabase().HashSetAsync(tenant + ":mu:producer", [new HashEntry("content", "synthetic failed content")]);
            await new NFalkorDB.FalkorDB(graph.Connection.GetDatabase()).SelectGraph(tenant).QueryAsync("CREATE (:MemoryUnit {id:'producer',content:'synthetic failed content'})");
        }
        await new PersistFailedUnitActivity(redis.Connection, NullLogger<PersistFailedUnitActivity>.Instance).RunAsync(null!,
            new(tenant, "case", "producer", input.SourceUri, SourceType.File, "test", "text/plain", "extracting", "synthetic", "synthetic failure", 1,
                DateTimeOffset.UtcNow, DateTimeOffset.UtcNow, captured!.PayloadReference));
        var workflows = Substitute.For<IIngestionWorkflowStateReader>();
        workflows.GetWorkflowStateAsync("producer", true, Arg.Any<CancellationToken>()).Returns(State(captured, status));
        await inventory.FenceAsync(tenant, input.SourceUri, true, token);
        using IConnectionMultiplexer restartedConnection = await ConnectionMultiplexer.ConnectAsync(redis.ConnectionString);
        var restarted = new SourceInventoryStore(restartedConnection, codec);
        var (app, client) = await StartAsync(tenant, restarted, payloads, workflows, token);
        await using (app)
        using (client)
        {
            SourceInventoryPage observed = await client.GetFromJsonAsync<SourceInventoryPage>(Route(tenant, input.SourceUri), token) ?? throw new InvalidOperationException();
            bool terminal = status == WorkflowRuntimeStatus.Failed;
            observed.Entries.ShouldHaveSingleItem().TerminalVerified.ShouldBe(terminal);
            (await redis.Connection.GetDatabase().KeyExistsAsync(PersistFailedUnitActivity.BuildHashKey(tenant, "producer"))).ShouldBe(!terminal);
            (await redis.Connection.GetDatabase().SortedSetScoreAsync(PersistFailedUnitActivity.BuildSortedSetKey(tenant, "case"), "producer")).HasValue.ShouldBe(!terminal);
            bytes.Count.ShouldBe(terminal ? 0 : 1);
            if (terminal)
            {
                (await redis.Connection.GetDatabase().KeyExistsAsync(tenant + ":mu:producer")).ShouldBeFalse();
                (await new NFalkorDB.FalkorDB(graph.Connection.GetDatabase()).SelectGraph(tenant).QueryAsync("MATCH (m:MemoryUnit {id:'producer'}) RETURN m.id")).ShouldBeEmpty();
                observed.Entries.Single().State.ShouldBe("Deleted");
                (await new PersistFailedUnitActivity(redis.Connection, NullLogger<PersistFailedUnitActivity>.Instance).RunAsync(null!,
                    new(tenant, "case", "producer", input.SourceUri, SourceType.File, "late personal actor", "text/plain", "extracting", "late", "late personal failure", 1,
                        DateTimeOffset.UtcNow, DateTimeOffset.UtcNow, captured!.PayloadReference))).ShouldBeFalse();
                (await redis.Connection.GetDatabase().KeyExistsAsync(PersistFailedUnitActivity.BuildHashKey(tenant, "producer"))).ShouldBeFalse();
                (await redis.Connection.GetDatabase().SortedSetScoreAsync(PersistFailedUnitActivity.BuildSortedSetKey(tenant, "case"), "producer")).ShouldBeNull();
            }
        }
    }

    [Theory]
    [InlineData("valid")]
    [InlineData("event")]
    [InlineData("url")]
    [InlineData("running")]
    [InlineData("wrong-source")]
    [InlineData("wrong-tenant")]
    [InlineData("wrong-case")]
    [InlineData("wrong-owner")]
    [InlineData("completed")]
    [InlineData("terminated")]
    [InlineData("tombstone")]
    [InlineData("tenant-fence")]
    [InlineData("source-fence")]
    public async Task FailedRetryRetainsOldInventoryAndSchedulesOnlyWithExactFailedProof(string scenario)
    {
        CancellationToken token = TestContext.Current.CancellationToken;
        string tenant = "retry-" + Guid.NewGuid().ToString("N");
        IngestionInput input = Input(tenant);
        if (scenario == "event") { input = input with { SourceType = SourceType.Event }; }
        if (scenario == "url") { input = input with { SourceType = SourceType.Url, SourceUri = "https://synthetic.invalid/source", ContentBytes = null }; }
        string previousWorkflow = scenario == "event" ? DedupKeyBuilder.BuildKey(tenant, input.CaseId, input.SourceUri) : "producer";
        await OwnedSemanticInventory.ReconcileAsync(redis.Connection.GetDatabase(), tenant, "producer");
        var inventory = new SourceInventoryStore(redis.Connection, new QueryCursorCodec(new EphemeralDataProtectionProvider(), "RetryTests"));
        var payloads = Payloads(new Dictionary<string, string>(StringComparer.Ordinal));
        var workflow = Substitute.For<IDaprWorkflowClient>();
        IngestionInput? captured = null;
        workflow.ScheduleNewWorkflowAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<IngestionInput>(), null, Arg.Any<CancellationToken>())
            .Returns(call => { captured = call.Arg<IngestionInput>(); return call.ArgAt<string>(1); });
        var scheduler = new DaprIngestionWorkflowScheduler(workflow, payloads,
            new IngestionWorkflowConfigurationCapture(Options.Create(new IngestionSettings()), Options.Create(new NaturalLanguageDescriptionOptions())),
            new WorkflowTraceContextCapture(), Substitute.For<IIngestionWorkflowInFlightRegistry>(), TimeProvider.System,
            NullLogger<DaprIngestionWorkflowScheduler>.Instance, inventory);
        await scheduler.ScheduleAsync(previousWorkflow, input, token);
        await inventory.RecordCandidateAsync(tenant, input.SourceUri, previousWorkflow, "producer", token);
        SourceInventoryEntry original = (await inventory.ReadAsync(tenant, input.SourceUri, null, 100, token)).Entries.ShouldHaveSingleItem();
        var record = new FailedUnitRecord(tenant, input.CaseId, "producer", input.SourceUri, input.SourceType, input.IngestedBy, input.ContentType,
            "embedding", "provider-failed", "failed source", 3, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow, captured!.PayloadReference,
            new Dictionary<string, MetadataField> { ["source"] = new("original", MetadataOrigin.Human, 1f) }, "cause", "correlation");
        var registry = new FailedUnitsRegistry(redis.Connection, NullLogger<FailedUnitsRegistry>.Instance);
        await registry.RestoreAsync(record, token);
        using var recreated = await ConnectionMultiplexer.ConnectAsync(redis.ConnectionString);
        var restoredRegistry = new FailedUnitsRegistry(recreated, NullLogger<FailedUnitsRegistry>.Instance);
        var restored = (await restoredRegistry.GetAsync(tenant, "producer", token)).ShouldNotBeNull();
        restored.SourcePayloadReference.ShouldBe(record.SourcePayloadReference);
        restored.Metadata!["source"].ShouldBe(record.Metadata!["source"]);
        restored.FailedAt.ShouldBe(record.FailedAt);
        restored.CausationId.ShouldBe(record.CausationId);
        IngestionInput stateInput = scenario switch
        {
            "wrong-source" => captured with { SourceUri = "file:///foreign" },
            "wrong-tenant" => captured with { TenantId = "foreign" },
            "wrong-case" => captured with { CaseId = "foreign" },
            "wrong-owner" => captured with { PayloadReference = captured.PayloadReference! with { MemoryUnitId = "foreign" } },
            _ => captured,
        };
        WorkflowRuntimeStatus runtime = scenario switch { "running" => WorkflowRuntimeStatus.Running,
            "completed" => WorkflowRuntimeStatus.Completed, "terminated" => WorkflowRuntimeStatus.Terminated,
            _ => WorkflowRuntimeStatus.Failed };
        workflow.GetWorkflowStateAsync(previousWorkflow, true, Arg.Any<CancellationToken>())
            .Returns(State(stateInput, runtime, instanceId: previousWorkflow));
        if (scenario == "source-fence") { await inventory.FenceAsync(tenant, input.SourceUri, true, token); }
        if (scenario == "tombstone") { await redis.Connection.GetDatabase().StringSetAsync(SourcePublicationVisibility.Tombstone(tenant, "producer"), "1"); }
        if (scenario == "tenant-fence") { await redis.Connection.GetDatabase().StringSetAsync(TenantIngestionOwnership.Fence(tenant), "1"); }
        var coordinator = new ReIngestionCoordinator(restoredRegistry, payloads, scheduler, NullLogger<ReIngestionCoordinator>.Instance);
        if (scenario is "valid" or "event" or "url")
        {
            var result = await coordinator.TryScheduleAsync(tenant, input.CaseId, "producer", token);
            result.Outcome.ShouldBe(ReIngestionAttemptOutcome.Scheduled);
            result.MemoryUnitId.ShouldBe("producer");
            result.WorkflowInstanceId.ShouldNotBe("producer");
            captured!.FailedRetry.ShouldBe(new FailedIngestionRetry(previousWorkflow, "producer"));
            if (scenario == "url") { captured.PayloadReference.ShouldBeNull(); }
            else { captured.PayloadReference!.MemoryUnitId.ShouldBe("producer"); }
            var entries = (await inventory.ReadAsync(tenant, input.SourceUri, null, 100, token)).Entries;
            entries.Count.ShouldBe(2);
            entries[0].ShouldBe(original);
            entries[1].WorkflowInstanceId.ShouldBe(result.WorkflowInstanceId);
            entries[1].CandidateMemoryUnitId.ShouldBeNull();
            entries[1].FailedRetryMemoryUnitId.ShouldBe("producer");
            await inventory.RecordCandidateAsync(tenant, input.SourceUri, result.WorkflowInstanceId!, "producer", token);
            entries[1].SchedulingState.ShouldBe("Scheduled");
            (await restoredRegistry.GetAsync(tenant, "producer", token)).ShouldBeNull();
            // Fail the replacement, then prove the next generation links to it rather than the original workflow.
            IngestionInput replacement = captured;
            string replacementWorkflow = result.WorkflowInstanceId!;
            SourceInventoryEntry retainedReplacement = (await inventory.ReadAsync(tenant, input.SourceUri, null, 100, token)).Entries[1];
            workflow.GetWorkflowStateAsync(replacementWorkflow, true, Arg.Any<CancellationToken>())
                .Returns(State(replacement, WorkflowRuntimeStatus.Failed, instanceId: replacementWorkflow));
            await restoredRegistry.RestoreAsync(record with { SourcePayloadReference = replacement.PayloadReference }, token);
            var second = await coordinator.TryScheduleAsync(tenant, input.CaseId, "producer", token);
            second.Outcome.ShouldBe(ReIngestionAttemptOutcome.Scheduled);
            second.WorkflowInstanceId.ShouldNotBe(replacementWorkflow);
            captured!.FailedRetry.ShouldBe(new FailedIngestionRetry(replacementWorkflow, "producer"));
            var all = (await inventory.ReadAsync(tenant, input.SourceUri, null, 100, token)).Entries;
            all.Count.ShouldBe(3);
            all[0].ShouldBe(original);
            all[1].ShouldBe(retainedReplacement);
            all[2].FailedRetryMemoryUnitId.ShouldBe("producer");
            await inventory.RecordCandidateAsync(tenant, input.SourceUri, second.WorkflowInstanceId!, "producer", token);
            // Close this owned source and reconcile all three retained obligations through the actual endpoint.
            var states = Substitute.For<IIngestionWorkflowStateReader>();
            states.GetWorkflowStateAsync(previousWorkflow, true, Arg.Any<CancellationToken>()).Returns(State(stateInput, WorkflowRuntimeStatus.Failed, instanceId: previousWorkflow));
            states.GetWorkflowStateAsync(replacementWorkflow, true, Arg.Any<CancellationToken>()).Returns(State(replacement, WorkflowRuntimeStatus.Failed, instanceId: replacementWorkflow));
            states.GetWorkflowStateAsync(second.WorkflowInstanceId!, true, Arg.Any<CancellationToken>()).Returns(State(captured, WorkflowRuntimeStatus.Failed, instanceId: second.WorkflowInstanceId!));
            await inventory.FenceAsync(tenant, input.SourceUri, true, token);
            var (app, client) = await StartAsync(tenant, inventory, payloads, states, token);
            await using (app)
            using (client)
            {
                var page = (await client.GetFromJsonAsync<SourceInventoryPage>(Route(tenant, input.SourceUri), token)).ShouldNotBeNull();
                page.Entries.Count.ShouldBe(3);
                page.Entries.ShouldAllBe(entry => entry.State == "Deleted" && entry.TerminalVerified);
            }
        }
        else
        {
            if (scenario is "tombstone" or "tenant-fence")
                await Should.ThrowAsync<InvalidOperationException>(() => coordinator.TryScheduleAsync(tenant, input.CaseId, "producer", token));
            else (await coordinator.TryScheduleAsync(tenant, input.CaseId, "producer", token)).Outcome.ShouldBe(ReIngestionAttemptOutcome.Conflict);
            (await inventory.ReadAsync(tenant, input.SourceUri, null, 100, token)).Entries.ShouldBe([original]);
            if (scenario is not ("tombstone" or "tenant-fence"))
                (await restoredRegistry.GetAsync(tenant, "producer", token)).ShouldNotBeNull();
            else
                (await restoredRegistry.GetAsync(tenant, "producer", token)).ShouldBeNull();
        }
    }

    [Theory]
    [InlineData("prepare")]
    [InlineData("track")]
    public async Task ConclusivelyUnscheduledRetryDoesNotHideTheFailedPredecessor(string fault)
    {
        var token = TestContext.Current.CancellationToken;
        string tenant = "retry-preparation-" + Guid.NewGuid().ToString("N");
        IngestionInput input = Input(tenant);
        var inventory = new SourceInventoryStore(redis.Connection, new QueryCursorCodec(new EphemeralDataProtectionProvider(), "RetryPreparation"));
        var bytes = new Dictionary<string, string>(StringComparer.Ordinal);
        bool inject = false;
        var payloads = Payloads(bytes, failSaveWhen: () => inject && fault == "prepare");
        var registry = Substitute.For<IIngestionWorkflowInFlightRegistry>();
        registry.TrackAsync(Arg.Any<IngestionWorkflowInFlightEntry>(), Arg.Any<CancellationToken>())
            .Returns(_ => inject && fault == "track" ? Task.FromException(new IOException("injected tracking failure")) : Task.CompletedTask);
        var client = Substitute.For<IDaprWorkflowClient>();
        IngestionInput? scheduled = null;
        client.ScheduleNewWorkflowAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<IngestionInput>(), null, Arg.Any<CancellationToken>())
            .Returns(call => { scheduled = call.Arg<IngestionInput>(); return call.ArgAt<string>(1); });
        var scheduler = new DaprIngestionWorkflowScheduler(client, payloads,
            new IngestionWorkflowConfigurationCapture(Options.Create(new IngestionSettings()), Options.Create(new NaturalLanguageDescriptionOptions())),
            new WorkflowTraceContextCapture(), registry, TimeProvider.System, NullLogger<DaprIngestionWorkflowScheduler>.Instance, inventory);
        await scheduler.ScheduleAsync("producer", input, token);
        await inventory.RecordCandidateAsync(tenant, input.SourceUri, "producer", "producer", token);
        client.GetWorkflowStateAsync("producer", true, Arg.Any<CancellationToken>()).Returns(State(scheduled!, WorkflowRuntimeStatus.Failed));
        var record = new FailedUnitRecord(tenant, input.CaseId, "producer", input.SourceUri, input.SourceType, input.IngestedBy, input.ContentType,
            "embedding", "failed", "synthetic", 1, null, DateTimeOffset.UtcNow, scheduled!.PayloadReference);
        var failed = new FailedUnitsRegistry(redis.Connection, NullLogger<FailedUnitsRegistry>.Instance);
        await failed.RestoreAsync(record, token);
        var coordinator = new ReIngestionCoordinator(failed, payloads, scheduler, NullLogger<ReIngestionCoordinator>.Instance);
        inject = true;
        await Should.ThrowAsync<Exception>(() => coordinator.TryScheduleAsync(tenant, input.CaseId, "producer", token));
        (await failed.GetAsync(tenant, "producer", token)).ShouldNotBeNull();
        var retained = (await inventory.ReadAsync(tenant, input.SourceUri, null, 100, token)).Entries;
        retained.Count.ShouldBe(2);
        retained[1].SchedulingState.ShouldBe("Unscheduled");
        retained[1].SourcePayloadReference.ShouldNotBeNull();
        retained[1].TerminalVerified.ShouldBeFalse();
        inject = false;
        var next = await coordinator.TryScheduleAsync(tenant, input.CaseId, "producer", token);
        next.Outcome.ShouldBe(ReIngestionAttemptOutcome.Scheduled);
        scheduled!.FailedRetry.ShouldBe(new FailedIngestionRetry("producer", "producer"));
        var all = (await inventory.ReadAsync(tenant, input.SourceUri, null, 100, token)).Entries;
        all.Count.ShouldBe(3);
        all.Take(2).ShouldBe(retained);
        all[2].WorkflowInstanceId.ShouldBe(next.WorkflowInstanceId);
        all[2].FailedRetryMemoryUnitId.ShouldBe("producer");
        bytes.ShouldContainKey(retained[1].SourcePayloadReference!.Id);
    }

    [Theory]
    [InlineData("Preparing")]
    [InlineData("Unknown")]
    [InlineData("Unscheduled")]
    public async Task UnprovedOrMaterializedNewerAttemptNeverFallsBackToAnOlderFailedWorkflow(string scheduling)
    {
        var token = TestContext.Current.CancellationToken;
        string tenant = "retry-blocker-" + Guid.NewGuid().ToString("N");
        IngestionInput input = Input(tenant);
        var inventory = new SourceInventoryStore(redis.Connection, new QueryCursorCodec(new EphemeralDataProtectionProvider(), "RetryBlocker"));
        var payloads = Payloads(new Dictionary<string, string>(StringComparer.Ordinal));
        IngestionInput prepared = await IngestionPayloadClaimCheck.PrepareAsync(payloads, "producer", input, token);
        await inventory.AdmitAsync("producer", prepared, token);
        await inventory.SetSchedulingAsync(tenant, input.SourceUri, "producer", "Scheduling", token);
        await inventory.SetSchedulingAsync(tenant, input.SourceUri, "producer", "Scheduled", token);
        await inventory.RecordCandidateAsync(tenant, input.SourceUri, "producer", "producer", token);
        var original = (await inventory.ReadAsync(tenant, input.SourceUri, null, 100, token)).Entries.Single();
        await inventory.AdmitFailedRetryAsync("blocking", input with { FailedRetry = new("producer", "producer") }, original, token);
        string key = SourceInventoryStore.BuildKey(tenant, input.SourceUri);
        var snapshot = System.Text.Json.Nodes.JsonNode.Parse((await redis.Connection.GetDatabase().StringGetAsync(key)).ToString())!;
        var latest = snapshot["entries"]!.AsArray().Last()!;
        latest["schedulingState"] = scheduling;
        if (scheduling == "Unscheduled") latest["candidateMemoryUnitId"] = "producer";
        await redis.Connection.GetDatabase().StringSetAsync(key, snapshot.ToJsonString());
        var client = Substitute.For<IDaprWorkflowClient>();
        client.GetWorkflowStateAsync("producer", true, Arg.Any<CancellationToken>()).Returns(State(prepared, WorkflowRuntimeStatus.Failed));
        var scheduler = new DaprIngestionWorkflowScheduler(client, payloads,
            new IngestionWorkflowConfigurationCapture(Options.Create(new IngestionSettings()), Options.Create(new NaturalLanguageDescriptionOptions())),
            new WorkflowTraceContextCapture(), Substitute.For<IIngestionWorkflowInFlightRegistry>(), TimeProvider.System,
            NullLogger<DaprIngestionWorkflowScheduler>.Instance, inventory);
        var record = new FailedUnitRecord(tenant, input.CaseId, "producer", input.SourceUri, input.SourceType, input.IngestedBy, input.ContentType,
            "embedding", "failed", "synthetic", 1, null, DateTimeOffset.UtcNow, prepared.PayloadReference);
        await Should.ThrowAsync<FailedRetryEligibilityException>(() => scheduler.ScheduleFailedRetryAsync(record, prepared, token));
        await client.DidNotReceiveWithAnyArgs().GetWorkflowStateAsync(default!, default, default);
        await client.DidNotReceiveWithAnyArgs().ScheduleNewWorkflowAsync(default!, default!, default!, default, default);
        (await inventory.ReadAsync(tenant, input.SourceUri, null, 100, token)).Entries.Count.ShouldBe(2);
    }

    [Theory]
    [InlineData("failed")]
    [InlineData("running")]
    [InlineData("missing")]
    [InlineData("wrong-lineage")]
    public async Task LostScheduledAcknowledgementRequiresExactFailedAcceptedAttemptBeforeAnotherRetry(string outcome)
    {
        var token = TestContext.Current.CancellationToken;
        string tenant = "retry-ack-" + Guid.NewGuid().ToString("N");
        IngestionInput input = Input(tenant) with { SourceType = SourceType.Event };
        string producer = DedupKeyBuilder.BuildKey(tenant, input.CaseId, input.SourceUri);
        var codec = new QueryCursorCodec(new EphemeralDataProtectionProvider(), "RetryAck");
        IDatabase actual = redis.Connection.GetDatabase();
        var database = Substitute.For<IDatabase>();
        database.StringGetAsync(Arg.Any<RedisKey>(), Arg.Any<CommandFlags>()).Returns(c => actual.StringGetAsync(c.Arg<RedisKey>(), c.Arg<CommandFlags>()));
        database.StringGetAsync(Arg.Any<RedisKey[]>(), Arg.Any<CommandFlags>()).Returns(c => actual.StringGetAsync(c.Arg<RedisKey[]>(), c.Arg<CommandFlags>()));
        bool loseAck = false;
        database.ScriptEvaluateAsync(Arg.Any<string>(), Arg.Any<RedisKey[]>(), Arg.Any<RedisValue[]>(), Arg.Any<CommandFlags>()).Returns(c =>
        {
            RedisValue[] values = c.Arg<RedisValue[]>();
            if (loseAck && values.Length == 2 && values[1].ToString().Contains("\"schedulingState\":\"Scheduled\"", StringComparison.Ordinal))
            {
                var snapshot = System.Text.Json.Nodes.JsonNode.Parse(values[1].ToString())!;
                if (snapshot["entries"]!.AsArray().Last()!["schedulingState"]!.GetValue<string>() == "Scheduled")
                    return Task.FromException<RedisResult>(new IOException("lost final scheduling acknowledgement"));
            }
            return actual.ScriptEvaluateAsync(c.Arg<string>(), c.Arg<RedisKey[]>(), values, c.Arg<CommandFlags>());
        });
        var connection = Substitute.For<IConnectionMultiplexer>();
        connection.GetDatabase(Arg.Any<int>(), Arg.Any<object>()).Returns(database);
        var inventory = new SourceInventoryStore(connection, codec);
        var payloads = Payloads(new Dictionary<string, string>(StringComparer.Ordinal));
        var client = Substitute.For<IDaprWorkflowClient>();
        IngestionInput? captured = null;
        string? accepted = null;
        client.ScheduleNewWorkflowAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<IngestionInput>(), null, Arg.Any<CancellationToken>())
            .Returns(c => { captured = c.Arg<IngestionInput>(); accepted = c.ArgAt<string>(1); return accepted; });
        var scheduler = new DaprIngestionWorkflowScheduler(client, payloads,
            new IngestionWorkflowConfigurationCapture(Options.Create(new IngestionSettings()), Options.Create(new NaturalLanguageDescriptionOptions())),
            new WorkflowTraceContextCapture(), Substitute.For<IIngestionWorkflowInFlightRegistry>(), TimeProvider.System,
            NullLogger<DaprIngestionWorkflowScheduler>.Instance, inventory);
        await scheduler.ScheduleAsync(producer, input, token);
        await inventory.RecordCandidateAsync(tenant, input.SourceUri, producer, "producer", token);
        client.GetWorkflowStateAsync(producer, true, Arg.Any<CancellationToken>()).Returns(State(captured!, WorkflowRuntimeStatus.Failed, instanceId: producer));
        var failed = new FailedUnitsRegistry(redis.Connection, NullLogger<FailedUnitsRegistry>.Instance);
        var record = new FailedUnitRecord(tenant, input.CaseId, "producer", input.SourceUri, input.SourceType, input.IngestedBy, input.ContentType,
            "embedding", "failed", "synthetic", 1, null, DateTimeOffset.UtcNow, captured!.PayloadReference);
        await failed.RestoreAsync(record, token);
        var coordinator = new ReIngestionCoordinator(failed, payloads, scheduler, NullLogger<ReIngestionCoordinator>.Instance);
        loseAck = true;
        await Should.ThrowAsync<IOException>(() => coordinator.TryScheduleAsync(tenant, input.CaseId, "producer", token));
        string acceptedRetry = accepted!;
        IngestionInput acceptedInput = captured!;
        (await failed.GetAsync(tenant, "producer", token)).ShouldNotBeNull();
        var before = (await inventory.ReadAsync(tenant, input.SourceUri, null, 100, token)).Entries;
        before.Count.ShouldBe(2);
        before[1].SchedulingState.ShouldBe("Scheduling");
        before[1].FailedRetryMemoryUnitId.ShouldBe("producer");
        loseAck = false;
        (await new PersistFailedUnitActivity(redis.Connection, NullLogger<PersistFailedUnitActivity>.Instance).RunAsync(null!,
            new(tenant, input.CaseId, "producer", input.SourceUri, input.SourceType, input.IngestedBy, input.ContentType,
                "embedding", "failed", "synthetic", 1, null, DateTimeOffset.UtcNow, acceptedInput.PayloadReference))).ShouldBeTrue();
        if (outcome != "missing") client.GetWorkflowStateAsync(acceptedRetry, true, Arg.Any<CancellationToken>())
            .Returns(State(outcome == "wrong-lineage" ? acceptedInput with { FailedRetry = new(producer, "foreign") } : acceptedInput,
                outcome == "running" ? WorkflowRuntimeStatus.Running : WorkflowRuntimeStatus.Failed, instanceId: acceptedRetry));
        var next = await coordinator.TryScheduleAsync(tenant, input.CaseId, "producer", token);
        var all = (await inventory.ReadAsync(tenant, input.SourceUri, null, 100, token)).Entries;
        all.Take(2).ShouldBe(before);
        if (outcome == "failed")
        {
            next.Outcome.ShouldBe(ReIngestionAttemptOutcome.Scheduled);
            captured!.FailedRetry.ShouldBe(new FailedIngestionRetry(acceptedRetry, "producer"));
            all.Count.ShouldBe(3);
        }
        else
        {
            next.Outcome.ShouldBe(ReIngestionAttemptOutcome.Conflict);
            all.Count.ShouldBe(2);
            (await failed.GetAsync(tenant, "producer", token)).ShouldNotBeNull();
        }
    }

    [Fact]
    public async Task ActualDaprPayloadAdapterRetainsEveryAcknowledgedArtifactAcrossRestartAndDeletionFence()
    {
        CancellationToken token = TestContext.Current.CancellationToken;
        string tenant = "payload-ledger-" + Guid.NewGuid().ToString("N");
        await OwnedSemanticInventory.ReconcileAsync(redis.Connection.GetDatabase(), tenant, "candidate");
        EnsureProvisionedSemanticIndex(tenant);
        await WaitForSemanticIndexAsync(tenant, token);
        var dapr = Substitute.For<Dapr.Client.DaprClient>();
        Dictionary<string, string> persisted = new(StringComparer.Ordinal);
        bool loseAcknowledgement = false;
        dapr.SaveStateAsync("statestore", Arg.Any<string>(), Arg.Any<WorkflowPayloadStoreEntry>(), metadata: Arg.Any<IReadOnlyDictionary<string, string>>(), cancellationToken: Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                persisted[call.ArgAt<string>(1)] = JsonSerializer.Serialize(call.Arg<WorkflowPayloadStoreEntry>());
                if (loseAcknowledgement) { throw new InvalidOperationException("lost Dapr payload acknowledgement"); }
                return Task.CompletedTask;
            });
        dapr.GetStateAsync<WorkflowPayloadStoreEntry?>("statestore", Arg.Any<string>(), cancellationToken: Arg.Any<CancellationToken>())
            .Returns(call => persisted.TryGetValue(call.ArgAt<string>(1), out string? raw) ? JsonSerializer.Deserialize<WorkflowPayloadStoreEntry>(raw) : null);
        dapr.DeleteStateAsync("statestore", Arg.Any<string>(), cancellationToken: Arg.Any<CancellationToken>())
            .Returns(call => { persisted.Remove(call.ArgAt<string>(1)); return Task.CompletedTask; });
        var store = new DaprWorkflowPayloadStore(dapr, Options.Create(new WorkflowPayloadStoreOptions()), TimeProvider.System, redis.Connection);
        await store.SaveAsync(tenant, "candidate", WorkflowPayloadKind.SourceBytes, new byte[] { 1 }, cancellationToken: token);
        await store.SaveAsync(tenant, "candidate", WorkflowPayloadKind.ExtractedText, new byte[] { 2 }, cancellationToken: token);
        await store.SaveAsync(tenant, "candidate", WorkflowPayloadKind.ChunkVector, new byte[] { 3 }, cancellationToken: token);
        persisted.Count.ShouldBe(3);
        (await redis.Connection.GetDatabase().SetLengthAsync(WorkflowPayloadLedger.Key(tenant, "candidate"))).ShouldBe(3);
        using IConnectionMultiplexer restarted = await ConnectionMultiplexer.ConnectAsync(redis.ConnectionString);
        var recreatedStore = new DaprWorkflowPayloadStore(dapr, Options.Create(new WorkflowPayloadStoreOptions()), TimeProvider.System, restarted);
        await new Hexalith.Memories.Server.Activities.Cases.DeleteMemoryUnitProjectionActivity(restarted, graph.Connection, new GraphQueryBuilder(), payloadStore: recreatedStore, workflowClient: Substitute.For<IDaprWorkflowClient>())
            .RunAsync(null!, new(tenant, "case", "candidate", []));
        persisted.ShouldBeEmpty();
        (await restarted.GetDatabase().SetLengthAsync(WorkflowPayloadLedger.Key(tenant, "candidate"))).ShouldBe(0);
        await Should.ThrowAsync<InvalidOperationException>(() => recreatedStore.SaveAsync(tenant, "candidate", WorkflowPayloadKind.SourceBytes, new byte[] { 4 }, cancellationToken: token));
        persisted.ShouldBeEmpty();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task PausedOrAmbiguousDaprSaveCannotBeForgottenByPurgeAcrossRestart(bool loseAcknowledgement)
    {
        CancellationToken token = TestContext.Current.CancellationToken;
        string tenant = "payload-pending-" + Guid.NewGuid().ToString("N");
        var dapr = Substitute.For<Dapr.Client.DaprClient>();
        Dictionary<string, string> persisted = new(StringComparer.Ordinal);
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        dapr.SaveStateAsync("statestore", Arg.Any<string>(), Arg.Any<WorkflowPayloadStoreEntry>(), metadata: Arg.Any<IReadOnlyDictionary<string, string>>(), cancellationToken: Arg.Any<CancellationToken>())
            .Returns(async call =>
            {
                entered.SetResult();
                await release.Task.WaitAsync(token);
                persisted[call.ArgAt<string>(1)] = JsonSerializer.Serialize(call.Arg<WorkflowPayloadStoreEntry>());
                if (loseAcknowledgement) { throw new InvalidOperationException("ambiguous Dapr save completion"); }
            });
        dapr.GetStateAsync<WorkflowPayloadStoreEntry?>("statestore", Arg.Any<string>(), cancellationToken: Arg.Any<CancellationToken>())
            .Returns(call => persisted.TryGetValue(call.ArgAt<string>(1), out string? raw) ? JsonSerializer.Deserialize<WorkflowPayloadStoreEntry>(raw) : null);
        dapr.DeleteStateAsync("statestore", Arg.Any<string>(), cancellationToken: Arg.Any<CancellationToken>())
            .Returns(call => { persisted.Remove(call.ArgAt<string>(1)); return Task.CompletedTask; });
        var store = new DaprWorkflowPayloadStore(dapr, Options.Create(new WorkflowPayloadStoreOptions()), TimeProvider.System, redis.Connection);
        Task<WorkflowPayloadReference> save = store.SaveAsync(tenant, "candidate", WorkflowPayloadKind.SourceBytes, new byte[] { 1 }, cancellationToken: token);
        await entered.Task.WaitAsync(token);
        persisted.ShouldBeEmpty();
        (await Should.ThrowAsync<InvalidOperationException>(() => new WorkflowPayloadLedger(redis.Connection).PurgeAsync(tenant, "candidate", store, token)))
            .Message.ShouldBe("workflow-payload-write-completion-unresolved");
        using IConnectionMultiplexer restarted = await ConnectionMultiplexer.ConnectAsync(redis.ConnectionString);
        var recreated = new DaprWorkflowPayloadStore(dapr, Options.Create(new WorkflowPayloadStoreOptions()), TimeProvider.System, restarted);
        (await Should.ThrowAsync<InvalidOperationException>(() => new WorkflowPayloadLedger(restarted).PurgeAsync(tenant, "candidate", recreated, token)))
            .Message.ShouldBe("workflow-payload-write-completion-unresolved");
        release.SetResult();
        await Should.ThrowAsync<InvalidOperationException>(() => save);
        if (loseAcknowledgement)
        {
            (await restarted.GetDatabase().HashLengthAsync(WorkflowPayloadLedger.PendingKey(tenant, "candidate"))).ShouldBe(1);
            persisted.ShouldHaveSingleItem();
            await Should.ThrowAsync<InvalidOperationException>(() => new WorkflowPayloadLedger(restarted).PurgeAsync(tenant, "candidate", recreated, token));
            persisted.ShouldHaveSingleItem("An ambiguous producer remains unresolved until independent completion authority is available.");
        }
        else
        {
            (await restarted.GetDatabase().HashLengthAsync(WorkflowPayloadLedger.PendingKey(tenant, "candidate"))).ShouldBe(0);
            await new WorkflowPayloadLedger(restarted).PurgeAsync(tenant, "candidate", recreated, token);
            persisted.ShouldBeEmpty();
        }
    }

    [Theory]
    [InlineData("graph")]
    [InlineData("child")]
    [InlineData("preparing")]
    public async Task AnnotationHistoryProvesOnlyUnattemptedChildrenUnscheduledAcrossRecreation(string boundary)
    {
        CancellationToken token = TestContext.Current.CancellationToken;
        string tenant = "annotation-proof-" + Guid.NewGuid().ToString("N");
        var codec = new QueryCursorCodec(new EphemeralDataProtectionProvider(), "AnnotationProofTests");
        var inventory = new SourceInventoryStore(redis.Connection, codec);
        var input = new Hexalith.Memories.Server.Activities.Cases.AnnotationProjectionInput(tenant, "case", "annotation", "target", "urn:annotation",
            "synthetic annotation", "correction", "test", new Dictionary<string, MetadataField>(), new IngestionWorkflowConfiguration())
            { TrackSourceInventory = true };
        WorkflowContext context = Substitute.For<WorkflowContext>();
        context.CallActivityAsync<IngestionInput>(nameof(AdmitSourceInventoryActivity), Arg.Any<SourceInventoryAdmissionInput>(), Arg.Any<WorkflowTaskOptions>())
            .Returns(call => new AdmitSourceInventoryActivity(inventory, Substitute.For<IDaprWorkflowClient>()).RunAsync(null!, call.Arg<SourceInventoryAdmissionInput>()));
        context.CallActivityAsync<bool>(nameof(SetSourceInventorySchedulingActivity), Arg.Any<SourceInventorySchedulingInput>(), Arg.Any<WorkflowTaskOptions>())
            .Returns(call => new SetSourceInventorySchedulingActivity(inventory).RunAsync(null!, call.Arg<SourceInventorySchedulingInput>()));
        var failure = (WorkflowTaskFailedException)System.Runtime.CompilerServices.RuntimeHelpers.GetUninitializedObject(typeof(WorkflowTaskFailedException));
        context.CallActivityAsync<bool>(nameof(Hexalith.Memories.Server.Activities.Cases.ProjectAnnotationGraphActivity), Arg.Any<Hexalith.Memories.Server.Activities.Cases.AnnotationProjectionInput>(), Arg.Any<WorkflowTaskOptions>())
            .Returns(boundary == "graph" ? Task.FromException<bool>(failure) : Task.FromResult(true));
        context.CallChildWorkflowAsync<IngestionResult>(nameof(IngestionWorkflow), Arg.Any<IngestionInput>(), Arg.Any<ChildWorkflowTaskOptions>())
            .Returns(Task.FromException<IngestionResult>(failure));
        if (boundary == "preparing")
        {
            await new AdmitSourceInventoryActivity(inventory, Substitute.For<IDaprWorkflowClient>()).RunAsync(null!, new("annotation", Input(tenant) with { SourceUri = input.SourceUri }));
        }
        else
        {
            await Should.ThrowAsync<WorkflowTaskFailedException>(() => new AnnotationProjectionWorkflow().RunAsync(context, input));
        }
        SourceInventoryEntry retained = (await inventory.ReadAsync(tenant, input.SourceUri, null, 100, token)).Entries.ShouldHaveSingleItem();
        retained.SchedulingState.ShouldBe(boundary == "graph" ? "Unscheduled" : boundary == "child" ? "Scheduling" : "Preparing");
        if (boundary == "graph")
        { await context.DidNotReceiveWithAnyArgs().CallChildWorkflowAsync<IngestionResult>(default!, default!, default); }
        using IConnectionMultiplexer restartedConnection = await ConnectionMultiplexer.ConnectAsync(redis.ConnectionString);
        var restarted = new SourceInventoryStore(restartedConnection, codec);
        await restarted.FenceAsync(tenant, input.SourceUri, true, token);
        var (app, client) = await StartAsync(tenant, restarted, Payloads(new Dictionary<string, string>()), Substitute.For<IIngestionWorkflowStateReader>(), token);
        await using (app)
        using (client)
        {
            SourceInventoryPage page = (await client.GetFromJsonAsync<SourceInventoryPage>(Route(tenant, input.SourceUri), token))!;
            page.Entries.ShouldHaveSingleItem().TerminalVerified.ShouldBe(boundary == "graph");
            page.Entries.Single().State.ShouldBe(boundary == "graph" ? "Deleted" : "Admitted");
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ActualPostIndexDuplicateBoundaryRetainsAndReconcilesDiscardedArtifactsAfterFailedCompensation(bool loseOutputAcknowledgement)
    {
        CancellationToken token = TestContext.Current.CancellationToken;
        string tenant = "duplicate-loser-" + Guid.NewGuid().ToString("N");
        await OwnedSemanticInventory.ReconcileAsync(redis.Connection.GetDatabase(), tenant, "producer", "winner");
        IngestionInput input = Input(tenant) with { SourceInventoryTracked = true };
        var codec = new QueryCursorCodec(new EphemeralDataProtectionProvider(), "DiscardedProducerTests");
        var inventory = new SourceInventoryStore(redis.Connection, codec);
        Dictionary<string, string> bytes = new(StringComparer.Ordinal);
        IWorkflowPayloadStore payloads = Payloads(bytes);
        await inventory.AdmitAsync("producer", input, token);
        input = await IngestionPayloadClaimCheck.PrepareAsync(payloads, "producer", input, token);
        await inventory.SetSchedulingAsync(tenant, input.SourceUri, "producer", "Scheduling", token);
        await new RecordSourceInventoryCandidateActivity(inventory).RunAsync(null!, new(tenant, input.SourceUri, "producer", "case", "producer"));
        IDatabase db = redis.Connection.GetDatabase();
        await db.HashSetAsync(tenant + ":mu:producer", [new("content", "synthetic discarded content")]);
        await db.HashSetAsync(tenant + ":vec:producer", [new("memoryUnitId", "producer")]);
        await db.HashSetAsync(tenant + ":vec:producer:0", [new("memoryUnitId", "producer")]);
        await db.HashSetAsync(tenant + ":mu:winner", [new("content", "synthetic canonical survivor")]);
        await db.HashSetAsync(tenant + ":mu:unrelated", [new("content", "unrelated survivor")]);
        await new NFalkorDB.FalkorDB(graph.Connection.GetDatabase()).SelectGraph(tenant)
            .QueryAsync("CREATE (:MemoryUnit {id:'producer',content:'discarded'}),(:MemoryUnit {id:'winner'}),(:MemoryUnit {id:'unrelated'})");
        await new PersistFailedUnitActivity(redis.Connection, NullLogger<PersistFailedUnitActivity>.Instance).RunAsync(null!,
            new(tenant, "case", "producer", input.SourceUri, SourceType.File, "test", "text/plain", "indexing", "synthetic", "synthetic failure", 1,
                DateTimeOffset.UtcNow, DateTimeOffset.UtcNow, input.PayloadReference));
        WorkflowContext context = Substitute.For<WorkflowContext>();
        context.InstanceId.Returns("producer"); context.CurrentUtcDateTime.Returns(DateTime.UtcNow);
        foreach (string cleanup in new[] { "CleanupSyntacticActivity", "CleanupSemanticActivity", "CleanupGraphActivity" })
        {
            context.CallActivityAsync<bool>(cleanup, Arg.Any<CleanupInput>(), Arg.Any<WorkflowTaskOptions>())
                .Returns(Task.FromException<bool>(new InvalidOperationException("exhausted loser compensation")));
        }
        context.CallActivityAsync<bool>(nameof(RecordSourceInventoryOutputActivity), Arg.Any<SourceInventoryOutputInput>(), Arg.Any<WorkflowTaskOptions>())
            .Returns(async call =>
            {
                await new RecordSourceInventoryOutputActivity(inventory, new RefreshPublicationStore(redis.Connection, inventory, graph.Connection)).RunAsync(null!, call.Arg<SourceInventoryOutputInput>());
                if (loseOutputAcknowledgement) { throw new InvalidOperationException("lost durable output acknowledgement"); }
                return true;
            });
        MethodInfo boundary = typeof(IngestionWorkflow).GetMethod("CompletePostIndexDuplicateAsync", BindingFlags.Static | BindingFlags.NonPublic)!;
        Task<IngestionResult> boundaryTask = (Task<IngestionResult>)boundary.Invoke(null, [context, "producer", "winner", null,
            new CleanupInput("producer", tenant), Array.Empty<WorkflowPayloadReference>(), new WorkflowTaskOptions(),
            NullLogger.Instance, (Func<string, string, Task<bool>>)((_, _) => Task.FromResult(true)), input])!;
        IngestionResult? result = null;
        if (loseOutputAcknowledgement) { await Should.ThrowAsync<InvalidOperationException>(() => boundaryTask); }
        else { result = await boundaryTask; result.WasDuplicate.ShouldBeTrue(); result.MemoryUnitId.ShouldBe("winner"); }
        SourceInventoryEntry admitted = (await inventory.ReadAsync(tenant, input.SourceUri, null, 100, token)).Entries.ShouldHaveSingleItem();
        admitted.CandidateMemoryUnitId.ShouldBe("producer"); admitted.MemoryUnitId.ShouldBe("winner"); admitted.TerminalVerified.ShouldBeFalse();
        (await db.KeyExistsAsync(tenant + ":mu:producer")).ShouldBeTrue(); bytes.ShouldHaveSingleItem();
        var workflows = Substitute.For<IIngestionWorkflowStateReader>();
        workflows.GetWorkflowStateAsync("producer", true, Arg.Any<CancellationToken>()).Returns(State(input, loseOutputAcknowledgement ? WorkflowRuntimeStatus.Failed : WorkflowRuntimeStatus.Completed, result));
        using IConnectionMultiplexer restartedConnection = await ConnectionMultiplexer.ConnectAsync(redis.ConnectionString);
        var restarted = new SourceInventoryStore(restartedConnection, codec);
        await restarted.FenceAsync(tenant, input.SourceUri, true, token);
        var (app, client) = await StartAsync(tenant, restarted, payloads, workflows, token);
        await using (app)
        using (client)
        {
            SourceInventoryPage page = (await client.GetFromJsonAsync<SourceInventoryPage>(Route(tenant, input.SourceUri), token))!;
            SourceInventoryEntry terminal = page.Entries.ShouldHaveSingleItem();
            terminal.TerminalVerified.ShouldBeTrue(); terminal.CandidateArtifactsDeleted.ShouldBeTrue(); terminal.State.ShouldBe("Completed");
            (await db.KeyExistsAsync(tenant + ":mu:producer")).ShouldBeFalse();
            (await db.KeyExistsAsync(tenant + ":vec:producer")).ShouldBeFalse();
            (await db.KeyExistsAsync(tenant + ":vec:producer:0")).ShouldBeFalse();
            (await db.KeyExistsAsync(PersistFailedUnitActivity.BuildHashKey(tenant, "producer"))).ShouldBeFalse();
            (await db.SortedSetScoreAsync(PersistFailedUnitActivity.BuildSortedSetKey(tenant, "case"), "producer")).ShouldBeNull();
            bytes.ShouldBeEmpty();
            (await db.KeyExistsAsync(tenant + ":mu:winner")).ShouldBeTrue(); (await db.KeyExistsAsync(tenant + ":mu:unrelated")).ShouldBeTrue();
            (await new NFalkorDB.FalkorDB(graph.Connection.GetDatabase()).SelectGraph(tenant).QueryAsync("MATCH (m:MemoryUnit {id:'producer'}) RETURN m.id")).ShouldBeEmpty();
            await new Hexalith.Memories.Server.Activities.Cases.DeleteMemoryUnitProjectionActivity(restartedConnection, graph.Connection, new GraphQueryBuilder(), restarted, payloadStore: payloads, workflowClient: Substitute.For<IDaprWorkflowClient>())
                .RunAsync(null!, new(tenant, "case", "winner", []));
            (await client.GetFromJsonAsync<SourceInventoryPage>(Route(tenant, input.SourceUri), token))!.Entries.Single().State.ShouldBe("Deleted");
            (await db.KeyExistsAsync(tenant + ":mu:unrelated")).ShouldBeTrue();
        }
    }

    [Fact]
    public async Task CompensatedSupersededOutputBecomesTerminalDeletedAfterActualOutputActivityAndRecreation()
    {
        CancellationToken token = TestContext.Current.CancellationToken;
        string tenant = "superseded-output-" + Guid.NewGuid().ToString("N");
        await OwnedSemanticInventory.ReconcileAsync(redis.Connection.GetDatabase(), tenant, "loser");
        IngestionInput input = Input(tenant) with { SourceInventoryTracked = true };
        var codec = new QueryCursorCodec(new EphemeralDataProtectionProvider(), "SupersededOutputTests");
        var inventory = new SourceInventoryStore(redis.Connection, codec);
        await inventory.AdmitAsync("loser", input, token);
        await inventory.RecordCandidateAsync(tenant, input.SourceUri, "loser", "loser", token);
        await redis.Connection.GetDatabase().HashSetAsync(tenant + ":mu:loser", [new("content", "synthetic loser")]);
        await redis.Connection.GetDatabase().HashSetAsync(tenant + ":mu:unrelated", [new("content", "survivor")]);
        await new NFalkorDB.FalkorDB(graph.Connection.GetDatabase()).SelectGraph(tenant).QueryAsync("CREATE (:MemoryUnit {id:'loser'}),(:MemoryUnit {id:'unrelated'})");
        await new Hexalith.Memories.Server.Activities.Cases.DeleteMemoryUnitProjectionActivity(redis.Connection, graph.Connection, new GraphQueryBuilder(), inventory, workflowClient: Substitute.For<IDaprWorkflowClient>())
            .RunAsync(null!, new(tenant, input.CaseId, "loser", []));
        await new RecordSourceInventoryOutputActivity(inventory, new RefreshPublicationStore(redis.Connection, inventory, graph.Connection))
            .RunAsync(null!, new(tenant, input.SourceUri, "loser", input.CaseId, "loser"));
        var workflows = Substitute.For<IIngestionWorkflowStateReader>();
        workflows.GetWorkflowStateAsync("loser", true, Arg.Any<CancellationToken>()).Returns(State(input, WorkflowRuntimeStatus.Completed,
            new("loser", MemoryUnitStatus.Failed, DateTimeOffset.UtcNow, false, "source-refresh-superseded")));
        using IConnectionMultiplexer connection = await ConnectionMultiplexer.ConnectAsync(redis.ConnectionString);
        var restarted = new SourceInventoryStore(connection, codec);
        await restarted.FenceAsync(tenant, input.SourceUri, true, token);
        var (app, client) = await StartAsync(tenant, restarted, Payloads(new Dictionary<string, string>()), workflows, token);
        await using (app)
        using (client)
        {
            SourceInventoryEntry entry = (await client.GetFromJsonAsync<SourceInventoryPage>(Route(tenant, input.SourceUri), token))!.Entries.ShouldHaveSingleItem();
            entry.MemoryUnitId.ShouldBe("loser"); entry.State.ShouldBe("Deleted"); entry.TerminalVerified.ShouldBeTrue();
            (await connection.GetDatabase().KeyExistsAsync(tenant + ":mu:loser")).ShouldBeFalse();
            (await connection.GetDatabase().KeyExistsAsync(tenant + ":mu:unrelated")).ShouldBeTrue();
        }
    }

    private async Task<(WebApplication App, HttpClient Client)> StartAsync(string tenant, SourceInventoryStore inventory, IWorkflowPayloadStore payloads,
        IIngestionWorkflowStateReader workflows, CancellationToken token)
    {
        EnsureProvisionedSemanticIndex(tenant);
        await WaitForSemanticIndexAsync(tenant, token);
        WebApplicationBuilder builder = WebApplication.CreateBuilder();
        builder.Logging.ClearProviders();
        builder.WebHost.UseUrls("http://127.0.0.1:0");
        builder.Services.AddSingleton(inventory);
        builder.Services.AddSingleton(workflows);
        builder.Services.AddSingleton(new SourceInventoryFailureReconciler(redis.Connection, graph.Connection, new GraphQueryBuilder(), payloads, inventory, workflowClient: NSubstitute.Substitute.For<Dapr.Workflow.IDaprWorkflowClient>()));
        WebApplication app = builder.Build();
        app.Use((context, next) => { context.User = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ServerTenantClaimsTransformation.TenantClaimType, tenant)], "owned-test")); return next(context); });
        app.MapSourceInventoryEndpoints();
        await app.StartAsync(token);
        return (app, new HttpClient { BaseAddress = new Uri(app.Urls.Single()) });
    }

    private static string Route(string tenant, string source) => "/api/v1/tenants/" + tenant + "/source-inventory?sourceUri=" + Uri.EscapeDataString(source);

    private IngestionInput Input(string tenant)
    {
        EnsureProvisionedSemanticIndex(tenant);
        return new() { TenantId = tenant, CaseId = "case", SourceUri = "urn:synthetic:failed-producer", ContentBytes = [1, 2, 3], ContentType = "text/plain", SourceType = SourceType.File, IngestedBy = "test" };
    }

    private void EnsureProvisionedSemanticIndex(string tenant)
    {
        try { redis.Connection.GetDatabase().FT().Create(IndexSchemaDefinitions.GetSemanticIndexName(tenant),
            IndexSchemaDefinitions.CreateSemanticParams(tenant), IndexSchemaDefinitions.CreateSemanticSchema(3)); }
        catch (RedisServerException error) when (error.Message.Contains("Index already exists", StringComparison.OrdinalIgnoreCase)) { }
    }

    private async Task WaitForSemanticIndexAsync(string tenant, CancellationToken token)
    {
        using var ready = CancellationTokenSource.CreateLinkedTokenSource(token);
        ready.CancelAfter(TimeSpan.FromSeconds(10));
        while (true)
        {
            RedisResult[] info = (RedisResult[])(await redis.Connection.GetDatabase().ExecuteAsync("FT.INFO", IndexSchemaDefinitions.GetSemanticIndexName(tenant)))!;
            int indexing = Array.FindIndex(info, value => value.ToString() == "indexing");
            if (indexing >= 0 && info[indexing + 1].ToString() == "0") return;
            await Task.Delay(25, ready.Token);
        }
    }

    private IWorkflowPayloadStore Payloads(Dictionary<string, string> bytes, bool failSave = false, Func<bool>? failSaveWhen = null)
    {
        var store = Substitute.For<IWorkflowPayloadStore>();
        store.SaveAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<WorkflowPayloadKind>(), Arg.Any<ReadOnlyMemory<byte>>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                var input = Input(call.ArgAt<string>(0)) with { ContentBytes = call.Arg<ReadOnlyMemory<byte>>().ToArray() };
                WorkflowPayloadReference reference = IngestionPayloadClaimCheck.GetSourceReference(call.ArgAt<string>(1), input)!;
                bytes[reference.Id] = JsonSerializer.Serialize(input.ContentBytes);
                if (failSave || failSaveWhen?.Invoke() == true) { throw new InvalidOperationException("lost payload save acknowledgement"); }
                return reference;
            });
        store.DeleteAsync(Arg.Any<WorkflowPayloadReference>(), Arg.Any<CancellationToken>()).Returns(call => { bytes.Remove(call.Arg<WorkflowPayloadReference>().Id); return Task.CompletedTask; });
        store.ReadAsync(Arg.Any<WorkflowPayloadReference>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<WorkflowPayloadKind>(), Arg.Any<CancellationToken>())
            .Returns(call => bytes.TryGetValue(call.Arg<WorkflowPayloadReference>().Id, out string? value)
                ? JsonSerializer.Deserialize<byte[]>(value)! : throw new WorkflowPayloadException("PAYLOAD_NOT_FOUND", call.Arg<WorkflowPayloadReference>().Id));
        return store;
    }

    private static WorkflowState State(IngestionInput input, WorkflowRuntimeStatus status, IngestionResult? result = null, string instanceId = "producer")
    {
        var serializer = new JsonDaprSerializer(MemoriesJsonContext.Options);
        Type metadataType = Type.GetType("Dapr.Workflow.Client.WorkflowMetadata, Dapr.Workflow", true)!;
        object metadata = Activator.CreateInstance(metadataType, instanceId, nameof(IngestionWorkflow), status, DateTime.UtcNow, DateTime.UtcNow, serializer)!;
        metadataType.GetProperty("SerializedInput")!.SetValue(metadata, serializer.Serialize(input));
        if (result is not null) { metadataType.GetProperty("SerializedOutput")!.SetValue(metadata, serializer.Serialize(result)); }
        return (WorkflowState)Activator.CreateInstance(typeof(WorkflowState), BindingFlags.Instance | BindingFlags.NonPublic, null, [metadata], null)!;
    }
}
