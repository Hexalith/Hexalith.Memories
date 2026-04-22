// <copyright file="EventIngestionOutcomeTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Tests.EventStoreIntegration;

using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

using Hexalith.Memories.EventStore;

using Microsoft.Extensions.Logging;

using NSubstitute;

using Shouldly;

/// <summary>Story 9.1 Tier-2 controller HTTP tests. Each test drives one subscription request through the
/// Memories Server HTTP pipeline (CloudEvents middleware + controller + outcome mapping + response DTO)
/// while stubbing the router/scheduler/preflight adapters so the outcome under test is the sole branch
/// reached. Verifies the HTTP-status / DAPR-retry / response-body contract established by AC #3 #6 #8 #9
/// #12 #13 #14a #14b.</summary>
public sealed class EventIngestionOutcomeTests : System.IDisposable
{
    private readonly EventStoreWebAppFactory _factory = new();

    [Fact]
    public async Task Accepted_Returns200_WithInstanceId()
    {
        const string tenantId = "acme";
        const string caseId = "case-1";
        const string aggregateType = "Claims";
        TenantEventRoute route = new(tenantId, caseId, aggregateType);
        _factory.Router
            .ResolveAsync(Arg.Any<CloudEventEnvelope>(), Arg.Any<System.Threading.CancellationToken>())
            .Returns(TenantEventRouteResolution.Accepted(route));
        _factory.PreflightDedup
            .TryReserveAsync(Arg.Any<string>(), Arg.Any<System.TimeSpan>(), Arg.Any<System.Threading.CancellationToken>())
            .Returns(PreflightReservationResult.Reserved);
        _factory.Scheduler
            .ScheduleAsync(Arg.Any<string>(), Arg.Any<Hexalith.Memories.Contracts.V1.IngestionInput>(), Arg.Any<System.Threading.CancellationToken>())
            .Returns(ci => ci.ArgAt<string>(0));

        HttpResponseMessage response = await PostCloudEventAsync(BuildValidEnvelope());

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        EventIngestionResponse body = await ReadResponseAsync(response);
        body.Status.ShouldBe(EventIngestionResponse.StatusAccepted);
        body.InstanceId.ShouldNotBeNull();
        body.WasDuplicate.ShouldBeFalse();
    }

    [Fact]
    public async Task DeletingTenant_Returns200_LogsWarning()
    {
        const string tenantId = "acme-deleting";
        _factory.Router
            .ResolveAsync(Arg.Any<CloudEventEnvelope>(), Arg.Any<System.Threading.CancellationToken>())
            .Returns(TenantEventRouteResolution.TenantDeleting(tenantId));

        HttpResponseMessage response = await PostCloudEventAsync(BuildValidEnvelope());

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        EventIngestionResponse body = await ReadResponseAsync(response);
        body.Status.ShouldBe(EventIngestionResponse.StatusTenantDeleting);
        body.InstanceId.ShouldBeNull();
        _factory.EventStoreLogs.EventStoreCaptures
            .Any(c => c.EventId == 9111 && c.Level == LogLevel.Warning)
            .ShouldBeTrue();
    }

    [Fact]
    public async Task ProvisioningTenant_Returns500_ForDaprRetry()
    {
        const string tenantId = "acme-provisioning";
        _factory.Router
            .ResolveAsync(Arg.Any<CloudEventEnvelope>(), Arg.Any<System.Threading.CancellationToken>())
            .Returns(TenantEventRouteResolution.TenantProvisioning(tenantId));

        HttpResponseMessage response = await PostCloudEventAsync(BuildValidEnvelope());

        response.StatusCode.ShouldBe(HttpStatusCode.InternalServerError);
        _factory.EventStoreLogs.EventStoreCaptures
            .Any(c => c.EventId == 9102 && c.Level == LogLevel.Information)
            .ShouldBeTrue();
    }

    [Fact]
    public async Task UnknownSource_Returns200_LogsWarning()
    {
        _factory.Router
            .ResolveAsync(Arg.Any<CloudEventEnvelope>(), Arg.Any<System.Threading.CancellationToken>())
            .Returns(TenantEventRouteResolution.UnknownSource());

        HttpResponseMessage response = await PostCloudEventAsync(BuildValidEnvelope());

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        EventIngestionResponse body = await ReadResponseAsync(response);
        body.Status.ShouldBe(EventIngestionResponse.StatusUnknownSource);
        _factory.EventStoreLogs.EventStoreCaptures
            .Any(c => c.EventId == 9110 && c.Level == LogLevel.Warning)
            .ShouldBeTrue();
    }

