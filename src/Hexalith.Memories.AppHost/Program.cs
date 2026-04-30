using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Eventing;
using CommunityToolkit.Aspire.Hosting.Dapr;
using System.Net.Sockets;

IDistributedApplicationBuilder builder = DistributedApplication.CreateBuilder(args);
string secretsFile = EnsureSecretsFile();
string daprConfigPath = ResolveDaprConfigPath();
string daprAppId = ResolveDaprAppId();
string redisConfigPath = ResolveRedisConfigPath();
string redisVolumeName = ResolveRedisVolumeName();
GeneratedDaprComponentPaths daprComponentPaths = EnsureDaprComponentFiles(daprAppId, secretsFile);
string? daprPlacementHostAddress = ResolveOptionalEnvironmentValue("MEMORIES_DAPR_PLACEMENT_HOST_ADDRESS");
string? daprSchedulerHostAddress = ResolveOptionalEnvironmentValue("MEMORIES_DAPR_SCHEDULER_HOST_ADDRESS");

// Story 5.4 AC3 — DAPR API token authentication.
//
// Tokens are only wired when DAPR_API_TOKEN_MODE=enabled is set in the environment (production/staging).
// They stay disabled for local development and for the Aspire integration-test fixture so the 39+ existing
// integration tests continue to pass without needing to inject a token into every request. The sidecar
// validates incoming app-to-sidecar calls using DAPR_API_TOKEN; the application validates incoming
// sidecar-to-app calls using APP_API_TOKEN. In production both tokens must be injected via Kubernetes
// Secrets / platform secret manager and the application port must NOT be exposed externally — direct
// access to the app port bypasses the token check. Story D8 (Phase 1.5) adds a proper
// TenantAuthorizationMiddleware for external callers.
(string? daprApiToken, string? appApiToken) = ResolveDaprApiTokens();
ApplyProcessEnvironmentTokens(daprApiToken, appApiToken);
builder.Eventing.Subscribe<BeforeResourceStartedEvent>(async (@event, cancellationToken) =>
{
    if (@event.Resource.Name is "memories-server-dapr" or "memories-server-dapr-cli" or
        "memories-mcp-dapr" or "memories-mcp-dapr-cli")
    {
        await WaitForRedisPingAsync("127.0.0.1", 6379, TimeSpan.FromMinutes(2), cancellationToken)
            .ConfigureAwait(false);
    }
});

// Story 6.4: make Redis durability explicit instead of relying on image defaults.
// The redis/redis-stack image auto-loads /redis-stack.conf from its /entrypoint.sh, so a
// repo-owned config bind-mount plus a named /data volume is enough to enable durable AOF+RDB.
// Tests can override the volume name for isolation via MEMORIES_REDIS_VOLUME_NAME; local/dev
// runs keep a stable named volume so controlled restarts preserve state.
IResourceBuilder<ContainerResource> redis = builder
    .AddContainer("redis", "redis/redis-stack")
    .WithBindMount(redisConfigPath, "/redis-stack.conf", isReadOnly: true)
    .WithVolume(redisVolumeName, "/data")
    .WithEndpoint(port: 6379, targetPort: 6379, name: "redis");
EndpointReference redisEndpoint = redis.GetEndpoint("redis");

IResourceBuilder<IDaprComponentResource> stateStore = builder
    .AddDaprComponent(
        "statestore",
        "state.redis",
        new DaprComponentOptions { LocalPath = daprComponentPaths.StateStore })
    .WaitFor(redis);

// Story 9.1: DAPR pub/sub component shared with the Redis dependency. AppHost emits concrete local
// component YAML for the host-pinned Redis endpoint so local/dev and test topologies cannot drift from
// the runtime broker wiring. Production deployments still bind-mount deploy/dapr/components/pubsub.yaml
// and inject PUBSUB_REDIS_HOST/PUBSUB_REDIS_PASSWORD from secrets.
IResourceBuilder<IDaprComponentResource> pubSub = builder
    .AddDaprComponent(
        "pubsub",
        "pubsub.redis",
        new DaprComponentOptions { LocalPath = daprComponentPaths.PubSub })
    .WaitFor(redis);

IResourceBuilder<IDaprComponentResource> secretStore = builder
    .AddDaprComponent(
        "secretstore",
        "secretstores.local.file",
        new DaprComponentOptions { LocalPath = daprComponentPaths.SecretStore });

