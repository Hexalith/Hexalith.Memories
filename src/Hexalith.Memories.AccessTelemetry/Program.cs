using Dapr.Actors;
using Dapr.Actors.Client;
using Dapr.Client;

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
var runtimeGate = new AccessTelemetryRuntimeGate();
AccessTelemetryOptionsValidationResult initialValidation = AccessTelemetryOptionsValidator.Validate(
    configuredOptions,
    builder.Environment.EnvironmentName);
bool awaitsDaprRetention = configuredOptions.Enabled &&
    configuredOptions.RetentionSource == RetentionConfigurationSource.DaprConfiguration &&
    configuredOptions.Retention is null;
if (configuredOptions.Enabled && !initialValidation.IsValid && !awaitsDaprRetention)
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
builder.Services.AddSingleton<MonotonicRecordIdGenerator>();
builder.Services.AddSingleton<IAccessTelemetryStateStore, DaprAccessTelemetryStateStore>();
builder.Services.AddSingleton<AccessTelemetryProcessorStatus>();
builder.Services.AddTransient<AccessTelemetryLifecycleProcessor>();
builder.Services.AddSingleton(configuredOptions);
builder.Services.AddSingleton<AccessTelemetryRuntimeOptionsProvider>();
builder.Services.AddSingleton(runtimeGate);
builder.Services.AddSingleton<IAccessTelemetryRuntimeGate>(services => services.GetRequiredService<AccessTelemetryRuntimeGate>());
builder.Services.AddSingleton(builder.Configuration
    .GetSection($"{AccessTelemetryOptions.SectionName}:CapabilityEvidence")
    .Get<AccessTelemetryCapabilityEvidenceOptions>() ?? new AccessTelemetryCapabilityEvidenceOptions());
builder.Services.AddSingleton<AccessTelemetryCapabilityProbeRunner>();
foreach (string capability in AccessTelemetryCapabilityProbeRunner.RequiredCapabilities)
{
    builder.Services.AddSingleton<IAccessTelemetryCapabilityProbe>(services => new DaprAccessTelemetryCapabilityProbe(
        capability,
        services.GetRequiredService<DaprClient>(),
        services.GetRequiredService<AccessTelemetryRuntimeOptionsProvider>(),
        services.GetRequiredService<MonotonicRecordIdGenerator>(),
        services.GetRequiredService<TimeProvider>(),
        services.GetRequiredService<ILogger<DaprAccessTelemetryCapabilityProbe>>()));
}
if (configuredOptions.Enabled)
{
    builder.Services.AddHostedService<AccessTelemetryLifecycleConfigurationHostedService>();
    builder.Services.AddHostedService<AccessTelemetryCapabilityProbeHostedService>();
}

_ = builder.Services.AddHealthChecks().AddCheck<AccessTelemetryRuntimeHealthCheck>(
    "access-telemetry-lifecycle",
    failureStatus: HealthStatus.Unhealthy,
    tags: ["ready"],
    timeout: TimeSpan.FromSeconds(3));
builder.Services.AddSingleton<IAccessTelemetryClockGate>(services =>
    new AccessTelemetryClockGate(
        services.GetRequiredService<AccessTelemetryRuntimeOptionsProvider>(),
        services.GetRequiredService<TimeProvider>()));
builder.Services.AddSingleton<ILifecycleClockEvidenceProvider, DaprLifecycleClockEvidenceProvider>();

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
    WriterHeartbeatRequest request,
    IActorProxyFactory proxyFactory) =>
{
    IAccessTelemetryLifecycleActor actor = proxyFactory.CreateActorProxy<IAccessTelemetryLifecycleActor>(
        new ActorId("global"),
        nameof(AccessTelemetryLifecycleActor));
    WriterHeartbeatResponse response = await actor.HeartbeatAsync(request).ConfigureAwait(false);
    return response.Accepted ? Results.Ok(response) : Results.BadRequest(response);
}).AllowAnonymous();

app.MapPost("/v1/access-telemetry/validate", (
    AccessTelemetryRuntimeValidationRequest request,
    IAccessTelemetryRuntimeGate gate,
    AccessTelemetryRuntimeOptionsProvider optionsProvider) =>
{
    AccessTelemetryOptions current = optionsProvider.Current;
    AccessTelemetryCapabilityGateResult decision = gate.Current;
    bool exact = optionsProvider.IsReady &&
        string.Equals(request.ConfigurationEpoch, current.ConfigurationEpoch, StringComparison.Ordinal) &&
        string.Equals(request.ComponentProfileHash, current.ComponentProfileHash, StringComparison.Ordinal);
    var response = new AccessTelemetryRuntimeValidationResponse(
        exact && decision.AllowsWrites,
        exact ? decision.Reason : AccessTelemetryReason.ConfigurationInvalid);
    return Results.Ok(response);
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
