// <copyright file="EmbeddingClientRetryAfterParsingTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Tests.Ingestion;

using System.Net.Http.Headers;

using Hexalith.Memories.Server.Ingestion;

using Shouldly;

public class EmbeddingClientRetryAfterParsingTests
{
    [Fact]
    public void Parse_NullHeader_ReturnsZero()
    {
        EmbeddingClient.ParseRetryAfterSeconds(null).ShouldBe(0);
    }

    [Theory]
    [InlineData(30, 30)]
    [InlineData(1, 1)]
    [InlineData(3600, 3600)]
    [InlineData(5000, 3600)]   // upper-clamped
    public void Parse_DeltaSeconds_ReturnsClampedValue(int deltaSeconds, int expected)
    {
        RetryConditionHeaderValue header = new(TimeSpan.FromSeconds(deltaSeconds));

        EmbeddingClient.ParseRetryAfterSeconds(header).ShouldBe(expected);
    }

    [Fact]
    public void Parse_DeltaZero_ReturnsZero()
    {
        // Delta = 0 is not a positive provider pause, so callers use their default fallback.
        RetryConditionHeaderValue header = new(TimeSpan.Zero);

        EmbeddingClient.ParseRetryAfterSeconds(header).ShouldBe(0);
    }

    [Fact]
    public void Parse_HttpDateInFuture_ReturnsDelta()
    {
        DateTimeOffset future = DateTimeOffset.UtcNow.AddSeconds(45);
        RetryConditionHeaderValue header = new(future);

        int parsed = EmbeddingClient.ParseRetryAfterSeconds(header);

        parsed.ShouldBeInRange(43, 46);
    }

    [Fact]
    public void Parse_HttpDateInPast_ReturnsZero()
    {
        DateTimeOffset past = DateTimeOffset.UtcNow.AddSeconds(-60);
        RetryConditionHeaderValue header = new(past);

        EmbeddingClient.ParseRetryAfterSeconds(header).ShouldBe(0);
    }

    [Fact]
    public void Parse_HttpDateFarFuture_ClampsTo3600()
    {
        DateTimeOffset farFuture = DateTimeOffset.UtcNow.AddHours(2);
        RetryConditionHeaderValue header = new(farFuture);

        EmbeddingClient.ParseRetryAfterSeconds(header).ShouldBe(3600);
    }
}
