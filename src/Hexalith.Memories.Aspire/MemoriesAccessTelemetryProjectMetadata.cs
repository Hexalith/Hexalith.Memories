// <copyright file="MemoriesAccessTelemetryProjectMetadata.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;

namespace Hexalith.Memories.Aspire;

/// <summary>Cross-repo metadata for the portable access-telemetry lifecycle service.</summary>
internal sealed class MemoriesAccessTelemetryProjectMetadata : IProjectMetadata
{
    /// <inheritdoc/>
    public string ProjectPath => RepositoryProjectPaths.GetReferencedModuleProjectPath(
        "Hexalith.Memories",
        "src",
        "Hexalith.Memories.AccessTelemetry",
        "Hexalith.Memories.AccessTelemetry.csproj");

    /// <inheritdoc/>
    public bool SuppressBuild => true;
}
