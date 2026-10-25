// <copyright file="AccessTelemetryLifecycleBootstrapFailureKind.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Telemetry.AccessTelemetryLifecycle;

/// <summary>
/// The fixed allow-list of bootstrap failure kinds. A kind never carries exception text, messages, URLs, keys or values.
/// </summary>
internal enum AccessTelemetryLifecycleBootstrapFailureKind
{
    /// <summary>The retention configuration was absent or invalid, or a validated marker key changed in place.</summary>
    ConfigurationInvalid,

    /// <summary>The marker-key secret was absent or blank.</summary>
    SecretMissing,

    /// <summary>
    /// The lifecycle application refused the runtime configuration, or a Dapr secret, configuration or invocation call was
    /// denied (HTTP 401/403, gRPC PermissionDenied/Unauthenticated); the failure stage names the denied call.
    /// </summary>
    RemoteDenied,

    /// <summary>A dependency call timed out.</summary>
    Timeout,

    /// <summary>A dependency call was canceled without the host stopping.</summary>
    Canceled,

    /// <summary>The sidecar or remote endpoint could not be reached.</summary>
    Transport,

    /// <summary>The Dapr API or the invoked application returned another failure.</summary>
    DaprApi,

    /// <summary>A value could not be decoded, such as a malformed marker key or validation response.</summary>
    Format,

    /// <summary>Any other unexpected failure.</summary>
    Other,
}
