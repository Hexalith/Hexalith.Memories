// <copyright file="AccessTelemetryCanonicalizer.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.AccessTelemetry.Contracts;

using System.Buffers;
using System.Security.Cryptography;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.RegularExpressions;

/// <summary>Canonicalizes and strictly parses the integer-only RFC 8785 lifecycle V1 profile.</summary>
public static partial class AccessTelemetryCanonicalizer
{
    private static readonly HashSet<string> ExpectedPropertyNames = new(StringComparer.Ordinal)
    {
        "acceptedAtUtc",
        "caseMarker",
        "durationMs",
        "emittedAtUtc",
        "envelopeHash",
        "errorCode",
        "eventId",
        "expiresAtUtc",
        "markerKeyId",
        "operationType",
        "outcome",
        "queryParams",
        "recordId",
        "resultCount",
        "schemaVersion",
        "spanId",
        "tenantMarker",
        "traceId",
        "userMarker",
    };

    private static readonly HashSet<string> Operations = new(StringComparer.Ordinal)
    {
        "search",
        "ingest",
        "traverse",
        "case-access",
        "delete",
        "tenant-lifecycle",
        "tenant-config",
        "case-member",
        "annotation",
    };

    /// <summary>Serializes the complete canonical record with explicit nulls.</summary>
    /// <param name="record">Record to serialize.</param>
    /// <returns>Canonical UTF-8 JSON.</returns>
    public static byte[] CanonicalizeRecord(AccessTelemetryRecord record)
    {
        Validate(record, requireEnvelopeHash: true);
        byte[] bytes = Write(record, envelopeOnly: false);
        if (bytes.Length > AccessTelemetryOptions.MaximumRecordBytes)
        {
            throw new AccessTelemetryContractException("canonical_record_too_large");
        }

        return bytes;
    }

    /// <summary>Serializes the immutable envelope used by idempotency checks.</summary>
    /// <param name="record">Record whose immutable fields are serialized.</param>
    /// <returns>Canonical UTF-8 JSON excluding acceptedAtUtc and envelopeHash.</returns>
    public static byte[] CanonicalizeEnvelope(AccessTelemetryRecord record)
    {
        Validate(record, requireEnvelopeHash: false);
        return Write(record, envelopeOnly: true);
    }

    /// <summary>Calculates the lowercase SHA-256 hash of the immutable envelope.</summary>
    /// <param name="record">Record to hash.</param>
    /// <returns>Lowercase hexadecimal SHA-256.</returns>
    public static string CalculateEnvelopeHash(AccessTelemetryRecord record)
        => Convert.ToHexString(SHA256.HashData(CanonicalizeEnvelope(record))).ToLowerInvariant();

