using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Text.Json;

using Hexalith.Memories.ServiceDefaults.Health;
using Hexalith.Memories.ServiceDefaults.Telemetry;
using Hexalith.Memories.Telemetry;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

using OpenTelemetry;
using OpenTelemetry.Instrumentation.StackExchangeRedis;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;

using StackExchange.Redis;

namespace Hexalith.Memories.ServiceDefaults;

public static class Extensions
{
    /// <summary>
    /// Story 8.5 — keyed <see cref="IConnectionMultiplexer"/> service keys that the Redis OTEL
    /// instrumentation subscribes to. Mirrors the registrations at
    /// <c>src/Hexalith.Memories.Server/Program.cs</c>. Kept internal so shared
    /// <see cref="AddServiceDefaults{TBuilder}(TBuilder)"/> remains the canonical public
    /// composition entry while Tier-2 registration tests still share the same source-of-truth key
    /// names via InternalsVisibleTo.
    /// </summary>
    internal const string RedisConnectionKey = "redis";

    /// <summary>Story 8.5 — FalkorDB keyed <see cref="IConnectionMultiplexer"/> service key.</summary>
    internal const string FalkorDbConnectionKey = "falkordb";

    /// <summary>
    /// Story 8.5 — flush interval applied to BOTH Redis OTEL instrumentation registrations.
    /// See ADR-8.5-001 (e): 100 ms in all environments, no env-gated override. Upstream stores
    /// the interval on a shared singleton; both keyed registrations must resolve to the same
    /// value to avoid silent drain-thread divergence if a future refactor splits the delegate.
    /// </summary>
    internal static readonly TimeSpan RedisInstrumentationFlushInterval = TimeSpan.FromMilliseconds(100);

    public static TBuilder AddServiceDefaults<TBuilder>(
        this TBuilder builder,
        bool configureRedisInstrumentation = true)
        where TBuilder : IHostApplicationBuilder
    {
        _ = builder.ConfigureOpenTelemetry(configureRedisInstrumentation);
        _ = builder.AddDefaultHealthChecks(configureRedisReadyCheck: configureRedisInstrumentation);
        _ = builder.Services.AddServiceDiscovery();

        _ = builder.Services.ConfigureHttpClientDefaults(http =>
        {
            _ = http.AddStandardResilienceHandler();
            _ = http.AddServiceDiscovery();
        });

        return builder;
    }

    public static TBuilder ConfigureOpenTelemetry<TBuilder>(
        this TBuilder builder,
        bool configureRedisInstrumentation = true)
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
            .WithTracing(tracing =>
            {
                _ = tracing
                    .AddSource(builder.Environment.ApplicationName)
                    .AddSource(MemoriesActivitySource.SourceName)
                    .AddAspNetCoreInstrumentation(aspnet =>
                        aspnet.Filter = ShouldTraceHttpRequest)
                    .AddHttpClientInstrumentation();

                if (configureRedisInstrumentation)
                {
                    ConfigureRedisTracing(tracing);
                }
            });

        if (configureRedisInstrumentation)
        {
            // Story 8.5 — add both keyed connections to the shared Redis instrumentation singleton
            // once the TracerProvider has been built. ConfigureRedisInstrumentation is invoked during
            // service resolution of the TracerProvider, which is AFTER the container has been built,
            // so both keyed IConnectionMultiplexer instances are available.
            _ = builder.Services
                .AddOpenTelemetry()
                .WithTracing(tracing => tracing.ConfigureRedisInstrumentation((sp, instrumentation) =>
                {
                    IConnectionMultiplexer redis = sp.GetRequiredKeyedService<IConnectionMultiplexer>(RedisConnectionKey);
                    IConnectionMultiplexer falkordb = sp.GetRequiredKeyedService<IConnectionMultiplexer>(FalkorDbConnectionKey);
                    instrumentation.AddConnection(RedisConnectionKey, redis);
                    instrumentation.AddConnection(FalkorDbConnectionKey, falkordb);
                }));
        }

        _ = builder.AddOpenTelemetryExporters();

