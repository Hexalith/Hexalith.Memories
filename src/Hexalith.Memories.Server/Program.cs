using Dapr.Client;
using Dapr.Workflow;

using Hexalith.Memories.Server.Activities.Indexing;
using Hexalith.Memories.Server.Activities.Ingestion;
using Hexalith.Memories.Server.Actors;
using Hexalith.Memories.Server.Graph;
using Hexalith.Memories.Server.HealthChecks;
using Hexalith.Memories.Server.Ingestion;
using Hexalith.Memories.ServiceDefaults;

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
builder.Services.AddHttpClient<EmbeddingClient>(client =>
{
    client.Timeout = TimeSpan.FromSeconds(30);
});

builder.Services.AddKeyedSingleton<IConnectionMultiplexer>("redis", (sp, _) =>
    ConnectRequiredMultiplexer(builder.Configuration, "redis"));
builder.Services.AddKeyedSingleton<IConnectionMultiplexer>("falkordb", (sp, _) =>
    ConnectRequiredMultiplexer(builder.Configuration, "falkordb"));
builder.Services.AddSingleton<IGraphQueryBuilder, GraphQueryBuilder>();

builder.Services.AddDaprWorkflow(options =>
{
    options.RegisterActivity<ExtractContentActivity>();
    options.RegisterActivity<GenerateEmbeddingActivity>();
    options.RegisterActivity<IndexSyntacticActivity>();
    options.RegisterActivity<IndexSemanticActivity>();
    options.RegisterActivity<IndexGraphActivity>();
});

builder.Services.AddActors(options =>
{
    options.Actors.RegisterActor<EmbeddingRateLimiterActor>();
    options.ActorIdleTimeout = TimeSpan.FromMinutes(60);
    options.ActorScanInterval = TimeSpan.FromSeconds(30);
    options.ReentrancyConfig = new Dapr.Actors.ActorReentrancyConfig { Enabled = false };
});

WebApplication app = builder.Build();

app.MapDefaultEndpoints();
app.MapActorsHandlers();

app.Run();

static IConnectionMultiplexer ConnectRequiredMultiplexer(IConfiguration configuration, string connectionName)
{
    string connectionString = configuration.GetConnectionString(connectionName)
        ?? throw new InvalidOperationException(
            $"Connection string '{connectionName}' is required. Start the server through AppHost or set ConnectionStrings__{connectionName}.");

    return ConnectionMultiplexer.Connect(connectionString);
}
