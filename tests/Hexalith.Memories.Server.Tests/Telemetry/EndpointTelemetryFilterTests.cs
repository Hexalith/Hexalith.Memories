// <copyright file="EndpointTelemetryFilterTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Tests.Telemetry;

using System.Diagnostics;

using Hexalith.Memories.Contracts.V1;
using Hexalith.Memories.Server.Telemetry;
using Hexalith.Memories.Server.Tests.Telemetry.Infrastructure;
using Hexalith.Memories.Telemetry;

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

using Shouldly;

/// <summary>Story 25.2 coverage for the centralized endpoint telemetry filter.</summary>
[Collection(Infrastructure.TelemetryTestCollection.Name)]
public sealed class EndpointTelemetryFilterTests : IDisposable
{
    private readonly ActivityListener _listener;

    public EndpointTelemetryFilterTests()
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

    [Fact]
    public async Task InvokeAsync_SuccessfulResult_EmitsOneSuccessAuditAndMetricCallback()
    {
        using CapturingAuditLoggerProvider provider = new();
        using ILoggerFactory loggerFactory = LoggerFactory.Create(builder => builder.AddProvider(provider));
        int metricCallbacks = 0;
        EndpointTelemetryFilter filter = CreateSearchFilter(
            loggerFactory,
            descriptor => descriptor with
            {
                RecordMetricOnDispose = _ => metricCallbacks++,
            });

        object? result = await filter.InvokeAsync(
            CreateInvocation(),
            _ => ValueTask.FromResult<object?>(Results.Ok(new { count = 1 })));

        result.ShouldBeAssignableTo<IResult>();
        metricCallbacks.ShouldBe(1);
        AuditLogCapture capture = provider.AccessTelemetryCaptures.ShouldHaveSingleItem();
        capture.EventId.ShouldBe(7501);
        capture.AuditEvent.ShouldNotBeNull();
        capture.AuditEvent.Outcome.ShouldBe(AccessTelemetryLog.OutcomeOk);
        capture.AuditEvent.TenantId.ShouldBe("acme");
        capture.AuditEvent.CaseId.ShouldBe("case-1");
    }

    [Fact]
    public async Task InvokeAsync_ErrorResponseResult_EmitsOneErrorAuditWithReturnedCode()
    {
        using CapturingAuditLoggerProvider provider = new();
        using ILoggerFactory loggerFactory = LoggerFactory.Create(builder => builder.AddProvider(provider));
        EndpointTelemetryFilter filter = CreateSearchFilter(loggerFactory);

        object? result = await filter.InvokeAsync(
            CreateInvocation(),
            _ => ValueTask.FromResult<object?>(Results.BadRequest(
                new ErrorResponse("INVALID_INPUT", "Bad request.", "Fix it."))));

        result.ShouldBeAssignableTo<IResult>();
        AuditLogCapture capture = provider.AccessTelemetryCaptures.ShouldHaveSingleItem();
        capture.EventId.ShouldBe(7511);
        capture.Level.ShouldBe(LogLevel.Warning);
        capture.AuditEvent.ShouldNotBeNull();
        capture.AuditEvent.Outcome.ShouldBe(AccessTelemetryLog.OutcomeError);
        capture.AuditEvent.ErrorCode.ShouldBe("INVALID_INPUT");
    }

    [Fact]
    public async Task InvokeAsync_ExceptionPath_MarksUnhandledExceptionAndEmitsExactlyOneAudit()
    {
        using CapturingAuditLoggerProvider provider = new();
        using ILoggerFactory loggerFactory = LoggerFactory.Create(builder => builder.AddProvider(provider));
        EndpointTelemetryFilter filter = CreateSearchFilter(loggerFactory);

        await Should.ThrowAsync<InvalidOperationException>(async () =>
            await filter.InvokeAsync(
                CreateInvocation(),
                _ => throw new InvalidOperationException("boom")));

        AuditLogCapture capture = provider.AccessTelemetryCaptures.ShouldHaveSingleItem();
        capture.EventId.ShouldBe(7511);
        capture.AuditEvent.ShouldNotBeNull();
        capture.AuditEvent.Outcome.ShouldBe(AccessTelemetryLog.OutcomeError);
        capture.AuditEvent.ErrorCode.ShouldBe("UNHANDLED_EXCEPTION");
    }

