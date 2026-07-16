// <copyright file="MiddlewareOrderTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Tests.EventStoreIntegration;

using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

using Hexalith.Memories.EventStore;
using Hexalith.Memories.Server.Tests.Authentication;

using NSubstitute;

using Shouldly;

/// <summary>Story 9.1 Risk #1 — prove <c>app.UseCloudEvents()</c> does NOT break the existing
/// <c>/api/v1/ingest</c> POST when the request arrives with <c>Content-Type: application/json</c> (plain JSON,
/// not a CloudEvents envelope). The Dapr CloudEvents middleware is a no-op for non-CloudEvents content
/// types — this guard test pins that behavior so a future middleware re-ordering can't silently regress
/// the plain-JSON ingestion path.</summary>
public sealed class MiddlewareOrderTests : System.IDisposable
{
    private readonly EventStoreWebAppFactory _factory = new();

    [Fact]
    public async Task CloudEventsIsNoOpForPlainJson_ReachesEventsIngestUnwrapped()
    {
        // A plain JSON body shaped like a CloudEvents envelope — if UseCloudEvents() is greedy on
        // application/json, it would unwrap the payload and the controller would receive just the inner
        // `data` object. The contract is: only application/cloudevents+json triggers unwrap. So posting
        // this payload as application/json should reach the service as the FULL envelope with `id`,
        // `source`, `type`, `data` — otherwise the envelope parser would not see the required `id` field
        // and the controller would return 400 INVALID_CLOUDEVENT.
        string envelope = """
            {
              "specversion": "1.0",
              "id": "evt-plain-json-1",
              "source": "enterprise/claims",
              "type": "MyApp.Claims.ClaimSubmittedV2",
              "subject": "claim-42",
              "time": "2026-04-22T10:00:00Z",
              "data": { "amount": 100 }
            }
            """;
        _factory.Router
            .ResolveAsync(Arg.Any<CloudEventEnvelope>(), Arg.Any<System.Threading.CancellationToken>())
            .Returns(TenantEventRouteResolution.Accepted(new TenantEventRoute("acme", "c1", "Claims")));
        _factory.PreflightDedup
            .TryReserveAsync(Arg.Any<string>(), Arg.Any<System.TimeSpan>(), Arg.Any<System.Threading.CancellationToken>())
            .Returns(PreflightReservationResult.Reserved);
        _factory.Scheduler
            .ScheduleAsync(Arg.Any<string>(), Arg.Any<Hexalith.Memories.Contracts.V1.IngestionInput>(), Arg.Any<System.Threading.CancellationToken>())
            .Returns(ci => ci.ArgAt<string>(0));

        using HttpClient client = _factory.CreateClient();
        StringContent content = new(envelope, Encoding.UTF8, "application/json");
        HttpResponseMessage response = await client.PostAsync("/events/ingest", content);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        EventIngestionResponse? body = await response.Content.ReadFromJsonAsync<EventIngestionResponse>();
        body.ShouldNotBeNull();
        body!.Status.ShouldBe(EventIngestionResponse.StatusAccepted);
    }

    [Fact]
    public async Task CloudEventsEnvelope_UnwrappedByMiddleware_StillReachesController()
    {
        // Sanity check: even when the publisher uses application/cloudevents+json, the controller still
        // receives a well-formed CloudEvents envelope (UseCloudEvents() may rewrap the body into the
        // controller's JsonElement parameter, but the envelope parser must still resolve required fields).
        string envelope = """
            {
              "specversion": "1.0",
              "id": "evt-ce-1",
              "source": "enterprise/claims",
              "type": "MyApp.Claims.ClaimSubmittedV2",
              "data": { "amount": 100 }
            }
            """;
        _factory.Router
            .ResolveAsync(Arg.Any<CloudEventEnvelope>(), Arg.Any<System.Threading.CancellationToken>())
            .Returns(TenantEventRouteResolution.Accepted(new TenantEventRoute("acme", "c1", "Claims")));
        _factory.PreflightDedup
            .TryReserveAsync(Arg.Any<string>(), Arg.Any<System.TimeSpan>(), Arg.Any<System.Threading.CancellationToken>())
            .Returns(PreflightReservationResult.Reserved);
        _factory.Scheduler
            .ScheduleAsync(Arg.Any<string>(), Arg.Any<Hexalith.Memories.Contracts.V1.IngestionInput>(), Arg.Any<System.Threading.CancellationToken>())
            .Returns(ci => ci.ArgAt<string>(0));

        using HttpClient client = _factory.CreateClient();
        StringContent content = new(envelope, Encoding.UTF8, "application/cloudevents+json");
        HttpResponseMessage response = await client.PostAsync("/events/ingest", content);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        EventIngestionResponse? body = await response.Content.ReadFromJsonAsync<EventIngestionResponse>();
        body.ShouldNotBeNull();
        body!.Status.ShouldBe(EventIngestionResponse.StatusAccepted);
    }

