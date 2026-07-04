using System.Diagnostics;
using System.Globalization;
using System.Net.Sockets;
using System.Text;

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Eventing;

using CommunityToolkit.Aspire.Hosting.Dapr;

using Hexalith.EventStore.Aspire;
using Hexalith.Memories.AppHost;

IDistributedApplicationBuilder builder = DistributedApplication.CreateBuilder(args);
HexalithEventStoreSecurityResources? security = builder.AddHexalithEventStoreSecurity();
string secretsFile = EnsureSecretsFile();
string daprConfigPath = ResolveDaprConfigPath();
string daprAppId = ResolveDaprAppId();
string redisConfigPath = ResolveRedisConfigPath();
string redisVolumeName = ResolveRedisVolumeName();
GeneratedDaprComponentPaths daprComponentPaths = EnsureDaprComponentFiles(daprAppId, secretsFile);

// Story 15.6 code review: the rewrite signal is refreshable so a transient OnResourceReady fault
// does not poison every subsequent sidecar start in the same AppHost session. The first
// OnResourceReady cycle observes the initial signal; once that signal completes (success OR fault),
// the next OnResourceReady installs a fresh signal before doing work. A lock guards the snapshot
// vs. replacement race between BeforeResourceStartedEvent and OnResourceReady when Redis restarts.
TaskCompletionSource redisComponentRewrite = new(TaskCreationOptions.RunContinuationsAsynchronously);
object redisComponentRewriteGate = new();
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

// Story 6.4: make Redis durability explicit instead of relying on image defaults.
// The redis/redis-stack image auto-loads /redis-stack.conf from its /entrypoint.sh, so a
// repo-owned config bind-mount plus a named /data volume is enough to enable durable AOF+RDB.
// Tests can override the volume name for isolation via MEMORIES_REDIS_VOLUME_NAME; local/dev
// runs keep a stable named volume so controlled restarts preserve state.
IResourceBuilder<ContainerResource> redis = builder
    .AddContainer("memories-vectors", "redis/redis-stack")
    .WithBindMount(redisConfigPath, "/redis-stack.conf", isReadOnly: true)
    .WithVolume(redisVolumeName, "/data")
    .WithEndpoint(targetPort: 6379, name: "redis");
EndpointReference redisEndpoint = redis.GetEndpoint("redis");
redis.OnResourceReady((resource, _, _) =>
{
    TaskCompletionSource cycleTcs;
    lock (redisComponentRewriteGate)
    {
        if (redisComponentRewrite.Task.IsCompleted)
        {
            // Previous cycle terminated (success or fault). Install a fresh signal so a transient
            // earlier failure does not permanently poison subsequent sidecar starts.
            redisComponentRewrite = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        }

        cycleTcs = redisComponentRewrite;
    }

    try
    {
        (string host, int port) = ResolveAllocatedEndpoint(resource, "redis");
        WriteDaprRedisComponentFiles(daprComponentPaths.StateStore, daprComponentPaths.PubSub, $"{host}:{port}");
        cycleTcs.TrySetResult();
        return Task.CompletedTask;
    }
    catch (Exception ex)
    {
        cycleTcs.TrySetException(ex);
        throw;
    }
});
builder.Eventing.Subscribe<BeforeResourceStartedEvent>(async (@event, cancellationToken) =>
{
    if (@event.Resource.Name is "memories-dapr" or "memories-dapr-cli" or
        "memories-mcp-dapr" or "memories-mcp-dapr-cli")
    {
        Task rewriteSignal;
        lock (redisComponentRewriteGate)
        {
            rewriteSignal = redisComponentRewrite.Task;
        }

        await WaitForRedisComponentRewriteAsync(rewriteSignal, TimeSpan.FromMinutes(2), cancellationToken)
            .ConfigureAwait(false);

        // Resolve the endpoint AFTER awaiting the rewrite signal — if BeforeResourceStartedEvent
        // ever races allocation, the rewrite-signal wait gives Redis a chance to finish allocating
        // before the endpoint lookup throws InvalidOperationException.
        (string host, int port) = ResolveAllocatedEndpoint(redis.Resource, "redis");
        await WaitForRedisPingAsync(host, port, TimeSpan.FromMinutes(2), cancellationToken)
            .ConfigureAwait(false);
    }
});

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
    .AddContainer("memories-graphs", "falkordb/falkordb")
    .WithEndpoint(targetPort: 6379, name: "falkordb");
EndpointReference falkordbEndpoint = falkordb.GetEndpoint("falkordb");

