using Dapr.Actors;
using Dapr.Actors.Client;
using Dapr.Client;
using Dapr.Workflow;

using System.Text.Json;

using Hexalith.Memories.Contracts.V1;
using Hexalith.Memories.Server.Activities.Indexing;
using Hexalith.Memories.Server.Activities.Ingestion;
using Hexalith.Memories.Server.Activities.Tenants;
using Hexalith.Memories.Server.Actors;
using Hexalith.Memories.Server.Cases;
using Hexalith.Memories.Server.Graph;
using Hexalith.Memories.Server.HealthChecks;
using Hexalith.Memories.Server.Ingestion;
using Hexalith.Memories.Server.Search;
using Hexalith.Memories.Server.Tenants;
using Hexalith.Memories.Server.Workflows;
using Hexalith.Memories.ServiceDefaults;

using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Diagnostics.HealthChecks;

using StackExchange.Redis;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.Services.AddDaprClient();

TimeSpan healthCheckTimeout = TimeSpan.FromSeconds(3);
_ = builder.Services.AddHealthChecks()
    .AddCheck<DaprSidecarHealthCheck>(
        "dapr-sidecar",
        failureStatus: HealthStatus.Unhealthy,
        tags: ["ready"],
        timeout: healthCheckTimeout)
    .Add(new HealthCheckRegistration(
        "dapr-statestore",
        sp => new DaprStateStoreHealthCheck(
            sp.GetRequiredService<DaprClient>(),
            "statestore"),
        failureStatus: HealthStatus.Unhealthy,
        tags: ["ready"],
        timeout: healthCheckTimeout));

builder.Services.AddSingleton<IContentExtractionClient, ContentExtractionClient>();
builder.Services.AddHttpClient("EmbeddingClient", client =>
{
    client.Timeout = TimeSpan.FromSeconds(30);
});
builder.Services.AddSingleton<EmbeddingClient>();

// Story 6.1: URL and directory ingestion — settings, named HttpClient, and services.
builder.Services.Configure<IngestionSettings>(builder.Configuration.GetSection("Ingestion"));
builder.Services.Configure<UrlFetcherOptions>(builder.Configuration.GetSection("Ingestion:UrlFetcher"));
builder.Services.AddHttpClient(UrlContentFetcher.HttpClientName)
    .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
    {
        AllowAutoRedirect = false,
    });
builder.Services.AddSingleton<IUrlContentFetcher, UrlContentFetcher>();
builder.Services.AddSingleton<DirectoryIngestionService>();

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
    var graph = sp.GetRequiredService<GraphScopedSearch>();
    return new HybridSearchService(
        query => syntactic.SearchAsync(query),
        (query, config, ct) => semantic.SearchAsync(query, config, ct),
        (query, startNode, depth, ct) => graph.SearchAsync(query, startNode, depth, innerSearch: null, ct),
        sp.GetRequiredService<IActorProxyFactory>(),
        sp.GetRequiredService<ILogger<HybridSearchService>>());
});

builder.Services.AddSingleton<CaseActivityService>(sp =>
    new CaseActivityService(
        sp.GetRequiredKeyedService<IConnectionMultiplexer>("redis"),
        sp.GetRequiredService<ILogger<CaseActivityService>>()));
builder.Services.AddScoped<CaseService>();
builder.Services.AddSingleton<TenantRegistryService>();
builder.Services.AddSingleton<TenantStatusGuard>();
// Story 5.5: per-tenant operational metrics for GET /api/tenants and GET /configuration.
builder.Services.AddSingleton<TenantMetricsService>();
builder.Services.AddSingleton<TenantIsolationVerifier>(sp =>
    new TenantIsolationVerifier(
        sp.GetRequiredService<TenantRegistryService>(),
        sp.GetRequiredKeyedService<IConnectionMultiplexer>("redis"),
        sp.GetRequiredKeyedService<IConnectionMultiplexer>("falkordb"),
        sp.GetRequiredService<ILogger<TenantIsolationVerifier>>()));

builder.Services.AddDaprWorkflow(options =>
{
    // Existing activities (Stories 1.3-1.5)
    options.RegisterActivity<ExtractContentActivity>();
    options.RegisterActivity<GenerateEmbeddingActivity>();
    options.RegisterActivity<IndexSyntacticActivity>();
    options.RegisterActivity<IndexSemanticActivity>();
    options.RegisterActivity<IndexGraphActivity>();

    // Story 1.6: Ingestion workflow + new activities
    options.RegisterWorkflow<IngestionWorkflow>();
    options.RegisterActivity<ValidateContentActivity>();
    options.RegisterActivity<CheckIdempotencyActivity>();
    options.RegisterActivity<SaveDedupKeyActivity>();
    options.RegisterActivity<VerifyConsistencyActivity>();
    options.RegisterActivity<CleanupSyntacticActivity>();
    options.RegisterActivity<CleanupSemanticActivity>();
    options.RegisterActivity<CleanupGraphActivity>();
    options.RegisterActivity<RecordCaseActivityActivity>();

    // Story 6.1: URL ingestion
    options.RegisterActivity<FetchUrlActivity>();

    // Story 5.1: Tenant provisioning workflow + activities
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

    // Story 5.2: Tenant deletion workflow + activities
    options.RegisterWorkflow<TenantDeletionWorkflow>();
    options.RegisterActivity<DeleteRediSearchActivity>();
    options.RegisterActivity<DeleteRedisVectorActivity>();
    options.RegisterActivity<DeleteFalkorDbBatchActivity>();
    options.RegisterActivity<DeleteFalkorDbGraphFinalizerActivity>();
    options.RegisterActivity<DeleteTenantDataKeysActivity>();
    options.RegisterActivity<GetTenantRegistryActivity>();
});

builder.Services.AddActors(options =>
{
    options.Actors.RegisterActor<EmbeddingRateLimiterActor>();
    options.Actors.RegisterActor<TenantConfigurationActor>();
    options.Actors.RegisterActor<CorpusStatisticsActor>();
    options.ActorIdleTimeout = TimeSpan.FromMinutes(60);
    options.ActorScanInterval = TimeSpan.FromSeconds(30);
    options.ReentrancyConfig = new Dapr.Actors.ActorReentrancyConfig { Enabled = false };
});

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.PropertyNamingPolicy = MemoriesJsonContext.Options.PropertyNamingPolicy;
    options.SerializerOptions.TypeInfoResolver = MemoriesJsonContext.Options.TypeInfoResolver;
});

WebApplication app = builder.Build();

app.MapDefaultEndpoints();
app.MapActorsHandlers();

app.MapPost("/api/ingest", async (DaprWorkflowClient workflowClient, TenantStatusGuard tenantGuard, IngestionInput input) =>
{
    ErrorResponse? validationError = ValidateIngestionRequest(input);
    if (validationError is not null)
    {
        return Results.BadRequest(validationError);
    }

    ErrorResponse? tenantStatusError = await tenantGuard.ValidateTenantActiveAsync(input.TenantId, CancellationToken.None);
    if (tenantStatusError is not null)
    {
        return TenantStatusGuard.ToHttpResult(tenantStatusError);
    }

    string instanceId = await workflowClient.ScheduleNewWorkflowAsync(
        nameof(IngestionWorkflow), input: input);
    return Results.Accepted($"/api/ingest/{instanceId}", new { instanceId });
}).WithMetadata(new RequestSizeLimitAttribute(2 * 1024 * 1024));

app.MapGet("/api/ingest/{instanceId}", async (DaprWorkflowClient workflowClient, string instanceId) =>
{
    WorkflowState? state = await workflowClient.GetWorkflowStateAsync(instanceId);
    return state is null ? Results.NotFound() : Results.Ok(state);
});

// Story 6.1: URL ingestion — single-URL entry point. Returns 202 with workflow instance id.
app.MapPost("/api/ingest/url", async (
    DaprWorkflowClient workflowClient,
    TenantStatusGuard tenantGuard,
    Microsoft.Extensions.Options.IOptions<UrlFetcherOptions> urlFetcherOptions,
    ILoggerFactory loggerFactory,
    UrlIngestionRequest request,
    CancellationToken cancellationToken) =>
{
    ILogger urlLogger = loggerFactory.CreateLogger("Hexalith.Memories.Server.Ingestion.Url");

    ErrorResponse? validationError = ValidateUrlIngestionRequest(request, urlFetcherOptions.Value, out Uri? url);
    if (validationError is not null || url is null)
    {
        IngestionEndpointLog.LogUrlIngestionRejected(
            urlLogger,
            request?.TenantId ?? "(missing)",
            request?.CaseId ?? "(missing)",
            IngestionEndpointLog.RedactUrl(request?.Url),
            validationError!.Code);
        return Results.BadRequest(validationError);
    }

    ErrorResponse? tenantStatusError = await tenantGuard.ValidateTenantActiveAsync(request.TenantId, cancellationToken);
    if (tenantStatusError is not null)
    {
        IngestionEndpointLog.LogUrlIngestionRejected(
            urlLogger,
            request.TenantId,
            request.CaseId,
            IngestionEndpointLog.RedactUrl(request.Url),
            tenantStatusError.Code);
        return TenantStatusGuard.ToHttpResult(tenantStatusError);
    }

    IngestionInput input = new()
    {
        TenantId = request.TenantId,
        CaseId = request.CaseId,
        SourceUri = request.Url,
        ContentBytes = null,
        ContentType = "application/octet-stream",
        SourceType = SourceType.Url,
        IngestedBy = request.IngestedBy,
        Metadata = request.Metadata,
        CausationId = request.CausationId,
        CorrelationId = request.CorrelationId,
    };

    string instanceId = await workflowClient.ScheduleNewWorkflowAsync(
        nameof(IngestionWorkflow),
        input: input);

    IngestionEndpointLog.LogUrlIngestionScheduled(
        urlLogger,
        request.TenantId,
        request.CaseId,
        instanceId,
        IngestionEndpointLog.RedactUrl(url));

    return Results.Accepted(
        $"/api/ingest/{instanceId}",
        new UrlIngestionResponse(instanceId, request.Url));
});

