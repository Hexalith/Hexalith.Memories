# Hexalith.Memories.Aspire

The package also exposes `AddHexalithMemoriesAccessTelemetry` for the fixed lifecycle and independent-clock topology. It is intentionally disabled by default: Story 27.2 supplies portable Dapr boundaries and component templates, while exact Production adapter selection and behavioral certification remain Story 27.3 work.

.NET Aspire hosting extensions for [Hexalith.Memories](https://github.com/Hexalith/Hexalith.Memories).

A Hexalith domain module that wants to embed the Memories search-index server in its own
`*.AppHost` (for example to make a curated read model searchable) previously had to hand-roll the
same wiring: a Redis Stack search/vector container, a FalkorDB graph container, a secret-store
component, a conversation/LLM component, the `memories` project with a DAPR sidecar, and the standard connection-string / topic
environment. That boilerplate now lives here.

## Usage

```csharp
using Hexalith.Memories.Aspire;

// stateStore / pubSub / secretStore are the shared DAPR components the Memories server should reuse
// (for example stateStore/pubSub from Hexalith.EventStore.Aspire's AddHexalithEventStore, and an
// OpenBao-backed secretstores.hashicorp.vault component the consumer provisions and scopes itself).
IResourceBuilder<IDaprComponentResource> secretStore = builder.AddDaprComponent(
    "secretstore",
    "secretstores.hashicorp.vault",
    new DaprComponentOptions { LocalPath = secretStoreYamlPath });

HexalithMemoriesSearchIndexServerResources memories = builder.AddHexalithMemoriesSearchIndexServer(
    stateStore,
    pubSub,
    secretStore,
    llmComponentPath: llmYamlPath,
    daprPlacementHostAddress: placementAddress,
    daprSchedulerHostAddress: schedulerAddress);

// Apply consumer-specific routing on the returned server resource.
_ = memories.Server
    .WithEnvironment("EventStoreIntegration__Routing__SourceToTenantMap__my-source", "my-index")
    .WithEnvironment("EventStoreIntegration__Routing__AutoProvisionRoutedTenants", "true");
```

The helper only **adds** the Memories topology and returns the resource builders; the consuming
AppHost owns its DAPR component YAML files, its secret-store provisioning (this library never creates
a secret-store component or depends on any secret-provider SDK), and any source-to-index routing.

`AddHexalithMemoriesAccessTelemetry` takes the same externally-provisioned `secretStore` parameter for
the access-telemetry lifecycle, clock, and server components — the consumer supplies one secret-store
resource, scoped and named to match its own Dapr configuration (for example `access-telemetry-secrets`
in the OpenBao-backed root AppHost topology):

```csharp
IResourceBuilder<IDaprComponentResource> accessTelemetrySecretStore = builder.AddDaprComponent(
    "access-telemetry-secrets",
    "secretstores.hashicorp.vault",
    new DaprComponentOptions { LocalPath = accessTelemetrySecretsYamlPath });

HexalithMemoriesAccessTelemetryResources accessTelemetry = builder.AddHexalithMemoriesAccessTelemetry(
    memories.Server,
    stateStoreComponentPath: accessTelemetryStateStoreYamlPath,
    accessTelemetrySecretStore,
    configurationStoreComponentPath: accessTelemetryConfigYamlPath);
```

By default the helper creates a `memories-vectors` `redis/redis-stack` container for the Memories
search/vector store. Pass `redisConnectionString` only when the consuming AppHost owns a compatible
Redis Stack dependency.

The `memories` project is referenced cross-repo with `IProjectMetadata.SuppressBuild`, so the
consuming AppHost never compiles it (Aspire runs children with `--no-build`) and the two
repositories' package graphs stay isolated.
