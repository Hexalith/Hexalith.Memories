// <copyright file="AccessTelemetryStoreWriteStatus.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.AccessTelemetry.Lifecycle;

/// <summary>Atomic state-adapter write outcome.</summary>
internal enum AccessTelemetryStoreWriteStatus
{
    /// <summary>Both state keys were written.</summary>
    Inserted,

    /// <summary>The exact record already exists.</summary>
    Idempotent,

    /// <summary>The identifier is already bound to different immutable data.</summary>
    Conflict,
}
