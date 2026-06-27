// <copyright file="MemoriesServerProjectMetadata.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;

namespace Hexalith.Memories.Aspire;

/// <summary>
/// Cross-repo project metadata for the Hexalith.Memories search-index server, resolved from the consuming
/// repository's <c>references/Hexalith.Memories</c> submodule. <see cref="SuppressBuild"/> is <see langword="true"/>: the
/// Memories server is built independently of the consuming domain-module AppHost (Aspire runs children with
/// <c>--no-build</c>), so the AppHost build never compiles it and the two repositories' package graphs stay
/// isolated.
/// </summary>
internal sealed class MemoriesServerProjectMetadata : IProjectMetadata
{
    /// <inheritdoc/>
    public string ProjectPath => RepositoryProjectPaths.GetProjectPath(
        "references",
        "Hexalith.Memories",
        "src",
        "Hexalith.Memories.Server",
        "Hexalith.Memories.Server.csproj");

    /// <inheritdoc/>
    public bool SuppressBuild => true;
}
