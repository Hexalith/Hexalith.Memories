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
using Hexalith.Memories.Server.Activities.Indexing;
using Hexalith.Memories.Server.Activities.Cases;
using Hexalith.Memories.Server.Activities.Tenants;
using Hexalith.Memories.EventStore;
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
    private readonly DaprStateSidecarFixture _sidecar = DaprStateSidecarFixture.CreateWorkflow(redis.ConnectionString, authenticate: true);
    private string? _previousApiToken;

    public async ValueTask InitializeAsync()
    {
        _previousApiToken = Environment.GetEnvironmentVariable("DAPR_API_TOKEN");
        await _sidecar.InitializeAsync();
        Environment.SetEnvironmentVariable("DAPR_API_TOKEN", _sidecar.ApiToken);
    }

    public async ValueTask DisposeAsync()
    {
        try { await _sidecar.DisposeAsync(); }
        finally { Environment.SetEnvironmentVariable("DAPR_API_TOKEN", _previousApiToken); }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task OriginalSameNameRawRetrySuspendedInEachContentActivityTerminatesAfterWorkerUpgradeAndPurgesHistory(bool embeddingPhase)
    {
        string? previous = Environment.GetEnvironmentVariable("DAPR_GRPC_ENDPOINT");
        Environment.SetEnvironmentVariable("DAPR_GRPC_ENDPOINT", _sidecar.DaprGrpcEndpoint);
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        deadline.CancelAfter(TimeSpan.FromSeconds(90));
        var token = deadline.Token;
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var activityReturned = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        string tenant = "same-name-upgrade-" + Guid.NewGuid().ToString("N");
        const string unit = "victim";
        string raw = "historical-raw-sentinel-" + Guid.NewGuid().ToString("N");
        string description = "historical-derived-sentinel-" + Guid.NewGuid().ToString("N");
        string id = NaturalLanguageEmbeddingRetryHostedService.GetLegacyRetryWorkflowInstanceId(tenant, unit);
        IDatabase db = redis.Connection.GetDatabase();
        string survivor = IndexSchemaDefinitions.BuildNaturalLanguageSemanticKey(tenant, "survivor");
        await db.HashSetAsync(survivor, "naturalLanguageDescription", "unrelated-content");
        await db.HashSetAsync(IndexSchemaDefinitions.BuildSyntacticKey(tenant, unit),
            [new("id", unit), new("tenantId", tenant), new("caseId", "case"), new("sourceUri", "urn:original")]);
        int contentCallsAfterUpgrade = 0;
        var config = EmbeddingProviderDefaults.Google();
        const string modernDescription = "Modern replacement succeeded.";
        var registry = new FailedNaturalLanguageEmbeddingRegistry(redis.Connection, NullLogger<FailedNaturalLanguageEmbeddingRegistry>.Instance);
        var retainedAdmission = new FailedNaturalLanguageEmbeddingRecord(tenant, unit, raw, "TestEvent", null, "case", config.Provider, config.Model, config.Dimensions, 1, 0);
        await registry.EnqueueAsync(retainedAdmission, token);
        string admittedBytes = (await db.HashGetAsync(FailedNaturalLanguageEmbeddingRegistry.LivePayloadKey(tenant), unit)).ToString();
        var options = new NaturalLanguageDescriptionOptions { RetryWorkerEnabled = true };
        var monitor = Substitute.For<IOptionsMonitor<NaturalLanguageDescriptionOptions>>(); monitor.CurrentValue.Returns(options);
        using var http = new HttpClient();
        var conversation = Substitute.For<DaprConversationClient>(null!, http, null!);
        conversation.ConverseAsync(Arg.Any<IReadOnlyList<ConversationInput>>(), Arg.Any<ConversationOptions>(), Arg.Any<CancellationToken>())
            .Returns(new ConversationResponse([new ConversationResponseResult([new ConversationResultChoice(null, 0, new ResultMessage(modernDescription))])], string.Empty));
        var environment = Substitute.For<IHostEnvironment>(); environment.EnvironmentName.Returns("Development");
        var embedding = Substitute.For<EmbeddingClient>(Substitute.For<IHttpClientFactory>(), Substitute.For<Dapr.Client.DaprClient>(), new ConfigurationBuilder().Build(), environment);
        embedding.PrimeApiKeyAsync(Arg.Any<string>(), Arg.Any<TenantEmbeddingConfig>(), Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);
        float[] vector = new float[config.Dimensions]; vector[0] = 1;
        embedding.GenerateAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<TenantEmbeddingConfig>(), Arg.Any<CancellationToken>()).Returns(vector);
        var actors = Substitute.For<IActorProxyFactory>();
        var limiter = Substitute.For<IEmbeddingRateLimiterActor>(); limiter.TryConsumeWithCeilingAsync(Arg.Any<int>()).Returns(true);
        actors.CreateActorProxy<IEmbeddingRateLimiterActor>(Arg.Any<ActorId>(), Arg.Any<string>()).Returns(limiter);
        var provider = Substitute.For<ITenantEmbeddingConfigProvider>(); provider.GetAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(config);
        try
        {
            using IHost original = Host.CreateDefaultBuilder().ConfigureServices(services =>
            {
                services.AddKeyedSingleton<IConnectionMultiplexer>("redis", redis.Connection);
                services.AddDaprWorkflow(options =>
                {
                    // The registered wire name and original activity order are the deployed protocol.
                    options.RegisterWorkflow<HistoricalNaturalLanguageEmbeddingRetryWorkflow>(nameof(NaturalLanguageEmbeddingRetryWorkflow));
                    options.RegisterActivity<CheckMemoryUnitExistsActivity>();
                    options.RegisterActivity<NaturalLanguageDescriptionInput, NaturalLanguageDescriptionResult>(nameof(GenerateNaturalLanguageDescriptionActivity), async (_, input) =>
                    {
                        input.RawJsonPayload.ShouldBe(raw);
                        if (!embeddingPhase) { entered.TrySetResult(); await release.Task; activityReturned.TrySetResult(); }
                        return new(description, null, ConfidenceSource.Constant, "test", "test");
                    });
                    options.RegisterActivity<EmbeddingInput, EmbeddingResult>(nameof(GenerateEmbeddingActivity), async (_, _) =>
                    {
                        if (embeddingPhase) { entered.TrySetResult(); await release.Task; activityReturned.TrySetResult(); }
                        return new([1f, 0f, 0f], "test", 3) { Model = "test" };
                    });
                });
            }).Build();
            await original.StartAsync(token);
            var oldClient = original.Services.GetRequiredService<DaprWorkflowClient>();
            // Persist the original nine-field input shape, without the later PayloadVersion member.
            var historicalInput = JsonSerializer.SerializeToElement(new
            {
                retainedAdmission.TenantId, retainedAdmission.MemoryUnitId, RawJsonPayload = retainedAdmission.TruncatedRawJsonPayload,
                retainedAdmission.EventType, retainedAdmission.AggregateType, retainedAdmission.CaseId,
                retainedAdmission.EmbeddingProvider, retainedAdmission.EmbeddingModel, retainedAdmission.EmbeddingDimensions,
            });
            await oldClient.ScheduleNewWorkflowAsync(typeof(NaturalLanguageEmbeddingRetryWorkflow).Name, id, historicalInput);
            await entered.Task.WaitAsync(token);
            await oldClient.SuspendWorkflowAsync(id, "owned-upgrade-boundary", token);
            while ((await oldClient.GetWorkflowStateAsync(id, true, token)).RuntimeStatus != WorkflowRuntimeStatus.Suspended)
                await Task.Delay(25, token);
            RedisKey[] historicalKeys = await FindWorkflowKeysAsync(id, token);
            historicalKeys.ShouldNotBeEmpty();
            var suspended = await oldClient.GetWorkflowStateAsync(id, true, token);
            suspended.ReadInputAs<NaturalLanguageEmbeddingRetryInput>()!.RawJsonPayload.ShouldBe(raw);
            suspended.ReadInputAs<JsonElement>().EnumerateObject().Count().ShouldBe(9);
            suspended.ReadInputAs<JsonElement>().TryGetProperty("PayloadVersion", out _).ShouldBeFalse();
            await original.StopAsync(token);

            using IHost upgraded = BuildUpgradedHost();
            await upgraded.StartAsync(token);
            var client = upgraded.Services.GetRequiredService<DaprWorkflowClient>();
            (await client.GetWorkflowStateAsync(id, true, token)).RuntimeStatus.ShouldBe(WorkflowRuntimeStatus.Suspended);
            await client.ResumeWorkflowAsync(id, "owned-upgraded-worker", token);
            WorkflowState terminal = await client.WaitForWorkflowCompletionAsync(id, true, token);
            terminal.RuntimeStatus.ShouldBe(WorkflowRuntimeStatus.Completed);
            var result = terminal.ReadOutputAs<NaturalLanguageEmbeddingRetryResult>().ShouldNotBeNull();
            result.Indexed.ShouldBeFalse();
            result.Reason.ShouldBe("legacy-retry-input-requires-restart");
            contentCallsAfterUpgrade.ShouldBe(0);
            release.Task.IsCompleted.ShouldBeFalse();
            activityReturned.Task.IsCompleted.ShouldBeFalse();
            (await db.HashGetAsync(FailedNaturalLanguageEmbeddingRegistry.LivePayloadKey(tenant), unit)).ToString().ShouldBe(admittedBytes);
            // The upgraded scheduler must progress the admission retained from before the old worker.

            db.FT().Create(IndexSchemaDefinitions.GetNaturalLanguageSemanticIndexName(tenant),
                IndexSchemaDefinitions.CreateNaturalLanguageSemanticParams(tenant), IndexSchemaDefinitions.CreateNaturalLanguageSemanticSchema(config.Dimensions));
            await new NaturalLanguageEmbeddingRetryHostedService(CreateOwnedRegistry(registry, tenant), client, monitor,
                NullLogger<NaturalLanguageEmbeddingRetryHostedService>.Instance, redis.Connection).TickAsync(options, token);
            string modernId = NaturalLanguageEmbeddingRetryHostedService.GetRetryWorkflowInstanceId(tenant, unit);
            WorkflowState modern = await client.GetWorkflowStateAsync(modernId, true, token);
            modern.RuntimeStatus.ShouldBe(WorkflowRuntimeStatus.Completed);
            modern.ReadInputAs<NaturalLanguageEmbeddingRetryInput>()!.RawJsonPayload.ShouldBeEmpty();
            modern.ReadInputAs<NaturalLanguageEmbeddingRetryInput>()!.PayloadVersion.ShouldBe(FailedNaturalLanguageEmbeddingRegistry.PayloadVersion(retainedAdmission));
            modern.ReadOutputAs<NaturalLanguageEmbeddingRetryResult>()!.Indexed.ShouldBeTrue();
            (await db.HashGetAsync(IndexSchemaDefinitions.BuildNaturalLanguageSemanticKey(tenant, unit), "naturalLanguageDescription")).ToString().ShouldBe(modernDescription);
            (await db.HashGetAsync(FailedNaturalLanguageEmbeddingRegistry.LivePayloadKey(tenant), unit)).HasValue.ShouldBeFalse();
            release.Task.IsCompleted.ShouldBeFalse();
            activityReturned.Task.IsCompleted.ShouldBeFalse();
            // This fixture owns every unit artifact and quiesced its sole legacy scheduler.
            await db.StringSetAsync(SourcePublicationVisibility.KeyProvenance(tenant, unit), "modern");
            await new DeleteMemoryUnitProjectionActivity(redis.Connection, graph.Connection, new GraphQueryBuilder(), workflowClient: client,
                retryOptions: Options.Create(new NaturalLanguageDescriptionOptions { LegacyRetrySchedulersDrainedForErasure = true }))
                .RunAsync(null!, new(tenant, "case", unit, []));
            (await FindWorkflowKeysAsync(id, token)).ShouldBeEmpty();
            foreach (var key in historicalKeys) (await db.KeyExistsAsync(key)).ShouldBeFalse();
            (await db.KeyExistsAsync(IndexSchemaDefinitions.BuildSyntacticKey(tenant, unit))).ShouldBeFalse();
            (await db.KeyExistsAsync(IndexSchemaDefinitions.BuildNaturalLanguageSemanticKey(tenant, unit))).ShouldBeFalse();
            (await client.GetWorkflowStateAsync(modernId, true, token)).Exists.ShouldBeFalse();
            activityReturned.Task.IsCompleted.ShouldBeFalse();
            release.TrySetResult();
            await activityReturned.Task.WaitAsync(token);
            (await db.HashGetAsync(survivor, "naturalLanguageDescription")).ToString().ShouldBe("unrelated-content");
            await upgraded.StopAsync(token);
            using IHost restarted = BuildUpgradedHost();
            await restarted.StartAsync(token);
            var restartedClient = restarted.Services.GetRequiredService<DaprWorkflowClient>();
            (await restartedClient.GetWorkflowStateAsync(id, true, token)).Exists.ShouldBeFalse();
            (await FindWorkflowKeysAsync(id, token)).ShouldBeEmpty();
            contentCallsAfterUpgrade.ShouldBe(0);
            (await db.HashGetAsync(survivor, "naturalLanguageDescription")).ToString().ShouldBe("unrelated-content");
            (await restartedClient.GetWorkflowStateAsync(modernId, true, token)).Exists.ShouldBeFalse();
            (await db.KeyExistsAsync(IndexSchemaDefinitions.BuildSyntacticKey(tenant, unit))).ShouldBeFalse();
            (await db.KeyExistsAsync(IndexSchemaDefinitions.BuildNaturalLanguageSemanticKey(tenant, unit))).ShouldBeFalse();
            (await db.HashGetAsync(FailedNaturalLanguageEmbeddingRegistry.LivePayloadKey(tenant), unit)).HasValue.ShouldBeFalse();
            await restarted.StopAsync(token);
        }
        finally { release.TrySetResult(); Environment.SetEnvironmentVariable("DAPR_GRPC_ENDPOINT", previous); }

        IHost BuildUpgradedHost() => Host.CreateDefaultBuilder().ConfigureServices(services =>
        {
            services.AddKeyedSingleton<IConnectionMultiplexer>("redis", redis.Connection);
            services.AddSingleton(conversation).AddSingleton(Options.Create(options)).AddSingleton(embedding)
                .AddSingleton(actors).AddSingleton(provider).AddSingleton(Substitute.For<IJitterSource>());
            services.AddLogging();
            services.AddDaprWorkflow(options =>
            {
                options.RegisterWorkflow<NaturalLanguageEmbeddingRetryWorkflow>();
                options.RegisterActivity<RetryNaturalLanguageEmbeddingActivity>();
                options.RegisterActivity<NaturalLanguageDescriptionInput, NaturalLanguageDescriptionResult>(nameof(GenerateNaturalLanguageDescriptionActivity), (_, _) =>
                { contentCallsAfterUpgrade++; throw new InvalidOperationException("legacy-description-must-not-reexecute"); });
                options.RegisterActivity<EmbeddingInput, EmbeddingResult>(nameof(GenerateEmbeddingActivity), (_, _) =>
                { contentCallsAfterUpgrade++; throw new InvalidOperationException("legacy-embedding-must-not-reexecute"); });
            });
        }).Build();
    }

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

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task WorkerSchedulesContentFreeDurableHistoryWhileAdmittedRegistryContentPersistsNaturalLanguageOutput(bool failAfterEmbedding)
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
        string description = "Derived business action " + Guid.NewGuid().ToString("N") + ".";
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
        string malformedTenant = tenant + "-malformed";
        string malformedBytes = FailedNaturalLanguageEmbeddingRegistry.SerializeRecord(record with { TenantId = malformedTenant, MemoryUnitId = "other-unit" });
        await db.SortedSetAddAsync(FailedNaturalLanguageEmbeddingRegistry.LiveKey(malformedTenant), "selected-unit", 1);
        await db.HashSetAsync(FailedNaturalLanguageEmbeddingRegistry.LivePayloadKey(malformedTenant), "selected-unit", malformedBytes);
        var owned = CreateOwnedRegistry(registry, malformedTenant, tenant);
        using var http = new HttpClient();
        var conversation = Substitute.For<DaprConversationClient>(null!, http, null!);
        conversation.ConverseAsync(Arg.Any<IReadOnlyList<ConversationInput>>(), Arg.Any<ConversationOptions>(), Arg.Any<CancellationToken>())
            .Returns(_ => { entered.TrySetResult(); return release.Task; });
        var environment = Substitute.For<IHostEnvironment>();
        environment.EnvironmentName.Returns("Development");
        var embedding = Substitute.For<EmbeddingClient>(Substitute.For<IHttpClientFactory>(), Substitute.For<Dapr.Client.DaprClient>(), new ConfigurationBuilder().Build(), environment);
        embedding.PrimeApiKeyAsync(Arg.Any<string>(), Arg.Any<TenantEmbeddingConfig>(), Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);
        float[] vector = new float[config.Dimensions]; vector[0] = 13579.25f; vector[1] = -24680.5f;
        byte[] vectorBytes = new byte[vector.Length * sizeof(float)]; Buffer.BlockCopy(vector, 0, vectorBytes, 0, vectorBytes.Length);
        bool generated = false;
        embedding.GenerateAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<TenantEmbeddingConfig>(), Arg.Any<CancellationToken>()).Returns(async call =>
        {
            call.ArgAt<string>(0).ShouldBe(description);
            generated = true;
            // Fail after both derived intermediates exist, before canonical publication.
            if (failAfterEmbedding) await db.ExecuteAsync("FT.DROPINDEX", IndexSchemaDefinitions.GetNaturalLanguageSemanticIndexName(tenant));
            return vector;
        });
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
            using IHost host = BuildHost();
            await host.StartAsync(token);
            var client = host.Services.GetRequiredService<DaprWorkflowClient>();
            var worker = new NaturalLanguageEmbeddingRetryHostedService(owned, client, monitor, NullLogger<NaturalLanguageEmbeddingRetryHostedService>.Instance, redis.Connection);
            Task tick = worker.TickAsync(options, token);
            await entered.Task.WaitAsync(token);
            WorkflowState running = await client.GetWorkflowStateAsync(id, true, token);
            running.ReadInputAs<NaturalLanguageEmbeddingRetryInput>().ShouldNotBeNull().RawJsonPayload.ShouldBeEmpty();
            (await db.HashGetAsync(FailedNaturalLanguageEmbeddingRegistry.LivePayloadKey(tenant), unit)).ToString().ShouldContain(sentinel);
            await AssertHistoryExcludesAsync();
            release.SetResult(new ConversationResponse([new ConversationResponseResult([new ConversationResultChoice(null, 0, new ResultMessage(description))])], string.Empty));
            await tick.WaitAsync(token);
            (await db.StringGetAsync(RetryWorkflowHistory.Unresolved(malformedTenant))).ToString().ShouldBe("1");
            (await db.HashGetAsync(FailedNaturalLanguageEmbeddingRegistry.LivePayloadKey(malformedTenant), "selected-unit")).ToString().ShouldBe(malformedBytes);
            WorkflowState completed = await client.GetWorkflowStateAsync(id, true, token);
            completed.RuntimeStatus.ShouldBe(failAfterEmbedding ? WorkflowRuntimeStatus.Failed : WorkflowRuntimeStatus.Completed);
            generated.ShouldBeTrue();
            if (!failAfterEmbedding) completed.ReadOutputAs<NaturalLanguageEmbeddingRetryResult>().ShouldNotBeNull().Indexed.ShouldBeTrue();
            await AssertHistoryExcludesAsync();
            string output = IndexSchemaDefinitions.BuildNaturalLanguageSemanticKey(tenant, unit);
            if (failAfterEmbedding) (await db.KeyExistsAsync(output)).ShouldBeFalse();
            else
            {
                (await db.HashGetAsync(output, "naturalLanguageDescription")).ToString().ShouldBe(description);
                ((byte[]?)(await db.HashGetAsync(output, "embedding")))!.ShouldBe(vectorBytes);
            }
            (await db.HashGetAsync(survivor, "naturalLanguageDescription")).ToString().ShouldBe("unchanged");
            (await db.HashGetAsync(FailedNaturalLanguageEmbeddingRegistry.LivePayloadKey(foreignTenant), foreign.MemoryUnitId)).ShouldBe(foreignBefore);
            (await db.HashGetAsync(FailedNaturalLanguageEmbeddingRegistry.LivePayloadKey(tenant), unit)).HasValue.ShouldBe(failAfterEmbedding);
            if (!failAfterEmbedding)
            {
                generated = false;
                description = "A newer business action " + Guid.NewGuid().ToString("N") + ".";
                release = new TaskCompletionSource<ConversationResponse>(TaskCreationOptions.RunContinuationsAsynchronously);
                release.SetResult(new ConversationResponse([new ConversationResponseResult([new ConversationResultChoice(null, 0, new ResultMessage(description))])], string.Empty));
                var replacement = record with { TruncatedRawJsonPayload = "replacement-raw", QueuedAtTicks = 2 };
                await registry.EnqueueAsync(replacement, token);
                await worker.TickAsync(options, token);
                generated.ShouldBeTrue();
                var replaced = await client.GetWorkflowStateAsync(id, true, token);
                replaced.RuntimeStatus.ShouldBe(WorkflowRuntimeStatus.Completed);
                replaced.ReadInputAs<NaturalLanguageEmbeddingRetryInput>().ShouldNotBeNull().PayloadVersion.ShouldBe(FailedNaturalLanguageEmbeddingRegistry.PayloadVersion(replacement));
                (await db.HashGetAsync(output, "naturalLanguageDescription")).ToString().ShouldBe(description);
                (await db.HashGetAsync(FailedNaturalLanguageEmbeddingRegistry.LivePayloadKey(tenant), unit)).HasValue.ShouldBeFalse();
                (await db.SetContainsAsync(RetryWorkflowHistory.Units(tenant), unit)).ShouldBeTrue();
            }
            await host.StopAsync(token);
            using IHost restarted = BuildHost();
            await restarted.StartAsync(token);
            var restartedClient = restarted.Services.GetRequiredService<DaprWorkflowClient>();
            await new NaturalLanguageEmbeddingRetryHostedService(owned, restartedClient, monitor, NullLogger<NaturalLanguageEmbeddingRetryHostedService>.Instance, redis.Connection)
                .TickAsync(options, token);
            (await restartedClient.GetWorkflowStateAsync(id, true, token)).RuntimeStatus.ShouldBe(completed.RuntimeStatus);
            await AssertHistoryExcludesAsync();
            if (!failAfterEmbedding)
            {
                (await db.HashGetAsync(output, "naturalLanguageDescription")).ToString().ShouldBe(description);
                ((byte[]?)(await db.HashGetAsync(output, "embedding")))!.ShouldBe(vectorBytes);
            }
            (await db.HashGetAsync(survivor, "naturalLanguageDescription")).ToString().ShouldBe("unchanged");
            (await db.HashGetAsync(FailedNaturalLanguageEmbeddingRegistry.LivePayloadKey(foreignTenant), foreign.MemoryUnitId)).ShouldBe(foreignBefore);
            await RetryWorkflowErasure.PurgeAsync(restartedClient, tenant, unit, token);
            await restarted.StopAsync(token);
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
                    AssertPrivateBytesAbsent((byte[])field.Value!);
            }
        }

        IHost BuildHost() => Host.CreateDefaultBuilder().ConfigureServices(services =>
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

        void AssertPrivateBytesAbsent(byte[] bytes, int depth = 0)
        {
            string text = Encoding.UTF8.GetString(bytes);
            text.ShouldNotContain(sentinel);
            text.ShouldNotContain(description);
            text.ShouldNotContain(vector[0].ToString("R", System.Globalization.CultureInfo.InvariantCulture));
            text.ShouldNotContain(vector[1].ToString("R", System.Globalization.CultureInfo.InvariantCulture));
            bytes.AsSpan().IndexOf(vectorBytes.AsSpan(0, 2 * sizeof(float))).ShouldBe(-1);
            if (depth >= 6) return;
            try
            {
                byte[] decoded = Convert.FromBase64String(text);
                if (decoded.Length > 0) AssertPrivateBytesAbsent(decoded, depth + 1);
            }
            catch (FormatException) { }
            try
            {
                using var json = JsonDocument.Parse(bytes);
                Inspect(json.RootElement);
            }
            catch (JsonException) { }
            void Inspect(JsonElement element)
            {
                if (element.ValueKind == JsonValueKind.String)
                    AssertPrivateBytesAbsent(Encoding.UTF8.GetBytes(element.GetString()!), depth + 1);
                else if (element.ValueKind == JsonValueKind.Object)
                    foreach (var field in element.EnumerateObject()) Inspect(field.Value);
                else if (element.ValueKind == JsonValueKind.Array)
                    foreach (var item in element.EnumerateArray()) Inspect(item);
            }
        }
    }

    /// <summary>
    /// Verifies tenant Deletion Purges Live Dead And Completed Or Running History While Unknown Coverage And Scheduling Remain Blocked.
    /// </summary>
    [Fact]
    public async Task TenantDeletionPurgesLiveDeadAndCompletedOrRunningHistoryWhileUnknownCoverageAndSchedulingRemainBlocked()
    {
        await WithClientAsync(async (client, token) =>
        {
            string tenant = "tenant-retry-erasure-" + Guid.NewGuid().ToString("N");
            string foreign = tenant + "-foreign";
            IDatabase db = redis.Connection.GetDatabase();
            var registry = new FailedNaturalLanguageEmbeddingRegistry(redis.Connection, NullLogger<FailedNaturalLanguageEmbeddingRegistry>.Instance);
            var live = new FailedNaturalLanguageEmbeddingRecord(tenant, "live", "synthetic-live", "Event", null, "case", "test", "test", 1, 1, 0);
            var dead = live with { MemoryUnitId = "dead", TruncatedRawJsonPayload = "synthetic-dead" };
            await registry.EnqueueAsync(live, token);
            await registry.EnqueueAsync(dead, token);
            await registry.IncrementAttemptsAsync(dead, 1, token);
            await registry.EnqueueAsync(live with { TenantId = foreign }, token);
            string running = NaturalLanguageEmbeddingRetryHostedService.GetRetryWorkflowInstanceId(tenant, "live");
            string completed = NaturalLanguageEmbeddingRetryHostedService.GetRetryWorkflowInstanceId(tenant, "history-only");
            string foreignId = NaturalLanguageEmbeddingRetryHostedService.GetRetryWorkflowInstanceId(foreign, "live");
            await client.ScheduleNewWorkflowAsync(nameof(LegacyRetryErasureQualificationWorkflow), running, Input(tenant, "live", "synthetic-history"));
            await client.ScheduleNewWorkflowAsync(nameof(LegacyRetryErasureQualificationWorkflow), foreignId, Input(foreign, "live", "foreign-history"));
            await client.ScheduleNewWorkflowAsync(nameof(NaturalLanguageEmbeddingRetryWorkflow), completed,
                new NaturalLanguageEmbeddingRetryInput(tenant, "history-only", string.Empty, "Event", null, "case", "test", "test", 1));
            await client.WaitForWorkflowStartAsync(running, true, token);
            await client.WaitForWorkflowStartAsync(foreignId, true, token);
            (await client.WaitForWorkflowCompletionAsync(completed, true, token)).RuntimeStatus.ShouldBe(WorkflowRuntimeStatus.Completed);
            await db.SetAddAsync(RetryWorkflowHistory.Units(tenant), "history-only");
            (await FindWorkflowKeysAsync(running, token)).ShouldNotBeEmpty();
            (await FindWorkflowKeysAsync(completed, token)).ShouldNotBeEmpty();
            using var http = new HttpClient();
            string metadata = _sidecar.DaprHttpEndpoint + "/v1.0/metadata";
            (await http.GetAsync(metadata, token)).StatusCode.ShouldBe(System.Net.HttpStatusCode.Unauthorized);
            http.DefaultRequestHeaders.Add("dapr-api-token", "incorrect-owned-token");
            (await http.GetAsync(metadata, token)).StatusCode.ShouldBe(System.Net.HttpStatusCode.Unauthorized);
            http.DefaultRequestHeaders.Remove("dapr-api-token");
            http.DefaultRequestHeaders.Add("dapr-api-token", _sidecar.ApiToken);
            (await http.GetAsync(metadata, token)).IsSuccessStatusCode.ShouldBeTrue();
            var lifetime = Substitute.For<IHostApplicationLifetime>();
            lifetime.ApplicationStopping.Returns(token);
            var activity = new DeleteTenantDataKeysActivity(redis.Connection, Substitute.For<IAggregateCaseMappingStore>(),
                Substitute.For<IObservedEventTypeStore>(), lifetime, NullLogger<DeleteTenantDataKeysActivity>.Instance,
                null, client, Options.Create(new NaturalLanguageDescriptionOptions()));
            (await Should.ThrowAsync<InvalidOperationException>(() => activity.RunAsync(null!, new(tenant)))).Message.ShouldBe("nl-retry-history-owner-reconciliation-required");
            (await db.KeyExistsAsync(TenantIngestionOwnership.Fence(tenant))).ShouldBeTrue();
            // Only this disposable fixture owner knows every history it created; no production coverage is attested.
            await db.StringSetAsync(RetryWorkflowHistory.Coverage(tenant), RetryWorkflowHistory.ReconciledCoverage);
            await db.HashSetAsync(RetryWorkflowHistory.Scheduling(tenant), "in-flight-attempt", "in-flight");
            (await Should.ThrowAsync<InvalidOperationException>(() => activity.RunAsync(null!, new(tenant)))).Message.ShouldBe("nl-retry-scheduling-unresolved");
            await db.HashDeleteAsync(RetryWorkflowHistory.Scheduling(tenant), "in-flight-attempt");
            (await activity.RunAsync(null!, new(tenant))).ShouldBeTrue();
            foreach (string key in new[] { FailedNaturalLanguageEmbeddingRegistry.LiveKey(tenant), FailedNaturalLanguageEmbeddingRegistry.LivePayloadKey(tenant),
                FailedNaturalLanguageEmbeddingRegistry.DeadKey(tenant), FailedNaturalLanguageEmbeddingRegistry.DeadPayloadKey(tenant) })
                (await db.KeyExistsAsync(key)).ShouldBeFalse();
            (await FindWorkflowKeysAsync(running, token)).ShouldBeEmpty();
            (await FindWorkflowKeysAsync(completed, token)).ShouldBeEmpty();
            (await client.GetWorkflowStateAsync(running, true, token)).Exists.ShouldBeFalse();
            (await client.GetWorkflowStateAsync(completed, true, token)).Exists.ShouldBeFalse();
            (await client.GetWorkflowStateAsync(foreignId, true, token)).Exists.ShouldBeTrue();
            (await db.HashGetAsync(FailedNaturalLanguageEmbeddingRegistry.LivePayloadKey(foreign), "live")).HasValue.ShouldBeTrue();
            using var restarted = await ConnectionMultiplexer.ConnectAsync(redis.ConnectionString);
            await new FailedNaturalLanguageEmbeddingRegistry(restarted, NullLogger<FailedNaturalLanguageEmbeddingRegistry>.Instance).EnqueueAsync(live, token);
            (await db.KeyExistsAsync(FailedNaturalLanguageEmbeddingRegistry.LivePayloadKey(tenant))).ShouldBeFalse();
            await RetryWorkflowHistory.PurgeTenantAsync(restarted.GetDatabase(), client, tenant, false, token);
            (await FindWorkflowKeysAsync(running, token)).ShouldBeEmpty();
            (await FindWorkflowKeysAsync(completed, token)).ShouldBeEmpty();
            await RetryWorkflowErasure.PurgeAsync(client, foreign, "live", token);
        });
    }

    /// <summary>
    /// Verifies scoped Terminal Retirement Rejects ANewer Execution Before Purge And Preserves Its Actual History.
    /// </summary>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ScopedTerminalRetirementRejectsANewerExecutionBeforePurgeAndPreservesItsActualHistory(bool runningReplacement)
    {
        await WithClientAsync(async (client, token) =>
        {
            string tenant = "terminal-replaced-" + Guid.NewGuid().ToString("N");
            const string unit = "victim";
            string id = NaturalLanguageEmbeddingRetryHostedService.GetRetryWorkflowInstanceId(tenant, unit);
            var old = new NaturalLanguageEmbeddingRetryInput(tenant, unit, string.Empty, "Event", null, "case", "test", "test", 1) { PayloadVersion = "old-version" };
            await client.ScheduleNewWorkflowAsync(nameof(NaturalLanguageEmbeddingRetryWorkflow), id, old);
            var observed = await client.WaitForWorkflowCompletionAsync(id, true, token);
            // This fixture intentionally makes the out-of-band change before the explicit authoritative re-read.
            await client.PurgeInstanceAsync(id, token);
            if (runningReplacement)
            {
                await client.ScheduleNewWorkflowAsync(nameof(LegacyRetryErasureQualificationWorkflow), id, Input(tenant, unit, "synthetic-replacement"));
                await client.WaitForWorkflowStartAsync(id, true, token);
            }
            else
            {
                await client.ScheduleNewWorkflowAsync(nameof(NaturalLanguageEmbeddingRetryWorkflow), id, old with { PayloadVersion = "new-version" });
                await client.WaitForWorkflowCompletionAsync(id, true, token);
            }
            RedisKey[] replacementKeys = await FindWorkflowKeysAsync(id, token);
            replacementKeys.ShouldNotBeEmpty();
            (await Should.ThrowAsync<InvalidOperationException>(() => RetryWorkflowErasure.PurgeScopedAsync(client, tenant, unit, observed, token)))
                .Message.ShouldBe("nl-retry-workflow-instance-changed");
            var retained = await client.GetWorkflowStateAsync(id, true, token);
            retained.Exists.ShouldBeTrue();
            retained.RuntimeStatus.ShouldBe(runningReplacement ? WorkflowRuntimeStatus.Running : WorkflowRuntimeStatus.Completed);
            foreach (RedisKey key in replacementKeys) (await redis.Connection.GetDatabase().KeyExistsAsync(key)).ShouldBeTrue();
            await RetryWorkflowErasure.PurgeAsync(client, tenant, unit, token);
        });
    }

    /// <summary>
    /// Verifies stale Dequeued Record Cannot Retire Newer Terminal History After Taking The Lease.
    /// </summary>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task StaleDequeuedRecordCannotRetireNewerTerminalHistoryAfterTakingTheLease(bool replaceDuringHistoryRead)
    {
        await WithClientAsync(async (client, token) =>
        {
            string tenant = "stale-dequeue-" + Guid.NewGuid().ToString("N");
            const string unit = "victim";
            IDatabase db = redis.Connection.GetDatabase();
            var old = new FailedNaturalLanguageEmbeddingRecord(tenant, unit, "old", "Event", null, "case", "test", "test", 1, 1, 0);
            var newer = old with { TruncatedRawJsonPayload = "newer", QueuedAtTicks = 2 };
            string id = NaturalLanguageEmbeddingRetryHostedService.GetRetryWorkflowInstanceId(tenant, unit);
            var input = new NaturalLanguageEmbeddingRetryInput(tenant, unit, string.Empty, "Event", null, "case", "test", "test", 1)
                { PayloadVersion = FailedNaturalLanguageEmbeddingRegistry.PayloadVersion(newer) };
            await client.ScheduleNewWorkflowAsync(nameof(NaturalLanguageEmbeddingRetryWorkflow), id, input);
            WorkflowState original = await client.WaitForWorkflowCompletionAsync(id, true, token);
            RedisKey[] before = await FindWorkflowKeysAsync(id, token);
            before.ShouldNotBeEmpty();
            await db.HashSetAsync(IndexSchemaDefinitions.BuildSyntacticKey(tenant, unit), "memoryUnitId", unit);
            var registry = new FailedNaturalLanguageEmbeddingRegistry(redis.Connection, NullLogger<FailedNaturalLanguageEmbeddingRegistry>.Instance);
            await registry.EnqueueAsync(old, token);
            var owned = CreateOwnedRegistry(registry, tenant);
            owned.DequeueBatchAsync(tenant, Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns(async call =>
            {
                var dequeued = await registry.DequeueBatchAsync(tenant, call.Arg<int>(), call.Arg<CancellationToken>());
                if (!replaceDuringHistoryRead) await registry.EnqueueAsync(newer, token);
                return dequeued;
            });
            var adapter = Substitute.For<IDaprWorkflowClient>();
            adapter.GetWorkflowStateAsync(id, true, Arg.Any<CancellationToken>()).Returns(async call =>
            {
                if (replaceDuringHistoryRead) await registry.EnqueueAsync(newer, token);
                return await client.GetWorkflowStateAsync(id, true, call.Arg<CancellationToken>());
            });
            var options = new NaturalLanguageDescriptionOptions { RetryWorkerEnabled = true };
            var monitor = Substitute.For<IOptionsMonitor<NaturalLanguageDescriptionOptions>>(); monitor.CurrentValue.Returns(options);
            await new NaturalLanguageEmbeddingRetryHostedService(owned, adapter, monitor,
                NullLogger<NaturalLanguageEmbeddingRetryHostedService>.Instance, redis.Connection).TickAsync(options, token);
            await adapter.DidNotReceive().PurgeInstanceAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
            _ = await adapter.DidNotReceive().ScheduleNewWorkflowAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<NaturalLanguageEmbeddingRetryInput>());
            var retained = await client.GetWorkflowStateAsync(id, true, token);
            retained.CreatedAt.ShouldBe(original.CreatedAt);
            retained.ReadInputAs<NaturalLanguageEmbeddingRetryInput>().ShouldNotBeNull().PayloadVersion.ShouldBe(input.PayloadVersion);
            foreach (RedisKey key in before) (await db.KeyExistsAsync(key)).ShouldBeTrue();
            (await db.HashGetAsync(FailedNaturalLanguageEmbeddingRegistry.LivePayloadKey(tenant), unit)).ToString().ShouldBe(FailedNaturalLanguageEmbeddingRegistry.SerializeRecord(newer));
            (await db.KeyExistsAsync(RetryWorkflowHistory.Lease(tenant, unit))).ShouldBeFalse();
            await RetryWorkflowErasure.PurgeAsync(client, tenant, unit, token);
        });
    }

    /// <summary>
    /// Verifies actual Worker Unacknowledged Submission Retains Authority Across Recreation And Blocks Tenant Cleanup.
    /// </summary>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ActualWorkerUnacknowledgedSubmissionRetainsAuthorityAcrossRecreationAndBlocksTenantCleanup(bool interruptCaller)
    {
        await WithClientAsync(async (client, token) =>
        {
            string tenant = "unacknowledged-retry-" + Guid.NewGuid().ToString("N");
            const string unit = "victim";
            IDatabase db = redis.Connection.GetDatabase();
            await db.HashSetAsync(IndexSchemaDefinitions.BuildSyntacticKey(tenant, unit), "memoryUnitId", unit);
            var registry = new FailedNaturalLanguageEmbeddingRegistry(redis.Connection, NullLogger<FailedNaturalLanguageEmbeddingRegistry>.Instance);
            var record = new FailedNaturalLanguageEmbeddingRecord(tenant, unit, "synthetic", "Event", null, "case", "test", "test", 1, 1, 0);
            await registry.EnqueueAsync(record, token);
            var owned = CreateOwnedRegistry(registry, tenant);
            using var interrupted = CancellationTokenSource.CreateLinkedTokenSource(token);
            var adapter = Substitute.For<IDaprWorkflowClient>();
            adapter.GetWorkflowStateAsync(Arg.Any<string>(), true, Arg.Any<CancellationToken>())
                .Returns(call => client.GetWorkflowStateAsync(call.Arg<string>(), true, call.Arg<CancellationToken>()));
            string id = NaturalLanguageEmbeddingRetryHostedService.GetRetryWorkflowInstanceId(tenant, unit);
            bool submitted = false;
            Exception? submissionSetupFailure = null;
            async Task<string> SubmitWithoutAcknowledgement(NSubstitute.Core.CallInfo call)
            {
                try
                {
                    await client.ScheduleNewWorkflowAsync(call.ArgAt<string>(0), id, call.ArgAt<NaturalLanguageEmbeddingRetryInput>(2));
                    await client.WaitForWorkflowCompletionAsync(id, true, token);
                    WorkflowState completed = await client.GetWorkflowStateAsync(id, true, token);
                    RetryWorkflowErasure.ReadScope(completed).ShouldBe((tenant, unit));
                    submitted = true;
                }
                catch (Exception error) { submissionSetupFailure = error; throw; }
                if (interruptCaller)
                {
                    interrupted.Cancel();
                    throw new OperationCanceledException(interrupted.Token);
                }
                throw new TimeoutException("synthetic-unacknowledged-submission");
            }
            adapter.ScheduleNewWorkflowAsync(Arg.Any<string>(), id, Arg.Any<NaturalLanguageEmbeddingRetryInput>()).Returns(SubmitWithoutAcknowledgement);
            var options = new NaturalLanguageDescriptionOptions { RetryWorkerEnabled = true };
            var monitor = Substitute.For<IOptionsMonitor<NaturalLanguageDescriptionOptions>>(); monitor.CurrentValue.Returns(options);
            var logger = Substitute.For<Microsoft.Extensions.Logging.ILogger<NaturalLanguageEmbeddingRetryHostedService>>();
            logger.IsEnabled(Arg.Any<Microsoft.Extensions.Logging.LogLevel>()).Returns(true);
            var worker = new NaturalLanguageEmbeddingRetryHostedService(owned, adapter, monitor, logger, redis.Connection);
            OperationCanceledException? cancellation = null;
            try { await worker.TickAsync(options, interrupted.Token); }
            catch (OperationCanceledException error) { cancellation = error; }
            if (!submitted) throw new InvalidOperationException("controlled-scheduling-setup-failed: "
                + string.Join(",", adapter.ReceivedCalls().Select(call => call.GetMethodInfo().Name))
                + "; admitted=" + (await FailedNaturalLanguageEmbeddingRegistry.ReadCurrentAsync(db, tenant, unit) == record)
                + "; backlog=" + await registry.GetBacklogCountAsync(tenant, token), submissionSetupFailure
                ?? logger.ReceivedCalls().Select(call => call.GetArguments().OfType<Exception>().FirstOrDefault()).LastOrDefault(error => error is not null));
            if (interruptCaller) cancellation.ShouldNotBeNull();
            else cancellation.ShouldBeNull();
            string authority = (await db.StringGetAsync(RetryWorkflowHistory.Lease(tenant, unit))).ToString();
            authority.ShouldNotBeNullOrWhiteSpace();
            (await db.HashGetAsync(RetryWorkflowHistory.Scheduling(tenant), authority)).ToString().ShouldBe(unit);
            (await client.GetWorkflowStateAsync(id, true, token)).Exists.ShouldBeTrue();
            using var restarted = await ConnectionMultiplexer.ConnectAsync(redis.ConnectionString);
            var recreatedRegistry = new FailedNaturalLanguageEmbeddingRegistry(restarted, NullLogger<FailedNaturalLanguageEmbeddingRegistry>.Instance);
            var untouched = Substitute.For<IDaprWorkflowClient>();
            await new NaturalLanguageEmbeddingRetryHostedService(CreateOwnedRegistry(recreatedRegistry, tenant), untouched, monitor,
                NullLogger<NaturalLanguageEmbeddingRetryHostedService>.Instance, restarted).TickAsync(options, token);
            untouched.ReceivedCalls().ShouldBeEmpty();
            (await restarted.GetDatabase().HashGetAsync(RetryWorkflowHistory.Scheduling(tenant), authority)).ToString().ShouldBe(unit);
            (await Should.ThrowAsync<InvalidOperationException>(() => RetryWorkflowHistory.PurgeTenantAsync(restarted.GetDatabase(), client, tenant, false, token)))
                .Message.ShouldBe("nl-retry-scheduling-unresolved");
            (await db.KeyExistsAsync(RetryWorkflowHistory.Lease(tenant, unit))).ShouldBeTrue();
            // Only this fixture owns and quiesces the failed submitter; settle its exact token for teardown.
            await RetryWorkflowHistory.AcknowledgeAsync(db, tenant, unit, authority, token);
            await RetryWorkflowErasure.PurgeAsync(client, tenant, unit, token);
        });
    }

    /// <summary>
    /// Verifies authenticated Tenant History Discovery Purges Queue Only And Orphan Hash Units Absent From The Ledger.
    /// </summary>
    [Fact]
    public async Task AuthenticatedTenantHistoryDiscoveryPurgesQueueOnlyAndOrphanHashUnitsAbsentFromTheLedger()
    {
        await WithClientAsync(async (client, token) =>
        {
            string tenant = "queue-discovered-history-" + Guid.NewGuid().ToString("N");
            string foreign = tenant + "-foreign";
            IDatabase db = redis.Connection.GetDatabase();
            string[] units = ["live-only", "dead-only", "hash-only", "dead-hash-only"];
            foreach (string unit in units)
            {
                var record = new FailedNaturalLanguageEmbeddingRecord(tenant, unit, "synthetic", "Event", null, "case", "test", "test", 1, 1, 0);
                bool dead = unit.StartsWith("dead", StringComparison.Ordinal);
                string payloads = dead ? FailedNaturalLanguageEmbeddingRegistry.DeadPayloadKey(tenant) : FailedNaturalLanguageEmbeddingRegistry.LivePayloadKey(tenant);
                await db.HashSetAsync(payloads, unit, FailedNaturalLanguageEmbeddingRegistry.SerializeRecord(record));
                if (unit.EndsWith("only", StringComparison.Ordinal) && !unit.Contains("hash", StringComparison.Ordinal))
                    await db.SortedSetAddAsync(dead ? FailedNaturalLanguageEmbeddingRegistry.DeadKey(tenant) : FailedNaturalLanguageEmbeddingRegistry.LiveKey(tenant), unit, 1);
                string id = NaturalLanguageEmbeddingRetryHostedService.GetRetryWorkflowInstanceId(tenant, unit);
                await client.ScheduleNewWorkflowAsync(nameof(NaturalLanguageEmbeddingRetryWorkflow), id,
                    new NaturalLanguageEmbeddingRetryInput(tenant, unit, string.Empty, "Event", null, "case", "test", "test", 1));
                await client.WaitForWorkflowCompletionAsync(id, true, token);
                (await FindWorkflowKeysAsync(id, token)).ShouldNotBeEmpty();
            }
            (await db.SetLengthAsync(RetryWorkflowHistory.Units(tenant))).ShouldBe(0);
            string foreignId = NaturalLanguageEmbeddingRetryHostedService.GetRetryWorkflowInstanceId(foreign, "live-only");
            await client.ScheduleNewWorkflowAsync(nameof(LegacyRetryErasureQualificationWorkflow), foreignId, Input(foreign, "live-only", "foreign-history"));
            await client.WaitForWorkflowStartAsync(foreignId, true, token);
            string survivor = FailedNaturalLanguageEmbeddingRegistry.SerializeRecord(new(foreign, "live-only", "foreign-content", "Event", null, "case", "test", "test", 1, 1, 0));
            await db.HashSetAsync(FailedNaturalLanguageEmbeddingRegistry.LivePayloadKey(foreign), "live-only", survivor);
            await db.StringSetAsync(RetryWorkflowHistory.Coverage(tenant), RetryWorkflowHistory.ReconciledCoverage);
            await RetryWorkflowHistory.PurgeTenantAsync(db, client, tenant, false, token);
            foreach (string unit in units)
            {
                string id = NaturalLanguageEmbeddingRetryHostedService.GetRetryWorkflowInstanceId(tenant, unit);
                (await client.GetWorkflowStateAsync(id, true, token)).Exists.ShouldBeFalse();
                (await FindWorkflowKeysAsync(id, token)).ShouldBeEmpty();
            }
            foreach (string key in new[] { FailedNaturalLanguageEmbeddingRegistry.LiveKey(tenant), FailedNaturalLanguageEmbeddingRegistry.DeadKey(tenant),
                FailedNaturalLanguageEmbeddingRegistry.LivePayloadKey(tenant), FailedNaturalLanguageEmbeddingRegistry.DeadPayloadKey(tenant) })
                (await db.KeyExistsAsync(key)).ShouldBeFalse();
            (await client.GetWorkflowStateAsync(foreignId, true, token)).Exists.ShouldBeTrue();
            (await db.HashGetAsync(FailedNaturalLanguageEmbeddingRegistry.LivePayloadKey(foreign), "live-only")).ToString().ShouldBe(survivor);
            await RetryWorkflowErasure.PurgeAsync(client, foreign, "live-only", token);
        });
    }

    private static IFailedNaturalLanguageEmbeddingRegistry CreateOwnedRegistry(FailedNaturalLanguageEmbeddingRegistry registry, params string[] tenants)
    {
        var owned = Substitute.For<IFailedNaturalLanguageEmbeddingRegistry>();
        owned.ListTenantsWithBacklogAsync(Arg.Any<CancellationToken>()).Returns(_ => OwnedTenantsAsync(tenants));
        owned.GetBacklogCountAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(call => registry.GetBacklogCountAsync(call.Arg<string>(), call.Arg<CancellationToken>()));
        owned.DequeueBatchAsync(Arg.Any<string>(), Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns(call => registry.DequeueBatchAsync(call.Arg<string>(), call.Arg<int>(), call.Arg<CancellationToken>()));
        owned.CompleteAsync(Arg.Any<FailedNaturalLanguageEmbeddingRecord>(), Arg.Any<CancellationToken>()).Returns(call => registry.CompleteAsync(call.Arg<FailedNaturalLanguageEmbeddingRecord>(), call.Arg<CancellationToken>()));
        owned.IncrementAttemptsAsync(Arg.Any<FailedNaturalLanguageEmbeddingRecord>(), Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns(call => registry.IncrementAttemptsAsync(call.Arg<FailedNaturalLanguageEmbeddingRecord>(), call.Arg<int>(), call.Arg<CancellationToken>()));
        return owned;
    }

    private static async IAsyncEnumerable<string> OwnedTenantsAsync(string[] tenants)
    {
        foreach (string tenant in tenants) yield return tenant;
        await Task.CompletedTask;
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
            using var http = new HttpClient();
            var conversation = Substitute.For<DaprConversationClient>(null!, http, null!);
            conversation.ConverseAsync(Arg.Any<IReadOnlyList<ConversationInput>>(), Arg.Any<ConversationOptions>(), Arg.Any<CancellationToken>())
                .Returns(Task.FromException<ConversationResponse>(new NaturalLanguageDescriptionUnavailableException("synthetic-unavailable", "owned-test")));
            var environment = Substitute.For<IHostEnvironment>(); environment.EnvironmentName.Returns("Development");
            var embedding = Substitute.For<EmbeddingClient>(Substitute.For<IHttpClientFactory>(), Substitute.For<Dapr.Client.DaprClient>(), new ConfigurationBuilder().Build(), environment);
            using IHost host = Host.CreateDefaultBuilder().ConfigureServices(services =>
                {
                services.AddKeyedSingleton<IConnectionMultiplexer>("redis", redis.Connection);
                services.AddSingleton(conversation).AddSingleton(Options.Create(new NaturalLanguageDescriptionOptions()))
                    .AddSingleton(embedding).AddSingleton(Substitute.For<IActorProxyFactory>()).AddSingleton(Substitute.For<IJitterSource>());
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
            try { await action(host.Services.GetRequiredService<DaprWorkflowClient>(), deadline.Token); }
            finally { await host.StopAsync(deadline.Token); }
        }
        finally
        {
            Environment.SetEnvironmentVariable("DAPR_GRPC_ENDPOINT", previous);
        }
    }

    private static JsonElement Input(string tenant, string unit, string raw)
        => JsonSerializer.SerializeToElement(new NaturalLanguageEmbeddingRetryInput(tenant, unit, raw, "Synthetic", null, "case", "test", "test", 1));
}
