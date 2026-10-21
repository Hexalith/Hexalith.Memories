// <copyright file="AccessTelemetryQueryParamsJsonConverter.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.AccessTelemetry.Contracts;

using System.Text.Json;
using System.Text.Json.Serialization;

/// <summary>Preserves the canonical scalar allowlist across HTTP JSON and actor remoting.</summary>
internal sealed class AccessTelemetryQueryParamsJsonConverter : JsonConverter<IReadOnlyDictionary<string, object?>>
{
    /// <summary>Initializes the bounded query-parameter converter.</summary>
    public AccessTelemetryQueryParamsJsonConverter()
    {
    }

    /// <inheritdoc/>
    public override bool HandleNull => true;

    /// <inheritdoc/>
    public override IReadOnlyDictionary<string, object?> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        using JsonDocument document = JsonDocument.ParseValue(ref reader);
        return AccessTelemetryCanonicalizer.ReadQueryParams(document.RootElement);
    }

    /// <inheritdoc/>
    public override void Write(Utf8JsonWriter writer, IReadOnlyDictionary<string, object?> value, JsonSerializerOptions options)
    {
        if (value is null)
        {
            throw new AccessTelemetryContractException("query_params_not_object");
        }

        writer.WriteStartObject();
        foreach ((string key, object? scalar) in value.OrderBy(static pair => pair.Key, StringComparer.Ordinal))
        {
            AccessTelemetryCanonicalizer.WriteScalar(writer, key, scalar);
        }

        writer.WriteEndObject();
    }
}
