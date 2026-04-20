// <copyright file="ConsistencyRepairRecommendation.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Contracts.V1;

using System.Text.Json.Serialization;

/// <summary>
/// Repair-plan classification for a single memory unit across the three backends
/// (syntactic RediSearch, semantic Redis Vector, graph FalkorDB). Emitted by
/// <c>RepairPlanCalculator</c> based on the presence booleans returned by
/// <c>VerifyConsistencyActivity</c>. Drives the action dispatched by
/// <c>RepairUnitActivity</c>.
/// </summary>
[JsonConverter(typeof(CamelCaseStringEnumConverter<ConsistencyRepairRecommendation>))]
public enum ConsistencyRepairRecommendation
{
    /// <summary>Unit is consistent across all three backends; no action required.</summary>
    NoOp,

    /// <summary>Syntactic + graph present; semantic missing. Re-create the vector entry.</summary>
    ReIndexSemantic,

    /// <summary>Syntactic + semantic present; graph missing. Re-merge the graph node.</summary>
    ReIndexGraph,

    /// <summary>Only syntactic present. Re-create both vector entry and graph node.</summary>
    ReIndexSemanticAndGraph,

    /// <summary>Syntactic missing; semantic present. Delete the orphaned vector entry.</summary>
    RemoveOrphanedSemantic,

    /// <summary>Syntactic missing; graph present. Delete the orphaned graph node.</summary>
    RemoveOrphanedGraph,

    /// <summary>Syntactic missing; semantic + graph present. Delete both orphans.</summary>
    RemoveOrphanedSemanticAndGraph,

    /// <summary>
    /// Nothing present anywhere (bookkeeping mismatch) or syntactic gone without enough
    /// information to rebuild. Flagged for manual intervention.
    /// </summary>
    Unrepairable,
}
