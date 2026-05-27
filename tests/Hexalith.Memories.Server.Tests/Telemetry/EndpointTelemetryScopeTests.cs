// <copyright file="EndpointTelemetryScopeTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Tests.Telemetry;

using System.Collections.Generic;
using System.Diagnostics;

using Hexalith.Memories.Contracts.V1;
using Hexalith.Memories.Server.Telemetry;
using Hexalith.Memories.Telemetry;

using Microsoft.Extensions.Logging;

using Shouldly;

/// <summary>
/// Story 7.5 Tasks 9.2 / 9.3 / 11.2 — Tier-2 coverage for <see cref="EndpointTelemetryScope"/>. Exercises the
/// scope directly (no <c>WebApplicationFactory</c>) with an <see cref="ActivityListener"/> so the activity
/// is materialised, a <see cref="CapturingLogger"/> that collects audit events, and a delegate that stands
/// in for the metric-on-dispose callback endpoints register. Asserts the per-operation-type × per-outcome
/// emission matrix (AC #4) plus the ADR-7.5-004 identity + cardinality invariants (Rev 0.3 finding 1b).
/// </summary>
[Collection(Infrastructure.TelemetryTestCollection.Name)]
public sealed class EndpointTelemetryScopeTests : IDisposable
{
    private readonly ActivityListener _listener;

    public EndpointTelemetryScopeTests()
    {
        _listener = new ActivityListener
        {
            ShouldListenTo = source => source.Name == MemoriesActivitySource.SourceName,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData,
            SampleUsingParentId = (ref ActivityCreationOptions<string> _) => ActivitySamplingResult.AllData,
        };
        ActivitySource.AddActivityListener(_listener);
    }

    public void Dispose() => _listener.Dispose();

    [Theory]
    [InlineData(AccessTelemetryLog.OperationSearch, 7501, 7511, MemoriesActivitySource.SearchRequest)]
    [InlineData(AccessTelemetryLog.OperationIngest, 7502, 7512, MemoriesActivitySource.IngestRequest)]
    [InlineData(AccessTelemetryLog.OperationTraverse, 7503, 7513, MemoriesActivitySource.TraverseRequest)]
    [InlineData(AccessTelemetryLog.OperationCaseAccess, 7504, 7514, MemoriesActivitySource.CaseAccess)]
    [InlineData(AccessTelemetryLog.OperationDelete, 7505, 7515, MemoriesActivitySource.DeleteRequest)]
    public void Dispose_SuccessOutcome_EmitsInformationAuditEvent(string operation, int successId, int errorId, string activityName)
    {
        var logger = new CapturingLogger();

        using (Activity? activity = MemoriesActivitySource.Instance.StartActivity(activityName))
        using (var scope = BuildScope(logger, operation, successId, errorId, activity))
        {
            scope.User = "tenant-admin";
            scope.ResultCount = 3;
            scope.QueryParams = new Dictionary<string, object?> { ["axis"] = "hybrid" };
            scope.CaseId = "case-1";
        }

        Capture single = logger.Captures.ShouldHaveSingleItem();
        single.Level.ShouldBe(LogLevel.Information);
        single.EventId.Id.ShouldBe(successId);
        single.AuditEvent.EventId.ShouldBe(successId);
        single.AuditEvent.OperationType.ShouldBe(operation);
        single.AuditEvent.Outcome.ShouldBe("ok");
        single.AuditEvent.ErrorCode.ShouldBeNull();
        single.AuditEvent.TenantId.ShouldBe("acme");
        single.AuditEvent.User.ShouldBe("tenant-admin");
        single.AuditEvent.ResultCount.ShouldBe(3);
        single.AuditEvent.CaseId.ShouldBe("case-1");
    }

