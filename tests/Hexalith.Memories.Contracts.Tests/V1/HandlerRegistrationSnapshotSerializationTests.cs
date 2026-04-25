// <copyright file="HandlerRegistrationSnapshotSerializationTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Contracts.Tests.V1;

using System.Collections.Generic;
using System.Text.Json;

using Hexalith.Memories.Contracts.V1;

using Shouldly;

/// <summary>Story 9.3 — round-trip and camelCase verification for the handler-registry contract types.</summary>
public class HandlerRegistrationSnapshotSerializationTests
{
    [Fact]
    public void HandlerRegistrationSnapshot_ShouldRoundTripThroughMemoriesJsonContext()
    {
        HandlerRegistrationSnapshot snapshot = new()
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

        string json = JsonSerializer.Serialize(snapshot, MemoriesJsonContext.Options);
        json.ShouldContain("\"subscriptionStatus\":\"active\"");
        json.ShouldContain("\"pubSubName\":\"pubsub\"");

        HandlerRegistrationSnapshot? roundTripped = JsonSerializer.Deserialize<HandlerRegistrationSnapshot>(
            json, MemoriesJsonContext.Options);

        roundTripped.ShouldNotBeNull();
        roundTripped!.SubscriptionStatus.ShouldBe(HandlerSubscriptionStatus.Active);
        roundTripped.Handlers.Count.ShouldBe(1);
        roundTripped.Handlers[0].EventsProcessedCount.ShouldBe(5L);
        roundTripped.Handlers[0].ObservedEventTypes[0].EventType.ShouldBe("ClaimSubmittedV2");
        roundTripped.Handlers[0].Error.ShouldBeNull();
    }

    [Theory]
    [InlineData(HandlerSubscriptionStatus.Disabled, "\"disabled\"")]
    [InlineData(HandlerSubscriptionStatus.Unknown, "\"unknown\"")]
    [InlineData(HandlerSubscriptionStatus.Active, "\"active\"")]
    public void HandlerSubscriptionStatus_ShouldRoundTripAsCamelCaseString(
        HandlerSubscriptionStatus value, string expectedJson)
    {
        string json = JsonSerializer.Serialize(value, MemoriesJsonContext.Options);
        json.ShouldBe(expectedJson);

        HandlerSubscriptionStatus deserialized = JsonSerializer.Deserialize<HandlerSubscriptionStatus>(
            json, MemoriesJsonContext.Options);
        deserialized.ShouldBe(value);
    }

    [Theory]
    [InlineData("0")]
    [InlineData("1")]
    public void HandlerSubscriptionStatus_ShouldRejectIntegerTokens(string json)
    {
        _ = Should.Throw<JsonException>(() => JsonSerializer.Deserialize<HandlerSubscriptionStatus>(
            json, MemoriesJsonContext.Options));
    }

    [Fact]
    public void HandlerRegistration_WithError_ShouldSerializeErrorAndEmptyArrays()
    {
        HandlerRegistration row = new()
        {
            TenantId = "broken-tenant",
            SourcePrefix = "broken.events",
            EventTypePatterns = new List<string>(),
            EventsProcessedCount = 0,
            LastEventAt = null,
            ObservedEventTypes = new List<ObservedEventTypeSummary>(),
            Error = "OBSERVATION_READ_FAILED",
        };

        string json = JsonSerializer.Serialize(row, MemoriesJsonContext.Options);
        json.ShouldContain("\"error\":\"OBSERVATION_READ_FAILED\"");
        json.ShouldContain("\"eventTypePatterns\":[]");
        json.ShouldContain("\"observedEventTypes\":[]");
        json.ShouldContain("\"lastEventAt\":null");
    }
}
