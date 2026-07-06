using Dapr.AspNetCore;
using Dapr.Actors;

using Hexalith.Memories.Server.Authentication;
using Hexalith.Memories.Server.Endpoints;
using Hexalith.Memories.Server.Hosting;
using Hexalith.Memories.Server.Ingestion;
using Hexalith.Memories.Server.NaturalLanguage;
using Hexalith.Memories.Server.Telemetry;
using Hexalith.Memories.ServiceDefaults;
using Hexalith.Memories.Telemetry;

using Microsoft.Extensions.Options;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.AddMemoriesServerServices();

WebApplication app = builder.Build();

RetryPolicyBuilder.Initialize(app.Services.GetRequiredService<IOptions<IngestionSettings>>().Value);

// Story 9.2 Task 5.7: publish the NL options snapshot so IngestionWorkflow can read
// PersistInMetadata without constructor injection (DAPR activates workflows via new()).
NaturalLanguageDescriptionOptionsSnapshot.Initialize(
    app.Services.GetRequiredService<IOptions<NaturalLanguageDescriptionOptions>>());

app.MapDefaultEndpoints();
// Dapr actor runtime endpoints are sidecar-facing infrastructure routes, not API routes.
// ServerEndpointAuthorizationTests guards this anonymous exception against broad route drift.
app.MapActorsHandlers().AllowAnonymous();

app.UseExceptionHandler();
// Story 9.1: DAPR pub/sub subscription middleware order. UseCloudEvents() is a no-op for plain-JSON
// requests (guards the /api/ingest POST from accidental envelope unwrapping). EventStore now supplies
// environment-backed topic metadata on the controller action, so the canonical MapSubscribeHandler()
// route emits the resolved topic without a handwritten /dapr/subscribe endpoint.
app.UseMiddleware<Hexalith.Memories.EventStore.CloudEventEnvelopeCaptureMiddleware>();
app.UseCloudEvents();
app.UseAuthentication();
app.UseAuthorization();
app.UseMiddleware<TenantAuthorizationMiddleware>();
app.UseRateLimiter();
app.MapControllers();
app.MapSubscribeHandler().AllowAnonymous();

TelemetrySnapshotCache telemetrySnapshotCache = app.Services.GetRequiredService<TelemetrySnapshotCache>();
MemoriesMeter.EnsureObservableGaugesCreated(
    telemetrySnapshotCache.GetIndexSizeMeasurements,
    telemetrySnapshotCache.GetQueueDepthMeasurements,
    telemetrySnapshotCache.GetNaturalLanguageEmbeddingQueueDepthMeasurements,
    telemetrySnapshotCache.GetNaturalLanguageEmbeddingQueueBytesMeasurements);

// Story 9.3 — handler registry per-tenant count gauge. Reads the singleton routing options directly
// (no Redis round-trip) — the gauge reports HOW MANY sources are registered per tenant, which is a
// pure function of TenantEventRoutingOptions.SourceToTenantMap.
IOptionsMonitor<Hexalith.Memories.EventStore.TenantEventRoutingOptions> handlerRoutingOptions =
    app.Services.GetRequiredService<IOptionsMonitor<Hexalith.Memories.EventStore.TenantEventRoutingOptions>>();
MemoriesMeter.EnsureHandlerGaugeCreated(() =>
{
    Hexalith.Memories.EventStore.TenantEventRoutingOptions options = handlerRoutingOptions.CurrentValue;
    return options.SourceToTenantMap
        .GroupBy(kvp => kvp.Value, StringComparer.Ordinal)
        .Select(g => new System.Diagnostics.Metrics.Measurement<int>(
            g.Count(),
            new KeyValuePair<string, object?>("tenant_id", g.Key)))
        .ToList();
});

app.MapIngestionEndpoints();
app.MapTenantLifecycleEndpoints();
app.MapExportEndpoints();
app.MapConsistencyEndpoints();
app.MapCasesEndpoints();
app.MapSearchEndpoints();
app.MapGraphEndpoints();

app.Run();

// Story 7.5 Rev 1.3 (Task 11.1/11.2): partial Program sentinel enables
// WebApplicationFactory<Program> to reference the top-level-statement program
// class by name from the Server.Tests project (InternalsVisibleTo is already
// granted on Server.csproj). Do NOT add members here — keep it empty.
#pragma warning disable SA1649, SA1402 // file-name-match + one-type-per-file: top-level statement convention
public partial class Program { }
#pragma warning restore SA1649, SA1402
