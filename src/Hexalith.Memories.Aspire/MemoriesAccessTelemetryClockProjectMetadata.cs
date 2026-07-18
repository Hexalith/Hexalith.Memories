// <copyright file="MemoriesAccessTelemetryClockProjectMetadata.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;

namespace Hexalith.Memories.Aspire;

/// <summary>Cross-repo metadata for the independent access-telemetry clock service.</summary>
internal sealed class MemoriesAccessTelemetryClockProjectMetadata : IProjectMetadata
{
    /// <inheritdoc/>
    public string ProjectPath => RepositoryProjectPaths.GetReferencedModuleProjectPath(
        "Hexalith.Memories",
        "src",
        "Hexalith.Memories.AccessTelemetry.Clock",
        "Hexalith.Memories.AccessTelemetry.Clock.csproj");

    /// <inheritdoc/>
    public bool SuppressBuild => true;
}
