// <copyright file="AccessTelemetryEvent.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Contracts.V1;

using System.Collections.Generic;
using System.Text.Json.Serialization;

/// <summary>
/// Story 7.5 — per-tenant audit event emitted for every search, ingest, traverse, case-access, or delete operation
/// (FR67). Serialized as a single JSON line via <c>[LoggerMessage]</c> structured destructuring.
/// Lives in <c>Contracts.V1</c> so Phase 1.5 consumers (MCP server, EventStore integration) can reference the
/// shape without pulling a Server dependency (ADR-7.5-001).
/// </summary>
/// <remarks>
/// Schema versioning: increment <see cref="SchemaVersion"/> ONLY for breaking schema changes.
/// Additive fields (new optional keys) stay at <c>1</c>. Matches the 7.2 envelope policy (ADR-7.2-001).
/// </remarks>
public sealed record AccessTelemetryEvent
{
    /// <summary>Current audit schema version. Bump only on breaking renames/removals.</summary>
    public const int CurrentSchemaVersion = 1;

    /// <summary>Gets the schema version for this event. Consumers branch on this value.</summary>
    [JsonPropertyName("schemaVersion")]
    public int SchemaVersion { get; init; } = CurrentSchemaVersion;

    /// <summary>Gets the <see cref="Microsoft.Extensions.Logging.EventId"/> bank 7500-7599 allocated to 7.5 audit events.</summary>
    [JsonPropertyName("eventId")]
    public required int EventId { get; init; }

    /// <summary>Gets the ISO 8601 UTC timestamp of event emission.</summary>
    [JsonPropertyName("timestamp")]
    public required string Timestamp { get; init; }

    /// <summary>Gets the tenant id this audit event applies to; <c>"__rejected__"</c> when the tenant guard rejected the request.</summary>
    [JsonPropertyName("tenantId")]
    public required string TenantId { get; init; }

    /// <summary>Gets the operation type: <c>search</c> | <c>ingest</c> | <c>traverse</c> | <c>case-access</c> | <c>delete</c>.</summary>
    [JsonPropertyName("operationType")]
    public required string OperationType { get; init; }

    /// <summary>Gets the case id involved (null when the operation is not case-scoped).</summary>
    [JsonPropertyName("caseId")]
    public string? CaseId { get; init; }

    /// <summary>Gets the user identity (per ADR-7.5-004 resolution rules; defaults to <c>"anonymous"</c>).</summary>
    [JsonPropertyName("user")]
    public required string User { get; init; }

    /// <summary>Gets the operation-specific parameter dictionary. MUST NOT contain raw tokens, authorization headers, or memory-unit content.</summary>
    [JsonPropertyName("queryParams")]
    public IReadOnlyDictionary<string, object?> QueryParams { get; init; } = new Dictionary<string, object?>(0);

    /// <summary>Gets the number of results returned on a read operation (null for write/schedule operations).</summary>
    [JsonPropertyName("resultCount")]
    public int? ResultCount { get; init; }

    /// <summary>Gets the elapsed duration in milliseconds.</summary>
    [JsonPropertyName("durationMs")]
    public required long DurationMs { get; init; }

    /// <summary>Gets the outcome: <c>ok</c> | <c>partial</c> | <c>error</c>.</summary>
    [JsonPropertyName("outcome")]
    public required string Outcome { get; init; }

    /// <summary>Gets the error code (catalog or synthetic) when outcome is error/partial; null on success.</summary>
    [JsonPropertyName("errorCode")]
    public string? ErrorCode { get; init; }

    /// <summary>Gets the W3C trace id (copied from <c>Activity.Current</c>).</summary>
    [JsonPropertyName("traceId")]
    public string? TraceId { get; init; }

    /// <summary>Gets the W3C span id (copied from <c>Activity.Current</c>).</summary>
    [JsonPropertyName("spanId")]
    public string? SpanId { get; init; }
}
