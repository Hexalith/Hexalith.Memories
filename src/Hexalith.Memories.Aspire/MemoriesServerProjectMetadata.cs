// <copyright file="MemoriesServerProjectMetadata.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;

namespace Hexalith.Memories.Aspire;

/// <summary>
/// Cross-repo project metadata for the Hexalith.Memories search-index server, resolved from the consuming
/// repository's <c>Hexalith.Memories</c> checkout via
/// <see cref="RepositoryProjectPaths.GetReferencedModuleProjectPath"/>: the repository itself when its root holds
/// <c>Hexalith.Memories.slnx</c>, otherwise the direct submodule <c>references/Hexalith.Memories</c>. Any other layout
/// fails, naming the dependency and the expected path. <see cref="SuppressBuild"/> stays <see langword="true"/> so Aspire launches
/// the server fast with <c>--no-build</c>; the consuming AppHost forces a fresh Debug compile via a build-only
/// <c>&lt;ProjectReference&gt;</c>, while Release builds keep the per-repo package graphs isolated.
/// </summary>
internal sealed class MemoriesServerProjectMetadata : IProjectMetadata
{
    /// <inheritdoc/>
    public string ProjectPath => RepositoryProjectPaths.GetReferencedModuleProjectPath(
        "Hexalith.Memories",
        "src",
        "Hexalith.Memories.Server",
        "Hexalith.Memories.Server.csproj");

    /// <inheritdoc/>
    public bool SuppressBuild => true;
}
