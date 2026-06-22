using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;

using CommunityToolkit.Aspire.Hosting.Dapr;

namespace Hexalith.Memories.Aspire;

/// <summary>
/// The resource builders created by
/// <see cref="HexalithMemoriesServerExtensions.AddHexalithMemoriesSearchIndexServer"/>, exposed so the consuming
/// AppHost can further configure them (consumer-specific routing, additional references, wait-for edges).
/// </summary>
/// <param name="Server">The Memories search-index server project resource builder.</param>
/// <param name="FalkorDb">The FalkorDB graph-store container resource builder.</param>
/// <param name="SecretStore">The Memories DAPR secret-store component resource builder.</param>
/// <param name="Llm">The Memories DAPR conversation/LLM component resource builder.</param>
public sealed record HexalithMemoriesSearchIndexServerResources(
    IResourceBuilder<ProjectResource> Server,
    IResourceBuilder<ContainerResource> FalkorDb,
    IResourceBuilder<IDaprComponentResource> SecretStore,
    IResourceBuilder<IDaprComponentResource> Llm);

/// <summary>
/// Provides the Aspire hosting extension that adds the Hexalith.Memories search-index server (FalkorDB graph
/// store, secret store, conversation/LLM component, the <c>memories-server</c> project and its DAPR sidecar) to
/// a consuming domain-module AppHost.
/// </summary>
/// <remarks>
/// <para>
/// A Hexalith domain module that embeds the Memories search index (for example to make a curated read model
/// searchable) needs the same Memories topology in its AppHost. Previously each AppHost hand-rolled it. That
/// boilerplate now lives here, in the Memories platform Aspire library, so consumers call a single helper.
/// </para>
/// <para>
/// The server reuses the <paramref name="stateStore"/> and <paramref name="pubSub"/> DAPR components supplied by
/// the consumer (typically the shared EventStore state store and pub/sub) and runs its own FalkorDB graph store
/// plus consumer-supplied secret-store and conversation components. The <c>memories-server</c> project is
/// referenced cross-repo with <see cref="IProjectMetadata.SuppressBuild"/> set to <see langword="true"/>; the
/// Memories platform is built independently (Aspire runs children with <c>--no-build</c>). This helper only
/// <i>adds</i> the topology and returns the resource builders — any source-to-index routing is applied by the
/// consumer on the returned <see cref="HexalithMemoriesSearchIndexServerResources.Server"/>.
/// </para>
/// </remarks>
public static class HexalithMemoriesServerExtensions
{
    /// <summary>
    /// Adds the Hexalith.Memories search-index server and its supporting DAPR topology to the distributed
    /// application, resolving the server project cross-repo from the consuming repository's
    /// <c>Hexalith.Memories</c> submodule.
    /// </summary>
    /// <param name="builder">The distributed application builder.</param>
    /// <param name="stateStore">The shared DAPR state-store component the Memories server reuses.</param>
    /// <param name="pubSub">The shared DAPR pub/sub component the Memories server reuses.</param>
    /// <param name="secretStoreComponentPath">Path to the local-file secret-store component YAML (consumer-owned).</param>
    /// <param name="llmComponentPath">Path to the conversation/LLM component YAML (consumer-owned).</param>
    /// <param name="redisConnectionString">The Redis connection string for the Memories vector/search store. Defaults to <c>"localhost:6379"</c>.</param>
    /// <param name="eventStoreTopic">The pub/sub topic the Memories server subscribes for EventStore integration. Defaults to <c>"memories-events"</c>.</param>
    /// <param name="serverName">The Aspire resource name and DAPR app id for the Memories server. Defaults to <c>"memories-server"</c>.</param>
    /// <param name="daprHttpPort">The Memories sidecar DAPR HTTP port. Defaults to <c>3502</c> (the EventStore platform uses 3501).</param>
    /// <param name="daprGrpcPort">The Memories sidecar DAPR gRPC port. Defaults to <c>50002</c>.</param>
    /// <param name="daprPlacementHostAddress">Optional DAPR placement service address (<c>host</c> or <c>host:port</c>). <see langword="null"/> uses the DAPR default.</param>
    /// <param name="daprSchedulerHostAddress">Optional DAPR scheduler service address (<c>host</c> or <c>host:port</c>). <see langword="null"/> uses the DAPR default.</param>
    /// <returns>
    /// A <see cref="HexalithMemoriesSearchIndexServerResources"/> exposing the server, FalkorDB, secret-store and
    /// LLM resource builders for further customization.
    /// </returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="builder"/>, <paramref name="stateStore"/> or <paramref name="pubSub"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">Thrown when a required path, connection string, topic or name is <see langword="null"/> or whitespace.</exception>
    public static HexalithMemoriesSearchIndexServerResources AddHexalithMemoriesSearchIndexServer(
        this IDistributedApplicationBuilder builder,
        IResourceBuilder<IDaprComponentResource> stateStore,
        IResourceBuilder<IDaprComponentResource> pubSub,
        string secretStoreComponentPath,
        string llmComponentPath,
        string redisConnectionString = "localhost:6379",
        string eventStoreTopic = "memories-events",
        string serverName = "memories-server",
        int daprHttpPort = 3502,
        int daprGrpcPort = 50002,
        string? daprPlacementHostAddress = null,
        string? daprSchedulerHostAddress = null)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(stateStore);
        ArgumentNullException.ThrowIfNull(pubSub);
        ArgumentException.ThrowIfNullOrWhiteSpace(secretStoreComponentPath);
        ArgumentException.ThrowIfNullOrWhiteSpace(llmComponentPath);
        ArgumentException.ThrowIfNullOrWhiteSpace(redisConnectionString);
        ArgumentException.ThrowIfNullOrWhiteSpace(eventStoreTopic);
        ArgumentException.ThrowIfNullOrWhiteSpace(serverName);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(daprHttpPort);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(daprHttpPort, 65535);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(daprGrpcPort);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(daprGrpcPort, 65535);

