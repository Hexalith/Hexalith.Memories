#pragma warning disable DAPR_CONVERSATION
namespace Hexalith.Memories.IntegrationTests.Ingestion;

using System.Text;
using System.Text.Json;
using Dapr.Workflow;
using Dapr.AI.Conversation;
using Hexalith.Memories.Server.Activities.Ingestion;
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
public sealed class RetryWorkflowErasureIntegrationTests(RedisStackFixture redis) : IAsyncLifetime
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

    [Fact]
    public async Task ContentBearingLegacyActivityCannotRestoreHistoryAfterTerminationPurgeLateCompletionAndRestart()
    {
        string previous = Environment.GetEnvironmentVariable("DAPR_GRPC_ENDPOINT") ?? string.Empty;
        Environment.SetEnvironmentVariable("DAPR_GRPC_ENDPOINT", _sidecar.DaprGrpcEndpoint);
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
                    options.RegisterActivity<GenerateNaturalLanguageDescriptionActivity>(); });
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
            await db.StringSetAsync(SourcePublicationVisibility.Tombstone(tenant, unit), "1");
            await RetryWorkflowErasure.PurgeAsync(client, tenant, unit, token, legacySchedulersDrained: true);
            (await FindWorkflowKeysAsync(id, token)).ShouldBeEmpty();
            release.TrySetResult(new ConversationResponse([new ConversationResponseResult([new ConversationResultChoice(null, 0, new ResultMessage(raw))])], string.Empty));
            // Observe actual late activity completion, then recreate both worker and persisted adapter.
            await Task.Delay(1500, token);
            await host.StopAsync(token);
            using var restartedRedis = await ConnectionMultiplexer.ConnectAsync(redis.ConnectionString);
            using IHost restarted = Host.CreateDefaultBuilder().ConfigureServices(services => services.AddDaprWorkflow(options =>
                options.RegisterWorkflow<ContentBearingLegacyRetryWorkflow>())).Build();
            await restarted.StartAsync(token);
            var restartedClient = restarted.Services.GetRequiredService<DaprWorkflowClient>();
            await RetryWorkflowErasure.PurgeAsync(restartedClient, tenant, unit, token, legacySchedulersDrained: true);
            (await restartedClient.GetWorkflowStateAsync(id, true, token)).Exists.ShouldBeFalse();
            (await FindWorkflowKeysAsync(id, token)).ShouldBeEmpty();
            (await restartedRedis.GetDatabase().KeyExistsAsync(SourcePublicationVisibility.Tombstone(tenant, unit))).ShouldBeTrue();
            await restarted.StopAsync(token);
        }
        finally { release.TrySetCanceled(); Environment.SetEnvironmentVariable("DAPR_GRPC_ENDPOINT", string.IsNullOrEmpty(previous) ? null : previous); }
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
