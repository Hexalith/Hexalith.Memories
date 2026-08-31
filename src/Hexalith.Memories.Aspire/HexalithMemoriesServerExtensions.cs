// <copyright file="HexalithMemoriesServerExtensions.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;

using CommunityToolkit.Aspire.Hosting.Dapr;

namespace Hexalith.Memories.Aspire;

/// <summary>
/// Provides the Aspire hosting extension that adds the Hexalith.Memories search-index server (FalkorDB graph
/// store, conversation/LLM component, the <c>memories</c> project and its DAPR sidecar, referencing a
/// consumer-supplied secret store) to a consuming domain-module AppHost.
/// </summary>
/// <remarks>
/// <para>
/// A Hexalith domain module that embeds the Memories search index (for example to make a curated read model
/// searchable) needs the same Memories topology in its AppHost. Previously each AppHost hand-rolled it. That
/// boilerplate now lives here, in the Memories platform Aspire library, so consumers call a single helper.
/// </para>
/// <para>
/// The server reuses the <paramref name="stateStore"/>, <paramref name="pubSub"/> and <paramref name="secretStore"/>
/// DAPR components supplied by the consumer (typically the shared EventStore state store/pub-sub and an
/// OpenBao-backed secret store) and runs its own Redis Stack search/vector store and FalkorDB graph store plus a
/// consumer-supplied conversation component. This library never provisions a secret store itself (no
/// <c>secretstores.local.file</c>, no OpenBao SDK/HTTP dependency) — it only references the resource the consumer
/// hands in. The <c>memories</c> project is
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
    /// <c>references/Hexalith.Memories</c> submodule.
    /// </summary>
    /// <param name="builder">The distributed application builder.</param>
    /// <param name="stateStore">The shared DAPR state-store component the Memories server reuses.</param>
    /// <param name="pubSub">The shared DAPR pub/sub component the Memories server reuses.</param>
    /// <param name="secretStore">
    /// The externally-provisioned DAPR secret-store component the Memories server references (for example an
    /// OpenBao-backed <c>secretstores.hashicorp.vault</c> component the consumer owns and seeds). This helper never
    /// creates its own secret-store component; the consumer is responsible for provisioning, scoping, and
    /// authenticating it.
    /// </param>
    /// <param name="llmComponentPath">Path to the conversation/LLM component YAML (consumer-owned).</param>
    /// <param name="redisConnectionString">Optional Redis connection string for the Memories vector/search store. When omitted, the helper adds a <c>redis/redis-stack</c> container.</param>
    /// <param name="eventStoreTopic">The pub/sub topic the Memories server subscribes for EventStore integration. Defaults to <c>"memories-events"</c>.</param>
    /// <param name="serverName">The Aspire resource name and DAPR app id for the Memories server. Defaults to <c>"memories"</c>.</param>
    /// <param name="daprHttpPort">The Memories sidecar DAPR HTTP port. Defaults to <c>3502</c> (the EventStore platform uses 3501).</param>
    /// <param name="daprGrpcPort">The Memories sidecar DAPR gRPC port. Defaults to <c>50002</c>.</param>
    /// <param name="daprPlacementHostAddress">Optional DAPR placement service address (<c>host</c> or <c>host:port</c>). <see langword="null"/> uses the DAPR default.</param>
    /// <param name="daprSchedulerHostAddress">Optional DAPR scheduler service address (<c>host</c> or <c>host:port</c>). <see langword="null"/> uses the DAPR default.</param>
    /// <returns>
    /// A <see cref="HexalithMemoriesSearchIndexServerResources"/> exposing the server, FalkorDB, secret-store and
    /// LLM resource builders for further customization.
    /// </returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="builder"/>, <paramref name="stateStore"/>, <paramref name="pubSub"/> or <paramref name="secretStore"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">Thrown when a required path, topic or name is <see langword="null"/> or whitespace.</exception>
    public static HexalithMemoriesSearchIndexServerResources AddHexalithMemoriesSearchIndexServer(
        this IDistributedApplicationBuilder builder,
        IResourceBuilder<IDaprComponentResource> stateStore,
        IResourceBuilder<IDaprComponentResource> pubSub,
        IResourceBuilder<IDaprComponentResource> secretStore,
        string llmComponentPath,
        string? redisConnectionString = null,
        string eventStoreTopic = "memories-events",
        string serverName = "memories",
        int daprHttpPort = 3502,
        int daprGrpcPort = 50002,
        string? daprPlacementHostAddress = null,
        string? daprSchedulerHostAddress = null)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(stateStore);
        ArgumentNullException.ThrowIfNull(pubSub);
        ArgumentNullException.ThrowIfNull(secretStore);
        ArgumentException.ThrowIfNullOrWhiteSpace(llmComponentPath);
        ArgumentException.ThrowIfNullOrWhiteSpace(eventStoreTopic);
        ArgumentException.ThrowIfNullOrWhiteSpace(serverName);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(daprHttpPort);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(daprHttpPort, 65535);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(daprGrpcPort);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(daprGrpcPort, 65535);

        // Static dev conversation (echo) component, loaded from the consumer-owned YAML file. The secret store is
        // never created here -- it is externally provisioned and passed in by the consumer (Story 29.2).
        IResourceBuilder<IDaprComponentResource> llm = builder.AddDaprComponent(
            "memories-llm",
            "conversation.echo",
            new DaprComponentOptions { LocalPath = llmComponentPath });

        // FalkorDB graph store for the Memories graph index.
        IResourceBuilder<ContainerResource> falkorDb = builder
            .AddContainer("memories-graphs", "falkordb/falkordb")
            .WithEndpoint(targetPort: 6379, name: "falkordb");
        EndpointReference falkorDbEndpoint = falkorDb.GetEndpoint("falkordb");

        string? redisSearchConnectionString = string.IsNullOrWhiteSpace(redisConnectionString)
            ? null
            : redisConnectionString.Trim();
        IResourceBuilder<ContainerResource>? redisSearch = null;
        EndpointReference? redisSearchEndpoint = null;
        if (redisSearchConnectionString is null)
        {
            redisSearch = builder
                .AddContainer("memories-vectors", "redis/redis-stack")
                .WithEndpoint(targetPort: 6379, name: "redis");
            redisSearchEndpoint = redisSearch.GetEndpoint("redis");
        }

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
            // Redis Stack for the Memories vector/search store; FalkorDB for the graph store.
            .WithEnvironment("ConnectionStrings__falkordb", ReferenceExpression.Create($"{falkorDbEndpoint.Property(EndpointProperty.HostAndPort)}"))
            // The controller subscription binding uses [Topic("pubsub", "$(MEMORIES_EVENTSTORE_TOPIC)")].
            .WithEnvironment("MEMORIES_EVENTSTORE_TOPIC", eventStoreTopic)
            .WaitFor(falkorDb)
            .WaitFor(secretStore)
            .WaitFor(llm);

        server = redisSearch is null || redisSearchEndpoint is null
            ? server.WithEnvironment("ConnectionStrings__redis", redisSearchConnectionString!)
            : server
                .WithEnvironment("ConnectionStrings__redis", ReferenceExpression.Create($"{redisSearchEndpoint.Property(EndpointProperty.HostAndPort)}"))
                .WaitFor(redisSearch);

#pragma warning disable CS0618 // CommunityToolkit.Aspire.Hosting.Dapr requires project-level component references.
        server = server
            .WithReference(stateStore)
            .WithReference(pubSub)
            .WithReference(secretStore)
            .WithReference(llm);
#pragma warning restore CS0618

        return new HexalithMemoriesSearchIndexServerResources(server, falkorDb, secretStore, llm);
    }
}
