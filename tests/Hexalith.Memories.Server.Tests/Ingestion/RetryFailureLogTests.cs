// <copyright file="RetryFailureLogTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Tests.Ingestion;

using Hexalith.Memories.Server.Ingestion;

using Microsoft.Extensions.Logging;

using Shouldly;

public class RetryFailureLogTests
{
    [Fact]
    public void LogRetryAttemptStarted_EmitsEvent6301AtDebug()
    {
        CapturingLogger logger = new();

        RetryFailureLog.LogRetryAttemptStarted(logger, "GenerateEmbeddingActivity", "mu-1", 2);

        logger.Entries.Count.ShouldBe(1);
        logger.Entries[0].EventId.Id.ShouldBe(6301);
        logger.Entries[0].Level.ShouldBe(LogLevel.Debug);
        logger.Entries[0].Message.ShouldContain("GenerateEmbeddingActivity");
    }

    [Fact]
    public void LogRetryExhausted_EmitsEvent6302AtWarning()
    {
        CapturingLogger logger = new();

        RetryFailureLog.LogRetryExhausted(logger, "GenerateEmbeddingActivity", "mu-1", "PROVIDER_500");

        logger.Entries.Count.ShouldBe(1);
        logger.Entries[0].EventId.Id.ShouldBe(6302);
        logger.Entries[0].Level.ShouldBe(LogLevel.Warning);
        logger.Entries[0].Message.ShouldContain("PROVIDER_500");
    }

    [Fact]
    public void LogFailedUnitPersisted_EmitsEvent6303AtInformation()
    {
        CapturingLogger logger = new();

        RetryFailureLog.LogFailedUnitPersisted(logger, "tenant-a", "mu-1", "embedding", "PROVIDER_500");

        logger.Entries.Count.ShouldBe(1);
        logger.Entries[0].EventId.Id.ShouldBe(6303);
        logger.Entries[0].Level.ShouldBe(LogLevel.Information);
        logger.Entries[0].Message.ShouldContain("mu-1");
    }

    [Fact]
    public void LogReIngestionScheduled_EmitsEvent6304AtInformation()
    {
        CapturingLogger logger = new();

        RetryFailureLog.LogReIngestionScheduled(logger, "tenant-a", "case-x", "mu-2", "wf-99");

        logger.Entries.Count.ShouldBe(1);
        logger.Entries[0].EventId.Id.ShouldBe(6304);
        logger.Entries[0].Level.ShouldBe(LogLevel.Information);
        logger.Entries[0].Message.ShouldContain("wf-99");
    }

    [Fact]
    public void LogBulkReIngestionUnitSkipped_EmitsEvent6305AtWarning()
    {
        CapturingLogger logger = new();

        RetryFailureLog.LogBulkReIngestionUnitSkipped(logger, "tenant-a", "mu-3", "conflict");

        logger.Entries.Count.ShouldBe(1);
        logger.Entries[0].EventId.Id.ShouldBe(6305);
        logger.Entries[0].Level.ShouldBe(LogLevel.Warning);
    }

    [Fact]
    public void LogFailedUnitsListQueried_EmitsEvent6306AtDebug()
    {
        CapturingLogger logger = new();

        RetryFailureLog.LogFailedUnitsListQueried(logger, "t", "c", 50, 0, 12, 12);

        logger.Entries.Count.ShouldBe(1);
        logger.Entries[0].EventId.Id.ShouldBe(6306);
        logger.Entries[0].Level.ShouldBe(LogLevel.Debug);
    }

    [Fact]
    public void LogCounterActorTransitionApplied_EmitsEvent6307AtDebug()
    {
        CapturingLogger logger = new();

        RetryFailureLog.LogCounterActorTransitionApplied(logger, "t", "c", "queued", "extracting", "tx-1");

        logger.Entries.Count.ShouldBe(1);
        logger.Entries[0].EventId.Id.ShouldBe(6307);
        logger.Entries[0].Level.ShouldBe(LogLevel.Debug);
    }

    [Fact]
    public void LogCounterActorTransitionIdempotent_EmitsEvent6308AtDebug()
    {
        CapturingLogger logger = new();

        RetryFailureLog.LogCounterActorTransitionIdempotent(logger, "t", "c", "tx-1");

        logger.Entries.Count.ShouldBe(1);
        logger.Entries[0].EventId.Id.ShouldBe(6308);
        logger.Entries[0].Level.ShouldBe(LogLevel.Debug);
        logger.Entries[0].Message.ShouldContain("tx-1");
    }

    [Fact]
    public void LogFailedUnitPersistenceFailed_EmitsEvent6309AtError()
    {
        CapturingLogger logger = new();

        RetryFailureLog.LogFailedUnitPersistenceFailed(logger, "mu-99", "redis hiccup");

        logger.Entries.Count.ShouldBe(1);
        logger.Entries[0].EventId.Id.ShouldBe(6309);
        logger.Entries[0].Level.ShouldBe(LogLevel.Error);
        logger.Entries[0].Message.ShouldContain("redis hiccup");
    }

    [Fact]
    public void LogCounterTransitionFailed_EmitsEvent6310AtWarning()
    {
        CapturingLogger logger = new();

        RetryFailureLog.LogCounterTransitionFailed(logger, "t", "c", "queued", "none", "actor unreachable");

        logger.Entries.Count.ShouldBe(1);
        logger.Entries[0].EventId.Id.ShouldBe(6310);
        logger.Entries[0].Level.ShouldBe(LogLevel.Warning);
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
