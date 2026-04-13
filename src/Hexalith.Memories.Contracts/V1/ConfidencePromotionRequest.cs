// <copyright file="ConfidencePromotionRequest.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Contracts.V1;

/// <summary>Request to promote (update) the confidence of an existing edge in the knowledge graph (FR51).</summary>
/// <param name="SourceNodeId">The source node of the directed edge.</param>
/// <param name="TargetNodeId">The target node of the directed edge.</param>
/// <param name="EdgeType">The relationship type of the edge.</param>
/// <param name="NewConfidence">The new confidence value in the range [0.0, 1.0].</param>
/// <param name="VerifiedBy">The identity of the person verifying the relationship.</param>
public sealed record ConfidencePromotionRequest(
    string SourceNodeId,
    string TargetNodeId,
    EdgeType EdgeType,
    float NewConfidence,
    string VerifiedBy);
