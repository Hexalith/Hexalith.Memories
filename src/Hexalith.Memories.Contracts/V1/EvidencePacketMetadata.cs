// <copyright file="EvidencePacketMetadata.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Contracts.V1;

using System.Text.Json.Serialization;

/// <summary>Optional metadata that lets Evidence Packet consumers render cross-surface diagnostics.</summary>
/// <param name="Freshness">Packet-level freshness and last-checked metadata, when known.</param>
/// <param name="Benchmark">Benchmark evidence linked to this packet, when known.</param>
/// <param name="McpSchema">MCP tool/schema metadata linked to this packet, when known.</param>
public sealed record EvidencePacketMetadata(
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] EvidencePacketFreshness? Freshness = null,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] EvidencePacketBenchmarkEvidence? Benchmark = null,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] EvidencePacketMcpSchema? McpSchema = null);
