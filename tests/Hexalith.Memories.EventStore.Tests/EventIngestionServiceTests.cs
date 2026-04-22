// <copyright file="EventIngestionServiceTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.EventStore.Tests;

using System.Text.Json;

using Hexalith.Memories.Contracts.V1;
using Hexalith.Memories.EventStore;

using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

using NSubstitute;

using Shouldly;

public sealed class EventIngestionServiceTests
{
    private static readonly TenantEventRoute Route = new("tenant-1", "case-1", "Claims");

    private static readonly string ValidEnvelopeJson = """
        {
          "id": "evt-1",
          "source": "/enterprise/hr",
          "type": "MyApp.Claims.ClaimSubmittedV2",
          "subject": "agg-1",
          "time": "2026-04-22T10:00:00Z",
          "datacontenttype": "application/json",
          "data": { "claimId": "abc" }
        }
        """;

    private static JsonElement Envelope(string json = null!)
        => JsonDocument.Parse(json ?? ValidEnvelopeJson).RootElement;

    private static (
        EventIngestionService Service,
        ITenantEventRouter Router,
        IEventIngestionWorkflowScheduler Scheduler,
        IPreflightDedupStore Dedup,
        IEventIngestionTelemetry Telemetry,
        TenantEventRoutingOptions Options) Build(
            TenantEventRouteResolution? resolution = null,
            bool preflightEnabled = true)
    {
        TenantEventRoutingOptions opts = new()
        {
            Topic = "t",
            PreflightDedupEnabled = preflightEnabled,
        };

        IOptionsMonitor<TenantEventRoutingOptions> optionsMonitor = Substitute.For<IOptionsMonitor<TenantEventRoutingOptions>>();
        optionsMonitor.CurrentValue.Returns(opts);

        ITenantEventRouter router = Substitute.For<ITenantEventRouter>();
        router.ResolveAsync(Arg.Any<CloudEventEnvelope>(), Arg.Any<CancellationToken>())
            .Returns(resolution ?? TenantEventRouteResolution.Accepted(Route));

        IEventIngestionWorkflowScheduler scheduler = Substitute.For<IEventIngestionWorkflowScheduler>();
        scheduler.ScheduleAsync(Arg.Any<string>(), Arg.Any<IngestionInput>(), Arg.Any<CancellationToken>())
            .Returns(callInfo => callInfo.ArgAt<string>(0));

        IPreflightDedupStore dedup = Substitute.For<IPreflightDedupStore>();
        dedup.TryReserveAsync(Arg.Any<string>(), Arg.Any<TimeSpan>(), Arg.Any<CancellationToken>())
            .Returns(PreflightReservationResult.Reserved);

        IEventIngestionTelemetry telemetry = Substitute.For<IEventIngestionTelemetry>();

        EventIngestionService service = new(
            router, scheduler, dedup, telemetry, optionsMonitor, NullLogger<EventIngestionService>.Instance);

        return (service, router, scheduler, dedup, telemetry, opts);
    }

    [Fact]
    public async Task ProcessAsync_HappyPath_ReturnsAcceptedWithInstanceId()
    {
        (EventIngestionService service, _, _, _, IEventIngestionTelemetry telemetry, _) = Build();

        EventIngestionProcessResult result = await service.ProcessAsync(Envelope(), CancellationToken.None);

        result.Outcome.ShouldBe(EventIngestionOutcome.Accepted);
        result.Response.Status.ShouldBe(EventIngestionResponse.StatusAccepted);
        result.Response.InstanceId.ShouldNotBeNullOrEmpty();
        telemetry.Received(1).RecordIngestion(
            "tenant-1", "case-1", "evt-1", "Claims",
            EventIngestionOutcome.Accepted, Arg.Any<long>());
    }