        return builder;
    }

    private static void ConfigureRedisTracing(TracerProviderBuilder tracing)
    {
        // Story 8.5 — Redis OTEL instrumentation. The 1.15.1-beta.1 upstream package does
        // not expose a `AddRedisInstrumentation(serviceKey)` keyed-DI overload (that was
        // the spec's original assumption, since rejected). Instead:
        //   1. AddRedisInstrumentation(configure) registers the source + FlushInterval.
        //   2. ConfigureRedisInstrumentation(...) is called post-TracerProvider-build with
        //      the StackExchangeRedisInstrumentation singleton; we resolve both keyed
        //      IConnectionMultiplexer instances from DI and call AddConnection per key.
        //
        // The DI-guard pattern from ADR-8.5-001 (f) is preserved: an AddInstrumentation
        // callback per key throws InvalidOperationException at TracerProvider.Build() if
        // the expected keyed multiplexer is absent, guarding against the silent-drop path
        // that would otherwise occur if the post-build ConfigureRedisInstrumentation
        // simply observed a null key.
        AddRedisKeyedConnectionGuard(tracing, RedisConnectionKey);
        AddRedisKeyedConnectionGuard(tracing, FalkorDbConnectionKey);
        _ = tracing.AddRedisInstrumentation(ConfigureRedisInstrumentation);

        // Story 8.5 ADR-8.5-001 (h) Path A — rewrite db.system tags on FalkorDB spans so
        // APM backends don't misclassify graph queries as generic Redis commands. Resolve
        // the acceptable FalkorDB hostnames from the keyed multiplexer so a future host /
        // alias change does not silently disable the rewrite. Placed AFTER the Redis
        // instrumentation registrations but BEFORE AddOpenTelemetryExporters so
        // processor-vs-exporter order is deterministic (the rewrite lands before any
        // exporter sees the activity).
        _ = tracing.AddProcessor(sp => new FalkorDbSemanticAttributeProcessor(
            ResolveFalkorDbHostnames(sp.GetRequiredKeyedService<IConnectionMultiplexer>(FalkorDbConnectionKey))));
    }

    /// <summary>
    /// Story 8.5 ADR-8.5-001 (e) — the single <c>FlushInterval</c> delegate passed to BOTH keyed
    /// Redis OTEL registrations. Exposed internal so Tier-2 tests can invoke it against a fresh
    /// <see cref="StackExchangeRedisInstrumentationOptions"/> instance per key and assert the
    /// invariant that both multiplexers resolve to the same flush cadence (guards against a
    /// future refactor that wires distinct per-key delegates).
    /// </summary>
    internal static void ConfigureRedisInstrumentation(StackExchangeRedisInstrumentationOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        options.FlushInterval = RedisInstrumentationFlushInterval;
    }

    /// <summary>
    /// Story 8.5 ADR-8.5-001 (f) — DI-keyed-service guard that fires at TracerProvider build time.
    /// Upstream <c>ConfigureRedisInstrumentation</c> accepts connections post-build, but by then
    /// a missing keyed <see cref="IConnectionMultiplexer"/> would either silently drop spans
    /// (if guarded with <c>GetKeyedService</c>) or throw inside a processor callback (if guarded
    /// with <c>GetRequiredKeyedService</c>). This helper moves the check EARLIER, to the
    /// <c>TracerProvider.Build()</c> point, so a misconfigured deployment fails fast at startup
    /// with a descriptive message instead of at the first Redis call.
    /// </summary>
    /// <remarks>
    /// The <c>AddInstrumentation</c> callback returns a private no-op guard handle as the
    /// "instrumentation" payload. That keeps the diagnostic surface meaningful without exposing or
    /// risking disposal of the live multiplexer that the real Redis instrumentation later uses.
    /// </remarks>
    internal static void AddRedisKeyedConnectionGuard(
        TracerProviderBuilder tracing,
        string serviceKey)
    {
        ArgumentNullException.ThrowIfNull(tracing);
        ArgumentException.ThrowIfNullOrEmpty(serviceKey);

        _ = tracing.AddInstrumentation(sp =>
        {
            IConnectionMultiplexer? mux = sp.GetKeyedService<IConnectionMultiplexer>(serviceKey);
            if (mux is null)
            {
                throw new InvalidOperationException(
                    $"Keyed IConnectionMultiplexer '{serviceKey}' not registered — "
                    + "Story 8.5 Redis OTEL needs both 'redis' and 'falkordb' keys.");
            }

            return new RedisKeyedConnectionGuardHandle(serviceKey);
        });
    }

    private static IReadOnlyCollection<string> ResolveFalkorDbHostnames(IConnectionMultiplexer multiplexer)
    {
        ArgumentNullException.ThrowIfNull(multiplexer);

        HashSet<string> hostnames = [FalkorDbSemanticAttributeProcessor.DefaultFalkorDbHostname];
        foreach (EndPoint endpoint in multiplexer.GetEndPoints(configuredOnly: true))
        {
            if (TryGetEndpointHost(endpoint) is string host)
            {
                _ = hostnames.Add(host);
            }
        }

        if (hostnames.Count == 1)
        {
            foreach (EndPoint endpoint in multiplexer.GetEndPoints())
            {
                if (TryGetEndpointHost(endpoint) is string host)
                {
                    _ = hostnames.Add(host);
                }
            }
        }

        return [.. hostnames];
    }

    private static string? TryGetEndpointHost(EndPoint endpoint)
        => endpoint switch
        {
            DnsEndPoint dns => dns.Host,
            IPEndPoint ip => ip.Address.ToString(),
            _ => null,
        };

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
        else if (builder.Environment.IsProduction())
        {
            _ = builder.Services.AddHostedService<OtlpExporterWarningHostedService>();
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
                    ILogger<IntegrationActivityProcessor> logger =
                        sp.GetRequiredService<ILogger<IntegrationActivityProcessor>>();
                    return new IntegrationActivityProcessor(collectorActivities, logger);
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
    /// <remarks>
    /// Story 8.5 widened the breadcrumb filter to include the StackExchange.Redis OTEL source, but
    /// only when the Redis span's parent chain reaches a <see cref="MemoriesActivitySource"/> or
    /// AspNetCore ancestor. Orphan Redis activity (connection-pool housekeeping, idle PING traffic)
    /// is silently dropped from stderr output; a DEBUG log entry explains why on drop so operators
    /// can triage "why didn't my Redis span show up?" without code spelunking.
    /// </remarks>
    internal sealed class IntegrationActivityProcessor : BaseProcessor<Activity>
    {
        internal const string AspNetCoreSourceName = "Microsoft.AspNetCore";

        /// <summary>
        /// Story 8.5 — StackExchange.Redis OTEL ActivitySource name. Matches the upstream
        /// <c>StackExchangeRedisConnectionInstrumentation.ActivitySourceName</c> constant
        /// (assembly-derived).
        /// </summary>
        internal const string RedisSourceName = "OpenTelemetry.Instrumentation.StackExchangeRedis";

        /// <summary>Story 8.5 — max parent-chain traversal depth. Mirrors Story 8.4's
        /// <c>AssertParentChainReachesCliRoot</c> max-depth convention.</summary>
        internal const int MaxParentChainDepth = 16;

        private readonly ICollection<Activity>? _target;
        private readonly ILogger<IntegrationActivityProcessor>? _logger;

        public IntegrationActivityProcessor(ICollection<Activity>? target)
            : this(target, logger: null)
        {
        }

        public IntegrationActivityProcessor(
            ICollection<Activity>? target,
            ILogger<IntegrationActivityProcessor>? logger)
        {
            _target = target;
            _logger = logger;
        }

        public override void OnEnd(Activity data)
        {
            ArgumentNullException.ThrowIfNull(data);
            _target?.Add(data);

            if (!ShouldEmitActivityBreadcrumb(data, _logger))
            {
                return;
            }

            // Serialization or stderr-write failure must NOT propagate into OpenTelemetry's
            // processor pipeline, or it silently kills subsequent span emission. The breadcrumb
            // is test-only diagnostic output; swallow + log a one-line marker on failure.
            try
            {
                Console.Error.WriteLine(FormatActivityBreadcrumb(data));
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(
                    $"{InMemoryTelemetryEnvironment.ActivityBreadcrumbPrefix}<breadcrumb-error:{ex.GetType().Name}>");
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

        /// <summary>
        /// Story 8.4 + 8.5 — decides whether an activity should emit a stderr breadcrumb.
        /// <para>
        /// Story 8.4 emits for the Memories ActivitySource OR AspNetCore Server spans. Story 8.5
        /// extends this to the Redis OTEL source, but ONLY when the parent chain reaches an
        /// ancestor in the Memories or AspNetCore source set. Housekeeping Redis activity
        /// (connection-pool maintenance, idle PINGs) lacks such an ancestor and is silently
        /// dropped from stderr output; the optional <paramref name="logger"/> receives a DEBUG
        /// entry explaining the drop reason.
        /// </para>
        /// </summary>
        internal static bool ShouldEmitActivityBreadcrumb(
            Activity data,
            ILogger<IntegrationActivityProcessor>? logger = null)
        {
            if (string.Equals(data.Source.Name, MemoriesActivitySource.SourceName, StringComparison.Ordinal))
            {
                return true;
            }

            if (data.Kind == ActivityKind.Server
                && string.Equals(data.Source.Name, AspNetCoreSourceName, StringComparison.Ordinal))
            {
                return true;
            }

            if (string.Equals(data.Source.Name, RedisSourceName, StringComparison.Ordinal))
            {
                return ParentChainReachesMemoriesOrAspNetCore(data, logger);
            }

            return false;
        }

        private static bool ParentChainReachesMemoriesOrAspNetCore(
            Activity activity,
            ILogger<IntegrationActivityProcessor>? logger)
        {
            Activity? parent = activity.Parent;
            if (parent is null)
            {
                LogBreadcrumbDrop(logger, activity, reason: "orphan_no_parent", walkedDepth: 0);
                return false;
            }

            HashSet<ActivitySpanId> visited = [];
            int depth = 0;
            while (parent is not null)
            {
                if (!visited.Add(parent.SpanId))
                {
                    // Cycle detection — treat as "not reachable" and drop.
                    LogBreadcrumbDrop(logger, activity, reason: "parent_chain_not_reachable", walkedDepth: depth);
                    return false;
                }

                if (++depth > MaxParentChainDepth)
                {
                    LogBreadcrumbDrop(logger, activity, reason: "depth_exceeded_16", walkedDepth: depth);
                    return false;
                }

                string parentSourceName = parent.Source.Name;
                if (string.Equals(parentSourceName, MemoriesActivitySource.SourceName, StringComparison.Ordinal)
                    || string.Equals(parentSourceName, AspNetCoreSourceName, StringComparison.Ordinal))
                {
                    return true;
                }

                parent = parent.Parent;
            }

            LogBreadcrumbDrop(logger, activity, reason: "parent_chain_not_reachable", walkedDepth: depth);
            return false;
        }

        private static void LogBreadcrumbDrop(
            ILogger<IntegrationActivityProcessor>? logger,
            Activity activity,
            string reason,
            int walkedDepth)
        {
            if (logger is null || !logger.IsEnabled(LogLevel.Debug))
            {
                return;
            }

            logger.LogDebug(
                "redis breadcrumb dropped: {Reason} operation={OperationName} source={SourceName} depth={WalkedDepth}",
                reason,
                activity.OperationName,
                activity.Source.Name,
                walkedDepth);
        }

        private static string? NormalizeSpanId(ActivitySpanId spanId)
        {
            string value = spanId.ToString();
            return value == InMemoryTelemetryEnvironment.EmptySpanIdHex ? null : value;
        }
    }

    private sealed class RedisKeyedConnectionGuardHandle(string serviceKey) : IDisposable
    {
        public string ServiceKey { get; } = serviceKey;

        public void Dispose()
        {
        }

        public override string ToString()
            => $"RedisKeyedConnectionGuard({ServiceKey})";
    }

    private sealed class RedisReadyHealthCheck(IServiceProvider services) : IHealthCheck
    {
        public async Task<HealthCheckResult> CheckHealthAsync(
            HealthCheckContext context,
            CancellationToken cancellationToken = default)
        {
            IConnectionMultiplexer? multiplexer = services.GetKeyedService<IConnectionMultiplexer>(RedisConnectionKey);
            if (multiplexer is null)
            {
                return HealthCheckResult.Unhealthy(
                    $"Keyed IConnectionMultiplexer '{RedisConnectionKey}' is not registered.");
            }

            try
            {
                IDatabase database = multiplexer.GetDatabase();

                // Story 15.6 code review:
                //   - CommandFlags.None (not DemandMaster) so the check stays accurate against a
                //     replica-only client, a Sentinel topology without an elected master, and a
                //     single-node Redis whose multiplexer never promotes a master view.
                //   - PingAsync ignores CancellationToken parameters in SE.Redis 2.x, so race the
                //     ping against the health-check token with WaitAsync — when the framework
                //     timeout fires we fail-Unhealthy promptly instead of hanging until SE.Redis's
                //     own command timeout (5 s default) or further.
                TimeSpan latency = await database
                    .PingAsync(CommandFlags.None)
                    .WaitAsync(cancellationToken)
                    .ConfigureAwait(false);
                return HealthCheckResult.Healthy($"Redis PING succeeded in {latency.TotalMilliseconds:n0} ms.");
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                // Honor cooperative cancellation from the framework health-check timeout.
                throw;
            }
            catch (Exception ex)
            {
                // Story 15.6 code review: previous filter only caught RedisException/TimeoutException/
                // InvalidOperationException, letting ObjectDisposedException and SocketException leak
                // out as the framework's generic "An unhandled exception was thrown". A readiness
                // probe should fail closed on ANY exception from the connection check, not propagate.
                return HealthCheckResult.Unhealthy("Redis PING failed.", ex);
            }
        }
    }

    private sealed class OtlpExporterWarningHostedService(IServiceProvider services) : IHostedService
    {
        public Task StartAsync(CancellationToken cancellationToken)
        {
            // Story 15.6 code review: resolve the logger lazily via IServiceProvider so hosts that
            // opt out of default logging (minimal test fixtures, headless console hosts) do not crash
            // at hosted-service activation time. When logging IS missing — which is precisely the
            // misconfigured-Production scenario this warning was added to surface — fall back to
            // stderr so the warning still reaches operators rather than being silently swallowed.
            const string Message = "OTEL_EXPORTER_OTLP_ENDPOINT is empty in Production; telemetry will be collected in-process but not exported.";
            ILogger<OtlpExporterWarningHostedService>? logger = services
                .GetService<ILogger<OtlpExporterWarningHostedService>>();
            if (logger is not null)
            {
                logger.LogWarning(Message);
            }
            else
            {
                Console.Error.WriteLine($"WARN: {Message}");
            }

            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }

    /// <summary>
    /// Registers the default liveness and readiness checks used by Hexalith services.
    /// </summary>
    /// <remarks>
    /// Story 15.6 closes the Story 1.1 re-review gap where <c>/ready</c> could be green even when Redis
    /// was unreachable. Redis-backed services keep this check enabled by default; hosts without a Redis
    /// dependency can opt out explicitly.
    /// </remarks>
    public static TBuilder AddDefaultHealthChecks<TBuilder>(
        this TBuilder builder,
        bool configureRedisReadyCheck = true)
        where TBuilder : IHostApplicationBuilder
    {
        IHealthChecksBuilder healthChecks = builder.Services.AddHealthChecks()
            .AddCheck("self", () => HealthCheckResult.Healthy(), ["live"]);

        if (configureRedisReadyCheck)
        {
            // ADR-15.6-001: Redis is the minimum viable readiness dependency for Memories services.
            // Keep the default fail-closed so orchestrators stop routing traffic when the keyed
            // multiplexer is absent or cannot answer PING.
            _ = healthChecks.AddCheck<RedisReadyHealthCheck>(
                "redis-ping",
                failureStatus: HealthStatus.Unhealthy,
                tags: ["ready"],
                timeout: TimeSpan.FromSeconds(3));
        }

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
