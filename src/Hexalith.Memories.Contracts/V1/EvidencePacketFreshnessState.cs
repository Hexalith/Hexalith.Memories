// <copyright file="EvidencePacketFreshnessState.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Contracts.V1;

using System.Text.Json.Serialization;

/// <summary>Machine-readable freshness state for Evidence Packet metadata.</summary>
[JsonConverter(typeof(CamelCaseStringEnumConverter<EvidencePacketFreshnessState>))]
public enum EvidencePacketFreshnessState
{
    /// <summary>Freshness is unknown.</summary>
    Unknown = 0,

    /// <summary>The evidence is current for the producer's freshness window.</summary>
    Current,

    /// <summary>The evidence may be stale.</summary>
    Stale,

    /// <summary>The evidence is known to be expired.</summary>
    Expired,

    /// <summary>The producer is still checking freshness.</summary>
    Pending,
}
