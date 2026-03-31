using Dapr.Actors;
using Dapr.Actors.Client;
using Dapr.Client;
using Dapr.Workflow;

using Hexalith.Memories.Contracts.V1;
using Hexalith.Memories.Server.Activities.Indexing;
using Hexalith.Memories.Server.Activities.Ingestion;
using Hexalith.Memories.Server.Actors;
using Hexalith.Memories.Server.Graph;
using Hexalith.Memories.Server.HealthChecks;
using Hexalith.Memories.Server.Ingestion;
using Hexalith.Memories.Server.Search;
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

builder.Services.AddKeyedSingleton<IConnectionMultiplexer>("redis", (sp, _) =>
    ConnectRequiredMultiplexer(builder.Configuration, "redis"));
builder.Services.AddKeyedSingleton<IConnectionMultiplexer>("falkordb", (sp, _) =>
    ConnectRequiredMultiplexer(builder.Configuration, "falkordb"));
builder.Services.AddSingleton<IGraphQueryBuilder, GraphQueryBuilder>();
builder.Services.AddSingleton<SyntacticSearchService>(sp =>
    new SyntacticSearchService(
        sp.GetRequiredKeyedService<IConnectionMultiplexer>("redis"),
        sp.GetRequiredService<ILogger<SyntacticSearchService>>()));

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
});

builder.Services.AddActors(options =>
{
    options.Actors.RegisterActor<EmbeddingRateLimiterActor>();
    options.Actors.RegisterActor<TenantConfigurationActor>();
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

app.MapPost("/api/ingest", async (DaprWorkflowClient workflowClient, IngestionInput input) =>
{
    ErrorResponse? validationError = ValidateIngestionRequest(input);
    if (validationError is not null)
    {
        return Results.BadRequest(validationError);
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

app.MapGet("/api/tenants/{tenantId}/embedding-config", async (IActorProxyFactory actorProxyFactory, string tenantId) =>
{
    ErrorResponse? tenantValidationError = ValidateTenantId(tenantId);
    if (tenantValidationError is not null)
    {
        return Results.BadRequest(tenantValidationError);
    }

    ITenantConfigurationActor actor = actorProxyFactory
        .CreateActorProxy<ITenantConfigurationActor>(new ActorId(tenantId), nameof(TenantConfigurationActor));
    TenantEmbeddingConfig config = await actor.GetEmbeddingConfigAsync();
    return Results.Ok(config);
});

app.MapPut("/api/tenants/{tenantId}/embedding-config",
    async (IActorProxyFactory actorProxyFactory, string tenantId, TenantEmbeddingConfig config, bool forceReindex = false) =>
{
    ErrorResponse? tenantValidationError = ValidateTenantId(tenantId);
    if (tenantValidationError is not null)
    {
        return Results.BadRequest(tenantValidationError);
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

app.MapGet("/api/search", async (
    SyntacticSearchService searchService,
    [FromQuery] string tenantId,
    [FromQuery] string query,
    [FromQuery] string? caseId,
    [FromQuery] int maxResults = 10,
    [FromQuery] int offset = 0) =>
{
    if (string.IsNullOrWhiteSpace(tenantId) || string.IsNullOrWhiteSpace(query))
    {
        return Results.BadRequest(new ErrorResponse(
            "INVALID_INPUT",
            "Both 'tenantId' and 'query' are required.",
            "Provide tenantId and query as query parameters."));
    }

    ErrorResponse? tenantValidationError = ValidateTenantId(tenantId);
    if (tenantValidationError is not null)
    {
        return Results.BadRequest(tenantValidationError);
    }

    int clampedMaxResults = Math.Clamp(maxResults, 1, 100);
    int clampedOffset = Math.Max(offset, 0);

    SearchResult searchResult = await searchService.SearchAsync(
        new SearchQuery
        {
            TenantId = tenantId,
            Query = query,
            CaseId = caseId,
            MaxResults = clampedMaxResults,
            Offset = clampedOffset,
        });

    return Results.Ok(searchResult);
});

app.Run();

static IConnectionMultiplexer ConnectRequiredMultiplexer(IConfiguration configuration, string connectionName)
{
    string connectionString = configuration.GetConnectionString(connectionName)
        ?? throw new InvalidOperationException(
            $"Connection string '{connectionName}' is required. Start the server through AppHost or set ConnectionStrings__{connectionName}.");

    return ConnectionMultiplexer.Connect(connectionString);
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
