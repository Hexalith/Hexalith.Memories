namespace Hexalith.Memories.IntegrationTests.Ingestion;

using System.Security.Claims;
using System.Text.Json;
using Dapr.Client;
using Dapr.Workflow;
using Hexalith.EventStore.Client.Queries;
using Hexalith.Memories.Client.Rest;
using Hexalith.Memories.Contracts.V1;
using Hexalith.Memories.EventStore;
using Hexalith.Memories.IntegrationTests.Fixtures;
using Hexalith.Memories.Server.Activities.Ingestion;
using Hexalith.Memories.Server.Activities.Tenants;
using Hexalith.Memories.Server.Authentication;
using Hexalith.Memories.Server.Endpoints;
using Hexalith.Memories.Server.Graph;
using Hexalith.Memories.Server.Ingestion;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using Shouldly;
using StackExchange.Redis;

/// <summary>Actual tenant teardown and admission controls with Redis and a persisted Dapr claim-check store.</summary>
[Collection("RedisStack")]
public sealed class TenantIngestionLifecycleIntegrationTests(RedisStackFixture redis, DaprStateSidecarFixture state, FalkorDbFixture graph)
    : IClassFixture<DaprStateSidecarFixture>, IClassFixture<FalkorDbFixture>
{
    [Fact]
    public async Task ActualTenantCleanupRemovesHashedOwnershipAndPayloadsAcrossRestartWhileOtherTenantSurvives()
    {
        CancellationToken token = TestContext.Current.CancellationToken;
        string tenant = "tenant-cleanup-" + Guid.NewGuid().ToString("N"), other = "tenant-survivor-" + Guid.NewGuid().ToString("N");
        var inventory = Inventory(redis.Connection);
        var payloads = Payloads(state.DaprClient, redis.Connection);
        IngestionInput input = Input(tenant);
        await inventory.AdmitAsync("workflow", input, token);
        await inventory.CompleteAsync(tenant, input.SourceUri, "workflow", input.CaseId, "unit", token);
        var publication = new RefreshPublicationStore(redis.Connection, inventory, graph.Connection);
        await publication.PrepareAsync(input with { RefreshExistingSource = true, IdempotencyToken = "refresh", SourceRevisionNumber = 1 }, token);
        WorkflowPayloadReference payload = await payloads.SaveAsync(tenant, "unit", WorkflowPayloadKind.SourceBytes, new byte[] { 8, 9 }, cancellationToken: token);
        WorkflowPayloadReference survivor = await payloads.SaveAsync(other, "unit", WorkflowPayloadKind.SourceBytes, new byte[] { 4, 5 }, cancellationToken: token);
        await inventory.AdmitAsync("other", Input(other), token);
        IDatabase db = redis.Connection.GetDatabase();
        RedisValue[] owned = await db.SetMembersAsync(TenantIngestionOwnership.Key(tenant));
        owned.ShouldContain(value => value.ToString() == SourceInventoryStore.BuildKey(tenant, input.SourceUri));
        owned.ShouldContain(value => value.ToString().StartsWith("source-inventory-output:", StringComparison.Ordinal));
        owned.ShouldContain(value => value.ToString().StartsWith("refresh-token-binding:", StringComparison.Ordinal));
        (await payloads.ReadAsync(payload, tenant, "unit", WorkflowPayloadKind.SourceBytes, token)).ShouldBe([8, 9]);
        (await Cleanup(redis.Connection, payloads).RunAsync(null!, new(tenant))).ShouldBeTrue();
        using IConnectionMultiplexer restarted = await ConnectionMultiplexer.ConnectAsync(redis.ConnectionString);
        foreach (RedisValue key in owned) { (await restarted.GetDatabase().KeyExistsAsync((RedisKey)key.ToString())).ShouldBeFalse(); }
        (await restarted.GetDatabase().KeyExistsAsync(TenantIngestionOwnership.Key(tenant))).ShouldBeFalse();
        (await restarted.GetDatabase().KeyExistsAsync(TenantIngestionOwnership.Fence(tenant))).ShouldBeTrue();
        var recreated = Payloads(state.DaprClient, restarted);
        (await Should.ThrowAsync<WorkflowPayloadException>(() => recreated.ReadAsync(payload, tenant, "unit", WorkflowPayloadKind.SourceBytes, token)))
            .ErrorCode.ShouldBe("PAYLOAD_NOT_FOUND");
        await Should.ThrowAsync<InvalidOperationException>(() => recreated.SaveAsync(tenant, "late", WorkflowPayloadKind.SourceBytes, new byte[] { 1 }, cancellationToken: token));
        await Should.ThrowAsync<InvalidOperationException>(() => Inventory(restarted).AdmitAsync("late", input, token));
        (await recreated.ReadAsync(survivor, other, "unit", WorkflowPayloadKind.SourceBytes, token)).ShouldBe([4, 5]);
        (await Inventory(restarted).ReadAsync(other, Input(other).SourceUri, null, 100, token)).Entries.ShouldHaveSingleItem();
        (await Cleanup(restarted, recreated).RunAsync(null!, new(tenant))).ShouldBeTrue();
    }

    [Fact]
    public async Task PendingActualPayloadSaveBlocksTenantSuccessAcrossSerializedRestartAndThenConverges()
    {
        CancellationToken token = TestContext.Current.CancellationToken;
        string tenant = "tenant-paused-" + Guid.NewGuid().ToString("N");
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var paused = Substitute.For<DaprClient>();
        paused.SaveStateAsync("statestore", Arg.Any<string>(), Arg.Any<WorkflowPayloadStoreEntry>(),
            metadata: Arg.Any<IReadOnlyDictionary<string, string>>(), cancellationToken: Arg.Any<CancellationToken>()).Returns(async call =>
        {
            entered.SetResult();
            await release.Task.WaitAsync(token);
            await state.DaprClient.SaveStateAsync("statestore", call.ArgAt<string>(1), call.Arg<WorkflowPayloadStoreEntry>(),
                metadata: call.Arg<IReadOnlyDictionary<string, string>>(), cancellationToken: token);
        });
        paused.GetStateAsync<WorkflowPayloadStoreEntry?>("statestore", Arg.Any<string>(), cancellationToken: Arg.Any<CancellationToken>())
            .Returns(call => state.DaprClient.GetStateAsync<WorkflowPayloadStoreEntry?>("statestore", call.ArgAt<string>(1), cancellationToken: token));
        paused.DeleteStateAsync("statestore", Arg.Any<string>(), cancellationToken: Arg.Any<CancellationToken>())
            .Returns(call => state.DaprClient.DeleteStateAsync("statestore", call.ArgAt<string>(1), cancellationToken: token));
        var payloads = Payloads(paused, redis.Connection);
        Task<WorkflowPayloadReference> writer = payloads.SaveAsync(tenant, "same-unit", WorkflowPayloadKind.SourceBytes, new byte[] { 1, 2 }, cancellationToken: token);
        await entered.Task.WaitAsync(token);
        await Should.ThrowAsync<InvalidOperationException>(() => Cleanup(redis.Connection, payloads).RunAsync(null!, new(tenant)));
        using IConnectionMultiplexer restarted = await ConnectionMultiplexer.ConnectAsync(redis.ConnectionString);
        var recreated = Payloads(state.DaprClient, restarted);
        await Should.ThrowAsync<InvalidOperationException>(() => Cleanup(restarted, recreated).RunAsync(null!, new(tenant)));
        (await restarted.GetDatabase().HashLengthAsync(WorkflowPayloadLedger.PendingKey(tenant, "same-unit"))).ShouldBe(1);
        release.SetResult();
        await Should.ThrowAsync<InvalidOperationException>(() => writer);
        (await Cleanup(restarted, recreated).RunAsync(null!, new(tenant))).ShouldBeTrue();
        (await restarted.GetDatabase().KeyExistsAsync(WorkflowPayloadLedger.PendingKey(tenant, "same-unit"))).ShouldBeFalse();
        await Should.ThrowAsync<InvalidOperationException>(() => recreated.SaveAsync(tenant, "same-unit", WorkflowPayloadKind.SourceBytes, new byte[] { 1, 2 }, cancellationToken: token));
    }

    [Fact]
    public async Task OverlappingActualSameUnitGraphWritesRetainIndependentAuthorityUntilEachAcknowledgement()
    {
        CancellationToken token = TestContext.Current.CancellationToken;
        string tenant = "tenant-overlap-" + Guid.NewGuid().ToString("N");
        var input = Hexalith.Memories.TestHelpers.Factories.IndexInputFactory.Create(tenantId: tenant, memoryUnitId: "same-unit", caseId: "case",
            sourceUri: "urn:synthetic:overlap", content: "synthetic overlapping producer") with { CausationId = null, CorrelationId = null };
        var native = new GraphQueryBuilder();
        var builder = Substitute.For<IGraphQueryBuilder>();
        using var firstRelease = new ManualResetEventSlim(); using var secondRelease = new ManualResetEventSlim();
        var bothEntered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        int started = 0;
        builder.BuildMergeCaseNode("case").Returns(_ =>
        {
            int order = Interlocked.Increment(ref started);
            if (order == 2) { bothEntered.SetResult(); }
            (order == 1 ? firstRelease : secondRelease).Wait(token);
            return native.BuildMergeCaseNode("case");
        });
        builder.BuildMergeMemoryUnitNode(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(),
            Arg.Any<SourceType>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<int>(), Arg.Any<string>(), Arg.Any<DateTimeOffset>(), Arg.Any<string>())
            .Returns(_ => native.BuildMergeMemoryUnitNode(input.MemoryUnitId, input.CaseId, input.Content, input.ContentHash, input.SourceUri,
                input.SourceType, input.EmbeddingProvider, input.EmbeddingModel, input.EmbeddingDimensions, input.IngestedBy, input.IngestedAt, "{}"));
        builder.BuildMergeEdge(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<EdgeType>(), Arg.Any<float>(), Arg.Any<EdgeOrigin>())
            .Returns(_ => native.BuildMergeEdge("case", "same-unit", EdgeType.Contains, 1, EdgeOrigin.Explicit));
        var activity = new Hexalith.Memories.Server.Activities.Indexing.IndexGraphActivity(graph.Connection, builder,
            NullLogger<Hexalith.Memories.Server.Activities.Indexing.IndexGraphActivity>.Instance, payloadStore: null, redis: redis.Connection);
        Task first = Task.Run(() => activity.RunAsync(null!, input), token);
        Task second = Task.Run(() => activity.RunAsync(null!, input), token);
        try
        {
            await bothEntered.Task.WaitAsync(TimeSpan.FromSeconds(5), token);
            firstRelease.Set();
            await (await Task.WhenAny(first, second)).WaitAsync(token);
            await Should.ThrowAsync<InvalidOperationException>(() => Cleanup(redis.Connection, null).RunAsync(null!, new(tenant)));
            using IConnectionMultiplexer restarted = await ConnectionMultiplexer.ConnectAsync(redis.ConnectionString);
            (await restarted.GetDatabase().SetLengthAsync(TenantIngestionOwnership.Operations(tenant))).ShouldBe(1);
            secondRelease.Set(); await Task.WhenAll(first, second);
            (await Cleanup(restarted, null).RunAsync(null!, new(tenant))).ShouldBeTrue();
            (await graph.Connection.GetDatabase().KeyExistsAsync(tenant)).ShouldBeTrue();
            await new DeleteFalkorDbGraphFinalizerActivity(graph.Connection, NullLogger<DeleteFalkorDbGraphFinalizerActivity>.Instance).RunAsync(null!, new(tenant));
            (await graph.Connection.GetDatabase().KeyExistsAsync(tenant)).ShouldBeFalse();
            await Should.ThrowAsync<InvalidOperationException>(() => activity.RunAsync(null!, input));
            (await graph.Connection.GetDatabase().KeyExistsAsync(tenant)).ShouldBeFalse();
        }
        finally { firstRelease.Set(); secondRelease.Set(); await Task.WhenAll(first, second); }
    }

    [Theory]
    [InlineData("tenant")]
    [InlineData("case")]
    [InlineData("source")]
    [InlineData("workflow")]
    [InlineData("predecessor")]
    [InlineData("case-control")]
    [InlineData("source-control")]
    [InlineData("workflow-control")]
    [InlineData("predecessor-control")]
    public async Task InvalidAdmissionIdentityFailsBeforeAnyDurablePreparation(string invalid)
    {
        CancellationToken token = TestContext.Current.CancellationToken;
        string tenant = "invalid-" + Guid.NewGuid().ToString("N"), workflow = "workflow";
        var input = Input(tenant) with { RefreshExistingSource = true, IdempotencyToken = "retry", SourceRevisionNumber = 1 };
        input = invalid switch
        {
            "tenant" => input with { TenantId = new string('t', 129) },
            "case" => input with { CaseId = new string('c', 513) },
            "source" => input with { SourceUri = new string('s', 4097) },
            "predecessor" => input with { ExpectedMemoryUnitId = new string('p', 513) },
            "case-control" => input with { CaseId = "case\ninvalid" },
            "source-control" => input with { SourceUri = "urn:source\ninvalid" },
            "predecessor-control" => input with { ExpectedMemoryUnitId = "unit\ninvalid" },
            _ => input,
        };
        if (invalid.StartsWith("workflow", StringComparison.Ordinal)) { workflow = invalid == "workflow" ? new string('w', 513) : "workflow\ninvalid"; }
        var inventory = Inventory(redis.Connection);
        await Should.ThrowAsync<ArgumentException>(() => inventory.AdmitAsync(workflow, input, token));
        IngestionIdentityGuard.Input(Input(tenant));
        if (!invalid.StartsWith("workflow", StringComparison.Ordinal))
        {
            await Should.ThrowAsync<ArgumentException>(() => new RefreshPublicationStore(redis.Connection, inventory, graph.Connection).PrepareAsync(input, token));
        }
        (await redis.Connection.GetDatabase().KeyExistsAsync(TenantIngestionOwnership.Key(input.TenantId))).ShouldBeFalse();
        (await redis.Connection.GetDatabase().KeyExistsAsync(TenantIngestionOwnership.Operations(input.TenantId))).ShouldBeFalse();
    }

    [Fact]
    public async Task AcceptedIdentityBoundariesRemainReadableThroughActualPublicSdk()
    {
        CancellationToken token = TestContext.Current.CancellationToken;
        string tenant = "boundary-" + Guid.NewGuid().ToString("N"); tenant = tenant.PadRight(128, 't');
        string workflow = new('w', 512), unit = new('u', 512), caseId = new('c', 512), source = "urn:" + new string('s', 4092);
        var inventory = Inventory(redis.Connection);
        IngestionInput input = Input(tenant) with { CaseId = caseId, SourceUri = source, ExpectedMemoryUnitId = unit };
        await inventory.AdmitAsync(workflow, input, token);
        await inventory.RecordCandidateAsync(tenant, source, workflow, unit, token);
        await inventory.CompleteAsync(tenant, source, workflow, caseId, unit, token);
        WebApplicationBuilder builder = WebApplication.CreateBuilder(); builder.Logging.ClearProviders();
        builder.WebHost.UseUrls("http://127.0.0.1:0");
        builder.Services.AddSingleton(inventory);
        builder.Services.AddSingleton(Substitute.For<IIngestionWorkflowStateReader>());
        builder.Services.AddSingleton(new SourceInventoryFailureReconciler(redis.Connection, graph.Connection, new GraphQueryBuilder(), Payloads(state.DaprClient, redis.Connection), inventory, workflowClient: NSubstitute.Substitute.For<Dapr.Workflow.IDaprWorkflowClient>()));
        await using WebApplication app = builder.Build();
        app.Use((context, next) => { context.User = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ServerTenantClaimsTransformation.TenantClaimType, tenant)], "boundary-test")); return next(context); });
        app.MapSourceInventoryEndpoints(); await app.StartAsync(token);
        using var http = new HttpClient { BaseAddress = new Uri(app.Urls.Single()) };
        var sdk = new MemoriesClient(http, Options.Create(new MemoriesClientOptions()), NullLogger<MemoriesClient>.Instance);
        SourceInventoryEntry entry = (await sdk.GetSourceInventoryAsync(tenant, source, ct: token)).Entries.ShouldHaveSingleItem();
        entry.CaseId.ShouldBe(caseId); entry.WorkflowInstanceId.ShouldBe(workflow); entry.MemoryUnitId.ShouldBe(unit); entry.CandidateMemoryUnitId.ShouldBe(unit);
    }

    private static IngestionInput Input(string tenant) => new() { TenantId = tenant, CaseId = "case", SourceUri = "urn:synthetic:tenant-purge",
        SourceType = SourceType.File, ContentType = "text/plain", ContentBytes = [1, 2, 3], IngestedBy = "synthetic" };
    private static SourceInventoryStore Inventory(IConnectionMultiplexer connection)
        => new(connection, new QueryCursorCodec(new EphemeralDataProtectionProvider(), "TenantLifecycleTests"));
    private static DaprWorkflowPayloadStore Payloads(DaprClient dapr, IConnectionMultiplexer connection)
        => new(dapr, Options.Create(new WorkflowPayloadStoreOptions()), TimeProvider.System, connection);
    private static DeleteTenantDataKeysActivity Cleanup(IConnectionMultiplexer connection, IWorkflowPayloadStore? payloads)
        => new(connection, Substitute.For<IAggregateCaseMappingStore>(), Substitute.For<IObservedEventTypeStore>(),
            Substitute.For<IHostApplicationLifetime>(), NullLogger<DeleteTenantDataKeysActivity>.Instance, payloads);
}
