using Dapr.Actors;
using Dapr.Actors.Client;

using Hexalith.Memories.AccessTelemetry.Contracts;
using Hexalith.Memories.AccessTelemetry.Capability;
using Hexalith.Memories.AccessTelemetry.Lifecycle;
using Hexalith.Memories.ServiceDefaults;
using Hexalith.Memories.ServiceDefaults.Security;

using Microsoft.Extensions.Diagnostics.HealthChecks;

using OpenTelemetry.Metrics;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults(configureRedisInstrumentation: false);
AccessTelemetryOptions configuredOptions = builder.Configuration
    .GetSection(AccessTelemetryOptions.SectionName)
    .Get<AccessTelemetryOptions>() ?? new AccessTelemetryOptions();
AccessTelemetryOptionsValidationResult optionsValidation = AccessTelemetryOptionsValidator.Validate(
    configuredOptions,
    builder.Environment.EnvironmentName);
AccessTelemetryOptions runtimeOptions = optionsValidation.EffectiveRetention is null
    ? configuredOptions
    : configuredOptions with { Retention = optionsValidation.EffectiveRetention };
var runtimeGate = new AccessTelemetryRuntimeGate();
if (runtimeOptions.Enabled && !optionsValidation.IsValid)
{
    runtimeGate.Publish(new AccessTelemetryCapabilityGateResult(
        false,
        true,
        AccessTelemetryHealthState.Unhealthy,
        AccessTelemetryReason.ConfigurationInvalid));
}

builder.Services.AddDaprClient();
_ = builder.Services.AddOpenTelemetry().WithMetrics(metrics =>
    metrics.AddMeter(Hexalith.Memories.AccessTelemetry.Observability.AccessTelemetryLifecycleMetrics.MeterName));
builder.Services.AddActors(options => options.Actors.RegisterActor<AccessTelemetryLifecycleActor>());
builder.Services.AddSingleton<TimeProvider>(TimeProvider.System);
builder.Services.AddSingleton<IAccessTelemetryStateStore, DaprAccessTelemetryStateStore>();
builder.Services.AddTransient<AccessTelemetryLifecycleProcessor>();
builder.Services.AddSingleton(runtimeOptions);
builder.Services.AddSingleton(runtimeGate);
builder.Services.AddSingleton<IAccessTelemetryRuntimeGate>(services => services.GetRequiredService<AccessTelemetryRuntimeGate>());
builder.Services.AddSingleton(builder.Configuration
    .GetSection($"{AccessTelemetryOptions.SectionName}:CapabilityEvidence")
    .Get<AccessTelemetryCapabilityEvidenceOptions>() ?? new AccessTelemetryCapabilityEvidenceOptions());
builder.Services.AddSingleton<AccessTelemetryCapabilityProbeRunner>();
if (runtimeOptions.Enabled && optionsValidation.IsValid)
{
    builder.Services.AddHostedService<AccessTelemetryCapabilityProbeHostedService>();
}

_ = builder.Services.AddHealthChecks().AddCheck<AccessTelemetryRuntimeHealthCheck>(
    "access-telemetry-lifecycle",
    failureStatus: HealthStatus.Unhealthy,
    tags: ["ready"],
    timeout: TimeSpan.FromSeconds(3));
builder.Services.AddSingleton<IAccessTelemetryClockGate>(services =>
{
    string encodedKey = builder.Configuration["AccessTelemetryLifecycle:AttestationVerificationKey"] ?? string.Empty;
    byte[] key = string.IsNullOrWhiteSpace(encodedKey) ? [] : Convert.FromBase64String(encodedKey);
    return new AccessTelemetryClockGate(
        builder.Configuration["AccessTelemetryLifecycle:DeploymentId"] ?? "unconfigured",
        builder.Configuration["AccessTelemetryLifecycle:ComponentProfileHash"] ?? "unconfigured",
        key,
        services.GetRequiredService<TimeProvider>());
});

WebApplication app = builder.Build();

app.UseMiddleware<DaprApplicationTokenMiddleware>();
app.MapDefaultEndpoints();
app.MapActorsHandlers().AllowAnonymous();

app.MapPost("/v1/access-telemetry/write", async (
    AccessTelemetryWriteBatchRequest request,
    IActorProxyFactory proxyFactory) =>
{
    IAccessTelemetryLifecycleActor actor = proxyFactory.CreateActorProxy<IAccessTelemetryLifecycleActor>(
        new ActorId("global"),
        nameof(AccessTelemetryLifecycleActor));
    AccessTelemetryWriteBatchResponse response = await actor.WriteBatchAsync(request).ConfigureAwait(false);
    return response.Rejected == 0 ? Results.Ok(response) : Results.BadRequest(response);
}).AllowAnonymous();

app.MapPost("/v1/access-telemetry/heartbeat", async (
    WriterHeartbeat heartbeat,
    IActorProxyFactory proxyFactory) =>
{
    IAccessTelemetryLifecycleActor actor = proxyFactory.CreateActorProxy<IAccessTelemetryLifecycleActor>(
        new ActorId("global"),
        nameof(AccessTelemetryLifecycleActor));
    await actor.HeartbeatAsync(heartbeat).ConfigureAwait(false);
    return Results.NoContent();
}).AllowAnonymous();

app.MapGet("/v1/access-telemetry/inspect", async (IActorProxyFactory proxyFactory) =>
{
    IAccessTelemetryLifecycleActor actor = proxyFactory.CreateActorProxy<IAccessTelemetryLifecycleActor>(
        new ActorId("global"),
        nameof(AccessTelemetryLifecycleActor));
    return Results.Ok(await actor.InspectAsync().ConfigureAwait(false));
}).AllowAnonymous();

app.Run();

internal partial class Program;
