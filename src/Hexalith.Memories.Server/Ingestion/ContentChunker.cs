// <copyright file="ContentChunker.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Ingestion;

/// <summary>Deterministically splits extracted payload text into embedding-sized chunks.</summary>
public sealed class ContentChunker
{
    private readonly ContentChunkingOptions _options;

    /// <summary>Initializes a new instance of the <see cref="ContentChunker"/> class.</summary>
    /// <param name="options">Chunking options captured inside the activity boundary.</param>
    public ContentChunker(ContentChunkingOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        if (options.MaxEstimatedTokens <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(options), "MaxEstimatedTokens must be positive.");
        }

        if (options.CharactersPerEstimatedToken <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(options), "CharactersPerEstimatedToken must be positive.");
        }

        if (options.OverlapEstimatedTokens < 0 || options.OverlapEstimatedTokens >= options.MaxEstimatedTokens)
        {
            throw new ArgumentOutOfRangeException(nameof(options), "OverlapEstimatedTokens must be non-negative and smaller than MaxEstimatedTokens.");
        }

        if (options.MaxChunksPerBatch <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(options), "MaxChunksPerBatch must be positive.");
        }

        _options = options;
    }

    /// <summary>Splits the supplied text into ordered non-empty chunks.</summary>
    /// <param name="text">The extracted payload content.</param>
    /// <returns>Ordered chunks with stable sequence numbers and source ranges.</returns>
    public IReadOnlyList<ContentChunk> Split(string text)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(text);

        int maxChars = _options.MaxEstimatedTokens * _options.CharactersPerEstimatedToken;
        int overlapChars = _options.OverlapEstimatedTokens * _options.CharactersPerEstimatedToken;
        if (text.Length <= maxChars)
        {
            return [CreateChunk(0, text, 0, text.Length)];
        }

        List<ContentChunk> chunks = [];
        int start = 0;
        int sequence = 0;
        while (start < text.Length)
        {
            int end = Math.Min(text.Length, start + maxChars);
            string chunkText = text[start..end];
            if (string.IsNullOrWhiteSpace(chunkText))
            {
                throw new InvalidOperationException(
                    "Chunking produced a whitespace-only range that cannot be embedded without dropping source text.");
            }

            chunks.Add(CreateChunk(sequence++, chunkText, start, end));

            if (end == text.Length)
            {
                break;
            }

            start = Math.Max(start + 1, end - overlapChars);
        }

        return chunks;
    }

    private ContentChunk CreateChunk(int sequence, string text, int startOffset, int endOffset)
        => new(sequence, text, startOffset, endOffset, EstimateTokens(text));

    private int EstimateTokens(string text)
        => Math.Max(1, (int)Math.Ceiling(text.Length / (double)_options.CharactersPerEstimatedToken));
}
