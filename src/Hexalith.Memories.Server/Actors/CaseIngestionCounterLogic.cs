// <copyright file="CaseIngestionCounterLogic.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Actors;

using Hexalith.Memories.Contracts.V1;

/// <summary>Pure transition logic for the case-ingestion counter, mirroring the <see cref="RateLimiterLogic"/>
/// precedent — testable without DAPR plumbing.</summary>
internal sealed class CaseIngestionCounterLogic
{
    /// <summary>Applies a stage transition to the supplied state. Returns the same instance unchanged when
    /// the transitionId has already been applied (idempotent).</summary>
    /// <param name="state">The current actor state.</param>
    /// <param name="previous">The previous-stage bucket (or <c>"none"</c>).</param>
    /// <param name="next">The next-stage bucket (or <c>"none"</c>).</param>
    /// <param name="transitionId">The deterministic transition id from the workflow.</param>
    /// <returns>The new state, or the original instance on idempotent replay.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="previous"/> or <paramref name="next"/> is unknown.</exception>
    public CaseIngestionCounterState Transition(
        CaseIngestionCounterState state,
        string previous,
        string next,
        string transitionId)
    {
        ArgumentNullException.ThrowIfNull(state);
        if (string.Equals(state.LastTransitionId, transitionId, StringComparison.Ordinal))
        {
            return state;
        }

        int q = state.Queued;
        int e = state.Extracting;
        int m = state.Embedding;
        int i = state.Indexing;

        switch (previous)
        {
            case "queued": q = Math.Max(0, q - 1); break;
            case "extracting": e = Math.Max(0, e - 1); break;
            case "embedding": m = Math.Max(0, m - 1); break;
            case "indexing": i = Math.Max(0, i - 1); break;
            case "none": break;
            default: throw new ArgumentException($"Invalid previousStage '{previous}'", nameof(previous));
        }

        switch (next)
        {
            case "queued": q++; break;
            case "extracting": e++; break;
            case "embedding": m++; break;
            case "indexing": i++; break;
            case "none": break;
            default: throw new ArgumentException($"Invalid nextStage '{next}'", nameof(next));
        }

        return new CaseIngestionCounterState(q, e, m, i, transitionId);
    }

    /// <summary>Projects the actor state to the public counts contract.</summary>
    /// <param name="s">The actor state.</param>
    /// <returns>A <see cref="CaseIngestionCounts"/>.</returns>
    public CaseIngestionCounts ToCounts(CaseIngestionCounterState s)
    {
        ArgumentNullException.ThrowIfNull(s);
        return new(s.Queued, s.Extracting, s.Embedding, s.Indexing);
    }
}
