#pragma warning disable DAPR_CONVERSATION
namespace Hexalith.Memories.IntegrationTests.Ingestion;

using System.Text;
using System.Text.Json;
using Dapr.Workflow;
using Dapr.Actors;
using Dapr.Actors.Client;
using Hexalith.Memories.Server.Actors;
using Hexalith.Memories.Server.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using NRedisStack.RedisStackCommands;
using Dapr.AI.Conversation;
using Hexalith.Memories.Server.Activities.Ingestion;
using Hexalith.Memories.Server.Activities.Cases;
using Hexalith.Memories.Server.Graph;
using Hexalith.Memories.Contracts.V1;
using Microsoft.Extensions.Options;
using NSubstitute;
using Hexalith.Memories.IntegrationTests.Fixtures;
using Hexalith.Memories.Server.Ingestion;
using Hexalith.Memories.Server.NaturalLanguage;
using Hexalith.Memories.Server.Workflows;
using Hexalith.Memories.Server.Workflows.Contracts;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Shouldly;
using StackExchange.Redis;

/// <summary>Actual Dapr Workflow persisted-history erasure over an explicitly owned sidecar and native Redis store.</summary>
[Collection("RedisStack")]
[Trait("Category", "Integration")]
[Trait("Category", "IntegrationSlow")]
public sealed class RetryWorkflowErasureIntegrationTests(RedisStackFixture redis, FalkorDbFixture graph) : IAsyncLifetime, IClassFixture<FalkorDbFixture>
{
    private readonly DaprStateSidecarFixture _sidecar = DaprStateSidecarFixture.CreateWorkflow(redis.ConnectionString);

    public ValueTask InitializeAsync() => _sidecar.InitializeAsync();
    public ValueTask DisposeAsync() => _sidecar.DisposeAsync();

