// <copyright file="MemoriesServerServiceCollectionExtensions.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Hosting;

using System.Threading.RateLimiting;

using Dapr.Actors;
using Dapr.AI.Conversation.Extensions;
using Dapr.Client;
using Dapr.Workflow;

using Hexalith.EventStore.Client.Registration;
using Hexalith.Memories.Contracts.V1;
using Hexalith.Memories.Server.Activities.Cases;
using Hexalith.Memories.Server.Activities.Indexing;
using Hexalith.Memories.Server.Activities.Ingestion;
using Hexalith.Memories.Server.Activities.Restore;
using Hexalith.Memories.Server.Activities.Tenants;
using Hexalith.Memories.Server.Import;
using Hexalith.Memories.Server.Actors;
using Hexalith.Memories.Server.Authentication;
using Hexalith.Memories.Server.Cases;
using Hexalith.Memories.Server.Consistency;
using Hexalith.Memories.Server.Diagnostics;
using Hexalith.Memories.Server.Endpoints;
using Hexalith.Memories.Server.EventStoreIntegration;
using Hexalith.Memories.Server.Graph;
using Hexalith.Memories.Server.HealthChecks;
using Hexalith.Memories.Server.Infrastructure;
using Hexalith.Memories.Server.Ingestion;
using Hexalith.Memories.Server.NaturalLanguage;
using Hexalith.Memories.Server.RateLimiting;
using Hexalith.Memories.Server.Search;
using Hexalith.Memories.Server.Telemetry;
using Hexalith.Memories.Server.Tenants;
using Hexalith.Memories.Server.Workflows;

using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

using StackExchange.Redis;

/// <summary>Registers the Memories Server composition-root services.</summary>
internal static class MemoriesServerServiceCollectionExtensions
{
    /// <summary>Registers the services, workflows, actors, health checks, JSON options, and EventStore integration.</summary>
    /// <param name="builder">The web application builder.</param>
    /// <returns>The web application builder.</returns>
    public static WebApplicationBuilder AddMemoriesServerServices(this WebApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.Services.AddDaprClient();
        builder.Services.AddExceptionHandler<MemoriesServerExceptionHandler>();
        builder.Services.AddProblemDetails();
        builder.Services.TryAddTransient<TenantIdValidationEndpointFilter>();
        builder.Services.AddOptions<MemoriesServerAuthenticationOptions>()
            .BindConfiguration("Authentication:JwtBearer")
            .ValidateOnStart();
        builder.Services.AddSingleton<IValidateOptions<MemoriesServerAuthenticationOptions>, ValidateServerAuthenticationOptions>();
        builder.Services.AddSingleton<IConfigureOptions<JwtBearerOptions>, ConfigureServerJwtBearerOptions>();
        builder.Services.AddTransient<Microsoft.AspNetCore.Authentication.IClaimsTransformation, ServerTenantClaimsTransformation>();
        builder.Services.AddHttpContextAccessor();
        builder.Services.AddScoped<IAuthorizedTenantAccessor, AuthorizedTenantAccessor>();
        // Story 21.2: authoritative case/memory-unit/tenant mutations are accepted by the EventStore
        // gateway (Hexalith.EventStore.Client SDK) before projection fan-out. The fast Aspire integration
        // fixture does not compose the EventStore gateway service, so it opts into the in-memory command
        // store explicitly with Memories:Testing:UseInMemoryCommandStore=true.
        if (builder.Configuration.GetValue("Memories:Testing:UseInMemoryCommandStore", false))
        {
            builder.Services.AddSingleton<IMemoriesCommandStore, InMemoryMemoriesCommandStore>();
        }
        else
        {
            // The base address defaults to Dapr sidecar service invocation for the "eventstore" app so
            // deployments only need configuration when the gateway is reached directly.
            builder.Services.AddEventStoreGatewayClient(options =>
            {
                string? configuredBaseAddress = builder.Configuration["EventStoreIntegration:CommandGateway:BaseAddress"];
                string daprHttpPort = Environment.GetEnvironmentVariable("DAPR_HTTP_PORT") ?? "3500";
                options.BaseAddress = string.IsNullOrWhiteSpace(configuredBaseAddress)
                    ? new Uri($"http://localhost:{daprHttpPort}/v1.0/invoke/eventstore/method/")
                    : new Uri(configuredBaseAddress, UriKind.Absolute);
            });
            builder.Services.AddSingleton<IMemoriesCommandStore, EventStoreMemoriesCommandStore>();
        }
        builder.Services.AddScoped<ICaseProjectionWorkflowScheduler, DaprCaseProjectionWorkflowScheduler>();
        builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer();
        builder.Services.AddAuthorizationBuilder()
            .SetFallbackPolicy(new AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .Build());
        builder.Services.Configure<InboundRateLimitOptions>(
            builder.Configuration.GetSection(InboundRateLimitOptions.SectionName));
        InboundRateLimitOptions inboundRateLimitOptions = builder.Configuration
            .GetSection(InboundRateLimitOptions.SectionName)
            .Get<InboundRateLimitOptions>() ?? new InboundRateLimitOptions();
        var inboundRequestRateLimiter = new InboundRequestRateLimiter(inboundRateLimitOptions);
        builder.Services.AddSingleton(inboundRequestRateLimiter);
        builder.Services.AddRateLimiter(options =>
        {
            options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
                InboundRateLimitPartitionFactory.CreatePartition(httpContext, inboundRequestRateLimiter));
            options.OnRejected = InboundRateLimitPartitionFactory.OnRejectedAsync;
        });

