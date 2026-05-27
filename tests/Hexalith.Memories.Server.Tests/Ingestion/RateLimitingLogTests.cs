// <copyright file="RateLimitingLogTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Tests.Ingestion;

using Hexalith.Memories.Server.Ingestion;

using Microsoft.Extensions.Logging;

using Shouldly;

public class RateLimitingLogTests
{
    [Fact]
    public void LogRateLimitExceededLocally_ShouldEmitEventId6201AtWarning()
    {
        CapturingLogger logger = new();

        RateLimitingLog.LogRateLimitExceededLocally(logger, "tenant-a");

        logger.Entries.Count.ShouldBe(1);
        logger.Entries[0].Level.ShouldBe(LogLevel.Warning);
        logger.Entries[0].EventId.Id.ShouldBe(6201);
        logger.Entries[0].Message.ShouldContain("tenant-a");
    }

    [Fact]
    public void LogProviderRateLimitReceived_ShouldEmitEventId6202AtWarning()
    {
        CapturingLogger logger = new();

        RateLimitingLog.LogProviderRateLimitReceived(logger, "tenant-b", 60);

        logger.Entries.Count.ShouldBe(1);
        logger.Entries[0].Level.ShouldBe(LogLevel.Warning);
        logger.Entries[0].EventId.Id.ShouldBe(6202);
        logger.Entries[0].Message.ShouldContain("tenant-b");
        logger.Entries[0].Message.ShouldContain("60");
    }

    [Fact]
    public void LogRateLimitActorUpdated_ShouldEmitEventId6203AtInformation()
    {
        CapturingLogger logger = new();
        DateTime windowStart = new(2026, 4, 15, 12, 0, 0, DateTimeKind.Utc);

        RateLimitingLog.LogRateLimitActorUpdated(logger, "tenant-c", 0, windowStart);

        logger.Entries.Count.ShouldBe(1);
        logger.Entries[0].Level.ShouldBe(LogLevel.Information);
        logger.Entries[0].EventId.Id.ShouldBe(6203);
        logger.Entries[0].Message.ShouldContain("tenant-c");
    }

    [Fact]
    public void LogExtractionGateAcquired_ShouldEmitEventId6204AtDebug()
    {
        CapturingLogger logger = new();

        RateLimitingLog.LogExtractionGateAcquired(logger, "tenant-d", 3);

        logger.Entries.Count.ShouldBe(1);
        logger.Entries[0].Level.ShouldBe(LogLevel.Debug);
        logger.Entries[0].EventId.Id.ShouldBe(6204);
        logger.Entries[0].Message.ShouldContain("tenant-d");
        logger.Entries[0].Message.ShouldContain("3");
    }

    [Fact]
    public void LogExtractionGateContended_ShouldEmitEventId6205AtInformation()
    {
        CapturingLogger logger = new();

        RateLimitingLog.LogExtractionGateContended(logger, "tenant-e", 7);

        logger.Entries.Count.ShouldBe(1);
        logger.Entries[0].Level.ShouldBe(LogLevel.Information);
        logger.Entries[0].EventId.Id.ShouldBe(6205);
        logger.Entries[0].Message.ShouldContain("tenant-e");
        logger.Entries[0].Message.ShouldContain("7");
    }

    [Fact]
    public void LogExtractionGateTimeout_ShouldEmitEventId6206AtWarning()
    {
        CapturingLogger logger = new();

        RateLimitingLog.LogExtractionGateTimeout(logger, "tenant-f", 300);

        logger.Entries.Count.ShouldBe(1);
        logger.Entries[0].Level.ShouldBe(LogLevel.Warning);
        logger.Entries[0].EventId.Id.ShouldBe(6206);
        logger.Entries[0].Message.ShouldContain("tenant-f");
        logger.Entries[0].Message.ShouldContain("300");
    }

    private sealed class CapturingLogger : ILogger
    {
        public List<(LogLevel Level, EventId EventId, string Message)> Entries { get; } = [];

        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
            => Entries.Add((logLevel, eventId, formatter(state, exception)));
    }
}
