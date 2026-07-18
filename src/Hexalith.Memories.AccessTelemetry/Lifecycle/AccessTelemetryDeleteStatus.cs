// <copyright file="AccessTelemetryDeleteStatus.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.AccessTelemetry.Lifecycle;

/// <summary>Strong delete-and-verify outcome for one expiry entry.</summary>
internal enum AccessTelemetryDeleteStatus
{
    /// <summary>The matching record was deleted and absence was verified.</summary>
    Deleted,

    /// <summary>Component TTL already removed the matching record and the index was removed.</summary>
    AlreadyAbsent,

    /// <summary>The index referred to a different, newer record and only the stale index was removed.</summary>
    StaleIndex,

    /// <summary>The matching record remained after deletion.</summary>
    VerificationFailed,
}