    [Theory]
    [InlineData(AccessTelemetryLog.OperationSearch, 7501, 7511)]
    [InlineData(AccessTelemetryLog.OperationIngest, 7502, 7512)]
    [InlineData(AccessTelemetryLog.OperationTraverse, 7503, 7513)]
    [InlineData(AccessTelemetryLog.OperationCaseAccess, 7504, 7514)]
    [InlineData(AccessTelemetryLog.OperationDelete, 7505, 7515)]
    public void Dispose_ErrorOutcome_EmitsWarningAuditEvent(string operation, int successId, int errorId)
    {
        var logger = new CapturingLogger();

        using (Activity? activity = MemoriesActivitySource.Instance.StartActivity("memories.test"))
        using (var scope = BuildScope(logger, operation, successId, errorId, activity))
        {
            scope.MarkValidationError("INVALID_INPUT");
        }

        Capture single = logger.Captures.ShouldHaveSingleItem();
        single.Level.ShouldBe(LogLevel.Warning);
        single.EventId.Id.ShouldBe(errorId);
        single.AuditEvent.Outcome.ShouldBe("error");
        single.AuditEvent.ErrorCode.ShouldBe("INVALID_INPUT");
    }

    [Fact]
    public void MarkTenantRejected_SwitchesTenantTagToSynthetic()
    {
        var logger = new CapturingLogger();

        using (var scope = BuildScope(logger, AccessTelemetryLog.OperationSearch, 7501, 7511, activity: null))
        {
            scope.TenantIdTag.ShouldBe("acme");
            scope.MarkTenantRejected("TENANT_NOT_FOUND");
            scope.TenantIdTag.ShouldBe(MemoriesMeter.RejectedTenantTag);
        }

        Capture captured = logger.Captures.ShouldHaveSingleItem();
        captured.AuditEvent.TenantId.ShouldBe("__rejected__");
        captured.AuditEvent.ErrorCode.ShouldBe("TENANT_NOT_FOUND");
        captured.AuditEvent.Outcome.ShouldBe("error");
    }

    [Theory]
    [InlineData("INVALID_TENANT_ID")]
    [InlineData("TENANT_NOT_FOUND")]
    [InlineData("TENANT_DELETING")]
    [InlineData("TENANT_PROVISIONING")]
    [InlineData("TENANT_FAILED")]
    [InlineData("TENANT_UNAVAILABLE")]
    public void MarkValidationError_TenantRejectionCode_EmitsSyntheticTenantTag(string errorCode)
    {
        var logger = new CapturingLogger();

        using (var scope = BuildScope(logger, AccessTelemetryLog.OperationSearch, 7501, 7511, activity: null))
        {
            scope.MarkValidationError(errorCode);
        }

        Capture captured = logger.Captures.ShouldHaveSingleItem();
        captured.AuditEvent.TenantId.ShouldBe("__rejected__");
        captured.AuditEvent.ErrorCode.ShouldBe(errorCode);
    }

    [Fact]
    public void MarkValidationError_NonTenantCode_KeepsOriginalTenantTag()
    {
        var logger = new CapturingLogger();

        using (var scope = BuildScope(logger, AccessTelemetryLog.OperationSearch, 7501, 7511, activity: null))
        {
            scope.MarkValidationError("INVALID_INPUT");
        }

        Capture captured = logger.Captures.ShouldHaveSingleItem();
        captured.AuditEvent.TenantId.ShouldBe("acme");
        captured.AuditEvent.ErrorCode.ShouldBe("INVALID_INPUT");
    }

    [Fact]
    public void MarkPartial_EmitsPartialOutcome_ViaSuccessChannel()
    {
        var logger = new CapturingLogger();

        using (var scope = BuildScope(logger, AccessTelemetryLog.OperationSearch, 7501, 7511, activity: null))
        {
            scope.MarkPartial(errorCode: "BACKEND_PARTIALLY_UNAVAILABLE");
        }

        Capture captured = logger.Captures.ShouldHaveSingleItem();
        // Partial is NOT error — goes through the Information/success-eventId channel.
        captured.Level.ShouldBe(LogLevel.Information);
        captured.AuditEvent.Outcome.ShouldBe("partial");
        captured.AuditEvent.ErrorCode.ShouldBe("BACKEND_PARTIALLY_UNAVAILABLE");
    }

