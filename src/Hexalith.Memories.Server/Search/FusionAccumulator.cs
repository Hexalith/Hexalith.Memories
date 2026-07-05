// <copyright file="FusionAccumulator.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Search;

using Hexalith.Memories.Contracts.V1;

/// <summary>Mutable accumulator used during fusion to collect per-axis scores and attribution for one memory unit.</summary>
internal sealed class FusionAccumulator
{
    public double? SyntacticScore;
    public double? SemanticScore;
    public double? GraphScore;
    public string? CaseId;
    public string? CaseName;
    public int AnnotationsCount;
    public required string ContentSnippet;
    public required string SourceUri;
    public required SourceType SourceType;
}