    [Fact]
    public async Task ProcessAsync_MalformedEnvelope_ReturnsInvalidCloudEvent()
    {
        (EventIngestionService service, _, _, _, _, _) = Build();

        JsonElement missingId = JsonDocument.Parse("""
            { "source": "/x", "type": "a.b.c", "data": {} }
            """).RootElement;

        EventIngestionProcessResult result = await service.ProcessAsync(missingId, CancellationToken.None);

        result.Outcome.ShouldBe(EventIngestionOutcome.InvalidCloudEvent);
        result.Response.Status.ShouldBe(EventIngestionResponse.StatusInvalidCloudEvent);
        result.Response.InstanceId.ShouldBeNull();
    }

    [Fact]
    public async Task ProcessAsync_MissingData_ReturnsInvalidCloudEvent()
    {
        (EventIngestionService service, _, _, _, _, _) = Build();

        JsonElement missingData = JsonDocument.Parse("""
            { "id": "evt-1", "source": "/x", "type": "a.b.c" }
            """).RootElement;

        EventIngestionProcessResult result = await service.ProcessAsync(missingData, CancellationToken.None);

        result.Outcome.ShouldBe(EventIngestionOutcome.InvalidCloudEvent);
        result.Response.Reason.ShouldBe("cloudevent.data missing");
    }