// Story 6.1: Directory ingestion — batch scheduler over an allow-listed server filesystem root.
app.MapPost("/api/ingest/directory", async (
    DirectoryIngestionService directoryService,
    TenantStatusGuard tenantGuard,
    ILoggerFactory loggerFactory,
    DirectoryIngestionRequest request,
    CancellationToken cancellationToken) =>
{
    ILogger dirLogger = loggerFactory.CreateLogger("Hexalith.Memories.Server.Ingestion.Directory");

    ErrorResponse? shapeError = ValidateDirectoryIngestionRequest(request);
    if (shapeError is not null)
    {
        IngestionEndpointLog.LogDirectoryBatchRejected(
            dirLogger,
            request?.TenantId ?? "(missing)",
            request?.CaseId ?? "(missing)",
            null,
            shapeError.Code,
            request?.DirectoryPath ?? string.Empty);
        return Results.BadRequest(shapeError);
    }

    ErrorResponse? tenantStatusError = await tenantGuard.ValidateTenantActiveAsync(request.TenantId, cancellationToken);
    if (tenantStatusError is not null)
    {
        IngestionEndpointLog.LogDirectoryBatchRejected(
            dirLogger,
            request.TenantId,
            request.CaseId,
            null,
            tenantStatusError.Code,
            request.DirectoryPath);
        return TenantStatusGuard.ToHttpResult(tenantStatusError);
    }

    DirectoryIngestionResult result = await directoryService.IngestAsync(request, cancellationToken);
    if (result.ErrorCode is not null)
    {
        IngestionEndpointLog.LogDirectoryBatchRejected(
            dirLogger,
            request.TenantId,
            request.CaseId,
            result.BatchId,
            result.ErrorCode,
            request.DirectoryPath);

        return result.ErrorCode switch
        {
            "DIRECTORY_INGESTION_DISABLED" => Results.Json(
                new ErrorResponse(
                    "DIRECTORY_INGESTION_DISABLED",
                    "Directory ingestion is not enabled on this server.",
                    "Configure Ingestion:AllowedDirectoryRoots to enable."),
                statusCode: StatusCodes.Status403Forbidden),
            "BATCH_TOO_LARGE" => Results.Json(
                new ErrorResponse(
                    "BATCH_TOO_LARGE",
                    "Batch exceeds the maximum supported number of files.",
                    "Ingest smaller sub-directories, or call POST /api/ingest per file."),
                statusCode: StatusCodes.Status400BadRequest),
            "BATCH_TRACKING_UNAVAILABLE" => Results.Json(
                new ErrorResponse(
                    "BATCH_TRACKING_UNAVAILABLE",
                    "Directory batch tracking is temporarily unavailable.",
                    "Retry when the DAPR state store is healthy; no successful batch response was returned."),
                statusCode: StatusCodes.Status503ServiceUnavailable),
            "DAPR_UNAVAILABLE" => Results.Json(
                new ErrorResponse(
                    "DAPR_UNAVAILABLE",
                    "DAPR sidecar is not ready.",
                    "Check service health via /healthz and retry."),
                statusCode: StatusCodes.Status503ServiceUnavailable),
            "BATCH_SCHEDULING_FAILED" => Results.Json(
                new ErrorResponse(
                    "BATCH_SCHEDULING_FAILED",
                    "Directory batch scheduling failed before the batch could be safely accepted.",
                    "Inspect server logs and retry the request."),
                statusCode: StatusCodes.Status500InternalServerError),
            _ => Results.Json(
                new ErrorResponse(
                    "INVALID_DIRECTORY_PATH",
                    "Directory path is not allowed.",
                    "Provide an absolute path under a configured Ingestion:AllowedDirectoryRoots entry."),
                statusCode: StatusCodes.Status400BadRequest),
        };
    }

    DirectoryIngestionOutcome outcome = result.Outcome!;
    IngestionEndpointLog.LogDirectoryBatchScheduled(
        dirLogger,
        request.TenantId,
        request.CaseId,
        outcome.BatchId,
        outcome.Discovered,
        outcome.Enqueued,
        outcome.Skipped.Count);

    return Results.Accepted(
        $"/api/ingest/batches/{outcome.BatchId}",
        outcome);
});

// Story 6.1: Aggregated batch status — queries persisted batch state and polls each workflow instance.
app.MapGet("/api/ingest/batches/{batchId}", async (
    DaprClient daprClient,
    DaprWorkflowClient workflowClient,
    string batchId,
    CancellationToken cancellationToken) =>
{
    if (string.IsNullOrWhiteSpace(batchId))
    {
        return Results.NotFound();
    }

    DirectoryBatchState? state;
    try
    {
        state = await daprClient.GetStateAsync<DirectoryBatchState>(
            DirectoryIngestionService.StateStoreName,
            DirectoryIngestionService.BatchStateKeyPrefix + batchId,
            cancellationToken: cancellationToken);
    }
    catch (Exception)
    {
        state = null;
    }

    if (state is null)
    {
        return Results.NotFound(new ErrorResponse(
            "BATCH_NOT_FOUND",
            $"Batch '{batchId}' was not found or has expired.",
            "Verify the batchId returned by POST /api/ingest/directory."));
    }

    using SemaphoreSlim gate = new(50);
    Task<BatchInstanceStatus>[] statusTasks = state.Files.Select(async file =>
    {
        await gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            WorkflowState? wfState = await workflowClient
                .GetWorkflowStateAsync(file.InstanceId)
                .ConfigureAwait(false);
            return DirectoryBatchStatusMapper.MapInstance(file, wfState);
        }
        catch (Exception)
        {
            return DirectoryBatchStatusMapper.MapInstance(file, null);
        }
        finally
        {
            gate.Release();
        }
    }).ToArray();

    BatchInstanceStatus[] instances = await Task.WhenAll(statusTasks);
    BatchStatusCounts counts = DirectoryBatchStatusMapper.BuildCounts(instances);

    BatchStatusResponse response = new(
        state.BatchId,
        state.TenantId,
        state.CaseId,
        Discovered: state.Discovered,
        Enqueued: state.Files.Length,
        Skipped: state.Skipped.Length,
        Counts: counts,
        Instances: instances);

    return Results.Ok(response);
});

app.MapGet("/api/tenants/{tenantId}/embedding-config", async (
    IActorProxyFactory actorProxyFactory,
    TenantStatusGuard tenantGuard,
    string tenantId,
    CancellationToken cancellationToken) =>
{
    ErrorResponse? tenantValidationError = ValidateTenantId(tenantId);
    if (tenantValidationError is not null)
    {
        return Results.BadRequest(tenantValidationError);
    }

    ErrorResponse? tenantStatusError = await tenantGuard.ValidateTenantActiveAsync(tenantId, cancellationToken);
    if (tenantStatusError is not null)
    {
        return TenantStatusGuard.ToHttpResult(tenantStatusError);
    }

    ITenantConfigurationActor actor = actorProxyFactory
        .CreateActorProxy<ITenantConfigurationActor>(new ActorId(tenantId), nameof(TenantConfigurationActor));
    TenantEmbeddingConfig config = await actor.GetEmbeddingConfigAsync();
    return Results.Ok(config);
});

app.MapPut("/api/tenants/{tenantId}/embedding-config",
    async (
        IActorProxyFactory actorProxyFactory,
        TenantStatusGuard tenantGuard,
        string tenantId,
        TenantEmbeddingConfig config,
        CancellationToken cancellationToken,
        bool forceReindex = false) =>
{
    ErrorResponse? tenantValidationError = ValidateTenantId(tenantId);
    if (tenantValidationError is not null)
    {
        return Results.BadRequest(tenantValidationError);
    }

    ErrorResponse? tenantStatusError = await tenantGuard.ValidateTenantActiveAsync(tenantId, cancellationToken);
    if (tenantStatusError is not null)
    {
        return TenantStatusGuard.ToHttpResult(tenantStatusError);
    }

    try
    {
        EmbeddingProviderDefaults.Validate(config);
    }
    catch (ArgumentException ex)
    {
        return Results.BadRequest(new ErrorResponse("INVALID_CONFIG", ex.Message, "Fix the configuration values and retry."));
    }

    ITenantConfigurationActor actor = actorProxyFactory
        .CreateActorProxy<ITenantConfigurationActor>(new ActorId(tenantId), nameof(TenantConfigurationActor));

    try
    {
        await actor.SetEmbeddingConfigAsync(config, forceReindex);
        TenantEmbeddingConfig updatedConfig = await actor.GetEmbeddingConfigAsync();
        return Results.Ok(updatedConfig);
    }
    catch (EmbeddingConfigChangeException ex)
    {
        return Results.Conflict(CreateEmbeddingConfigConflictResponse(
            ex.TenantId,
            ex.CurrentConfig ?? EmbeddingProviderDefaults.Google(),
            ex.ProposedConfig ?? config,
            ex.AffectedFields));
    }
    catch (ActorMethodInvocationException) when (!forceReindex)
    {
        TenantEmbeddingConfig currentConfig = await actor.GetEmbeddingConfigAsync();
        string[] affectedFields = EmbeddingProviderDefaults.GetBreakingChangeFields(currentConfig, config);
        if (affectedFields.Length > 0)
        {
            return Results.Conflict(CreateEmbeddingConfigConflictResponse(
                tenantId,
                currentConfig,
                config,
                affectedFields));
        }

        throw;
    }
});

// Story 5.1: Tenant provisioning endpoints
app.MapPost("/api/tenants", async (
    DaprWorkflowClient workflowClient,
    IActorProxyFactory actorProxyFactory,
    TenantProvisioningInput input,
    ILogger<Program> logger) =>
{
    ErrorResponse? tenantValidationError = ValidateTenantId(input.TenantId);
    if (tenantValidationError is not null)
    {
        return Results.BadRequest(tenantValidationError);
    }

    if (string.IsNullOrWhiteSpace(input.DisplayName))
    {
        return Results.BadRequest(new ErrorResponse(
            "INVALID_INPUT",
            "DisplayName is required.",
            "Provide a non-empty display name for the tenant."));
    }

    // Resolve vector dimensions from TenantConfigurationActor or default to 768
    int resolvedDimensions = EmbeddingProviderDefaults.Google().Dimensions;
    try
    {
        ITenantConfigurationActor actor = actorProxyFactory
            .CreateActorProxy<ITenantConfigurationActor>(new ActorId(input.TenantId), nameof(TenantConfigurationActor));
        TenantEmbeddingConfig config = await actor.GetEmbeddingConfigAsync();
        resolvedDimensions = config.Dimensions;
    }
    catch (Exception ex)
    {
        logger.LogWarning(ex, "Could not resolve embedding config for tenant {TenantId} — defaulting to {Dimensions} dimensions",
            input.TenantId, resolvedDimensions);
    }

    if (resolvedDimensions < 1 || resolvedDimensions > 4096)
    {
        return Results.BadRequest(new ErrorResponse(
            "INVALID_DIMENSIONS",
            $"Vector dimensions {resolvedDimensions} must be between 1 and 4096.",
            "Check the embedding provider configuration for this tenant."));
    }

    input = input with { VectorDimensions = resolvedDimensions };

    string instanceId = $"provision-{input.TenantId}-{Guid.NewGuid():N}";
    try
    {
        await workflowClient.ScheduleNewWorkflowAsync(
            nameof(TenantProvisioningWorkflow), instanceId, input);
    }
    catch (Dapr.DaprException)
    {
        return Results.Json(
            new ErrorResponse(
                "DAPR_UNAVAILABLE",
                "DAPR sidecar is not ready.",
                "Check service health via /healthz and retry."),
            statusCode: 503);
    }

    return Results.Accepted($"/api/tenants/{input.TenantId}/provision-status/{instanceId}",
        new { workflowInstanceId = instanceId });
});

