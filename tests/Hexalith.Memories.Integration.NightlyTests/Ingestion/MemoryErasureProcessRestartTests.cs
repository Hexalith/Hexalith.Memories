namespace Hexalith.Memories.IntegrationTests.Ingestion;

using System.Security.Cryptography;
using System.Text.Json;
using Dapr.Workflow;
using Hexalith.EventStore.Client.Queries;
using Hexalith.Memories.Contracts.V1;
using Hexalith.Memories.IntegrationTests.Fixtures;
using Hexalith.Memories.Server.Activities.Ingestion;
using Hexalith.Memories.Server.Infrastructure;
using Hexalith.Memories.Server.Ingestion;
using Hexalith.Memories.Server.NaturalLanguage;
using Hexalith.Memories.Server.Workflows;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NRedisStack.RedisStackCommands;
using NSubstitute;
using Shouldly;
using StackExchange.Redis;

/// <summary>Real worker process replacement with parent-held backing resources and independent persisted-state reads.</summary>
[Collection("RedisStack")]
[Trait("Category", "Integration")]
[Trait("Category", "IntegrationSlow")]
public sealed class MemoryErasureProcessRestartTests(RedisStackFixture redis, FalkorDbFixture graph, ITestOutputHelper output)
    : IAsyncLifetime, IClassFixture<FalkorDbFixture>
{
    private readonly DaprStateSidecarFixture _sidecar = DaprStateSidecarFixture.CreateWorkflow(redis.ConnectionString, authenticate: true);
    private readonly List<JsonElement> _incarnations = [];
    private readonly List<MemoryErasureProcessProtocol> _workers = [];
    private readonly List<object> _checkpoints = [];
    private string? _previousToken;
    private string? _previousEndpoint;
    private string? _custody;
    private bool _custodyVerified;

    /// <inheritdoc />
    public async ValueTask InitializeAsync()
    {
        _previousToken = Environment.GetEnvironmentVariable("DAPR_API_TOKEN");
        _previousEndpoint = Environment.GetEnvironmentVariable("DAPR_GRPC_ENDPOINT");
        await _sidecar.InitializeAsync();
        Environment.SetEnvironmentVariable("DAPR_API_TOKEN", _sidecar.ApiToken);
        Environment.SetEnvironmentVariable("DAPR_GRPC_ENDPOINT", _sidecar.DaprGrpcEndpoint);
        _custody = await CustodyAsync();
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        try
        {
            if (_custody is not null) (await CustodyAsync()).ShouldBe(_custody);
            _workers.ShouldAllBe(worker => worker.Exited);
            _custodyVerified = _custody is not null;
        }
        finally
        {
            try { await _sidecar.StopObservedAsync(); }
            finally
            {
                try { await _sidecar.DisposeAsync(); }
                finally
                {
                    Environment.SetEnvironmentVariable("DAPR_API_TOKEN", _previousToken);
                    Environment.SetEnvironmentVariable("DAPR_GRPC_ENDPOINT", _previousEndpoint);
                    await SaveReceiptAsync();
                }
            }
        }
    }

    private async Task SaveReceiptAsync()
    {
        object receipt = new { ObservedExits = _workers.Select(worker => new { worker.Identity, worker.Exited }), Incarnations = _incarnations,
            Checkpoints = _checkpoints, BackingCustody = _custody, BackingCustodyUnchanged = _custodyVerified, SidecarExitObserved = _sidecar.ProcessExitObserved };
        string json = JsonSerializer.Serialize(receipt);
        output.WriteLine(json);
        if (Environment.GetEnvironmentVariable("MEMORIES_ERASURE_RECEIPT_DIR") is { Length: > 0 } directory)
        {
            Directory.CreateDirectory(directory);
            await File.WriteAllTextAsync(Path.Combine(directory, "process-" + Guid.NewGuid().ToString("N") + ".json"), json);
        }
    }

    /// <summary>Both victim namespaces stay absent through two worker replacements while all survivor bytes remain unchanged.</summary>
    /// <param name="caseDeletion">Whether the actual case deletion caller performs erasure.</param>
    [Theory(Timeout = 180000)]
    [InlineData(false)]
    [InlineData(true)]
    public async Task MigratedLegacyNamespacesRemainErasedAcrossWorkerReplacement(bool caseDeletion)
    {
        CancellationToken token = TestContext.Current.CancellationToken;
        string tenant = "process-migration-" + Guid.NewGuid().ToString("N");
        IDatabase db = redis.Connection.GetDatabase();
        foreach (string unit in new[] { "victim", "survivor" })
        {
            await db.HashSetAsync(IndexSchemaDefinitions.BuildSyntacticKey(tenant, unit), Owner(tenant, unit));
            await db.HashSetAsync(IndexSchemaDefinitions.BuildLegacyNaturalLanguageSemanticKey(tenant, unit), Fields(tenant, unit));
        }
        string unknown = IndexSchemaDefinitions.BuildLegacyNaturalLanguageSemanticKey(tenant, "unknown");
        await db.HashSetAsync(unknown, Fields(tenant, "unknown"));
        string survivor = await HashesAsync(tenant, "survivor");
        string unknownBefore = await HashesAsync(tenant, "unknown");
        await using (var original = await StartAsync("modern", token))
        {
            (await Should.ThrowAsync<InvalidOperationException>(() => original.CallAsync("migrate", new { Tenant = tenant, Unit = "unknown" }, token)))
                .Message.ShouldBe("nl-namespace-live-owner-reconciliation-required");
            await original.CallAsync("migrate", new { Tenant = tenant, Unit = "victim" }, token);
            (await db.KeyExistsAsync(IndexSchemaDefinitions.BuildLegacyNaturalLanguageSemanticKey(tenant, "victim"))).ShouldBeFalse();
            (await db.HashGetAllAsync(IndexSchemaDefinitions.BuildNaturalLanguageSemanticKey(tenant, "victim"))).ShouldBe(Fields(tenant, "victim"), ignoreOrder: true);
            _checkpoints.Add(new { Boundary = "migration-persisted-before-original-exit", Victim = await HashesAsync(tenant, "victim"), Survivor = survivor });
        }
        await using (var replacement = await StartAsync("modern", token))
            await replacement.CallAsync("delete", new { Tenant = tenant, Unit = "victim", Reconcile = true, CaseDeletion = caseDeletion }, token);
        await using (var late = await StartAsync("modern", token))
            await late.CallAsync("migrate", new { Tenant = tenant, Unit = "victim" }, token);
        await AssertErasedAsync(tenant, "victim");
        (await HashesAsync(tenant, "survivor")).ShouldBe(survivor);
        (await HashesAsync(tenant, "unknown")).ShouldBe(unknownBefore);
        _checkpoints.Add(new { Boundary = "after-erasure-and-late-migration", VictimAbsent = true, Survivor = survivor, Unknown = unknownBefore });
    }

    /// <summary>Actual historical chunks survive real workflow purge, process exit, and attempted identity reuse without acquiring authority.</summary>
    /// <param name="crossSource">Whether source B is independently complete before attempting the old identity.</param>
    [Theory(Timeout = 180000)]
    [InlineData(false)]
    [InlineData(true)]
    public async Task PurgedHistoricalIdentityCannotGainCompletenessAfterProcessReplacement(bool crossSource)
    {
        CancellationToken token = TestContext.Current.CancellationToken;
        string tenant = "process-purge-" + Guid.NewGuid().ToString("N");
        string unit = "old-" + Guid.NewGuid().ToString("N");
        IDatabase db = redis.Connection.GetDatabase();
        CreateChunkIndex(db, tenant);
        using ServiceProvider services = ClientServices();
        var client = services.GetRequiredService<DaprWorkflowClient>();
        await using (var historical = await StartAsync("historical", token))
        {
            await historical.CallAsync("historical", new { Tenant = tenant, Unit = unit }, token);
            await historical.CallAsync("historical", new { Tenant = tenant, Unit = "survivor" }, token);
            RedisKey[] retainedHistory = await WorkflowKeysAsync(unit, token);
            retainedHistory.ShouldNotBeEmpty();
            _checkpoints.Add(new { Boundary = "actual-completed-history-before-purge", History = await DumpsAsync(retainedHistory) });
            await client.PurgeInstanceAsync(unit, token);
            (await client.GetWorkflowStateAsync(unit, true, token)).Exists.ShouldBeFalse();
            (await WorkflowKeysAsync(unit, token)).ShouldBeEmpty();
            foreach (RedisKey key in retainedHistory) (await db.KeyExistsAsync(key)).ShouldBeFalse();
        }
        await AssertHistoricalArtifactsAsync(tenant, unit);
        await AssertHistoricalArtifactsAsync(tenant, "survivor");
        string before = await HashesAsync(tenant, unit);
        string survivor = await HashesAsync(tenant, "survivor");
        _checkpoints.Add(new { Boundary = "actual-workflow-purged-before-replacement", Victim = before, Survivor = survivor });
        await using var replacement = await StartAsync("modern", token);
        var inventory = Inventory();
        IngestionInput input = Input(tenant) with { SourceUri = crossSource ? "https://synthetic.invalid/source-b" : Input(tenant).SourceUri };
        if (crossSource)
        {
            await inventory.FenceAsync(tenant, input.SourceUri, false, token);
            var fenced = await inventory.ReadAsync(tenant, input.SourceUri, null, 100, token);
            await inventory.ReconcileAsync(tenant, input.SourceUri, fenced.Version, [], "owned-source-b-only", token);
            var reconciled = await inventory.ReadAsync(tenant, input.SourceUri, null, 100, token);
            await inventory.ReopenAsync(tenant, input.SourceUri, reconciled.Version, token);
        }
        (await Scheduler(client, inventory).ScheduleAsync(unit, input, token)).ShouldBe(unit);
        WorkflowState result = await client.WaitForWorkflowCompletionAsync(unit, true, token);
        result.RuntimeStatus.ShouldBe(WorkflowRuntimeStatus.Failed);
        JsonSerializer.Serialize(result).ShouldContain("source-publication-existing-candidate");
        await AssertIncompleteAsync(replacement, tenant, unit, before, survivor, token);
    }

    /// <summary>The older process completes after a real absent lookup and exits before ordinary or annotation admission.</summary>
    /// <param name="annotation">Whether to exercise the actual annotation admission activity.</param>
    [Theory(Timeout = 180000)]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ActualOlderProducerCompletesBetweenAbsentReadAndAdmissionAcrossReplacement(bool annotation)
    {
        CancellationToken token = TestContext.Current.CancellationToken;
        string tenant = "process-concurrent-" + Guid.NewGuid().ToString("N");
        string unit = "old-" + Guid.NewGuid().ToString("N");
        IDatabase db = redis.Connection.GetDatabase();
        CreateChunkIndex(db, tenant);
        JsonElement observation;
        await using (var historical = await StartAsync("historical", token))
        {
            observation = await historical.CallAsync("observe-absence", new { Tenant = tenant, Unit = unit }, token);
            observation.GetProperty("Exists").GetBoolean().ShouldBeFalse();
            observation.GetProperty("Pid").GetInt32().ShouldBe(historical.Identity.GetProperty("Pid").GetInt32());
            observation.GetProperty("StartTicks").GetInt64().ShouldBe(historical.Identity.GetProperty("StartTicks").GetInt64());
            _checkpoints.Add(new { Boundary = "actual-absent-history-read", Unit = unit, Observation = observation });
            await historical.CallAsync("historical", new { Tenant = tenant, Unit = unit }, token);
            await historical.CallAsync("historical", new { Tenant = tenant, Unit = "survivor" }, token);
        }
        await AssertHistoricalArtifactsAsync(tenant, unit);
        await AssertHistoricalArtifactsAsync(tenant, "survivor");
        string before = await HashesAsync(tenant, unit);
        string survivor = await HashesAsync(tenant, "survivor");
        _checkpoints.Add(new { Boundary = "older-producer-completed-and-exited-before-admission", Victim = before, Survivor = survivor });
        await using var replacement = await StartAsync("modern", token);
        string observationBytes = (await db.StringGetAsync(observation.GetProperty("Key").GetString()!)).ToString();
        Digest(observationBytes).ShouldBe(observation.GetProperty("Sha256").GetString());
        JsonElement admission = await replacement.CallAsync("admit-raced", new { Tenant = tenant, Unit = unit, Annotation = annotation,
            ObservationKey = observation.GetProperty("Key").GetString(), ObservationSha256 = observation.GetProperty("Sha256").GetString(),
            ObservationPid = observation.GetProperty("Pid").GetInt32(), ObservationStartTicks = observation.GetProperty("StartTicks").GetInt64() }, token);
        admission.GetProperty("Pid").GetInt32().ShouldBe(replacement.Identity.GetProperty("Pid").GetInt32());
        admission.GetProperty("StartTicks").GetInt64().ShouldBe(replacement.Identity.GetProperty("StartTicks").GetInt64());
        admission.GetProperty("ObservationSha256").GetString().ShouldBe(observation.GetProperty("Sha256").GetString());
        admission.GetProperty("ObservationReads").GetInt32().ShouldBe(1);
        admission.GetProperty("SchedulingAttempts").GetInt32().ShouldBe(1);
        admission.GetProperty("SchedulingDisposition").GetString().ShouldBeOneOf("accepted", "duplicate-address-refused");
        admission.GetProperty("TerminalStatus").GetString().ShouldBe(admission.GetProperty("SchedulingDisposition").GetString() == "accepted" ? "Failed" : "Completed");
        admission.GetProperty("CandidateRefused").GetBoolean().ShouldBeTrue();
        admission.GetProperty("Admission").GetProperty("WorkflowInstanceId").GetString().ShouldBe(unit);
        string retainedAdmission = (await db.StringGetAsync(SourceInventoryStore.BuildKey(tenant, Input(tenant).SourceUri))).ToString();
        Digest(retainedAdmission).ShouldBe(admission.GetProperty("AdmissionSha256").GetString());
        _checkpoints.Add(new { Boundary = "replacement-worker-admission-and-real-scheduling", Evidence = admission });
        await AssertIncompleteAsync(replacement, tenant, unit, before, survivor, token);
    }

    /// <summary>Original nine-field raw retry history is suspended at an actual content activity before worker exit and scoped erasure.</summary>
    /// <param name="embeddingPhase">Whether the original embedding activity is interrupted instead of description.</param>
    [Theory(Timeout = 180000)]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ContentBearingRetryHistoryAndPayloadStayErasedAcrossWorkerReplacement(bool embeddingPhase)
    {
        CancellationToken token = TestContext.Current.CancellationToken;
        string tenant = "process-retry-" + Guid.NewGuid().ToString("N");
        IDatabase db = redis.Connection.GetDatabase();
        using ServiceProvider services = ClientServices();
        var client = services.GetRequiredService<DaprWorkflowClient>();
        var registry = new FailedNaturalLanguageEmbeddingRegistry(redis.Connection, NullLogger<FailedNaturalLanguageEmbeddingRegistry>.Instance);
        TenantEmbeddingConfig config = EmbeddingProviderDefaults.Google();
        db.FT().Create(IndexSchemaDefinitions.GetNaturalLanguageSemanticIndexName(tenant), IndexSchemaDefinitions.CreateNaturalLanguageSemanticParams(tenant), IndexSchemaDefinitions.CreateNaturalLanguageSemanticSchema(config.Dimensions));
        foreach (string unit in new[] { "victim", "survivor" })
        {
            await db.HashSetAsync(IndexSchemaDefinitions.BuildSyntacticKey(tenant, unit), Owner(tenant, unit));
            var record = new FailedNaturalLanguageEmbeddingRecord(tenant, unit, "raw-process-content-" + unit, "TestEvent", null, "case", config.Provider, config.Model, config.Dimensions, 1, 0);
            await registry.EnqueueAsync(record, token);
            (await registry.IncrementAttemptsAsync(record, 1, token)).ShouldBeTrue();
            await registry.EnqueueAsync(record, token);
            // Retain the authentic content-bearing legacy sorted-member representation alongside both modern payload hashes.
            await db.SortedSetAddAsync(FailedNaturalLanguageEmbeddingRegistry.LiveKey(tenant), FailedNaturalLanguageEmbeddingRegistry.SerializeRecord(record with { Attempts = 9 }), 2);
            await db.SortedSetAddAsync(FailedNaturalLanguageEmbeddingRegistry.DeadKey(tenant), FailedNaturalLanguageEmbeddingRegistry.SerializeRecord(record with { Attempts = 10 }), 3);
        }
        await db.HashSetAsync(IndexSchemaDefinitions.BuildNaturalLanguageSemanticKey(tenant, "victim"), Fields(tenant, "victim"));
        string survivorBefore = await HashesAsync(tenant, "survivor");
        string survivorQueue = await QueueStateAsync(tenant, "survivor");
        string victimQueue = await QueueStateAsync(tenant, "victim");
        FailedNaturalLanguageEmbeddingRecord admittedVictim = (await FailedNaturalLanguageEmbeddingRegistry.ReadCurrentAsync(db, tenant, "victim")).ShouldNotBeNull();
        var retainedRetry = new NaturalLanguageEmbeddingRetryInput(tenant, "victim", string.Empty, admittedVictim.EventType,
            admittedVictim.AggregateType, admittedVictim.CaseId, admittedVictim.EmbeddingProvider, admittedVictim.EmbeddingModel, admittedVictim.EmbeddingDimensions)
            { PayloadVersion = FailedNaturalLanguageEmbeddingRegistry.PayloadVersion(admittedVictim) };
        retainedRetry.RawJsonPayload.ShouldBeEmpty();
        retainedRetry.PayloadVersion.ShouldNotBeNullOrWhiteSpace();
        string progressTenant = "process-retry-progress-" + Guid.NewGuid().ToString("N");
        db.FT().Create(IndexSchemaDefinitions.GetNaturalLanguageSemanticIndexName(progressTenant), IndexSchemaDefinitions.CreateNaturalLanguageSemanticParams(progressTenant), IndexSchemaDefinitions.CreateNaturalLanguageSemanticSchema(config.Dimensions));
        await db.HashSetAsync(IndexSchemaDefinitions.BuildSyntacticKey(progressTenant, "progress"), Owner(progressTenant, "progress"));
        await registry.EnqueueAsync(new(progressTenant, "progress", "unrelated-modern-progress", "TestEvent", null, "case", config.Provider, config.Model, config.Dimensions, 1, 0), token);
        string id = NaturalLanguageEmbeddingRetryHostedService.GetLegacyRetryWorkflowInstanceId(tenant, "victim");
        RedisKey[] historicalKeys;
        await using (var original = await StartAsync(embeddingPhase ? "legacy-embedding" : "legacy-description", token))
        {
            await client.ScheduleNewWorkflowAsync(typeof(NaturalLanguageEmbeddingRetryWorkflow).Name, id, JsonSerializer.SerializeToElement(new { TenantId = tenant, MemoryUnitId = "victim", RawJsonPayload = "raw-process-content-victim", EventType = "TestEvent", AggregateType = (string?)null, CaseId = "case", EmbeddingProvider = config.Provider, EmbeddingModel = config.Model, EmbeddingDimensions = config.Dimensions }), null, token);
            using var phase = CancellationTokenSource.CreateLinkedTokenSource(token); phase.CancelAfter(TimeSpan.FromSeconds(45));
            while ((await original.CallAsync("boundary", new { }, phase.Token)).GetProperty("Boundary").GetString() != (embeddingPhase ? "embedding" : "description")) await Task.Delay(25, phase.Token);
            await client.SuspendWorkflowAsync(id, "owned-process-content-boundary", token);
            while ((await client.GetWorkflowStateAsync(id, true, token)).RuntimeStatus != WorkflowRuntimeStatus.Suspended) await Task.Delay(25, phase.Token);
            var state = await client.GetWorkflowStateAsync(id, true, token);
            state.ReadInputAs<JsonElement>().EnumerateObject().Count().ShouldBe(9);
            state.ReadInputAs<NaturalLanguageEmbeddingRetryInput>()!.RawJsonPayload.ShouldBe("raw-process-content-victim");
            historicalKeys = await WorkflowKeysAsync(id, token); historicalKeys.ShouldNotBeEmpty();
            _checkpoints.Add(new { Boundary = embeddingPhase ? "actual-embedding-suspended" : "actual-description-suspended", History = await DumpsAsync(historicalKeys), Victim = await HashesAsync(tenant, "victim"), VictimQueues = victimQueue, RetainedContentFreeRetry = retainedRetry, Survivor = survivorBefore, SurvivorQueues = survivorQueue });
        }
        await using (var upgraded = await StartAsync("modern", token))
        {
            (await client.GetWorkflowStateAsync(id, true, token)).RuntimeStatus.ShouldBe(WorkflowRuntimeStatus.Suspended);
            (await HashesAsync(tenant, "survivor")).ShouldBe(survivorBefore);
            (await QueueStateAsync(tenant, "survivor")).ShouldBe(survivorQueue);
            await upgraded.CallAsync("delete", new { Tenant = tenant, Unit = "victim", Reconcile = true, CaseDeletion = false }, token);
            await AssertErasedAsync(tenant, "victim");
            foreach (RedisKey key in historicalKeys) (await db.KeyExistsAsync(key)).ShouldBeFalse();
            (await client.GetWorkflowStateAsync(id, true, token)).Exists.ShouldBeFalse();
            await upgraded.CallAsync("retry", new { Tenant = progressTenant, Unit = "progress" }, token);
            string modern = NaturalLanguageEmbeddingRetryHostedService.GetRetryWorkflowInstanceId(progressTenant, "progress");
            var completed = await client.WaitForWorkflowCompletionAsync(modern, true, token);
            completed.RuntimeStatus.ShouldBe(WorkflowRuntimeStatus.Completed);
            completed.ReadInputAs<NaturalLanguageEmbeddingRetryInput>()!.RawJsonPayload.ShouldBeEmpty();
            completed.ReadOutputAs<NaturalLanguageEmbeddingRetryResult>()!.Indexed.ShouldBeTrue();
            (await db.HashGetAsync(IndexSchemaDefinitions.BuildNaturalLanguageSemanticKey(progressTenant, "progress"), "naturalLanguageDescription")).ToString().ShouldBe("Modern process retry succeeded.");
        }
        (await HashesAsync(tenant, "survivor")).ShouldBe(survivorBefore);
        (await QueueStateAsync(tenant, "survivor")).ShouldBe(survivorQueue);
        string survivorAfter = await HashesAsync(tenant, "survivor");
        string progressAfter = await HashesAsync(progressTenant, "progress");
        await using (var restarted = await StartAsync("modern", token))
        {
            JsonElement late = await restarted.CallAsync("late-retry-activity", new { Tenant = tenant, Unit = "victim", Input = retainedRetry }, token);
            late.GetProperty("Pid").GetInt32().ShouldBe(restarted.Identity.GetProperty("Pid").GetInt32());
            late.GetProperty("StartTicks").GetInt64().ShouldBe(restarted.Identity.GetProperty("StartTicks").GetInt64());
            late.GetProperty("Indexed").GetBoolean().ShouldBeFalse();
            late.GetProperty("Reason").GetString().ShouldBe("memory-unit-deleted-during-retry");
            late.GetProperty("PayloadVersion").GetString().ShouldBe(retainedRetry.PayloadVersion);
            _checkpoints.Add(new { Boundary = "retained-pre-erasure-retry-activity-refused", Result = late });
            await AssertErasedAsync(tenant, "victim");
            (await WorkflowKeysAsync(id, token)).ShouldBeEmpty();
            string modernVictim = NaturalLanguageEmbeddingRetryHostedService.GetRetryWorkflowInstanceId(tenant, "victim");
            (await client.GetWorkflowStateAsync(modernVictim, true, token)).Exists.ShouldBeFalse();
            (await WorkflowKeysAsync(modernVictim, token)).ShouldBeEmpty();
            (await HashesAsync(tenant, "survivor")).ShouldBe(survivorAfter);
            (await QueueStateAsync(tenant, "survivor")).ShouldBe(survivorQueue);
            (await HashesAsync(progressTenant, "progress")).ShouldBe(progressAfter);
            _checkpoints.Add(new { Boundary = "purged-history-and-late-retry-after-second-replacement", VictimAbsent = true, Survivor = survivorAfter, SurvivorQueues = survivorQueue, UnrelatedProgress = progressAfter });
        }
    }

    private async Task<MemoryErasureProcessProtocol> StartAsync(string role, CancellationToken token)
    {
        _workers.ShouldAllBe(previous => previous.Exited);
        var worker = await MemoryErasureProcessProtocol.StartAsync(token);
        _workers.Add(worker);
        try
        {
            await worker.CallAsync("start", new { Redis = redis.ConnectionString, Graph = graph.ConnectionString, Role = role }, token);
            _incarnations.ShouldNotContain(item => item.GetProperty("Pid").GetInt32() == worker.Identity.GetProperty("Pid").GetInt32()
                && item.GetProperty("StartTicks").GetInt64() == worker.Identity.GetProperty("StartTicks").GetInt64());
            if (_incarnations.Count != 0) worker.Identity.GetProperty("Assemblies").GetRawText().ShouldBe(_incarnations[0].GetProperty("Assemblies").GetRawText());
            _incarnations.Add(worker.Identity);
            (await CustodyAsync()).ShouldBe(_custody);
            return worker;
        }
        catch { await worker.DisposeAsync(); throw; }
    }

    private async Task AssertIncompleteAsync(MemoryErasureProcessProtocol worker, string tenant, string unit, string before, string survivor, CancellationToken token)
    {
        using var connection = await ConnectionMultiplexer.ConnectAsync(redis.ConnectionString);
        var failure = await Should.ThrowAsync<InvalidOperationException>(() => SemanticKeyDiscovery.FindAsync(connection.GetDatabase(), tenant, unit, token));
        failure.Message.ShouldBe("source-publication-legacy-owner-reconciliation-required");
        (await Should.ThrowAsync<InvalidOperationException>(() => worker.CallAsync("delete", new { Tenant = tenant, Unit = unit, Reconcile = false, CaseDeletion = false }, token)))
            .Message.ShouldContain("source-publication-legacy-owner-reconciliation-required");
        (await HashesAsync(tenant, unit)).ShouldBe(before);
        (await HashesAsync(tenant, "survivor")).ShouldBe(survivor);
        (await connection.GetDatabase().KeyExistsAsync(UnitOwnershipAuthority.Key(tenant, unit))).ShouldBeFalse();
        (await connection.GetDatabase().KeyExistsAsync(SourcePublicationVisibility.KeyProvenance(tenant, unit))).ShouldBeFalse();
        _checkpoints.Add(new { Boundary = "incomplete-erasure-refused-after-replacement", Victim = before, Survivor = survivor, AuthorityAbsent = true });
    }

    private async Task AssertErasedAsync(string tenant, string unit)
    {
        using var independent = await ConnectionMultiplexer.ConnectAsync(redis.ConnectionString);
        IDatabase db = independent.GetDatabase();
        foreach (string key in Keys(tenant, unit)) (await db.KeyExistsAsync(key)).ShouldBeFalse();
        (await db.StringGetAsync(SourcePublicationVisibility.Tombstone(tenant, unit))).ToString().ShouldBe("1");
        (await db.KeyTimeToLiveAsync(SourcePublicationVisibility.Tombstone(tenant, unit))).ShouldBeNull();
        (await db.HashGetAsync(FailedNaturalLanguageEmbeddingRegistry.LivePayloadKey(tenant), unit)).HasValue.ShouldBeFalse();
        (await db.HashGetAsync(FailedNaturalLanguageEmbeddingRegistry.DeadPayloadKey(tenant), unit)).HasValue.ShouldBeFalse();
        foreach (string queue in new[] { FailedNaturalLanguageEmbeddingRegistry.LiveKey(tenant), FailedNaturalLanguageEmbeddingRegistry.DeadKey(tenant) })
        {
            foreach (SortedSetEntry entry in await db.SortedSetRangeByRankWithScoresAsync(queue))
            {
                entry.Element.ToString().ShouldNotBe(unit);
                var record = FailedNaturalLanguageEmbeddingRegistry.TryDeserialize(entry.Element.ToString());
                if (record is not null) (record.TenantId == tenant && record.MemoryUnitId == unit).ShouldBeFalse();
            }
        }
    }

    private async Task AssertHistoricalArtifactsAsync(string tenant, string unit)
    {
        using var independent = await ConnectionMultiplexer.ConnectAsync(redis.ConnectionString);
        IDatabase db = independent.GetDatabase();
        string chunk = IndexSchemaDefinitions.BuildSemanticChunkKey(tenant, unit, 7);
        string nl = IndexSchemaDefinitions.BuildNaturalLanguageSemanticKey(tenant, unit);
        byte[] expectedEmbedding = new[] { 1f, 0f, 0f }.SelectMany(value => BitConverter.GetBytes(value)).ToArray();
        foreach (string key in new[] { chunk, nl })
        {
            (await db.KeyExistsAsync(key)).ShouldBeTrue();
            (await db.HashGetAsync(key, new RedisValue[] { "tenantId", "memoryUnitId", "caseId", "embeddingProvider", "embeddingModel", "embeddingDimensions" }))
                .Select(value => value.ToString()).ShouldBe(new[] { tenant, unit, "case", "google", "synthetic", "3" });
            ((byte[])(await db.HashGetAsync(key, "embedding"))!).ShouldBe(expectedEmbedding);
        }
        (await db.HashGetAsync(chunk, "chunkText")).ToString().ShouldBe("owned historical chunk");
        (await db.HashGetAsync(chunk, "chunkSequence")).ToString().ShouldBe("7");
        (await db.HashGetAsync(nl, "naturalLanguageDescription")).ToString().ShouldBe("Owned actual historical process NL.");
        (await db.HashGetAsync(nl, "descriptionOrigin")).ToString().ShouldBe("ai");
        (await db.KeyExistsAsync(UnitOwnershipAuthority.Key(tenant, unit))).ShouldBeFalse();
        (await db.KeyExistsAsync(SourcePublicationVisibility.KeyProvenance(tenant, unit))).ShouldBeFalse();
    }

    private static string Digest(string value) => Convert.ToHexStringLower(SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(value)));

    private async Task<string> QueueStateAsync(string tenant, string unit)
    {
        using var independent = await ConnectionMultiplexer.ConnectAsync(redis.ConnectionString);
        IDatabase db = independent.GetDatabase();
        var rows = new List<object>();
        foreach (var (queue, payloads) in new[] { (FailedNaturalLanguageEmbeddingRegistry.LiveKey(tenant), FailedNaturalLanguageEmbeddingRegistry.LivePayloadKey(tenant)),
            (FailedNaturalLanguageEmbeddingRegistry.DeadKey(tenant), FailedNaturalLanguageEmbeddingRegistry.DeadPayloadKey(tenant)) })
        {
            var entries = new List<object>();
            foreach (SortedSetEntry entry in (await db.SortedSetRangeByRankWithScoresAsync(queue)).OrderBy(item => item.Element.ToString(), StringComparer.Ordinal))
            {
                var record = FailedNaturalLanguageEmbeddingRegistry.TryDeserialize(entry.Element.ToString());
                if (entry.Element.ToString() == unit || record is not null && record.TenantId == tenant && record.MemoryUnitId == unit)
                    entries.Add(new { MemberSha256 = Convert.ToHexStringLower(SHA256.HashData((byte[])entry.Element!)), entry.Score });
            }
            RedisValue payload = await db.HashGetAsync(payloads, unit);
            entries.Count.ShouldBe(2, "Each positive checkpoint contains both modern and legacy queue members.");
            payload.HasValue.ShouldBeTrue();
            rows.Add(new { Queue = queue, Entries = entries, PayloadSha256 = Convert.ToHexStringLower(SHA256.HashData((byte[])payload!)) });
        }
        return JsonSerializer.Serialize(rows);
    }

    private async Task<string> CustodyAsync()
    {
        var redisOptions = ConfigurationOptions.Parse(redis.ConnectionString); redisOptions.AllowAdmin = true;
        var graphOptions = ConfigurationOptions.Parse(graph.ConnectionString); graphOptions.AllowAdmin = true;
        using var redisIdentity = await ConnectionMultiplexer.ConnectAsync(redisOptions);
        using var graphIdentity = await ConnectionMultiplexer.ConnectAsync(graphOptions);
        var first = await redisIdentity.GetDatabase().ExecuteAsync("INFO", "server");
        var second = await graphIdentity.GetDatabase().ExecuteAsync("INFO", "server");
        string Id(RedisResult info) => info.ToString().Split('\n').Single(line => line.StartsWith("run_id:", StringComparison.Ordinal)).Trim();
        return JsonSerializer.Serialize(new { Redis = Id(first), RedisContainer = redis.ContainerIdentity, Graph = Id(second), GraphContainer = graph.ContainerIdentity, Sidecar = _sidecar.ProcessIdentity });
    }

    private async Task<string> HashesAsync(string tenant, string unit)
    {
        using var independent = await ConnectionMultiplexer.ConnectAsync(redis.ConnectionString);
        var rows = new List<object>();
        foreach (string key in Keys(tenant, unit))
        {
            HashEntry[] fields = await independent.GetDatabase().HashGetAllAsync(key);
            rows.Add(new { Key = key, Fields = fields.OrderBy(field => field.Name.ToString(), StringComparer.Ordinal)
                .Select(field => new { Name = field.Name.ToString(), Sha256 = Convert.ToHexStringLower(SHA256.HashData((byte[])field.Value!)) }) });
        }
        return JsonSerializer.Serialize(rows);
    }

    private async Task<object[]> DumpsAsync(RedisKey[] keys)
    {
        var result = new List<object>();
        foreach (RedisKey key in keys)
        {
            byte[] bytes = (byte[])(await redis.Connection.GetDatabase().ExecuteAsync("DUMP", key))!;
            result.Add(new { Key = key.ToString(), Sha256 = Convert.ToHexStringLower(SHA256.HashData(bytes)) });
        }
        return result.ToArray();
    }

    private async Task<RedisKey[]> WorkflowKeysAsync(string id, CancellationToken token)
    {
        List<RedisKey> keys = [];
        await foreach (RedisKey key in redis.Connection.GetServer(redis.Connection.GetEndPoints().Single()).KeysAsync(pattern: _sidecar.AppId + "||*" + id + "*", pageSize: 100).WithCancellation(token)) keys.Add(key);
        return keys.OrderBy(key => key.ToString(), StringComparer.Ordinal).ToArray();
    }

    private SourceInventoryStore Inventory() => new(redis.Connection, new QueryCursorCodec(new EphemeralDataProtectionProvider(), "ProcessErasure"));
    private static ServiceProvider ClientServices() { var services = new ServiceCollection(); services.AddLogging(); services.AddDaprWorkflow(_ => { }); return services.BuildServiceProvider(); }
    private static DaprIngestionWorkflowScheduler Scheduler(IDaprWorkflowClient client, SourceInventoryStore inventory) => new(client, Substitute.For<IWorkflowPayloadStore>(),
        new IngestionWorkflowConfigurationCapture(Options.Create(new IngestionSettings()), Options.Create(new NaturalLanguageDescriptionOptions())),
        new WorkflowTraceContextCapture(), Substitute.For<IIngestionWorkflowInFlightRegistry>(), TimeProvider.System, NullLogger<DaprIngestionWorkflowScheduler>.Instance, inventory);
    private static IngestionInput Input(string tenant) => new() { TenantId = tenant, CaseId = "case", SourceUri = "https://synthetic.invalid/process-history", SourceType = SourceType.Url, ContentType = "text/plain", IngestedBy = "owned-process-control" };
    private static HashEntry[] Owner(string tenant, string unit) => [new("id", unit), new("tenantId", tenant), new("caseId", "case"), new("sourceUri", "urn:synthetic:" + unit)];
    private static HashEntry[] Fields(string tenant, string unit) => [new("memoryUnitId", unit), new("tenantId", tenant), new("caseId", "case"), new("naturalLanguageDescription", "owned NL " + unit), new("embedding", new byte[] { 0, 255, 128, 13, 10 })];
    private static string[] Keys(string tenant, string unit) => [IndexSchemaDefinitions.BuildSyntacticKey(tenant, unit), IndexSchemaDefinitions.BuildSemanticKey(tenant, unit),
        IndexSchemaDefinitions.BuildSemanticChunkKey(tenant, unit, 7), IndexSchemaDefinitions.BuildNaturalLanguageSemanticKey(tenant, unit), IndexSchemaDefinitions.BuildLegacyNaturalLanguageSemanticKey(tenant, unit)];
    private static void CreateChunkIndex(IDatabase db, string tenant)
    {
        db.FT().Create(IndexSchemaDefinitions.GetSemanticIndexName(tenant), IndexSchemaDefinitions.CreateSemanticParams(tenant), IndexSchemaDefinitions.CreateSemanticSchema(3));
        db.FT().Create(IndexSchemaDefinitions.GetNaturalLanguageSemanticIndexName(tenant), IndexSchemaDefinitions.CreateNaturalLanguageSemanticParams(tenant), IndexSchemaDefinitions.CreateNaturalLanguageSemanticSchema(3));
    }
}
