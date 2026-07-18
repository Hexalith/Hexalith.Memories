// <copyright file="AccessTelemetryExpiryEntry.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.AccessTelemetry.Lifecycle;

using System.Text.Json.Serialization;

/// <summary>Portable minute/shard expiry-index entry.</summary>
internal sealed record AccessTelemetryExpiryEntry(
    [property: JsonPropertyName("recordId")] string RecordId,
    [property: JsonPropertyName("expiryMinute")] long ExpiryMinute,
    [property: JsonPropertyName("shard")] int Shard,
    [property: JsonPropertyName("envelopeHash")] string EnvelopeHash,
    [property: JsonPropertyName("expiresAtUtc")] string ExpiresAtUtc);
