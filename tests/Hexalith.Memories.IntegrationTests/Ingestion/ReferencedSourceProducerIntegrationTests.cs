namespace Hexalith.Memories.IntegrationTests.Ingestion;

using System.Reflection;
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
    [InlineData(false)]
    [InlineData(true)]
    public async Task FailedReingestionCopiesPriorSourceAndReconcilesOnlyTheNewOwnerAcrossRestart(bool failBeforeScheduling)
    {
        CancellationToken token = TestContext.Current.CancellationToken;
        string tenant = "reference-copy-" + Guid.NewGuid().ToString("N");
        if (!failBeforeScheduling) await OwnedSemanticInventory.ReconcileAsync(redis.Connection.GetDatabase(), tenant, "retry-unit");
        const string source = "urn:synthetic:referenced";
        string sourceOwner = DedupKeyBuilder.BuildKey(tenant, "case", source);
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
        client.ScheduleNewWorkflowAsync(nameof(IngestionWorkflow), "retry-unit", Arg.Any<IngestionInput>(), null, Arg.Any<CancellationToken>())
            .Returns(call => { scheduled = call.Arg<IngestionInput>(); return "retry-unit"; });
        var scheduler = new DaprIngestionWorkflowScheduler(client, payloads,
            new IngestionWorkflowConfigurationCapture(Options.Create(new IngestionSettings()), Options.Create(new NaturalLanguageDescriptionOptions())),
            new WorkflowTraceContextCapture(), registry, TimeProvider.System, NullLogger<DaprIngestionWorkflowScheduler>.Instance, inventory);
        var failed = new FailedUnitsRegistry(redis.Connection, NullLogger<FailedUnitsRegistry>.Instance);
        await new PersistFailedUnitActivity(redis.Connection, NullLogger<PersistFailedUnitActivity>.Instance).RunAsync(null!,
            new(tenant, "case", "retry-unit", "urn:synthetic:referenced", SourceType.Event, "test", "text/plain", "extracting", "failure", "synthetic", 1,
                DateTimeOffset.UtcNow, DateTimeOffset.UtcNow, original));
        var coordinator = new ReIngestionCoordinator(failed, payloads, scheduler, NullLogger<ReIngestionCoordinator>.Instance);
        if (failBeforeScheduling) { await Should.ThrowAsync<InvalidOperationException>(() => coordinator.TryScheduleAsync(tenant, "case", "retry-unit", token)); }
        else { (await coordinator.TryScheduleAsync(tenant, "case", "retry-unit", token)).WorkflowInstanceId.ShouldBe("retry-unit"); }
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
            var serializer = new JsonDaprSerializer(MemoriesJsonContext.Options);
            Type type = Type.GetType("Dapr.Workflow.Client.WorkflowMetadata, Dapr.Workflow", true)!;
            object metadata = Activator.CreateInstance(type, "retry-unit", nameof(IngestionWorkflow), WorkflowRuntimeStatus.Failed, DateTime.UtcNow, DateTime.UtcNow, serializer)!;
            type.GetProperty("SerializedInput")!.SetValue(metadata, serializer.Serialize(scheduled));
            var workflow = (WorkflowState)Activator.CreateInstance(typeof(WorkflowState), BindingFlags.Instance | BindingFlags.NonPublic, null, [metadata], null)!;
            workflows.GetWorkflowStateAsync("retry-unit", true, Arg.Any<CancellationToken>()).Returns(workflow);
        }
        WebApplicationBuilder builder = WebApplication.CreateBuilder(); builder.Logging.ClearProviders(); builder.WebHost.UseUrls("http://127.0.0.1:0");
        builder.Services.AddSingleton(recreated); builder.Services.AddSingleton(workflows);
        builder.Services.AddSingleton(new SourceInventoryFailureReconciler(restarted, graph.Connection, new GraphQueryBuilder(), recreatedPayloads, recreated, workflowClient: NSubstitute.Substitute.For<Dapr.Workflow.IDaprWorkflowClient>()));
        await using WebApplication app = builder.Build();
        app.Use((context, next) => { context.User = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ServerTenantClaimsTransformation.TenantClaimType, tenant)], "owned-test")); return next(context); });
        app.MapSourceInventoryEndpoints(); await app.StartAsync(token);
        using var http = new HttpClient { BaseAddress = new Uri(app.Urls.Single()) };
        using HttpResponseMessage response = await http.GetAsync("/api/v1/tenants/" + tenant + "/source-inventory?sourceUri=urn:synthetic:referenced", token);
        response.StatusCode.ShouldBe(System.Net.HttpStatusCode.OK, await response.Content.ReadAsStringAsync(token));
        SourceInventoryEntry terminal = (await recreated.ReadAsync(tenant, "urn:synthetic:referenced", null, 100, token)).Entries.ShouldHaveSingleItem();
        terminal.State.ShouldBe("Deleted"); terminal.TerminalVerified.ShouldBeTrue();
        (await Should.ThrowAsync<WorkflowPayloadException>(() => recreatedPayloads.ReadAsync(admitted.SourcePayloadReference!, tenant, "retry-unit", WorkflowPayloadKind.SourceBytes, token))).ErrorCode.ShouldBe("PAYLOAD_NOT_FOUND");
        (await recreatedPayloads.ReadAsync(original, tenant, sourceOwner, WorkflowPayloadKind.SourceBytes, token)).ShouldBe([1, 2, 3]);
        (await restarted.GetDatabase().SetLengthAsync(WorkflowPayloadLedger.Key(tenant, sourceOwner))).ShouldBe(1);
        (await restarted.GetDatabase().KeyExistsAsync(PersistFailedUnitActivity.BuildHashKey(tenant, "retry-unit"))).ShouldBeFalse();
    }
}
