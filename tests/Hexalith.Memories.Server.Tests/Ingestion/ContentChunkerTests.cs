// <copyright file="ContentChunkerTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Tests.Ingestion;

using Hexalith.Memories.Server.Ingestion;

using Shouldly;

public class ContentChunkerTests
{
    [Fact]
    public void Split_SmallText_ReturnsSingleChunk()
    {
        ContentChunker chunker = CreateChunker(maxTokens: 4, overlapTokens: 1, charsPerToken: 4);

        IReadOnlyList<ContentChunk> chunks = chunker.Split("small text");

        ContentChunk chunk = chunks.ShouldHaveSingleItem();
        chunk.Sequence.ShouldBe(0);
        chunk.Text.ShouldBe("small text");
        chunk.StartOffset.ShouldBe(0);
        chunk.EndOffset.ShouldBe(10);
    }

    [Fact]
    public void Split_LargeText_ReturnsOrderedChunksWithBoundedOverlapAndNoTextLoss()
    {
        ContentChunker chunker = CreateChunker(maxTokens: 2, overlapTokens: 1, charsPerToken: 4);

        IReadOnlyList<ContentChunk> chunks = chunker.Split("abcdefghijklmnop");

        chunks.Count.ShouldBe(3);
        chunks.Select(c => c.Sequence).ShouldBe([0, 1, 2]);
        chunks.Select(c => c.Text).ShouldBe(["abcdefgh", "efghijkl", "ijklmnop"]);
        chunks[1].StartOffset.ShouldBe(chunks[0].EndOffset - 4);
        chunks[2].StartOffset.ShouldBe(chunks[1].EndOffset - 4);
        string reconstructed = chunks[0].Text + chunks[1].Text[4..] + chunks[2].Text[4..];
        reconstructed.ShouldBe("abcdefghijklmnop");
    }

    [Fact]
    public void Split_EmptyText_ThrowsArgumentException()
    {
        ContentChunker chunker = CreateChunker(maxTokens: 2, overlapTokens: 1, charsPerToken: 4);

        Should.Throw<ArgumentException>(() => chunker.Split(" "));
    }

    [Fact]
    public void Split_WhitespaceOnlyWindow_ThrowsInsteadOfDroppingText()
    {
        ContentChunker chunker = CreateChunker(maxTokens: 1, overlapTokens: 0, charsPerToken: 2);

        Should.Throw<InvalidOperationException>(() => chunker.Split("a    b"));
    }

    private static ContentChunker CreateChunker(int maxTokens, int overlapTokens, int charsPerToken)
        => new(new ContentChunkingOptions
        {
            MaxEstimatedTokens = maxTokens,
            OverlapEstimatedTokens = overlapTokens,
            CharactersPerEstimatedToken = charsPerToken,
        });
}
