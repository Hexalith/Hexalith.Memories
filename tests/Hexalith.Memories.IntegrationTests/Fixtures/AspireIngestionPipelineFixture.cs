// <copyright file="AspireIngestionPipelineFixture.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.IntegrationTests.Fixtures;

using System.Diagnostics;
using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Testing;

using Dapr.Actors;
using Dapr.Actors.Client;

using Hexalith.Memories.AppHost;
using Hexalith.Memories.Contracts.V1;
using Hexalith.Memories.Server.Actors;
using Hexalith.Memories.Server.Tenants;
using Hexalith.Memories.IntegrationTests.Telemetry.Infrastructure;
using Hexalith.Memories.Telemetry;
using Hexalith.Memories.TestHelpers.Process;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

using StackExchange.Redis;

/// <summary>Starts the full Aspire topology for end-to-end ingestion workflow tests.</summary>
public sealed class AspireIngestionPipelineFixture : IAsyncLifetime
{
    private const string StateStoreName = "statestore";
    private const string TenantRegistryIndexKey = "tenant-registry-index";

    private static readonly TimeSpan TopologyStartupTimeout = TimeSpan.FromMinutes(12);
    private static readonly TimeSpan ResourceHealthyTimeout = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan EndpointReadyTimeout = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan EndpointProbeTimeout = TimeSpan.FromSeconds(45);
    private static readonly TimeSpan EndpointPollInterval = TimeSpan.FromSeconds(2);
    private static readonly TimeSpan DockerVolumeCleanupTimeout = TimeSpan.FromSeconds(30);

    private DistributedApplication? _app;
    private IDistributedApplicationTestingBuilder? _builder;
    private string _daprAppId = string.Empty;
    private string _redisVolumeName = string.Empty;
    private string _falkorVolumeName = string.Empty;
    private string _eventStoreMappedTenantId = string.Empty;
    private ActorProxyFactory? _actorProxyFactory;
    private ActorProxyOptions? _actorProxyOptions;
    private HttpClientHandler? _actorHttpMessageHandler;
    private HttpClient? _daprStateClient;
    private EnvVarScope? _aspNetCoreEnvironmentScope;
    private EnvVarScope? _dotNetEnvironmentScope;
    private EnvVarScope? _fakeEmbeddingScope;
    private EnvVarScope? _inMemoryCommandStoreScope;
    private EnvVarScope? _allowPrivateHostsScope;
    private EnvVarScope? _daprAppIdScope;
    private EnvVarScope? _daprConfigPathScope;
    private EnvVarScope? _redisVolumeNameScope;
    private EnvVarScope? _falkorVolumeNameScope;
    private EnvVarScope? _eventStoreSourceMapScope;
    private EnvVarScope? _enableKeycloakScope;
    private EnvVarScope? _telemetryInMemoryScope;
    private EnvVarScope? _workflowReplaySafetyScope;
    private readonly EmbeddingProviderTestMode _providerMode;
    private readonly EmbeddingProviderSecret? _embeddingProviderSecret;
    private bool _secretsFileExisted;
    private string? _originalSecretsFileContent;
    private string? _secretsFilePath;
    private string? _tempDaprConfigPath;
    private readonly TestLogProvider _logProvider = new();
    private static readonly Regex DaprHttpPortRegex = new(
        @"HTTP server listening on TCP address: :(?<port>\d+)",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    /// <summary>Gets the HTTP client for the Memories Server resource.</summary>
    public HttpClient MemoriesClient { get; private set; } = null!;

    /// <summary>Initializes a new instance of the <see cref="AspireIngestionPipelineFixture"/> class.</summary>
    public AspireIngestionPipelineFixture()
        : this(EmbeddingProviderTestMode.GoogleFake, embeddingProviderSecret: null)
    {
    }

    /// <summary>Initializes a new instance of the <see cref="AspireIngestionPipelineFixture"/> class.</summary>
    /// <param name="providerMode">The embedding provider mode to use for this topology.</param>
    /// <param name="embeddingProviderSecret">Optional DAPR secret-store entry for provider-specific tests.</param>
    internal AspireIngestionPipelineFixture(
        EmbeddingProviderTestMode providerMode,
        EmbeddingProviderSecret? embeddingProviderSecret)
    {
        _providerMode = providerMode;
        _embeddingProviderSecret = embeddingProviderSecret;
    }

    /// <summary>Gets the embedding provider mode used by the fixture.</summary>
    public EmbeddingProviderTestMode ProviderMode => _providerMode;

    /// <summary>Gets the HTTP client for the MCP server resource (Story 10.1).</summary>
    public HttpClient McpClient { get; private set; } = null!;

    /// <summary>Gets the endpoint URI for the MCP server resource (Story 10.1).</summary>
    public Uri McpEndpoint { get; private set; } = null!;

    /// <summary>Symmetric signing key matching <c>src/Hexalith.Memories.Mcp/appsettings.Development.json</c>.</summary>
    public const string McpDevSigningKey = "hexalith-memories-test-signing-key-32b";

    /// <summary>Issuer matching <c>src/Hexalith.Memories.Mcp/appsettings.Development.json</c>.</summary>
    public const string McpDevIssuer = "hexalith-memories-test";

    /// <summary>Audience matching <c>src/Hexalith.Memories.Mcp/appsettings.Development.json</c>.</summary>
    public const string McpDevAudience = "hexalith-memories-server";

    /// <summary>Symmetric signing key matching the Memories Server's test <c>Authentication:JwtBearer:SigningKey</c>
    /// in <c>src/Hexalith.Memories.Server/appsettings.Development.json</c>.</summary>
    public const string ServerDevSigningKey = "hexalith-memories-test-signing-key-32b";

    /// <summary>Issuer matching the Memories Server's test <c>Authentication:JwtBearer:Issuer</c>.</summary>
    public const string ServerDevIssuer = "hexalith-memories-test";

    /// <summary>Audience matching the Memories Server's test <c>Authentication:JwtBearer:Audience</c>.</summary>
    public const string ServerDevAudience = "hexalith-memories-server";

    /// <summary>
    /// Mints a Story 10.2 development bearer token signed with the symmetric key shared with the MCP
    /// resource's <c>appsettings.Development.json</c>. The resulting token carries a
    /// <c>tenant_id</c> claim that the server-side claims transformation maps to
    /// <c>memories:tenant</c> for tenant-claim authorization.
    /// </summary>
    /// <param name="tenantId">The tenant identifier to embed as the <c>tenant_id</c> claim.</param>
    /// <param name="lifetime">Optional token lifetime; defaults to 5 minutes.</param>
    /// <param name="expiresAt">Optional explicit expiry overriding <paramref name="lifetime"/> (for clock-skew or expired-token tests).</param>
    /// <returns>The compact-serialized JWT string ready to be set as <c>Authorization: Bearer &lt;value&gt;</c>.</returns>
    public static string MintDevBearer(
        string tenantId,
        TimeSpan? lifetime = null,
        DateTime? expiresAt = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);

        DateTime now = DateTime.UtcNow;
        DateTime expires = expiresAt ?? now.Add(lifetime ?? TimeSpan.FromMinutes(5));
        DateTime notBefore = expires <= now ? expires.AddMinutes(-5) : now;
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(McpDevSigningKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = McpDevIssuer,
            Audience = McpDevAudience,
            IssuedAt = notBefore,
            NotBefore = notBefore,
            Expires = expires,
            Claims = new Dictionary<string, object>(StringComparer.Ordinal)
            {
                ["sub"] = $"integration-test-{tenantId}",
                ["tenant_id"] = tenantId,
            },
            SigningCredentials = credentials,
        };

        return new JsonWebTokenHandler().CreateToken(descriptor);
    }