app.MapGet("/api/tenants/{tenantId}/provision-status/{instanceId}", async (
    DaprWorkflowClient workflowClient,
    TenantStatusGuard tenantGuard,
    string tenantId,
    string instanceId,
    CancellationToken cancellationToken) =>
{
    ErrorResponse? tenantValidationError = ValidateTenantId(tenantId);
    if (tenantValidationError is not null)
    {
        return Results.BadRequest(tenantValidationError);
    }

    ErrorResponse? tenantExistsError = await tenantGuard.ValidateTenantExistsAsync(tenantId, cancellationToken);
    if (tenantExistsError is not null)
    {
        return TenantStatusGuard.ToHttpResult(tenantExistsError);
    }

    if (!instanceId.StartsWith($"provision-{tenantId}-", StringComparison.Ordinal))
    {
        return Results.NotFound(new ErrorResponse(
            "PROVISIONING_STATUS_NOT_FOUND",
            $"Provisioning workflow '{instanceId}' was not found for tenant '{tenantId}'.",
            "Use the workflowInstanceId returned by POST /api/tenants for the same tenant."));
    }

    WorkflowState? state = await workflowClient.GetWorkflowStateAsync(instanceId);
    return state is null ? Results.NotFound() : Results.Ok(state);
});

// Story 5.5 AC1 / FR41: enriched tenant listing — per-tenant counts + index health + activity.
// Contract change (pre-Gate-2): now returns TenantSummary[] instead of TenantInfo[].
app.MapGet("/api/tenants", async (
    TenantRegistryService registry,
    TenantMetricsService metrics,
    IActorProxyFactory actorProxyFactory,
    CancellationToken cancellationToken) =>
{
    IReadOnlyList<TenantInfo> tenants = await registry.ListTenantsAsync(cancellationToken);
    Task<TenantSummary>[] tasks = tenants
        .Select(tenant => TenantEndpointHandlers.BuildTenantSummaryAsync(tenant, metrics, actorProxyFactory, cancellationToken))
        .ToArray();
    TenantSummary[] summaries = await Task.WhenAll(tasks);
    return Results.Ok(summaries);
});

app.MapGet("/api/tenants/{tenantId}", async (TenantRegistryService registry, string tenantId) =>
{
    ErrorResponse? tenantValidationError = ValidateTenantId(tenantId);
    if (tenantValidationError is not null)
    {
        return Results.BadRequest(tenantValidationError);
    }

    TenantInfo? tenant = await registry.GetTenantAsync(tenantId, CancellationToken.None);
    return tenant is null
        ? Results.NotFound(new ErrorResponse(
            "TENANT_NOT_FOUND",
            $"Tenant '{tenantId}' not found.",
            "Use GET /api/tenants to list available tenants."))
        : Results.Ok(tenant);
});

// Story 5.5 AC2 / FR45: composed configuration view (embedding + metrics + health).
app.MapGet("/api/tenants/{tenantId}/configuration", TenantEndpointHandlers.GetTenantConfigurationAsync);

// Story 5.5 AC3 / FR42: PATCH display name (rate-limit updates go through PUT /embedding-config).
app.MapPatch("/api/tenants/{tenantId}", TenantEndpointHandlers.PatchDisplayNameAsync);

// Story 5.2: Tenant deletion endpoints
app.MapDelete("/api/tenants/{tenantId}", async (
    DaprWorkflowClient workflowClient,
    TenantRegistryService registry,
    string tenantId) =>
{
    ErrorResponse? tenantValidationError = ValidateTenantId(tenantId);
    if (tenantValidationError is not null)
    {
        return Results.BadRequest(tenantValidationError);
    }

    TenantRegistryEntry? tenantEntry = await registry.GetTenantEntryAsync(tenantId, CancellationToken.None);
    if (tenantEntry is null)
    {
        return Results.NotFound(new ErrorResponse(
            "TENANT_NOT_FOUND",
            $"Tenant '{tenantId}' not found.",
            "Use GET /api/tenants to list available tenants."));
    }

    if (tenantEntry.Tenant.Status == TenantStatus.Provisioning)
    {
        return Results.Conflict(new ErrorResponse(
            "TENANT_PROVISIONING",
            $"Tenant '{tenantId}' is still provisioning.",
            "Wait for provisioning to complete."));
    }

    if (tenantEntry.Tenant.Status == TenantStatus.Deleting &&
        !string.IsNullOrWhiteSpace(tenantEntry.WorkflowInstanceId))
    {
        try
        {
            WorkflowState? existingState = await workflowClient.GetWorkflowStateAsync(tenantEntry.WorkflowInstanceId);
            if (existingState?.Exists == true && !existingState.IsWorkflowCompleted)
            {
                return Results.Accepted(
                    $"/api/tenants/{tenantId}/deletion-status/{tenantEntry.WorkflowInstanceId}",
                    new
                    {
                        workflowInstanceId = tenantEntry.WorkflowInstanceId,
                        message = "Deletion already in progress.",
                    });
            }
        }
        catch (Dapr.DaprException)
        {
            return Results.Json(
                new ErrorResponse(
                    "DAPR_UNAVAILABLE",
                    "DAPR sidecar is not ready.",
                    "Check service health via /healthz and retry."),
                statusCode: 503);
        }
    }

    string instanceId = $"delete-{tenantId}-{Guid.NewGuid():N}";
    TenantRegistryEntry? deletionClaim = await registry.BeginTenantDeletionAsync(
        tenantId,
        instanceId,
        allowRetryFromDeleting: tenantEntry.Tenant.Status == TenantStatus.Deleting,
        tenantEntry.WorkflowInstanceId,
        CancellationToken.None);

    if (deletionClaim is null)
    {
        return Results.NotFound(new ErrorResponse(
            "TENANT_NOT_FOUND",
            $"Tenant '{tenantId}' not found.",
            "Use GET /api/tenants to list available tenants."));
    }

    if (deletionClaim.Tenant.Status == TenantStatus.Provisioning)
    {
        return Results.Conflict(new ErrorResponse(
            "TENANT_PROVISIONING",
            $"Tenant '{tenantId}' is still provisioning.",
            "Wait for provisioning to complete."));
    }

    if (!string.Equals(deletionClaim.WorkflowInstanceId, instanceId, StringComparison.Ordinal))
    {
        return Results.Accepted(
            $"/api/tenants/{tenantId}/deletion-status/{deletionClaim.WorkflowInstanceId}",
            new
            {
                workflowInstanceId = deletionClaim.WorkflowInstanceId,
                message = "Deletion already in progress.",
            });
    }

    try
    {
        await workflowClient.ScheduleNewWorkflowAsync(
            nameof(TenantDeletionWorkflow), instanceId, new TenantDeletionInput(tenantId));
    }
    catch (Dapr.DaprException)
    {
        if (tenantEntry.Tenant.Status != TenantStatus.Deleting)
        {
            try
            {
                await registry.UpdateTenantStatusAsync(
                    tenantId,
                    tenantEntry.Tenant.Status,
                    CancellationToken.None,
                    tenantEntry.WorkflowInstanceId);
            }
            catch (InvalidOperationException)
            {
                // Best effort rollback only — the original Dapr error is more actionable to callers.
            }
        }

        return Results.Json(
            new ErrorResponse(
                "DAPR_UNAVAILABLE",
                "DAPR sidecar is not ready.",
                "Check service health via /healthz and retry."),
            statusCode: 503);
    }

    return Results.Accepted($"/api/tenants/{tenantId}/deletion-status/{instanceId}",
        new { workflowInstanceId = instanceId });
});

app.MapGet("/api/tenants/{tenantId}/deletion-status/{instanceId}", async (
    DaprWorkflowClient workflowClient,
    TenantStatusGuard tenantGuard,
    string tenantId,
    string instanceId,
    CancellationToken cancellationToken) =>
{
    ErrorResponse? tenantValidationError = ValidateTenantId(tenantId);
    if (tenantValidationError is not null)
    {
        return Results.BadRequest(tenantValidationError);
    }

    ErrorResponse? tenantExistsError = await tenantGuard.ValidateTenantExistsAsync(tenantId, cancellationToken);
    if (tenantExistsError is not null)
    {
        return TenantStatusGuard.ToHttpResult(tenantExistsError);
    }

    if (!instanceId.StartsWith($"delete-{tenantId}-", StringComparison.Ordinal))
    {
        return Results.NotFound(new ErrorResponse(
            "DELETION_STATUS_NOT_FOUND",
            $"Deletion workflow '{instanceId}' was not found for tenant '{tenantId}'.",
            "Use the workflowInstanceId returned by DELETE /api/tenants/{tenantId} for the same tenant."));
    }

    WorkflowState? state = await workflowClient.GetWorkflowStateAsync(instanceId);
    return state is null ? Results.NotFound() : Results.Ok(state);
});

// Story 5.3: Tenant isolation verification
app.MapPost("/api/tenants/{tenantId}/verify", async (
    TenantIsolationVerifier verifier,
    TenantStatusGuard tenantGuard,
    string tenantId,
    CancellationToken cancellationToken) =>
{
    ErrorResponse? tenantValidationError = ValidateTenantId(tenantId);
    if (tenantValidationError is not null)
    {
        return Results.BadRequest(tenantValidationError);
    }

    // Verification is diagnostic — allow it on non-Active tenants (Provisioning, Deleting, Failed)
    // as long as the tenant exists in the registry. Existence-only check is correct here.
    ErrorResponse? tenantExistsError = await tenantGuard.ValidateTenantExistsAsync(tenantId, cancellationToken);
    if (tenantExistsError is not null)
    {
        return TenantStatusGuard.ToHttpResult(tenantExistsError);
    }

    try
    {
        TenantIsolationVerificationResult result = await verifier.VerifyAsync(tenantId, cancellationToken);
        return Results.Ok(result);
    }
    catch (Dapr.DaprException ex)
    {
        return Results.Json(
            new ErrorResponse("DAPR_UNAVAILABLE", $"DAPR sidecar unavailable: {ex.Message}", "Check DAPR sidecar connectivity and retry."),
            statusCode: 503);
    }
    catch (RedisException ex)
    {
        return Results.Json(
            new ErrorResponse("BACKEND_UNAVAILABLE", $"Backend unavailable: {ex.Message}", "Check Redis/FalkorDB connectivity and retry."),
            statusCode: 503);
    }
});

app.MapPost("/api/tenants/{tenantId}/cases", async (
    string tenantId,
    CreateCaseInput input,
    CaseService caseService,
    TenantStatusGuard tenantGuard,
    CancellationToken cancellationToken) =>
{
    var validatedInput = input with { TenantId = tenantId };
    ErrorResponse? error = CaseValidator.ValidateCreateCase(validatedInput);
    if (error is not null)
    {
        return Results.BadRequest(error);
    }

    ErrorResponse? tenantStatusError = await tenantGuard.ValidateTenantActiveAsync(tenantId, cancellationToken);
    if (tenantStatusError is not null)
    {
        return TenantStatusGuard.ToHttpResult(tenantStatusError);
    }

    Case created = await caseService.CreateCaseAsync(validatedInput, cancellationToken);
    return Results.Created($"/api/tenants/{tenantId}/cases/{created.Id}", created);
});

app.MapGet("/api/tenants/{tenantId}/cases", async (
    string tenantId,
    int? limit,
    CaseService caseService,
    CancellationToken cancellationToken) =>
{
    // TODO: Extract TenantIdValidationFilter when endpoint count > 5 (Story 3.4+)
    try
    {
        TenantIdGuard.Validate(tenantId);
    }
    catch (ArgumentException)
    {
        return Results.BadRequest(new ErrorResponse("INVALID_TENANT_ID", "TenantId contains invalid characters.", "Only alphanumeric and hyphens allowed."));
    }

    int effectiveLimit = Math.Clamp(limit ?? 100, 1, 500);
    List<Case> cases = await caseService.ListCasesAsync(tenantId, effectiveLimit, cancellationToken);
    return Results.Ok(cases);
});