// Story 9.2: DAPR Conversation component — drives GenerateNaturalLanguageDescriptionActivity in the
// dual-embedding ingestion path. Dev default is conversation.echo so Aspire/test runs exercise the full
// pipeline deterministically without a real LLM provider; echo returns the input unchanged. Production
// deployments bind-mount deploy/dapr/components/conversation-llm.yaml with a real provider
// (conversation.openai / conversation.anthropic / conversation.googleai) wired to the secretstore.
// The component name "llm" is referenced by NaturalLanguageDescriptionOptions.DaprComponentName and
// asserted NOT to equal "conversation.echo" by the options validator when running in Production.
IResourceBuilder<IDaprComponentResource> conversationLlm = builder
    .AddDaprComponent(
        "llm",
        "conversation.echo",
        new DaprComponentOptions { LocalPath = daprComponentPaths.ConversationLlm });

// FalkorDB: graph database (Redis-protocol compatible, internal port 6379 mapped to 6380)
IResourceBuilder<ContainerResource> falkordb = builder
    .AddContainer("falkordb", "falkordb/falkordb")
    .WithEndpoint(port: 6380, targetPort: 6379, name: "falkordb");
EndpointReference falkordbEndpoint = falkordb.GetEndpoint("falkordb");

// Memories Server with DAPR sidecar
// DAPR sidecar manages connections to Redis/FalkorDB via component config
// AppPort is intentionally omitted so Aspire Testing can auto-detect the
// randomized project port instead of pinning the sidecar to localhost:5000.
IResourceBuilder<ProjectResource> server = builder
    .AddProject<Projects.Hexalith_Memories_Server>("memories-server")
    .WithDaprSidecar(sidecar =>
    {
        _ = sidecar.WithOptions(CreateDaprSidecarOptions(
                appId: daprAppId,
                httpPort: 3500,
                grpcPort: 50001,
                configPath: daprConfigPath,
                placementHostAddress: daprPlacementHostAddress,
                schedulerHostAddress: daprSchedulerHostAddress));
        _ = sidecar.WithReference(stateStore);
        _ = sidecar.WithReference(pubSub);
        _ = sidecar.WithReference(secretStore);
        _ = sidecar.WithReference(conversationLlm);
    })
    .WithEnvironment(
        "ConnectionStrings__redis",
        ReferenceExpression.Create($"{redisEndpoint.Property(EndpointProperty.HostAndPort)}"))
    .WithEnvironment(
        "ConnectionStrings__falkordb",
        ReferenceExpression.Create($"{falkordbEndpoint.Property(EndpointProperty.HostAndPort)}"))
    .WaitFor(redis)
    .WaitFor(falkordb);

#pragma warning disable CS0618 // CommunityToolkit.Aspire.Hosting.Dapr 9.7 reads project-level component references.
server = server
    .WithReference(stateStore)
    .WithReference(pubSub)
    .WithReference(secretStore)
    .WithReference(conversationLlm);
#pragma warning restore CS0618

// Story 6.1: dev-only default allow-list for POST /api/ingest/directory so developers can batch-ingest
// the repo-local test-data/ folder without touching config. Production deployments must NOT rely on this
// — appsettings.json keeps AllowedDirectoryRoots empty, so the endpoint is disabled by default.
string testDataRoot = EnsureTestDataRoot();
server = server.WithEnvironment("Ingestion__AllowedDirectoryRoots__0", testDataRoot);

// Story 9.1: the controller subscription binding uses [Topic("pubsub", "$(MEMORIES_EVENTSTORE_TOPIC)")].
// Keep the runtime env var aligned with the route/topic config so /dapr/subscribe is deterministic.
server = server.WithEnvironment("MEMORIES_EVENTSTORE_TOPIC", "memories-events");

// Story 5.4 AC3 — application-side token injection.
// The AppHost now propagates both APP_API_TOKEN and DAPR_API_TOKEN to the application resource and the
// DAPR sidecar when DAPR_API_TOKEN_MODE=enabled. Both values still come from the ambient environment so
// local development and the Aspire integration-test fixture remain token-free by default.
// Production deployments must inject the token values via Kubernetes Secrets / platform secret manager;
// the application port must never be exposed externally — external traffic must terminate at the sidecar
// for the token check to apply.
if (appApiToken is not null)
{
    server = server.WithEnvironment("APP_API_TOKEN", appApiToken);
}

if (daprApiToken is not null)
{
    server = server.WithEnvironment("DAPR_API_TOKEN", daprApiToken);
}

_ = server;