        // Static dev secret store + conversation (echo) components, loaded from the consumer-owned YAML files.
        IResourceBuilder<IDaprComponentResource> secretStore = builder.AddDaprComponent(
            "memories-secretstore",
            "secretstores.local.file",
            new DaprComponentOptions { LocalPath = secretStoreComponentPath });
        IResourceBuilder<IDaprComponentResource> llm = builder.AddDaprComponent(
            "memories-llm",
            "conversation.echo",
            new DaprComponentOptions { LocalPath = llmComponentPath });

        // FalkorDB graph store for the Memories graph index.
        IResourceBuilder<ContainerResource> falkorDb = builder
            .AddContainer("memories-falkordb", "falkordb/falkordb")
            .WithEndpoint(targetPort: 6379, name: "falkordb");
        EndpointReference falkorDbEndpoint = falkorDb.GetEndpoint("falkordb");

        // The server reuses the shared state store + pub/sub, runs its own FalkorDB, and gets a DAPR sidecar on a
        // unique HTTP port (the EventStore platform uses 3501). The project is built independently
        // (MemoriesServerProjectMetadata.SuppressBuild).
        IResourceBuilder<ProjectResource> server = builder
            .AddProject<MemoriesServerProjectMetadata>(serverName, launchProfileName: "http")
            .WithDaprSidecar(sidecar => sidecar
                .WithOptions(new DaprSidecarOptions
                {
                    AppId = serverName,
                    DaprHttpPort = daprHttpPort,
                    DaprGrpcPort = daprGrpcPort,
                    PlacementHostAddress = daprPlacementHostAddress,
                    SchedulerHostAddress = daprSchedulerHostAddress,
                })
                .WithReference(stateStore)
                .WithReference(pubSub)
                .WithReference(secretStore)
                .WithReference(llm))
            // Shared dapr-init Redis for the Memories vector/search store; FalkorDB for the graph store.
            .WithEnvironment("ConnectionStrings__redis", redisConnectionString)
            .WithEnvironment("ConnectionStrings__falkordb", ReferenceExpression.Create($"{falkorDbEndpoint.Property(EndpointProperty.HostAndPort)}"))
            // The controller subscription binding uses [Topic("pubsub", "$(MEMORIES_EVENTSTORE_TOPIC)")].
            .WithEnvironment("MEMORIES_EVENTSTORE_TOPIC", eventStoreTopic)
            .WaitFor(falkorDb)
            .WaitFor(secretStore)
            .WaitFor(llm);

        return new HexalithMemoriesSearchIndexServerResources(server, falkorDb, secretStore, llm);
    }
}
