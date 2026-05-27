// <copyright file="RepairPlanCalculator.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Consistency;

using Hexalith.Memories.Contracts.V1;

/// <summary>
/// Pure mapping from the three backend-presence booleans to a
/// <see cref="ConsistencyRepairRecommendation"/>. Used by both workflows AND the
/// inspection endpoint so the recommendation is consistent across every code path.
/// </summary>
/// <remarks>
/// Authoritative source is the syntactic <c>{tenantId}:mu:{id}</c> Redis hash
/// (it stores the full content + metadata). When syntactic is absent:
/// <list type="bullet">
///   <item>Non-authoritative backends holding data → orphans to delete.</item>
///   <item>Nothing anywhere → <see cref="ConsistencyRepairRecommendation.Unrepairable"/>.</item>
/// </list>
/// </remarks>
public static class RepairPlanCalculator
{
    /// <summary>
    /// Maps a presence triple to the corrective recommendation. Closed switch expression —
    /// every possible combination is covered; adding an enum value does not change the logic.
    /// </summary>
    /// <param name="syntactic">Whether the syntactic hash is present.</param>
    /// <param name="semantic">Whether the vector hash is present.</param>
    /// <param name="graph">Whether the graph node is present.</param>
    /// <returns>The repair recommendation.</returns>
    public static ConsistencyRepairRecommendation Calculate(bool syntactic, bool semantic, bool graph)
        => (syntactic, semantic, graph) switch
        {
            (true, true, true) => ConsistencyRepairRecommendation.NoOp,
            (true, false, true) => ConsistencyRepairRecommendation.ReIndexSemantic,
            (true, true, false) => ConsistencyRepairRecommendation.ReIndexGraph,
            (true, false, false) => ConsistencyRepairRecommendation.ReIndexSemanticAndGraph,
            (false, true, true) => ConsistencyRepairRecommendation.RemoveOrphanedSemanticAndGraph,
            (false, true, false) => ConsistencyRepairRecommendation.RemoveOrphanedSemantic,
            (false, false, true) => ConsistencyRepairRecommendation.RemoveOrphanedGraph,
            (false, false, false) => ConsistencyRepairRecommendation.Unrepairable,
        };
}
