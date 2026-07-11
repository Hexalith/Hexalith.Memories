// <copyright file="ConsistencyGraphDetail.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Contracts.V1;

/// <summary>
/// Details from the graph-axis memory-unit node. Edge counts surface the unit's
/// graph connectivity for operator diagnosis.
/// </summary>
/// <param name="OutgoingEdgeCount">Number of edges where this unit is the source.</param>
/// <param name="IncomingEdgeCount">Number of edges where this unit is the target.</param>
/// <param name="CaseEdgeCount">Number of <c>CONTAINS</c> edges from case nodes to this unit.</param>
public sealed record ConsistencyGraphDetail(
    int OutgoingEdgeCount,
    int IncomingEdgeCount,
    int CaseEdgeCount);
