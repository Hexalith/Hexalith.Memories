using Dapr.Workflow;

using Hexalith.Memories.ServiceDefaults;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.Services.AddDaprClient();

// Test: Register DAPR Workflow with empty options.
// If this throws at runtime with zero workflows, defer to Story 1.3.
builder.Services.AddDaprWorkflow(options => { });

// Test: Register DAPR Actors with configuration.
// If this throws at runtime with zero actors, defer to Story 1.4.
builder.Services.AddActors(options =>
{
    options.ActorIdleTimeout = TimeSpan.FromMinutes(60);
    options.ActorScanInterval = TimeSpan.FromSeconds(30);
    options.ReentrancyConfig = new Dapr.Actors.ActorReentrancyConfig { Enabled = false };
});

WebApplication app = builder.Build();

app.MapDefaultEndpoints();
app.MapActorsHandlers();

app.Run();
