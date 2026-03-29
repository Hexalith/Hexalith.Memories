using Dapr.Client;
using Dapr.Workflow;

using Hexalith.Memories.Server.Activities.Ingestion;
using Hexalith.Memories.Server.Actors;
using Hexalith.Memories.Server.HealthChecks;
using Hexalith.Memories.Server.Ingestion;
using Hexalith.Memories.ServiceDefaults;

using Microsoft.Extensions.Diagnostics.HealthChecks;

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
builder.Services.AddDaprWorkflow(options =>
{
    options.RegisterActivity<ExtractContentActivity>();
    options.RegisterActivity<GenerateEmbeddingActivity>();
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