app.MapGet("/api/tenants/{tenantId}/cases/{caseId}", async (
    string tenantId,
    string caseId,
    CaseService caseService,
    CancellationToken cancellationToken) =>
{
    // TODO: Extract TenantIdValidationFilter when endpoint count > 5 (Story 3.4+)
    try
    {
        TenantIdGuard.Validate(tenantId);
    }
    catch (ArgumentException)
    {
        return Results.BadRequest(new ErrorResponse("INVALID_TENANT_ID", "TenantId contains invalid characters.", "Only alphanumeric and hyphens allowed."));
    }

    Case? caseResult = await caseService.GetCaseAsync(tenantId, caseId, cancellationToken);
    return caseResult is null
        ? Results.NotFound(new ErrorResponse("CASE_NOT_FOUND", $"Case '{caseId}' does not exist in tenant '{tenantId}'.", "Run 'memories case list' to see available cases."))
        : Results.Ok(caseResult);
});

app.MapGet("/api/tenants/{tenantId}/cases/{caseId}/status", async (
    string tenantId,
    string caseId,
    CaseService caseService,
    CancellationToken cancellationToken) =>
{
    try
    {
        TenantIdGuard.Validate(tenantId);
    }
    catch (ArgumentException)
    {
        return Results.BadRequest(new ErrorResponse("INVALID_TENANT_ID", "TenantId contains invalid characters.", "Only alphanumeric and hyphens allowed."));
    }

    CaseStatusDetail? status = await caseService.GetCaseStatusAsync(tenantId, caseId, cancellationToken);
    return status is null
        ? Results.NotFound(new ErrorResponse("CASE_NOT_FOUND", $"Case '{caseId}' does not exist in tenant '{tenantId}'.", "Run 'memories case list' to see available cases."))
        : Results.Ok(status);
});

app.MapGet("/api/tenants/{tenantId}/cases/{caseId}/activity", async (
    string tenantId,
    string caseId,
    int? limit,
    CaseService caseService,
    CaseActivityService activityService,
    CancellationToken cancellationToken) =>
{
    try
    {
        TenantIdGuard.Validate(tenantId);
    }
    catch (ArgumentException)
    {
        return Results.BadRequest(new ErrorResponse("INVALID_TENANT_ID", "TenantId contains invalid characters.", "Only alphanumeric and hyphens allowed."));
    }

    Case? caseResult = await caseService.GetCaseAsync(tenantId, caseId, cancellationToken);
    if (caseResult is null)
    {
        return Results.NotFound(new ErrorResponse("CASE_NOT_FOUND", $"Case '{caseId}' does not exist in tenant '{tenantId}'.", "Run 'memories case list' to see available cases."));
    }

    int effectiveLimit = Math.Clamp(limit ?? 50, 1, 500);
    List<CaseActivityEvent> events = await activityService.GetRecentActivityAsync(tenantId, caseId, effectiveLimit, cancellationToken);
    return Results.Ok(events);
});

app.MapPut("/api/tenants/{tenantId}/cases/{caseId}/members/{memberId}", async (
    string tenantId,
    string caseId,
    string memberId,
    JsonElement requestBody,
    CaseService caseService,
    CancellationToken cancellationToken) =>
{
    ErrorResponse? bodyError = TryDeserializeAddCaseMemberInput(requestBody, out AddCaseMemberInput? input);
    if (bodyError is not null)
    {
        return Results.BadRequest(bodyError);
    }

    AddCaseMemberInput validatedInput = input! with { MemberId = memberId };
    ErrorResponse? error = CaseValidator.ValidateAddMember(tenantId, caseId, validatedInput);
    if (error is not null)
    {
        return Results.BadRequest(error);
    }

    Case? caseResult = await caseService.GetCaseAsync(tenantId, caseId, cancellationToken);
    if (caseResult is null)
    {
        return Results.NotFound(new ErrorResponse("CASE_NOT_FOUND", $"Case '{caseId}' does not exist in tenant '{tenantId}'.", "Run 'memories case list' to see available cases."));
    }

    try
    {
        (CaseMember member, bool created) = await caseService.AddMemberAsync(tenantId, caseId, validatedInput, cancellationToken);
        return created
            ? Results.Created($"/api/tenants/{tenantId}/cases/{caseId}/members/{memberId}", member)
            : Results.Ok(member);
    }
    catch (InvalidOperationException ex) when (ex.Message.Contains("maximum"))
    {
        return Results.BadRequest(new ErrorResponse("MEMBER_LIMIT_EXCEEDED", ex.Message, "Remove existing members before adding new ones."));
    }
});

app.MapDelete("/api/tenants/{tenantId}/cases/{caseId}/members/{memberId}", async (
    string tenantId,
    string caseId,
    string memberId,
    CaseService caseService,
    CancellationToken cancellationToken) =>
{
    ErrorResponse? error = CaseValidator.ValidateRemoveMember(tenantId, caseId, memberId);
    if (error is not null)
    {
        return Results.BadRequest(error);
    }

    Case? caseResult = await caseService.GetCaseAsync(tenantId, caseId, cancellationToken);
    if (caseResult is null)
    {
        return Results.NotFound(new ErrorResponse("CASE_NOT_FOUND", $"Case '{caseId}' does not exist in tenant '{tenantId}'.", "Run 'memories case list' to see available cases."));
    }

    bool removed = await caseService.RemoveMemberAsync(tenantId, caseId, memberId, cancellationToken);
    return removed
        ? Results.NoContent()
        : Results.NotFound(new ErrorResponse("MEMBER_NOT_FOUND", $"Member '{memberId}' is not in case '{caseId}'.", "Run GET /cases/{caseId}/members to see current members."));
});

app.MapGet("/api/tenants/{tenantId}/cases/{caseId}/members", async (
    string tenantId,
    string caseId,
    CaseService caseService,
    CancellationToken cancellationToken) =>
{
    ErrorResponse? caseIdError = CaseValidator.ValidateCaseId(caseId);
    if (caseIdError is not null)
    {
        return Results.BadRequest(caseIdError);
    }

    try
    {
        TenantIdGuard.Validate(tenantId);
    }
    catch (ArgumentException)
    {
        return Results.BadRequest(new ErrorResponse("INVALID_TENANT_ID", "TenantId contains invalid characters.", "Only alphanumeric and hyphens allowed."));
    }

    Case? caseResult = await caseService.GetCaseAsync(tenantId, caseId, cancellationToken);
    if (caseResult is null)
    {
        return Results.NotFound(new ErrorResponse("CASE_NOT_FOUND", $"Case '{caseId}' does not exist in tenant '{tenantId}'.", "Run 'memories case list' to see available cases."));
    }

    List<CaseMember> members = await caseService.ListMembersAsync(tenantId, caseId, cancellationToken);
    return Results.Ok(members);
});

app.MapDelete("/api/tenants/{tenantId}/cases/{caseId}/memory-units/{memoryUnitId}", async (
    string tenantId,
    string caseId,
    string memoryUnitId,
    CaseService caseService,
    TenantStatusGuard tenantGuard,
    CancellationToken cancellationToken) =>
{
    ErrorResponse? validationError = CaseValidator.ValidateDeleteMemoryUnit(tenantId, caseId, memoryUnitId);
    if (validationError is not null)
    {
        return Results.BadRequest(validationError);
    }

    ErrorResponse? tenantStatusError = await tenantGuard.ValidateTenantActiveAsync(tenantId, cancellationToken);
    if (tenantStatusError is not null)
    {
        return TenantStatusGuard.ToHttpResult(tenantStatusError);
    }

    Case? targetCase = await caseService.GetCaseAsync(tenantId, caseId, cancellationToken);
    if (targetCase is null)
    {
        return Results.NotFound(new ErrorResponse(
            "CASE_NOT_FOUND",
            $"Case '{caseId}' not found in tenant '{tenantId}'.",
            $"Use GET /api/tenants/{tenantId}/cases to list available cases."));
    }

    if (targetCase.Status == CaseStatus.Deleting)
    {
        return Results.Conflict(new ErrorResponse(
            "CASE_DELETING",
            $"Case '{caseId}' is being deleted.",
            "Wait for deletion to complete or retry later."));
    }

    bool deleted = await caseService.DeleteMemoryUnitAsync(tenantId, caseId, memoryUnitId, cancellationToken);
    return deleted
        ? Results.NoContent()
        : Results.NotFound(new ErrorResponse(
            "MEMORY_UNIT_NOT_FOUND",
            $"Memory unit '{memoryUnitId}' not found in case '{caseId}'.",
            $"Use GET /api/search?tenantId={tenantId}&caseId={caseId} to find available memory units."));
});

app.MapDelete("/api/tenants/{tenantId}/cases/{caseId}", async (
    string tenantId,
    string caseId,
    CaseService caseService,
    TenantStatusGuard tenantGuard,
    CancellationToken cancellationToken) =>
{
    ErrorResponse? tenantError = CaseValidator.ValidateTenantId(tenantId);
    if (tenantError is not null)
    {
        return Results.BadRequest(tenantError);
    }

    ErrorResponse? caseError = CaseValidator.ValidateCaseId(caseId);
    if (caseError is not null)
    {
        return Results.BadRequest(caseError);
    }

    ErrorResponse? tenantStatusError = await tenantGuard.ValidateTenantActiveAsync(tenantId, cancellationToken);
    if (tenantStatusError is not null)
    {
        return TenantStatusGuard.ToHttpResult(tenantStatusError);
    }

    bool deleted = await caseService.DeleteCaseAsync(tenantId, caseId, cancellationToken);
    return deleted
        ? Results.NoContent()
        : Results.NotFound(new ErrorResponse(
            "CASE_NOT_FOUND",
            $"Case '{caseId}' not found in tenant '{tenantId}'.",
            $"Use GET /api/tenants/{tenantId}/cases to list available cases."));
});

