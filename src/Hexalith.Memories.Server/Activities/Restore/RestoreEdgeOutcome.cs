// <copyright file="RestoreEdgeOutcome.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Activities.Restore;

/// <summary>The outcome of restoring a single exported edge.</summary>
internal enum RestoreEdgeOutcome
{
    /// <summary>The edge was MERGEd into the graph.</summary>
    Restored,

    /// <summary>Skipped by design (a CONTAINS edge rebuilt from caseId, or an unrecognized edge type) — not data loss.</summary>
    SkippedByDesign,

    /// <summary>Skipped because the edge was invalid/corrupt (best-effort restore) — reported via the skipped count.</summary>
    SkippedInvalid,
}
