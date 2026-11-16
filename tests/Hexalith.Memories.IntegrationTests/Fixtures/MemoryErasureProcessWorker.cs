#pragma warning disable DAPR_CONVERSATION
namespace Hexalith.Memories.IntegrationTests.Fixtures;

using System.Diagnostics;
using System.IO.Pipes;
using System.Reflection;
using System.Text;
using System.Security.Cryptography;
using System.Text.Json;
using Dapr.Workflow;
using Grpc.Core;
using Shouldly;
using Dapr.AI.Conversation;
using Dapr.Actors;
using Dapr.Actors.Client;
using Hexalith.EventStore.Client.Queries;
using Hexalith.Memories.Contracts.V1;
using Hexalith.Memories.IntegrationTests.Ingestion;
using Hexalith.Memories.Server.Activities.Cases;
using Hexalith.Memories.Server.Activities.Indexing;
using Hexalith.Memories.Server.Activities.Ingestion;
using Hexalith.Memories.Server.Actors;
using Hexalith.Memories.Server.Graph;
using Hexalith.Memories.Server.Infrastructure;
using Hexalith.Memories.Server.Ingestion;
using Hexalith.Memories.Server.Migration;
using Hexalith.Memories.Server.NaturalLanguage;
using Hexalith.Memories.Server.Workflows;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using StackExchange.Redis;

