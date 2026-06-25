// <copyright file="HexalithMemoriesSearchIndexServerResources.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

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
