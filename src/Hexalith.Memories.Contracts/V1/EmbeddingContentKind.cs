// <copyright file="EmbeddingContentKind.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Contracts.V1;

using System.Text.Json.Serialization;

/// <summary>Story 9.2 Task 3.2 — classifies an embedding request by what kind of content is being
/// embedded. Used to tag telemetry (activity span + per-call counter) so operators can observe the
/// raw-payload / NL-description 2:1 call split under dual-embedding (Risk #6).</summary>
[JsonConverter(typeof(CamelCaseStringEnumConverter<EmbeddingContentKind>))]
public enum EmbeddingContentKind
{
    /// <summary>The embedding is computed from the extracted text of an ingestion payload — the default
    /// for <c>SourceType.File</c>, <c>SourceType.Url</c>, and the raw-payload axis of
    /// <c>SourceType.Event</c>. Wire-compat default when older workflow histories are replayed under
    /// 9.2+ code.</summary>
    Payload,

    /// <summary>The embedding is computed from an LLM-authored natural-language description of an event
    /// payload (Story 9.2 FR60 dual-embedding pipeline, NL axis).</summary>
    NaturalLanguageDescription,
}
