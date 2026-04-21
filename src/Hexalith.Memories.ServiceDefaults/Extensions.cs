using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text.Json;

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

        // Story 8.4 Task 1.1 — env-var-triggered integration-only activity capture for Tier-3 tests.
        // ADR-8.4-002 (option B): the IActivityCollector implementation lives in the integration-test
        // assembly; ServiceDefaults resolves it opportunistically at processor-creation time when one is
        // registered, but the env-var branch MUST remain safe when the hosting process does not register a
        // collector (for example the separate Memories Server process under Aspire).
        //
        // In addition to the optional in-memory collector sink, the branch emits test-only server-side
        // activity breadcrumbs to stderr for relevant spans. The telemetry integration tests parse those
        // breadcrumbs from the Aspire-captured resource log stream to prove the real server-side
        // memories.search + AspNetCore spans exist without standing up a separate OTLP receiver.
        string? inMemoryFlag = Environment.GetEnvironmentVariable(InMemoryTelemetryEnvironment.EnvVar);
        if (InMemoryTelemetryEnvironment.IsEnabled(inMemoryFlag))
        {
            _ = builder.Services.AddOpenTelemetry()
                .WithTracing(tracing => tracing.AddProcessor(sp =>
                {
                    ICollection<Activity>? collectorActivities = sp.GetService<IActivityCollector>()?.Activities;
                    return new IntegrationActivityProcessor(collectorActivities);
                }));
        }
        else if (!string.IsNullOrEmpty(inMemoryFlag))
        {
            // Risk 7 mitigation: when the env var is set but to a non-activating value, surface a
            // one-line warning so a misconfigured developer sees why capture did nothing.
            Console.Error.WriteLine(InMemoryTelemetryEnvironment.FormatIgnoredValueWarning(inMemoryFlag));
        }

        return builder;
    }

    /// <summary>
    /// Story 8.4 — integration-only <see cref="BaseProcessor{T}"/> used under the
    /// <see cref="InMemoryTelemetryEnvironment.EnvVar"/> test gate. When an
    /// <see cref="IActivityCollector"/> is present it appends completed activities to the supplied
    /// collection; regardless of collector presence it emits test-only server activity breadcrumbs to
    /// stderr for the server-side spans the telemetry integration tests need to observe.
    /// </summary>
    private sealed class IntegrationActivityProcessor : BaseProcessor<Activity>
    {
        private readonly ICollection<Activity>? _target;

        public IntegrationActivityProcessor(ICollection<Activity>? target) => _target = target;

        public override void OnEnd(Activity data)
        {
            ArgumentNullException.ThrowIfNull(data);
            _target?.Add(data);

            if (ShouldEmitActivityBreadcrumb(data))
            {
                Console.Error.WriteLine(FormatActivityBreadcrumb(data));
            }
        }

        private static string FormatActivityBreadcrumb(Activity data)
            => InMemoryTelemetryEnvironment.ActivityBreadcrumbPrefix + JsonSerializer.Serialize(new
            {
                sourceName = data.Source.Name,
                operationName = data.OperationName,
                traceId = data.TraceId.ToString(),
                spanId = data.SpanId.ToString(),
                parentSpanId = NormalizeSpanId(data.ParentSpanId),
                kind = data.Kind.ToString(),
            });

        private static bool ShouldEmitActivityBreadcrumb(Activity data)
            => string.Equals(data.Source.Name, MemoriesActivitySource.SourceName, StringComparison.Ordinal)
                || (data.Kind == ActivityKind.Server
                    && data.Source.Name.Contains("AspNetCore", StringComparison.OrdinalIgnoreCase));

        private static string? NormalizeSpanId(ActivitySpanId spanId)
        {
            string value = spanId.ToString();
            return value == "0000000000000000" ? null : value;
        }
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
