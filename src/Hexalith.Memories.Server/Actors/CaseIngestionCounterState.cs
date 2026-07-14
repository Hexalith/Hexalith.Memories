// <copyright file="CaseIngestionCounterState.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Actors;

/// <summary>Persisted state for <see cref="CaseIngestionCounterActor"/>. <c>LastTransitionId</c> preserves
/// compatibility with existing actor state; <see cref="AppliedTransitionSequences"/> records a bounded
/// per-workflow high-water mark so delayed, non-adjacent workflow replay is also idempotent.</summary>
internal sealed record CaseIngestionCounterState(
    int Queued,
    int Extracting,
    int Embedding,
    int Indexing,
    string? LastTransitionId)
{
    /// <summary>Gets applied sequence high-water marks keyed by workflow instance id.</summary>
    public Dictionary<string, int>? AppliedTransitionSequences { get; init; }
}
