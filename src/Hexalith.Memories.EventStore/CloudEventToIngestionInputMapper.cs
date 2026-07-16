// <copyright file="CloudEventToIngestionInputMapper.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.EventStore;

using System.Text;
using System.Text.Json;

using Hexalith.Memories.Contracts.V1;

/// <summary>Maps a parsed CloudEvents envelope to the existing <see cref="IngestionInput"/> contract.
/// Pure function — preserves required CloudEvents envelope fields as metadata and defers all side-effects
/// (scheduling, dedup, tenant lookup) to the caller.</summary>
internal static class CloudEventToIngestionInputMapper
{
    /// <summary>Metadata key constants preserved from the CloudEvents envelope. Subject and aggregate type
    /// are exposed as constants so the exact-match metadata filter/index path (AC #2, #4) can reference
    /// them by a single authoritative symbol.</summary>
    internal const string MetadataCloudEventId = "cloudevent.id";

    internal const string MetadataCloudEventSource = "cloudevent.source";

    internal const string MetadataCloudEventType = "cloudevent.type";

    internal const string MetadataCloudEventSubject = "cloudevent.subject";

    internal const string MetadataCloudEventTime = "cloudevent.time";

    internal const string MetadataEventAggregateType = "event.aggregateType";

    /// <summary>Constant used as <see cref="IngestionInput.IngestedBy"/> for every event-sourced ingestion,
    /// preserving provenance regardless of any <c>userid</c> attribute the publisher included.</summary>
    internal const string IngestedByEvents = "events";

    /// <summary>Placeholder written when CloudEvents <c>subject</c> is absent, so downstream exact-match
    /// filters can still group un-subjected events deterministically without crashing on null.</summary>
    internal const string SubjectUnset = "(unset)";

    /// <summary>Maps <paramref name="envelope"/> to an <see cref="IngestionInput"/> ready for the existing
    /// <c>IngestionWorkflow</c>. Envelope validity is the caller's responsibility — this method assumes
    /// <see cref="CloudEventEnvelopeParser"/> has already thrown for missing required fields.</summary>
    /// <param name="envelope">The parsed CloudEvents envelope.</param>
    /// <param name="route">The resolved tenant + case + aggregate-type routing outcome.</param>
    /// <returns>An <see cref="IngestionInput"/> populated with envelope metadata and the event data payload.</returns>
    internal static IngestionInput Map(CloudEventEnvelope envelope, TenantEventRoute route)
    {
        ArgumentNullException.ThrowIfNull(envelope);
        ArgumentNullException.ThrowIfNull(route);

        if (envelope.Data.ValueKind == JsonValueKind.Undefined)
        {
            throw new InvalidOperationException("cloudevent.data missing");
        }

        byte[] contentBytes = Encoding.UTF8.GetBytes(envelope.Data.GetRawText());
        string contentType = string.IsNullOrWhiteSpace(envelope.DataContentType)
            ? "application/json"
            : envelope.DataContentType;

        string subject = envelope.Subject ?? SubjectUnset;

        Dictionary<string, MetadataField> metadata = new(StringComparer.Ordinal)
        {
            [MetadataCloudEventId] = Field(envelope.Id),
            [MetadataCloudEventSource] = Field(envelope.Source),
            [MetadataCloudEventType] = Field(envelope.Type),
            [MetadataCloudEventSubject] = Field(subject),
            [MetadataCloudEventTime] = Field(envelope.Time ?? string.Empty),
            [MetadataEventAggregateType] = Field(route.AggregateType),
        };

        return new IngestionInput
        {
            TenantId = route.TenantId,
            CaseId = route.CaseId,
            SourceUri = envelope.Id,
            ContentBytes = contentBytes,
            ContentType = contentType,
            SourceType = SourceType.Event,
            IngestedBy = IngestedByEvents,
            Metadata = metadata,
        };
    }

    private static MetadataField Field(string value)
        => new(value, MetadataOrigin.Ai, Confidence: 1.0f);
}
