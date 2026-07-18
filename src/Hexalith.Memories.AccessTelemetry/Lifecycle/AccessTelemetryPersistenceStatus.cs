// <copyright file="AccessTelemetryPersistenceStatus.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.AccessTelemetry.Lifecycle;

/// <summary>Bounded outcomes of one serialized lifecycle mutation.</summary>
internal enum AccessTelemetryPersistenceStatus
{
    /// <summary>A new record and index were committed.</summary>
    Inserted,

    /// <summary>The exact immutable record already existed.</summary>
    Idempotent,

    /// <summary>The record was rejected before mutation.</summary>
    Rejected,

    /// <summary>The record identifier conflicts with retained state.</summary>
    Conflict,
}
