// <copyright file="EmbeddingMigrationExitCodes.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Migration;

/// <summary>Exit codes returned by the embedding vector migration command.</summary>
public static class EmbeddingMigrationExitCodes
{
    /// <summary>Successful migration or dry-run.</summary>
    public const int Success = 0;

    /// <summary>Domain failure, such as failed units or unavailable rollback state.</summary>
    public const int DomainError = 1;

    /// <summary>Command-line or infrastructure configuration failure.</summary>
    public const int Plumbing = 2;

    /// <summary>Operation cancelled by the caller.</summary>
    public const int Cancelled = 130;
}
