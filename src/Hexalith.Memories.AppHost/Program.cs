using CommunityToolkit.Aspire.Hosting.Dapr;

IDistributedApplicationBuilder builder = DistributedApplication.CreateBuilder(args);

// Redis Stack: RediSearch (syntactic) + Vector Search (semantic) + DAPR state store
IResourceBuilder<ContainerResource> redis = builder
    .AddContainer("redis", "redis/redis-stack")
    .WithEndpoint(port: 6379, targetPort: 6379, name: "redis");

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
        }))
    .WaitFor(redis)
    .WaitFor(falkordb);

builder.Build().Run();
