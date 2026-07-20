// <copyright file="AccessTelemetryExpiryBucket.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.AccessTelemetry.Lifecycle;

using System.Text.Json.Serialization;

/// <summary>Explicit Dapr-state minute/shard bucket used without the optional Query API.</summary>
internal sealed record AccessTelemetryExpiryBucket(
    [property: JsonPropertyName("expiryMinute")] long ExpiryMinute,
    [property: JsonPropertyName("shard")] int Shard,
    [property: JsonPropertyName("entries")] IReadOnlyList<AccessTelemetryExpiryEntry> Entries);