app.MapPost("/api/tenants/{tenantId}/cases/{caseId}/memory-units/{memoryUnitId}/annotations", async (
    string tenantId,
    string caseId,
    string memoryUnitId,
    CreateAnnotationInput input,
    CaseService caseService,
    CancellationToken cancellationToken) =>
{
    var validatedInput = input with { TenantId = tenantId, CaseId = caseId, TargetMemoryUnitId = memoryUnitId };
    ErrorResponse? validationError = CaseValidator.ValidateCreateAnnotation(tenantId, caseId, memoryUnitId, validatedInput);
    if (validationError is not null)
    {
        return Results.BadRequest(validationError);
    }

    Case? targetCase = await caseService.GetCaseAsync(tenantId, caseId, cancellationToken);
    if (targetCase is null)
    {
        return Results.NotFound(new ErrorResponse(
            "CASE_NOT_FOUND",
            $"Case '{caseId}' not found in tenant '{tenantId}'.",
            $"Use GET /api/tenants/{tenantId}/cases to list available cases."));
    }

    if (targetCase.Status == CaseStatus.Deleting)
    {
        return Results.Conflict(new ErrorResponse(
            "CASE_DELETING",
            $"Case '{caseId}' is being deleted.",
            "Wait for deletion to complete or retry later."));
    }

    try
    {
        var result = await caseService.CreateAnnotationAsync(validatedInput, cancellationToken);
        if (result is null)
        {
            return Results.NotFound(new ErrorResponse(
                "MEMORY_UNIT_NOT_FOUND",
                $"Memory unit '{memoryUnitId}' not found in case '{caseId}'.",
                $"Use GET /api/search?tenantId={tenantId}&caseId={caseId} to find available memory units."));
        }

        return Results.Accepted(
            $"/api/ingest/{result.Value.WorkflowInstanceId}",
            new { memoryUnit = result.Value.Annotation, instanceId = result.Value.WorkflowInstanceId });
    }
    catch (InvalidOperationException ex) when (ex.Message == "MEMORY_UNIT_NOT_INDEXED")
    {
        return Results.BadRequest(new ErrorResponse(
            "MEMORY_UNIT_NOT_INDEXED",
            $"Memory unit '{memoryUnitId}' is not yet indexed.",
            "Wait for ingestion to complete before annotating."));
    }
    catch (InvalidOperationException ex) when (ex.Message == "NESTED_ANNOTATION_NOT_ALLOWED")
    {
        return Results.BadRequest(new ErrorResponse(
            "NESTED_ANNOTATION_NOT_ALLOWED",
            "Cannot annotate an annotation. The target memory unit is itself an annotation.",
            "Annotate the original memory unit instead."));
    }
});

app.MapGet("/api/tenants/{tenantId}/cases/{caseId}/memory-units/{memoryUnitId}/annotations", async (
    string tenantId,
    string caseId,
    string memoryUnitId,
    CaseService caseService,
    CancellationToken cancellationToken) =>
{
    ErrorResponse? validationError = CaseValidator.ValidateDeleteMemoryUnit(tenantId, caseId, memoryUnitId);
    if (validationError is not null)
    {
        return Results.BadRequest(validationError);
    }

    Case? targetCase = await caseService.GetCaseAsync(tenantId, caseId, cancellationToken);
    if (targetCase is null)
    {
        return Results.NotFound(new ErrorResponse(
            "CASE_NOT_FOUND",
            $"Case '{caseId}' not found in tenant '{tenantId}'.",
            $"Use GET /api/tenants/{tenantId}/cases to list available cases."));
    }

    MemoryUnit? targetMemoryUnit = await caseService.GetMemoryUnitAsync(tenantId, memoryUnitId, cancellationToken);
    if (targetMemoryUnit is null || !string.Equals(targetMemoryUnit.CaseId, caseId, StringComparison.Ordinal))
    {
        return Results.NotFound(new ErrorResponse(
            "MEMORY_UNIT_NOT_FOUND",
            $"Memory unit '{memoryUnitId}' not found in case '{caseId}'.",
            $"Use GET /api/search?tenantId={tenantId}&caseId={caseId} to find available memory units."));
    }

    List<MemoryUnit> annotations = await caseService.ListAnnotationsAsync(tenantId, memoryUnitId, cancellationToken);
    return Results.Ok(annotations);
});

