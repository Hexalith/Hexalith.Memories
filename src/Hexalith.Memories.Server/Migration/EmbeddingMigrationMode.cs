// <copyright file="EmbeddingMigrationMode.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Migration;

/// <summary>Mutation mode selected for the embedding vector migration command.</summary>
public enum EmbeddingMigrationMode
{
    /// <summary>No mode was selected.</summary>
    None,

    /// <summary>Inventory affected tenants without writes.</summary>
    DryRun,

    /// <summary>Execute a live Path A tenant migration.</summary>
    Live,

    /// <summary>Attempt guarded rollback to retained previous-version indexes.</summary>
    Rollback,

    /// <summary>Abort or clean up an interrupted blue/green migration.</summary>
    Abort,
}
