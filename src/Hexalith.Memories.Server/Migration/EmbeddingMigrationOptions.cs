// <copyright file="EmbeddingMigrationOptions.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Migration;

/// <summary>Options for dry-run, live, resume, and rollback embedding vector migration.</summary>
public sealed class EmbeddingMigrationOptions
{
    /// <summary>Gets or sets the selected command mode.</summary>
    public EmbeddingMigrationMode Mode { get; set; }

    /// <summary>Gets or sets the tenant identifier for live or rollback mode.</summary>
    public string? TenantId { get; set; }

    /// <summary>Gets or sets the target embedding provider.</summary>
    public string? TargetProvider { get; set; }

    /// <summary>Gets or sets the target embedding model.</summary>
    public string? TargetModel { get; set; }

    /// <summary>Gets or sets the target embedding dimensions.</summary>
    public int? TargetDimensions { get; set; }

    /// <summary>Gets or sets the per-batch progress size.</summary>
    public int BatchSize { get; set; } = 100;

    /// <summary>Gets or sets a value indicating whether mutation confirmation is supplied.</summary>
    public bool Yes { get; set; }

    /// <summary>Gets or sets a value indicating whether an interactive prompt may ask for confirmation.</summary>
    public bool Interactive { get; set; }

    /// <summary>Gets or sets a value indicating whether the migration is resuming a prior attempt.</summary>
    public bool Resume { get; set; }

    /// <summary>Gets or sets the output format requested by the command.</summary>
    public string Format { get; set; } = "human";

    /// <summary>Gets or sets an optional progress callback used by interactive command hosts.</summary>
    public Func<EmbeddingMigrationProgress, CancellationToken, Task>? ProgressHandler { get; set; }
}
