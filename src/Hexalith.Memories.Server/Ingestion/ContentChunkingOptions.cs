// <copyright file="ContentChunkingOptions.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Ingestion;

/// <summary>Replay-safe activity-local options for deterministic raw payload chunking.</summary>
public sealed class ContentChunkingOptions
{
    /// <summary>Gets the configuration section name.</summary>
    public const string SectionName = "Ingestion:Chunking";

    /// <summary>Gets or sets the maximum estimated tokens in one embedding chunk.</summary>
    public int MaxEstimatedTokens { get; set; } = 2048;

    /// <summary>Gets or sets the bounded overlap between adjacent chunks, in estimated tokens.</summary>
    public int OverlapEstimatedTokens { get; set; } = 128;

    /// <summary>Gets or sets the conservative character-to-token divisor used when no provider tokenizer is available locally.</summary>
    public int CharactersPerEstimatedToken { get; set; } = 4;

    /// <summary>Gets or sets the maximum number of chunks sent in one provider batch call.</summary>
    public int MaxChunksPerBatch { get; set; } = 32;
}
