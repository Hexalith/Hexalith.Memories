// <copyright file="ConfidencePromotionResult.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Contracts.V1;

/// <summary>Result of a confidence promotion operation, including audit fields for traceability.</summary>
/// <param name="SourceNodeId">The source node of the promoted edge.</param>
/// <param name="TargetNodeId">The target node of the promoted edge.</param>
/// <param name="EdgeType">The relationship type of the promoted edge.</param>
/// <param name="PreviousConfidence">The confidence value before promotion (audit trail).</param>
/// <param name="NewConfidence">The updated confidence value.</param>
/// <param name="VerifiedBy">The identity of the person who verified the relationship.</param>
public sealed record ConfidencePromotionResult(
    string SourceNodeId,
    string TargetNodeId,
    EdgeType EdgeType,
    float PreviousConfidence,
    float NewConfidence,
    string VerifiedBy);