// Memories Server with DAPR sidecar
// DAPR sidecar manages connections to Redis/FalkorDB via component config
// AppPort is intentionally omitted so Aspire Testing can auto-detect the
// randomized project port instead of pinning the sidecar to localhost:5000.
IResourceBuilder<ProjectResource> server = builder
    .AddProject<Projects.Hexalith_Memories_Server>("memories", launchProfileName: "http")
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
    .WaitFor(falkordb)
    .WaitFor(secretStore)
    .WaitFor(conversationLlm);

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

server = security is null
    ? PropagateJwtBearerAuthenticationEnvironment(server)
    : server.WithJwtBearerSecurity(security);

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
    .AddProject<Projects.Hexalith_Memories_Mcp>("memories-mcp", launchProfileName: "http")
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

mcp = security is null
    ? PropagateJwtBearerAuthenticationEnvironment(mcp)
    : mcp.WithJwtBearerSecurity(security);

_ = mcp;

DistributedApplication app = builder.Build();
try
{
    app.Run();
}
finally
{
    DeleteDaprComponentDirectory(daprComponentPaths.ComponentsDirectory);
}

static string EnsureTestDataRoot()
{
    string repoRoot = RepositoryRootLocator.Resolve();
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
    string repoRoot = RepositoryRootLocator.Resolve();
    string secretsFile = Path.Combine(repoRoot, "secrets.json");

    if (!File.Exists(secretsFile))
    {
        File.WriteAllText(secretsFile, "{}" + Environment.NewLine);
    }

    // Story 15.6 code review: tighten permissions every time the file is observed (idempotent chmod
    // on Linux/macOS) so a pre-existing secrets.json from a previous AppHost run, or from a release
    // pre-dating this hardening, does not remain world-readable.
    TryRestrictSecretFilePermissions(secretsFile);

    return secretsFile;
}

static GeneratedDaprComponentPaths EnsureDaprComponentFiles(string daprAppId, string secretsFile)
{
    // Story 15.6 code review: sweep stale per-PID directories before creating ours. ProcessExit
    // handlers do not fire on SIGKILL / FailFast / BSOD, so crashed prior sessions leak component
    // YAMLs (each containing a `secretsFile` path) under %TEMP%. The sweep deletes only directories
    // whose PID is no longer alive — never touches a running AppHost's directory.
    SweepStaleDaprComponentDirectories(daprAppId);

    string componentsDirectory = Path.Combine(
        Path.GetTempPath(),
        "hexalith-memories-dapr",
        $"{daprAppId}-{Process.GetCurrentProcess().Id}");
    Directory.CreateDirectory(componentsDirectory);
    RegisterDaprComponentDirectoryCleanup(componentsDirectory);

    string stateStorePath = Path.Combine(componentsDirectory, "statestore.yaml");
    string pubSubPath = Path.Combine(componentsDirectory, "pubsub.yaml");
    string secretStorePath = Path.Combine(componentsDirectory, "secretstore.yaml");
    string conversationLlmPath = Path.Combine(componentsDirectory, "llm.yaml");

    // These files are rewritten with Aspire's allocated Redis endpoint before the Dapr sidecars start.
    // The initial localhost value keeps the files valid for design-time inspection and local fallbacks.
    WriteDaprRedisComponentFiles(stateStorePath, pubSubPath, "127.0.0.1:6379");

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
              value: "{EscapeYamlDoubleQuotedScalar(secretsFile)}"
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
        componentsDirectory,
        stateStorePath,
        pubSubPath,
        secretStorePath,
        conversationLlmPath);
}

static void TryRestrictSecretFilePermissions(string secretsFile)
{
    if (OperatingSystem.IsWindows())
    {
        return;
    }

    // Story 15.6 code review: the method name promises best-effort, so do not crash AppHost startup
    // when the filesystem cannot honor POSIX permissions (FAT32/exFAT, SMB without unix-mode
    // mapping, sandboxed mounts). Wrap and continue.
    try
    {
        File.SetUnixFileMode(secretsFile, UnixFileMode.UserRead | UnixFileMode.UserWrite);
    }
    catch (IOException ex)
    {
        Console.Error.WriteLine(
            $"Hexalith.Memories AppHost: best-effort chmod on '{secretsFile}' skipped ({ex.GetType().Name}: {ex.Message}).");
    }
    catch (UnauthorizedAccessException ex)
    {
        Console.Error.WriteLine(
            $"Hexalith.Memories AppHost: best-effort chmod on '{secretsFile}' denied ({ex.GetType().Name}: {ex.Message}).");
    }
    catch (PlatformNotSupportedException ex)
    {
        Console.Error.WriteLine(
            $"Hexalith.Memories AppHost: best-effort chmod on '{secretsFile}' unsupported on this runtime ({ex.GetType().Name}).");
    }
}

