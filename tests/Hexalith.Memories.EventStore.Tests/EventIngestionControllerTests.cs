// <copyright file="EventIngestionControllerTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.EventStore.Tests;

using System.Text;
using System.Text.Json;

using Hexalith.Memories.Contracts.V1;
using Hexalith.Memories.EventStore;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

using NSubstitute;

using Shouldly;

public sealed class EventIngestionControllerTests
{
    private const string AnyEnvelopeJson = """
        { "id": "evt-1", "source": "/x", "type": "a.b.c", "data": { } }
        """;

    private static EventIngestionController BuildController(EventIngestionProcessResult processResult, string requestBody = AnyEnvelopeJson)
    {
        IEventIngestionService service = Substitute.For<IEventIngestionService>();
        service.ProcessAsync(Arg.Any<JsonElement>(), Arg.Any<CancellationToken>())
            .Returns(processResult);

        EventIngestionController controller = new(service)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext(),
            },
        };

        controller.Request.ContentType = "application/json";
        controller.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes(requestBody));
        return controller;
    }

    [Fact]
    public async Task OnEvent_Accepted_Returns200WithResponse()
    {
        EventIngestionController controller = BuildController(
            new EventIngestionProcessResult(EventIngestionOutcome.Accepted, EventIngestionResponse.Accepted("wf-1")));

        IActionResult actionResult = await controller.OnEvent(CancellationToken.None);

        OkObjectResult ok = actionResult.ShouldBeOfType<OkObjectResult>();
        EventIngestionResponse response = ok.Value.ShouldBeOfType<EventIngestionResponse>();
        response.Status.ShouldBe(EventIngestionResponse.StatusAccepted);
        response.InstanceId.ShouldBe("wf-1");
    }

    [Fact]
    public async Task OnEvent_Duplicate_Returns200WithWasDuplicateTrue()
    {
        EventIngestionController controller = BuildController(
            new EventIngestionProcessResult(EventIngestionOutcome.Duplicate, EventIngestionResponse.Duplicate()));

        IActionResult actionResult = await controller.OnEvent(CancellationToken.None);

        OkObjectResult ok = actionResult.ShouldBeOfType<OkObjectResult>();
        EventIngestionResponse response = ok.Value.ShouldBeOfType<EventIngestionResponse>();
        response.WasDuplicate.ShouldBeTrue();
        response.InstanceId.ShouldBeNull();
    }

    [Fact]
    public async Task OnEvent_InvalidCloudEvent_Returns400WithErrorResponse()
    {
        EventIngestionController controller = BuildController(
            new EventIngestionProcessResult(
                EventIngestionOutcome.InvalidCloudEvent,
                EventIngestionResponse.Invalid("cloudevent.data missing")));

        IActionResult actionResult = await controller.OnEvent(CancellationToken.None);

        BadRequestObjectResult bad = actionResult.ShouldBeOfType<BadRequestObjectResult>();
        ErrorResponse error = bad.Value.ShouldBeOfType<ErrorResponse>();
        error.Code.ShouldBe("INVALID_CLOUDEVENT");
        error.Message.ShouldBe("cloudevent.data missing");
    }

    [Fact]
    public async Task OnEvent_TenantProvisioning_Returns500ForRetry()
    {
        EventIngestionController controller = BuildController(
            new EventIngestionProcessResult(
                EventIngestionOutcome.TenantProvisioning,
                EventIngestionResponse.Drop("tenant-provisioning", "Tenant is provisioning")));

        IActionResult actionResult = await controller.OnEvent(CancellationToken.None);

        ObjectResult status = actionResult.ShouldBeOfType<ObjectResult>();
        status.StatusCode.ShouldBe(StatusCodes.Status500InternalServerError);
    }

    [Fact]
    public async Task OnEvent_ScheduleFailed_Returns500ForRetry()
    {
        EventIngestionController controller = BuildController(
            new EventIngestionProcessResult(
                EventIngestionOutcome.ScheduleFailed,
                EventIngestionResponse.Drop("schedule-failed", "dapr sidecar down")));

        IActionResult actionResult = await controller.OnEvent(CancellationToken.None);

        ObjectResult status = actionResult.ShouldBeOfType<ObjectResult>();
        status.StatusCode.ShouldBe(StatusCodes.Status500InternalServerError);
    }

    [Theory]
    [InlineData(EventIngestionOutcome.UnknownSource)]
    [InlineData(EventIngestionOutcome.AutoCreateDisabled)]
    [InlineData(EventIngestionOutcome.CaseCapExceeded)]
    public async Task OnEvent_PermanentDropOutcomes_Return200(EventIngestionOutcome outcome)
    {
        EventIngestionController controller = BuildController(
            new EventIngestionProcessResult(outcome, EventIngestionResponse.Drop("drop", "reason")));

        IActionResult actionResult = await controller.OnEvent(CancellationToken.None);

        actionResult.ShouldBeOfType<OkObjectResult>();
    }

    [Theory]
    [InlineData(EventIngestionOutcome.TenantNotFound)]
    [InlineData(EventIngestionOutcome.TenantDeleting)]
    public async Task OnEvent_TenantLifecycleRouteFailures_Return500ForRetry(EventIngestionOutcome outcome)
    {
        EventIngestionController controller = BuildController(
            new EventIngestionProcessResult(outcome, EventIngestionResponse.Drop("drop", "reason")));

        IActionResult actionResult = await controller.OnEvent(CancellationToken.None);

        ObjectResult status = actionResult.ShouldBeOfType<ObjectResult>();
        status.StatusCode.ShouldBe(StatusCodes.Status500InternalServerError);
    }

    [Fact]
    public async Task OnEvent_MalformedJson_Returns400WithoutInvokingService()
    {
        IEventIngestionService service = Substitute.For<IEventIngestionService>();
        EventIngestionController controller = new(service)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext(),
            },
        };
        controller.Request.ContentType = "application/json";
        controller.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes("{"));

        IActionResult actionResult = await controller.OnEvent(CancellationToken.None);

        actionResult.ShouldBeOfType<BadRequestObjectResult>();
        await service.DidNotReceive().ProcessAsync(Arg.Any<JsonElement>(), Arg.Any<CancellationToken>());
    }
}
