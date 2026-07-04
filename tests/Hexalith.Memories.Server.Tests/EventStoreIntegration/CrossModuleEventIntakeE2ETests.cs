// <copyright file="CrossModuleEventIntakeE2ETests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Tests.EventStoreIntegration;

using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Hexalith.Memories.EventStore;

using NSubstitute;

using Shouldly;

/// <summary>Story 18.8 AC2/AC4/AC5/AC7 — end-to-end (Tier-2 in-process HTTP) proof that downstream Hexalith
/// modules can publish CloudEvents to the shared topic and have the Memories Server sidecar route, idempotency,
/// and drop semantics behave as the cross-module event-intake contract promises. The existing
/// <see cref="MiddlewareOrderTests"/> already cover sidecar subscription discovery, plain-JSON/CloudEvents
/// unwrapping, and the <c>/process</c> absence; these tests close the QA gap where source-prefix routing for
/// two synthetic modules (AC2), duplicate-safe delivery (AC4), and the unknown-source non-retry drop (AC5)
/// were proven only at the unit level and never through the real HTTP pipeline (middleware order + CloudEvents
/// normalization + controller outcome→HTTP mapping). AC7 asks for exactly this evidence.</summary>
public sealed class CrossModuleEventIntakeE2ETests : System.IDisposable
{
    private readonly EventStoreWebAppFactory _factory = new();

    private static string ModuleEnvelope(string id, string source, string type) => $$"""
        {
          "specversion": "1.0",
          "id": "{{id}}",
          "source": "{{source}}",
          "type": "{{type}}",
          "subject": "agg-1",
          "time": "2026-06-25T10:00:00Z",
          "data": { "value": 1 }
        }
        """;