        // Story 9.2 Task 1: DAPR Conversation API registration — backs GenerateNaturalLanguageDescriptionActivity.
        // The component name (default "llm") is resolved at activity-call time from NaturalLanguageDescriptionOptions.
        // AddDaprConversationClient registers the DaprConversationClient; the activity injects it directly.
        builder.Services.AddDaprConversationClient();
        builder.Services.Configure<NaturalLanguageDescriptionOptions>(
            builder.Configuration.GetSection("NaturalLanguage"));
        builder.Services.Configure<CaseActivityOptions>(
            builder.Configuration.GetSection(CaseActivityOptions.SectionName));

        // Options validator (Task 1.7): Production guard against conversation.echo (9161) + cross-tenant cache
        // acknowledgment gate (9164). The YAML reader discovers responseCacheTTL from deploy/dapr/components/*.yaml.
        builder.Services.AddSingleton<IComponentYamlReader>(_ =>
        {
            // Published containers carry the canonical component below the content root. Repository runs
            // fall back to the root deploy directory so the exact same material is validated in both modes.
            string publishedCandidate = Path.Combine(
                builder.Environment.ContentRootPath,
                "deploy",
                "dapr",
                "components");
            string repositoryCandidate = Path.Combine(
                builder.Environment.ContentRootPath,
                "..",
                "..",
                "deploy",
                "dapr",
                "components");
            string resolved = Directory.Exists(publishedCandidate)
                ? Path.GetFullPath(publishedCandidate)
                : Path.GetFullPath(repositoryCandidate);
            return new FileSystemComponentYamlReader(resolved);
        });
        builder.Services.AddSingleton<IValidateOptions<NaturalLanguageDescriptionOptions>,
            NaturalLanguageDescriptionOptionsValidator>();

        // Story 9.2 Task 8.2 / Story 24.5: Redis-backed NL retry registry. The live/dead sorted sets use
        // stable memory-unit members, companion hashes hold payloads, and a tenant backlog set avoids key scans.
        builder.Services.AddSingleton<FailedNaturalLanguageEmbeddingRegistry>();
        builder.Services.AddSingleton<IFailedNaturalLanguageEmbeddingRegistry>(sp =>
            sp.GetRequiredService<FailedNaturalLanguageEmbeddingRegistry>());

        // Story 9.2 Task 8.5: background retry service that drains the NL retry queue.
        builder.Services.AddHostedService<NaturalLanguageEmbeddingRetryHostedService>();