app.MapGet("/api/search", async (
    SyntacticSearchService syntacticService,
    SemanticSearchService semanticService,
    GraphScopedSearch graphScopedSearch,
    HybridSearchService hybridSearchService,
    IActorProxyFactory actorProxyFactory,
    CaseActivityService activityService,
    CaseService caseService,
    TenantStatusGuard tenantGuard,
    IGraphQueryBuilder graphQueryBuilder,
    [FromKeyedServices("falkordb")] IConnectionMultiplexer falkorDb,
    ILogger<Program> logger,
    HttpContext httpContext,
    [FromQuery] string tenantId,
    [FromQuery] string? query,
    [FromQuery] string? caseId,
    [FromQuery] string? sourceType = null,
    [FromQuery] string? metadataQuery = null,
    [FromQuery] int maxResults = 10,
    [FromQuery] int offset = 0,
    [FromQuery] string axis = "syntactic",
    [FromQuery] string? axes = null,
    [FromQuery] string? startNodeId = null,
    [FromQuery(Name = "graphStartNodeId")] string? graphStartNodeId = null,
    [FromQuery] int depth = 2,
    [FromQuery] bool explain = false,
    CancellationToken cancellationToken = default) =>
{
    void RecordSearchActivity()
    {
        if (!string.IsNullOrWhiteSpace(caseId))
        {
            _ = activityService.RecordEventAsync(tenantId, caseId!, CaseActivityEventType.SearchExecuted, "system", $"Search '{query}' via {axis}", null);
        }
    }

    if (string.IsNullOrWhiteSpace(tenantId))
    {
        return Results.BadRequest(new ErrorResponse(
            "INVALID_INPUT",
            "Parameter 'tenantId' is required.",
            "Provide tenantId as a query parameter."));
    }

    ErrorResponse? tenantValidationError = ValidateTenantId(tenantId);
    if (tenantValidationError is not null)
    {
        return Results.BadRequest(tenantValidationError);
    }

    ErrorResponse? tenantStatusError = await tenantGuard.ValidateTenantActiveAsync(tenantId, cancellationToken);
    if (tenantStatusError is not null)
    {
        return TenantStatusGuard.ToHttpResult(tenantStatusError);
    }

    // Validate caseId exists before executing search
    if (!string.IsNullOrWhiteSpace(caseId))
    {
        Case? targetCase = await caseService.GetCaseAsync(tenantId, caseId, cancellationToken);
        if (targetCase is null)
        {
            return Results.NotFound(new ErrorResponse(
                "CASE_NOT_FOUND",
                $"Case '{caseId}' not found in tenant '{tenantId}'.",
                $"Use GET /api/tenants/{tenantId}/cases to list available cases."));
        }
    }

    // Validate sourceType is a known enum value
    if (!string.IsNullOrWhiteSpace(sourceType) && !Enum.TryParse<SourceType>(sourceType, ignoreCase: true, out _))
    {
        return Results.BadRequest(new ErrorResponse(
            "INVALID_SOURCE_TYPE",
            $"Source type '{sourceType}' is not recognized.",
            "Valid values: file, url, event, command, projection, discussion, annotation."));
    }

    // Validate axis BEFORE query — axis determines whether query is required
    if (!string.Equals(axis, "syntactic", StringComparison.OrdinalIgnoreCase) &&
        !string.Equals(axis, "semantic", StringComparison.OrdinalIgnoreCase) &&
        !string.Equals(axis, "graph", StringComparison.OrdinalIgnoreCase) &&
        !string.Equals(axis, "hybrid", StringComparison.OrdinalIgnoreCase))
    {
        return Results.BadRequest(new ErrorResponse(
            "INVALID_AXIS",
            $"Search axis '{axis}' is not supported. Supported axes: syntactic, semantic, graph, hybrid.",
            "Use axis=syntactic, axis=semantic, axis=graph, or axis=hybrid."));
    }

    // --- axis=graph: pure traversal (query NOT required) ---
    if (string.Equals(axis, "graph", StringComparison.OrdinalIgnoreCase))
    {
        if (string.IsNullOrWhiteSpace(startNodeId))
        {
            return Results.BadRequest(new ErrorResponse(
                "MISSING_START_NODE",
                "Graph-scoped search requires a startNodeId parameter.",
                "Provide startNodeId=<memoryUnitId> to specify the graph traversal starting point."));
        }

        int clampedDepth = Math.Clamp(depth, 0, 10);
        int clampedMaxResults = Math.Clamp(maxResults, 1, 100);
        var searchQuery = new SearchQuery
        {
            TenantId = tenantId,
            Query = query ?? string.Empty,
            CaseId = caseId,
            SourceTypeFilter = sourceType,
            MetadataQuery = metadataQuery,
            MaxResults = clampedMaxResults,
            Offset = Math.Max(offset, 0),
        };

        SearchResult result;
        try
        {
            result = await graphScopedSearch.SearchAsync(
                searchQuery, startNodeId, clampedDepth,
                innerSearch: null, cancellationToken);
        }
        catch (RedisConnectionException ex)
        {
            return SearchEndpointDegradationResponses.BuildGraphUnavailableResponse(httpContext, logger, tenantId, startNodeId, ex);
        }
        catch (RedisTimeoutException ex)
        {
            return SearchEndpointDegradationResponses.BuildGraphUnavailableResponse(httpContext, logger, tenantId, startNodeId, ex);
        }
        catch (RedisServerException ex) when (SearchEndpointDegradationLog.IsTransientRedisError(ex))
        {
            return SearchEndpointDegradationResponses.BuildGraphUnavailableResponse(httpContext, logger, tenantId, startNodeId, ex);
        }
        catch (TimeoutException)
        {
            // TimeoutException (504 GRAPH_TIMEOUT) is semantically distinct from RedisTimeoutException (503):
            // 504 = live FalkorDB, query exceeded deadline → caller should reduce depth.
            // 503 = multiplexer command-layer timeout → caller should retry.
            return SearchEndpointDegradationResponses.BuildGraphTimeoutResponse();
        }

        result = await EnrichResultWithCaseAttributionAsync(result, caseService, tenantId, cancellationToken);
        result = await EnrichResultWithAnnotationCountsAsync(result, graphQueryBuilder, falkorDb, tenantId, cancellationToken);
        if (explain)
        {
            result = result with { Explanation = ExplainMetadataBuilder.BuildForSingleAxis("graph") };
        }

        RecordSearchActivity();
        return Results.Ok(result);
    }

    // --- axis=hybrid: multi-axis fusion ---
    if (string.Equals(axis, "hybrid", StringComparison.OrdinalIgnoreCase))
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return Results.BadRequest(new ErrorResponse(
                "INVALID_INPUT",
                "Parameter 'query' is required for hybrid search.",
                "Provide query as a query parameter."));
        }

        // Parse enabled axes (default: all three)
        HashSet<string> enabledAxes = new(StringComparer.OrdinalIgnoreCase) { "syntactic", "semantic", "graph" };
        if (!string.IsNullOrWhiteSpace(axes))
        {
            enabledAxes = new HashSet<string>(
                axes.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries),
                StringComparer.OrdinalIgnoreCase);
        }

        string? invalidAxis = HybridSearchService.FindInvalidAxis(enabledAxes);
        if (invalidAxis is not null)
        {
            return Results.BadRequest(new ErrorResponse(
                "INVALID_AXIS",
                $"Unknown axis '{invalidAxis}' in axes parameter. Valid axes: syntactic, semantic, graph.",
                "Use a comma-separated list of valid axis names, e.g., axes=syntactic,semantic."));
        }

        var hybridQuery = new SearchQuery
        {
            TenantId = tenantId,
            Query = query,
            CaseId = caseId,
            SourceTypeFilter = sourceType,
            MetadataQuery = metadataQuery,
            MaxResults = Math.Clamp(maxResults, 1, 100),
            Offset = Math.Max(offset, 0),
        };

        var weights = new FusionWeights();
        TenantEmbeddingConfig? embeddingConfig = null;
        List<string> preUnavailableAxes = [];
        string? effectiveGraphStartNodeId = !string.IsNullOrWhiteSpace(graphStartNodeId)
            ? graphStartNodeId
            : startNodeId;

        if (enabledAxes.Contains("semantic"))
        {
            try
            {
                ITenantConfigurationActor actor = actorProxyFactory
                    .CreateActorProxy<ITenantConfigurationActor>(
                        new ActorId(tenantId), nameof(TenantConfigurationActor));
                embeddingConfig = await actor.GetEmbeddingConfigAsync();
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch
            {
                preUnavailableAxes.Add("semantic");
            }
        }

        int clampedDepth = Math.Clamp(depth, 0, 10);
        HybridSearchResult hybridResult = await hybridSearchService.SearchAsync(
            hybridQuery,
            embeddingConfig,
            effectiveGraphStartNodeId,
            clampedDepth,
            weights,
            enabledAxes,
            preUnavailableAxes,
            cancellationToken);

        if (hybridResult.AllEnabledAxesUnavailable == true)
        {
            // Total failure: every attempted axis landed in unavailableAxes. Promote to 503
            // rather than the misleading "200 OK { results: [], degraded: true }" shape that would
            // otherwise be returned.
            return SearchEndpointDegradationResponses.BuildAllBackendsUnavailableResponse(
                httpContext,
                logger,
                tenantId,
                hybridResult.UnavailableAxes,
                enabledAxes.ToArray());
        }

        hybridResult = await EnrichHybridResultWithCaseAttributionAsync(hybridResult, caseService, tenantId, cancellationToken);
        hybridResult = await EnrichHybridResultWithAnnotationCountsAsync(hybridResult, graphQueryBuilder, falkorDb, tenantId, cancellationToken);

        if (explain)
        {
            IReadOnlySet<string> explanationAxes = DetermineHybridExplanationAxes(
                enabledAxes,
                hybridResult.UnavailableAxes,
                embeddingConfig is not null,
                !string.IsNullOrWhiteSpace(effectiveGraphStartNodeId));
            hybridResult = hybridResult with { Explanation = ExplainMetadataBuilder.BuildForHybrid(explanationAxes, weights) };
        }

        RecordSearchActivity();
        return Results.Ok(hybridResult);
    }

    // --- For syntactic, semantic, and graph-scoped inner search: query IS required ---
    if (string.IsNullOrWhiteSpace(query))
    {
        return Results.BadRequest(new ErrorResponse(
            "INVALID_INPUT",
            "Parameter 'query' is required for syntactic and semantic search.",
            "Provide query as a query parameter."));
    }

    int clampedMax = Math.Clamp(maxResults, 1, 100);
    int clampedOff = Math.Max(offset, 0);
    var mainSearchQuery = new SearchQuery
    {
        TenantId = tenantId,
        Query = query,
        CaseId = caseId,
        SourceTypeFilter = sourceType,
        MetadataQuery = metadataQuery,
        MaxResults = clampedMax,
        Offset = clampedOff,
    };

    // --- Graph-scoped inner search (syntactic/semantic + startNodeId) ---
    if (!string.IsNullOrWhiteSpace(startNodeId))
    {
        int clampedDepth = Math.Clamp(depth, 0, 10);

        if (string.Equals(axis, "semantic", StringComparison.OrdinalIgnoreCase))
        {
            ITenantConfigurationActor actor = actorProxyFactory
                .CreateActorProxy<ITenantConfigurationActor>(
                    new ActorId(tenantId), nameof(TenantConfigurationActor));

            // Task 2.3 Option A: flip innerSearchStarted=true at the top of the inner-search lambda.
            // A transient exception thrown BEFORE the inner callback fires is graph-origin
            // (FalkorDB traversal failure) → GRAPH_UNAVAILABLE. After the callback fires, a transient
            // exception is redis-origin → BACKEND_UNAVAILABLE.
            bool innerSearchStarted = false;

            TenantEmbeddingConfig config = await actor.GetEmbeddingConfigAsync();
            SearchResult result;

            try
            {
                result = await graphScopedSearch.SearchAsync(
                    mainSearchQuery, startNodeId, clampedDepth,
                    q =>
                    {
                        innerSearchStarted = true;
                        return semanticService.SearchAsync(q, config, cancellationToken);
                    },
                    cancellationToken);
            }
            catch (EmbeddingApiException ex)
            {
                return Results.Json(
                    SearchEndpointErrorResponseFactory.CreateEmbeddingUnavailable(ex),
                    statusCode: 503);
            }
            catch (EmbeddingRateLimitException ex)
            {
                return Results.Json(
                    SearchEndpointErrorResponseFactory.CreateEmbeddingUnavailable(ex),
                    statusCode: 503);
            }
            catch (SemanticSearchDimensionMismatchException ex)
            {
                return Results.Json(
                    SearchEndpointErrorResponseFactory.CreateDimensionMismatch(ex),
                    statusCode: 500);
            }
            catch (RedisConnectionException ex)
            {
                return SearchEndpointDegradationResponses.BuildGraphScopedAxisFailureResponse(
                    httpContext,
                    logger,
                    "semantic",
                    tenantId,
                    startNodeId,
                    innerSearchStarted,
                    ex);
            }
            catch (RedisTimeoutException ex)
            {
                // Must precede TimeoutException (base class) — RedisTimeoutException is a multiplexer
                // command-layer timeout → 503, whereas TimeoutException from WaitAsync is the
                // graph-query deadline → 504 GRAPH_TIMEOUT.
                return SearchEndpointDegradationResponses.BuildGraphScopedAxisFailureResponse(
                    httpContext,
                    logger,
                    "semantic",
                    tenantId,
                    startNodeId,
                    innerSearchStarted,
                    ex);
            }
            catch (RedisServerException ex) when (SearchEndpointDegradationLog.IsTransientRedisError(ex))
            {
                return SearchEndpointDegradationResponses.BuildGraphScopedAxisFailureResponse(
                    httpContext,
                    logger,
                    "semantic",
                    tenantId,
                    startNodeId,
                    innerSearchStarted,
                    ex);
            }
            catch (TimeoutException)
            {
                return SearchEndpointDegradationResponses.BuildGraphTimeoutResponse();
            }

            result = await EnrichResultWithCaseAttributionAsync(result, caseService, tenantId, cancellationToken);
            result = await EnrichResultWithAnnotationCountsAsync(result, graphQueryBuilder, falkorDb, tenantId, cancellationToken);
            if (explain)
            {
                result = result with { Explanation = ExplainMetadataBuilder.BuildForSingleAxis("semantic") };
            }

            RecordSearchActivity();
            return Results.Ok(result);
        }

        bool innerSyntacticStarted = false;
        SearchResult syntacticResult;
        try
        {
            syntacticResult = await graphScopedSearch.SearchAsync(
                mainSearchQuery, startNodeId, clampedDepth,
                q =>
                {
                    innerSyntacticStarted = true;
                    return syntacticService.SearchAsync(q);
                },
                cancellationToken);
        }
        catch (RedisConnectionException ex)
        {
            return SearchEndpointDegradationResponses.BuildGraphScopedAxisFailureResponse(
                httpContext,
                logger,
                "syntactic",
                tenantId,
                startNodeId,
                innerSyntacticStarted,
                ex);
        }
        catch (RedisTimeoutException ex)
        {
            // Must precede TimeoutException (base class).
            return SearchEndpointDegradationResponses.BuildGraphScopedAxisFailureResponse(
                httpContext,
                logger,
                "syntactic",
                tenantId,
                startNodeId,
                innerSyntacticStarted,
                ex);
        }
        catch (RedisServerException ex) when (SearchEndpointDegradationLog.IsTransientRedisError(ex))
        {
            return SearchEndpointDegradationResponses.BuildGraphScopedAxisFailureResponse(
                httpContext,
                logger,
                "syntactic",
                tenantId,
                startNodeId,
                innerSyntacticStarted,
                ex);
        }
        catch (TimeoutException)
        {
            return SearchEndpointDegradationResponses.BuildGraphTimeoutResponse();
        }

        syntacticResult = await EnrichResultWithCaseAttributionAsync(syntacticResult, caseService, tenantId, cancellationToken);
        syntacticResult = await EnrichResultWithAnnotationCountsAsync(syntacticResult, graphQueryBuilder, falkorDb, tenantId, cancellationToken);
        if (explain)
        {
            syntacticResult = syntacticResult with { Explanation = ExplainMetadataBuilder.BuildForSingleAxis("syntactic") };
        }

        RecordSearchActivity();
        return Results.Ok(syntacticResult);
    }

    // --- Existing routing for syntactic/semantic without graph scope ---
    if (string.Equals(axis, "semantic", StringComparison.OrdinalIgnoreCase))
    {
        ITenantConfigurationActor actor = actorProxyFactory
            .CreateActorProxy<ITenantConfigurationActor>(
                new ActorId(tenantId), nameof(TenantConfigurationActor));

        TenantEmbeddingConfig config = await actor.GetEmbeddingConfigAsync();
        SearchResult searchResult;
        try
        {
            searchResult = await semanticService.SearchAsync(
                mainSearchQuery, config, cancellationToken);
        }
        catch (EmbeddingApiException ex)
        {
            return Results.Json(
                SearchEndpointErrorResponseFactory.CreateEmbeddingUnavailable(ex),
                statusCode: 503);
        }
        catch (EmbeddingRateLimitException ex)
        {
            return Results.Json(
                SearchEndpointErrorResponseFactory.CreateEmbeddingUnavailable(ex),
                statusCode: 503);
        }
        catch (SemanticSearchDimensionMismatchException ex)
        {
            return Results.Json(
                SearchEndpointErrorResponseFactory.CreateDimensionMismatch(ex),
                statusCode: 500);
        }
        catch (RedisConnectionException ex)
        {
            return SearchEndpointDegradationResponses.BuildBackendUnavailableResponse(httpContext, logger, "semantic", tenantId, ex);
        }
        catch (RedisTimeoutException ex)
        {
            return SearchEndpointDegradationResponses.BuildBackendUnavailableResponse(httpContext, logger, "semantic", tenantId, ex);
        }
        catch (RedisServerException ex) when (SearchEndpointDegradationLog.IsTransientRedisError(ex))
        {
            return SearchEndpointDegradationResponses.BuildBackendUnavailableResponse(httpContext, logger, "semantic", tenantId, ex);
        }

        searchResult = await EnrichResultWithCaseAttributionAsync(searchResult, caseService, tenantId, cancellationToken);
        searchResult = await EnrichResultWithAnnotationCountsAsync(searchResult, graphQueryBuilder, falkorDb, tenantId, cancellationToken);
        if (explain)
        {
            searchResult = searchResult with { Explanation = ExplainMetadataBuilder.BuildForSingleAxis("semantic") };
        }

        RecordSearchActivity();
        return Results.Ok(searchResult);
    }

    SearchResult syntacticDefault;
    try
    {
        syntacticDefault = await syntacticService.SearchAsync(mainSearchQuery);
    }
    catch (RedisConnectionException ex)
    {
        return SearchEndpointDegradationResponses.BuildBackendUnavailableResponse(httpContext, logger, "syntactic", tenantId, ex);
    }
    catch (RedisTimeoutException ex)
    {
        return SearchEndpointDegradationResponses.BuildBackendUnavailableResponse(httpContext, logger, "syntactic", tenantId, ex);
    }
    catch (RedisServerException ex) when (SearchEndpointDegradationLog.IsTransientRedisError(ex))
    {
        return SearchEndpointDegradationResponses.BuildBackendUnavailableResponse(httpContext, logger, "syntactic", tenantId, ex);
    }

    syntacticDefault = await EnrichResultWithCaseAttributionAsync(syntacticDefault, caseService, tenantId, cancellationToken);
    syntacticDefault = await EnrichResultWithAnnotationCountsAsync(syntacticDefault, graphQueryBuilder, falkorDb, tenantId, cancellationToken);
    if (explain)
    {
        syntacticDefault = syntacticDefault with { Explanation = ExplainMetadataBuilder.BuildForSingleAxis("syntactic") };
    }

    RecordSearchActivity();
    return Results.Ok(syntacticDefault);
});