// Story 10.1 — MCP Server.
//
// Runs as a sibling DAPR service (app-id `memories-mcp`) with its own sidecar pinned to ports
// 3600/50101 so it does not collide with the Memories Server sidecar at 3500/50001. The MCP
// resource intentionally does NOT receive stateStore / pubSub / secretStore / conversationLlm
// references — NFR11 + architecture.md §Cross-Cutting Concerns #4 (DAPR Secrets scoping) keep
// embedding-provider API keys exclusively on the Memories Server. MCP reaches the server via
// DAPR service invocation through its own sidecar.
//
// `WaitFor(server)` blocks the MCP startup probe until the Memories Server health check passes,
// avoiding a flapping `/ready` row in the Aspire Dashboard during cold starts.
IResourceBuilder<ProjectResource> mcp = builder
    .AddProject<Projects.Hexalith_Memories_Mcp>("memories-mcp")
    .WithDaprSidecar(sidecar =>
    {
        _ = sidecar.WithOptions(CreateDaprSidecarOptions(
                appId: "memories-mcp",
                httpPort: 3600,
                grpcPort: 50101,
                configPath: daprConfigPath,
                placementHostAddress: daprPlacementHostAddress,
                schedulerHostAddress: daprSchedulerHostAddress));
    })
    .WithEnvironment("MEMORIES_MCP_UPSTREAM_APP_ID", daprAppId)
    .WaitFor(server);

if (appApiToken is not null)
{
    mcp = mcp.WithEnvironment("APP_API_TOKEN", appApiToken);
}

if (daprApiToken is not null)
{
    mcp = mcp.WithEnvironment("DAPR_API_TOKEN", daprApiToken);
}

mcp = PropagateJwtBearerAuthenticationEnvironment(mcp);

_ = mcp;

builder.Build().Run();

static string EnsureTestDataRoot()
{
    string repoRoot = ResolveRepositoryRoot();
    string testData = Path.Combine(repoRoot, "test-data");
    Directory.CreateDirectory(testData);
    string readme = Path.Combine(testData, "README.md");
    if (!File.Exists(readme))
    {
        File.WriteAllText(
            readme,
            "# test-data\n\nDev-only allow-list root for POST /api/ingest/directory. Safe to add sample files here; the endpoint is still disabled in production by default (appsettings.json AllowedDirectoryRoots=[]).\n");
    }

    return testData;
}

static string EnsureSecretsFile()
{
    string repoRoot = ResolveRepositoryRoot();
    string secretsFile = Path.Combine(repoRoot, "secrets.json");

    if (!File.Exists(secretsFile))
    {
        File.WriteAllText(secretsFile, "{}" + Environment.NewLine);
    }

    return secretsFile;
}

static GeneratedDaprComponentPaths EnsureDaprComponentFiles(string daprAppId, string secretsFile)
{
    string componentsDirectory = Path.Combine(Path.GetTempPath(), "hexalith-memories-dapr", daprAppId);
    Directory.CreateDirectory(componentsDirectory);

    string stateStorePath = Path.Combine(componentsDirectory, "statestore.yaml");
    string pubSubPath = Path.Combine(componentsDirectory, "pubsub.yaml");
    string secretStorePath = Path.Combine(componentsDirectory, "secretstore.yaml");
    string conversationLlmPath = Path.Combine(componentsDirectory, "llm.yaml");

    // AppHost pins Redis to host port 6379 and the Dapr sidecars run as local host processes.
    // Concrete LocalPath components avoid relying on toolkit-generated temp YAML that CI failed to load.
    string redisHost = "127.0.0.1:6379";

    File.WriteAllText(
        stateStorePath,
        $"""
        apiVersion: dapr.io/v1alpha1
        kind: Component
        metadata:
          name: statestore
        spec:
          type: state.redis
          version: v1
          metadata:
            - name: redisHost
              value: "{redisHost}"
            - name: redisPassword
              value: ""
            - name: redisMaxRetries
              value: "60"
            - name: redisMinRetryInterval
              value: "500ms"
            - name: redisMaxRetryInterval
              value: "2s"
            - name: actorStateStore
              value: "true"
        """);

    File.WriteAllText(
        pubSubPath,
        $"""
        apiVersion: dapr.io/v1alpha1
        kind: Component
        metadata:
          name: pubsub
        spec:
          type: pubsub.redis
          version: v1
          metadata:
            - name: redisHost
              value: "{redisHost}"
            - name: redisPassword
              value: ""
            - name: redisMaxRetries
              value: "60"
            - name: redisMinRetryInterval
              value: "500ms"
            - name: redisMaxRetryInterval
              value: "2s"
        """);

    File.WriteAllText(
        secretStorePath,
        $"""
        apiVersion: dapr.io/v1alpha1
        kind: Component
        metadata:
          name: secretstore
        spec:
          type: secretstores.local.file
          version: v1
          metadata:
            - name: secretsFile
              value: "{secretsFile.Replace("\\", "\\\\", StringComparison.Ordinal)}"
            - name: nestedSeparator
              value: ":"
        """);

    File.WriteAllText(
        conversationLlmPath,
        """
        apiVersion: dapr.io/v1alpha1
        kind: Component
        metadata:
          name: llm
        spec:
          type: conversation.echo
          version: v1
          metadata:
            - name: responseCacheTTL
              value: "0s"
            - name: piiScrubbing
              value: "false"
        """);

    return new GeneratedDaprComponentPaths(
        stateStorePath,
        pubSubPath,
        secretStorePath,
        conversationLlmPath);
}

