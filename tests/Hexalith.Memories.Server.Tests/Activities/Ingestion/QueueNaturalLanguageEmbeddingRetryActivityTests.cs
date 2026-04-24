// <copyright file="QueueNaturalLanguageEmbeddingRetryActivityTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Tests.Activities.Ingestion;

using System.Text;

using Hexalith.Memories.Server.Activities.Ingestion;

using Shouldly;

public class QueueNaturalLanguageEmbeddingRetryActivityTests
{
    [Fact]
    public void Truncate_ShortString_ReturnsUnchanged()
    {
        QueueNaturalLanguageEmbeddingRetryActivity.Truncate("{}", 4096).ShouldBe("{}");
    }

    [Fact]
    public void Truncate_LongString_TruncatesToMaxBytes()
    {
        string input = new string('A', 5000);
        string result = QueueNaturalLanguageEmbeddingRetryActivity.Truncate(input, 4096);

        Encoding.UTF8.GetByteCount(result).ShouldBeLessThanOrEqualTo(4096);
    }

    [Fact]
    public void Truncate_EmptyInput_ReturnsEmpty()
    {
        QueueNaturalLanguageEmbeddingRetryActivity.Truncate(string.Empty, 4096).ShouldBe(string.Empty);
    }

    [Fact]
    public void Truncate_NullInput_ReturnsEmpty()
    {
        QueueNaturalLanguageEmbeddingRetryActivity.Truncate(null!, 4096).ShouldBe(string.Empty);
    }

    [Fact]
    public void Truncate_MultiByteInput_DoesNotEmitReplacementCharacterOrOverflowByteBudget()
    {
        string input = string.Concat(Enumerable.Repeat("🙂", 8));

        string result = QueueNaturalLanguageEmbeddingRetryActivity.Truncate(input, 9);

        Encoding.UTF8.GetByteCount(result).ShouldBeLessThanOrEqualTo(9);
        result.ShouldNotContain('�');
        result.ShouldBe("🙂🙂");
    }

    [Fact]
    public void Truncate_ZeroBudget_ReturnsEmpty()
    {
        QueueNaturalLanguageEmbeddingRetryActivity.Truncate("payload", 0).ShouldBe(string.Empty);
    }
}
