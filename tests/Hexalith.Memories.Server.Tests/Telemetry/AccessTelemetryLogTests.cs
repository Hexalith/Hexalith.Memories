// <copyright file="AccessTelemetryLogTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Tests.Telemetry;

using System.Collections.Generic;
using System.Text.Json;

using Hexalith.Memories.Contracts.V1;
using Hexalith.Memories.Server.Telemetry;
using Hexalith.Memories.Server.Tests.Telemetry.Infrastructure;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

using NSubstitute;

using Shouldly;

/// <summary>Story 7.5 Task 8.3 — asserts each LogXxx emitter produces a single event with the expected id + level.</summary>
public sealed class AccessTelemetryLogTests
{
    [Fact]
    public void CreateEvent_PopulatesAllRequiredFields()
    {
        AccessTelemetryEvent evt = AccessTelemetryLog.CreateEvent(
            eventId: 7501,
            tenantId: "acme",
            operationType: AccessTelemetryLog.OperationSearch,
            caseId: "case-1",
            user: "anonymous",
            queryParams: new Dictionary<string, object?> { ["query"] = "hello" },
            resultCount: 3,
            durationMs: 42,
            outcome: AccessTelemetryLog.OutcomeOk,
            errorCode: null,
            currentActivity: null);

        evt.SchemaVersion.ShouldBe(1);
        evt.EventId.ShouldBe(7501);
        evt.TenantId.ShouldBe("acme");
        evt.OperationType.ShouldBe("search");
        evt.CaseId.ShouldBe("case-1");
        evt.User.ShouldBe("anonymous");
        evt.DurationMs.ShouldBe(42);
        evt.Outcome.ShouldBe("ok");
        evt.ErrorCode.ShouldBeNull();
        evt.ResultCount.ShouldBe(3);
        evt.Timestamp.ShouldNotBeNullOrWhiteSpace();
    }

    [Fact]
    public void AuditEmitters_ProjectPrivateInputsBeforeEveryLoggerProvider()
    {
        const string Query = "sensitive acquisition terms";
        const string Subject = "private strategy";
        const string Uri = "https://example.test/private/customer?token=value";
        using CapturingAuditLoggerProvider first = new();
        using CapturingAuditLoggerProvider second = new();
        using ILoggerFactory factory = LoggerFactory.Create(builder => builder.AddProvider(first).AddProvider(second));
        ILogger<AccessTelemetryCategory> logger = factory.CreateLogger<AccessTelemetryCategory>();

        // A caller can construct typed state directly, so prove the emitter itself enforces projection.
        AccessTelemetryLog.LogSearchAccess(logger, CreateSample(7501, AccessTelemetryLog.OutcomeOk) with
        {
            QueryParams = new Dictionary<string, object?> { ["query"] = Query, ["subject"] = Subject },
        });
        AccessTelemetryLog.LogCaseAccess(logger, CreateSample(7504, AccessTelemetryLog.OutcomeOk) with
        {
            OperationType = AccessTelemetryLog.OperationCaseAccess,
            QueryParams = new Dictionary<string, object?> { ["sourceUri"] = Uri, ["memoryUnitId"] = "private-unit-id" },
        });

        foreach (CapturingAuditLoggerProvider provider in new[] { first, second })
        {
            AuditLogCapture search = provider.AccessTelemetryCaptures.Single(capture => capture.EventId == 7501);
            AuditLogCapture source = provider.AccessTelemetryCaptures.Single(capture => capture.EventId == 7504);
            search.AuditEvent.ShouldNotBeNull().QueryParams["queryLengthBucket"].ShouldBe("1-32");
            search.AuditEvent.QueryParams["subjectPresent"].ShouldBe(true);
            source.AuditEvent.ShouldNotBeNull().QueryParams["accessKind"].ShouldBe("source-uri");
            source.AuditEvent.QueryParams["sourceKind"].ShouldBe("url");
            foreach (AuditLogCapture capture in new[] { search, source })
            {
                string serialized = JsonSerializer.Serialize(capture.Fields);
                foreach (string secret in new[] { Query, Subject, Uri, "private-unit-id" })
                {
                    capture.Message.ShouldNotContain(secret);
                    serialized.ShouldNotContain(secret);
                }
            }
        }
    }

    [Theory]
    [InlineData("ok", AccessTelemetryLog.OutcomeOk)]
    [InlineData("partial", AccessTelemetryLog.OutcomePartial)]
    [InlineData("error", AccessTelemetryLog.OutcomeError)]
    public void OutcomeConstants_ArePinned(string expected, string actual) => actual.ShouldBe(expected);

    [Fact]
    public void OperationConstants_ArePinned()
    {
        AccessTelemetryLog.OperationSearch.ShouldBe("search");
        AccessTelemetryLog.OperationIngest.ShouldBe("ingest");
        AccessTelemetryLog.OperationTraverse.ShouldBe("traverse");
        AccessTelemetryLog.OperationCaseAccess.ShouldBe("case-access");
        AccessTelemetryLog.OperationDelete.ShouldBe("delete");
        AccessTelemetryLog.OperationTenantLifecycle.ShouldBe("tenant-lifecycle");
        AccessTelemetryLog.OperationTenantConfig.ShouldBe("tenant-config");
        AccessTelemetryLog.OperationCaseMember.ShouldBe("case-member");
        AccessTelemetryLog.OperationAnnotation.ShouldBe("annotation");
    }