    [Fact]
    public async Task SubscribeHandler_ExposesEventStoreTopicBinding()
    {
        using HttpClient client = _factory.CreateClient();

        HttpResponseMessage response = await client.GetAsync("/dapr/subscribe");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        await using Stream stream = await response.Content.ReadAsStreamAsync();
        using JsonDocument document = await JsonDocument.ParseAsync(stream);

        JsonElement subscription = document.RootElement
            .EnumerateArray()
            .Single(item => string.Equals(GetRoute(item), "events/ingest", StringComparison.OrdinalIgnoreCase)
                || string.Equals(GetRoute(item), "/events/ingest", StringComparison.OrdinalIgnoreCase));

        GetPropertyCaseInsensitive(subscription, "pubsubname").GetString().ShouldBe("pubsub");
        GetPropertyCaseInsensitive(subscription, "topic").GetString().ShouldBe("memories-events");
        GetRoute(subscription).ShouldBeOneOf("events/ingest", "/events/ingest");
    }

    [Fact]
    public async Task ProcessRoute_IsNotMappedAsEventIngestionSurface()
    {
        using HttpClient client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", ServerTestBearerToken.Create());

        StringContent content = new("{}", Encoding.UTF8, "application/json");
        HttpResponseMessage response = await client.PostAsync("/process", content);

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task MalformedStructuredCloudEvent_Returns400InsteadOfMiddleware500()
    {
        using HttpClient client = _factory.CreateClient();

        StringContent content = new("{", Encoding.UTF8, "application/cloudevents+json");
        HttpResponseMessage response = await client.PostAsync("/events/ingest", content);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    private static string GetRoute(JsonElement subscription)
    {
        if (TryGetPropertyCaseInsensitive(subscription, "route", out JsonElement routeProperty)
            && routeProperty.ValueKind == JsonValueKind.String)
        {
            return routeProperty.GetString()!;
        }

        if (TryGetPropertyCaseInsensitive(subscription, "routes", out JsonElement routes)
            && routes.ValueKind == JsonValueKind.Object
            && TryGetPropertyCaseInsensitive(routes, "default", out JsonElement defaultRoute)
            && defaultRoute.ValueKind == JsonValueKind.String)
        {
            return defaultRoute.GetString()!;
        }

        throw new ShouldAssertException($"Subscription payload did not expose a route: {subscription}");
    }

    private static JsonElement GetPropertyCaseInsensitive(JsonElement element, string propertyName)
        => TryGetPropertyCaseInsensitive(element, propertyName, out JsonElement value)
            ? value
            : throw new ShouldAssertException($"Property '{propertyName}' was not found in {element}.");

    private static bool TryGetPropertyCaseInsensitive(JsonElement element, string propertyName, out JsonElement value)
    {
        foreach (JsonProperty property in element.EnumerateObject())
        {
            if (string.Equals(property.Name, propertyName, StringComparison.OrdinalIgnoreCase))
            {
                value = property.Value;
                return true;
            }
        }

        value = default;
        return false;
    }

    public void Dispose() => _factory.Dispose();
}