    [Fact]
    public async Task TwoHexalithModulePrefixes_PublishedToSharedTopic_AreAcceptedAndRoutedDistinctly()
    {
        // AC2 + AC7: two synthetic Hexalith modules (Tenants, Parties) publish to the SAME topic/endpoint and
        // the shared sidecar routes each source prefix to its own configured tenant. Driven through the real
        // /events/ingest HTTP surface — the router unit tests prove prefix matching, this proves the endpoint
        // accepts and differentiates both module streams end-to-end.
        _factory.Router
            .ResolveAsync(
                Arg.Is<CloudEventEnvelope>(e => e.Source.StartsWith("hexalith/tenants", StringComparison.OrdinalIgnoreCase)),
                Arg.Any<CancellationToken>())
            .Returns(TenantEventRouteResolution.Accepted(new TenantEventRoute("tenant-events", "tenant-events:case", "Tenants")));
        _factory.Router
            .ResolveAsync(
                Arg.Is<CloudEventEnvelope>(e => e.Source.StartsWith("hexalith/parties", StringComparison.OrdinalIgnoreCase)),
                Arg.Any<CancellationToken>())
            .Returns(TenantEventRouteResolution.Accepted(new TenantEventRoute("party-events", "party-events:case", "Parties")));
        _factory.PreflightDedup
            .TryReserveAsync(Arg.Any<string>(), Arg.Any<TimeSpan>(), Arg.Any<CancellationToken>())
            .Returns(PreflightReservationResult.Reserved);
        _factory.Scheduler
            .ScheduleAsync(Arg.Any<string>(), Arg.Any<Hexalith.Memories.Contracts.V1.IngestionInput>(), Arg.Any<CancellationToken>())
            .Returns(ci => ci.ArgAt<string>(0));

        using HttpClient client = _factory.CreateClient();

        EventIngestionResponse tenants = await PublishAsync(
            client, ModuleEnvelope("evt-tenant-1", "hexalith/tenants/events", "Hexalith.Tenants.TenantCreatedV1"));
        EventIngestionResponse parties = await PublishAsync(
            client, ModuleEnvelope("evt-party-1", "hexalith/parties/events", "Hexalith.Parties.PartyRegisteredV1"));

        tenants.Status.ShouldBe(EventIngestionResponse.StatusAccepted);
        tenants.InstanceId.ShouldNotBeNullOrEmpty();
        parties.Status.ShouldBe(EventIngestionResponse.StatusAccepted);
        parties.InstanceId.ShouldNotBeNullOrEmpty();

        // Distinct routes (different tenant + case) must produce distinct workflow instance ids — proof the
        // shared topic did not collapse the two module streams into one.
        parties.InstanceId.ShouldNotBe(tenants.InstanceId);

        await _factory.Scheduler.Received(2).ScheduleAsync(
            Arg.Any<string>(), Arg.Any<Hexalith.Memories.Contracts.V1.IngestionInput>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UnknownSourcePrefix_DropsWithoutRetry_AndReturnsDiagnosableStatus()
    {
        // AC5: an unknown source prefix must DROP (HTTP 200, so DAPR does NOT redeliver) rather than fault
        // (HTTP 500 would trigger an at-least-once retry storm), and the response status string must name the
        // diagnosable outcome so an operator can identify the missing route. No workflow may be scheduled.
        _factory.Router
            .ResolveAsync(Arg.Any<CloudEventEnvelope>(), Arg.Any<CancellationToken>())
            .Returns(TenantEventRouteResolution.UnknownSource());

        using HttpClient client = _factory.CreateClient();
        using HttpResponseMessage response = await PostAsync(
            client, ModuleEnvelope("evt-unknown-1", "hexalith/not-registered/events", "Hexalith.Unknown.SomethingV1"));

        // 200, not 500 → DAPR drops the message instead of retrying.
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        EventIngestionResponse? body = await response.Content.ReadFromJsonAsync<EventIngestionResponse>();
        body.ShouldNotBeNull();
        body!.Status.ShouldBe(EventIngestionResponse.StatusUnknownSource);
        body.InstanceId.ShouldBeNull();
        body.WasDuplicate.ShouldBeFalse();

        await _factory.Scheduler.DidNotReceive().ScheduleAsync(
            Arg.Any<string>(), Arg.Any<Hexalith.Memories.Contracts.V1.IngestionInput>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task TenantNotFoundRouteFailure_Returns500ForDaprRetryWithoutScheduling()
    {
        // Story 21.6: missing tenants can be rollout-ordering or registry-lag conditions, so the endpoint
        // must return non-2xx to drive DAPR retry rather than ACK/drop. The event must not reserve a dedup
        // key or schedule a workflow until a later retry resolves an active tenant route.
        _factory.Router
            .ResolveAsync(Arg.Any<CloudEventEnvelope>(), Arg.Any<CancellationToken>())
            .Returns(TenantEventRouteResolution.TenantNotFound("tenant-events"));

        using HttpClient client = _factory.CreateClient();
        using HttpResponseMessage response = await PostAsync(
            client, ModuleEnvelope("evt-missing-tenant-1", "hexalith/tenants/events", "Hexalith.Tenants.TenantCreatedV1"));

        response.StatusCode.ShouldBe(HttpStatusCode.InternalServerError);

        EventIngestionResponse? body = await response.Content.ReadFromJsonAsync<EventIngestionResponse>();
        body.ShouldNotBeNull();
        body!.Status.ShouldBe(EventIngestionResponse.StatusTenantNotFound);
        body.InstanceId.ShouldBeNull();
        body.WasDuplicate.ShouldBeFalse();

        await _factory.PreflightDedup.DidNotReceive().TryReserveAsync(
            Arg.Any<string>(), Arg.Any<TimeSpan>(), Arg.Any<CancellationToken>());
        await _factory.Scheduler.DidNotReceive().ScheduleAsync(
            Arg.Any<string>(), Arg.Any<Hexalith.Memories.Contracts.V1.IngestionInput>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task TenantDeletingOrUnavailableRouteFailure_Returns500ForDaprRetryWithoutScheduling()
    {
        // Story 21.6: deleting and unavailable tenant states share the TenantDeleting route-resolution path.
        // The HTTP boundary must therefore retry both lifecycle failures while preserving no-schedule/no-dedup
        // behavior before tenant routing is accepted.
        _factory.Router
            .ResolveAsync(Arg.Any<CloudEventEnvelope>(), Arg.Any<CancellationToken>())
            .Returns(TenantEventRouteResolution.TenantDeleting("tenant-events"));

        using HttpClient client = _factory.CreateClient();
        using HttpResponseMessage response = await PostAsync(
            client, ModuleEnvelope("evt-unavailable-tenant-1", "hexalith/tenants/events", "Hexalith.Tenants.TenantCreatedV1"));

        response.StatusCode.ShouldBe(HttpStatusCode.InternalServerError);

        EventIngestionResponse? body = await response.Content.ReadFromJsonAsync<EventIngestionResponse>();
        body.ShouldNotBeNull();
        body!.Status.ShouldBe(EventIngestionResponse.StatusTenantDeleting);
        body.InstanceId.ShouldBeNull();
        body.WasDuplicate.ShouldBeFalse();

        await _factory.PreflightDedup.DidNotReceive().TryReserveAsync(
            Arg.Any<string>(), Arg.Any<TimeSpan>(), Arg.Any<CancellationToken>());
        await _factory.Scheduler.DidNotReceive().ScheduleAsync(
            Arg.Any<string>(), Arg.Any<Hexalith.Memories.Contracts.V1.IngestionInput>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DuplicateDelivery_ToSharedTopic_IsIdempotent_SecondDeliveryReturnsDuplicateWithoutRescheduling()
    {
        // AC4 + AC7: DAPR pub/sub is at-least-once, so the SAME CloudEvent can be delivered twice. The second
        // delivery must be absorbed by preflight dedup — returning duplicate WITHOUT scheduling a second
        // workflow — so the shared-topic path produces exactly one memory unit per logical event.
        _factory.Router
            .ResolveAsync(Arg.Any<CloudEventEnvelope>(), Arg.Any<CancellationToken>())
            .Returns(TenantEventRouteResolution.Accepted(new TenantEventRoute("tenant-events", "tenant-events:case", "Tenants")));
        _factory.PreflightDedup
            .TryReserveAsync(Arg.Any<string>(), Arg.Any<TimeSpan>(), Arg.Any<CancellationToken>())
            .Returns(PreflightReservationResult.Reserved, PreflightReservationResult.Duplicate);
        _factory.Scheduler
            .ScheduleAsync(Arg.Any<string>(), Arg.Any<Hexalith.Memories.Contracts.V1.IngestionInput>(), Arg.Any<CancellationToken>())
            .Returns(ci => ci.ArgAt<string>(0));

        string envelope = ModuleEnvelope("evt-dup-1", "hexalith/tenants/events", "Hexalith.Tenants.TenantCreatedV1");

        using HttpClient client = _factory.CreateClient();
        EventIngestionResponse first = await PublishAsync(client, envelope);
        EventIngestionResponse second = await PublishAsync(client, envelope);

        first.Status.ShouldBe(EventIngestionResponse.StatusAccepted);
        first.InstanceId.ShouldNotBeNullOrEmpty();

        second.Status.ShouldBe(EventIngestionResponse.StatusDuplicate);
        second.WasDuplicate.ShouldBeTrue();
        second.InstanceId.ShouldBeNull();

        // Exactly one workflow scheduled across both deliveries — duplicate-safe.
        await _factory.Scheduler.Received(1).ScheduleAsync(
            Arg.Any<string>(), Arg.Any<Hexalith.Memories.Contracts.V1.IngestionInput>(), Arg.Any<CancellationToken>());
    }

    private static async Task<EventIngestionResponse> PublishAsync(HttpClient client, string envelope)
    {
        using HttpResponseMessage response = await PostAsync(client, envelope);
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        EventIngestionResponse? body = await response.Content.ReadFromJsonAsync<EventIngestionResponse>();
        body.ShouldNotBeNull();
        return body!;
    }

    private static Task<HttpResponseMessage> PostAsync(HttpClient client, string envelope)
    {
        // CloudEvents structured-mode content type — the shape DAPR pub/sub delivers to /events/ingest.
        StringContent content = new(envelope, Encoding.UTF8, "application/cloudevents+json");
        return client.PostAsync("/events/ingest", content);
    }

    public void Dispose() => _factory.Dispose();
}
