// <copyright file="ConsistencyNoteKind.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Contracts.V1;

using System.Text.Json.Serialization;

/// <summary>Story 9.2 Review D7 — canonical identifier for <see cref="ConsistencyInspectionResult"/> /
/// <see cref="ConsistencyDiscrepancy"/> informational notes. Downstream consumers filter/pattern-match
/// on the enum member rather than parsing the accompanying human-readable string.</summary>
[JsonConverter(typeof(CamelCaseStringEnumConverter<ConsistencyNoteKind>))]
public enum ConsistencyNoteKind
{
    /// <summary>No informational note applies.</summary>
    None = 0,

    /// <summary>Natural-language semantic hash is pending a queued retry because the DAPR Conversation
    /// API was unavailable at ingest time. Degraded but VALID state — the memory unit is searchable on
    /// the raw semantic axis until the retry hosted service completes.</summary>
    NaturalLanguageEmbeddingQueued = 1,

    /// <summary>Memory unit reports <c>NaturalLanguageEmbeddingStatus = Indexed</c> but no NL semantic
    /// hash exists. REAL inconsistency that consistency-verification must flag for repair.</summary>
    NaturalLanguageEmbeddingMissing = 2,
}
