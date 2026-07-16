// <copyright file="ICaseIngestionCounterActor.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Actors;

using Dapr.Actors;

using Hexalith.Memories.Contracts.V1;

/// <summary>DAPR Actor interface tracking in-flight ingestion counts per (tenantId, caseId) (Story 6.3 FR10).
/// Actor ID is <c>"{tenantId}:{caseId}"</c>. Stage values: <c>"none"</c>, <c>"queued"</c>, <c>"extracting"</c>,
/// <c>"embedding"</c>, <c>"indexing"</c>. <c>"indexed"</c> and <c>"failed"</c> are NOT actor states — they
/// source from FalkorDB and the activity stream respectively.</summary>
public interface ICaseIngestionCounterActor : IActor
{
    /// <summary>Decrements the previous-stage bucket and increments the next-stage bucket atomically.</summary>
    /// <param name="previousStage">The bucket to decrement (or <c>"none"</c> for increment-only).</param>
    /// <param name="nextStage">The bucket to increment (or <c>"none"</c> for decrement-only).</param>
    /// <param name="transitionId">A unique <c>{instanceId}:{sequence}</c> id; replays with the same id are no-ops.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task TransitionAsync(string previousStage, string nextStage, string transitionId);

    /// <summary>Returns the current in-flight counts.</summary>
    /// <returns>A <see cref="CaseIngestionCounts"/> snapshot.</returns>
    Task<CaseIngestionCounts> GetCountsAsync();

    /// <summary>Resets all counts to zero (used on case delete).</summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task ResetAsync();
}