        TimeSpan healthCheckTimeout = TimeSpan.FromSeconds(3);
        _ = builder.Services.AddHealthChecks()
            .AddCheck<DaprSidecarHealthCheck>(
                "dapr-sidecar",
                failureStatus: HealthStatus.Unhealthy,
                tags: ["live", "ready"],
                timeout: healthCheckTimeout)
            .Add(new HealthCheckRegistration(
                "dapr-statestore",
                sp => new DaprStateStoreHealthCheck(
                    sp.GetRequiredService<DaprClient>(),
                    "statestore"),
                failureStatus: HealthStatus.Unhealthy,
                tags: ["ready"],
                timeout: healthCheckTimeout))
            .AddCheck<RediSearchHealthCheck>(
                "redisearch",
                failureStatus: HealthStatus.Degraded,
                tags: ["ready"],
                timeout: healthCheckTimeout)
            .AddCheck<RedisVectorHealthCheck>(
                "redis-vector",
                failureStatus: HealthStatus.Degraded,
                tags: ["ready"],
                timeout: healthCheckTimeout)
            .AddCheck<FalkorDbHealthCheck>(
                "falkordb",
                failureStatus: HealthStatus.Degraded,
                tags: ["ready"],
                timeout: healthCheckTimeout);

        builder.Services.AddSingleton<IContentExtractionClient, ContentExtractionClient>();
        builder.Services.AddHttpClient("EmbeddingClient", client =>
            {
                client.Timeout = TimeSpan.FromSeconds(30);
            })
            // Provider responses are interpreted by EmbeddingClient and fed into the durable
            // workflow/tenant rate-limiter retry path. An outer HTTP resilience handler would
            // consume 429/5xx responses before that state machine can observe and persist them.
            .RemoveAllResilienceHandlers();
        builder.Services.AddSingleton<EmbeddingClient>();
        builder.Services.TryAddSingleton(TimeProvider.System);
        builder.Services.Configure<TenantEmbeddingConfigCacheOptions>(
            builder.Configuration.GetSection(TenantEmbeddingConfigCacheOptions.SectionName));
        builder.Services.Configure<TenantReadCacheOptions>(
            builder.Configuration.GetSection(TenantReadCacheOptions.SectionName));
        builder.Services.AddSingleton<ITenantEmbeddingConfigProvider, TenantEmbeddingConfigProvider>();
        builder.Services.AddHttpClient(OidcTokenProvider.HttpClientName, client =>
        {
            client.Timeout = TimeSpan.FromSeconds(10);
        });
        builder.Services.AddSingleton<OidcTokenProvider>(sp =>
            new OidcTokenProvider(
                sp.GetRequiredService<IHttpClientFactory>(),
                sp.GetRequiredService<TimeProvider>(),
                sp.GetRequiredService<ILogger<OidcTokenProvider>>()));
        builder.Services.AddSingleton<IOidcTokenProvider>(sp => sp.GetRequiredService<OidcTokenProvider>());