    [Fact]
    public void MarkUnhandledException_SetsErrorOutcomeAndDefaultCode()
    {
        var logger = new CapturingLogger();

        using (var scope = BuildScope(logger, AccessTelemetryLog.OperationIngest, 7502, 7512, activity: null))
        {
            scope.MarkUnhandledException(new InvalidOperationException("boom"));
        }

        Capture captured = logger.Captures.ShouldHaveSingleItem();
        captured.Level.ShouldBe(LogLevel.Warning);
        captured.AuditEvent.Outcome.ShouldBe("error");
        captured.AuditEvent.ErrorCode.ShouldBe("UNHANDLED_EXCEPTION");
    }

    [Fact]
    public void MarkUnhandledException_OperationCancelled_UsesRequestCancelledCode()
    {
        var logger = new CapturingLogger();

        using (var scope = BuildScope(logger, AccessTelemetryLog.OperationIngest, 7502, 7512, activity: null))
        {
            scope.MarkUnhandledException(new OperationCanceledException());
        }

        logger.Captures.Single().AuditEvent.ErrorCode.ShouldBe("REQUEST_CANCELLED");
    }

    [Fact]
    public void MarkUnhandledException_PreservesPriorErrorCode()
    {
        var logger = new CapturingLogger();

        using (var scope = BuildScope(logger, AccessTelemetryLog.OperationIngest, 7502, 7512, activity: null))
        {
            scope.MarkValidationError("SOURCE_TYPE_INVALID");
            scope.MarkUnhandledException(new InvalidOperationException("boom"));
        }

        logger.Captures.Single().AuditEvent.ErrorCode.ShouldBe("SOURCE_TYPE_INVALID");
    }

    [Fact]
    public void Dispose_WithActivity_TagsOutcomeAndErrorCodeAndStatus()
    {
        var logger = new CapturingLogger();
        using Activity? activity = MemoriesActivitySource.Instance.StartActivity(MemoriesActivitySource.SearchRequest);
        activity.ShouldNotBeNull();

        using (var scope = BuildScope(logger, AccessTelemetryLog.OperationSearch, 7501, 7511, activity))
        {
            scope.MarkValidationError("INVALID_AXIS");
        }

        activity.GetTagItem(MemoriesActivitySource.TagOutcome).ShouldBe("error");
        activity.GetTagItem(MemoriesActivitySource.TagErrorCode).ShouldBe("INVALID_AXIS");
        activity.Status.ShouldBe(ActivityStatusCode.Error);
    }

    [Fact]
    public void Dispose_WithActivity_PopulatesTraceIdAndSpanIdOnAuditEvent()
    {
        var logger = new CapturingLogger();
        using Activity? activity = MemoriesActivitySource.Instance.StartActivity(MemoriesActivitySource.SearchRequest);
        activity.ShouldNotBeNull();

        using (var scope = BuildScope(logger, AccessTelemetryLog.OperationSearch, 7501, 7511, activity))
        {
        }

        Capture captured = logger.Captures.ShouldHaveSingleItem();
        captured.AuditEvent.TraceId.ShouldBe(activity.TraceId.ToString());
        captured.AuditEvent.SpanId.ShouldBe(activity.SpanId.ToString());
    }

    [Fact]
    public void Dispose_WithoutActivity_ProducesNullTraceAndSpanIds()
    {
        var logger = new CapturingLogger();

        using (var scope = BuildScope(logger, AccessTelemetryLog.OperationTraverse, 7503, 7513, activity: null))
        {
        }

        Capture captured = logger.Captures.ShouldHaveSingleItem();
        captured.AuditEvent.TraceId.ShouldBeNull();
        captured.AuditEvent.SpanId.ShouldBeNull();
    }

