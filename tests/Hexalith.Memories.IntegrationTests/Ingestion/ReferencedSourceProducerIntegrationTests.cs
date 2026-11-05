namespace Hexalith.Memories.IntegrationTests.Ingestion;

using System.Reflection;
using System.Text.Json;
using System.Security.Claims;
using Dapr.Common.Serialization;
using Dapr.Workflow;
using Hexalith.EventStore.Client.Queries;
using Hexalith.Memories.Contracts.V1;
using Hexalith.Memories.IntegrationTests.Fixtures;
using Hexalith.Memories.Server.Activities.Ingestion;
using Hexalith.Memories.Server.Authentication;
using Hexalith.Memories.Server.Endpoints;
using Hexalith.Memories.Server.Graph;
using Hexalith.Memories.Server.Ingestion;
using Hexalith.Memories.Server.NaturalLanguage;
using Hexalith.Memories.Server.Workflows;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using Shouldly;
using StackExchange.Redis;
using NRedisStack.RedisStackCommands;
using Hexalith.Memories.Server.Infrastructure;

/// <summary>Prior-owner claim checks become independently retained source bytes through actual payload, scheduler and failed-ingestion adapters.</summary>
[Collection("RedisStack")]
public sealed class ReferencedSourceProducerIntegrationTests(RedisStackFixture redis, DaprStateSidecarFixture state, FalkorDbFixture graph)
    : IClassFixture<DaprStateSidecarFixture>, IClassFixture<FalkorDbFixture>
{
    [Theory]
    [InlineData(false, "foreign")]
    [InlineData(true, "foreign")]
    [InlineData(true, "unknown")]
    [InlineData(true, "candidate")]
    public async Task FailedReingestionCopiesPriorSourceAndReconcilesOnlyTheNewOwnerAcrossRestart(bool failBeforeScheduling, string storedOwner)
    {
        CancellationToken token = TestContext.Current.CancellationToken;
        string tenant = "reference-copy-" + Guid.NewGuid().ToString("N");
        if (!failBeforeScheduling) await OwnedSemanticInventory.ReconcileAsync(redis.Connection.GetDatabase(), tenant, "retry-unit");
        const string source = "urn:synthetic:referenced";
        string sourceOwner = DedupKeyBuilder.BuildKey(tenant, "case", source);
        string foreignKey = IndexSchemaDefinitions.BuildSemanticKey(tenant, "nl:retry-unit");
        byte[]? foreignBefore = null;
        if (failBeforeScheduling)
        {
            foreignKey.ShouldBe(IndexSchemaDefinitions.BuildLegacyNaturalLanguageSemanticKey(tenant, "retry-unit"));
            await redis.Connection.GetDatabase().HashSetAsync(foreignKey,
                [new("tenantId", tenant), new("memoryUnitId", storedOwner == "foreign" ? "nl:retry-unit" : storedOwner == "unknown" ? "" : "retry-unit"), new("content", "foreign-private-content")]);
            foreignBefore = (byte[])(await redis.Connection.GetDatabase().ExecuteAsync("DUMP", foreignKey))!;
        }
        redis.Connection.GetDatabase().FT().Create(IndexSchemaDefinitions.GetSemanticIndexName(tenant),
            IndexSchemaDefinitions.CreateSemanticParams(tenant), IndexSchemaDefinitions.CreateSemanticSchema(3));
        // Production provisioning waits for the tenant's newly created index before deletion can certify completeness.
        using (var ready = CancellationTokenSource.CreateLinkedTokenSource(token))
        {
            ready.CancelAfter(TimeSpan.FromSeconds(10));
            while (true)
            {
                RedisResult[] info = (RedisResult[])(await redis.Connection.GetDatabase().ExecuteAsync("FT.INFO", IndexSchemaDefinitions.GetSemanticIndexName(tenant)))!;
                int indexing = Array.FindIndex(info, value => value.ToString() == "indexing");
                if (indexing >= 0 && info[indexing + 1].ToString() == "0")
                {
                    break;
                }
                await Task.Delay(25, ready.Token);
            }
        }
        var payloads = new DaprWorkflowPayloadStore(state.DaprClient, Options.Create(new WorkflowPayloadStoreOptions()), TimeProvider.System, redis.Connection);
        WorkflowPayloadReference original = await payloads.SaveAsync(tenant, sourceOwner, WorkflowPayloadKind.SourceBytes, new byte[] { 1, 2, 3 }, cancellationToken: token);
        var codec = new QueryCursorCodec(new EphemeralDataProtectionProvider(), "ReferenceCopyTests");
        var inventory = new SourceInventoryStore(redis.Connection, codec);
        var registry = Substitute.For<IIngestionWorkflowInFlightRegistry>();
        if (failBeforeScheduling) { registry.TrackAsync(Arg.Any<IngestionWorkflowInFlightEntry>(), Arg.Any<CancellationToken>()).Returns(Task.FromException(new InvalidOperationException("tracking failed"))); }
        var client = Substitute.For<IDaprWorkflowClient>();
        IngestionInput? scheduled = null;
        string? retryWorkflow = null;
        var priorInput = new IngestionInput { TenantId = tenant, CaseId = "case", SourceUri = source, SourceType = SourceType.Event,
            ContentType = "text/plain", IngestedBy = "test", PayloadReference = original };
        client.GetWorkflowStateAsync(sourceOwner, true, Arg.Any<CancellationToken>()).Returns(WorkflowStateFor(sourceOwner, priorInput));
        client.ScheduleNewWorkflowAsync(nameof(IngestionWorkflow), Arg.Any<string>(), Arg.Any<IngestionInput>(), null, Arg.Any<CancellationToken>())
            .Returns(call => { scheduled = call.Arg<IngestionInput>(); retryWorkflow = call.ArgAt<string>(1); return retryWorkflow; });
        var scheduler = new DaprIngestionWorkflowScheduler(client, payloads,
            new IngestionWorkflowConfigurationCapture(Options.Create(new IngestionSettings()), Options.Create(new NaturalLanguageDescriptionOptions())),
            new WorkflowTraceContextCapture(), registry, TimeProvider.System, NullLogger<DaprIngestionWorkflowScheduler>.Instance, inventory);
        var failed = new FailedUnitsRegistry(redis.Connection, NullLogger<FailedUnitsRegistry>.Instance);
        await new PersistFailedUnitActivity(redis.Connection, NullLogger<PersistFailedUnitActivity>.Instance).RunAsync(null!,
            new(tenant, "case", "retry-unit", "urn:synthetic:referenced", SourceType.Event, "test", "text/plain", "extracting", "failure", "synthetic", 1,
                DateTimeOffset.UtcNow, DateTimeOffset.UtcNow, original));
        var coordinator = new ReIngestionCoordinator(failed, payloads, scheduler, NullLogger<ReIngestionCoordinator>.Instance);
        if (failBeforeScheduling) { await Should.ThrowAsync<InvalidOperationException>(() => coordinator.TryScheduleAsync(tenant, "case", "retry-unit", token)); }
        else
        {
            var result = await coordinator.TryScheduleAsync(tenant, "case", "retry-unit", token);
            result.MemoryUnitId.ShouldBe("retry-unit");
            result.WorkflowInstanceId.ShouldNotBe("retry-unit");
            result.WorkflowInstanceId.ShouldBe(retryWorkflow);
        }
        SourceInventoryEntry admitted = (await inventory.ReadAsync(tenant, "urn:synthetic:referenced", null, 100, token)).Entries.ShouldHaveSingleItem();
        admitted.SourcePayloadReference.ShouldNotBeNull().MemoryUnitId.ShouldBe("retry-unit");
        admitted.SourcePayloadReference.ShouldNotBe(original);
        (await payloads.ReadAsync(admitted.SourcePayloadReference!, tenant, "retry-unit", WorkflowPayloadKind.SourceBytes, token)).ShouldBe([1, 2, 3]);
        (await payloads.ReadAsync(original, tenant, sourceOwner, WorkflowPayloadKind.SourceBytes, token)).ShouldBe([1, 2, 3]);
        if (!failBeforeScheduling)
        {
            scheduled.ShouldNotBeNull().ContentBytes.ShouldBeNull();
            scheduled.PayloadReference.ShouldBe(admitted.SourcePayloadReference);
            await new PersistFailedUnitActivity(redis.Connection, NullLogger<PersistFailedUnitActivity>.Instance).RunAsync(null!,
                new(tenant, "case", "retry-unit", "urn:synthetic:referenced", SourceType.Event, "test", "text/plain", "extracting", "failure", "synthetic", 1,
                    DateTimeOffset.UtcNow, DateTimeOffset.UtcNow, scheduled.PayloadReference));
        }
        using IConnectionMultiplexer restarted = await ConnectionMultiplexer.ConnectAsync(redis.ConnectionString);
        var recreated = new SourceInventoryStore(restarted, codec);
        var recreatedPayloads = new DaprWorkflowPayloadStore(state.DaprClient, Options.Create(new WorkflowPayloadStoreOptions()), TimeProvider.System, restarted);
        await recreated.FenceAsync(tenant, "urn:synthetic:referenced", true, token);
        var workflows = Substitute.For<IIngestionWorkflowStateReader>();
        if (scheduled is not null)
        {
            workflows.GetWorkflowStateAsync(retryWorkflow!, true, Arg.Any<CancellationToken>()).Returns(WorkflowStateFor(retryWorkflow!, scheduled));
        }
        WebApplicationBuilder builder = WebApplication.CreateBuilder(); builder.Logging.ClearProviders(); builder.WebHost.UseUrls("http://127.0.0.1:0");
        builder.Services.AddSingleton(recreated); builder.Services.AddSingleton(workflows);
        builder.Services.AddSingleton(new SourceInventoryFailureReconciler(restarted, graph.Connection, new GraphQueryBuilder(), recreatedPayloads, recreated, workflowClient: NSubstitute.Substitute.For<Dapr.Workflow.IDaprWorkflowClient>()));
        await using WebApplication app = builder.Build();
        app.Use((context, next) => { context.User = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ServerTenantClaimsTransformation.TenantClaimType, tenant)], "owned-test")); return next(context); });
        app.MapSourceInventoryEndpoints(); await app.StartAsync(token);
        using var http = new HttpClient { BaseAddress = new Uri(app.Urls.Single()) };
        using HttpResponseMessage response = await http.GetAsync("/api/v1/tenants/" + tenant + "/source-inventory?sourceUri=urn:synthetic:referenced", token);
        if (failBeforeScheduling && storedOwner != "foreign")
        {
            response.StatusCode.ShouldBe(System.Net.HttpStatusCode.Conflict);
            (await recreated.ReadAsync(tenant, source, null, 100, token)).Entries.ShouldHaveSingleItem().TerminalVerified.ShouldBeFalse();
            ((byte[])(await restarted.GetDatabase().ExecuteAsync("DUMP", foreignKey))!).ShouldBe(foreignBefore);
            (await recreatedPayloads.ReadAsync(admitted.SourcePayloadReference!, tenant, "retry-unit", WorkflowPayloadKind.SourceBytes, token)).ShouldBe([1, 2, 3]);
            (await restarted.GetDatabase().KeyExistsAsync(SourcePublicationVisibility.Tombstone(tenant, "retry-unit"))).ShouldBeFalse();
            return;
        }
        response.StatusCode.ShouldBe(System.Net.HttpStatusCode.OK, await response.Content.ReadAsStringAsync(token));
        SourceInventoryEntry terminal = (await recreated.ReadAsync(tenant, "urn:synthetic:referenced", null, 100, token)).Entries.ShouldHaveSingleItem();
        terminal.State.ShouldBe("Deleted"); terminal.TerminalVerified.ShouldBeTrue();
        if (failBeforeScheduling)
            ((byte[])(await restarted.GetDatabase().ExecuteAsync("DUMP", foreignKey))!).ShouldBe(foreignBefore);
        (await Should.ThrowAsync<WorkflowPayloadException>(() => recreatedPayloads.ReadAsync(admitted.SourcePayloadReference!, tenant, "retry-unit", WorkflowPayloadKind.SourceBytes, token))).ErrorCode.ShouldBe("PAYLOAD_NOT_FOUND");
        (await recreatedPayloads.ReadAsync(original, tenant, sourceOwner, WorkflowPayloadKind.SourceBytes, token)).ShouldBe([1, 2, 3]);
        (await restarted.GetDatabase().SetLengthAsync(WorkflowPayloadLedger.Key(tenant, sourceOwner))).ShouldBe(1);
        (await restarted.GetDatabase().KeyExistsAsync(PersistFailedUnitActivity.BuildHashKey(tenant, "retry-unit"))).ShouldBeFalse();
    }
    [Theory]
    [InlineData("tracked")]
    [InlineData("running")]
    [InlineData("missing")]
    [InlineData("wrong-source")]
    [InlineData("wrong-name")]
    public async Task MissingInventoryNeverSubstitutesForExactLegacyFailedWorkflowProof(string fault)
    {
        CancellationToken token = TestContext.Current.CancellationToken;
        string tenant = "legacy-refusal-" + Guid.NewGuid().ToString("N");
        var input = new IngestionInput { TenantId = tenant, CaseId = "case", SourceUri = "https://synthetic.invalid/source",
            SourceType = SourceType.Url, ContentType = "text/plain", IngestedBy = "test" };
        var record = new FailedUnitRecord(tenant, "case", "legacy-unit", input.SourceUri, SourceType.Url, "test", "text/plain",
            "fetching", "failed", "synthetic", 1, null, DateTimeOffset.UtcNow);
        var failed = new FailedUnitsRegistry(redis.Connection, NullLogger<FailedUnitsRegistry>.Instance);
        await failed.RestoreAsync(record, token);
        var inventory = new SourceInventoryStore(redis.Connection, new QueryCursorCodec(new EphemeralDataProtectionProvider(), "LegacyRefusal"));
        var client = Substitute.For<IDaprWorkflowClient>();
        if (fault != "missing")
        {
            var stored = input with { SourceInventoryTracked = fault == "tracked", SourceUri = fault == "wrong-source" ? "https://foreign.invalid" : input.SourceUri };
            client.GetWorkflowStateAsync("legacy-unit", true, Arg.Any<CancellationToken>())
                .Returns(WorkflowStateFor("legacy-unit", stored, fault == "running" ? WorkflowRuntimeStatus.Running : WorkflowRuntimeStatus.Failed,
                    fault == "wrong-name" ? "OtherWorkflow" : nameof(IngestionWorkflow)));
        }
        var payloads = new DaprWorkflowPayloadStore(state.DaprClient, Options.Create(new WorkflowPayloadStoreOptions()), TimeProvider.System, redis.Connection);
        var scheduler = new DaprIngestionWorkflowScheduler(client, payloads,
            new IngestionWorkflowConfigurationCapture(Options.Create(new IngestionSettings()), Options.Create(new NaturalLanguageDescriptionOptions())),
            new WorkflowTraceContextCapture(), Substitute.For<IIngestionWorkflowInFlightRegistry>(), TimeProvider.System,
            NullLogger<DaprIngestionWorkflowScheduler>.Instance, inventory);
        var coordinator = new ReIngestionCoordinator(failed, payloads, scheduler, NullLogger<ReIngestionCoordinator>.Instance);
        (await coordinator.TryScheduleAsync(tenant, "case", "legacy-unit", token)).Outcome.ShouldBe(ReIngestionAttemptOutcome.Conflict);
        await client.DidNotReceiveWithAnyArgs().ScheduleNewWorkflowAsync(default!, default!, default!, default, default);
        (await failed.GetAsync(tenant, "legacy-unit", token)).ShouldNotBeNull();
        var observed = await inventory.ReadAsync(tenant, input.SourceUri, null, 100, token);
        observed.Entries.ShouldBeEmpty();
        observed.CoverageComplete.ShouldBeFalse();
    }

    [Theory]
    [InlineData("valid")]
    [InlineData("wrong-source")]
    [InlineData("wrong-tenant")]
    [InlineData("wrong-case")]
    [InlineData("wrong-owner")]
    [InlineData("wrong-bytes")]
    [InlineData("tracked")]
    [InlineData("running")]
    [InlineData("wrong-handle")]
    [InlineData("no-retained-source")]
    public async Task RetainedHistoricalEventReferenceRequiresActualProducerInputAndExactFailedDedup(string fault)
    {
        var token = TestContext.Current.CancellationToken;
        string tenant = "legacy-event-" + Guid.NewGuid().ToString("N");
        using JsonDocument envelope = JsonDocument.Parse("{\"id\":\"event-source\",\"source\":\"/owned\",\"type\":\"Claims.Submitted\",\"data\":{\"value\":1}}");
        var route = new Hexalith.Memories.EventStore.TenantEventRoute(tenant, "case", "Claims");
        var router = Substitute.For<Hexalith.Memories.EventStore.ITenantEventRouter>();
        router.ResolveAsync(Arg.Any<Hexalith.Memories.EventStore.CloudEventEnvelope>(), Arg.Any<CancellationToken>())
            .Returns(Hexalith.Memories.EventStore.TenantEventRouteResolution.Accepted(route));
        IngestionInput? producerInput = null;
        string? producerWorkflow = null;
        var direct = Substitute.For<Hexalith.Memories.EventStore.IEventIngestionWorkflowScheduler>();
        direct.ScheduleAsync(Arg.Any<string>(), Arg.Any<IngestionInput>(), Arg.Any<CancellationToken>())
            .Returns(c => { producerWorkflow = c.ArgAt<string>(0); producerInput = c.Arg<IngestionInput>(); return producerWorkflow; });
        var options = Substitute.For<IOptionsMonitor<Hexalith.Memories.EventStore.TenantEventRoutingOptions>>();
        options.CurrentValue.Returns(new Hexalith.Memories.EventStore.TenantEventRoutingOptions { PreflightDedupEnabled = false });
        var producer = new Hexalith.Memories.EventStore.EventIngestionService(router, direct,
            Substitute.For<Hexalith.Memories.EventStore.IPreflightDedupStore>(), Substitute.For<Hexalith.Memories.EventStore.ISearchIndexMaintenance>(),
            Substitute.For<Hexalith.Memories.EventStore.IEventIngestionTelemetry>(), options,
            NullLogger<Hexalith.Memories.EventStore.EventIngestionService>.Instance);
        (await producer.ProcessAsync(envelope.RootElement, token)).Outcome.ShouldBe(Hexalith.Memories.EventStore.EventIngestionOutcome.Accepted);
        producerInput.ShouldNotBeNull().SourceInventoryTracked.ShouldBeFalse();
        producerWorkflow.ShouldBe(DedupKeyBuilder.BuildKey(tenant, "case", producerInput.SourceUri));
        var payloads = new DaprWorkflowPayloadStore(state.DaprClient, Options.Create(new WorkflowPayloadStoreOptions()), TimeProvider.System, redis.Connection);
        // Only historical/custom hosts supply this retained reference; default inline failures do not acquire one here.
        WorkflowPayloadReference source = await payloads.SaveAsync(tenant, producerWorkflow!, WorkflowPayloadKind.SourceBytes,
            producerInput.ContentBytes!, idSuffix: "source", cancellationToken: token);
        var record = new FailedUnitRecord(tenant, "case", "business-output", producerInput.SourceUri, SourceType.Event,
            producerInput.IngestedBy, producerInput.ContentType, "embedding", "failed", "synthetic", 1, null, DateTimeOffset.UtcNow, source);
        IngestionInput proof = fault switch
        {
            "wrong-source" => producerInput with { SourceUri = "foreign" },
            "wrong-tenant" => producerInput with { TenantId = "foreign" },
            "wrong-case" => producerInput with { CaseId = "foreign" },
            "wrong-owner" => producerInput with { PayloadReference = source with { MemoryUnitId = "foreign" } },
            "wrong-bytes" => producerInput with { ContentBytes = [9, 9, 9] },
            "tracked" => producerInput with { SourceInventoryTracked = true },
            _ => producerInput,
        };
        var client = Substitute.For<IDaprWorkflowClient>();
        client.GetWorkflowStateAsync(fault == "wrong-handle" ? "dedup:foreign" : producerWorkflow!, true, Arg.Any<CancellationToken>())
            .Returns(WorkflowStateFor(producerWorkflow!, proof, fault == "running" ? WorkflowRuntimeStatus.Running : WorkflowRuntimeStatus.Failed));
        IngestionInput? replacement = null;
        client.ScheduleNewWorkflowAsync(nameof(IngestionWorkflow), Arg.Any<string>(), Arg.Any<IngestionInput>(), null, Arg.Any<CancellationToken>())
            .Returns(c => { replacement = c.Arg<IngestionInput>(); return c.ArgAt<string>(1); });
        var inventory = new SourceInventoryStore(redis.Connection, new QueryCursorCodec(new EphemeralDataProtectionProvider(), "LegacyEvent"));
        var failed = new FailedUnitsRegistry(redis.Connection, NullLogger<FailedUnitsRegistry>.Instance);
        if (fault == "no-retained-source") record = record with { SourcePayloadReference = null };
        await failed.RestoreAsync(record, token);
        var scheduler = new DaprIngestionWorkflowScheduler(client, payloads,
            new IngestionWorkflowConfigurationCapture(Options.Create(new IngestionSettings()), Options.Create(new NaturalLanguageDescriptionOptions())),
            new WorkflowTraceContextCapture(), Substitute.For<IIngestionWorkflowInFlightRegistry>(), TimeProvider.System,
            NullLogger<DaprIngestionWorkflowScheduler>.Instance, inventory);
        var coordinator = new ReIngestionCoordinator(failed, payloads, scheduler, NullLogger<ReIngestionCoordinator>.Instance);
        var result = await coordinator.TryScheduleAsync(tenant, "case", "business-output", token);
        result.Outcome.ShouldBe(fault == "no-retained-source" ? ReIngestionAttemptOutcome.UnsupportedSourcePayload
            : fault == "valid" ? ReIngestionAttemptOutcome.Scheduled : ReIngestionAttemptOutcome.Conflict);
        if (fault == "no-retained-source")
            await client.DidNotReceiveWithAnyArgs().GetWorkflowStateAsync(default!, default, default);
        else await client.Received(1).GetWorkflowStateAsync(producerWorkflow!, true, Arg.Any<CancellationToken>());
        var observed = await inventory.ReadAsync(tenant, producerInput.SourceUri, null, 100, token);
        observed.CoverageComplete.ShouldBeFalse();
        if (fault == "valid")
        {
            result.WorkflowInstanceId.ShouldNotBe(producerWorkflow);
            result.WorkflowInstanceId.ShouldNotBe(record.MemoryUnitId);
            replacement.ShouldNotBeNull().FailedRetry.ShouldBe(new FailedIngestionRetry(producerWorkflow!, record.MemoryUnitId));
            replacement.PayloadReference!.MemoryUnitId.ShouldBe(record.MemoryUnitId);
            observed.Entries.ShouldHaveSingleItem().FailedRetryMemoryUnitId.ShouldBe(record.MemoryUnitId);
            (await failed.GetAsync(tenant, record.MemoryUnitId, token)).ShouldBeNull();
        }
        else
        {
            replacement.ShouldBeNull();
            observed.Entries.ShouldBeEmpty();
            (await failed.GetAsync(tenant, record.MemoryUnitId, token)).ShouldNotBeNull();
        }
        (await payloads.ReadAsync(source, tenant, producerWorkflow!, WorkflowPayloadKind.SourceBytes, token)).ShouldBe(producerInput.ContentBytes);
    }

    [Theory]
    [InlineData("none")]
    [InlineData("tenant")]
    [InlineData("tombstone")]
    public async Task AcceptedRetryFailureSurvivesOriginalClaimRestorationAndKeepsLatestLineage(string fence)
    {
        var token = TestContext.Current.CancellationToken;
        string tenant = "restore-race-" + Guid.NewGuid().ToString("N");
        const string sourceUri = "event://historical-retained";
        const string unit = "business-output";
        string dedup = DedupKeyBuilder.BuildKey(tenant, "case", sourceUri);
        var payloads = new DaprWorkflowPayloadStore(state.DaprClient, Options.Create(new WorkflowPayloadStoreOptions()), TimeProvider.System, redis.Connection);
        var reference = await payloads.SaveAsync(tenant, dedup, WorkflowPayloadKind.SourceBytes, new byte[] { 1, 2, 3 }, idSuffix: "source", cancellationToken: token);
        var historical = new IngestionInput { TenantId = tenant, CaseId = "case", SourceUri = sourceUri, SourceType = SourceType.Event,
            ContentType = "application/json", IngestedBy = "events", PayloadReference = reference };
        var original = new FailedUnitRecord(tenant, "case", unit, sourceUri, SourceType.Event, "events", "application/json",
            "extracting", "old-failure", "original", 1, null, DateTimeOffset.UtcNow.AddMinutes(-1), reference,
            new Dictionary<string, MetadataField> { ["generation"] = new("old", MetadataOrigin.Human, 1f) }, "old-cause", "old-correlation");
        var failed = new FailedUnitsRegistry(redis.Connection, NullLogger<FailedUnitsRegistry>.Instance);
        await failed.RestoreAsync(original, token);
        var client = Substitute.For<IDaprWorkflowClient>();
        client.GetWorkflowStateAsync(dedup, true, Arg.Any<CancellationToken>()).Returns(WorkflowStateFor(dedup, historical));
        var primary = new IOException("accepted retry acknowledgement lost after its terminal failure was published");
        IngestionInput? acceptedInput = null;
        string? acceptedId = null;
        IngestionInput? nextInput = null;
        byte[]? publishedHash = null;
        DateTimeOffset latestAt = DateTimeOffset.UtcNow;
        client.ScheduleNewWorkflowAsync(nameof(IngestionWorkflow), Arg.Any<string>(), Arg.Any<IngestionInput>(), null, Arg.Any<CancellationToken>())
            .Returns(async call =>
            {
                IngestionInput captured = call.Arg<IngestionInput>();
                string attempt = call.ArgAt<string>(1);
                if (acceptedInput is null)
                {
                    acceptedInput = captured;
                    acceptedId = attempt;
                    // A historical/custom host retained the original dedup source; the accepted retry owns a stable-output copy.
                    captured.PayloadReference!.MemoryUnitId.ShouldBe(unit);
                    (await new PersistFailedUnitActivity(redis.Connection, NullLogger<PersistFailedUnitActivity>.Instance).RunAsync(null!,
                        new(tenant, "case", unit, sourceUri, SourceType.Event, "events", "application/json", "embedding", "new-failure", "replacement failed", 7,
                            latestAt, latestAt, captured.PayloadReference,
                            new Dictionary<string, MetadataField> { ["generation"] = new("new", MetadataOrigin.Human, 1f) }, "new-cause", "new-correlation"))).ShouldBeTrue();
                    publishedHash = (byte[])(await redis.Connection.GetDatabase().ExecuteAsync("DUMP", PersistFailedUnitActivity.BuildHashKey(tenant, unit)))!;
                    throw primary;
                }
                nextInput = captured;
                return attempt;
            });
        var inventory = new SourceInventoryStore(redis.Connection, new QueryCursorCodec(new EphemeralDataProtectionProvider(), "RestoreRace"));
        var scheduler = new DaprIngestionWorkflowScheduler(client, payloads,
            new IngestionWorkflowConfigurationCapture(Options.Create(new IngestionSettings()), Options.Create(new NaturalLanguageDescriptionOptions())),
            new WorkflowTraceContextCapture(), Substitute.For<IIngestionWorkflowInFlightRegistry>(), TimeProvider.System,
            NullLogger<DaprIngestionWorkflowScheduler>.Instance, inventory);
        var coordinator = new ReIngestionCoordinator(failed, payloads, scheduler, NullLogger<ReIngestionCoordinator>.Instance);
        (await Should.ThrowAsync<IOException>(() => coordinator.TryScheduleAsync(tenant, "case", unit, token))).ShouldBe(primary);
        var retained = (await failed.GetAsync(tenant, unit, token)).ShouldNotBeNull();
        retained.SourcePayloadReference.ShouldBe(acceptedInput!.PayloadReference);
        retained.Metadata!["generation"].Value.ShouldBe("new");
        retained.ErrorCode.ShouldBe("new-failure");
        retained.RetryCount.ShouldBe(7);
        retained.FailedAt.ShouldBe(latestAt);
        retained.CausationId.ShouldBe("new-cause");
        retained.CorrelationId.ShouldBe("new-correlation");
        ((byte[])(await redis.Connection.GetDatabase().ExecuteAsync("DUMP", PersistFailedUnitActivity.BuildHashKey(tenant, unit)))!).ShouldBe(publishedHash);
        (await redis.Connection.GetDatabase().SortedSetScoreAsync(PersistFailedUnitActivity.BuildSortedSetKey(tenant, "case"), unit)).ShouldBe((double)latestAt.ToUnixTimeMilliseconds());
        var before = (await inventory.ReadAsync(tenant, sourceUri, null, 100, token)).Entries.ShouldHaveSingleItem();
        before.SchedulingState.ShouldBe("Scheduling");
        if (fence != "none")
        {
            await redis.Connection.GetDatabase().StringSetAsync(fence == "tenant" ? TenantIngestionOwnership.Fence(tenant) : SourcePublicationVisibility.Tombstone(tenant, unit), "1");
            var refusal = await Should.ThrowAsync<InvalidOperationException>(() => failed.RestoreAsync(original, token));
            refusal.Message.ShouldContain(fence == "tenant" ? "tenant-ingestion-erased" : "failed-unit-restore-fenced");
            ((byte[])(await redis.Connection.GetDatabase().ExecuteAsync("DUMP", PersistFailedUnitActivity.BuildHashKey(tenant, unit)))!).ShouldBe(publishedHash);
            return;
        }
        client.GetWorkflowStateAsync(acceptedId!, true, Arg.Any<CancellationToken>()).Returns(WorkflowStateFor(acceptedId!, acceptedInput));
        var next = await coordinator.TryScheduleAsync(tenant, "case", unit, token);
        next.Outcome.ShouldBe(ReIngestionAttemptOutcome.Scheduled);
        nextInput!.FailedRetry.ShouldBe(new FailedIngestionRetry(acceptedId!, unit));
        nextInput.PayloadReference!.MemoryUnitId.ShouldBe(unit);
        nextInput.Metadata["generation"].Value.ShouldBe("new");
        nextInput.CausationId.ShouldBe("new-cause");
        var obligations = (await inventory.ReadAsync(tenant, sourceUri, null, 100, token)).Entries;
        obligations.Count.ShouldBe(2);
        obligations[0].ShouldBe(before);
        obligations[1].FailedRetryMemoryUnitId.ShouldBe(unit);
        (await payloads.ReadAsync(reference, tenant, dedup, WorkflowPayloadKind.SourceBytes, token)).ShouldBe([1, 2, 3]);
    }

    private static WorkflowState WorkflowStateFor(string instanceId, IngestionInput input, WorkflowRuntimeStatus status = WorkflowRuntimeStatus.Failed, string workflowName = nameof(IngestionWorkflow))
    {
        var serializer = new JsonDaprSerializer(MemoriesJsonContext.Options);
        Type type = Type.GetType("Dapr.Workflow.Client.WorkflowMetadata, Dapr.Workflow", true)!;
        object metadata = Activator.CreateInstance(type, instanceId, workflowName, status, DateTime.UtcNow, DateTime.UtcNow, serializer)!;
        type.GetProperty("SerializedInput")!.SetValue(metadata, serializer.Serialize(input));
        return (WorkflowState)Activator.CreateInstance(typeof(WorkflowState), BindingFlags.Instance | BindingFlags.NonPublic, null, [metadata], null)!;
    }

}
