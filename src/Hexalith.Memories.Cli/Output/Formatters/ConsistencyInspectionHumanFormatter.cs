// <copyright file="ConsistencyInspectionHumanFormatter.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Cli.Output.Formatters;

using Hexalith.Memories.Contracts.V1;

/// <summary>Plain-text rendering of <see cref="ConsistencyInspectionResult"/>.</summary>
public sealed class ConsistencyInspectionHumanFormatter : IOutputFormatter<ConsistencyInspectionResult>
{
    /// <inheritdoc />
    public OutputFormat Format => OutputFormat.Human;

    /// <inheritdoc />
    public void Write(ConsistencyInspectionResult value, TextWriter writer)
    {
        ArgumentNullException.ThrowIfNull(value);
        ArgumentNullException.ThrowIfNull(writer);

        writer.WriteLine($"Tenant:       {value.TenantId}");
        writer.WriteLine($"Memory unit:  {value.MemoryUnitId}");
        writer.WriteLine($"Checked at:   {value.CheckedAt:O}");
        writer.WriteLine($"Syntactic:    {FormatPresence(value.SyntacticPresent)}");
        writer.WriteLine($"Semantic:     {FormatPresence(value.SemanticPresent)}");
        writer.WriteLine($"Graph:        {FormatPresence(value.GraphPresent)}");
        writer.WriteLine($"Recommendation: {value.Recommendation}");

        if (value.SyntacticDetail is not null)
        {
            writer.WriteLine("Syntactic detail:");
            writer.WriteLine($"  contentHash:       {value.SyntacticDetail.ContentHash}");
            writer.WriteLine($"  ingestedAt:        {value.SyntacticDetail.IngestedAt:O}");
            writer.WriteLine($"  sourceUri:         {value.SyntacticDetail.SourceUri}");
            writer.WriteLine($"  sourceType:        {value.SyntacticDetail.SourceType}");
            writer.WriteLine($"  caseId:            {value.SyntacticDetail.CaseId}");
            writer.WriteLine($"  embeddingProvider: {value.SyntacticDetail.EmbeddingProvider}");
            writer.WriteLine($"  embeddingModel:    {value.SyntacticDetail.EmbeddingModel}");
        }

        if (value.SemanticDetail is not null)
        {
            writer.WriteLine("Semantic detail:");
            writer.WriteLine($"  embeddingDimensions: {value.SemanticDetail.EmbeddingDimensions}");
            writer.WriteLine($"  vectorHashKey:       {value.SemanticDetail.VectorHashKey}");
        }

        if (value.GraphDetail is not null)
        {
            writer.WriteLine("Graph detail:");
            writer.WriteLine($"  outgoingEdges: {value.GraphDetail.OutgoingEdgeCount}");
            writer.WriteLine($"  incomingEdges: {value.GraphDetail.IncomingEdgeCount}");
            writer.WriteLine($"  caseEdges:     {value.GraphDetail.CaseEdgeCount}");
        }
    }

    private static string FormatPresence(bool present) => present ? "present" : "absent";
}