    /// <summary>
    /// Mints a Memories <b>Server</b>-realm bearer token (issuer/audience/key matching the server's test
    /// <c>Authentication:JwtBearer</c> configuration) so the fixture's <see cref="MemoriesClient"/> can satisfy the
    /// server's fallback authentication policy (Story 20.1) and per-tenant authorization filter (Story 20.2). The
    /// Development MCP and Server resources intentionally share this realm so MCP can forward the validated bearer.
    /// </summary>
    /// <param name="tenantId">Tenant to embed as the <c>tenant_id</c> claim. When null/blank an auth-only token is
    /// produced for endpoints that require an authenticated principal but no specific tenant claim.</param>
    /// <param name="lifetime">Optional token lifetime; defaults to 10 minutes.</param>
    /// <returns>The compact-serialized JWT string.</returns>
    public static string MintServerBearer(string? tenantId = null, TimeSpan? lifetime = null)
    {
        DateTime now = DateTime.UtcNow;
        DateTime expires = now.Add(lifetime ?? TimeSpan.FromMinutes(10));
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(ServerDevSigningKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new Dictionary<string, object>(StringComparer.Ordinal)
        {
            ["sub"] = string.IsNullOrWhiteSpace(tenantId) ? "integration-test-server" : $"integration-test-{tenantId}",
        };
        if (!string.IsNullOrWhiteSpace(tenantId))
        {
            claims["tenant_id"] = tenantId;
        }

        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = ServerDevIssuer,
            Audience = ServerDevAudience,
            IssuedAt = now,
            NotBefore = now,
            Expires = expires,
            Claims = claims,
            SigningCredentials = credentials,
        };

        return new JsonWebTokenHandler().CreateToken(descriptor);
    }

    /// <summary>
    /// Attaches a per-request Memories Server bearer token to outbound <see cref="MemoriesClient"/> requests. The
    /// tenant claim is derived from the outgoing request — route (<c>/api/v1/tenants/{tenantId}/...</c>), query
    /// (<c>?tenantId=</c>), or JSON body (<c>tenantId</c>) — so it always matches the tenant the request operates on.
    /// Requests that already carry an <c>Authorization</c> header are left untouched, letting individual tests inject
    /// their own token (e.g. expired- or wrong-tenant-token scenarios).
    /// </summary>
    private sealed class ServerBearerAuthHandler(HttpMessageHandler innerHandler) : DelegatingHandler(innerHandler)
    {
        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            if (request.Headers.Authorization is null)
            {
                string? tenantId = await ResolveRequestTenantAsync(request, cancellationToken).ConfigureAwait(false);
                request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(
                    "Bearer",
                    MintServerBearer(tenantId));
            }

            return await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
        }

        private static async Task<string?> ResolveRequestTenantAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            // 1. Route: /api/v1/tenants/{tenantId}/...
            string path = request.RequestUri?.AbsolutePath ?? string.Empty;
            const string tenantsPrefix = "/api/v1/tenants/";
            if (path.StartsWith(tenantsPrefix, StringComparison.OrdinalIgnoreCase))
            {
                string remainder = path[tenantsPrefix.Length..];
                int slash = remainder.IndexOf('/', StringComparison.Ordinal);
                string segment = slash >= 0 ? remainder[..slash] : remainder;
                if (!string.IsNullOrWhiteSpace(segment))
                {
                    return Uri.UnescapeDataString(segment);
                }
            }

            // 2. Query: ?tenantId=...
            string query = request.RequestUri?.Query ?? string.Empty;
            foreach (string pair in query.TrimStart('?').Split('&', StringSplitOptions.RemoveEmptyEntries))
            {
                int eq = pair.IndexOf('=', StringComparison.Ordinal);
                if (eq > 0 && string.Equals(pair[..eq], "tenantId", StringComparison.OrdinalIgnoreCase))
                {
                    return Uri.UnescapeDataString(pair[(eq + 1)..]);
                }
            }

            // 3. JSON body: { "tenantId": "..." } (e.g. POST /api/v1/tenants, POST /api/v1/ingest).
            if (request.Content is not null
                && string.Equals(
                    request.Content.Headers.ContentType?.MediaType,
                    "application/json",
                    StringComparison.OrdinalIgnoreCase))
            {
                byte[] payload = await request.Content.ReadAsByteArrayAsync(cancellationToken).ConfigureAwait(false);

                // Re-buffer the content so the pipeline can still send it after we consumed the stream.
                var buffered = new ByteArrayContent(payload);
                foreach (KeyValuePair<string, IEnumerable<string>> header in request.Content.Headers)
                {
                    buffered.Headers.TryAddWithoutValidation(header.Key, header.Value);
                }

                request.Content = buffered;
                return TryReadTenantFromJson(payload);
            }