static string ResolveDaprConfigPath()
{
    string repoRoot = ResolveRepositoryRoot();
    string configPath = Path.Combine(repoRoot, "deploy", "dapr", "config.yaml");

    if (!File.Exists(configPath))
    {
        throw new FileNotFoundException(
            "DAPR configuration not found. Ensure deploy/dapr/config.yaml exists.",
            configPath);
    }

    return configPath;
}

static string ResolveRedisConfigPath()
{
    string repoRoot = ResolveRepositoryRoot();
    string configPath = Path.Combine(repoRoot, "deploy", "redis", "redis.conf");

    if (!File.Exists(configPath))
    {
        throw new FileNotFoundException(
            "Redis persistence configuration not found. Ensure deploy/redis/redis.conf exists.",
            configPath);
    }

    // Story 6.4: the redis/redis-stack image silently falls back to in-memory defaults if the bind-mounted
    // config is present but empty or missing the AOF directive — which would make "restart durability"
    // green while actually losing data. Reject that up front so AppHost fails loudly instead.
    string content = File.ReadAllText(configPath);
    if (!content.Contains("appendonly yes", StringComparison.OrdinalIgnoreCase))
    {
        throw new InvalidOperationException(
            $"Redis persistence configuration at '{configPath}' must set 'appendonly yes' to enable AOF durability.");
    }

    return configPath;
}

static string ResolveDaprAppId()
{
    string? configured = Environment.GetEnvironmentVariable("MEMORIES_DAPR_APP_ID");
    return string.IsNullOrWhiteSpace(configured)
        ? "memories-server"
        : configured.Trim();
}

static DaprSidecarOptions CreateDaprSidecarOptions(
    string appId,
    int httpPort,
    int grpcPort,
    string configPath,
    string? placementHostAddress,
    string? schedulerHostAddress)
{
    var options = new DaprSidecarOptions
    {
        AppId = appId,
        DaprHttpPort = httpPort,
        DaprGrpcPort = grpcPort,
        Config = configPath,
        // GitHub Linux runners can resolve localhost to ::1 while the locally initialized DAPR
        // placement/scheduler services listen on IPv4. Keep this opt-in so developer machines and
        // non-default DAPR installs can use the toolkit defaults.
        PlacementHostAddress = placementHostAddress,
        SchedulerHostAddress = schedulerHostAddress,
    };

    return options;
}

static string? ResolveOptionalEnvironmentValue(string name)
{
    string? configured = Environment.GetEnvironmentVariable(name);
    return string.IsNullOrWhiteSpace(configured) ? null : configured.Trim();
}

static string ResolveRedisVolumeName()
{
    string? configured = Environment.GetEnvironmentVariable("MEMORIES_REDIS_VOLUME_NAME");
    return string.IsNullOrWhiteSpace(configured)
        ? "hexalith-memories-redis-data"
        : configured.Trim();
}

static void ApplyProcessEnvironmentTokens(string? daprApiToken, string? appApiToken)
{
    // CommunityToolkit.Aspire.Hosting.Dapr 9.7 / Aspire 13.1 does not expose a sidecar-specific
    // environment-builder API. When token mode is enabled, seed the AppHost process environment so
    // the spawned daprd sidecar inherits the required variables, while still explicitly passing them
    // to the application project resource below.
    if (!string.IsNullOrWhiteSpace(appApiToken))
    {
        Environment.SetEnvironmentVariable("APP_API_TOKEN", appApiToken);
    }

    if (!string.IsNullOrWhiteSpace(daprApiToken))
    {
        Environment.SetEnvironmentVariable("DAPR_API_TOKEN", daprApiToken);
    }
}

