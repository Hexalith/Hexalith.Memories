// <copyright file="EmbeddingMigrationUnitCounters.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Migration;

/// <summary>Processed, skipped, missing, and failed counts for one migration content kind.</summary>
/// <param name="Processed">The number of units successfully processed.</param>
/// <param name="Skipped">The number of units skipped because they already match the target metadata.</param>
/// <param name="Missing">The number of units skipped because the source data was absent or empty.</param>
/// <param name="Failed">The number of units that failed.</param>
public sealed record EmbeddingMigrationUnitCounters(int Processed, int Skipped, int Missing, int Failed)
{
    /// <summary>Gets an empty counter value.</summary>
    public static EmbeddingMigrationUnitCounters Empty { get; } = new(0, 0, 0, 0);

    /// <summary>Gets the total number of units the loop has accounted for.</summary>
    public int Completed => Processed + Skipped + Missing + Failed;

    /// <summary>Returns a copy with one additional processed unit.</summary>
    public EmbeddingMigrationUnitCounters AddProcessed() => this with { Processed = Processed + 1 };

    /// <summary>Returns a copy with one additional skipped unit.</summary>
    public EmbeddingMigrationUnitCounters AddSkipped() => this with { Skipped = Skipped + 1 };

    /// <summary>Returns a copy with one additional missing unit.</summary>
    public EmbeddingMigrationUnitCounters AddMissing() => this with { Missing = Missing + 1 };

    /// <summary>Returns a copy with one additional failed unit.</summary>
    public EmbeddingMigrationUnitCounters AddFailed() => this with { Failed = Failed + 1 };
}