app.MapGet("/api/tenants/{tenantId}/traverse", async (
    string tenantId,
    GraphTraversalService traversalService,
    ILogger<Program> logger,
    HttpContext httpContext,
    [FromQuery] string? startNodeId,
    [FromQuery] int depth = 2,
    [FromQuery] string? caseId = null,
    [FromQuery] string? edgeTypes = null,
    CancellationToken cancellationToken = default) =>
{
    if (string.IsNullOrWhiteSpace(tenantId))
    {
        return Results.BadRequest(new ErrorResponse("INVALID_TENANT_ID", "TenantId is required.", "Provide a valid tenantId."));
    }

    ErrorResponse? tenantValidationError = ValidateTenantId(tenantId);
    if (tenantValidationError is not null)
    {
        return Results.BadRequest(tenantValidationError);
    }

    if (string.IsNullOrWhiteSpace(startNodeId))
    {
        return Results.BadRequest(new ErrorResponse(
            "MISSING_START_NODE",
            "startNodeId query parameter is required.",
            "Provide startNodeId=<memoryUnitId> to specify the traversal starting point."));
    }

    // Parse edgeTypes: null/empty/whitespace means "use default semantic types".
    IReadOnlyList<EdgeType>? parsedEdgeTypes = null;
    if (!string.IsNullOrWhiteSpace(edgeTypes))
    {
        List<EdgeType> types = [];
        string[] parts = edgeTypes.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        string validTypesString = string.Join(", ", Enum.GetValues<EdgeType>().Select(e => char.ToLowerInvariant(e.ToString()[0]) + e.ToString()[1..]));
        foreach (string part in parts)
        {
            if (!Enum.TryParse<EdgeType>(part, ignoreCase: true, out EdgeType et) || !Enum.IsDefined(et))
            {
                return Results.BadRequest(new ErrorResponse(
                    "INVALID_EDGE_TYPE",
                    $"Unknown edge type: '{part}'. Valid types: {validTypesString}",
                    "Use comma-separated camelCase edge type names (not underscore format)."));
            }

            types.Add(et);
        }

        parsedEdgeTypes = types;
    }

    int clampedDepth = Math.Clamp(depth, 0, 10);
    try
    {
        TraversalResult result = await traversalService.TraverseAsync(
            tenantId, startNodeId, clampedDepth, caseId, parsedEdgeTypes, cancellationToken);
        return Results.Ok(result);
    }
    catch (RedisConnectionException ex)
    {
        return SearchEndpointDegradationResponses.BuildGraphUnavailableResponse(httpContext, logger, tenantId, startNodeId, ex);
    }
    catch (RedisTimeoutException ex)
    {
        return SearchEndpointDegradationResponses.BuildGraphUnavailableResponse(httpContext, logger, tenantId, startNodeId, ex);
    }
    catch (RedisServerException ex) when (SearchEndpointDegradationLog.IsTransientRedisError(ex))
    {
        return SearchEndpointDegradationResponses.BuildGraphUnavailableResponse(httpContext, logger, tenantId, startNodeId, ex);
    }
});

app.MapPatch("/api/tenants/{tenantId}/edges/confidence", async (
    string tenantId,
    JsonElement requestBody,
    GraphTraversalService traversalService,
    CancellationToken cancellationToken) =>
{
    ErrorResponse? tenantValidationError = ValidateTenantId(tenantId);
    if (tenantValidationError is not null)
    {
        return Results.BadRequest(tenantValidationError);
    }

    ErrorResponse? requestBodyError = TryReadConfidencePromotionRequest(requestBody, out ConfidencePromotionRequest? request);
    if (requestBodyError is not null)
    {
        return Results.BadRequest(requestBodyError);
    }

    if (string.IsNullOrWhiteSpace(request!.SourceNodeId))
    {
        return Results.BadRequest(new ErrorResponse(
            "MISSING_SOURCE_NODE",
            "sourceNodeId is required.",
            "Provide the source node ID of the edge to promote."));
    }

    if (string.IsNullOrWhiteSpace(request.TargetNodeId))
    {
        return Results.BadRequest(new ErrorResponse(
            "MISSING_TARGET_NODE",
            "targetNodeId is required.",
            "Provide the target node ID of the edge to promote."));
    }

    if (string.IsNullOrWhiteSpace(request.VerifiedBy))
    {
        return Results.BadRequest(new ErrorResponse(
            "MISSING_VERIFIED_BY",
            "verifiedBy is required.",
            "Provide the identity of the person verifying the relationship."));
    }

    if (!float.IsFinite(request.NewConfidence) || request.NewConfidence < 0f || request.NewConfidence > 1f)
    {
        return Results.BadRequest(new ErrorResponse(
            "INVALID_CONFIDENCE",
            $"Confidence must be between 0.0 and 1.0, got {request.NewConfidence}.",
            "Provide a confidence value in the range [0.0, 1.0]."));
    }

    ConfidencePromotionResult? result = await traversalService.PromoteEdgeConfidenceAsync(
        tenantId, request, cancellationToken);

    if (result is null)
    {
        return Results.NotFound(new ErrorResponse(
            "EDGE_NOT_FOUND",
            $"No {request.EdgeType} edge found from '{request.SourceNodeId}' to '{request.TargetNodeId}'.",
            "Verify the edge exists by traversing from either node. Note: edges are directed — sourceNodeId must be the relationship origin (e.g., for causedBy, the CausationId is the source)."));
    }

    return Results.Ok(result);
});

app.Run();

static IConnectionMultiplexer ConnectRequiredMultiplexer(IConfiguration configuration, string connectionName)
{
    string connectionString = configuration.GetConnectionString(connectionName)
        ?? throw new InvalidOperationException(
            $"Connection string '{connectionName}' is required. Start the server through AppHost or set ConnectionStrings__{connectionName}.");

    return ConnectionMultiplexer.Connect(connectionString);
}

static ErrorResponse? ValidateUrlIngestionRequest(UrlIngestionRequest request, UrlFetcherOptions options, out Uri? url)
{
    url = null;
    if (request is null)
    {
        return new ErrorResponse("INVALID_INPUT", "Request body is required.", "Provide a JSON body with tenantId, caseId, url, and ingestedBy.");
    }

    ErrorResponse? tenantError = ValidateTenantId(request.TenantId);
    if (tenantError is not null)
    {
        return tenantError;
    }

    if (string.IsNullOrWhiteSpace(request.CaseId))
    {
        return new ErrorResponse("INVALID_INPUT", "CaseId is required.", "Provide a non-empty caseId.");
    }

    if (string.IsNullOrWhiteSpace(request.IngestedBy))
    {
        return new ErrorResponse("INVALID_INPUT", "IngestedBy is required.", "Provide the identity of the ingesting principal.");
    }

    if (string.IsNullOrWhiteSpace(request.Url)
        || !Uri.TryCreate(request.Url, UriKind.Absolute, out Uri? parsed)
        || (parsed.Scheme is not "http" and not "https"))
    {
        return new ErrorResponse(
            "INVALID_URL",
            "URL scheme or host is not allowed.",
            "Use an http(s) URL with a publicly routable host.");
    }

    if (!UrlHostValidator.IsAllowedHost(parsed, options))
    {
        return new ErrorResponse(
            "INVALID_URL",
            "URL scheme or host is not allowed.",
            "Use an http(s) URL with a publicly routable host. Set Ingestion:UrlFetcher:AllowPrivateHosts=true in configuration to allow private hosts (development only).");
    }

    foreach ((string key, MetadataField field) in request.Metadata)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            return new ErrorResponse("INVALID_INPUT", "Metadata keys must not be empty.", "Remove empty metadata keys.");
        }

        if (float.IsNaN(field.Confidence) || float.IsInfinity(field.Confidence) || field.Confidence < 0f || field.Confidence > 1f)
        {
            return new ErrorResponse(
                "INVALID_INPUT",
                $"Metadata field '{key}' confidence must be between 0.0 and 1.0.",
                "Adjust metadata confidence to a value between 0 and 1.");
        }
    }

    url = parsed;
    return null;
}

static ErrorResponse? ValidateDirectoryIngestionRequest(DirectoryIngestionRequest request)
{
    if (request is null)
    {
        return new ErrorResponse("INVALID_INPUT", "Request body is required.", "Provide a JSON body with tenantId, caseId, directoryPath, and ingestedBy.");
    }

    ErrorResponse? tenantError = ValidateTenantId(request.TenantId);
    if (tenantError is not null)
    {
        return tenantError;
    }

    if (string.IsNullOrWhiteSpace(request.CaseId))
    {
        return new ErrorResponse("INVALID_INPUT", "CaseId is required.", "Provide a non-empty caseId.");
    }

    if (string.IsNullOrWhiteSpace(request.IngestedBy))
    {
        return new ErrorResponse("INVALID_INPUT", "IngestedBy is required.", "Provide the identity of the ingesting principal.");
    }

    if (string.IsNullOrWhiteSpace(request.DirectoryPath))
    {
        return new ErrorResponse("INVALID_INPUT", "DirectoryPath is required.", "Provide an absolute directory path under a configured root.");
    }

    foreach ((string key, MetadataField field) in request.Metadata)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            return new ErrorResponse("INVALID_INPUT", "Metadata keys must not be empty.", "Remove empty metadata keys.");
        }

        if (float.IsNaN(field.Confidence) || float.IsInfinity(field.Confidence) || field.Confidence < 0f || field.Confidence > 1f)
        {
            return new ErrorResponse(
                "INVALID_INPUT",
                $"Metadata field '{key}' confidence must be between 0.0 and 1.0.",
                "Adjust metadata confidence to a value between 0 and 1.");
        }
    }

    return null;
}

static ErrorResponse? ValidateIngestionRequest(IngestionInput input)
{
    try
    {
        IngestionInputValidator.Validate(input);
        return null;
    }
    catch (ArgumentException ex)
    {
        return new ErrorResponse(
            "INVALID_INPUT",
            ex.Message,
            "Ensure the ingestion request is valid before scheduling ingestion.");
    }
}

static ErrorResponse? ValidateTenantId(string tenantId)
{
    try
    {
        TenantIdGuard.Validate(tenantId);
        return null;
    }
    catch (ArgumentException ex)
    {
        return new ErrorResponse(
            "INVALID_TENANT_ID",
            ex.Message,
            "Use only alphanumeric characters and hyphens for tenant identifiers.");
    }
}

