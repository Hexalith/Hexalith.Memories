// <copyright file="MemoriesServerExceptionHandlerTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Tests.Diagnostics;

using System.Diagnostics;
using System.IO;
using System.Text.Json;

using Hexalith.Memories.Contracts.V1;
using Hexalith.Memories.Server.Diagnostics;
using Hexalith.Memories.Server.Endpoints;
using Hexalith.Memories.Server.Telemetry;
using Hexalith.Memories.Server.Tests.Telemetry.Infrastructure;
using Hexalith.Memories.Telemetry;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Logging;

using Shouldly;

/// <summary>Story 25.2 coverage for sanitized server exception handling.</summary>
public sealed class MemoriesServerExceptionHandlerTests
{
    [Fact]
    public async Task TryHandleAsync_ResponseNotStarted_WritesSanitizedUnhandledExceptionEnvelope()
    {
        using CapturingAuditLoggerProvider provider = new();
        using ILoggerFactory loggerFactory = LoggerFactory.Create(builder => builder.AddProvider(provider));
        var handler = CreateHandler(loggerFactory);
        DefaultHttpContext context = new();
        context.Response.Body = new MemoryStream();
        context.Request.RouteValues["tenantId"] = "tenant-a";

        bool handled = await handler.TryHandleAsync(
            context,
            new InvalidOperationException("secret backend details"),
            CancellationToken.None);

        handled.ShouldBeTrue();
        context.Response.StatusCode.ShouldBe(StatusCodes.Status500InternalServerError);
        context.Response.Body.Position = 0;
        ErrorResponse? error = await JsonSerializer.DeserializeAsync<ErrorResponse>(
            context.Response.Body,
            MemoriesJsonContext.Options);
        error.ShouldNotBeNull();
        error.Code.ShouldBe("UNHANDLED_EXCEPTION");
        error.Message.ShouldNotContain("secret backend details");
        error.Suggestion.ShouldContain("trace identifier");
        AuditLogCapture capture = provider.AccessTelemetryCaptures.ShouldHaveSingleItem();
        capture.EventId.ShouldBe(7511);
        capture.AuditEvent.ShouldNotBeNull();
        capture.AuditEvent.TenantId.ShouldBe("tenant-a");
        capture.AuditEvent.ErrorCode.ShouldBe("UNHANDLED_EXCEPTION");
    }

    [Fact]
    public async Task TryHandleAsync_ResponseAlreadyStarted_DoesNotWriteEnvelope()
    {
        using ILoggerFactory loggerFactory = LoggerFactory.Create(_ => { });
        var handler = CreateHandler(loggerFactory);
        DefaultHttpContext context = new();
        context.Features.Set<IHttpResponseFeature>(new StartedResponseFeature());
        context.Response.Body = new MemoryStream();

        bool handled = await handler.TryHandleAsync(
            context,
            new InvalidOperationException("boom"),
            CancellationToken.None);

        handled.ShouldBeFalse();
        context.Response.Body.Length.ShouldBe(0);
    }

    [Fact]
    public async Task TryHandleAsync_TenantConfigActivity_EmitsOperationSpecificFallbackAudit()
    {
        using CapturingAuditLoggerProvider provider = new();
        using ILoggerFactory loggerFactory = LoggerFactory.Create(builder => builder.AddProvider(provider));
        var handler = CreateHandler(loggerFactory);
        DefaultHttpContext context = new();
        context.Response.Body = new MemoryStream();
        context.Request.RouteValues["tenantId"] = "bad tenant!";

        using Activity activity = new("memories.test");
        activity.Start();
        activity.SetTag(MemoriesActivitySource.TagOperation, AccessTelemetryLog.OperationTenantConfig);

        bool handled = await handler.TryHandleAsync(
            context,
            new InvalidOperationException("secret backend details"),
            CancellationToken.None);

        handled.ShouldBeTrue();
        AuditLogCapture capture = provider.AccessTelemetryCaptures.ShouldHaveSingleItem();
        capture.EventId.ShouldBe(7517);
        capture.AuditEvent.ShouldNotBeNull();
        capture.AuditEvent.OperationType.ShouldBe(AccessTelemetryLog.OperationTenantConfig);
        capture.AuditEvent.TenantId.ShouldBe(MemoriesMeter.RejectedTenantTag);
        capture.AuditEvent.DurationMs.ShouldBeGreaterThanOrEqualTo(0);
    }

    [Fact]
    public async Task TryHandleAsync_EndpointAuditAlreadyEmitted_SkipsFallbackAudit()
    {
        using CapturingAuditLoggerProvider provider = new();
        using ILoggerFactory loggerFactory = LoggerFactory.Create(builder => builder.AddProvider(provider));
        var handler = CreateHandler(loggerFactory);
        DefaultHttpContext context = new();
        context.Response.Body = new MemoryStream();
        EndpointTelemetryHelpers.MarkEndpointAuditEmitted(context);

        bool handled = await handler.TryHandleAsync(
            context,
            new InvalidOperationException("secret backend details"),
            CancellationToken.None);

        handled.ShouldBeTrue();
        provider.AccessTelemetryCaptures.ShouldBeEmpty();
    }

    private static MemoriesServerExceptionHandler CreateHandler(ILoggerFactory loggerFactory)
        => new(
            loggerFactory.CreateLogger<MemoriesServerExceptionHandler>(),
            loggerFactory.CreateLogger<AccessTelemetryCategory>());

    private sealed class StartedResponseFeature : IHttpResponseFeature
    {
        public int StatusCode { get; set; } = StatusCodes.Status200OK;

        public string? ReasonPhrase { get; set; }

        public IHeaderDictionary Headers { get; set; } = new HeaderDictionary();

        public Stream Body { get; set; } = new MemoryStream();

        public bool HasStarted => true;

        public void OnCompleted(Func<object, Task> callback, object state)
        {
        }

        public void OnStarting(Func<object, Task> callback, object state)
        {
        }
    }
}