        // Story 6.1: URL and directory ingestion — settings, named HttpClient, and services.
        builder.Services.Configure<IngestionSettings>(builder.Configuration.GetSection("Ingestion"));
        builder.Services.Configure<WorkflowPayloadStoreOptions>(
            builder.Configuration.GetSection(WorkflowPayloadStoreOptions.SectionName));
        builder.Services.Configure<ContentChunkingOptions>(
            builder.Configuration.GetSection(ContentChunkingOptions.SectionName));
        builder.Services.Configure<UrlFetcherOptions>(builder.Configuration.GetSection("Ingestion:UrlFetcher"));
        builder.Services.AddHttpClient(UrlContentFetcher.HttpClientName)
            .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
            {
                AllowAutoRedirect = false,
            });
        builder.Services.AddSingleton<IUrlContentFetcher, UrlContentFetcher>();
        builder.Services.AddSingleton<IWorkflowPayloadStore, DaprWorkflowPayloadStore>();
        // Story 23.7 (A34): memoized, process-local tenant index-readiness verification shared across all indexing
        // activity invocations so the ingestion hot path stops issuing per-document FT.CREATE. Singleton so the cache
        // survives across activity instances (but never a process restart).
        builder.Services.AddSingleton<ITenantIndexReadinessVerifier, TenantIndexReadinessVerifier>();
        builder.Services.AddSingleton<DirectoryIngestionService>();

        // Story 6.2: per-tenant rate limiting and concurrency gate.
        builder.Services.AddSingleton<PerTenantConcurrencyGate>();
        builder.Services.AddSingleton<IJitterSource, ThreadSafeRandomJitterSource>();
        builder.Services.AddSingleton<CaseIngestionCounterLogic>();
        builder.Services.AddSingleton<FailedUnitsRegistry>();
        builder.Services.AddSingleton<IFailedUnitsRegistry>(sp => sp.GetRequiredService<FailedUnitsRegistry>());
        builder.Services.AddSingleton<IngestionWorkflowConfigurationCapture>();
        builder.Services.AddSingleton<WorkflowTraceContextCapture>();
        builder.Services.AddSingleton<IIngestionWorkflowInFlightRegistry, RedisIngestionWorkflowInFlightRegistry>();
        builder.Services.AddSingleton<IIngestionWorkflowScheduler, DaprIngestionWorkflowScheduler>();
        builder.Services.AddSingleton<IIngestionWorkflowStateReader, DaprIngestionWorkflowStateReader>();
        builder.Services.AddSingleton<ReIngestionCoordinator>();
        builder.Services.AddSingleton<IngestDedupReservation>();

        // Story 18.5 — exact sourceUri → MemoryUnitId lookup seam over the permanent dedup record (reads the same
        // keyed-redis index SaveDedupKeyActivity/CheckIdempotencyActivity use; no parallel store).
        builder.Services.AddSingleton<SourceUriMemoryUnitLookup>();

        builder.Services.AddKeyedSingleton<IConnectionMultiplexer>("redis", (sp, _) =>
            ConnectRequiredMultiplexer(builder.Configuration, "redis"));
        builder.Services.AddKeyedSingleton<IConnectionMultiplexer>("falkordb", (sp, _) =>
            ConnectRequiredMultiplexer(builder.Configuration, "falkordb"));
        builder.Services.AddSingleton<IGraphQueryBuilder, GraphQueryBuilder>();
        builder.Services.AddSingleton<SyntacticSearchService>(sp =>
            new SyntacticSearchService(
                sp.GetRequiredKeyedService<IConnectionMultiplexer>("redis"),
                sp.GetRequiredService<ILogger<SyntacticSearchService>>()));
        builder.Services.AddSingleton<SemanticSearchService>(sp =>
            new SemanticSearchService(
                sp.GetRequiredKeyedService<IConnectionMultiplexer>("redis"),
                sp.GetRequiredService<EmbeddingClient>(),
                sp.GetRequiredService<ILogger<SemanticSearchService>>()));
        // Story 9.2 Task 4.9 + Story 22.7: NL semantic search service used directly and by hybrid search.
        // (AC #7 staged rollout) — consumers opt in by requesting this type directly.
        builder.Services.AddSingleton<NaturalLanguageSemanticSearchService>(sp =>
            new NaturalLanguageSemanticSearchService(
                sp.GetRequiredKeyedService<IConnectionMultiplexer>("redis"),
                sp.GetRequiredService<EmbeddingClient>(),
                sp.GetRequiredService<ILogger<NaturalLanguageSemanticSearchService>>()));
        builder.Services.AddSingleton<IResultFuser, IdentityResultFuser>();

        // Story 9.2 Task 4.10 (chaos Scenario D): one-shot startup reconciler sweeps orphan NL semantic indexes
        // that a SIGKILL mid-provisioning could have left behind when compensation cannot run.
        builder.Services.AddHostedService<Hexalith.Memories.Server.Hosting.OrphanSemanticIndexReconciler>();

        // Story 9.2 Task 7.6 / Review D3: one-shot isStub backfill migration — sets m.isStub=false on pre-9.2
        // MemoryUnit nodes with content so GraphTraversalService's content-absent fallback becomes redundant.
        // The migration class itself is not a hosted service; the wrapper enumerates tenants at startup and
        // calls RunAsync per graph. Each graph is gated by (:SchemaMigration {id: "9.2-isStub-backfill"}) so
        // repeated startups are idempotent no-ops.
        builder.Services.AddSingleton<Hexalith.Memories.Server.Migrations.IsStubBackfillMigration>();
        builder.Services.AddHostedService<Hexalith.Memories.Server.Hosting.IsStubBackfillMigrationHostedService>();

        // Story 9.2 Task 5.9: startup gate that delays workflow-host startup until in-flight IngestionWorkflow
        // instances drain (Risk #13 replay determinism fail-safe). Uses IHostedLifecycleService (Spike 0.4)
        // so ordering is DI-registration-independent. Same-version integration restart tests disable this
        // gate because there is no second replica available to drain in-flight workflows before startup.
        if (builder.Configuration.GetValue("WorkflowReplaySafety:Enabled", true))
        {
            builder.Services.AddHostedService<Hexalith.Memories.Server.Hosting.WorkflowReplaySafetyHostedService>();
        }
        builder.Services.AddSingleton<GraphScopedSearch>(sp =>
            new GraphScopedSearch(
                sp.GetRequiredKeyedService<IConnectionMultiplexer>("falkordb"),
                sp.GetRequiredKeyedService<IConnectionMultiplexer>("redis"),
                sp.GetRequiredService<IGraphQueryBuilder>(),
                sp.GetRequiredService<ILogger<GraphScopedSearch>>()));
        builder.Services.AddSingleton<GraphTraversalService>(sp =>
            new GraphTraversalService(
                sp.GetRequiredKeyedService<IConnectionMultiplexer>("falkordb"),
                sp.GetRequiredKeyedService<IConnectionMultiplexer>("redis"),
                sp.GetRequiredService<IGraphQueryBuilder>(),
                sp.GetRequiredService<ILogger<GraphTraversalService>>()));
        builder.Services.AddSingleton<HybridSearchService>(sp =>
        {
            var syntactic = sp.GetRequiredService<SyntacticSearchService>();
            var semantic = sp.GetRequiredService<SemanticSearchService>();
            var naturalLanguage = sp.GetRequiredService<NaturalLanguageSemanticSearchService>();
            var graph = sp.GetRequiredService<GraphScopedSearch>();
            return new HybridSearchService(
                query => syntactic.SearchAsync(query),
                (query, config, ct) => semantic.SearchAsync(query, config, ct),
                (query, config, ct) => naturalLanguage.SearchAsync(query, config, ct),
                (query, startNode, depth, ct) => graph.SearchAsync(query, startNode, depth, innerSearch: null, ct),
                sp.GetRequiredService<IResultFuser>(),
                sp.GetRequiredService<ILogger<HybridSearchService>>());
        });

        builder.Services.AddSingleton<CaseActivityService>(sp =>
            new CaseActivityService(
                sp.GetRequiredKeyedService<IConnectionMultiplexer>("redis"),
                sp.GetRequiredService<ILogger<CaseActivityService>>(),
                sp.GetRequiredService<IOptions<CaseActivityOptions>>()));
        builder.Services.AddScoped<CaseService>();
        // Story 8.3: streaming data exporter (case + tenant scope).
        builder.Services.AddScoped<Hexalith.Memories.Server.Export.TenantExportService>();
        builder.Services.AddSingleton<TenantRegistryService>();
        builder.Services.AddSingleton<TenantStatusGuard>();
        builder.Services.AddSingleton<TenantMetricsService>();
        builder.Services.AddSingleton<TenantSummaryCache>();
        builder.Services.AddSingleton<RollingCounterStore>();
        builder.Services.AddSingleton<IHostedService>(sp => sp.GetRequiredService<RollingCounterStore>());
        builder.Services.AddSingleton<TelemetrySummaryService>();
        builder.Services.AddSingleton<TelemetrySnapshotCache>();

        // Story 9.3 — handler registry + mismatch detector (scoped, mirror TelemetrySummaryService lifetime).
        builder.Services.AddSingleton<Hexalith.Memories.Server.Handlers.ProcessLifetimeClock>();
        builder.Services.AddScoped<Hexalith.Memories.Server.Handlers.HandlerRegistryService>();
        builder.Services.AddScoped<Hexalith.Memories.Server.Handlers.HandlerMismatchDetector>();
        builder.Services.AddSingleton<TenantIsolationVerifier>(sp =>
            new TenantIsolationVerifier(
                sp.GetRequiredService<TenantRegistryService>(),
                sp.GetRequiredKeyedService<IConnectionMultiplexer>("redis"),
                sp.GetRequiredKeyedService<IConnectionMultiplexer>("falkordb"),
                sp.GetRequiredService<ILogger<TenantIsolationVerifier>>()));

        builder.Services.AddDaprWorkflow(options =>
        {
            options.RegisterActivity<ExtractContentActivity>();
            options.RegisterActivity<GenerateEmbeddingActivity>();
            options.RegisterActivity<GenerateChunkEmbeddingsActivity>();
            options.RegisterActivity<IndexSyntacticActivity>();
            options.RegisterActivity<IndexSemanticActivity>();
            options.RegisterActivity<IndexSemanticChunksActivity>();
            options.RegisterActivity<IndexGraphActivity>();

            // Story 9.2 Task 2.6: NL description activity (dual-embedding pipeline, SourceType.Event).
            options.RegisterActivity<GenerateNaturalLanguageDescriptionActivity>();

            // Story 9.2 Task 4.4: NL semantic index activity (writes to {tenant}:memories:vec:nl).
            options.RegisterActivity<IndexNaturalLanguageSemanticActivity>();

            // Story 9.2 Task 5.4: enqueue activity for the degraded path (LLM unavailable → queue for retry).
            options.RegisterActivity<QueueNaturalLanguageEmbeddingRetryActivity>();

            // Story 9.2 Task 8.4a: retry guard activity that prevents recreating orphan NL hashes after delete/rollback.
            options.RegisterActivity<CheckMemoryUnitExistsActivity>();

            // Story 9.2 Task 8.4: retry workflow that re-runs NL description + embedding + index.
            options.RegisterWorkflow<NaturalLanguageEmbeddingRetryWorkflow>();

            options.RegisterWorkflow<IngestionWorkflow>();
            options.RegisterWorkflow<CaseCreationProjectionWorkflow>();
            options.RegisterWorkflow<AnnotationProjectionWorkflow>();
            options.RegisterWorkflow<MemoryUnitDeletionProjectionWorkflow>();
            options.RegisterWorkflow<CaseDeletionProjectionWorkflow>();
            options.RegisterActivity<ValidateContentActivity>();
            options.RegisterActivity<CheckIdempotencyActivity>();
            options.RegisterActivity<SaveDedupKeyActivity>();
            options.RegisterActivity<ReleaseDedupKeyIfOwnedActivity>();
            options.RegisterActivity<VerifyConsistencyActivity>();
            options.RegisterActivity<CleanupSyntacticActivity>();
            options.RegisterActivity<CleanupSemanticActivity>();
            options.RegisterActivity<CleanupGraphActivity>();
            options.RegisterActivity<CleanupWorkflowPayloadsActivity>();
            options.RegisterActivity<RecordCaseActivityActivity>();
            options.RegisterActivity<ProjectCaseHashActivity>();
            options.RegisterActivity<ProjectCaseGraphActivity>();
            options.RegisterActivity<CleanupCaseProjectionActivity>();
            options.RegisterActivity<ProjectAnnotationGraphActivity>();
            options.RegisterActivity<ScheduleAnnotationIngestionActivity>();
            options.RegisterActivity<DeleteMemoryUnitProjectionActivity>();
            options.RegisterActivity<MarkCaseDeletingActivity>();
            options.RegisterActivity<DeleteCaseProjectionActivity>();
            options.RegisterActivity<DeleteCaseRouteMappingsActivity>();

            options.RegisterActivity<FetchUrlActivity>();

            options.RegisterActivity<PersistFailedUnitActivity>();
            options.RegisterActivity<UpdateCaseIngestionCounterActivity>();

            options.RegisterWorkflow<TenantProvisioningWorkflow>();
            options.RegisterActivity<ProvisionRediSearchActivity>();
            options.RegisterActivity<ProvisionRedisVectorActivity>();
            options.RegisterActivity<ProvisionFalkorDbActivity>();
            options.RegisterActivity<VerifyTenantActivity>();
            options.RegisterActivity<DeleteRediSearchIndexActivity>();
            options.RegisterActivity<DeleteRedisVectorIndexActivity>();
            options.RegisterActivity<DeleteFalkorDbGraphActivity>();
            options.RegisterActivity<InitializeTenantRegistryActivity>();
            options.RegisterActivity<UpdateTenantStatusActivity>();
            options.RegisterActivity<RemoveTenantRegistryActivity>();

            options.RegisterWorkflow<TenantDeletionWorkflow>();
            options.RegisterActivity<DeleteRediSearchActivity>();
            options.RegisterActivity<DeleteRedisVectorActivity>();
            options.RegisterActivity<DeleteFalkorDbBatchActivity>();
            options.RegisterActivity<DeleteFalkorDbGraphFinalizerActivity>();
            options.RegisterActivity<DeleteTenantDataKeysActivity>();
            options.RegisterActivity<GetTenantRegistryActivity>();

            // Story 8.2: consistency verification & repair.
            options.RegisterWorkflow<ConsistencyVerificationWorkflow>();
            options.RegisterWorkflow<ConsistencyRepairWorkflow>();
            options.RegisterActivity<EnumerateMemoryUnitIdsActivity>();
            options.RegisterActivity<RepairUnitActivity>();

            // Story 26.2: backup/restore — durable restore orchestration + activities.
            options.RegisterWorkflow<RestoreWorkflow>();
            options.RegisterActivity<RestoreDataPlaneActivity>();
            options.RegisterActivity<RestoreReindexUnitActivity>();
            options.RegisterActivity<DeleteRestoreStagingActivity>();
        });
        builder.Services.TryAddSingleton<IDaprWorkflowClient>(sp => sp.GetRequiredService<DaprWorkflowClient>());

        // Story 26.2: import/restore payload staging store.
        builder.Services.TryAddSingleton<IImportStagingStore, RedisImportStagingStore>();

        // Story 8.2: consistency services.
        builder.Services.AddScoped<IConsistencyInspectionService, ConsistencyInspectionService>();
        builder.Services.AddScoped<IConsistencyWorkflowService, ConsistencyWorkflowService>();
        builder.Services.AddScoped<ISemanticIndexer, SemanticIndexer>();
        builder.Services.AddScoped<IGraphNodeMerger, GraphNodeMerger>();

        builder.Services.AddActors(options =>
        {
            options.Actors.RegisterActor<EmbeddingRateLimiterActor>();
            options.Actors.RegisterActor<TenantConfigurationActor>();
            options.Actors.RegisterActor<CorpusStatisticsActor>();
            options.Actors.RegisterActor<CaseIngestionCounterActor>();
            options.ActorIdleTimeout = TimeSpan.FromMinutes(60);
            options.ActorScanInterval = TimeSpan.FromSeconds(30);
            options.ReentrancyConfig = new Dapr.Actors.ActorReentrancyConfig { Enabled = false };
        });

        builder.Services.ConfigureHttpJsonOptions(options =>
        {
            options.SerializerOptions.PropertyNamingPolicy = MemoriesJsonContext.Options.PropertyNamingPolicy;
            // Story 9.1: combine EventStore package's source-generated types so event subscription responses
            // serialize without falling back to reflection (AOT-safe path).
            options.SerializerOptions.TypeInfoResolver = System.Text.Json.Serialization.Metadata.JsonTypeInfoResolver.Combine(
                MemoriesJsonContext.Options.TypeInfoResolver!,
                Hexalith.Memories.EventStore.EventStoreJsonContext.Default);
        });

        // Story 9.1: EventStore pub/sub subscription surface. Registers the controller (via application-part
        // discovery), the EventStore package, and the Server-side adapters (workflow scheduler, tenant status,
        // case creation, telemetry, preflight dedup). See deploy/dapr/components/pubsub.yaml for the broker
        // component definition and docs/dev/eventstore-integration.md for the operator guide.
        builder.Services.AddServerEventStoreIntegration(builder.Configuration);

        return builder;
    }

    private static IConnectionMultiplexer ConnectRequiredMultiplexer(IConfiguration configuration, string connectionName)
    {
        string connectionString = configuration.GetConnectionString(connectionName)
            ?? throw new InvalidOperationException(
                $"Connection string '{connectionName}' is required. Start the server through AppHost or set ConnectionStrings__{connectionName}.");

        return ConnectionMultiplexer.Connect(connectionString);
    }
}
