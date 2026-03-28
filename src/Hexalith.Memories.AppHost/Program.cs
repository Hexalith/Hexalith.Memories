using CommunityToolkit.Aspire.Hosting.Dapr;

IDistributedApplicationBuilder builder = DistributedApplication.CreateBuilder(args);
string secretsFile = EnsureSecretsFile();

// Redis Stack: RediSearch (syntactic) + Vector Search (semantic) + DAPR state store
IResourceBuilder<ContainerResource> redis = builder
    .AddContainer("redis", "redis/redis-stack")
    .WithEndpoint(port: 6379, targetPort: 6379, name: "redis");

IResourceBuilder<IDaprComponentResource> stateStore = builder
    .AddDaprComponent("statestore", "state.redis")
    .WithMetadata("redisHost", "127.0.0.1:6379")
    .WithMetadata("redisPassword", string.Empty)
    .WithMetadata("actorStateStore", "true");

IResourceBuilder<IDaprComponentResource> secretStore = builder
    .AddDaprComponent("secretstore", "secretstores.local.file")
    .WithMetadata("secretsFile", secretsFile)
    .WithMetadata("nestedSeparator", ":");

// FalkorDB: graph database (Redis-protocol compatible, internal port 6379 mapped to 6380)
IResourceBuilder<ContainerResource> falkordb = builder
    .AddContainer("falkordb", "falkordb/falkordb")
    .WithEndpoint(port: 6380, targetPort: 6379, name: "falkordb");

// Memories Server with DAPR sidecar
// DAPR sidecar manages connections to Redis/FalkorDB via component config
_ = builder
    .AddProject<Projects.Hexalith_Memories_Server>("memories-server")
    .WithDaprSidecar(sidecar => sidecar
        .WithOptions(new DaprSidecarOptions
        {
            AppId = "memories-server",
            AppPort = 5000,
            DaprHttpPort = 3500,
            DaprGrpcPort = 50001,
        })
        .WithReference(stateStore)
        .WithReference(secretStore))
    .WaitFor(redis)
    .WaitFor(falkordb);

builder.Build().Run();

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