            return null;
        }

        private static string? TryReadTenantFromJson(byte[] payload)
        {
            if (payload.Length == 0)
            {
                return null;
            }

            try
            {
                using JsonDocument document = JsonDocument.Parse(payload);
                if (document.RootElement.ValueKind != JsonValueKind.Object)
                {
                    return null;
                }

                foreach (JsonProperty property in document.RootElement.EnumerateObject())
                {
                    if (string.Equals(property.Name, "tenantId", StringComparison.OrdinalIgnoreCase)
                        && property.Value.ValueKind == JsonValueKind.String)
                    {
                        return property.Value.GetString();
                    }
                }
            }
            catch (JsonException)
            {
                // Non-JSON or malformed body — fall back to an auth-only token.
            }

            return null;
        }
    }

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

    /// <summary>Gets the raw Dapr workflow-management projection for failure diagnostics.</summary>
    /// <param name="instanceId">Workflow instance identifier.</param>
    /// <param name="cancellationToken">Cooperative cancellation.</param>
    /// <returns>The HTTP status and raw JSON body returned by the local Dapr sidecar.</returns>
    public async Task<string> GetDaprWorkflowStateDiagnosticAsync(
        string instanceId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(instanceId);
        HttpClient client = _daprStateClient
            ?? throw new InvalidOperationException("DAPR state client is unavailable before the topology has started.");
        using HttpResponseMessage response = await client
            .GetAsync($"/v1.0/workflows/dapr/{Uri.EscapeDataString(instanceId)}", cancellationToken)
            .ConfigureAwait(false);
        string body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        return $"{(int)response.StatusCode} {response.StatusCode}: {body}";
    }

    /// <summary>Creates a rate-limiter actor proxy against the test DAPR sidecar endpoint.</summary>
    /// <param name="tenantId">Tenant identifier.</param>
    /// <returns>The actor proxy.</returns>
    public IEmbeddingRateLimiterActor CreateEmbeddingRateLimiterActorProxy(string tenantId)
        => CreateActorProxy<IEmbeddingRateLimiterActor>(tenantId, "EmbeddingRateLimiterActor");

    /// <summary>Creates a tenant-configuration actor proxy against the test DAPR sidecar endpoint.</summary>
    /// <param name="tenantId">Tenant identifier.</param>
    /// <returns>The actor proxy.</returns>
    public ITenantConfigurationActor CreateTenantConfigurationActorProxy(string tenantId)
        => CreateActorProxy<ITenantConfigurationActor>(tenantId, nameof(TenantConfigurationActor));

    /// <summary>Creates a corpus-statistics actor proxy against the test DAPR sidecar endpoint.</summary>
    /// <param name="tenantId">Tenant identifier.</param>
    /// <returns>The actor proxy.</returns>
    public ICorpusStatisticsActor CreateCorpusStatisticsActorProxy(string tenantId)
        => CreateActorProxy<ICorpusStatisticsActor>(tenantId, "CorpusStatisticsActor");

    /// <summary>Restarts the full topology and reconnects all clients.</summary>
    /// <returns>The elapsed warm-restart duration.</returns>
    public async Task<TimeSpan> RestartTopologyAsync()
    {
        using var cts = new CancellationTokenSource(TopologyStartupTimeout);
        Stopwatch stopwatch = Stopwatch.StartNew();
        await DisposeTopologyAsync(cts.Token).ConfigureAwait(false);
        await StartTopologyAsync(cts.Token).ConfigureAwait(false);
        stopwatch.Stop();
        return stopwatch.Elapsed;
    }

    /// <inheritdoc/>
    public async ValueTask InitializeAsync()
    {
        _daprAppId = $"memories-it-{Guid.NewGuid():N}";
        _redisVolumeName = $"hexalith-memories-it-{Guid.NewGuid():N}";
        _falkorVolumeName = $"hexalith-memories-falkor-it-{Guid.NewGuid():N}";
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
            _fakeEmbeddingScope = EnvVarScope.Set(
                "Memories__Testing__UseFakeEmbedding",
                _providerMode == EmbeddingProviderTestMode.GoogleFake ? "true" : "false");
            _inMemoryCommandStoreScope = EnvVarScope.Set("Memories__Testing__UseInMemoryCommandStore", "true");
            _allowPrivateHostsScope = EnvVarScope.Set("Ingestion__UrlFetcher__AllowPrivateHosts", "true");
            _daprAppIdScope = EnvVarScope.Set("MEMORIES_DAPR_APP_ID", _daprAppId);
            _redisVolumeNameScope = EnvVarScope.Set("MEMORIES_REDIS_VOLUME_NAME", _redisVolumeName);
            _falkorVolumeNameScope = EnvVarScope.Set("MEMORIES_FALKOR_VOLUME_NAME", _falkorVolumeName);
            _eventStoreSourceMapScope = EnvVarScope.Set(
                "EventStoreIntegration__Routing__SourceToTenantMap__enterprise.claims",
                _eventStoreMappedTenantId);
            _enableKeycloakScope = EnvVarScope.Set("EnableKeycloak", "false");
            _workflowReplaySafetyScope = EnvVarScope.Set("WorkflowReplaySafety__Enabled", "false");

            WriteLocalDaprSecretIfNeeded();
            _daprConfigPathScope = CreateDaprConfigOverrideIfNeeded();

            using var cts = new CancellationTokenSource(TopologyStartupTimeout);
            await StartTopologyAsync(cts.Token).ConfigureAwait(false);
        }
        catch
        {
            DisposeEnvVarScopes();
            RestoreLocalDaprSecret();
            DeleteTempDaprConfig();
            throw;
        }
    }

    private void DisposeEnvVarScopes()
    {
        _eventStoreSourceMapScope?.Dispose();
        _eventStoreSourceMapScope = null;
        _enableKeycloakScope?.Dispose();
        _enableKeycloakScope = null;
        _workflowReplaySafetyScope?.Dispose();
        _workflowReplaySafetyScope = null;
        _redisVolumeNameScope?.Dispose();
        _redisVolumeNameScope = null;
        _falkorVolumeNameScope?.Dispose();
        _falkorVolumeNameScope = null;
        _daprAppIdScope?.Dispose();
        _daprAppIdScope = null;
        _daprConfigPathScope?.Dispose();
        _daprConfigPathScope = null;
        _allowPrivateHostsScope?.Dispose();
        _allowPrivateHostsScope = null;
        _inMemoryCommandStoreScope?.Dispose();
        _inMemoryCommandStoreScope = null;
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
    /// Provisions a new tenant via <c>POST /api/v1/tenants</c> and waits for it to reach
    /// <see cref="TenantStatus.Active"/>. Use in tests that need a tenant in place before
    /// calling case, search, or ingestion endpoints — <see cref="Hexalith.Memories.Server.Tenants.TenantStatusGuard"/>
    /// rejects operations against unknown or non-Active tenants with 404/409.
    /// </summary>
    /// <param name="tenantId">Optional tenant identifier. When null, a random one is generated.</param>
    /// <param name="displayName">Optional display name. Defaults to the tenant id.</param>
    /// <param name="vectorDimensions">Optional semantic vector dimensions to provision for the tenant.</param>
    /// <param name="activationTimeout">Max wait for Active status. Defaults to <see cref="DefaultTenantActivationTimeout"/>.</param>
    /// <param name="cancellationToken">Cooperative cancellation.</param>
    /// <returns>The provisioned tenant id.</returns>
    public async Task<string> ProvisionActiveTenantAsync(
        string? tenantId = null,
        string? displayName = null,
        int? vectorDimensions = null,
        TimeSpan? activationTimeout = null,
        CancellationToken cancellationToken = default)
    {
        int logStartIndex = _logProvider.Count;
        string id = tenantId ?? $"tenant-it-{Guid.NewGuid():N}";
        string name = displayName ?? $"Tenant {id}";

        TenantProvisioningInput payload = new(id, name);
        if (vectorDimensions is { } dimensions)
        {
            payload = payload with { VectorDimensions = dimensions };
        }

        using HttpResponseMessage provisionResponse = await MemoriesClient.PostAsJsonAsync(
            "/api/v1/tenants",
            payload,
            MemoriesJsonContext.Options,
            cancellationToken).ConfigureAwait(false);

        // 202 Accepted on fresh provision; 409 Conflict when the caller passed a pre-existing id —
        // treat both as "tenant exists" so callers can idempotently re-use a deterministic id.
        if (provisionResponse.StatusCode is not (HttpStatusCode.Accepted or HttpStatusCode.Conflict))
        {
            string body = await provisionResponse.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            throw new InvalidOperationException(
                $"Unexpected POST /api/v1/tenants response for '{id}': {(int)provisionResponse.StatusCode} {provisionResponse.ReasonPhrase}. Body: {body}");
        }

        await WaitForTenantActiveAsync(
            id,
            activationTimeout ?? DefaultTenantActivationTimeout,
            logStartIndex,
            cancellationToken).ConfigureAwait(false);
        return id;
    }

    /// <summary>
    /// Seeds a tenant registry entry without provisioning backend indexes. Use this for endpoint
    /// tests that need a specific lifecycle state such as Provisioning or Failed.
    /// </summary>
    /// <param name="tenantId">Tenant identifier.</param>
    /// <param name="status">Lifecycle status to write.</param>
    /// <param name="workflowInstanceId">Optional workflow instance id associated with the state.</param>
    /// <param name="cancellationToken">Cooperative cancellation.</param>
    public async Task SeedTenantRegistryEntryAsync(
        string tenantId,
        TenantStatus status,
        string? workflowInstanceId = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        HttpClient client = _daprStateClient
            ?? throw new InvalidOperationException("DAPR state client is unavailable before the topology has started.");

        DateTimeOffset now = DateTimeOffset.UtcNow;
        TenantRegistryEntry entry = new(
            new TenantInfo(tenantId, $"Tenant {tenantId}", status, now),
            workflowInstanceId,
            now);

        await SaveDaprStateAsync(
            client,
            $"tenant-registry-{tenantId}",
            PersistenceModelMapper.ToStored(entry),
            cancellationToken).ConfigureAwait(false);

        List<string> index = await GetDaprStateAsync<List<string>>(
            client,
            TenantRegistryIndexKey,
            cancellationToken).ConfigureAwait(false) ?? [];
        if (!index.Contains(tenantId, StringComparer.Ordinal))
        {
            index.Add(tenantId);
            await SaveDaprStateAsync(
                client,
                TenantRegistryIndexKey,
                index,
                cancellationToken).ConfigureAwait(false);
        }
    }

    /// <summary>Polls <c>GET /api/v1/tenants/{tenantId}</c> until the tenant reports <see cref="TenantStatus.Active"/>.</summary>
    /// <param name="tenantId">Tenant identifier.</param>
    /// <param name="timeout">Max wait duration.</param>
    /// <param name="cancellationToken">Cooperative cancellation.</param>
    public async Task WaitForTenantActiveAsync(
        string tenantId,
        TimeSpan? timeout = null,
        CancellationToken cancellationToken = default)
        => await WaitForTenantActiveAsync(
            tenantId,
            timeout,
            _logProvider.Count,
            cancellationToken).ConfigureAwait(false);

    private async Task WaitForTenantActiveAsync(
        string tenantId,
        TimeSpan? timeout,
        int logStartIndex,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);

        TimeSpan budget = timeout ?? DefaultTenantActivationTimeout;
        DateTimeOffset deadline = DateTimeOffset.UtcNow.Add(budget);
        HttpStatusCode? lastStatusCode = null;
        TenantStatus? lastTenantStatus = null;
        Exception? lastException = null;

        while (DateTimeOffset.UtcNow < deadline)
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                using HttpResponseMessage tenantResponse = await MemoriesClient.GetAsync(
                    $"/api/v1/tenants/{tenantId}",
                    cancellationToken).ConfigureAwait(false);
                lastStatusCode = tenantResponse.StatusCode;
                if (tenantResponse.StatusCode == HttpStatusCode.OK)
                {
                    TenantInfo? tenant = await tenantResponse.Content.ReadFromJsonAsync<TenantInfo>(
                        MemoriesJsonContext.Options,
                        cancellationToken).ConfigureAwait(false);
                    lastTenantStatus = tenant?.Status;
                    if (tenant?.Status == TenantStatus.Active)
                    {
                        return;
                    }

                    if (tenant?.Status is TenantStatus.Failed or TenantStatus.CompensationFailed)
                    {
                        throw new InvalidOperationException(
                            $"Tenant '{tenantId}' entered terminal provisioning state {tenant.Status} before becoming Active."
                            + $"{Environment.NewLine}{FormatRecentLogs(_logProvider.GetEntriesSince(logStartIndex), maxLines: 40)}");
                    }
                }
            }
            catch (InvalidOperationException)
            {
                throw;
            }
            catch (Exception ex)
            {
                lastException = ex;
            }

            await Task.Delay(TimeSpan.FromMilliseconds(500), cancellationToken).ConfigureAwait(false);
        }

        throw new TimeoutException(
            $"Tenant '{tenantId}' did not reach Active state within {budget}. " +
            $"Last status: {lastStatusCode?.ToString() ?? "n/a"}. " +
            $"Last tenant status: {lastTenantStatus?.ToString() ?? "n/a"}. " +
            $"Last error: {FormatException(lastException)}." +
            $"{Environment.NewLine}{FormatRecentLogs(_logProvider.GetEntriesSince(logStartIndex), maxLines: 40)}");
    }

    /// <summary>Stops the FalkorDB resource in place through the Aspire resource command service.</summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task that completes when Aspire reports the resource stopped.</returns>
    public async Task StopFalkorDbContainerAsync(CancellationToken cancellationToken = default)
    {
        DistributedApplication app = _app
            ?? throw new InvalidOperationException("The Aspire topology has not been started.");
        using CancellationTokenSource commandCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        commandCts.CancelAfter(ResourceHealthyTimeout);
        bool stopCommandSucceeded = false;

        try
        {
            await ExecuteResourceCommandAsync(
                app,
                "memories-graphs",
                KnownResourceCommands.StopCommand,
                commandCts.Token).ConfigureAwait(false);
            stopCommandSucceeded = true;
            _ = await app.ResourceNotifications
                .WaitForResourceAsync(
                    "memories-graphs",
                    [KnownResourceStates.Exited, KnownResourceStates.Finished],
                    commandCts.Token)
                .ConfigureAwait(false);
        }
        catch (Exception stopException) when (stopCommandSucceeded)
        {
            try
            {
                await RecoverFalkorDbAfterPartialStopAsync(app).ConfigureAwait(false);
            }
            catch (Exception recoveryException)
            {
                throw new AggregateException(
                    "FalkorDB stop convergence failed after Aspire accepted the command, and automatic recovery also failed.",
                    stopException,
                    recoveryException);
            }

            throw;
        }
    }

    /// <summary>Reads the durable tenant registry entry directly through the DAPR state API.</summary>
    /// <param name="tenantId">Tenant identifier.</param>
    /// <param name="cancellationToken">Cooperative cancellation.</param>
    /// <returns>The durable entry, or <see langword="null"/> when absent.</returns>
    public async Task<TenantRegistryEntry?> GetTenantRegistryEntryAsync(
        string tenantId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        HttpClient client = _daprStateClient
            ?? throw new InvalidOperationException("DAPR state client is unavailable before the topology has started.");
        return await GetDaprStateAsync<TenantRegistryEntry>(
            client,
            $"tenant-registry-{tenantId}",
            cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Starts the stopped FalkorDB resource in place and waits for backend and API recovery.</summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task that completes once the resource, connection, and Memories readiness endpoint recover.</returns>
    public async Task StartFalkorDbContainerAsync(CancellationToken cancellationToken = default)
    {
        DistributedApplication app = _app
            ?? throw new InvalidOperationException("The Aspire topology has not been started.");
        using CancellationTokenSource commandCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        commandCts.CancelAfter(ResourceHealthyTimeout);

        await ExecuteResourceCommandAsync(
            app,
            "memories-graphs",
            KnownResourceCommands.StartCommand,
            commandCts.Token).ConfigureAwait(false);
        _ = await app.ResourceNotifications
            .WaitForResourceHealthyAsync("memories-graphs", commandCts.Token)
            .ConfigureAwait(false);

        await WaitForFalkorConnectionAsync(commandCts.Token).ConfigureAwait(false);
        await WaitForEndpointAsync(
            MemoriesClient,
            "/ready",
            [HttpStatusCode.OK],
            ResourceHealthyTimeout,
            EndpointPollInterval,
            _logProvider.Count,
            commandCts.Token).ConfigureAwait(false);
    }

    private async Task RecoverFalkorDbAfterPartialStopAsync(DistributedApplication app)
    {
        using CancellationTokenSource recoveryCts = new(ResourceHealthyTimeout);
        try
        {
            _ = await app.ResourceNotifications
                .WaitForResourceAsync(
                    "memories-graphs",
                    [KnownResourceStates.Exited, KnownResourceStates.Finished],
                    recoveryCts.Token)
                .ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (
            app.ResourceNotifications.TryGetCurrentState("memories-graphs", out ResourceEvent? current) &&
            string.Equals(current?.Snapshot.State?.Text, KnownResourceStates.Running, StringComparison.Ordinal))
        {
            using CancellationTokenSource readinessCts = new(ResourceHealthyTimeout);
            _ = await app.ResourceNotifications
                .WaitForResourceHealthyAsync("memories-graphs", readinessCts.Token)
                .ConfigureAwait(false);
            await WaitForFalkorConnectionAsync(readinessCts.Token).ConfigureAwait(false);
            await WaitForEndpointAsync(
                MemoriesClient,
                "/ready",
                [HttpStatusCode.OK],
                ResourceHealthyTimeout,
                EndpointPollInterval,
                _logProvider.Count,
                readinessCts.Token).ConfigureAwait(false);
            return;
        }

        await StartFalkorDbContainerAsync(CancellationToken.None).ConfigureAwait(false);
    }

    /// <summary>Stops the Dapr sidecar process for the Memories Server resource.</summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task that completes when the sidecar has stopped.</returns>
    public Task StopDaprSidecarAsync(CancellationToken cancellationToken = default)
        => StopProcessListeningOnPortAsync(
            DaprSidecarHttpEndpoint.Port,
            "Memories Server Dapr sidecar",
            cancellationToken);

    /// <inheritdoc/>
    public async ValueTask DisposeAsync()
    {
        await DisposeTopologyAsync(CancellationToken.None).ConfigureAwait(false);
        DisposeEnvVarScopes();
        RestoreLocalDaprSecret();
        DeleteTempDaprConfig();
        await RemoveDockerVolumeIfPresentAsync(_falkorVolumeName).ConfigureAwait(false);
        await RemoveDockerVolumeIfPresentAsync(_redisVolumeName).ConfigureAwait(false);
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

    private void WriteLocalDaprSecretIfNeeded()
    {
        if (_providerMode != EmbeddingProviderTestMode.OllamaOidcFake || _embeddingProviderSecret is null)
        {
            return;
        }

        // Snapshot existence and content BEFORE storing the path so a read failure cannot cause
        // RestoreLocalDaprSecret to delete a pre-existing user secrets.json (data loss).
        string secretsPath = Path.Combine(RepositoryRootLocator.Resolve(), "secrets.json");
        bool existed = File.Exists(secretsPath);
        string? originalContent = existed ? File.ReadAllText(secretsPath) : null;

        JsonObject root;
        if (existed && !string.IsNullOrWhiteSpace(originalContent))
        {
            JsonNode? parsed = JsonNode.Parse(originalContent);
            // JsonNode.AsObject() throws when the root is an array or primitive; cast as JsonObject
            // and surface a clear error so we never silently overwrite a non-object secrets file.
            root = parsed as JsonObject
                ?? throw new InvalidOperationException(
                    $"'{secretsPath}' must be a JSON object at its root for DAPR local secret-store; found {parsed?.GetType().Name ?? "null"}.");
        }
        else
        {
            root = [];
        }

        root[_embeddingProviderSecret.Name] = _embeddingProviderSecret.Value;

        _secretsFilePath = secretsPath;
        _secretsFileExisted = existed;
        _originalSecretsFileContent = originalContent;
        File.WriteAllText(
            secretsPath,
            root.ToJsonString(new JsonSerializerOptions { WriteIndented = true }) + Environment.NewLine);
    }

    private void RestoreLocalDaprSecret()
    {
        if (_secretsFilePath is null)
        {
            return;
        }

        if (_secretsFileExisted)
        {
            File.WriteAllText(_secretsFilePath, _originalSecretsFileContent ?? string.Empty);
        }
        else if (File.Exists(_secretsFilePath))
        {
            File.Delete(_secretsFilePath);
        }

        _secretsFilePath = null;
        _originalSecretsFileContent = null;
        _secretsFileExisted = false;
    }

    private EnvVarScope? CreateDaprConfigOverrideIfNeeded()
    {
        if (_providerMode != EmbeddingProviderTestMode.OllamaOidcFake || _embeddingProviderSecret is null)
        {
            return null;
        }

        string tempDirectory = Path.Combine(Path.GetTempPath(), "hexalith-memories-dapr", _daprAppId);
        Directory.CreateDirectory(tempDirectory);
        _tempDaprConfigPath = Path.Combine(tempDirectory, "config.yaml");
        File.WriteAllText(
            _tempDaprConfigPath,
            $$"""
            apiVersion: dapr.io/v1alpha1
            kind: Configuration
            metadata:
              name: memories-config
            spec:
              secrets:
                scopes:
                  - storeName: secretstore
                    defaultAccess: deny
                    allowedSecrets:
                      - embedding-api-key
                      - llm-secret
                      - {{_embeddingProviderSecret.Name}}
            """);

        return EnvVarScope.Set("MEMORIES_DAPR_CONFIG_PATH", _tempDaprConfigPath);
    }

    private void DeleteTempDaprConfig()
    {
        DeleteFixtureOwnedTempDaprDirectory(_tempDaprConfigPath, _daprAppId);
        _tempDaprConfigPath = null;
    }

    /// <summary>
    /// Removes the fixture-owned <c>%TEMP%/hexalith-memories-dapr/{daprAppId}</c> directory
    /// (containing <c>config.yaml</c> plus any AppHost-generated component yamls). The caller's
    /// <paramref name="fixtureAppId"/> must match the leaf directory name; otherwise the directory
    /// is left in place. The shared parent <c>%TEMP%/hexalith-memories-dapr</c> is never deleted.
    /// </summary>
    /// <param name="configFilePath">Path to <c>config.yaml</c>, or <c>null</c> if it was never set.</param>
    /// <param name="fixtureAppId">The fixture-owned DAPR app id used as the leaf directory name.</param>
    internal static void DeleteFixtureOwnedTempDaprDirectory(string? configFilePath, string? fixtureAppId)
    {
        if (string.IsNullOrEmpty(configFilePath))
        {
            return;
        }

        string? parentDir = Path.GetDirectoryName(configFilePath);
        if (string.IsNullOrEmpty(parentDir) || string.IsNullOrEmpty(fixtureAppId))
        {
            return;
        }

        // Anchor cleanup to the fixture-owned leaf so a misconfigured caller cannot accidentally
        // delete the shared %TEMP%/hexalith-memories-dapr root or anything outside its own dir.
        if (!string.Equals(Path.GetFileName(parentDir), fixtureAppId, StringComparison.Ordinal))
        {
            return;
        }

        string expectedParentDir = Path.Combine(Path.GetTempPath(), "hexalith-memories-dapr", fixtureAppId);
        if (!string.Equals(
            Path.GetFullPath(parentDir),
            Path.GetFullPath(expectedParentDir),
            StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        if (File.Exists(configFilePath))
        {
            try
            {
                File.Delete(configFilePath);
            }
            catch (IOException)
            {
                // Best-effort cleanup; another process may still hold a handle to config.yaml.
            }
            catch (UnauthorizedAccessException)
            {
                // Same rationale as IOException.
            }
        }

        if (!Directory.Exists(parentDir))
        {
            return;
        }

        try
        {
            Directory.Delete(parentDir, recursive: true);
        }
        catch (IOException)
        {
            // AppHost-generated component yamls may briefly be locked during teardown. The temp
            // root is already namespaced per-fixture, so leftovers do not leak across fixtures.
        }
        catch (UnauthorizedAccessException)
        {
            // Same rationale as IOException.
        }
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
            .WaitForResourceHealthyAsync("memories", cancellationToken)
            .WaitAsync(ResourceHealthyTimeout, cancellationToken)
            .ConfigureAwait(false);

        // The Memories Server enforces a fallback authentication policy (Story 20.1) plus a per-tenant
        // authorization filter (Story 20.2), so every non-anonymous call needs a server-realm bearer whose
        // tenant claim matches the accessed tenant. Reuse Aspire's endpoint resolution for the base address, then
        // route requests through ServerBearerAuthHandler, which mints and attaches that token per request.
        Uri memoriesBaseAddress;
        using (HttpClient endpointProbe = _app.CreateHttpClient("memories"))
        {
            memoriesBaseAddress = endpointProbe.BaseAddress
                ?? throw new InvalidOperationException("The 'memories' resource did not expose an HTTP endpoint.");
        }

        MemoriesClient = new HttpClient(new ServerBearerAuthHandler(new HttpClientHandler()))
        {
            BaseAddress = memoriesBaseAddress,
            Timeout = TimeSpan.FromSeconds(60),
        };

        // The AppHost resource-health wait above is the backend readiness gate. Use the liveness
        // endpoint here so a slow aggregate health check cannot fail fixture initialization for
        // unrelated API tests.
        await WaitForEndpointAsync(
            MemoriesClient,
            "/alive",
            [HttpStatusCode.OK],
            EndpointReadyTimeout,
            EndpointPollInterval,
            logStartIndex,
            cancellationToken).ConfigureAwait(false);

        // Story 10.1 — wait for the MCP service and expose its endpoint + client. The upstream
        // Memories Server checks remain covered by MCP API tests; fixture startup only needs the
        // MCP HTTP surface and sidecar to be reachable.
        _ = await _app.ResourceNotifications
            .WaitForResourceHealthyAsync("memories-mcp", cancellationToken)
            .WaitAsync(ResourceHealthyTimeout, cancellationToken)
            .ConfigureAwait(false);

        McpClient = _app.CreateHttpClient("memories-mcp");
        McpClient.Timeout = TimeSpan.FromSeconds(60);
        McpEndpoint = _app.GetEndpoint("memories-mcp", "http");

        await WaitForEndpointAsync(
            McpClient,
            "/alive",
            [HttpStatusCode.OK],
            EndpointReadyTimeout,
            EndpointPollInterval,
            logStartIndex,
            cancellationToken).ConfigureAwait(false);

        DaprSidecarHttpEndpoint = ResolveDaprSidecarHttpEndpoint(logStartIndex);
        _daprStateClient = new HttpClient
        {
            BaseAddress = DaprSidecarHttpEndpoint,
            Timeout = TimeSpan.FromSeconds(30),
        };
        string? daprApiToken = Environment.GetEnvironmentVariable("DAPR_API_TOKEN");
        if (!string.IsNullOrWhiteSpace(daprApiToken))
        {
            _daprStateClient.DefaultRequestHeaders.TryAddWithoutValidation("dapr-api-token", daprApiToken);
        }

        Uri redisEndpoint = _app.GetEndpoint("memories-vectors", "redis");
        Uri falkorEndpoint = _app.GetEndpoint("memories-graphs", "falkordb");

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
    /// category names with either the resource id ("memories") or the AppHost resource-log
    /// category prefix ("Hexalith.Memories.AppHost.Resources.memories"), followed by either
    /// the end of the category or a "-" / "." separator (for related sub-resources such as
    /// "memories-dapr-cli"). A raw substring <c>Contains</c> match is too broad — any unrelated
    /// future test-runner category that happens to embed "memories" would be elevated above the
    /// Warning floor and add noise to the captured stream.
    /// </summary>
    /// <param name="category">The logger category name as provided by the logging pipeline.</param>
    /// <returns><c>true</c> when the category identifies the Memories Server resource or one of its sub-resources.</returns>
    private static bool IsMemoriesServerCategory(string? category)
    {
        if (string.IsNullOrEmpty(category))
        {
            return false;
        }

        const string resourceId = "memories";
        const string aspireResourceMarker = ".Resources." + resourceId;
        // Anchor the resource match to two well-known shapes so an unrelated category like
        // `Foo.Bar.SomeResource.memories` cannot collide with the real resource:
        //   1. Direct resource category: `memories[.|-]<sub>` (or exact match).
        //   2. AppHost resource category: `<assembly>.Resources.memories[.|-]<sub>`.
        int resourceIndex;
        if (category.StartsWith(resourceId, StringComparison.OrdinalIgnoreCase))
        {
            resourceIndex = 0;
        }
        else
        {
            resourceIndex = category.IndexOf(aspireResourceMarker, StringComparison.OrdinalIgnoreCase);
        }

        if (resourceIndex < 0)
        {
            return false;
        }

        int suffixStart = resourceIndex == 0
            ? resourceId.Length
            : resourceIndex + aspireResourceMarker.Length;

        if (category.Length == suffixStart)
        {
            return true;
        }

        char next = category[suffixStart];
        return next is '-' or '.';
    }

    private Uri ResolveDaprSidecarHttpEndpoint(int logStartIndex)
    {
        try
        {
            return ResolveDaprSidecarHttpEndpoint(_logProvider.GetEntriesSince(logStartIndex));
        }
        catch (InvalidOperationException)
        {
            // Fall back to Aspire resource endpoints when the DAPR CLI log line is unavailable.
        }

        if (_app is not null)
        {
            foreach (string resourceName in new[] { "memories-dapr", "memories-dapr-cli" })
            {
                try
                {
                    return _app.GetEndpoint(resourceName, "http");
                }
                catch (ArgumentException)
                {
                    // Continue probing known sidecar resource names.
                }
            }
        }

        throw new InvalidOperationException(
            "Could not determine the Memories Server Dapr sidecar HTTP endpoint from Aspire resources or captured logs.");
    }

    private static Uri ResolveDaprSidecarHttpEndpoint(IReadOnlyList<CapturedLogEntry> entries)
    {
        ArgumentNullException.ThrowIfNull(entries);

        for (int i = entries.Count - 1; i >= 0; i--)
        {
            CapturedLogEntry entry = entries[i];
            if (!entry.Category.Contains("memories-dapr-cli", StringComparison.OrdinalIgnoreCase))
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

    private static async Task SaveDaprStateAsync<T>(
        HttpClient client,
        string key,
        T value,
        CancellationToken cancellationToken)
    {
        using MemoryStream stream = new();
        await using (Utf8JsonWriter writer = new(stream))
        {
            writer.WriteStartArray();
            writer.WriteStartObject();
            writer.WriteString("key", key);
            writer.WritePropertyName("value");
            JsonSerializer.Serialize(writer, value, MemoriesJsonContext.Options);
            writer.WriteEndObject();
            writer.WriteEndArray();
        }

        using ByteArrayContent content = new(stream.ToArray());
        content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");
        using HttpResponseMessage response = await client.PostAsync(
            $"/v1.0/state/{StateStoreName}",
            content,
            cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
    }

    private static async Task<T?> GetDaprStateAsync<T>(
        HttpClient client,
        string key,
        CancellationToken cancellationToken)
    {
        using HttpResponseMessage response = await client.GetAsync(
            $"/v1.0/state/{StateStoreName}/{Uri.EscapeDataString(key)}",
            cancellationToken).ConfigureAwait(false);
        if (response.StatusCode == HttpStatusCode.NoContent || response.StatusCode == HttpStatusCode.NotFound)
        {
            return default;
        }

        response.EnsureSuccessStatusCode();
        string json = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        return string.IsNullOrWhiteSpace(json)
            ? default
            : JsonSerializer.Deserialize<T>(json, MemoriesJsonContext.Options);
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

        _daprStateClient?.Dispose();
        _daprStateClient = null;

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
        process.Kill(entireProcessTree: false);
        try
        {
            await process.WaitForExitAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            if (!process.HasExited)
            {
                try
                {
                    process.Kill(entireProcessTree: true);
                }
                catch (InvalidOperationException)
                {
                    // The process exited between the state check and Kill.
                }
            }

            try
            {
                await process.WaitForExitAsync(CancellationToken.None)
                    .WaitAsync(TimeSpan.FromSeconds(5))
                    .ConfigureAwait(false);
            }
            catch (TimeoutException)
            {
                // Preserve the caller's cancellation; teardown diagnostics identify the command.
            }

            throw;
        }
    }

    private static async Task ExecuteResourceCommandAsync(
        DistributedApplication app,
        string resourceName,
        string commandName,
        CancellationToken cancellationToken)
    {
        ExecuteCommandResult result = await app.ResourceCommands
            .ExecuteCommandAsync(resourceName, commandName, cancellationToken)
            .ConfigureAwait(false);
        if (!result.Success)
        {
            throw new InvalidOperationException(
                $"Aspire command '{commandName}' failed for resource '{resourceName}': " +
                (result.Message ?? "no command detail was returned"));
        }
    }

    private async Task WaitForFalkorConnectionAsync(CancellationToken cancellationToken)
    {
        Exception? lastException = null;
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                _ = await FalkorDbConnection.GetDatabase().PingAsync().ConfigureAwait(false);
                return;
            }
            catch (Exception ex) when (ex is RedisConnectionException or RedisTimeoutException)
            {
                lastException = ex;
            }

            await Task.Delay(EndpointPollInterval, cancellationToken).ConfigureAwait(false);
        }

        throw new TimeoutException(
            $"FalkorDB connection did not recover before the resource-command deadline. Last error: {FormatException(lastException)}.");
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

    private static async Task RemoveDockerVolumeIfPresentAsync(string volumeName)
    {
        if (string.IsNullOrWhiteSpace(volumeName))
        {
            return;
        }

        try
        {
            using CancellationTokenSource cleanupCts = new(DockerVolumeCleanupTimeout);
            _ = await RunProcessCommandAsync(
                "docker",
                $"volume rm {volumeName}",
                cleanupCts.Token).ConfigureAwait(false);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("No such volume", StringComparison.OrdinalIgnoreCase))
        {
        }
        catch (OperationCanceledException ex)
        {
            throw new TimeoutException(
                $"Docker volume removal timed out after {DockerVolumeCleanupTimeout} for fixture-owned volume '{volumeName}'.",
                ex);
        }
    }

    private async Task WaitForEndpointAsync(
        HttpClient client,
        string url,
        IReadOnlyCollection<HttpStatusCode> expectedStatusCodes,
        TimeSpan timeout,
        TimeSpan pollInterval,
        int logStartIndex,
        CancellationToken cancellationToken)
    {
        DateTimeOffset deadline = DateTimeOffset.UtcNow.Add(timeout);
        Exception? lastException = null;
        HttpStatusCode? lastStatusCode = null;

        while (DateTimeOffset.UtcNow < deadline)
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                using CancellationTokenSource probeCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                probeCts.CancelAfter(EndpointProbeTimeout);
                using HttpResponseMessage response = await client.GetAsync(url, probeCts.Token).ConfigureAwait(false);
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

            await Task.Delay(pollInterval, cancellationToken).ConfigureAwait(false);
        }

        throw new TimeoutException(
            $"Endpoint '{url}' did not become ready within {timeout}. " +
            $"Last status: {lastStatusCode?.ToString() ?? "n/a"}. " +
            $"Last error: {FormatException(lastException)}." +
            $"{Environment.NewLine}{FormatRecentLogs(_logProvider.GetEntriesSince(logStartIndex), maxLines: 40)}");
    }

    private static string FormatException(Exception? exception)
        => exception is null
            ? "n/a"
            : $"{exception.GetType().Name}: {exception.Message}";

    private static string FormatRecentLogs(IReadOnlyList<CapturedLogEntry> entries, int maxLines)
    {
        if (entries.Count == 0)
        {
            return "Recent captured logs: n/a";
        }

        IEnumerable<CapturedLogEntry> recent = entries
            .Skip(Math.Max(0, entries.Count - maxLines));

        return "Recent captured logs:" + Environment.NewLine + string.Join(
            Environment.NewLine,
            recent.Select(e => $"[{e.Level}] {e.Category}: {e.Message}"));
    }

    /// <summary>Represents a captured integration-test log entry.</summary>
    public sealed record CapturedLogEntry(LogLevel Level, string Category, string Message);


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
            {
                string message = formatter(state, exception);
                if (exception is not null)
                {
                    message += $" | {exception.GetType().Name}: {exception.Message}";
                }

                owner.Add(new CapturedLogEntry(logLevel, categoryName, message));
            }
        }
    }
}

[CollectionDefinition("AspireIngestionPipeline", DisableParallelization = true)]
public sealed class AspireIngestionPipelineCollection : ICollectionFixture<AspireIngestionPipelineFixture>;
