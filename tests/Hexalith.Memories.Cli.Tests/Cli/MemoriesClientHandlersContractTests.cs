// <copyright file="MemoriesClientHandlersContractTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Cli.Tests.Cli;

using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;

using Hexalith.Memories.Client.Rest;
using Hexalith.Memories.Contracts.V1;

using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

using Shouldly;

/// <summary>Story 9.3 Fix #6 — consumer-driven contract test. Constructs server-shape instances,
/// serializes them via <see cref="MemoriesJsonContext.Options"/>, pipes the bytes through a mocked
/// <see cref="HttpMessageHandler"/> into the client, and asserts the round-tripped object is
/// structurally equal. Catches <c>MemoriesJsonContext</c> registration drift + enum-converter
/// omissions at <c>dotnet build</c> time rather than commit time.</summary>
public sealed class MemoriesClientHandlersContractTests
{
    [Fact]
    public async Task ListHandlersAsync_RoundTripsServerShapeViaMemoriesJsonContext()
    {
        HandlerRegistrationSnapshot serverInstance = new()
        {
            PubSubName = "pubsub",
            Topic = "events",
            AsOf = "2026-04-24T12:00:00+00:00",
            SubscriptionStatus = HandlerSubscriptionStatus.Active,
            Handlers = new List<HandlerRegistration>
            {
                new()
                {
                    TenantId = "acme",
                    SourcePrefix = "acme.events",
                    EventTypePatterns = new List<string> { "Claims" },
                    EventsProcessedCount = 5,
                    LastEventAt = "2026-04-24T11:59:00+00:00",
                    ObservedEventTypes = new List<ObservedEventTypeSummary>
                    {
                        new()
                        {
                            AggregateType = "Claims",
                            EventType = "ClaimSubmittedV2",
                            Count = 5,
                            LastSeenAt = "2026-04-24T11:59:00+00:00",
                        },
                    },
                    Error = null,
                },
            },
        };

        string json = JsonSerializer.Serialize(serverInstance, MemoriesJsonContext.Options);
        json.ShouldContain("\"subscriptionStatus\":\"active\"");

        Uri? requestUri = null;
        using HttpClient httpClient = CreateClient(json, request => requestUri = request.RequestUri);
        MemoriesClient client = new(
            httpClient,
            Options.Create(new MemoriesClientOptions { Endpoint = httpClient.BaseAddress! }),
            NullLogger<MemoriesClient>.Instance);

#pragma warning disable HXL002
        HandlerRegistrationSnapshot received = await client.ListHandlersAsync(CancellationToken.None);
#pragma warning restore HXL002

        received.PubSubName.ShouldBe(serverInstance.PubSubName);
        received.Topic.ShouldBe(serverInstance.Topic);
        received.SubscriptionStatus.ShouldBe(HandlerSubscriptionStatus.Active);
        received.Handlers.Count.ShouldBe(1);
        received.Handlers[0].EventsProcessedCount.ShouldBe(5L);
        received.Handlers[0].ObservedEventTypes[0].EventType.ShouldBe("ClaimSubmittedV2");
        requestUri.ShouldNotBeNull();
        requestUri.AbsolutePath.ShouldBe(MemoriesRoutes.Handlers);
    }

    [Fact]
    public async Task GetHandlerMismatchesAsync_RoundTripsServerShapeWithCamelCaseEnums()
    {
        HandlerMismatchReport serverInstance = new()
        {
            TenantId = "acme",
            AsOf = "2026-04-24T12:00:00+00:00",
            WindowHours = 24,
            Summary = new HandlerMismatchReportSummary { RoutesConfigured = 1, ObservationsChecked = 2 },
            Mismatches = new List<HandlerMismatch>
            {
                new()
                {
                    Category = HandlerMismatchCategory.ProjectionBindingMissing,
                    Severity = HandlerMismatchSeverity.Warning,
                    Subject = "acme/enterprise/claims/claimsubmitted",
                    Context = "projection binding missing",
                    Suggestion = "register an authoritative projection binding",
                },
            },
        };

        string json = JsonSerializer.Serialize(serverInstance, MemoriesJsonContext.Options);
        json.ShouldContain("\"category\":\"projectionBindingMissing\"");
        json.ShouldContain("\"severity\":\"warning\"");

        Uri? requestUri = null;
        using HttpClient httpClient = CreateClient(json, request => requestUri = request.RequestUri);
        MemoriesClient client = new(
            httpClient,
            Options.Create(new MemoriesClientOptions { Endpoint = httpClient.BaseAddress! }),
            NullLogger<MemoriesClient>.Instance);

#pragma warning disable HXL002
        HandlerMismatchReport received = await client.GetHandlerMismatchesAsync("acme", CancellationToken.None);
#pragma warning restore HXL002

        received.Mismatches.Count.ShouldBe(1);
        received.Mismatches[0].Category.ShouldBe(HandlerMismatchCategory.ProjectionBindingMissing);
        received.Mismatches[0].Severity.ShouldBe(HandlerMismatchSeverity.Warning);
        received.Summary.RoutesConfigured.ShouldBe(1);
        received.HasWarnings.ShouldBeTrue();
        requestUri.ShouldNotBeNull();
        requestUri.AbsolutePath.ShouldBe(MemoriesRoutes.TenantHandlerMismatches.Replace("{tenantId}", "acme", StringComparison.Ordinal));
    }

    [Fact]
    public async Task ListHandlersAsync_OnNonSuccess_ThrowsMemoriesRemoteException()
    {
        HttpResponseMessage response = new(HttpStatusCode.InternalServerError)
        {
            Content = new StringContent(
                "{\"code\":\"REDIS_UNAVAILABLE\",\"message\":\"boom\",\"suggestion\":\"check redis\"}",
                Encoding.UTF8,
                "application/json"),
        };
        using HttpClient httpClient = new(new StaticResponseHandler(response)) { BaseAddress = new Uri("http://127.0.0.1:65012/") };
        MemoriesClient client = new(
            httpClient,
            Options.Create(new MemoriesClientOptions { Endpoint = httpClient.BaseAddress! }),
            NullLogger<MemoriesClient>.Instance);

#pragma warning disable HXL002
        MemoriesRemoteException exception = await Should.ThrowAsync<MemoriesRemoteException>(
            () => client.ListHandlersAsync(CancellationToken.None));
#pragma warning restore HXL002

        exception.Error.Code.ShouldBe("REDIS_UNAVAILABLE");
    }

    private static HttpClient CreateClient(
        string jsonBody,
        Action<HttpRequestMessage>? inspectRequest = null)
    {
        HttpResponseMessage response = new(HttpStatusCode.OK)
        {
            Content = new StringContent(jsonBody, Encoding.UTF8, "application/json"),
        };
        return new HttpClient(new StaticResponseHandler(response, inspectRequest)) { BaseAddress = new Uri("http://127.0.0.1:65011/") };
    }

    private sealed class StaticResponseHandler(
        HttpResponseMessage response,
        Action<HttpRequestMessage>? inspectRequest = null) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            inspectRequest?.Invoke(request);
            return Task.FromResult(CloneResponse(response));
        }

        private static HttpResponseMessage CloneResponse(HttpResponseMessage source)
        {
            HttpResponseMessage clone = new(source.StatusCode);
            if (source.Content is not null)
            {
                string content = source.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                clone.Content = new StringContent(
                    content,
                    Encoding.UTF8,
                    source.Content.Headers.ContentType?.MediaType ?? "application/json");
            }

            return clone;
        }
    }
}
