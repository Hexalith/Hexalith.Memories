// <copyright file="EvidencePacketFreshness.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Contracts.V1;

using System.Text.Json.Serialization;

/// <summary>Freshness metadata for packet-level or source-level evidence.</summary>
/// <param name="State">Machine-readable freshness state.</param>
/// <param name="ProducedAt">Timestamp when the evidence was produced, when known.</param>
/// <param name="LastCheckedAt">Timestamp when freshness was last checked, when known.</param>
/// <param name="ExpiresAt">Timestamp after which the evidence should be considered expired, when known.</param>
/// <param name="AgeSeconds">Evidence age in seconds at composition time, when known.</param>
public sealed record EvidencePacketFreshness(
    EvidencePacketFreshnessState State,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] DateTimeOffset? ProducedAt = null,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] DateTimeOffset? LastCheckedAt = null,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] DateTimeOffset? ExpiresAt = null,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] long? AgeSeconds = null);