    [Fact]
    public async Task InvokeAsync_ConfigureActivityThrows_MarksUnhandledExceptionAndEmitsExactlyOneAudit()
    {
        using CapturingAuditLoggerProvider provider = new();
        using ILoggerFactory loggerFactory = LoggerFactory.Create(builder => builder.AddProvider(provider));
        EndpointTelemetryFilter filter = CreateSearchFilter(
            loggerFactory,
            descriptor => descriptor with
            {
                ConfigureActivity = (_, _, _) => throw new InvalidOperationException("tag failure"),
            });
        bool nextCalled = false;

        await Should.ThrowAsync<InvalidOperationException>(async () =>
            await filter.InvokeAsync(
                CreateInvocation(),
                _ =>
                {
                    nextCalled = true;
                    return ValueTask.FromResult<object?>(Results.Ok());
                }));

        nextCalled.ShouldBeFalse();
        AuditLogCapture capture = provider.AccessTelemetryCaptures.ShouldHaveSingleItem();
        capture.EventId.ShouldBe(7511);
        capture.AuditEvent.ShouldNotBeNull();
        capture.AuditEvent.Outcome.ShouldBe(AccessTelemetryLog.OutcomeError);
        capture.AuditEvent.ErrorCode.ShouldBe("UNHANDLED_EXCEPTION");
    }


    [Fact]
    public async Task InvokeAsync_ConfiguresActivityTags()
    {
        using CapturingAuditLoggerProvider provider = new();
        using ILoggerFactory loggerFactory = LoggerFactory.Create(builder => builder.AddProvider(provider));
        Activity? capturedActivity = null;
        EndpointTelemetryFilter filter = CreateSearchFilter(
            loggerFactory,
            descriptor => descriptor with
            {
                ConfigureActivity = (activity, _, _) =>
                {
                    capturedActivity = activity;
                    activity?.SetTag(MemoriesActivitySource.TagAxis, "syntactic");
                },
            });

        _ = await filter.InvokeAsync(
            CreateInvocation(),
            _ => ValueTask.FromResult<object?>(Results.Ok()));

        capturedActivity.ShouldNotBeNull();
        capturedActivity.DisplayName.ShouldBe(MemoriesActivitySource.SearchRequest);
        capturedActivity.GetTagItem(MemoriesActivitySource.TagOperation).ShouldBe(AccessTelemetryLog.OperationSearch);
        capturedActivity.GetTagItem(MemoriesActivitySource.TagTenantId).ShouldBe("acme");
        capturedActivity.GetTagItem(MemoriesActivitySource.TagCaseId).ShouldBe("case-1");
        capturedActivity.GetTagItem(MemoriesActivitySource.TagAxis).ShouldBe("syntactic");
        capturedActivity.GetTagItem(MemoriesActivitySource.TagOutcome).ShouldBe(AccessTelemetryLog.OutcomeOk);
    }

    private static EndpointTelemetryFilter CreateSearchFilter(
        ILoggerFactory loggerFactory,
        Func<EndpointTelemetryDescriptor, EndpointTelemetryDescriptor>? configure = null)
    {
        EndpointTelemetryDescriptor descriptor = new(
            AccessTelemetryLog.OperationSearch,
            MemoriesActivitySource.SearchRequest,
            7501,
            7511)
        {
            TenantIdResolver = _ => "acme",
            CaseIdResolver = _ => "case-1",
            QueryParamsFactory = _ => new Dictionary<string, object?>
            {
                ["axis"] = "syntactic",
            },
        };

        descriptor = configure?.Invoke(descriptor) ?? descriptor;
        return new EndpointTelemetryFilter(
            descriptor,
            loggerFactory.CreateLogger<AccessTelemetryCategory>());
    }

    private static EndpointFilterInvocationContext CreateInvocation()
        => new DefaultEndpointFilterInvocationContext(new DefaultHttpContext());
}
