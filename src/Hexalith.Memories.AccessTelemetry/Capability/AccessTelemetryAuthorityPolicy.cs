// <copyright file="AccessTelemetryAuthorityPolicy.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.AccessTelemetry.Capability;

/// <summary>Explicit least-privilege authority matrix.</summary>
internal static class AccessTelemetryAuthorityPolicy
{
    /// <summary>Checks whether an authority owns an action.</summary>
    public static bool Allows(AccessTelemetryAuthority authority, AccessTelemetryAuthorityAction action)
        => authority switch
        {
            AccessTelemetryAuthority.ServerWriter => action is AccessTelemetryAuthorityAction.Write or AccessTelemetryAuthorityAction.Heartbeat,
            AccessTelemetryAuthority.LifecycleService => action is AccessTelemetryAuthorityAction.Write or AccessTelemetryAuthorityAction.Read or AccessTelemetryAuthorityAction.Delete or AccessTelemetryAuthorityAction.RotateKeys,
            AccessTelemetryAuthority.Clock => action == AccessTelemetryAuthorityAction.SignTime,
            AccessTelemetryAuthority.Inspector => action == AccessTelemetryAuthorityAction.SanitizedInspect,
            AccessTelemetryAuthority.AdapterEvidence => action == AccessTelemetryAuthorityAction.PhysicalEvidence,
            _ => false,
        };
}