static (string? DaprApiToken, string? AppApiToken) ResolveDaprApiTokens()
{
    // Gate on DAPR_API_TOKEN_MODE=enabled so tokens are opt-in. Default (unset) keeps local dev and
    // the integration-test fixture working without token propagation.
    string? mode = Environment.GetEnvironmentVariable("DAPR_API_TOKEN_MODE");
    if (!string.Equals(mode, "enabled", StringComparison.OrdinalIgnoreCase))
    {
        return (null, null);
    }

    string? daprToken = Environment.GetEnvironmentVariable("DAPR_API_TOKEN");
    string? appToken = Environment.GetEnvironmentVariable("APP_API_TOKEN");

    if (string.IsNullOrWhiteSpace(daprToken) || string.IsNullOrWhiteSpace(appToken))
    {
        throw new InvalidOperationException(
            "DAPR_API_TOKEN_MODE=enabled requires both DAPR_API_TOKEN and APP_API_TOKEN environment variables to be set.");
    }

    return (daprToken, appToken);
}

static IResourceBuilder<ProjectResource> PropagateJwtBearerAuthenticationEnvironment(IResourceBuilder<ProjectResource> resource)
{
    string[] keys =
    [
        "Authentication__JwtBearer__Authority",
        "Authentication__JwtBearer__Audience",
        "Authentication__JwtBearer__Issuer",
        "Authentication__JwtBearer__SigningKey",
        "Authentication__JwtBearer__RequireHttpsMetadata",
        "Authentication__JwtBearer__TenantClaimName",
    ];

    foreach (string key in keys)
    {
        string? value = Environment.GetEnvironmentVariable(key);
        if (!string.IsNullOrWhiteSpace(value))
        {
            resource = resource.WithEnvironment(key, value);
        }
    }

    return resource;
}

static async Task WaitForRedisPingAsync(
    string host,
    int port,
    TimeSpan timeout,
    CancellationToken cancellationToken)
{
    DateTimeOffset deadline = DateTimeOffset.UtcNow.Add(timeout);
    Exception? lastError = null;
    byte[] ping = "*1\r\n$4\r\nPING\r\n"u8.ToArray();
    byte[] response = new byte[64];

    while (DateTimeOffset.UtcNow < deadline)
    {
        try
        {
            using CancellationTokenSource attemptCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            attemptCts.CancelAfter(TimeSpan.FromSeconds(2));

            using TcpClient client = new();
            await client.ConnectAsync(host, port, attemptCts.Token)
                .AsTask()
                .ConfigureAwait(false);

            await using NetworkStream stream = client.GetStream();
            await stream.WriteAsync(ping, attemptCts.Token).ConfigureAwait(false);
            int bytesRead = await stream.ReadAsync(response.AsMemory(0, response.Length), attemptCts.Token)
                .ConfigureAwait(false);

            if (bytesRead >= 5 &&
                response[0] == (byte)'+' &&
                response[1] == (byte)'P' &&
                response[2] == (byte)'O' &&
                response[3] == (byte)'N' &&
                response[4] == (byte)'G')
            {
                return;
            }

            throw new InvalidOperationException("Redis did not return PONG to the readiness probe.");
        }
        catch (Exception ex) when (ex is SocketException or TimeoutException or System.IO.IOException or InvalidOperationException ||
                                   (ex is OperationCanceledException && !cancellationToken.IsCancellationRequested))
        {
            lastError = ex;
            await Task.Delay(TimeSpan.FromMilliseconds(500), cancellationToken).ConfigureAwait(false);
        }
    }

    throw new TimeoutException($"{host}:{port} did not respond to Redis PING within {timeout}.", lastError);
}

static string ResolveRepositoryRoot()
{
    string currentDirectory = Directory.GetCurrentDirectory();
    if (File.Exists(Path.Combine(currentDirectory, "Hexalith.Memories.slnx")))
    {
        return currentDirectory;
    }

    string candidate = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));
    return File.Exists(Path.Combine(candidate, "Hexalith.Memories.slnx"))
        ? candidate
        : currentDirectory;
}

internal sealed record GeneratedDaprComponentPaths(
    string StateStore,
    string PubSub,
    string SecretStore,
    string ConversationLlm);