    [Fact]
    public async Task ExistingScheduledRawRetryHistoryIsPhysicallyAbsentAfterPurgeAndAdapterRestart()
    {
        string endpoint = _sidecar.DaprGrpcEndpoint;
        var uri = new Uri(endpoint);
        uri.Host.ShouldBe("127.0.0.1");
        string? previous = Environment.GetEnvironmentVariable("DAPR_GRPC_ENDPOINT");
        Environment.SetEnvironmentVariable("DAPR_GRPC_ENDPOINT", endpoint);
        try
        {
            using IHost host = Host.CreateDefaultBuilder().ConfigureServices(services =>
                {
                    services.AddKeyedSingleton<IConnectionMultiplexer>("redis", redis.Connection);
                    services.AddDaprWorkflow(options =>
                    {
                        options.RegisterWorkflow<LegacyRetryErasureQualificationWorkflow>();
                        options.RegisterWorkflow<NaturalLanguageEmbeddingRetryWorkflow>();
                        options.RegisterActivity<RetryNaturalLanguageEmbeddingActivity>();
                    });
                }).Build();
            using CancellationTokenSource deadline = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
            deadline.CancelAfter(TimeSpan.FromSeconds(60));
            await host.StartAsync(deadline.Token);
            IDaprWorkflowClient client = host.Services.GetRequiredService<DaprWorkflowClient>();
            string tenant = "workflow-erasure-" + Guid.NewGuid().ToString("N");
            const string unit = "victim";
            string id = NaturalLanguageEmbeddingRetryHostedService.GetLegacyRetryWorkflowInstanceId(tenant, unit);
            string raw = "synthetic-raw-secret-" + Guid.NewGuid().ToString("N");
            await client.ScheduleNewWorkflowAsync(nameof(LegacyRetryErasureQualificationWorkflow), id, Input(tenant, unit, raw));
            await client.WaitForWorkflowStartAsync(id, true, deadline.Token);
            string survivorId = NaturalLanguageEmbeddingRetryHostedService.GetLegacyRetryWorkflowInstanceId(tenant, "survivor");
            await client.ScheduleNewWorkflowAsync(nameof(LegacyRetryErasureQualificationWorkflow), survivorId, Input(tenant, "survivor", "synthetic-survivor"));
            await client.WaitForWorkflowStartAsync(survivorId, true, deadline.Token);
            IDatabase db = redis.Connection.GetDatabase();
            async Task<RedisKey[]> FindOwnedKeysAsync()
            {
                var server = redis.Connection.GetServer(redis.Connection.GetEndPoints().Single());
                List<RedisKey> keys = [];
                await foreach (RedisKey key in server.KeysAsync(pattern: "*" + id + "*", pageSize: 100).WithCancellation(deadline.Token))
                {
                    keys.Add(key);
                }
                return keys.ToArray();
            }
            RedisKey[] before = await FindOwnedKeysAsync();
            before.ShouldNotBeEmpty();
            bool foundRaw = false;
            foreach (RedisKey key in before)
            {
                if (await db.KeyTypeAsync(key) == RedisType.Hash)
                {
                    foreach (HashEntry field in await db.HashGetAllAsync(key))
                    {
                        foundRaw |= Encoding.UTF8.GetString((byte[]?)field.Value ?? []).Contains(raw, StringComparison.Ordinal);
                    }
                }
            }
            foundRaw.ShouldBeTrue();
            await db.StringSetAsync(SourcePublicationVisibility.Tombstone(tenant, unit), "1");
            InvalidOperationException refusal = await Should.ThrowAsync<InvalidOperationException>(
                () => RetryWorkflowErasure.PurgeAsync(client, tenant, unit, deadline.Token));
            refusal.Message.ShouldBe("nl-retry-legacy-scheduler-quiescence-required");
            WorkflowState retained = await client.GetWorkflowStateAsync(id, true, deadline.Token);
            retained.RuntimeStatus.ShouldBe(WorkflowRuntimeStatus.Running);
            retained.ReadInputAs<NaturalLanguageEmbeddingRetryInput>().ShouldNotBeNull().RawJsonPayload.ShouldBe(raw);
            foreach (RedisKey key in before)
            {
                (await db.KeyExistsAsync(key)).ShouldBeTrue();
            }
            await RetryWorkflowErasure.PurgeAsync(client, tenant, unit, deadline.Token, legacySchedulersDrained: true);
            foreach (RedisKey key in before)
            {
                (await db.KeyExistsAsync(key)).ShouldBeFalse();
            }
            (await FindOwnedKeysAsync()).ShouldBeEmpty();
            (await client.GetWorkflowStateAsync(survivorId, true, deadline.Token)).Exists.ShouldBeTrue();
            id = NaturalLanguageEmbeddingRetryHostedService.GetRetryWorkflowInstanceId(tenant, unit);
            await client.ScheduleNewWorkflowAsync(nameof(NaturalLanguageEmbeddingRetryWorkflow), id,
                new NaturalLanguageEmbeddingRetryInput(tenant, unit, string.Empty, "Synthetic", null, "case", "test", "test", 1));
            WorkflowState delayed = await client.WaitForWorkflowCompletionAsync(id, true, deadline.Token);
            delayed.ReadOutputAs<NaturalLanguageEmbeddingRetryResult>().ShouldNotBeNull().Indexed.ShouldBeFalse();
            await RetryWorkflowErasure.PurgeAsync(client, tenant, unit, deadline.Token);
            (await FindOwnedKeysAsync()).ShouldBeEmpty();
            await RetryWorkflowErasure.PurgeAsync(client, tenant, "survivor", deadline.Token, legacySchedulersDrained: true);
            await host.StopAsync(deadline.Token);
            using IHost restarted = Host.CreateDefaultBuilder().ConfigureServices(services =>
                {
                    services.AddKeyedSingleton<IConnectionMultiplexer>("redis", redis.Connection);
                    services.AddDaprWorkflow(options =>
                    {
                        options.RegisterWorkflow<LegacyRetryErasureQualificationWorkflow>();
                        options.RegisterWorkflow<NaturalLanguageEmbeddingRetryWorkflow>();
                        options.RegisterActivity<RetryNaturalLanguageEmbeddingActivity>();
                    });
                }).Build();
            await restarted.StartAsync(deadline.Token);
            await RetryWorkflowErasure.PurgeAsync(restarted.Services.GetRequiredService<DaprWorkflowClient>(), tenant, unit, deadline.Token);
            (await FindOwnedKeysAsync()).ShouldBeEmpty();
            await restarted.StopAsync(deadline.Token);
        }
        finally
        {
            Environment.SetEnvironmentVariable("DAPR_GRPC_ENDPOINT", previous);
        }
    }