static ErrorResponse? TryReadConfidencePromotionRequest(
    JsonElement requestBody,
    out ConfidencePromotionRequest? request)
{
    request = null;

    if (requestBody.ValueKind != JsonValueKind.Object)
    {
        return new ErrorResponse(
            "INVALID_REQUEST_BODY",
            "Request body must be a JSON object.",
            "Provide a valid confidence promotion request payload.");
    }

    if (!requestBody.TryGetProperty("edgeType", out _))
    {
        return new ErrorResponse(
            "MISSING_EDGE_TYPE",
            "edgeType is required.",
            "Provide the relationship type of the edge to promote.");
    }

    if (!requestBody.TryGetProperty("newConfidence", out _))
    {
        return new ErrorResponse(
            "MISSING_NEW_CONFIDENCE",
            "newConfidence is required.",
            "Provide the new confidence value in the range [0.0, 1.0].");
    }

    try
    {
        request = JsonSerializer.Deserialize<ConfidencePromotionRequest>(requestBody.GetRawText(), MemoriesJsonContext.Options);
    }
    catch (JsonException ex)
    {
        return new ErrorResponse(
            "INVALID_REQUEST_BODY",
            ex.Message,
            "Provide a valid confidence promotion request payload.");
    }

    return request is null
        ? new ErrorResponse(
            "INVALID_REQUEST_BODY",
            "Request body could not be deserialized.",
            "Provide a valid confidence promotion request payload.")
        : null;
}

static ErrorResponse? TryDeserializeAddCaseMemberInput(JsonElement requestBody, out AddCaseMemberInput? input)
{
    input = null;

    if (requestBody.ValueKind != JsonValueKind.Object)
    {
        return new ErrorResponse(
            "INVALID_MEMBER_INPUT",
            "Request body must be a JSON object.",
            "Provide a JSON object with memberType set to 'user' or 'role'.");
    }

    if (!TryGetJsonPropertyIgnoreCase(requestBody, "memberType", out JsonElement memberTypeElement) ||
        memberTypeElement.ValueKind != JsonValueKind.String ||
        string.IsNullOrWhiteSpace(memberTypeElement.GetString()))
    {
        return new ErrorResponse(
            "INVALID_MEMBER_TYPE",
            "MemberType is required.",
            "Provide memberType as 'user' or 'role'.");
    }

    try
    {
        input = JsonSerializer.Deserialize<AddCaseMemberInput>(requestBody.GetRawText(), MemoriesJsonContext.Options);
    }
    catch (JsonException ex)
    {
        return new ErrorResponse(
            "INVALID_MEMBER_TYPE",
            ex.Message,
            "Provide memberType as 'user' or 'role'.");
    }

    return input is null
        ? new ErrorResponse(
            "INVALID_MEMBER_INPUT",
            "Request body is required.",
            "Provide a JSON object with memberType set to 'user' or 'role'.")
        : null;
}

static bool TryGetJsonPropertyIgnoreCase(JsonElement element, string propertyName, out JsonElement value)
{
    if (element.TryGetProperty(propertyName, out value))
    {
        return true;
    }

    foreach (JsonProperty property in element.EnumerateObject())
    {
        if (string.Equals(property.Name, propertyName, StringComparison.OrdinalIgnoreCase))
        {
            value = property.Value;
            return true;
        }
    }

    value = default;
    return false;
}

static IReadOnlySet<string> DetermineHybridExplanationAxes(
    IReadOnlySet<string> requestedAxes,
    IReadOnlyCollection<string> unavailableAxes,
    bool hasSemanticConfiguration,
    bool hasGraphStartNode)
{
    ArgumentNullException.ThrowIfNull(requestedAxes);
    ArgumentNullException.ThrowIfNull(unavailableAxes);

    HashSet<string> explanationAxes = new(requestedAxes, StringComparer.OrdinalIgnoreCase);

    if (!hasSemanticConfiguration)
    {
        _ = explanationAxes.Remove("semantic");
    }

    if (!hasGraphStartNode)
    {
        _ = explanationAxes.Remove("graph");
    }

    foreach (string unavailableAxis in unavailableAxes)
    {
        _ = explanationAxes.Remove(unavailableAxis);
    }

    return explanationAxes;
}
static object CreateEmbeddingConfigConflictResponse(
    string tenantId,
    TenantEmbeddingConfig currentConfig,
    TenantEmbeddingConfig proposedConfig,
    string[] affectedFields)
{
    EmbeddingConfigChangeException exception = new(
        tenantId,
        currentConfig,
        proposedConfig,
        affectedFields);

    return new
    {
        error = "EmbeddingConfigChangeRequired",
        message = exception.Message,
        currentConfig,
        proposedConfig,
        affectedFields,
    };
}

static async Task<SearchResult> EnrichResultWithCaseAttributionAsync(
    SearchResult result,
    CaseService caseService,
    string tenantId,
    CancellationToken cancellationToken)
{
    if (result.Results.Count == 0)
    {
        return result;
    }

    List<string> caseIds = result.Results
        .Where(r => r.CaseId is not null)
        .Select(r => r.CaseId!)
        .Distinct()
        .ToList();

    if (caseIds.Count == 0)
    {
        return result;
    }

    Dictionary<string, string> caseNames = await caseService.ResolveNamesAsync(tenantId, caseIds, cancellationToken);

    List<ScoredResult> enrichedResults = result.Results
        .Select(r => r.CaseId is not null && caseNames.TryGetValue(r.CaseId, out string? name)
            ? r with { CaseName = name }
            : r)
        .ToList();

    List<CaseGroupSummary> caseGroups = BuildCaseGroups(enrichedResults, caseNames);

    return result with
    {
        Results = enrichedResults,
        CaseGroups = caseGroups.Count > 0 ? caseGroups : null,
    };
}

static async Task<HybridSearchResult> EnrichHybridResultWithCaseAttributionAsync(
    HybridSearchResult result,
    CaseService caseService,
    string tenantId,
    CancellationToken cancellationToken)
{
    if (result.Results.Count == 0)
    {
        return result;
    }

    List<string> caseIds = result.Results
        .Where(r => r.CaseId is not null)
        .Select(r => r.CaseId!)
        .Distinct()
        .ToList();

    if (caseIds.Count == 0)
    {
        return result;
    }

    Dictionary<string, string> caseNames = await caseService.ResolveNamesAsync(tenantId, caseIds, cancellationToken);

    List<FusedScoredResult> enrichedResults = result.Results
        .Select(r => r.CaseId is not null && caseNames.TryGetValue(r.CaseId, out string? name)
            ? r with { CaseName = name }
            : r)
        .ToList();

    List<CaseGroupSummary> caseGroups = enrichedResults
        .Where(r => r.CaseId is not null)
        .GroupBy(r => r.CaseId!)
        .Select(g => new CaseGroupSummary(g.Key, caseNames.GetValueOrDefault(g.Key, g.Key), g.Count()))
        .OrderByDescending(c => c.ResultCount)
        .ToList();

    return result with
    {
        Results = enrichedResults,
        CaseGroups = caseGroups.Count > 0 ? caseGroups : null,
    };
}

static async Task<SearchResult> EnrichResultWithAnnotationCountsAsync(
    SearchResult result,
    IGraphQueryBuilder graphQueryBuilder,
    IConnectionMultiplexer falkorDb,
    string tenantId,
    CancellationToken cancellationToken)
{
    if (result.Results.Count == 0)
    {
        return result;
    }

    List<string> muIds = result.Results.Select(r => r.MemoryUnitId).Distinct().ToList();
    if (muIds.Count == 0)
    {
        return result;
    }

    Dictionary<string, int> counts = await LoadAnnotationCountsAsync(
        graphQueryBuilder,
        falkorDb,
        tenantId,
        muIds,
        cancellationToken).ConfigureAwait(false);

    if (counts.Count == 0)
    {
        return result;
    }

    List<ScoredResult> enriched = result.Results
        .Select(r => counts.TryGetValue(r.MemoryUnitId, out int count) ? r with { AnnotationsCount = count } : r)
        .ToList();

    return result with { Results = enriched };
}

static async Task<HybridSearchResult> EnrichHybridResultWithAnnotationCountsAsync(
    HybridSearchResult result,
    IGraphQueryBuilder graphQueryBuilder,
    IConnectionMultiplexer falkorDb,
    string tenantId,
    CancellationToken cancellationToken)
{
    if (result.Results.Count == 0)
    {
        return result;
    }

    List<string> muIds = result.Results.Select(r => r.MemoryUnitId).Distinct().ToList();
    if (muIds.Count == 0)
    {
        return result;
    }

    Dictionary<string, int> counts = await LoadAnnotationCountsAsync(
        graphQueryBuilder,
        falkorDb,
        tenantId,
        muIds,
        cancellationToken).ConfigureAwait(false);

    if (counts.Count == 0)
    {
        return result;
    }

    List<FusedScoredResult> enriched = result.Results
        .Select(r => counts.TryGetValue(r.MemoryUnitId, out int count) ? r with { AnnotationsCount = count } : r)
        .ToList();

    return result with { Results = enriched };
}

static async Task<Dictionary<string, int>> LoadAnnotationCountsAsync(
    IGraphQueryBuilder graphQueryBuilder,
    IConnectionMultiplexer falkorDb,
    string tenantId,
    IReadOnlyList<string> memoryUnitIds,
    CancellationToken cancellationToken)
{
    if (memoryUnitIds.Count == 0)
    {
        return [];
    }

    try
    {
        (string query, IDictionary<string, object> parameters) = graphQueryBuilder.BuildBatchCountAnnotations(memoryUnitIds);
        NFalkorDB.FalkorDB falkor = new(falkorDb.GetDatabase());
        NFalkorDB.ResultSet countResult = await falkor.QueryAsync(tenantId, query, parameters)
            .WaitAsync(TimeSpan.FromSeconds(10), cancellationToken)
            .ConfigureAwait(false);

        Dictionary<string, int> counts = [];
        foreach (NFalkorDB.Record record in countResult)
        {
            if (!TryReadAnnotationCount(record, out string? memoryUnitId, out int count) ||
                string.IsNullOrWhiteSpace(memoryUnitId) ||
                count <= 0)
            {
                continue;
            }

            counts[memoryUnitId] = count;
        }

        return counts;
    }
    catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
    {
        throw;
    }
    catch
    {
        return [];
    }
}

static bool TryReadAnnotationCount(NFalkorDB.Record record, out string? memoryUnitId, out int count)
{
    memoryUnitId = null;
    count = 0;

    try
    {
        memoryUnitId = record.GetValue<string>("muId");
        long parsedCount = record.GetValue<long>("count");
        count = checked((int)parsedCount);
        return !string.IsNullOrWhiteSpace(memoryUnitId);
    }
    catch
    {
        return false;
    }
}

static List<CaseGroupSummary> BuildCaseGroups(
    IReadOnlyList<ScoredResult> results, Dictionary<string, string> caseNames)
{
    return results
        .Where(r => r.CaseId is not null)
        .GroupBy(r => r.CaseId!)
        .Select(g => new CaseGroupSummary(g.Key, caseNames.GetValueOrDefault(g.Key, g.Key), g.Count()))
        .OrderByDescending(c => c.ResultCount)
        .ToList();
}
