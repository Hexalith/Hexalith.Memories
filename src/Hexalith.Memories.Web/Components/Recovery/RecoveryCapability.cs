// <copyright file="RecoveryCapability.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Web.Components.Recovery;

/// <summary>
/// The capability affected by a recovery state. Surfaced as a localized label so the operator can see
/// what the state affects, never as the sole signal and never leaking restricted detail.
/// </summary>
public enum RecoveryCapability
{
    /// <summary>Tenant or case access and authorization.</summary>
    Access,

    /// <summary>Confidence that the presented answer is trustworthy.</summary>
    AnswerConfidence,

    /// <summary>Ingestion or indexing of source knowledge.</summary>
    Ingestion,

    /// <summary>Retrieval across backends and axes.</summary>
    Retrieval,

    /// <summary>Graph traversal context.</summary>
    GraphContext,

    /// <summary>Search matching within the authorized scope.</summary>
    Search,

    /// <summary>Freshness of the available evidence.</summary>
    Freshness,

    /// <summary>Strength of the supporting evidence.</summary>
    EvidenceStrength,

    /// <summary>Completeness of the returned detail.</summary>
    DetailCompleteness,

    /// <summary>Overall support for a confident answer.</summary>
    AnswerSupport,
}
