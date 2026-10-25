// <copyright file="AccessTelemetryLifecycleBootstrapStage.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Telemetry.AccessTelemetryLifecycle;

/// <summary>The fixed, secret-free stages of the lifecycle sanitizer bootstrap.</summary>
internal enum AccessTelemetryLifecycleBootstrapStage
{
    /// <summary>No bootstrap attempt has started.</summary>
    NotStarted,

    /// <summary>The retention configuration is being read and validated.</summary>
    Configuration,

    /// <summary>The marker-key secret is being read and decoded.</summary>
    Secret,

    /// <summary>The lifecycle application is validating the runtime configuration.</summary>
    RemoteValidation,

    /// <summary>The attempt completed with a validated sanitizer published.</summary>
    Published,
}
