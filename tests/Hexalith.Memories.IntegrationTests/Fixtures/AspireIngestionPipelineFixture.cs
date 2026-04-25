// <copyright file="AspireIngestionPipelineFixture.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.IntegrationTests.Fixtures;

using System.Diagnostics;
using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.RegularExpressions;

using Aspire.Hosting;
using Aspire.Hosting.Testing;

using Dapr.Actors;
using Dapr.Actors.Client;

using Hexalith.Memories.Contracts.V1;
using Hexalith.Memories.Server.Actors;
using Hexalith.Memories.IntegrationTests.Telemetry.Infrastructure;
using Hexalith.Memories.Telemetry;
using Hexalith.Memories.TestHelpers.Process;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using StackExchange.Redis;

/// <summary>Starts the full Aspire topology for end-to-end ingestion workflow tests.</summary>
public sealed class AspireIngestionPipelineFixture : IAsyncLifetime
{
    private DistributedApplication? _app;
    private IDistributedApplicationTestingBuilder? _builder;
    private string _daprAppId = string.Empty;
    private string _redisVolumeName = string.Empty;
    private string _eventStoreMappedTenantId = string.Empty;
    private ActorProxyFactory? _actorProxyFactory;
    private ActorProxyOptions? _actorProxyOptions;
    private HttpClientHandler? _actorHttpMessageHandler;
    private EnvVarScope? _aspNetCoreEnvironmentScope;
    private EnvVarScope? _dotNetEnvironmentScope;
    private EnvVarScope? _fakeEmbeddingScope;
    private EnvVarScope? _allowPrivateHostsScope;
    private EnvVarScope? _daprAppIdScope;
    private EnvVarScope? _redisVolumeNameScope;
    private EnvVarScope? _eventStoreSourceMapScope;
    private EnvVarScope? _telemetryInMemoryScope;
    private readonly TestLogProvider _logProvider = new();
    private static readonly Regex DaprHttpPortRegex = new(
        @"HTTP server listening on TCP address: :(?<port>\d+)",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    /// <summary>Gets the HTTP client for the Memories Server resource.</summary>
    public HttpClient MemoriesClient { get; private set; } = null!;

    /// <summary>Gets the HTTP client for the MCP server resource (Story 10.1).</summary>
    public HttpClient McpClient { get; private set; } = null!;

    /// <summary>Gets the endpoint URI for the MCP server resource (Story 10.1).</summary>
    public Uri McpEndpoint { get; private set; } = null!;

    /// <summary>Gets the DAPR HTTP sidecar endpoint used by the Memories Server resource.</summary>
    public Uri DaprSidecarHttpEndpoint { get; private set; } = new("http://127.0.0.1:3500");

    /// <summary>Gets the tenant id bound to the default integration-test EventStore source prefix.</summary>
    public string EventStoreMappedTenantId => _eventStoreMappedTenantId;

    /// <summary>Gets the CloudEvents <c>source</c> prefix mapped to <see cref="EventStoreMappedTenantId"/>.</summary>
    public string EventStoreMappedSourcePrefix => "enterprise.claims";

    /// <summary>Gets the number of captured log entries.</summary>
    public int LogEntryCount => _logProvider.Count;

    /// <summary>Gets the Redis Stack connection for backend verification.</summary>
    public IConnectionMultiplexer RedisConnection { get; private set; } = null!;

    /// <summary>Gets the FalkorDB connection for backend verification.</summary>
    public IConnectionMultiplexer FalkorDbConnection { get; private set; } = null!;

    /// <summary>Creates a counter-actor proxy against the test DAPR sidecar endpoint.</summary>
    /// <param name="tenantId">Tenant identifier.</param>
    /// <param name="caseId">Case identifier.</param>
    /// <returns>The actor proxy.</returns>
    public ICaseIngestionCounterActor CreateCaseIngestionCounterActorProxy(string tenantId, string caseId)
        => CreateActorProxy<ICaseIngestionCounterActor>($"{tenantId}:{caseId}", "CaseIngestionCounterActor");

    /// <summary>Creates a rate-limiter actor proxy against the test DAPR sidecar endpoint.</summary>
    /// <param name="tenantId">Tenant identifier.</param>
    /// <returns>The actor proxy.</returns>
    public IEmbeddingRateLimiterActor CreateEmbeddingRateLimiterActorProxy(string tenantId)
        => CreateActorProxy<IEmbeddingRateLimiterActor>(tenantId, "EmbeddingRateLimiterActor");

    /// <summary>Creates a corpus-statistics actor proxy against the test DAPR sidecar endpoint.</summary>
    /// <param name="tenantId">Tenant identifier.</param>
    /// <returns>The actor proxy.</returns>
    public ICorpusStatisticsActor CreateCorpusStatisticsActorProxy(string tenantId)
        => CreateActorProxy<ICorpusStatisticsActor>(tenantId, "CorpusStatisticsActor");

    /// <summary>Restarts the full topology and reconnects all clients.</summary>
    /// <returns>The elapsed warm-restart duration.</returns>
    public async Task<TimeSpan> RestartTopologyAsync()
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(3));
        Stopwatch stopwatch = Stopwatch.StartNew();
        await DisposeTopologyAsync(cts.Token).ConfigureAwait(false);
        await StartTopologyAsync(cts.Token).ConfigureAwait(false);
        stopwatch.Stop();
        return stopwatch.Elapsed;
    }

    /// <inheritdoc/>
    public async Task InitializeAsync()
    {
        _daprAppId = $"memories-server-it-{Guid.NewGuid():N}";
        _redisVolumeName = $"hexalith-memories-it-{Guid.NewGuid():N}";
        _eventStoreMappedTenantId = $"tenant-eventstore-{Guid.NewGuid():N}";

        // If anything after the env-var scopes are acquired fails, xUnit does NOT call DisposeAsync
        // (the fixture failed to initialize). Acquire every process-wide override via EnvVarScope so
        // the shared serialization helper protects cross-assembly tests from snapshot/restore races,
        // then tear the scopes down on failure.
        try
        {
            _telemetryInMemoryScope = EnvVarScope.Set(
                InMemoryTelemetryEnvironment.EnvVar,
                InMemoryTelemetryEnvironment.EnabledValue);
            _aspNetCoreEnvironmentScope = EnvVarScope.Set("ASPNETCORE_ENVIRONMENT", "Development");
            _dotNetEnvironmentScope = EnvVarScope.Set("DOTNET_ENVIRONMENT", "Development");
            _fakeEmbeddingScope = EnvVarScope.Set("Memories__Testing__UseFakeEmbedding", "true");
            _allowPrivateHostsScope = EnvVarScope.Set("Ingestion__UrlFetcher__AllowPrivateHosts", "true");
            _daprAppIdScope = EnvVarScope.Set("MEMORIES_DAPR_APP_ID", _daprAppId);
            _redisVolumeNameScope = EnvVarScope.Set("MEMORIES_REDIS_VOLUME_NAME", _redisVolumeName);
            _eventStoreSourceMapScope = EnvVarScope.Set(
                "EventStoreIntegration__Routing__SourceToTenantMap__enterprise.claims",
                _eventStoreMappedTenantId);

            using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(3));
            await StartTopologyAsync(cts.Token).ConfigureAwait(false);
        }
        catch
        {
            DisposeEnvVarScopes();
            throw;
        }
    }

    private void DisposeEnvVarScopes()
    {
        _eventStoreSourceMapScope?.Dispose();
        _eventStoreSourceMapScope = null;
        _redisVolumeNameScope?.Dispose();
        _redisVolumeNameScope = null;
        _daprAppIdScope?.Dispose();
        _daprAppIdScope = null;
        _allowPrivateHostsScope?.Dispose();
        _allowPrivateHostsScope = null;
        _fakeEmbeddingScope?.Dispose();
        _fakeEmbeddingScope = null;
        _dotNetEnvironmentScope?.Dispose();
        _dotNetEnvironmentScope = null;
        _aspNetCoreEnvironmentScope?.Dispose();
        _aspNetCoreEnvironmentScope = null;
        _telemetryInMemoryScope?.Dispose();
        _telemetryInMemoryScope = null;
    }

    /// <summary>Returns a snapshot of log entries captured since the specified starting index.</summary>
    /// <param name="startIndex">The 0-based index from which to read newly-captured log entries.</param>
    /// <returns>The captured log entries after the starting index.</returns>
    public IReadOnlyList<CapturedLogEntry> GetLogEntriesSince(int startIndex) => _logProvider.GetEntriesSince(startIndex);

    /// <summary>Default wait budget for a tenant to reach <see cref="TenantStatus.Active"/>.</summary>
    public static readonly TimeSpan DefaultTenantActivationTimeout = TimeSpan.FromMinutes(2);

    /// <summary>
    /// Provisions a new tenant via <c>POST /api/tenants</c> and waits for it to reach
    /// <see cref="TenantStatus.Active"/>. Use in tests that need a tenant in place before
    /// calling case, search, or ingestion endpoints — <see cref="Hexalith.Memories.Server.Tenants.TenantStatusGuard"/>
    /// rejects operations against unknown or non-Active tenants with 404/409.
    /// </summary>
    /// <param name="tenantId">Optional tenant identifier. When null, a random one is generated.</param>
    /// <param name="displayName">Optional display name. Defaults to the tenant id.</param>
    /// <param name="activationTimeout">Max wait for Active status. Defaults to <see cref="DefaultTenantActivationTimeout"/>.</param>
    /// <param name="cancellationToken">Cooperative cancellation.</param>
    /// <returns>The provisioned tenant id.</returns>
    public async Task<string> ProvisionActiveTenantAsync(
        string? tenantId = null,
        string? displayName = null,
        TimeSpan? activationTimeout = null,
        CancellationToken cancellationToken = default)
    {
        string id = tenantId ?? $"tenant-it-{Guid.NewGuid():N}";
        string name = displayName ?? $"Tenant {id}";

        using HttpResponseMessage provisionResponse = await MemoriesClient.PostAsJsonAsync(
            "/api/tenants",
            new TenantProvisioningInput(id, name),
            MemoriesJsonContext.Options,
            cancellationToken).ConfigureAwait(false);

        // 202 Accepted on fresh provision; 409 Conflict when the caller passed a pre-existing id —
        // treat both as "tenant exists" so callers can idempotently re-use a deterministic id.
        if (provisionResponse.StatusCode is not (HttpStatusCode.Accepted or HttpStatusCode.Conflict))
        {
            string body = await provisionResponse.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            throw new InvalidOperationException(
                $"Unexpected POST /api/tenants response for '{id}': {(int)provisionResponse.StatusCode} {provisionResponse.ReasonPhrase}. Body: {body}");
        }

        await WaitForTenantActiveAsync(id, activationTimeout ?? DefaultTenantActivationTimeout, cancellationToken).ConfigureAwait(false);
        return id;
    }

    /// <summary>Polls <c>GET /api/tenants/{tenantId}</c> until the tenant reports <see cref="TenantStatus.Active"/>.</summary>
    /// <param name="tenantId">Tenant identifier.</param>
    /// <param name="timeout">Max wait duration.</param>
    /// <param name="cancellationToken">Cooperative cancellation.</param>
    public async Task WaitForTenantActiveAsync(
        string tenantId,
        TimeSpan? timeout = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);

        TimeSpan budget = timeout ?? DefaultTenantActivationTimeout;
        DateTimeOffset deadline = DateTimeOffset.UtcNow.Add(budget);
        while (DateTimeOffset.UtcNow < deadline)
        {
            cancellationToken.ThrowIfCancellationRequested();
            using HttpResponseMessage tenantResponse = await MemoriesClient.GetAsync(
                $"/api/tenants/{tenantId}",
                cancellationToken).ConfigureAwait(false);
            if (tenantResponse.StatusCode == HttpStatusCode.OK)
            {
                TenantInfo? tenant = await tenantResponse.Content.ReadFromJsonAsync<TenantInfo>(
                    MemoriesJsonContext.Options,
                    cancellationToken).ConfigureAwait(false);
                if (tenant?.Status == TenantStatus.Active)
                {
                    return;
                }
            }

            await Task.Delay(TimeSpan.FromMilliseconds(500), cancellationToken).ConfigureAwait(false);
        }

        throw new TimeoutException($"Tenant '{tenantId}' did not reach Active state within {budget}.");
    }

    /// <summary>Stops the FalkorDB container hosted by the Aspire topology.</summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task that completes when the container has stopped.</returns>
    public Task StopFalkorDbContainerAsync(CancellationToken cancellationToken = default)
        => StopContainerAsync(
            "FalkorDB",
            static container => container.Image.Contains("falkordb/falkordb", StringComparison.OrdinalIgnoreCase)
                || container.Name.Contains("falkordb", StringComparison.OrdinalIgnoreCase),
            cancellationToken);

    /// <summary>Stops the Dapr sidecar process for the Memories Server resource.</summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task that completes when the sidecar has stopped.</returns>
    public Task StopDaprSidecarAsync(CancellationToken cancellationToken = default)
        => StopProcessListeningOnPortAsync(
            DaprSidecarHttpEndpoint.Port,
            "Memories Server Dapr sidecar",
            cancellationToken);

    /// <inheritdoc/>
    public async Task DisposeAsync()
    {
        await DisposeTopologyAsync(CancellationToken.None).ConfigureAwait(false);
        DisposeEnvVarScopes();
    }

    private TActor CreateActorProxy<TActor>(string actorId, string actorType)
        where TActor : IActor
    {
        if (_actorProxyFactory is null || _actorProxyOptions is null)
        {
            throw new InvalidOperationException("Actor proxies are unavailable before the topology has started.");
        }

        return _actorProxyFactory.CreateActorProxy<TActor>(new ActorId(actorId), actorType, _actorProxyOptions);
    }

    private async Task StartTopologyAsync(CancellationToken cancellationToken)
    {
        int logStartIndex = _logProvider.Count;

        _builder = await DistributedApplicationTestingBuilder
            .CreateAsync<Projects.Hexalith_Memories_AppHost>()
            .ConfigureAwait(false);

        _ = _builder.Services.AddLogging(logging =>
        {
            _ = logging.SetMinimumLevel(LogLevel.Warning);
            _ = logging.AddFilter((category, level) =>
            {
                if (category?.StartsWith("Aspire.", StringComparison.Ordinal) == true)
                {
                    return level >= LogLevel.Warning;
                }

                if (IsMemoriesServerCategory(category))
                {
                    return level >= LogLevel.Information;
                }

                return level >= LogLevel.Warning;
            });
            _ = logging.AddProvider(_logProvider);
        });

        _app = await _builder.BuildAsync().ConfigureAwait(false);
        await _app.StartAsync(cancellationToken).ConfigureAwait(false);

        _ = await _app.ResourceNotifications
            .WaitForResourceHealthyAsync("memories-server", cancellationToken)
            .WaitAsync(TimeSpan.FromMinutes(3), cancellationToken)
            .ConfigureAwait(false);

        MemoriesClient = _app.CreateHttpClient("memories-server");
        MemoriesClient.Timeout = TimeSpan.FromSeconds(60);

        await WaitForEndpointAsync(
            MemoriesClient,
            "/health",
            [HttpStatusCode.OK],
            TimeSpan.FromMinutes(3),
            TimeSpan.FromSeconds(2)).ConfigureAwait(false);

        // Story 10.1 — wait for the MCP service and expose its endpoint + client. The MCP /ready
        // probe waits on the upstream Memories Server (3-strike rolling window); since we already
        // confirmed memories-server /health above, the MCP readiness check converges quickly.
        _ = await _app.ResourceNotifications
            .WaitForResourceHealthyAsync("memories-mcp", cancellationToken)
            .WaitAsync(TimeSpan.FromMinutes(3), cancellationToken)
            .ConfigureAwait(false);

        McpClient = _app.CreateHttpClient("memories-mcp");
        McpClient.Timeout = TimeSpan.FromSeconds(60);
        McpEndpoint = _app.GetEndpoint("memories-mcp", "http");

        await WaitForEndpointAsync(
            McpClient,
            "/health",
            [HttpStatusCode.OK],
            TimeSpan.FromMinutes(3),
            TimeSpan.FromSeconds(2)).ConfigureAwait(false);

        DaprSidecarHttpEndpoint = ResolveDaprSidecarHttpEndpoint(logStartIndex);

        Uri redisEndpoint = _app.GetEndpoint("redis", "redis");
        Uri falkorEndpoint = _app.GetEndpoint("falkordb", "falkordb");

        RedisConnection = await ConnectionMultiplexer.ConnectAsync(redisEndpoint.Authority).ConfigureAwait(false);
        FalkorDbConnection = await ConnectionMultiplexer.ConnectAsync(falkorEndpoint.Authority).ConfigureAwait(false);

        _actorProxyOptions = new ActorProxyOptions
        {
            HttpEndpoint = DaprSidecarHttpEndpoint.ToString(),
            RequestTimeout = TimeSpan.FromSeconds(30),
            JsonSerializerOptions = MemoriesJsonContext.Options,
        };
        _actorHttpMessageHandler = new HttpClientHandler();
        _actorProxyFactory = new ActorProxyFactory(_actorProxyOptions, (HttpMessageHandler)_actorHttpMessageHandler);
    }

    /// <summary>
    /// Accepts the Aspire resource-log category for the Memories Server resource. The Aspire runtime prefixes
    /// category names with the resource id ("memories-server") followed by either the end of the category or
    /// a "-" / "." separator (for related sub-resources such as "memories-server-dapr-cli"). A substring
    /// <c>Contains</c> match is too broad — any unrelated future test-runner category that happens to embed
    /// "memories-server" would be elevated above the Warning floor and add noise to the captured stream.
    /// </summary>
    /// <param name="category">The logger category name as provided by the logging pipeline.</param>
    /// <returns><c>true</c> when the category identifies the Memories Server resource or one of its sub-resources.</returns>
    private static bool IsMemoriesServerCategory(string? category)
    {
        if (string.IsNullOrEmpty(category))
        {
            return false;
        }

        const string resourceId = "memories-server";
        if (!category.StartsWith(resourceId, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (category.Length == resourceId.Length)
        {
            return true;
        }

        char next = category[resourceId.Length];
        return next is '-' or '.';
    }

    private Uri ResolveDaprSidecarHttpEndpoint(int logStartIndex)
    {
        if (_app is not null)
        {
            foreach (string resourceName in new[] { "memories-server-dapr", "memories-server-dapr-cli" })
            {
                try
                {
                    return _app.GetEndpoint(resourceName, "http");
                }
                catch (ArgumentException)
                {
                    // Fall back to the historical log-scrape path below when the sidecar resource
                    // does not expose a directly allocated endpoint under this name.
                }
            }
        }

        return ResolveDaprSidecarHttpEndpoint(_logProvider.GetEntriesSince(logStartIndex));
    }

    private static Uri ResolveDaprSidecarHttpEndpoint(IReadOnlyList<CapturedLogEntry> entries)
    {
        ArgumentNullException.ThrowIfNull(entries);

        for (int i = entries.Count - 1; i >= 0; i--)
        {
            CapturedLogEntry entry = entries[i];
            if (!entry.Category.Contains("memories-server-dapr-cli", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            Match match = DaprHttpPortRegex.Match(entry.Message);
            if (match.Success && int.TryParse(match.Groups["port"].Value, out int port) && port > 0)
            {
                return new Uri($"http://127.0.0.1:{port}");
            }
        }

        throw new InvalidOperationException(
            "Could not determine the Memories Server Dapr sidecar HTTP endpoint from the captured Aspire logs.");
    }

    private async Task DisposeTopologyAsync(CancellationToken cancellationToken)
    {
        if (MemoriesClient is not null)
        {
            MemoriesClient.Dispose();
            MemoriesClient = null!;
        }

        if (RedisConnection is not null)
        {
            await RedisConnection.CloseAsync().ConfigureAwait(false);
            RedisConnection.Dispose();
            RedisConnection = null!;
        }

        if (FalkorDbConnection is not null)
        {
            await FalkorDbConnection.CloseAsync().ConfigureAwait(false);
            FalkorDbConnection.Dispose();
            FalkorDbConnection = null!;
        }

        cancellationToken.ThrowIfCancellationRequested();

        if (_app is not null)
        {
            await _app.DisposeAsync().ConfigureAwait(false);
            _app = null;
        }

        cancellationToken.ThrowIfCancellationRequested();

        if (_builder is not null)
        {
            await _builder.DisposeAsync().ConfigureAwait(false);
            _builder = null;
        }

        _actorProxyFactory = null;
        _actorProxyOptions = null;
        _actorHttpMessageHandler?.Dispose();
        _actorHttpMessageHandler = null;
    }

    private async Task StopContainerAsync(
        string description,
        Func<RunningContainer, bool> predicate,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(description);
        ArgumentNullException.ThrowIfNull(predicate);

        IReadOnlyList<RunningContainer> containers = await ListRunningContainersAsync(cancellationToken).ConfigureAwait(false);
        RunningContainer container = containers.FirstOrDefault(predicate);

        if (string.IsNullOrWhiteSpace(container.Name))
        {
            string available = string.Join(
                Environment.NewLine,
                containers.Select(c => $"- {c.Name} ({c.Image})"));
            throw new InvalidOperationException(
                $"Could not find the {description} container in the running Aspire topology. " +
                $"Available containers:{Environment.NewLine}{available}");
        }

        _ = await RunDockerCommandAsync($"stop --time 0 {container.Name}", cancellationToken).ConfigureAwait(false);
    }

    private static async Task<IReadOnlyList<RunningContainer>> ListRunningContainersAsync(CancellationToken cancellationToken)
    {
        string output = await RunDockerCommandAsync(
            "ps --format \"{{.Names}}|{{.Image}}\" --no-trunc",
            cancellationToken).ConfigureAwait(false);

        List<RunningContainer> containers = [];
        foreach (string line in output.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            string[] parts = line.Split('|', 2, StringSplitOptions.TrimEntries);
            if (parts.Length == 2)
            {
                containers.Add(new RunningContainer(parts[0], parts[1]));
            }
        }

        return containers;
    }

    private static async Task StopProcessListeningOnPortAsync(
        int port,
        string description,
        CancellationToken cancellationToken)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(port);
        ArgumentException.ThrowIfNullOrWhiteSpace(description);

        int processId = OperatingSystem.IsWindows()
            ? await FindWindowsListeningProcessIdAsync(port, cancellationToken).ConfigureAwait(false)
            : await FindUnixListeningProcessIdAsync(port, cancellationToken).ConfigureAwait(false);

        if (processId <= 0)
        {
            throw new InvalidOperationException(
                $"Could not find the {description} process listening on port {port}.");
        }

        using Process process = Process.GetProcessById(processId);
        process.Kill(entireProcessTree: true);
        await process.WaitForExitAsync(cancellationToken).ConfigureAwait(false);
    }

    private static async Task<int> FindWindowsListeningProcessIdAsync(int port, CancellationToken cancellationToken)
    {
        string output = await RunProcessCommandAsync("netstat", "-ano -p tcp", cancellationToken).ConfigureAwait(false);

        foreach (string line in output.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            string[] parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (parts.Length < 5 || !parts[0].Equals("TCP", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (!parts[1].EndsWith($":{port}", StringComparison.Ordinal) ||
                !parts[3].Equals("LISTENING", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (int.TryParse(parts[4], out int processId))
            {
                return processId;
            }
        }

        return 0;
    }

    private static async Task<int> FindUnixListeningProcessIdAsync(int port, CancellationToken cancellationToken)
    {
        string output = await RunProcessCommandAsync(
            "/bin/sh",
            $"-c \"lsof -ti tcp:{port} -sTCP:LISTEN\"",
            cancellationToken).ConfigureAwait(false);

        string firstLine = output.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .FirstOrDefault() ?? string.Empty;

        return int.TryParse(firstLine, out int processId) ? processId : 0;
    }

    private static async Task<string> RunDockerCommandAsync(string arguments, CancellationToken cancellationToken)
        => await RunProcessCommandAsync("docker", arguments, cancellationToken).ConfigureAwait(false);

    private static async Task<string> RunProcessCommandAsync(
        string fileName,
        string arguments,
        CancellationToken cancellationToken)
    {
        using Process process = new();
        process.StartInfo.FileName = fileName;
        process.StartInfo.Arguments = arguments;
        process.StartInfo.RedirectStandardOutput = true;
        process.StartInfo.RedirectStandardError = true;
        process.StartInfo.UseShellExecute = false;
        process.StartInfo.CreateNoWindow = true;

        if (!process.Start())
        {
            throw new InvalidOperationException($"Failed to start '{fileName} {arguments}'.");
        }

        Task<string> stdoutTask = process.StandardOutput.ReadToEndAsync();
        Task<string> stderrTask = process.StandardError.ReadToEndAsync();

        await process.WaitForExitAsync(cancellationToken).ConfigureAwait(false);

        string stdout = (await stdoutTask.ConfigureAwait(false)).Trim();
        string stderr = (await stderrTask.ConfigureAwait(false)).Trim();

        if (process.ExitCode != 0)
        {
            throw new InvalidOperationException(
                $"{fileName} {arguments} failed with exit code {process.ExitCode}: {stderr}");
        }

        return stdout;
    }

    private static async Task WaitForEndpointAsync(
        HttpClient client,
        string url,
        IReadOnlyCollection<HttpStatusCode> expectedStatusCodes,
        TimeSpan timeout,
        TimeSpan pollInterval)
    {
        DateTimeOffset deadline = DateTimeOffset.UtcNow.Add(timeout);
        Exception? lastException = null;
        HttpStatusCode? lastStatusCode = null;

        while (DateTimeOffset.UtcNow < deadline)
        {
            try
            {
                using HttpResponseMessage response = await client.GetAsync(url).ConfigureAwait(false);
                lastStatusCode = response.StatusCode;

                if (expectedStatusCodes.Contains(response.StatusCode))
                {
                    return;
                }
            }
            catch (Exception ex)
            {
                lastException = ex;
            }

            await Task.Delay(pollInterval).ConfigureAwait(false);
        }

        throw new TimeoutException(
            $"Endpoint '{url}' did not become ready within {timeout}. " +
            $"Last status: {lastStatusCode?.ToString() ?? "n/a"}. " +
            $"Last error: {lastException?.Message ?? "n/a"}.");
    }

    /// <summary>Represents a captured integration-test log entry.</summary>
    public sealed record CapturedLogEntry(LogLevel Level, string Category, string Message);

    private readonly record struct RunningContainer(string Name, string Image);

    private sealed class TestLogProvider : ILoggerProvider
    {
        private readonly object _gate = new();
        private readonly List<CapturedLogEntry> _entries = [];

        public int Count
        {
            get
            {
                lock (_gate)
                {
                    return _entries.Count;
                }
            }
        }

        public ILogger CreateLogger(string categoryName) => new TestLogger(categoryName, this);

        public void Dispose()
        {
        }

        public IReadOnlyList<CapturedLogEntry> GetEntriesSince(int startIndex)
        {
            lock (_gate)
            {
                int effectiveIndex = Math.Clamp(startIndex, 0, _entries.Count);
                return _entries.Skip(effectiveIndex).ToList();
            }
        }

        private void Add(CapturedLogEntry entry)
        {
            lock (_gate)
            {
                _entries.Add(entry);
            }
        }

        private sealed class TestLogger(string categoryName, TestLogProvider owner) : ILogger
        {
            public IDisposable? BeginScope<TState>(TState state)
                where TState : notnull => null;

            public bool IsEnabled(LogLevel logLevel) => true;

            public void Log<TState>(
                LogLevel logLevel,
                EventId eventId,
                TState state,
                Exception? exception,
                Func<TState, Exception?, string> formatter)
                => owner.Add(new CapturedLogEntry(logLevel, categoryName, formatter(state, exception)));
        }
    }
}

[CollectionDefinition("AspireIngestionPipeline", DisableParallelization = true)]
public sealed class AspireIngestionPipelineCollection : ICollectionFixture<AspireIngestionPipelineFixture>;
