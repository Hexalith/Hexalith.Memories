// <copyright file="CloudEventRequestNormalizer.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.EventStore;

using System.Text.Json;

using Microsoft.AspNetCore.Http;

/// <summary>Normalizes incoming subscription request bodies so the ingestion service always sees a
/// CloudEvents-shaped envelope. Dapr's middleware can deliver either the full structured envelope or the
/// unwrapped <c>data</c> payload plus <c>ce-*</c> headers; this helper rehydrates the latter into the former.</summary>
internal static class CloudEventRequestNormalizer
{
    internal static JsonElement Normalize(JsonElement requestBody, IHeaderDictionary headers, JsonElement? capturedEnvelope = null)
    {
        ArgumentNullException.ThrowIfNull(headers);

        if (LooksLikeCloudEventEnvelope(requestBody))
        {
            return requestBody;
        }

        if (capturedEnvelope is JsonElement originalEnvelope && LooksLikeCloudEventEnvelope(originalEnvelope))
        {
            return originalEnvelope;
        }

        if (!HasRequiredCloudEventHeaders(headers))
        {
            return requestBody;
        }

        Dictionary<string, object?> envelope = new(StringComparer.Ordinal)
        {
            ["id"] = headers["ce-id"].ToString(),
            ["source"] = headers["ce-source"].ToString(),
            ["type"] = headers["ce-type"].ToString(),
            ["data"] = requestBody.Clone(),
        };

        string subject = headers["ce-subject"].ToString();
        if (!string.IsNullOrWhiteSpace(subject))
        {
            envelope["subject"] = subject;
        }

        string time = headers["ce-time"].ToString();
        if (!string.IsNullOrWhiteSpace(time))
        {
            envelope["time"] = time;
        }

        string dataContentType = headers["ce-datacontenttype"].ToString();
        if (!string.IsNullOrWhiteSpace(dataContentType))
        {
            envelope["datacontenttype"] = dataContentType;
        }

        return JsonSerializer.SerializeToElement(envelope);
    }

    private static bool LooksLikeCloudEventEnvelope(JsonElement requestBody)
        => requestBody.ValueKind == JsonValueKind.Object
        && requestBody.TryGetProperty("id", out _)
        && requestBody.TryGetProperty("source", out _)
        && requestBody.TryGetProperty("type", out _)
        && requestBody.TryGetProperty("data", out _);

    private static bool HasRequiredCloudEventHeaders(IHeaderDictionary headers)
        => headers.ContainsKey("ce-id")
        && headers.ContainsKey("ce-source")
        && headers.ContainsKey("ce-type");
}
