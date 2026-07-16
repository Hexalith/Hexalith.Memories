// <copyright file="SearchResultTableFormatter.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Cli.Output.Formatters;

using System.Globalization;

using Hexalith.Memories.Contracts.V1;

/// <summary>Tabular rendering of a single-axis <see cref="SearchResult"/>.</summary>
public sealed class SearchResultTableFormatter : IOutputFormatter<SearchResult>
{
    /// <inheritdoc />
    public OutputFormat Format => OutputFormat.Table;

    /// <inheritdoc />
    public void Write(SearchResult value, TextWriter writer)
    {
        ArgumentNullException.ThrowIfNull(value);
        ArgumentNullException.ThrowIfNull(writer);

        IReadOnlyList<string> headers = new[] { "RANK", "SCORE", "URI", "SNIPPET" };
        var rows = new List<IReadOnlyList<string>>(value.Results.Count);
        int rank = 1;
        foreach (ScoredResult r in value.Results)
        {
            rows.Add(new[]
            {
                rank.ToString(CultureInfo.InvariantCulture),
                r.Score.ToString("F3", CultureInfo.InvariantCulture),
                r.SourceUri,
                SnippetTruncator.Truncate(r.ContentSnippet),
            });
            rank++;
        }

        TableWriter.Write(writer, headers, rows);

        if (value.Explanation is { } explanation)
        {
            writer.WriteLine(explanation.Caveat);
        }
    }
}