    [Fact]
    public void Dispose_CalledTwice_EmitsExactlyOnce()
    {
        var logger = new CapturingLogger();
        var scope = BuildScope(logger, AccessTelemetryLog.OperationSearch, 7501, 7511, activity: null);

        scope.Dispose();
        scope.Dispose();

        logger.Captures.Count.ShouldBe(1);
    }

    [Fact]
    public void Dispose_InvokesMetricCallbackExactlyOnce()
    {
        var logger = new CapturingLogger();
        int callbackCount = 0;

        var scope = new EndpointTelemetryScope(
            logger,
            activity: null,
            operationType: AccessTelemetryLog.OperationSearch,
            successEventId: 7501,
            errorEventId: 7511,
            tenantIdTag: "acme",
            recordMetricOnDispose: _ => callbackCount++);

        scope.Dispose();
        scope.Dispose();

        callbackCount.ShouldBe(1);
    }

    [Fact]
    public void Dispose_MetricCallbackThrows_DoesNotPropagate_AuditStillEmitted()
    {
        var logger = new CapturingLogger();

        var scope = new EndpointTelemetryScope(
            logger,
            activity: null,
            operationType: AccessTelemetryLog.OperationSearch,
            successEventId: 7501,
            errorEventId: 7511,
            tenantIdTag: "acme",
            recordMetricOnDispose: _ => throw new InvalidOperationException("metric backend went away"));

        Should.NotThrow(() => scope.Dispose());
        logger.Captures.Count.ShouldBe(1);
    }

    [Fact]
    public void Dispose_MeasuresElapsedTime()
    {
        var logger = new CapturingLogger();

        var scope = BuildScope(logger, AccessTelemetryLog.OperationSearch, 7501, 7511, activity: null);
        Thread.Sleep(10);
        scope.Dispose();

        scope.ElapsedMs.ShouldBeGreaterThanOrEqualTo(0);
        logger.Captures.Single().AuditEvent.DurationMs.ShouldBe(scope.ElapsedMs);
    }

    [Fact]
    public void Dispose_UnknownOperationType_FallsThroughToSearchChannel()
    {
        var logger = new CapturingLogger();

        using (var scope = BuildScope(logger, operationType: "phantom", successEventId: 7599, errorEventId: 7599, activity: null))
        {
        }

        Capture captured = logger.Captures.ShouldHaveSingleItem();
        // Safe-default: audits get emitted via LogSearchAccess (eventId 7501) even when the operation type
        // doesn't match a known emitter — the record itself still carries the caller-supplied operation.
        captured.EventId.Id.ShouldBe(7501);
        captured.AuditEvent.OperationType.ShouldBe("phantom");
    }

    private static EndpointTelemetryScope BuildScope(
        CapturingLogger logger,
        string operationType,
        int successEventId,
        int errorEventId,
        Activity? activity)
        => new(
            logger,
            activity,
            operationType,
            successEventId,
            errorEventId,
            tenantIdTag: "acme");

    private sealed record Capture(LogLevel Level, EventId EventId, AccessTelemetryEvent AuditEvent);

    private sealed class CapturingLogger : ILogger<AccessTelemetryCategory>
    {
        public List<Capture> Captures { get; } = [];

        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull => NullScope.Instance;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            AccessTelemetryEvent? captured = ExtractAuditEvent(state);
            if (captured is null)
            {
                return;
            }

            Captures.Add(new Capture(logLevel, eventId, captured));
        }

        private static AccessTelemetryEvent? ExtractAuditEvent<TState>(TState state)
        {
            if (state is IReadOnlyList<KeyValuePair<string, object?>> kvp)
            {
                foreach (KeyValuePair<string, object?> entry in kvp)
                {
                    if (entry.Value is AccessTelemetryEvent evt)
                    {
                        return evt;
                    }
                }
            }

            return null;
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