static void RegisterDaprComponentDirectoryCleanup(string componentsDirectory)
{
    AppDomain.CurrentDomain.ProcessExit += (_, _) => DeleteDaprComponentDirectory(componentsDirectory);
}

static void DeleteDaprComponentDirectory(string componentsDirectory)
{
    try
    {
        if (Directory.Exists(componentsDirectory))
        {
            Directory.Delete(componentsDirectory, recursive: true);
        }
    }
    catch (IOException ex)
    {
        // Story 15.6 code review: surface the failure on stderr instead of swallowing silently.
        // On Windows shutdown the daprd sidecar typically dies later than AppHost, so files in the
        // dir may still be locked at the moment cleanup runs — operators previously had no way to
        // tell why temp directories accumulated.
        Console.Error.WriteLine(
            $"Hexalith.Memories AppHost: failed to delete DAPR component directory '{componentsDirectory}': {ex.GetType().Name}: {ex.Message}");
    }
    catch (UnauthorizedAccessException ex)
    {
        Console.Error.WriteLine(
            $"Hexalith.Memories AppHost: cleanup denied on DAPR component directory '{componentsDirectory}': {ex.GetType().Name}: {ex.Message}");
    }
}

static void SweepStaleDaprComponentDirectories(string daprAppId)
{
    string root = Path.Combine(Path.GetTempPath(), "hexalith-memories-dapr");
    if (!Directory.Exists(root))
    {
        return;
    }

    int currentPid = Process.GetCurrentProcess().Id;

    IEnumerable<string> candidates;
    try
    {
        candidates = Directory.EnumerateDirectories(root, $"{daprAppId}-*");
    }
    catch (IOException)
    {
        return;
    }
    catch (UnauthorizedAccessException)
    {
        return;
    }

    foreach (string dir in candidates)
    {
        string name = Path.GetFileName(dir);
        int dashIndex = name.LastIndexOf('-');
        if (dashIndex < 0 || dashIndex == name.Length - 1)
        {
            continue;
        }

        ReadOnlySpan<char> pidSpan = name.AsSpan(dashIndex + 1);
        if (!int.TryParse(pidSpan, NumberStyles.Integer, CultureInfo.InvariantCulture, out int pid))
        {
            continue;
        }

        if (pid == currentPid)
        {
            continue;
        }

        bool processAlive = true;
        try
        {
            using Process existing = Process.GetProcessById(pid);
            // Cannot fully verify the process is the SAME AppHost (PID reuse is possible), but the
            // safe default is to leave any directory tied to a live PID alone.
            processAlive = !existing.HasExited;
        }
        catch (ArgumentException)
        {
            processAlive = false;
        }
        catch (InvalidOperationException)
        {
            // Process exited but the handle has not been released — safe to clean.
            processAlive = false;
        }

        if (!processAlive)
        {
            DeleteDaprComponentDirectory(dir);
        }
    }
}

static string EscapeYamlDoubleQuotedScalar(string value)
{
    ArgumentNullException.ThrowIfNull(value);

    var builder = new StringBuilder(value.Length);
    foreach (char ch in value)
    {
        _ = ch switch
        {
            '\\' => builder.Append(@"\\"),
            '"' => builder.Append("\\\""),
            '\0' => builder.Append(@"\0"),
            '\a' => builder.Append(@"\a"),
            '\b' => builder.Append(@"\b"),
            '\t' => builder.Append(@"\t"),
            '\n' => builder.Append(@"\n"),
            '\v' => builder.Append(@"\v"),
            '\f' => builder.Append(@"\f"),
            '\r' => builder.Append(@"\r"),
            // Story 15.6 code review: YAML 1.2 §5.7 treats U+2028 (Line Separator) and U+2029
            // (Paragraph Separator) as line breaks inside double-quoted scalars; char.IsControl is
            // false for both, so the unconditional `Append(ch)` fall-through previously emitted
            // them verbatim, which a daprd YAML parser would split across logical lines.
            '\u2028' => builder.Append(@"\L"),
            '\u2029' => builder.Append(@"\P"),
            _ when char.IsControl(ch) => builder.Append("\\x").Append(((int)ch).ToString("X2", CultureInfo.InvariantCulture)),
            _ => builder.Append(ch),
        };
    }

    return builder.ToString();
}

