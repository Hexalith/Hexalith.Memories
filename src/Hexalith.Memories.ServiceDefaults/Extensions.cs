using Hexalith.Memories.ServiceDefaults.Health;
using Hexalith.Memories.Telemetry;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;

namespace Hexalith.Memories.ServiceDefaults;

public static class Extensions
{

    public static TBuilder AddServiceDefaults<TBuilder>(this TBuilder builder)
        where TBuilder : IHostApplicationBuilder
    {
        _ = builder.ConfigureOpenTelemetry();
        _ = builder.AddDefaultHealthChecks();
        _ = builder.Services.AddServiceDiscovery();

        _ = builder.Services.ConfigureHttpClientDefaults(http =>
        {
            _ = http.AddStandardResilienceHandler();
            _ = http.AddServiceDiscovery();
        });

        return builder;
    }

    public static TBuilder ConfigureOpenTelemetry<TBuilder>(this TBuilder builder)
        where TBuilder : IHostApplicationBuilder
    {
        _ = builder.Logging.AddOpenTelemetry(logging =>
        {
            logging.IncludeFormattedMessage = true;
            logging.IncludeScopes = true;
        });

        _ = builder.Logging.AddJsonConsole(options => options.UseUtcTimestamp = true);

        _ = builder.Services.AddOpenTelemetry()
            .WithMetrics(metrics => metrics
                .AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation()
                .AddRuntimeInstrumentation()
                .AddMeter(MemoriesMeter.Name))
            .WithTracing(tracing => tracing
                .AddSource(builder.Environment.ApplicationName)
                .AddSource(MemoriesActivitySource.SourceName)
                .AddAspNetCoreInstrumentation(tracing =>
                    tracing.Filter = ShouldTraceHttpRequest)
                .AddHttpClientInstrumentation());

        _ = builder.AddOpenTelemetryExporters();

        return builder;
    }

    /// <summary>
    /// Returns <c>false</c> when the request path targets one of the health probe endpoints
    /// (<c>/health</c>, <c>/alive</c>, <c>/ready</c>) so those requests are NOT traced on the
    /// default ASP.NET Core source. Extracted from the inline lambda in
    /// <see cref="ConfigureOpenTelemetry{TBuilder}"/> for Story 7.5 AC #5 regression testing.
    /// </summary>
    public static bool ShouldTraceHttpRequest(HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return !context.Request.Path.StartsWithSegments(HealthEndpointPaths.Health)
            && !context.Request.Path.StartsWithSegments(HealthEndpointPaths.Alive)
            && !context.Request.Path.StartsWithSegments(HealthEndpointPaths.Ready);
    }

    private static TBuilder AddOpenTelemetryExporters<TBuilder>(this TBuilder builder)
        where TBuilder : IHostApplicationBuilder
    {
        bool useOtlpExporter = !string.IsNullOrWhiteSpace(builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"]);

        if (useOtlpExporter)
        {
            _ = builder.Services.AddOpenTelemetry().UseOtlpExporter();
        }

        return builder;
    }

    public static TBuilder AddDefaultHealthChecks<TBuilder>(this TBuilder builder)
        where TBuilder : IHostApplicationBuilder
    {
        _ = builder.Services.AddHealthChecks()
            .AddCheck("self", () => HealthCheckResult.Healthy(), ["live"]);

        return builder;
    }

    public static WebApplication MapDefaultEndpoints(this WebApplication app)
    {
        ArgumentNullException.ThrowIfNull(app);

        var statusCodes = new Dictionary<HealthStatus, int>
        {
            [HealthStatus.Healthy] = StatusCodes.Status200OK,
            [HealthStatus.Degraded] = StatusCodes.Status200OK,
            [HealthStatus.Unhealthy] = StatusCodes.Status503ServiceUnavailable,
        };

        var healthOptions = new HealthCheckOptions
        {
            ResultStatusCodes = statusCodes,
            ResponseWriter = BackendHealthResponseWriter.WriteAsync,
        };

        _ = app.MapHealthChecks(HealthEndpointPaths.Health, healthOptions);

        _ = app.MapHealthChecks(HealthEndpointPaths.Alive, new HealthCheckOptions
        {
            Predicate = r => r.Tags.Contains("live"),
            ResultStatusCodes = statusCodes,
            ResponseWriter = BackendHealthResponseWriter.WriteAsync,
        });

        _ = app.MapHealthChecks(HealthEndpointPaths.Ready, new HealthCheckOptions
        {
            Predicate = r => r.Tags.Contains("ready"),
            ResultStatusCodes = statusCodes,
            ResponseWriter = BackendHealthResponseWriter.WriteAsync,
        });

        return app;
    }
}
