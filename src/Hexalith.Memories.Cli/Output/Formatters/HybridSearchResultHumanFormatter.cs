// <copyright file="HybridSearchResultHumanFormatter.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Cli.Output.Formatters;

using System.Globalization;
using System.Text;

using Hexalith.Memories.Contracts.V1;

/// <summary>
/// Renders a <see cref="HybridSearchResult"/> as plain text. With <c>--explain</c>, prints the caveat
/// FIRST so <c>memories search query --explain | head -N</c> still carries the compliance guarantee
/// (Task 6.3). Degradation handling moved to <c>SearchQueryCommand</c> per Story 7.3 Task 5.1 —
/// formatter is now degradation-agnostic.
/// </summary>
public sealed class HybridSearchResultHumanFormatter : IOutputFormatter<HybridSearchResult>
{
    /// <inheritdoc />
    public OutputFormat Format => OutputFormat.Human;

    /// <inheritdoc />
    public void Write(HybridSearchResult value, TextWriter writer)
    {
        ArgumentNullException.ThrowIfNull(value);
        ArgumentNullException.ThrowIfNull(writer);

        if (value.Explanation is { } explanation)
        {
            writer.WriteLine(explanation.Caveat);
        }

        int rank = 1;
        foreach (FusedScoredResult result in value.Results)
        {
            string composite = result.CompositeScore.ToString("F3", CultureInfo.InvariantCulture);
            string snippet = SnippetTruncator.Truncate(result.ContentSnippet);
            writer.WriteLine($"{rank}. [{composite}] {result.SourceUri} — {snippet}");

            if (value.Explanation is not null)
            {
                WriteExplainBlock(writer, result, value.Explanation);
            }

            rank++;
        }
    }

    private static void WriteExplainBlock(TextWriter writer, FusedScoredResult result, SearchExplanation explanation)
    {
        var builder = new StringBuilder("    composite=");
        builder.Append(result.CompositeScore.ToString("F3", CultureInfo.InvariantCulture));

        if (result.SyntacticScore is double syntactic)
        {
            builder.Append(", syntactic=").Append(syntactic.ToString("F3", CultureInfo.InvariantCulture));
        }

        if (result.SemanticScore is double semantic)
        {
            builder.Append(", semantic=").Append(semantic.ToString("F3", CultureInfo.InvariantCulture));
        }

        if (result.GraphScore is double graph)
        {
            builder.Append(", graph=").Append(graph.ToString("F3", CultureInfo.InvariantCulture));
        }

        writer.WriteLine(builder.ToString());

        foreach (KeyValuePair<string, AxisExplanation> axis in explanation.AxisDetails)
        {
            writer.WriteLine($"      ({axis.Key}: {axis.Value.NormalizationMethod})");
        }
    }
}
