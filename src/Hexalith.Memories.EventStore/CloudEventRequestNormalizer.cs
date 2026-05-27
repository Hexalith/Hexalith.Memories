// <copyright file="CloudEventRequestNormalizer.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.EventStore;

using System.Text.Json;

using Microsoft.AspNetCore.Http;

public sealed record NormalizedCloudEventEnvelope(
    string Id,
    string Source,
    string Type,
    JsonElement Data,
    string? Subject,
    string? Time,
    string? DataContentType);

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

        string? subject = GetOptionalHeader(headers, "ce-subject");
        string? time = GetOptionalHeader(headers, "ce-time");
        string? dataContentType = GetOptionalHeader(headers, "ce-datacontenttype");

        NormalizedCloudEventEnvelope envelope = new(
            headers["ce-id"].ToString(),
            headers["ce-source"].ToString(),
            headers["ce-type"].ToString(),
            requestBody.Clone(),
            subject,
            time,
            dataContentType);

        return JsonSerializer.SerializeToElement(
            envelope,
            EventStoreJsonContext.Default.NormalizedCloudEventEnvelope);
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

    private static string? GetOptionalHeader(IHeaderDictionary headers, string name)
    {
        string value = headers[name].ToString();
        return string.IsNullOrWhiteSpace(value) ? null : value;
    }
}
