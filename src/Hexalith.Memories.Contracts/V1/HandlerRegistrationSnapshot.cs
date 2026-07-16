// <copyright file="HandlerRegistrationSnapshot.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Contracts.V1;

using System.Collections.Generic;
using System.Text.Json.Serialization;

/// <summary>Story 9.3 — point-in-time snapshot of every registered handler returned by
/// <c>GET /api/v1/handlers</c>. Experimental surface (see diagnostic HXL002).</summary>
public sealed record HandlerRegistrationSnapshot
{
    [JsonPropertyName("pubSubName")]
    public required string PubSubName { get; init; }

    [JsonPropertyName("topic")]
    public required string Topic { get; init; }

    [JsonPropertyName("asOf")]
    public required string AsOf { get; init; }

    [JsonPropertyName("subscriptionStatus")]
    public required HandlerSubscriptionStatus SubscriptionStatus { get; init; }

    [JsonPropertyName("handlers")]
    public required IReadOnlyList<HandlerRegistration> Handlers { get; init; }
}

/// <summary>Story 9.3 — per-<c>SourceToTenantMap</c>-entry handler registration row.</summary>
public sealed record HandlerRegistration
{
    [JsonPropertyName("tenantId")]
    public required string TenantId { get; init; }

    [JsonPropertyName("sourcePrefix")]
    public required string SourcePrefix { get; init; }

    /// <summary>Distinct <c>aggregateType</c> values observed for this tenant in the last 24h. Always
    /// <c>[]</c> when none seen — the CLI table formatter substitutes a sentinel string at rendering time.</summary>
    [JsonPropertyName("eventTypePatterns")]
    public required IReadOnlyList<string> EventTypePatterns { get; init; }

    /// <summary>Sum of observation counts in the 24h window.</summary>
    [JsonPropertyName("eventsProcessedCount")]
    public required long EventsProcessedCount { get; init; }

    /// <summary>ISO-8601 timestamp of the most-recent observation, or <c>null</c> when none.</summary>
    [JsonPropertyName("lastEventAt")]
    public required string? LastEventAt { get; init; }

    [JsonPropertyName("observedEventTypes")]
    public required IReadOnlyList<ObservedEventTypeSummary> ObservedEventTypes { get; init; }

    /// <summary>Story 9.3 AC #27 / Finding S — per-tenant graceful degradation sentinel. Set to
    /// <c>"OBSERVATION_READ_FAILED"</c> when this tenant's Redis read threw; <c>null</c> on the happy path.</summary>
    [JsonPropertyName("error")]
    public string? Error { get; init; }
}

/// <summary>Story 9.3 — one observed (aggregateType, eventType) tuple with counter + recency.</summary>
public sealed record ObservedEventTypeSummary
{
    [JsonPropertyName("aggregateType")]
    public required string AggregateType { get; init; }

    [JsonPropertyName("eventType")]
    public required string EventType { get; init; }

    [JsonPropertyName("count")]
    public required long Count { get; init; }

    [JsonPropertyName("lastSeenAt")]
    public required string LastSeenAt { get; init; }
}

/// <summary>Story 9.3 — top-level subscription health (3-state; see ADR-9.3-004).</summary>
[JsonConverter(typeof(CamelCaseStringEnumConverter<HandlerSubscriptionStatus>))]
public enum HandlerSubscriptionStatus
{
    /// <summary>Routing-config <c>Topic</c> is empty — subscription is intentionally off.</summary>
    Disabled,

    /// <summary>Routing-config is incomplete or the process has not yet observed traffic during a startup grace window.</summary>
    Unknown,

    /// <summary>Routing-config is set up and the process has either observed traffic or passed the startup grace window.</summary>
    Active,
}
