// <copyright file="UrlFetchExceptionTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Tests.Ingestion;

using Hexalith.Memories.Server.Ingestion;

using Shouldly;

public class UrlFetchExceptionTests
{
    [Theory]
    [InlineData("URL_NETWORK_ERROR", true)]
    [InlineData("URL_CLIENT_ERROR", true)]
    [InlineData("URL_SERVER_ERROR", true)]
    [InlineData("URL_TIMEOUT", true)]
    [InlineData("PAYLOAD_TOO_LARGE", false)]
    [InlineData("UNSUPPORTED_CONTENT_TYPE", false)]
    [InlineData("INVALID_URL", false)]
    [InlineData("TOO_MANY_REDIRECTS", false)]
    public void IsRetryable_ClassifiesCodes(string code, bool expected)
    {
        UrlFetchException.IsRetryable(code).ShouldBe(expected);
    }

    [Fact]
    public void Constructor_StoresCodeAndMessage()
    {
        UrlFetchException ex = new("URL_TIMEOUT", "fetch timed out");

        ex.ErrorCode.ShouldBe("URL_TIMEOUT");
        ex.DetailMessage.ShouldBe("fetch timed out");
        ex.Message.ShouldBe("[URL_TIMEOUT] fetch timed out");
    }

    [Fact]
    public void Constructor_WithInner_PreservesInnerException()
    {
        InvalidOperationException inner = new();
        UrlFetchException ex = new("URL_NETWORK_ERROR", "net err", inner);

        ex.InnerException.ShouldBeSameAs(inner);
    }

    [Fact]
    public void TryExtractErrorCode_WithSerializedMessage_ReturnsCode()
    {
        UrlFetchException.TryExtractErrorCode("[UNSUPPORTED_CONTENT_TYPE] nope", out string errorCode).ShouldBeTrue();

        errorCode.ShouldBe("UNSUPPORTED_CONTENT_TYPE");
    }
}
