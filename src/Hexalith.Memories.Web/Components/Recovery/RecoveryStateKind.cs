// <copyright file="RecoveryStateKind.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Web.Components.Recovery;

/// <summary>
/// Presentation-only recovery state derived from the canonical Story 2.7 Evidence Packet.
/// </summary>
/// <remarks>
/// Story 17.2 — this is a web presentation mapping over <c>Contracts.V1</c> Evidence Packet fields,
/// not a parallel taxonomy. Each value is produced only from named contract fields by
/// <see cref="RecoveryStateMapper"/>; when the contract cannot safely distinguish a cause the mapper
/// falls back to <see cref="InsufficientEvidence"/> or <see cref="Unknown"/> rather than guessing.
/// </remarks>
public enum RecoveryStateKind
{
    /// <summary>The packet is a confident, complete answer that needs no recovery action.</summary>
    Supported,

    /// <summary>An answer exists but the evidence strength is weak.</summary>
    Weak,

    /// <summary>Available evidence may be old or superseded.</summary>
    StaleMemory,

    /// <summary>One or more retrieval axes or backends were unavailable.</summary>
    DegradedBackend,

    /// <summary>
    /// Authorization or scope prevents access. Side-channel safe: this collapses forbidden scope and
    /// inaccessible tenant/case so the UI never reveals whether matching evidence exists.
    /// </summary>
    Unauthorized,

    /// <summary>Details were omitted under a response/token budget and can be expanded.</summary>
    Compressed,

    /// <summary>Sources, backend health, or retrieval axes disagree; the answer must not look confident.</summary>
    Conflicting,

    /// <summary>The search completed in the authorized scope but found no supported candidate.</summary>
    NoMatch,

    /// <summary>Matching knowledge may still be pending ingestion or indexing.</summary>
    NotIngestedYet,

    /// <summary>
    /// The query likely belongs to another selected case. Represented for completeness, but the
    /// contract exposes no side-channel-safe signal for it, so the mapper never emits this value.
    /// </summary>
    WrongCase,

    /// <summary>Graph context is incomplete or missing causal links.</summary>
    GraphGap,

    /// <summary>The system cannot support a confident answer from the available data.</summary>
    InsufficientEvidence,

    /// <summary>The contract cannot safely distinguish a cause; render a safe unknown state.</summary>
    Unknown,
}
