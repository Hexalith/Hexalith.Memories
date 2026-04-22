using Aspire.Hosting.ApplicationModel;
using CommunityToolkit.Aspire.Hosting.Dapr;

IDistributedApplicationBuilder builder = DistributedApplication.CreateBuilder(args);
string secretsFile = EnsureSecretsFile();
string daprConfigPath = ResolveDaprConfigPath();
string daprAppId = ResolveDaprAppId();
string redisConfigPath = ResolveRedisConfigPath();
string redisVolumeName = ResolveRedisVolumeName();

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
    .AddDaprComponent("statestore", "state.redis")
    .WithMetadata("redisHost", "127.0.0.1:6379")
    .WithMetadata("redisPassword", string.Empty)
    .WithMetadata("actorStateStore", "true");

// Story 9.1: DAPR pub/sub component shared with the Redis dependency. Must stay aligned with
// deploy/dapr/components/pubsub.yaml (same component name, same broker). Local dev uses localhost:6379
// because Aspire pins the Redis endpoint on the host; production deployments should bind-mount
// deploy/dapr/components/pubsub.yaml and inject PUBSUB_REDIS_HOST/PUBSUB_REDIS_PASSWORD from secrets.
IResourceBuilder<IDaprComponentResource> pubSub = builder
    .AddDaprComponent("pubsub", "pubsub.redis")
    .WithMetadata("redisHost", "127.0.0.1:6379")
    .WithMetadata("redisPassword", string.Empty);

IResourceBuilder<IDaprComponentResource> secretStore = builder
    .AddDaprComponent("secretstore", "secretstores.local.file")
    .WithMetadata("secretsFile", secretsFile)
    .WithMetadata("nestedSeparator", ":");

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
        sidecar = sidecar
            .WithOptions(new DaprSidecarOptions
            {
                AppId = daprAppId,
                DaprHttpPort = 3500,
                DaprGrpcPort = 50001,
                Config = daprConfigPath,
            })
            .WithReference(stateStore)
            .WithReference(pubSub)
            .WithReference(secretStore);
    })
    .WithEnvironment(
        "ConnectionStrings__redis",
        ReferenceExpression.Create($"{redisEndpoint.Property(EndpointProperty.HostAndPort)}"))
    .WithEnvironment(
        "ConnectionStrings__falkordb",
        ReferenceExpression.Create($"{falkordbEndpoint.Property(EndpointProperty.HostAndPort)}"))
    .WaitFor(redis)
    .WaitFor(falkordb);

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
