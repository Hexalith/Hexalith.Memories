// <copyright file="CloudEventEnvelopeParser.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.EventStore;

using System.Text.Json;

/// <summary>Parses a raw CloudEvents 1.0 JSON envelope into a <see cref="CloudEventEnvelope"/>.
/// Validates required fields (<c>id</c>, <c>source</c>, <c>type</c>, <c>data</c>) and throws
/// <see cref="InvalidOperationException"/> with the canonical <c>cloudevent.&lt;field&gt; missing</c> message
/// so the controller can translate the failure to a typed 400 response (AC #6, #8).</summary>
internal static class CloudEventEnvelopeParser
{
    /// <summary>Parses <paramref name="root"/> as a CloudEvents JSON envelope.</summary>
    /// <param name="root">The root JSON element containing the CloudEvents envelope.</param>
    /// <returns>A <see cref="CloudEventEnvelope"/> populated from <paramref name="root"/>.</returns>
    /// <exception cref="InvalidOperationException">Thrown when a required field is missing or the envelope is not a JSON object.</exception>
    internal static CloudEventEnvelope Parse(JsonElement root)
    {
        if (root.ValueKind != JsonValueKind.Object)
        {
            throw new InvalidOperationException("cloudevent.envelope missing");
        }

        string id = RequiredString(root, "id");
        string source = RequiredString(root, "source");
        string type = RequiredString(root, "type");

        if (!root.TryGetProperty("data", out JsonElement data) || data.ValueKind == JsonValueKind.Null)
        {
            throw new InvalidOperationException("cloudevent.data missing");
        }

        string? subject = OptionalString(root, "subject");
        string? time = OptionalString(root, "time");
        string? dataContentType = OptionalString(root, "datacontenttype");

        return new CloudEventEnvelope(id, source, type, subject, time, dataContentType, data);
    }

    private static string RequiredString(JsonElement root, string property)
    {
        if (!root.TryGetProperty(property, out JsonElement element) || element.ValueKind != JsonValueKind.String)
        {
            throw new InvalidOperationException($"cloudevent.{property} missing");
        }

        string? value = element.GetString();
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException($"cloudevent.{property} missing");
        }

        return value;
    }

    private static string? OptionalString(JsonElement root, string property)
    {
        if (!root.TryGetProperty(property, out JsonElement element) || element.ValueKind != JsonValueKind.String)
        {
            return null;
        }

        string? value = element.GetString();
        return string.IsNullOrWhiteSpace(value) ? null : value;
    }
}
