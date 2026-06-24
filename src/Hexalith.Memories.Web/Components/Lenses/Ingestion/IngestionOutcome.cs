// <copyright file="IngestionOutcome.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Web.Components.Lenses.Ingestion;

/// <summary>
/// Ingestion outcome at the granularity the canonical Evidence Packet exposes.
/// </summary>
/// <remarks>
/// Story 17.4 — the canonical contract does not expose a fine-grained ingestion stage taxonomy (queued,
/// extracting, embedding, syntactic/vector/graph indexing, verifying, compensated, …). The tracker renders
/// the stage as an explicit unavailable boundary and reports only the outcomes the contract can support.
/// Unknown or future outcomes fail closed to <see cref="Unknown"/> rather than to empty success.
/// </remarks>
public enum IngestionOutcome
{
    /// <summary>The unit is indexed (Result.HasIndexedMemoryUnits and a returned source).</summary>
    Indexed = 0,

    /// <summary>Matching knowledge is not ingested or indexed yet (Result.HasIndexedMemoryUnits is false).</summary>
    NotIngestedYet,

    /// <summary>A retrieval backend or axis was degraded (Evidence.Degraded / UnavailableAxes).</summary>
    Degraded,

    /// <summary>A backend was unavailable (OmittedDetails.Reason is BackendUnavailable).</summary>
    BackendUnavailable,

    /// <summary>The scope is restrictive; unit detail is suppressed.</summary>
    Unauthorized,

    /// <summary>The contract cannot safely distinguish an outcome; render a safe unknown state.</summary>
    Unknown,
}