/// <summary>Private pipe-only mode in the existing executable; ordinary discovery performs no worker operation.</summary>
public sealed class MemoryErasureProcessWorker
{
    /// <summary>Runs only when the parent supplied inherited private pipe capabilities and a random credential.</summary>
    [Fact]
    public async Task RunPrivateWorker()
    {
        string? secret = Environment.GetEnvironmentVariable("MEMORIES_ERASURE_WORKER_SECRET");
        if (secret is null) return;
        using var deadline = new CancellationTokenSource(TimeSpan.FromMinutes(8));
        CancellationToken token = deadline.Token;
        using var requests = new AnonymousPipeClientStream(PipeDirection.In, Environment.GetEnvironmentVariable("MEMORIES_ERASURE_REQUEST_PIPE")!);
        using var responses = new AnonymousPipeClientStream(PipeDirection.Out, Environment.GetEnvironmentVariable("MEMORIES_ERASURE_RESPONSE_PIPE")!);
        if (Environment.GetEnvironmentVariable("MEMORIES_ERASURE_FAIL_START") == "1") throw new IOException("Controlled private worker start failure.");
        long sequence = 0;
        IConnectionMultiplexer? redis = null;
        IConnectionMultiplexer? graph = null;
        IHost? host = null;
        string? boundary = null;
        try
        {
            while (true)
            {
                MemoryErasureProcessMessage request = await MemoryErasureProcessProtocol.ReadAsync(requests, token);
                if (!MemoryErasureProcessProtocol.Authentic(request, secret) || request.Sequence != sequence + 1)
                {
                    await ReplyAsync("rejected", new { Error = "foreign-or-replayed-request" });
                    continue;
                }
                sequence = request.Sequence;
                try { await ReplyAsync("ok", await ExecuteAsync(request.Operation, request.Payload)); }
                catch (Exception error) { await ReplyAsync("error", new { Error = error.Message, Type = error.GetType().FullName }); }

                Task ReplyAsync(string operation, object payload) => MemoryErasureProcessProtocol.WriteAsync(responses,
                    MemoryErasureProcessProtocol.Create(request.Sequence, operation, JsonSerializer.SerializeToElement(payload), secret), token);
            }
        }
        catch (EndOfStreamException) { }
        finally
        {
            if (host is not null)
            {
                using var cleanup = new CancellationTokenSource(TimeSpan.FromSeconds(10));
                try { await host.StopAsync(cleanup.Token); }
                finally { host.Dispose(); }
            }
            graph?.Dispose(); redis?.Dispose();
        }

        async Task<object> ExecuteAsync(string operation, JsonElement payload)
        {
            if (operation == "identity")
            {
                using Process process = Process.GetCurrentProcess();
                return new { Pid = process.Id, StartTicks = MemoryErasureProcessProtocol.StartIdentity(process.Id),
                    Assemblies = new[] { typeof(MemoryErasureProcessWorker).Assembly, typeof(SourceInventoryStore).Assembly,
                        typeof(IngestionInput).Assembly, typeof(DaprWorkflowClient).Assembly, typeof(IConnectionMultiplexer).Assembly }
                        .Select(assembly => new { Name = assembly.GetName().Name, Identity = assembly.FullName, Path = Path.GetFileName(assembly.Location), Sha256 = Convert.ToHexStringLower(SHA256.HashData(File.ReadAllBytes(assembly.Location))) }) };
            }
            if (operation == "start")
            {
                if (host is not null) throw new InvalidOperationException("Worker already configured.");
                redis = await ConnectionMultiplexer.ConnectAsync(payload.GetProperty("Redis").GetString()!);
                graph = await ConnectionMultiplexer.ConnectAsync(payload.GetProperty("Graph").GetString()!);
                string role = payload.GetProperty("Role").GetString()!;
                host = BuildHost(role, redis, value => boundary = value, token);
                await host.StartAsync(token);
                return new { Started = true };
            }
            if (operation == "boundary") return new { Boundary = boundary };
            if (host is null || redis is null || graph is null) throw new InvalidOperationException("Unconfigured worker.");
            string tenant = payload.GetProperty("Tenant").GetString()!;
            string unit = payload.GetProperty("Unit").GetString()!;
            IDatabase db = redis.GetDatabase();
            var client = host.Services.GetRequiredService<DaprWorkflowClient>();
            if (operation == "observe-absence")
            {
                WorkflowState absent = await client.GetWorkflowStateAsync(unit, true, token);
                absent.Exists.ShouldBeFalse();
                string key = "process-erasure-observation:" + Guid.NewGuid().ToString("N");
                string record = JsonSerializer.Serialize(new { Tenant = tenant, Unit = unit, Case = "case",
                    SourceUri = "https://synthetic.invalid/process-history", Endpoint = Environment.GetEnvironmentVariable("DAPR_GRPC_ENDPOINT"),
                    Pid = Environment.ProcessId, StartTicks = MemoryErasureProcessProtocol.StartIdentity(Environment.ProcessId), Snapshot = JsonSerializer.SerializeToElement(absent) });
                (await db.StringSetAsync(key, record, when: When.NotExists)).ShouldBeTrue();
                return new { Key = key, Sha256 = Digest(record), Pid = Environment.ProcessId,
                    StartTicks = MemoryErasureProcessProtocol.StartIdentity(Environment.ProcessId), Exists = absent.Exists };
            }
            if (operation == "admit-raced")
            {
                string key = payload.GetProperty("ObservationKey").GetString()!;
                key.ShouldStartWith("process-erasure-observation:");
                string record = (await db.StringGetAsync(key)).ToString();
                Digest(record).ShouldBe(payload.GetProperty("ObservationSha256").GetString());
                using JsonDocument observation = JsonDocument.Parse(record);
                JsonElement prior = observation.RootElement;
                prior.GetProperty("Tenant").GetString().ShouldBe(tenant);
                prior.GetProperty("Unit").GetString().ShouldBe(unit);
                prior.GetProperty("Case").GetString().ShouldBe("case");
                prior.GetProperty("Endpoint").GetString().ShouldBe(Environment.GetEnvironmentVariable("DAPR_GRPC_ENDPOINT"));
                prior.GetProperty("Pid").GetInt32().ShouldBe(payload.GetProperty("ObservationPid").GetInt32());
                prior.GetProperty("StartTicks").GetInt64().ShouldBe(payload.GetProperty("ObservationStartTicks").GetInt64());
                (prior.GetProperty("Pid").GetInt32() == Environment.ProcessId
                    && prior.GetProperty("StartTicks").GetInt64() == MemoryErasureProcessProtocol.StartIdentity(Environment.ProcessId)).ShouldBeFalse();
                // Rehydrate only the exact genuinely observed absent state, retained before the older producer ran.
                prior.GetProperty("Snapshot").GetProperty("Exists").GetBoolean().ShouldBeFalse();
                var absent = (WorkflowState)Activator.CreateInstance(typeof(WorkflowState), BindingFlags.Instance | BindingFlags.NonPublic,
                    null, new object?[] { null }, null)!;
                JsonElement.DeepEquals(JsonSerializer.SerializeToElement(absent), prior.GetProperty("Snapshot")).ShouldBeTrue();
                WorkflowState historical = await client.GetWorkflowStateAsync(unit, true, token);
                historical.RuntimeStatus.ShouldBe(WorkflowRuntimeStatus.Completed);
                historical.WorkflowName.ShouldBe(nameof(IngestionWorkflow));
                IngestionInput historicalInput = historical.ReadInputAs<IngestionInput>()!;
                historicalInput.TenantId.ShouldBe(tenant);
                historicalInput.CaseId.ShouldBe("case");
                historicalInput.SourceUri.ShouldBe(prior.GetProperty("SourceUri").GetString());
                historicalInput.SourceInventoryTracked.ShouldBeFalse();
                bool annotation = payload.GetProperty("Annotation").GetBoolean();
                var input = historicalInput with { SourceType = annotation ? SourceType.Annotation : SourceType.Url };
                var inventory = host.Services.GetRequiredService<SourceInventoryStore>();
                var racing = Substitute.For<IDaprWorkflowClient>();
                int observationReads = 0;
                int schedules = 0;
                racing.GetWorkflowStateAsync(unit, true, Arg.Any<CancellationToken>()).Returns(_ => { observationReads++; return absent; });
                racing.ScheduleNewWorkflowAsync(Arg.Any<string>(), unit, Arg.Any<IngestionInput>(), null, Arg.Any<CancellationToken>())
                    .Returns(call => { schedules++; return client.ScheduleNewWorkflowAsync(call.ArgAt<string>(0), unit, call.Arg<IngestionInput>(), null, token); });
                string scheduling = "accepted";
                try
                {
                    if (annotation)
                    {
                        IngestionInput admitted = await new AdmitSourceInventoryActivity(inventory, racing).RunAsync(null!, new(unit, input));
                        admitted.SourceInventoryTracked.ShouldBeTrue();
                        (await racing.ScheduleNewWorkflowAsync(nameof(IngestionWorkflow), unit, admitted, null, token)).ShouldBe(unit);
                    }
                    else
                    {
                        var scheduler = new DaprIngestionWorkflowScheduler(racing, Substitute.For<IWorkflowPayloadStore>(),
                            new IngestionWorkflowConfigurationCapture(Options.Create(new IngestionSettings()), Options.Create(new NaturalLanguageDescriptionOptions())),
                            new WorkflowTraceContextCapture(), Substitute.For<IIngestionWorkflowInFlightRegistry>(), TimeProvider.System,
                            NullLogger<DaprIngestionWorkflowScheduler>.Instance, inventory);
                        (await scheduler.ScheduleAsync(unit, input, token)).ShouldBe(unit);
                    }
                }
                catch (RpcException error) when (schedules == 1 && error.StatusCode == StatusCode.AlreadyExists)
                { scheduling = "duplicate-address-refused"; }
                schedules.ShouldBe(1);
                observationReads.ShouldBe(1);
                WorkflowState terminal = scheduling == "accepted" ? await client.WaitForWorkflowCompletionAsync(unit, true, token) : historical;
                terminal.RuntimeStatus.ShouldBe(scheduling == "accepted" ? WorkflowRuntimeStatus.Failed : WorkflowRuntimeStatus.Completed);
                if (scheduling == "accepted") JsonSerializer.Serialize(terminal).ShouldContain("source-publication-existing-candidate");
                var refused = await Should.ThrowAsync<RedisServerException>(() => inventory.RecordCandidateAsync(tenant, input.SourceUri, unit, unit, token));
                refused.Message.ShouldContain("source-publication-existing-candidate");
                SourceInventoryEntry admittedEntry = (await inventory.ReadAsync(tenant, input.SourceUri, null, 100, token)).Entries.ShouldHaveSingleItem();
                admittedEntry.WorkflowInstanceId.ShouldBe(unit);
                admittedEntry.CaseId.ShouldBe("case");
                string admission = (await db.StringGetAsync(SourceInventoryStore.BuildKey(tenant, input.SourceUri))).ToString();
                return new { Pid = Environment.ProcessId, StartTicks = MemoryErasureProcessProtocol.StartIdentity(Environment.ProcessId),
                    ObservationSha256 = Digest(record), ObservationReads = observationReads, SchedulingAttempts = schedules,
                    SchedulingDisposition = scheduling, TerminalStatus = terminal.RuntimeStatus.ToString(), Admission = admittedEntry, AdmissionSha256 = Digest(admission), CandidateRefused = true };
            }
            if (operation == "late-retry-activity")
            {
                NaturalLanguageEmbeddingRetryInput input = payload.GetProperty("Input").Deserialize<NaturalLanguageEmbeddingRetryInput>()!;
                input.TenantId.ShouldBe(tenant);
                input.MemoryUnitId.ShouldBe(unit);
                input.RawJsonPayload.ShouldBeEmpty();
                input.PayloadVersion.ShouldNotBeNullOrWhiteSpace();
                NaturalLanguageEmbeddingRetryResult result = await new RetryNaturalLanguageEmbeddingActivity(redis, host.Services).RunAsync(null!, input);
                return new { result.Indexed, result.Reason, input.PayloadVersion, Pid = Environment.ProcessId,
                    StartTicks = MemoryErasureProcessProtocol.StartIdentity(Environment.ProcessId) };
            }
            if (operation == "migrate")
                await RedisNaturalLanguageNamespaceMigrator.MigrateOneAsync(db, tenant, unit, token);
            else if (operation == "delete")
            {
                if (payload.GetProperty("Reconcile").GetBoolean()) await OwnedSemanticInventory.ReconcileAsync(db, tenant, unit);
                var options = Options.Create(new NaturalLanguageDescriptionOptions { LegacyRetrySchedulersDrainedForErasure = true });
                if (payload.GetProperty("CaseDeletion").GetBoolean())
                    await new DeleteCaseProjectionActivity(redis, graph, new GraphQueryBuilder(), workflowClient: client, retryOptions: options)
                        .RunAsync(null!, new(tenant, "case", [unit]));
                else
                    await new DeleteMemoryUnitProjectionActivity(redis, graph, new GraphQueryBuilder(), workflowClient: client, retryOptions: options)
                        .RunAsync(null!, new(tenant, "case", unit, []));
            }
            else if (operation == "historical")
            {
                var input = new IngestionInput { TenantId = tenant, CaseId = "case", SourceUri = "https://synthetic.invalid/process-history",
                    SourceType = SourceType.Url, ContentType = "text/plain", IngestedBy = "owned-process-control" };
                await client.ScheduleNewWorkflowAsync(nameof(IngestionWorkflow), unit, input, null, token);
                var state = await client.WaitForWorkflowCompletionAsync(unit, true, token);
                if (state.RuntimeStatus != WorkflowRuntimeStatus.Completed) throw new InvalidOperationException("Historical producer did not complete.");
            }
            else if (operation == "retry")
            {
                var registry = new FailedNaturalLanguageEmbeddingRegistry(redis, NullLogger<FailedNaturalLanguageEmbeddingRegistry>.Instance);
                var owned = Substitute.For<IFailedNaturalLanguageEmbeddingRegistry>();
                owned.DequeueBatchAsync(tenant, Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns(async call =>
                    (IReadOnlyList<FailedNaturalLanguageEmbeddingRecord>)(await registry.DequeueBatchAsync(tenant, call.Arg<int>(), token))
                        .Where(record => record.MemoryUnitId == unit).ToArray());
                owned.ListTenantsWithBacklogAsync(Arg.Any<CancellationToken>()).Returns(_ => OwnedTenantAsync(tenant));
                owned.GetBacklogCountAsync(tenant, Arg.Any<CancellationToken>()).Returns(_ => registry.GetBacklogCountAsync(tenant, token));
                owned.CompleteAsync(Arg.Any<FailedNaturalLanguageEmbeddingRecord>(), Arg.Any<CancellationToken>()).Returns(call => registry.CompleteAsync(call.Arg<FailedNaturalLanguageEmbeddingRecord>(), token));
                owned.IncrementAttemptsAsync(Arg.Any<FailedNaturalLanguageEmbeddingRecord>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
                    .Returns(call => registry.IncrementAttemptsAsync(call.Arg<FailedNaturalLanguageEmbeddingRecord>(), call.Arg<int>(), token));
                var settings = new NaturalLanguageDescriptionOptions { RetryWorkerEnabled = true };
                var monitor = Substitute.For<IOptionsMonitor<NaturalLanguageDescriptionOptions>>(); monitor.CurrentValue.Returns(settings);
                await new NaturalLanguageEmbeddingRetryHostedService(owned, client, monitor,
                    NullLogger<NaturalLanguageEmbeddingRetryHostedService>.Instance, redis).TickAsync(settings, token);
            }
            else throw new InvalidOperationException("Unknown private worker operation.");
            return new { Completed = true };
        }
    }

    private static IHost BuildHost(string role, IConnectionMultiplexer redis, Action<string> checkpoint, CancellationToken token)
        => Host.CreateDefaultBuilder().ConfigureServices(services =>
        {
            services.AddKeyedSingleton("redis", redis);
            var inventory = new SourceInventoryStore(redis, new QueryCursorCodec(new EphemeralDataProtectionProvider(), "ProcessErasure"));
            services.AddSingleton(inventory);
            if (role == "modern") AddRetryServices(services);
            services.AddDaprWorkflow(options =>
            {
                if (role == "historical")
                {
                    options.RegisterWorkflow<HistoricalProcessArtifactWorkflow>(nameof(IngestionWorkflow));
                    options.RegisterActivity<IndexSemanticChunksActivity>();
                    options.RegisterActivity<IndexNaturalLanguageSemanticActivity>();
                }
                else if (role.StartsWith("legacy-", StringComparison.Ordinal))
                {
                    options.RegisterWorkflow<HistoricalNaturalLanguageEmbeddingRetryWorkflow>(nameof(NaturalLanguageEmbeddingRetryWorkflow));
                    options.RegisterActivity<CheckMemoryUnitExistsActivity>();
                    options.RegisterActivity<NaturalLanguageDescriptionInput, NaturalLanguageDescriptionResult>(nameof(GenerateNaturalLanguageDescriptionActivity), async (_, _) =>
                    {
                        if (role == "legacy-description") { checkpoint("description"); await Task.Delay(Timeout.InfiniteTimeSpan, token); }
                        return new("Historical process description.", null, ConfidenceSource.Constant, "test", "test");
                    });
                    options.RegisterActivity<EmbeddingInput, EmbeddingResult>(nameof(GenerateEmbeddingActivity), async (_, _) =>
                    {
                        checkpoint("embedding"); await Task.Delay(Timeout.InfiniteTimeSpan, token);
                        return new([1f, 0f, 0f], "test", 3) { Model = "test" };
                    });
                }
                else
                {
                    options.RegisterWorkflow<CandidateOnlyQualificationWorkflow>(nameof(IngestionWorkflow));
                    options.RegisterActivity<RecordSourceInventoryCandidateActivity>();
                    options.RegisterWorkflow<NaturalLanguageEmbeddingRetryWorkflow>();
                    options.RegisterActivity<RetryNaturalLanguageEmbeddingActivity>();
                    options.RegisterActivity<NaturalLanguageDescriptionInput, NaturalLanguageDescriptionResult>(nameof(GenerateNaturalLanguageDescriptionActivity), (_, _) =>
                        throw new InvalidOperationException("legacy-description-must-not-reexecute"));
                    options.RegisterActivity<EmbeddingInput, EmbeddingResult>(nameof(GenerateEmbeddingActivity), (_, _) =>
                        throw new InvalidOperationException("legacy-embedding-must-not-reexecute"));
                }
            });
        }).Build();

    private static void AddRetryServices(IServiceCollection services)
    {
        var config = EmbeddingProviderDefaults.Google();
        var conversation = Substitute.For<DaprConversationClient>(null!, new HttpClient(), null!);
        conversation.ConverseAsync(Arg.Any<IReadOnlyList<ConversationInput>>(), Arg.Any<ConversationOptions>(), Arg.Any<CancellationToken>())
            .Returns(new ConversationResponse([new ConversationResponseResult([new ConversationResultChoice(null, 0, new ResultMessage("Modern process retry succeeded."))])], string.Empty));
        var environment = Substitute.For<IHostEnvironment>(); environment.EnvironmentName.Returns("Development");
        var embedding = Substitute.For<EmbeddingClient>(Substitute.For<IHttpClientFactory>(), Substitute.For<Dapr.Client.DaprClient>(), new ConfigurationBuilder().Build(), environment);
        embedding.PrimeApiKeyAsync(Arg.Any<string>(), Arg.Any<TenantEmbeddingConfig>(), Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);
        float[] vector = new float[config.Dimensions]; vector[0] = 1;
        embedding.GenerateAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<TenantEmbeddingConfig>(), Arg.Any<CancellationToken>()).Returns(vector);
        var actors = Substitute.For<IActorProxyFactory>(); var limiter = Substitute.For<IEmbeddingRateLimiterActor>();
        limiter.TryConsumeWithCeilingAsync(Arg.Any<int>()).Returns(true);
        actors.CreateActorProxy<IEmbeddingRateLimiterActor>(Arg.Any<ActorId>(), Arg.Any<string>()).Returns(limiter);
        var provider = Substitute.For<ITenantEmbeddingConfigProvider>(); provider.GetAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(config);
        services.AddSingleton(conversation).AddSingleton(Options.Create(new NaturalLanguageDescriptionOptions()))
            .AddSingleton(embedding).AddSingleton(actors).AddSingleton(provider).AddSingleton(Substitute.For<IJitterSource>());
    }

    private static string Digest(string value) => Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(value)));

    private static async IAsyncEnumerable<string> OwnedTenantAsync(string tenant)
    {
        yield return tenant;
        await Task.CompletedTask;
    }
}
