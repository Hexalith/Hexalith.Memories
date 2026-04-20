// <copyright file="ConsistencyInspectionTableFormatter.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Cli.Output.Formatters;

using Hexalith.Memories.Contracts.V1;

/// <summary>
/// Table rendering of <see cref="ConsistencyInspectionResult"/> — compact one-row summary
/// with the three presence booleans plus the recommendation.
/// </summary>
public sealed class ConsistencyInspectionTableFormatter : IOutputFormatter<ConsistencyInspectionResult>
{
    /// <inheritdoc />
    public OutputFormat Format => OutputFormat.Table;

    /// <inheritdoc />
    public void Write(ConsistencyInspectionResult value, TextWriter writer)
    {
        ArgumentNullException.ThrowIfNull(value);
        ArgumentNullException.ThrowIfNull(writer);

        string[] headers = ["memoryUnit", "syntactic", "semantic", "graph", "recommendation"];
        string[] row =
        [
            value.MemoryUnitId,
            value.SyntacticPresent ? "✓" : "✗",
            value.SemanticPresent ? "✓" : "✗",
            value.GraphPresent ? "✓" : "✗",
            value.Recommendation.ToString(),
        ];

        TableWriter.Write(writer, headers, [row]);
    }
}
