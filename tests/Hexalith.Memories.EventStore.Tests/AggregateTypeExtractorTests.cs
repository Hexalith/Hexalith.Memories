// <copyright file="AggregateTypeExtractorTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.EventStore.Tests;

using Hexalith.Memories.EventStore;

using Shouldly;

public sealed class AggregateTypeExtractorTests
{
    [Fact]
    public void Extract_ThreeDottedSegments_ReturnsSecondSegment()
        => AggregateTypeExtractor.Extract("MyApp.Claims.ClaimSubmittedV2").ShouldBe("Claims");

    [Fact]
    public void Extract_FourDottedSegments_ReturnsSecondSegment()
        => AggregateTypeExtractor.Extract("MyApp.Orders.Order.PlacedV1").ShouldBe("Orders");

    [Fact]
    public void Extract_SingleSegment_ReturnsFullValue()
        => AggregateTypeExtractor.Extract("BareType").ShouldBe("BareType");

    [Fact]
    public void Extract_TwoSegmentsEmptySecond_FallsBackToFullValue()
        => AggregateTypeExtractor.Extract("MyApp..").ShouldBe("MyApp..");

    [Fact]
    public void Extract_EmptyInput_Throws()
        => Should.Throw<ArgumentException>(() => AggregateTypeExtractor.Extract(string.Empty));
}
