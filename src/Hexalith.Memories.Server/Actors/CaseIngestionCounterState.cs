// <copyright file="CaseIngestionCounterState.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Actors;

/// <summary>Persisted state for <see cref="CaseIngestionCounterActor"/>. <c>LastTransitionId</c> records the
/// most-recent applied transitionId so workflow replay re-invocations are idempotent.</summary>
internal sealed record CaseIngestionCounterState(
    int Queued,
    int Extracting,
    int Embedding,
    int Indexing,
    string? LastTransitionId);
