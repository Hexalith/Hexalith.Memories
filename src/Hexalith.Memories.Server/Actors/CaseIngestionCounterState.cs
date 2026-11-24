// <copyright file="CaseIngestionCounterState.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Actors;

using System.Text.Json.Serialization;

/// <summary>Persisted state for <see cref="CaseIngestionCounterActor"/>. <c>LastTransitionId</c> preserves
/// compatibility with existing actor state; <see cref="AppliedTransitionSequences"/> records a bounded
/// per-workflow high-water mark and <see cref="AppliedTransitionWorkflowOrder"/> records explicit
/// least-recently-updated order so delayed, non-adjacent workflow replay is also idempotent.</summary>
internal sealed record CaseIngestionCounterState(
    [property: JsonPropertyName("queued")] int Queued,
    [property: JsonPropertyName("extracting")] int Extracting,
    [property: JsonPropertyName("embedding")] int Embedding,
    [property: JsonPropertyName("indexing")] int Indexing,
    [property: JsonPropertyName("lastTransitionId")] string? LastTransitionId)
{
    /// <summary>Gets applied sequence high-water marks keyed by workflow instance id.</summary>
    [JsonPropertyName("appliedTransitionSequences")]
    public Dictionary<string, int>? AppliedTransitionSequences { get; init; }

    /// <summary>Gets tracked workflow ids from least to most recently updated.</summary>
    [JsonPropertyName("appliedTransitionWorkflowOrder")]
    public string[]? AppliedTransitionWorkflowOrder { get; init; }
}