    [Fact]
    public void LogSearchAccess_UsesEventId7501()
    {
        var logger = new CapturingLogger<AccessTelemetryCategory>();
        AccessTelemetryEvent evt = CreateSample(7501, AccessTelemetryLog.OutcomeOk);
        AccessTelemetryLog.LogSearchAccess(logger, evt);
        logger.Captures.ShouldHaveSingleItem();
        logger.Captures[0].EventId.Id.ShouldBe(7501);
        logger.Captures[0].Level.ShouldBe(LogLevel.Information);
    }

    [Fact]
    public void LogSearchAccessError_UsesEventId7511_Warning()
    {
        var logger = new CapturingLogger<AccessTelemetryCategory>();
        AccessTelemetryEvent evt = CreateSample(7511, AccessTelemetryLog.OutcomeError);
        AccessTelemetryLog.LogSearchAccessError(logger, evt);
        logger.Captures.ShouldHaveSingleItem();
        logger.Captures[0].EventId.Id.ShouldBe(7511);
        logger.Captures[0].Level.ShouldBe(LogLevel.Warning);
    }

    [Fact]
    public void LogIngestAccess_UsesEventId7502()
    {
        var logger = new CapturingLogger<AccessTelemetryCategory>();
        AccessTelemetryLog.LogIngestAccess(logger, CreateSample(7502, AccessTelemetryLog.OutcomeOk));
        logger.Captures[0].EventId.Id.ShouldBe(7502);
    }

    [Fact]
    public void LogTraverseAccess_UsesEventId7503()
    {
        var logger = new CapturingLogger<AccessTelemetryCategory>();
        AccessTelemetryLog.LogTraverseAccess(logger, CreateSample(7503, AccessTelemetryLog.OutcomeOk));
        logger.Captures[0].EventId.Id.ShouldBe(7503);
    }

    [Fact]
    public void LogCaseAccess_UsesEventId7504()
    {
        var logger = new CapturingLogger<AccessTelemetryCategory>();
        AccessTelemetryLog.LogCaseAccess(logger, CreateSample(7504, AccessTelemetryLog.OutcomeOk));
        logger.Captures[0].EventId.Id.ShouldBe(7504);
    }

    [Theory]
    [InlineData(AccessTelemetryLog.OperationTenantLifecycle, 7506, 7516)]
    [InlineData(AccessTelemetryLog.OperationTenantConfig, 7507, 7517)]
    [InlineData(AccessTelemetryLog.OperationCaseMember, 7508, 7518)]
    [InlineData(AccessTelemetryLog.OperationAnnotation, 7509, 7519)]
    public void AddedMutationLoggers_UsePinnedEventIds(string operation, int successId, int errorId)
    {
        var logger = new CapturingLogger<AccessTelemetryCategory>();
        AccessTelemetryEvent success = CreateSample(successId, AccessTelemetryLog.OutcomeOk) with { OperationType = operation };
        AccessTelemetryEvent error = CreateSample(errorId, AccessTelemetryLog.OutcomeError) with { OperationType = operation };

        switch (operation)
        {
            case AccessTelemetryLog.OperationTenantLifecycle:
                AccessTelemetryLog.LogTenantLifecycleAccess(logger, success);
                AccessTelemetryLog.LogTenantLifecycleAccessError(logger, error);
                break;
            case AccessTelemetryLog.OperationTenantConfig:
                AccessTelemetryLog.LogTenantConfigAccess(logger, success);
                AccessTelemetryLog.LogTenantConfigAccessError(logger, error);
                break;
            case AccessTelemetryLog.OperationCaseMember:
                AccessTelemetryLog.LogCaseMemberAccess(logger, success);
                AccessTelemetryLog.LogCaseMemberAccessError(logger, error);
                break;
            case AccessTelemetryLog.OperationAnnotation:
                AccessTelemetryLog.LogAnnotationAccess(logger, success);
                AccessTelemetryLog.LogAnnotationAccessError(logger, error);
                break;
        }

        logger.Captures.Count.ShouldBe(2);
        logger.Captures[0].EventId.Id.ShouldBe(successId);
        logger.Captures[0].Level.ShouldBe(LogLevel.Information);
        logger.Captures[1].EventId.Id.ShouldBe(errorId);
        logger.Captures[1].Level.ShouldBe(LogLevel.Warning);
    }

    private static AccessTelemetryEvent CreateSample(int eventId, string outcome)
        => AccessTelemetryLog.CreateEvent(
            eventId,
            tenantId: "acme",
            operationType: "search",
            caseId: null,
            user: "anonymous",
            queryParams: new Dictionary<string, object?>(0),
            resultCount: null,
            durationMs: 1,
            outcome: outcome,
            errorCode: outcome == AccessTelemetryLog.OutcomeError ? "TEST_ERROR" : null,
            currentActivity: null);

    private sealed class CapturingLogger<T> : ILogger<T>
    {
        public List<(LogLevel Level, EventId EventId, string Message)> Captures { get; } = [];

        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull => NullScope.Instance;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            Captures.Add((logLevel, eventId, formatter(state, exception)));
        }

        private sealed class NullScope : IDisposable
        {
            public static readonly NullScope Instance = new();

            public void Dispose()
            {
            }
        }
    }
}
