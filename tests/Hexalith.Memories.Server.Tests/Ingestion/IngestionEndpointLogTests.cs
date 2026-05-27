// <copyright file="IngestionEndpointLogTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Tests.Ingestion;

using Hexalith.Memories.Server.Ingestion;

using Microsoft.Extensions.Logging.Abstractions;

using Shouldly;

public class IngestionEndpointLogTests
{
    [Fact]
    public void RedactUrl_DropsQueryAndFragment()
    {
        Uri uri = new("https://example.com/path?token=secret&q=foo#frag");

        string redacted = IngestionEndpointLog.RedactUrl(uri);

        redacted.ShouldBe("https://example.com/path");
        redacted.ShouldNotContain("secret");
        redacted.ShouldNotContain("frag");
    }

    [Fact]
    public void RedactUrl_PreservesSchemeHostPath()
    {
        Uri uri = new("https://example.com/api/v1/doc");

        IngestionEndpointLog.RedactUrl(uri).ShouldBe("https://example.com/api/v1/doc");
    }

    [Fact]
    public void RedactUrl_InvalidString_ReturnsPlaceholder()
    {
        IngestionEndpointLog.RedactUrl("definitely not a uri").ShouldBe("(invalid-url)");
    }

    [Fact]
    public void RedactUrl_NullUri_Throws()
    {
        Should.Throw<ArgumentNullException>(() => IngestionEndpointLog.RedactUrl((Uri)null!));
    }

    [Fact]
    public void LogUrlIngestionScheduled_WithNullLogger_DoesNotThrow()
    {
        // Smoke test — [LoggerMessage] partial methods must be callable.
        Should.NotThrow(() => IngestionEndpointLog.LogUrlIngestionScheduled(
            NullLogger.Instance, "t1", "c1", "inst-1", "https://example.com/x"));
    }

    [Fact]
    public void LogUrlFetchCompleted_WithNullLogger_DoesNotThrow()
    {
        Should.NotThrow(() => IngestionEndpointLog.LogUrlFetchCompleted(
            NullLogger.Instance, "mu-1", 200, 1024L, 42L, "https://example.com/final"));
    }

    [Fact]
    public void LogDirectoryBatchScheduled_WithNullLogger_DoesNotThrow()
    {
        Should.NotThrow(() => IngestionEndpointLog.LogDirectoryBatchScheduled(
            NullLogger.Instance, "t1", "c1", "batch-1", 10, 8, 2));
    }

    [Fact]
    public void LogUrlIngestionRejected_WithNullLogger_DoesNotThrow()
    {
        Should.NotThrow(() => IngestionEndpointLog.LogUrlIngestionRejected(
            NullLogger.Instance, "t1", "c1", "https://example.com/x", "INVALID_URL"));
    }

    [Fact]
    public void LogDirectoryBatchRejected_WithNullLogger_DoesNotThrow()
    {
        Should.NotThrow(() => IngestionEndpointLog.LogDirectoryBatchRejected(
            NullLogger.Instance, "t1", "c1", null, "INVALID_DIRECTORY_PATH", @"D:\\files"));
    }

    [Fact]
    public void LogUrlFetchFailed_WithNullLogger_DoesNotThrow()
    {
        Should.NotThrow(() => IngestionEndpointLog.LogUrlFetchFailed(
            NullLogger.Instance, "mu-1", "URL_CLIENT_ERROR", 404, 25));
    }
}
