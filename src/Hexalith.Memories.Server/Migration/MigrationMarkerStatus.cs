// <copyright file="MigrationMarkerStatus.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Migration;

/// <summary>Durable status values for the tenant-scoped embedding migration marker.</summary>
public static class MigrationMarkerStatus
{
    /// <summary>Migration cutover began with a fresh start.</summary>
    public const string Started = "started";

    /// <summary>Migration cutover was resumed from a prior interrupted run.</summary>
    public const string Resumed = "resumed";

    /// <summary>Migration completed cleanly; the marker no longer protects writes.</summary>
    public const string Completed = "completed";

    /// <summary>Migration has switched active aliases but has not yet been completed.</summary>
    public const string Cutover = "cutover";

    /// <summary>Migration was aborted by an operator recovery command.</summary>
    public const string Aborted = "aborted";

    /// <summary>Migration was rolled back to the previous active targets.</summary>
    public const string RolledBack = "rolledBack";
}
