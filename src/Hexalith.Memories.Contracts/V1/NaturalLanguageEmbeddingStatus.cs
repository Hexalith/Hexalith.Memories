// <copyright file="NaturalLanguageEmbeddingStatus.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Contracts.V1;

using System.Text.Json.Serialization;

/// <summary>Story 9.2 Task 5.2 — classifies the outcome of the natural-language embedding side-path for
/// a given memory unit. Surfaced on <see cref="IngestionResult"/> so callers and telemetry can observe
/// the healthy-vs-degraded split without inspecting Redis directly.</summary>
[JsonConverter(typeof(CamelCaseStringEnumConverter<NaturalLanguageEmbeddingStatus>))]
public enum NaturalLanguageEmbeddingStatus
{
    /// <summary>The memory unit is not a <see cref="SourceType.Event"/> — no NL description was attempted.
    /// This is also the default value so pre-9.2 serialized <see cref="IngestionResult"/> payloads replay
    /// safely.</summary>
    NotApplicable = 0,

    /// <summary>The NL embedding was generated and indexed in <c>{tenant}:memories:vec:nl</c>
    /// successfully — business-meaning search is available for the unit.</summary>
    Indexed = 1,

    /// <summary>The LLM was unavailable at ingestion time; the raw embedding was indexed and the NL
    /// embedding was queued via <c>FailedNaturalLanguageEmbeddingRegistry</c> for background retry. The
    /// unit is <c>MemoryUnitStatus.Indexed</c> and searchable on the three non-NL axes.</summary>
    Queued = 2,
}