    [Fact]
    public async Task ProcessAsync_PreflightReservation_ReturnsDuplicate_WhenKeyAlreadyExists()
    {
        (EventIngestionService service, _, IEventIngestionWorkflowScheduler scheduler, IPreflightDedupStore dedup, _, _) = Build();
        dedup.TryReserveAsync(Arg.Any<string>(), Arg.Any<TimeSpan>(), Arg.Any<CancellationToken>())
            .Returns(PreflightReservationResult.Duplicate);

        EventIngestionProcessResult result = await service.ProcessAsync(Envelope(), CancellationToken.None);

        result.Outcome.ShouldBe(EventIngestionOutcome.Duplicate);
        result.Response.Status.ShouldBe(EventIngestionResponse.StatusDuplicate);
        result.Response.WasDuplicate.ShouldBeTrue();
        result.Response.InstanceId.ShouldBeNull();
        await scheduler.DidNotReceive().ScheduleAsync(Arg.Any<string>(), Arg.Any<IngestionInput>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ProcessAsync_PreflightReservation_ReleasesKey_WhenSchedulingFails()
    {
        (EventIngestionService service, _, IEventIngestionWorkflowScheduler scheduler, IPreflightDedupStore dedup, _, _) = Build();
        scheduler.ScheduleAsync(Arg.Any<string>(), Arg.Any<IngestionInput>(), Arg.Any<CancellationToken>())
            .Returns<string>(_ => throw new InvalidOperationException("dapr sidecar down"));

        EventIngestionProcessResult result = await service.ProcessAsync(Envelope(), CancellationToken.None);

        result.Outcome.ShouldBe(EventIngestionOutcome.ScheduleFailed);
        await dedup.Received(1).ReleaseAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ProcessAsync_PreflightFailOpen_StillSchedules()
    {
        (EventIngestionService service, _, IEventIngestionWorkflowScheduler scheduler, IPreflightDedupStore dedup, _, _) = Build();
        dedup.TryReserveAsync(Arg.Any<string>(), Arg.Any<TimeSpan>(), Arg.Any<CancellationToken>())
            .Returns(PreflightReservationResult.FailOpen);

        EventIngestionProcessResult result = await service.ProcessAsync(Envelope(), CancellationToken.None);

        result.Outcome.ShouldBe(EventIngestionOutcome.Accepted);
        await scheduler.Received(1).ScheduleAsync(Arg.Any<string>(), Arg.Any<IngestionInput>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ProcessAsync_PreflightFailOpen_DoesNotReleaseOnScheduleFailure()
    {
        (EventIngestionService service, _, IEventIngestionWorkflowScheduler scheduler, IPreflightDedupStore dedup, _, _) = Build();
        dedup.TryReserveAsync(Arg.Any<string>(), Arg.Any<TimeSpan>(), Arg.Any<CancellationToken>())
            .Returns(PreflightReservationResult.FailOpen);
        scheduler.ScheduleAsync(Arg.Any<string>(), Arg.Any<IngestionInput>(), Arg.Any<CancellationToken>())
            .Returns<string>(_ => throw new InvalidOperationException("boom"));

        _ = await service.ProcessAsync(Envelope(), CancellationToken.None);

        await dedup.DidNotReceive().ReleaseAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ProcessAsync_UnknownSource_ReturnsDropNoInstanceId()
    {
        (EventIngestionService service, _, IEventIngestionWorkflowScheduler scheduler, _, _, _) =
            Build(TenantEventRouteResolution.UnknownSource());

        EventIngestionProcessResult result = await service.ProcessAsync(Envelope(), CancellationToken.None);

        result.Outcome.ShouldBe(EventIngestionOutcome.UnknownSource);
        result.Response.Status.ShouldBe(EventIngestionResponse.StatusUnknownSource);
        result.Response.InstanceId.ShouldBeNull();
        await scheduler.DidNotReceive().ScheduleAsync(Arg.Any<string>(), Arg.Any<IngestionInput>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ProcessAsync_TenantProvisioning_ReturnsRetryableOutcome()
    {
        (EventIngestionService service, _, _, _, _, _) =
            Build(TenantEventRouteResolution.TenantProvisioning("hr-tenant"));

        EventIngestionProcessResult result = await service.ProcessAsync(Envelope(), CancellationToken.None);

        result.Outcome.ShouldBe(EventIngestionOutcome.TenantProvisioning);
        result.Response.InstanceId.ShouldBeNull();
    }

    [Fact]
    public async Task ProcessAsync_TenantDeleting_ReturnsDropOutcome()
    {
        (EventIngestionService service, _, _, _, _, _) =
            Build(TenantEventRouteResolution.TenantDeleting("hr-tenant"));

        EventIngestionProcessResult result = await service.ProcessAsync(Envelope(), CancellationToken.None);

        result.Outcome.ShouldBe(EventIngestionOutcome.TenantDeleting);
    }

    [Fact]
    public async Task ProcessAsync_AutoCreateDisabled_ReturnsDropOutcome()
    {
        (EventIngestionService service, _, _, _, _, _) =
            Build(TenantEventRouteResolution.AutoCreateDisabled("hr-tenant"));

        EventIngestionProcessResult result = await service.ProcessAsync(Envelope(), CancellationToken.None);

        result.Outcome.ShouldBe(EventIngestionOutcome.AutoCreateDisabled);
    }

    [Fact]
    public async Task ProcessAsync_CaseCapExceeded_ReturnsDropOutcome()
    {
        (EventIngestionService service, _, _, _, _, _) =
            Build(TenantEventRouteResolution.CaseCapExceeded("hr-tenant"));

        EventIngestionProcessResult result = await service.ProcessAsync(Envelope(), CancellationToken.None);

        result.Outcome.ShouldBe(EventIngestionOutcome.CaseCapExceeded);
    }

    [Fact]
    public async Task ProcessAsync_PreflightDisabled_NeverCallsReserveOrRelease()
    {
        (EventIngestionService service, _, _, IPreflightDedupStore dedup, _, _) = Build(preflightEnabled: false);

        _ = await service.ProcessAsync(Envelope(), CancellationToken.None);

        await dedup.DidNotReceive().TryReserveAsync(Arg.Any<string>(), Arg.Any<TimeSpan>(), Arg.Any<CancellationToken>());
        await dedup.DidNotReceive().ReleaseAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ProcessAsync_InstanceIdMatchesDedupKeyFormat_ForIdempotentScheduling()
    {
        (EventIngestionService service, _, _, _, _, _) = Build();

        EventIngestionProcessResult result = await service.ProcessAsync(Envelope(), CancellationToken.None);

        string expectedKey = EventStoreDedupKey.Build("tenant-1", "case-1", "evt-1");
        result.Response.InstanceId.ShouldBe(expectedKey);
    }
}
