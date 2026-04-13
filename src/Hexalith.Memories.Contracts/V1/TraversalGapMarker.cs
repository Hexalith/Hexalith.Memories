// <copyright file="TraversalGapMarker.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Contracts.V1;

/// <summary>Represents a missing node detected during causal chain traversal (FR49).</summary>
/// <param name="MissingNodeId">The identifier of the stub node that has no content.</param>
/// <param name="HopDistance">The distance from the traversal start node.</param>
/// <param name="Edges">The edges incident on the missing node, showing relationships to other nodes.</param>
public sealed record TraversalGapMarker(
    string MissingNodeId,
    int HopDistance,
    IReadOnlyList<TraversalEdgeInfo> Edges);