    [Fact]
    public async Task CollidingLegacyAddressPreservesForeignHistoryAndScopedNewSchedulesUntilTheirOwnErasure()
    {
        await WithClientAsync(async (client, token) =>
        {
            string tenant = "workflow-collision-" + Guid.NewGuid().ToString("N");
            const string unit = "b-c";
            string foreignTenant = tenant + "-b";
            const string foreignUnit = "c";
            string legacyId = NaturalLanguageEmbeddingRetryHostedService.GetLegacyRetryWorkflowInstanceId(tenant, unit);
            legacyId.ShouldBe(NaturalLanguageEmbeddingRetryHostedService.GetLegacyRetryWorkflowInstanceId(foreignTenant, foreignUnit));
            string ownId = NaturalLanguageEmbeddingRetryHostedService.GetRetryWorkflowInstanceId(tenant, unit);
            string foreignId = NaturalLanguageEmbeddingRetryHostedService.GetRetryWorkflowInstanceId(foreignTenant, foreignUnit);
            ownId.ShouldNotBe(foreignId);
            string raw = "synthetic-foreign-secret-" + Guid.NewGuid().ToString("N");
            await client.ScheduleNewWorkflowAsync(nameof(LegacyRetryErasureQualificationWorkflow), legacyId, Input(foreignTenant, foreignUnit, raw));
            await client.ScheduleNewWorkflowAsync(nameof(LegacyRetryErasureQualificationWorkflow), ownId, Input(tenant, unit, string.Empty));
            await client.ScheduleNewWorkflowAsync(nameof(LegacyRetryErasureQualificationWorkflow), foreignId, Input(foreignTenant, foreignUnit, string.Empty));
            await client.WaitForWorkflowStartAsync(legacyId, true, token);
            await client.WaitForWorkflowStartAsync(ownId, true, token);
            await client.WaitForWorkflowStartAsync(foreignId, true, token);
            RedisKey[] foreignKeys = await FindWorkflowKeysAsync(legacyId, token);
            foreignKeys.ShouldNotBeEmpty();

            await RetryWorkflowErasure.PurgeAsync(client, tenant, unit, token);

            (await client.GetWorkflowStateAsync(ownId, true, token)).Exists.ShouldBeFalse();
            WorkflowState foreign = await client.GetWorkflowStateAsync(legacyId, true, token);
            foreign.RuntimeStatus.ShouldBe(WorkflowRuntimeStatus.Running);
            foreign.ReadInputAs<NaturalLanguageEmbeddingRetryInput>().ShouldNotBeNull().RawJsonPayload.ShouldBe(raw);
            (await client.GetWorkflowStateAsync(foreignId, true, token)).RuntimeStatus.ShouldBe(WorkflowRuntimeStatus.Running);
            foreach (RedisKey key in foreignKeys)
            {
                (await redis.Connection.GetDatabase().KeyExistsAsync(key)).ShouldBeTrue();
            }

            // The same historic address is purgeable only under its authoritative stored scope.
            await RetryWorkflowErasure.PurgeAsync(client, foreignTenant, foreignUnit, token, legacySchedulersDrained: true);
            (await client.GetWorkflowStateAsync(legacyId, true, token)).Exists.ShouldBeFalse();
            (await client.GetWorkflowStateAsync(foreignId, true, token)).Exists.ShouldBeFalse();
            (await FindWorkflowKeysAsync(legacyId, token)).ShouldBeEmpty();
        });
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task UnverifiableLegacyScopeBlocksErasureWithoutChangingHistory(bool duplicateScope)
    {
        await WithClientAsync(async (client, token) =>
        {
            string tenant = "workflow-ambiguous-" + Guid.NewGuid().ToString("N");
            const string unit = "victim";
            string id = NaturalLanguageEmbeddingRetryHostedService.GetLegacyRetryWorkflowInstanceId(tenant, unit);
            string json = duplicateScope
                ? "{\"TenantId\":" + JsonSerializer.Serialize(tenant) + ",\"tenantId\":\"foreign\",\"MemoryUnitId\":\"victim\"}"
                : "\"synthetic-unscoped-raw-input\"";
            using JsonDocument document = JsonDocument.Parse(json);
            await client.ScheduleNewWorkflowAsync(nameof(LegacyRetryErasureQualificationWorkflow), id, document.RootElement.Clone());
            await client.WaitForWorkflowStartAsync(id, true, token);
            RedisKey[] before = await FindWorkflowKeysAsync(id, token);
            before.ShouldNotBeEmpty();
            InvalidOperationException error = await Should.ThrowAsync<InvalidOperationException>(
                () => RetryWorkflowErasure.PurgeAsync(client, tenant, unit, token));
            error.Message.ShouldBe("nl-retry-workflow-scope-unverifiable");
            WorkflowState after = await client.GetWorkflowStateAsync(id, true, token);
            after.RuntimeStatus.ShouldBe(WorkflowRuntimeStatus.Running);
            after.ReadInputAs<JsonElement>().GetRawText().ShouldBe(json);
            foreach (RedisKey key in before)
            {
                (await redis.Connection.GetDatabase().KeyExistsAsync(key)).ShouldBeTrue();
            }
            // Only the fixture owner cleans up this deliberately unscoped synthetic instance.
            await client.TerminateWorkflowAsync(id, cancellation: token);
            await client.WaitForWorkflowCompletionAsync(id, false, token);
            await client.PurgeInstanceAsync(id, token);
        });
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ContentBearingLegacyActivityCannotRestoreHistoryAfterTerminationPurgeLateCompletionAndRestart(bool caseDeletion)
    {
        string previous = Environment.GetEnvironmentVariable("DAPR_GRPC_ENDPOINT") ?? string.Empty;
        Environment.SetEnvironmentVariable("DAPR_GRPC_ENDPOINT", _sidecar.DaprGrpcEndpoint);
        var completed = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource<ConversationResponse>(TaskCreationOptions.RunContinuationsAsynchronously);
        using var http = new HttpClient();
        var conversation = Substitute.For<DaprConversationClient>(null!, http, null!);
        conversation.ConverseAsync(Arg.Any<IReadOnlyList<ConversationInput>>(), Arg.Any<ConversationOptions>(), Arg.Any<CancellationToken>())
            .Returns(_ => { entered.TrySetResult(); return release.Task; });
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        deadline.CancelAfter(TimeSpan.FromSeconds(90));
        var token = deadline.Token;
        string tenant = "legacy-activity-" + Guid.NewGuid().ToString("N");
        const string unit = "victim";
        string id = NaturalLanguageEmbeddingRetryHostedService.GetLegacyRetryWorkflowInstanceId(tenant, unit);
        string raw = "raw-legacy-activity-" + Guid.NewGuid().ToString("N");
        try
        {
            using IHost host = Host.CreateDefaultBuilder().ConfigureServices(services =>
            {
                services.AddSingleton(conversation);
                services.AddSingleton(Options.Create(new NaturalLanguageDescriptionOptions()));
                services.AddDaprWorkflow(options => { options.RegisterWorkflow<ContentBearingLegacyRetryWorkflow>();
                    options.RegisterActivity<NaturalLanguageDescriptionInput, NaturalLanguageDescriptionResult>(nameof(GenerateNaturalLanguageDescriptionActivity), async (context, input) =>
                    {
                        try { return await new GenerateNaturalLanguageDescriptionActivity(conversation, Options.Create(new NaturalLanguageDescriptionOptions()), NullLogger<GenerateNaturalLanguageDescriptionActivity>.Instance).RunAsync(context, input); }
                        finally { completed.TrySetResult(); }
                    }); });
            }).Build();
            await host.StartAsync(token);
            var client = host.Services.GetRequiredService<DaprWorkflowClient>();
            await client.ScheduleNewWorkflowAsync(nameof(ContentBearingLegacyRetryWorkflow), id,
                new NaturalLanguageEmbeddingRetryInput(tenant, unit, raw, "TestEvent", null, "case", "test", "test", 1));
            await entered.Task.WaitAsync(token);
            RedisKey[] persisted = await FindWorkflowKeysAsync(id, token);
            persisted.ShouldNotBeEmpty();
            IDatabase db = redis.Connection.GetDatabase();
            bool activityContainsRaw = false;
            foreach (RedisKey key in persisted.Where(key => key.ToString().Contains("Activity", StringComparison.OrdinalIgnoreCase)))
                if (await db.KeyTypeAsync(key) == RedisType.Hash)
                    activityContainsRaw |= (await db.HashGetAllAsync(key)).Any(field => field.Value.ToString().Contains(raw, StringComparison.Ordinal));
            activityContainsRaw.ShouldBeTrue();
            var registry = new FailedNaturalLanguageEmbeddingRegistry(redis.Connection, NullLogger<FailedNaturalLanguageEmbeddingRegistry>.Instance);
            var victim = new FailedNaturalLanguageEmbeddingRecord(tenant, unit, raw, "TestEvent", null, "case", "p", "m", 1, 1, 0);
            var survivor = victim with { MemoryUnitId = "survivor", TruncatedRawJsonPayload = "unrelated-content" };
            await registry.EnqueueAsync(victim, token);
            await registry.EnqueueAsync(survivor, token);
            foreach (string queue in new[] { FailedNaturalLanguageEmbeddingRegistry.LiveKey(tenant), FailedNaturalLanguageEmbeddingRegistry.DeadKey(tenant) })
                await db.SortedSetAddAsync(queue, FailedNaturalLanguageEmbeddingRegistry.SerializeRecord(victim with { Attempts = 5 }), 1);
            await db.HashSetAsync(FailedNaturalLanguageEmbeddingRegistry.DeadPayloadKey(tenant), unit, FailedNaturalLanguageEmbeddingRegistry.SerializeRecord(victim));
            await db.SortedSetAddAsync(FailedNaturalLanguageEmbeddingRegistry.DeadKey(tenant), unit, 1);
            await db.StringSetAsync(SourcePublicationVisibility.KeyProvenance(tenant, unit), "modern");
            string foreignKey = IndexSchemaDefinitions.BuildSemanticKey(tenant, "nl:victim");
            await db.HashSetAsync(foreignKey, [new("memoryUnitId", "nl:victim"), new("content", "unrelated-content")]);
            var options = Options.Create(new NaturalLanguageDescriptionOptions { LegacyRetrySchedulersDrainedForErasure = true });
            if (caseDeletion) await new DeleteCaseProjectionActivity(redis.Connection, graph.Connection, new GraphQueryBuilder(), workflowClient: client, retryOptions: options)
                .RunAsync(null!, new(tenant, "case", [unit]));
            else await new DeleteMemoryUnitProjectionActivity(redis.Connection, graph.Connection, new GraphQueryBuilder(), workflowClient: client, retryOptions: options)
                .RunAsync(null!, new(tenant, "case", unit, []));
            foreach (string payload in new[] { FailedNaturalLanguageEmbeddingRegistry.LivePayloadKey(tenant), FailedNaturalLanguageEmbeddingRegistry.DeadPayloadKey(tenant) })
                (await db.HashGetAsync(payload, unit)).HasValue.ShouldBeFalse();
            foreach (string queue in new[] { FailedNaturalLanguageEmbeddingRegistry.LiveKey(tenant), FailedNaturalLanguageEmbeddingRegistry.DeadKey(tenant) })
                (await db.SortedSetRangeByRankAsync(queue)).ShouldNotContain(value => value.ToString().Contains(raw, StringComparison.Ordinal) || value == unit);
            (await db.HashGetAsync(FailedNaturalLanguageEmbeddingRegistry.LivePayloadKey(tenant), "survivor")).HasValue.ShouldBeTrue();
            (await db.HashGetAsync(foreignKey, "content")).ToString().ShouldBe("unrelated-content");
            (await FindWorkflowKeysAsync(id, token)).ShouldBeEmpty();
            release.TrySetResult(new ConversationResponse([new ConversationResponseResult([new ConversationResultChoice(null, 0, new ResultMessage(raw))])], string.Empty));
            // Observe actual late activity completion, then recreate both worker and persisted adapter.
            await completed.Task.WaitAsync(token);
            await host.StopAsync(token);
            using var restartedRedis = await ConnectionMultiplexer.ConnectAsync(redis.ConnectionString);
            using IHost restarted = Host.CreateDefaultBuilder().ConfigureServices(services => services.AddDaprWorkflow(options =>
                options.RegisterWorkflow<ContentBearingLegacyRetryWorkflow>())).Build();
            await restarted.StartAsync(token);
            var restartedClient = restarted.Services.GetRequiredService<DaprWorkflowClient>();
            // No additional purge may mask a late write after the original deletion.
            (await FindWorkflowKeysAsync(id, token)).ShouldBeEmpty();
            (await restartedClient.GetWorkflowStateAsync(id, true, token)).Exists.ShouldBeFalse();
            (await FindWorkflowKeysAsync(id, token)).ShouldBeEmpty();
            (await restartedRedis.GetDatabase().KeyExistsAsync(SourcePublicationVisibility.Tombstone(tenant, unit))).ShouldBeTrue();
            await restarted.StopAsync(token);
        }
        finally { release.TrySetCanceled(); Environment.SetEnvironmentVariable("DAPR_GRPC_ENDPOINT", string.IsNullOrEmpty(previous) ? null : previous); }
    }

    [Fact]
    public async Task WorkerSchedulesContentFreeDurableHistoryWhileAdmittedRegistryContentPersistsNaturalLanguageOutput()
    {
        string? previous = Environment.GetEnvironmentVariable("DAPR_GRPC_ENDPOINT");
        Environment.SetEnvironmentVariable("DAPR_GRPC_ENDPOINT", _sidecar.DaprGrpcEndpoint);
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        deadline.CancelAfter(TimeSpan.FromSeconds(90));
        var token = deadline.Token;
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource<ConversationResponse>(TaskCreationOptions.RunContinuationsAsynchronously);
        string tenant = "worker-content-free-" + Guid.NewGuid().ToString("N");
        const string unit = "victim";
        string sentinel = "raw-registry-sentinel-" + Guid.NewGuid().ToString("N");
        string id = NaturalLanguageEmbeddingRetryHostedService.GetRetryWorkflowInstanceId(tenant, unit);
        IDatabase db = redis.Connection.GetDatabase();
        var config = EmbeddingProviderDefaults.Google();
        await db.HashSetAsync(IndexSchemaDefinitions.BuildSyntacticKey(tenant, unit), [new("memoryUnitId", unit), new("caseId", "case"), new("content", "canonical")]);
        string survivor = IndexSchemaDefinitions.BuildNaturalLanguageSemanticKey(tenant, "survivor");
        await db.HashSetAsync(survivor, [new("memoryUnitId", "survivor"), new("naturalLanguageDescription", "unchanged")]);
        db.FT().Create(IndexSchemaDefinitions.GetNaturalLanguageSemanticIndexName(tenant),
            IndexSchemaDefinitions.CreateNaturalLanguageSemanticParams(tenant), IndexSchemaDefinitions.CreateNaturalLanguageSemanticSchema(config.Dimensions));
        var registry = new FailedNaturalLanguageEmbeddingRegistry(redis.Connection, NullLogger<FailedNaturalLanguageEmbeddingRegistry>.Instance);
        var record = new FailedNaturalLanguageEmbeddingRecord(tenant, unit, sentinel, "TestEvent", null, "case", config.Provider, config.Model, config.Dimensions, 1, 0);
        await registry.EnqueueAsync(record, token);
        string foreignTenant = tenant + "-foreign";
        var foreign = record with { TenantId = foreignTenant, MemoryUnitId = "survivor", TruncatedRawJsonPayload = "foreign-raw" };
        await registry.EnqueueAsync(foreign, token);
        RedisValue foreignBefore = await db.HashGetAsync(FailedNaturalLanguageEmbeddingRegistry.LivePayloadKey(foreignTenant), foreign.MemoryUnitId);
        // This adapter limits the worker to the fixture-owned tenant; dequeue and finalization use the
        // real persisted registry. Other integration tenants can share the collection's native Redis.
        var owned = Substitute.For<IFailedNaturalLanguageEmbeddingRegistry>();
        owned.ListTenantsWithBacklogAsync(Arg.Any<CancellationToken>()).Returns(_ => OwnedTenantAsync(tenant));
        owned.GetBacklogCountAsync(tenant, Arg.Any<CancellationToken>()).Returns(call => registry.GetBacklogCountAsync(tenant, call.Arg<CancellationToken>()));
        owned.DequeueBatchAsync(tenant, Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns(call => registry.DequeueBatchAsync(tenant, call.Arg<int>(), call.Arg<CancellationToken>()));
        owned.CompleteAsync(record, Arg.Any<CancellationToken>()).Returns(call => registry.CompleteAsync(record, call.Arg<CancellationToken>()));
        using var http = new HttpClient();
        var conversation = Substitute.For<DaprConversationClient>(null!, http, null!);
        conversation.ConverseAsync(Arg.Any<IReadOnlyList<ConversationInput>>(), Arg.Any<ConversationOptions>(), Arg.Any<CancellationToken>())
            .Returns(_ => { entered.TrySetResult(); return release.Task; });
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
        var options = new NaturalLanguageDescriptionOptions { RetryWorkerEnabled = true };
        var monitor = Substitute.For<IOptionsMonitor<NaturalLanguageDescriptionOptions>>();
        monitor.CurrentValue.Returns(options);
        try
        {
            using IHost host = Host.CreateDefaultBuilder().ConfigureServices(services =>
            {
                services.AddKeyedSingleton<IConnectionMultiplexer>("redis", redis.Connection);
                services.AddSingleton(conversation).AddSingleton(Options.Create(options)).AddSingleton(embedding)
                    .AddSingleton(actors).AddSingleton(provider).AddSingleton(Substitute.For<IJitterSource>());
                services.AddDaprWorkflow(workflows =>
                {
                    workflows.RegisterWorkflow<NaturalLanguageEmbeddingRetryWorkflow>();
                    workflows.RegisterActivity<RetryNaturalLanguageEmbeddingActivity>();
                });
            }).Build();
            await host.StartAsync(token);
            var client = host.Services.GetRequiredService<DaprWorkflowClient>();
            var worker = new NaturalLanguageEmbeddingRetryHostedService(owned, client, monitor, NullLogger<NaturalLanguageEmbeddingRetryHostedService>.Instance);
            Task tick = worker.TickAsync(options, token);
            await entered.Task.WaitAsync(token);
            WorkflowState running = await client.GetWorkflowStateAsync(id, true, token);
            running.ReadInputAs<NaturalLanguageEmbeddingRetryInput>().ShouldNotBeNull().RawJsonPayload.ShouldBeEmpty();
            (await db.HashGetAsync(FailedNaturalLanguageEmbeddingRegistry.LivePayloadKey(tenant), unit)).ToString().ShouldContain(sentinel);
            await AssertHistoryExcludesAsync();
            release.SetResult(new ConversationResponse([new ConversationResponseResult([new ConversationResultChoice(null, 0, new ResultMessage("An admitted action occurred."))])], string.Empty));
            await tick.WaitAsync(token);
            WorkflowState completed = await client.GetWorkflowStateAsync(id, true, token);
            completed.RuntimeStatus.ShouldBe(WorkflowRuntimeStatus.Completed);
            completed.ReadOutputAs<NaturalLanguageEmbeddingRetryResult>().ShouldNotBeNull().Indexed.ShouldBeTrue();
            await AssertHistoryExcludesAsync();
            string output = IndexSchemaDefinitions.BuildNaturalLanguageSemanticKey(tenant, unit);
            (await db.HashGetAsync(output, "naturalLanguageDescription")).ToString().ShouldBe("An admitted action occurred.");
            ((byte[]?)(await db.HashGetAsync(output, "embedding")))!.Length.ShouldBe(config.Dimensions * sizeof(float));
            (await db.HashGetAsync(survivor, "naturalLanguageDescription")).ToString().ShouldBe("unchanged");
            (await db.HashGetAsync(FailedNaturalLanguageEmbeddingRegistry.LivePayloadKey(foreignTenant), foreign.MemoryUnitId)).ShouldBe(foreignBefore);
            (await db.HashGetAsync(FailedNaturalLanguageEmbeddingRegistry.LivePayloadKey(tenant), unit)).HasValue.ShouldBeFalse();
            await RetryWorkflowErasure.PurgeAsync(client, tenant, unit, token);
            await host.StopAsync(token);
        }
        finally
        {
            release.TrySetCanceled();
            Environment.SetEnvironmentVariable("DAPR_GRPC_ENDPOINT", previous);
        }

        async Task AssertHistoryExcludesAsync()
        {
            RedisKey[] keys = await FindWorkflowKeysAsync(id, token);
            keys.ShouldNotBeEmpty();
            foreach (RedisKey key in keys)
            {
                (await db.KeyTypeAsync(key)).ShouldBe(RedisType.Hash);
                foreach (HashEntry field in await db.HashGetAllAsync(key))
                    field.Value.ToString().ShouldNotContain(sentinel);
            }
        }
    }

    private static async IAsyncEnumerable<string> OwnedTenantAsync(string tenant)
    {
        yield return tenant;
        await Task.CompletedTask;
    }

    private async Task<RedisKey[]> FindWorkflowKeysAsync(string id, CancellationToken token)
    {
        var server = redis.Connection.GetServer(redis.Connection.GetEndPoints().Single());
        List<RedisKey> keys = [];
        await foreach (RedisKey key in server.KeysAsync(pattern: "*" + id + "*", pageSize: 100).WithCancellation(token))
        {
            keys.Add(key);
        }
        return keys.ToArray();
    }

    private async Task WithClientAsync(Func<IDaprWorkflowClient, CancellationToken, Task> action)
    {
        string endpoint = _sidecar.DaprGrpcEndpoint;
        new Uri(endpoint).Host.ShouldBe("127.0.0.1");
        string? previous = Environment.GetEnvironmentVariable("DAPR_GRPC_ENDPOINT");
        Environment.SetEnvironmentVariable("DAPR_GRPC_ENDPOINT", endpoint);
        try
        {
            using IHost host = Host.CreateDefaultBuilder().ConfigureServices(services =>
                services.AddDaprWorkflow(options => options.RegisterWorkflow<LegacyRetryErasureQualificationWorkflow>())).Build();
            using CancellationTokenSource deadline = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
            deadline.CancelAfter(TimeSpan.FromSeconds(60));
            await host.StartAsync(deadline.Token);
            await action(host.Services.GetRequiredService<DaprWorkflowClient>(), deadline.Token);
            await host.StopAsync(deadline.Token);
        }
        finally
        {
            Environment.SetEnvironmentVariable("DAPR_GRPC_ENDPOINT", previous);
        }
    }

    private static JsonElement Input(string tenant, string unit, string raw)
        => JsonSerializer.SerializeToElement(new NaturalLanguageEmbeddingRetryInput(tenant, unit, raw, "Synthetic", null, "case", "test", "test", 1));
}
