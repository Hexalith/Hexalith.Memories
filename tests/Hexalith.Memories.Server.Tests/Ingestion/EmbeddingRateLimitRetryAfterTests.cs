// <copyright file="EmbeddingRateLimitRetryAfterTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Tests.Ingestion;

using Hexalith.Memories.Server.Ingestion;

using Shouldly;

public class EmbeddingRateLimitRetryAfterTests
{
    [Theory]
    [InlineData(90, 90)]
    [InlineData(0, 30)]
    [InlineData(-5, 30)]
    [InlineData(5000, 3600)]
    public void NormalizeSeconds_ShouldDefaultAndClamp(int input, int expected)
        => EmbeddingRateLimitRetryAfter.NormalizeSeconds(input).ShouldBe(expected);

    [Fact]
    public void TryExtractProviderSeconds_WithPositiveMarker_ShouldReturnValue()
    {
        bool found = EmbeddingRateLimitRetryAfter.TryExtractProviderSeconds(
            "Embedding provider rate limit exceeded. ProviderRetryAfterSeconds=90.",
            out int retryAfterSeconds);

        found.ShouldBeTrue();
        retryAfterSeconds.ShouldBe(90);
    }

    [Fact]
    public void TryExtractProviderSeconds_WithMalformedMarker_ShouldReturnDefault()
    {
        bool found = EmbeddingRateLimitRetryAfter.TryExtractProviderSeconds(
            "Embedding provider rate limit exceeded. ProviderRetryAfterSeconds=abc.",
            out int retryAfterSeconds);

        found.ShouldBeTrue();
        retryAfterSeconds.ShouldBe(30);
    }

    [Fact]
    public void TryExtractProviderSeconds_WithExcessiveMarker_ShouldClamp()
    {
        bool found = EmbeddingRateLimitRetryAfter.TryExtractProviderSeconds(
            "Embedding provider rate limit exceeded. ProviderRetryAfterSeconds=5000.",
            out int retryAfterSeconds);

        found.ShouldBeTrue();
        retryAfterSeconds.ShouldBe(3600);
    }

    [Fact]
    public void TryExtractProviderSeconds_WithoutProviderMarker_ShouldReturnFalse()
    {
        bool found = EmbeddingRateLimitRetryAfter.TryExtractProviderSeconds(
            "Embedding rate limit exceeded for tenant 'tenant-a'.",
            out int retryAfterSeconds);

        found.ShouldBeFalse();
        retryAfterSeconds.ShouldBe(30);
    }
}
