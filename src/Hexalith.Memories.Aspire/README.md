# Hexalith.Memories.Aspire

.NET Aspire hosting extensions for [Hexalith.Memories](https://github.com/Hexalith/Hexalith.Memories).

A Hexalith domain module that wants to embed the Memories search-index server in its own
`*.AppHost` (for example to make a curated read model searchable) previously had to hand-roll the
same wiring: a FalkorDB graph container, a secret-store component, a conversation/LLM component, the
`memories-server` project with a DAPR sidecar, and the standard connection-string / topic
environment. That boilerplate now lives here.

## Usage

```csharp
using Hexalith.Memories.Aspire;

// stateStore / pubSub are the shared DAPR components the Memories server should reuse
// (for example the ones returned by Hexalith.EventStore.Aspire's AddHexalithEventStore).
HexalithMemoriesSearchIndexServerResources memories = builder.AddHexalithMemoriesSearchIndexServer(
    stateStore,
    pubSub,
    secretStoreComponentPath: secretStoreYamlPath,
    llmComponentPath: llmYamlPath,
    daprPlacementHostAddress: placementAddress,
    daprSchedulerHostAddress: schedulerAddress);

// Apply consumer-specific routing on the returned server resource.
_ = memories.Server
    .WithEnvironment("EventStoreIntegration__Routing__SourceToTenantMap__my-source", "my-index")
    .WithEnvironment("EventStoreIntegration__Routing__AutoProvisionRoutedTenants", "true");
```

The helper only **adds** the Memories topology and returns the resource builders; the consuming
AppHost owns its DAPR component YAML files and any source-to-index routing.

The `memories-server` project is referenced cross-repo with `IProjectMetadata.SuppressBuild`, so the
consuming AppHost never compiles it (Aspire runs children with `--no-build`) and the two
repositories' package graphs stay isolated.
