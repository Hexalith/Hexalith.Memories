// <copyright file="SearchResultHumanFormatter.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Cli.Output.Formatters;

using System.Globalization;

using Hexalith.Memories.Contracts.V1;

/// <summary>Renders a single-axis <see cref="SearchResult"/> as plain text with optional explain block.</summary>
public sealed class SearchResultHumanFormatter : IOutputFormatter<SearchResult>
{
    /// <inheritdoc />
    public OutputFormat Format => OutputFormat.Human;

    /// <inheritdoc />
    public void Write(SearchResult value, TextWriter writer)
    {
        ArgumentNullException.ThrowIfNull(value);
        ArgumentNullException.ThrowIfNull(writer);

        if (value.Explanation is { } explanation)
        {
            writer.WriteLine(explanation.Caveat);
        }

        int rank = 1;
        foreach (ScoredResult result in value.Results)
        {
            string score = result.Score.ToString("F3", CultureInfo.InvariantCulture);
            string snippet = SnippetTruncator.Truncate(result.ContentSnippet);
            writer.WriteLine($"{rank}. [{score}] {result.SourceUri} — {snippet}");
            rank++;
        }

        if (value.Explanation is { } exp)
        {
            foreach (KeyValuePair<string, AxisExplanation> axis in exp.AxisDetails)
            {
                writer.WriteLine($"    ({axis.Key}: {axis.Value.NormalizationMethod})");
            }
        }
    }
}