    /// <summary>Strictly parses and verifies a canonical V1 record.</summary>
    /// <param name="utf8Json">Canonical UTF-8 JSON.</param>
    /// <returns>The verified record.</returns>
    public static AccessTelemetryRecord ParseCanonicalRecord(ReadOnlySpan<byte> utf8Json)
    {
        if (utf8Json.Length == 0 || utf8Json.Length > AccessTelemetryOptions.MaximumRecordBytes)
        {
            throw new AccessTelemetryContractException("canonical_record_size_invalid");
        }

        try
        {
            using JsonDocument document = JsonDocument.Parse(
                utf8Json.ToArray(),
                new JsonDocumentOptions { AllowTrailingCommas = false, CommentHandling = JsonCommentHandling.Disallow, MaxDepth = 3 });
            JsonElement root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object)
            {
                throw new AccessTelemetryContractException("canonical_record_not_object");
            }

            Dictionary<string, JsonElement> properties = new(StringComparer.Ordinal);
            foreach (JsonProperty property in root.EnumerateObject())
            {
                if (!ExpectedPropertyNames.Contains(property.Name) || !properties.TryAdd(property.Name, property.Value))
                {
                    throw new AccessTelemetryContractException("canonical_record_field_invalid");
                }
            }

            if (properties.Count != ExpectedPropertyNames.Count)
            {
                throw new AccessTelemetryContractException("canonical_record_field_missing");
            }

            AccessTelemetryRecord record = new()
            {
                AcceptedAtUtc = GetRequiredString(properties, "acceptedAtUtc"),
                CaseMarker = GetNullableString(properties, "caseMarker"),
                DurationMs = GetRequiredInt64(properties, "durationMs"),
                EmittedAtUtc = GetRequiredString(properties, "emittedAtUtc"),
                EnvelopeHash = GetRequiredString(properties, "envelopeHash"),
                ErrorCode = GetNullableString(properties, "errorCode"),
                EventId = GetRequiredInt32(properties, "eventId"),
                ExpiresAtUtc = GetRequiredString(properties, "expiresAtUtc"),
                MarkerKeyId = GetRequiredString(properties, "markerKeyId"),
                OperationType = GetRequiredString(properties, "operationType"),
                Outcome = GetRequiredString(properties, "outcome"),
                QueryParams = ReadQueryParams(properties["queryParams"]),
                RecordId = GetRequiredString(properties, "recordId"),
                ResultCount = GetNullableInt32(properties, "resultCount"),
                SchemaVersion = GetRequiredInt32(properties, "schemaVersion"),
                SpanId = GetNullableString(properties, "spanId"),
                TenantMarker = GetRequiredString(properties, "tenantMarker"),
                TraceId = GetNullableString(properties, "traceId"),
                UserMarker = GetNullableString(properties, "userMarker"),
            };
            byte[] canonical = CanonicalizeRecord(record);
            if (!utf8Json.SequenceEqual(canonical))
            {
                throw new AccessTelemetryContractException("canonical_record_noncanonical");
            }

            return record;
        }
        catch (AccessTelemetryContractException)
        {
            throw;
        }
        catch (Exception exception) when (exception is JsonException or InvalidOperationException or FormatException or OverflowException)
        {
            throw new AccessTelemetryContractException("canonical_record_invalid");
        }
    }

    private static int GetRequiredInt32(Dictionary<string, JsonElement> properties, string name)
    {
        if (!properties[name].TryGetInt32(out int value))
        {
            throw new AccessTelemetryContractException("canonical_record_number_invalid");
        }

        return value;
    }

    private static long GetRequiredInt64(Dictionary<string, JsonElement> properties, string name)
    {
        if (!properties[name].TryGetInt64(out long value))
        {
            throw new AccessTelemetryContractException("canonical_record_number_invalid");
        }

        return value;
    }

    private static string GetRequiredString(Dictionary<string, JsonElement> properties, string name)
        => properties[name].ValueKind == JsonValueKind.String
            ? properties[name].GetString() ?? throw new AccessTelemetryContractException("canonical_record_string_invalid")
            : throw new AccessTelemetryContractException("canonical_record_string_invalid");

    private static int? GetNullableInt32(Dictionary<string, JsonElement> properties, string name)
        => properties[name].ValueKind == JsonValueKind.Null ? null : GetRequiredInt32(properties, name);

    private static string? GetNullableString(Dictionary<string, JsonElement> properties, string name)
        => properties[name].ValueKind == JsonValueKind.Null ? null : GetRequiredString(properties, name);

    private static IReadOnlyDictionary<string, object?> ReadQueryParams(JsonElement element)
    {
        if (element.ValueKind != JsonValueKind.Object)
        {
            throw new AccessTelemetryContractException("query_params_not_object");
        }

        Dictionary<string, object?> values = new(StringComparer.Ordinal);
        foreach (JsonProperty property in element.EnumerateObject())
        {
            if (property.Name.Length is < 1 or > 32 || !QueryKeyRegex().IsMatch(property.Name) || !values.TryAdd(property.Name, ReadScalar(property.Value)))
            {
                throw new AccessTelemetryContractException("query_params_field_invalid");
            }
        }

        if (values.Count > 6)
        {
            throw new AccessTelemetryContractException("query_params_count_invalid");
        }

        return values;
    }

    private static object? ReadScalar(JsonElement value)
        => value.ValueKind switch
        {
            JsonValueKind.String => value.GetString(),
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.Number when value.TryGetInt32(out int number) => number,
            JsonValueKind.Null => null,
            _ => throw new AccessTelemetryContractException("query_params_value_invalid"),
        };

    private static void Validate(AccessTelemetryRecord record, bool requireEnvelopeHash)
    {
        ArgumentNullException.ThrowIfNull(record);
        if (record.SchemaVersion != 1 || !UlidRegex().IsMatch(record.RecordId))
        {
            throw new AccessTelemetryContractException("schema_or_record_id_invalid");
        }

        if (!Operations.Contains(record.OperationType) || record.Outcome is not ("ok" or "partial" or "error"))
        {
            throw new AccessTelemetryContractException("operation_or_outcome_invalid");
        }

        if (record.EventId is < 7501 or > 7519 || record.EventId == 7510 || record.DurationMs is < 0 or > 86_400_000)
        {
            throw new AccessTelemetryContractException("event_or_duration_invalid");
        }

        if (record.ResultCount is < 0 or > 1_000_000 || !TimestampRegex().IsMatch(record.EmittedAtUtc) ||
            !TimestampRegex().IsMatch(record.AcceptedAtUtc) || !TimestampRegex().IsMatch(record.ExpiresAtUtc))
        {
            throw new AccessTelemetryContractException("numeric_or_timestamp_invalid");
        }

        ValidateMarker(record.TenantMarker, allowRejected: true);
        ValidateNullableMarker(record.UserMarker);
        ValidateNullableMarker(record.CaseMarker);
        if (!KeyIdRegex().IsMatch(record.MarkerKeyId) || !NullableHexRegex(record.TraceId, 32) || !NullableHexRegex(record.SpanId, 16))
        {
            throw new AccessTelemetryContractException("marker_or_trace_invalid");
        }

        if (record.QueryParams.Count > 6)
        {
            throw new AccessTelemetryContractException("query_params_count_invalid");
        }

        foreach ((string key, object? value) in record.QueryParams)
        {
            if (!QueryKeyRegex().IsMatch(key) || key.Length > 32 || value is not (null or string or bool or int))
            {
                throw new AccessTelemetryContractException("query_params_field_invalid");
            }

            if (value is string text && text.Length > 64)
            {
                throw new AccessTelemetryContractException("query_params_value_invalid");
            }
        }

        if (record.Outcome == "ok" && record.ErrorCode is not null)
        {
            throw new AccessTelemetryContractException("error_code_invalid");
        }

        if (requireEnvelopeHash)
        {
            if (!LowerHex64Regex().IsMatch(record.EnvelopeHash) ||
                !CryptographicOperations.FixedTimeEquals(
                    Convert.FromHexString(record.EnvelopeHash),
                    Convert.FromHexString(CalculateEnvelopeHash(record))))
            {
                throw new AccessTelemetryContractException("envelope_hash_invalid");
            }
        }
    }

    private static bool NullableHexRegex(string? value, int length)
        => value is null || (value.Length == length && value.All(static character => character is >= '0' and <= '9' or >= 'a' and <= 'f'));

    private static void ValidateMarker(string value, bool allowRejected)
    {
        if ((allowRejected && value == "__rejected__") || LowerHex64Regex().IsMatch(value))
        {
            return;
        }

        throw new AccessTelemetryContractException("marker_invalid");
    }

    private static void ValidateNullableMarker(string? value)
    {
        if (value is not null)
        {
            ValidateMarker(value, allowRejected: false);
        }
    }

    private static byte[] Write(AccessTelemetryRecord record, bool envelopeOnly)
    {
        var buffer = new ArrayBufferWriter<byte>(AccessTelemetryOptions.MaximumRecordBytes);
        using (var writer = new Utf8JsonWriter(
            buffer,
            new JsonWriterOptions { Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping, Indented = false, SkipValidation = false }))
        {
            writer.WriteStartObject();
            if (!envelopeOnly)
            {
                writer.WriteString("acceptedAtUtc", record.AcceptedAtUtc);
            }

            WriteNullableString(writer, "caseMarker", record.CaseMarker);
            writer.WriteNumber("durationMs", record.DurationMs);
            writer.WriteString("emittedAtUtc", record.EmittedAtUtc);
            if (!envelopeOnly)
            {
                writer.WriteString("envelopeHash", record.EnvelopeHash);
            }

            WriteNullableString(writer, "errorCode", record.ErrorCode);
            writer.WriteNumber("eventId", record.EventId);
            writer.WriteString("expiresAtUtc", record.ExpiresAtUtc);
            writer.WriteString("markerKeyId", record.MarkerKeyId);
            writer.WriteString("operationType", record.OperationType);
            writer.WriteString("outcome", record.Outcome);
            writer.WritePropertyName("queryParams");
            writer.WriteStartObject();
            foreach ((string key, object? value) in record.QueryParams.OrderBy(static pair => pair.Key, StringComparer.Ordinal))
            {
                WriteScalar(writer, key, value);
            }

            writer.WriteEndObject();
            writer.WriteString("recordId", record.RecordId);
            if (record.ResultCount is int resultCount)
            {
                writer.WriteNumber("resultCount", resultCount);
            }
            else
            {
                writer.WriteNull("resultCount");
            }

            writer.WriteNumber("schemaVersion", record.SchemaVersion);
            WriteNullableString(writer, "spanId", record.SpanId);
            writer.WriteString("tenantMarker", record.TenantMarker);
            WriteNullableString(writer, "traceId", record.TraceId);
            WriteNullableString(writer, "userMarker", record.UserMarker);
            writer.WriteEndObject();
        }

        return buffer.WrittenSpan.ToArray();
    }

    private static void WriteNullableString(Utf8JsonWriter writer, string name, string? value)
    {
        if (value is null)
        {
            writer.WriteNull(name);
        }
        else
        {
            writer.WriteString(name, value);
        }
    }

    private static void WriteScalar(Utf8JsonWriter writer, string name, object? value)
    {
        switch (value)
        {
            case null:
                writer.WriteNull(name);
                break;
            case string text:
                writer.WriteString(name, text);
                break;
            case bool flag:
                writer.WriteBoolean(name, flag);
                break;
            case int number:
                writer.WriteNumber(name, number);
                break;
            default:
                throw new AccessTelemetryContractException("query_params_value_invalid");
        }
    }

    [GeneratedRegex("^[a-z][A-Za-z0-9]{0,31}$", RegexOptions.CultureInvariant)]
    private static partial Regex QueryKeyRegex();

    [GeneratedRegex("^[a-z0-9][a-z0-9-]{0,31}$", RegexOptions.CultureInvariant)]
    private static partial Regex KeyIdRegex();

    [GeneratedRegex("^[0-9a-f]{64}$", RegexOptions.CultureInvariant)]
    private static partial Regex LowerHex64Regex();

    [GeneratedRegex("^[0-9A-HJKMNP-TV-Z]{26}$", RegexOptions.CultureInvariant)]
    private static partial Regex UlidRegex();

    [GeneratedRegex("^\\d{4}-\\d{2}-\\d{2}T\\d{2}:\\d{2}:\\d{2}\\.\\d{3}Z$", RegexOptions.CultureInvariant)]
    private static partial Regex TimestampRegex();
}