static void WriteDaprRedisComponentFiles(string stateStorePath, string pubSubPath, string redisHost)
{
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
}

static string ResolveDaprConfigPath()
{
    string? configured = ResolveOptionalEnvironmentValue("MEMORIES_DAPR_CONFIG_PATH");
    if (!string.IsNullOrWhiteSpace(configured))
    {
        string configuredPath = Path.GetFullPath(configured);
        if (!File.Exists(configuredPath))
        {
            throw new FileNotFoundException(
                "Configured DAPR configuration not found. Ensure MEMORIES_DAPR_CONFIG_PATH points to an existing file.",
                configuredPath);
        }

        return configuredPath;
    }

    string repoRoot = RepositoryRootLocator.Resolve();
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
    string repoRoot = RepositoryRootLocator.Resolve();
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
    //
    // Story 15.6 code review: also tolerate inline comments without a leading space ("appendonly yes#comment")
    // and a leading UTF-8 BOM on the first line, which previously produced false negatives that crashed
    // AppHost startup against valid (if unusual) Redis config files.
    bool hasAppendOnly = File.ReadLines(configPath)
        .Select(StripBomAndInlineCommentForRedisConf)
        .Where(line => line.Length > 0 && !line.StartsWith('#'))
        .Select(line => line.Split([' ', '\t'], StringSplitOptions.RemoveEmptyEntries))
        .Any(parts => parts.Length >= 2
            && string.Equals(parts[0], "appendonly", StringComparison.OrdinalIgnoreCase)
            && string.Equals(parts[1], "yes", StringComparison.OrdinalIgnoreCase));

    static string StripBomAndInlineCommentForRedisConf(string line)
    {
        string trimmed = line.TrimStart('﻿').Trim();
        int hashIndex = trimmed.IndexOf('#');
        return (hashIndex >= 0 ? trimmed[..hashIndex] : trimmed).Trim();
    }

    if (!hasAppendOnly)
    {
        throw new InvalidOperationException(
            $"Redis persistence configuration at '{configPath}' must set 'appendonly yes' to enable AOF durability.");
    }

    return configPath;
}