    [Fact]
    public async Task InvalidEnvelope_Returns400()
    {
        string invalidEnvelope = /* missing required `id` + `source` + `type` */
            "{\"data\": {\"foo\": \"bar\"}}";

        HttpResponseMessage response = await PostRawAsync(invalidEnvelope);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Duplicate_Returns200_WithWasDuplicateTrue()
    {
        const string tenantId = "acme";
        const string caseId = "case-1";
        TenantEventRoute route = new(tenantId, caseId, "Claims");
        _factory.Router
            .ResolveAsync(Arg.Any<CloudEventEnvelope>(), Arg.Any<System.Threading.CancellationToken>())
            .Returns(TenantEventRouteResolution.Accepted(route));
        _factory.PreflightDedup
            .TryReserveAsync(Arg.Any<string>(), Arg.Any<System.TimeSpan>(), Arg.Any<System.Threading.CancellationToken>())
            .Returns(PreflightReservationResult.Duplicate);

        HttpResponseMessage response = await PostCloudEventAsync(BuildValidEnvelope());

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        EventIngestionResponse body = await ReadResponseAsync(response);
        body.Status.ShouldBe(EventIngestionResponse.StatusDuplicate);
        body.WasDuplicate.ShouldBeTrue();
        body.InstanceId.ShouldBeNull();
    }

    [Fact]
    public async Task ScheduleFailure_ReleasesReservation_AndReturns500()
    {
        const string tenantId = "acme";
        const string caseId = "case-1";
        TenantEventRoute route = new(tenantId, caseId, "Claims");
        _factory.Router
            .ResolveAsync(Arg.Any<CloudEventEnvelope>(), Arg.Any<System.Threading.CancellationToken>())
            .Returns(TenantEventRouteResolution.Accepted(route));
        _factory.PreflightDedup
            .TryReserveAsync(Arg.Any<string>(), Arg.Any<System.TimeSpan>(), Arg.Any<System.Threading.CancellationToken>())
            .Returns(PreflightReservationResult.Reserved);
        _factory.Scheduler
            .ScheduleAsync(Arg.Any<string>(), Arg.Any<Hexalith.Memories.Contracts.V1.IngestionInput>(), Arg.Any<System.Threading.CancellationToken>())
            .Returns<Task<string>>(_ => throw new System.InvalidOperationException("sidecar down"));

        HttpResponseMessage response = await PostCloudEventAsync(BuildValidEnvelope());

        response.StatusCode.ShouldBe(HttpStatusCode.InternalServerError);
        await _factory.PreflightDedup.Received(1)
            .ReleaseAsync(Arg.Any<string>(), Arg.Any<System.Threading.CancellationToken>());
    }

    public void Dispose() => _factory.Dispose();

    private static JsonElement BuildValidEnvelope()
    {
        string json = $$"""
            {
              "specversion": "1.0",
              "id": "evt-{{System.Guid.NewGuid():N}}",
              "source": "enterprise/claims",
              "type": "MyApp.Claims.ClaimSubmittedV2",
              "subject": "claim-42",
              "time": "2026-04-22T10:00:00Z",
              "datacontenttype": "application/json",
              "data": { "amount": 100 }
            }
            """;
        return JsonDocument.Parse(json).RootElement.Clone();
    }

    private async Task<HttpResponseMessage> PostCloudEventAsync(JsonElement envelope)
    {
        using HttpClient client = _factory.CreateClient();
        StringContent content = new(envelope.GetRawText(), Encoding.UTF8, "application/json");
        return await client.PostAsync("/events/ingest", content);
    }

    private async Task<HttpResponseMessage> PostRawAsync(string json)
    {
        using HttpClient client = _factory.CreateClient();
        StringContent content = new(json, Encoding.UTF8, "application/json");
        return await client.PostAsync("/events/ingest", content);
    }

    private static async Task<EventIngestionResponse> ReadResponseAsync(HttpResponseMessage response)
    {
        EventIngestionResponse? body = await response.Content.ReadFromJsonAsync<EventIngestionResponse>();
        body.ShouldNotBeNull();
        return body!;
    }
}
