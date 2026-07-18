// <copyright file="AccessTelemetryAuthorityAction.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.AccessTelemetry.Capability;

/// <summary>Bounded privileged actions.</summary>
internal enum AccessTelemetryAuthorityAction
{
    /// <summary>Invoke write.</summary>
    Write,

    /// <summary>Send heartbeat.</summary>
    Heartbeat,

    /// <summary>Read retained state.</summary>
    Read,

    /// <summary>Delete retained state.</summary>
    Delete,

    /// <summary>Rotate marker keys.</summary>
    RotateKeys,

    /// <summary>Sign independent time.</summary>
    SignTime,

    /// <summary>Inspect sanitized operational evidence.</summary>
    SanitizedInspect,

    /// <summary>Collect backend-specific physical evidence.</summary>
    PhysicalEvidence,
}