static string ResolveDaprAppId()
{
    string? configured = Environment.GetEnvironmentVariable("MEMORIES_DAPR_APP_ID");
    if (string.IsNullOrWhiteSpace(configured))
    {
        return "memories";
    }

    string trimmed = configured.Trim();

    // Story 15.6 code review: the daprAppId is interpolated into a temp directory path and used by
    // recursive Directory.Delete on shutdown and by the stale-PID-sweep on startup. A hostile env
    // var containing path-traversal segments ('..'), path separators, or Windows-illegal characters
    // (':', '<', '>', '|', etc.) could redirect cleanup to operator-chosen directories or fail
    // unexpectedly at CreateDirectory. Reject anything outside an allow-listed safe charset.
    if (!IsSafeDaprAppId(trimmed))
    {
        throw new InvalidOperationException(
            $"MEMORIES_DAPR_APP_ID='{trimmed}' is invalid. Allowed characters: ASCII letters, digits, '.', '_', '-'. Length: 1-64.");
    }

    return trimmed;

    static bool IsSafeDaprAppId(string value)
    {
        if (value.Length is 0 or > 64)
        {
            return false;
        }

        foreach (char ch in value)
        {
            bool valid = ch is (>= 'a' and <= 'z') or (>= 'A' and <= 'Z') or (>= '0' and <= '9')
                or '.' or '_' or '-';
            if (!valid)
            {
                return false;
            }
        }

        return true;
    }
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

static (string Host, int Port) ResolveAllocatedEndpoint(IResource resource, string endpointName)
{
    if (!resource.TryGetEndpoints(out IEnumerable<EndpointAnnotation>? endpoints))
    {
        throw new InvalidOperationException($"Resource '{resource.Name}' does not expose endpoints.");
    }

    EndpointAnnotation endpoint = endpoints.Single(candidate =>
        string.Equals(candidate.Name, endpointName, StringComparison.Ordinal));
    AllocatedEndpoint allocated = endpoint.AllocatedEndpoint
        ?? throw new InvalidOperationException($"Endpoint '{resource.Name}/{endpointName}' has not been allocated yet.");

    string host = allocated.Address;
    if (string.IsNullOrWhiteSpace(host) ||
        string.Equals(host, "0.0.0.0", StringComparison.Ordinal) ||
        string.Equals(host, "::", StringComparison.Ordinal))
    {
        host = "127.0.0.1";
    }

    return (host, allocated.Port);
}

static async Task WaitForRedisComponentRewriteAsync(
    Task rewriteTask,
    TimeSpan timeout,
    CancellationToken cancellationToken)
{
    try
    {
        await rewriteTask.WaitAsync(timeout, cancellationToken).ConfigureAwait(false);
    }
    catch (TimeoutException ex)
    {
        throw new TimeoutException($"DAPR Redis component files were not rewritten within {timeout}.", ex);
    }
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
    byte[] responseChunk = new byte[16];

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

            List<byte> response = [];
            while (!EndsWithCrlf(response))
            {
                int bytesRead = await stream.ReadAsync(responseChunk.AsMemory(), attemptCts.Token)
                    .ConfigureAwait(false);
                if (bytesRead == 0)
                {
                    throw new InvalidOperationException("Redis closed the connection before returning PONG.");
                }

                response.AddRange(responseChunk.Take(bytesRead));
            }

            if (IsRedisPong(response))
            {
                return;
            }

            // Story 15.6 code review: parse RESP error replies (response starts with '-'). A
            // -LOADING reply is transient (Redis is restoring its dataset) — keep retrying. Anything
            // else (-NOAUTH, -ERR, -WRONGPASS, -MASTERDOWN) is misconfiguration that will not
            // resolve by waiting; throw a non-retryable exception so the operator sees the actual
            // cause instead of a generic TimeoutException 2 minutes later.
            string preview = Encoding.UTF8.GetString([.. response]).TrimEnd('\r', '\n');
            if (response.Count > 0 && response[0] == (byte)'-')
            {
                string errorTag = ExtractRedisErrorTag(preview);
                if (string.Equals(errorTag, "LOADING", StringComparison.Ordinal))
                {
                    throw new InvalidOperationException(
                        $"Redis is still loading its dataset: {preview}");
                }

                throw new RedisProbeNonRetryableException(
                    $"{host}:{port} returned a non-retryable Redis error reply to PING: {preview}");
            }

            throw new InvalidOperationException(
                $"Redis did not return PONG to the readiness probe (got: '{preview}').");
        }
        catch (Exception ex) when (ex is SocketException or TimeoutException or System.IO.IOException or InvalidOperationException ||
                                   (ex is OperationCanceledException && !cancellationToken.IsCancellationRequested))
        {
            lastError = ex;
            await Task.Delay(TimeSpan.FromMilliseconds(500), cancellationToken).ConfigureAwait(false);
        }

        // RedisProbeNonRetryableException is intentionally NOT in the catch filter above — it
        // escapes the retry loop and propagates the real Redis error text to the caller.
    }

    throw new TimeoutException($"{host}:{port} did not respond to Redis PING within {timeout}.", lastError);
}

static string ExtractRedisErrorTag(string preview)
{
    // RESP error: "-<TAG> <message>". The tag is the first whitespace-delimited token after '-'.
    if (preview.Length < 2 || preview[0] != '-')
    {
        return string.Empty;
    }

    int spaceIndex = preview.IndexOf(' ', 1);
    return spaceIndex < 0 ? preview[1..] : preview[1..spaceIndex];
}

static bool EndsWithCrlf(IReadOnlyList<byte> bytes)
    => bytes.Count >= 2 && bytes[^2] == (byte)'\r' && bytes[^1] == (byte)'\n';

static bool IsRedisPong(IReadOnlyList<byte> bytes)
{
    ReadOnlySpan<byte> expected = "+PONG\r\n"u8;
    if (bytes.Count != expected.Length)
    {
        return false;
    }

    for (int i = 0; i < expected.Length; i++)
    {
        if (bytes[i] != expected[i])
        {
            return false;
        }
    }

    return true;
}

internal sealed record GeneratedDaprComponentPaths(
    string ComponentsDirectory,
    string StateStore,
    string PubSub,
    string SecretStore,
    string ConversationLlm);

/// <summary>
/// Story 15.6 code review: signals a Redis readiness probe failure that should NOT be retried —
/// e.g., authentication misconfiguration or wrong master. Deliberately does NOT inherit from any
/// type in <see cref="WaitForRedisPingAsync"/>'s catch filter, so the exception escapes the retry
/// loop and surfaces the real Redis error text to the caller instead of being lost to a generic
/// <see cref="TimeoutException"/> two minutes later.
/// </summary>
internal sealed class RedisProbeNonRetryableException : Exception
{
    public RedisProbeNonRetryableException(string message) : base(message)
    {
    }
}
