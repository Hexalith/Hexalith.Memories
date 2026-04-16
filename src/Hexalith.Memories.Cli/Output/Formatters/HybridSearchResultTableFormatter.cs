// <copyright file="HybridSearchResultTableFormatter.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Cli.Output.Formatters;

using System.Globalization;

using Hexalith.Memories.Contracts.V1;

/// <summary>
/// Tabular rendering of <see cref="HybridSearchResult"/>. The caveat is printed AFTER the table (Task 6.5)
/// so the interactive header-to-data alignment stays intact. The degraded-axes notice still prints BEFORE
/// the header per Task 6.6a — header is the second line of output when <see cref="HybridSearchResult.Degraded"/>
/// is <see langword="true"/>.
/// </summary>
public sealed class HybridSearchResultTableFormatter : IOutputFormatter<HybridSearchResult>
{
    /// <inheritdoc />
    public OutputFormat Format => OutputFormat.Table;

    /// <inheritdoc />
    public void Write(HybridSearchResult value, TextWriter writer)
    {
        ArgumentNullException.ThrowIfNull(value);
        ArgumentNullException.ThrowIfNull(writer);

        if (value.Degraded)
        {
            IEnumerable<string> axes = value.UnavailableAxes ?? Array.Empty<string>();
            writer.WriteLine(
                $"Note: search degraded — axes unavailable: {string.Join(", ", axes)}");
        }

        bool withExplain = value.Explanation is not null;
        IReadOnlyList<string> headers = withExplain
            ? new[] { "RANK", "COMPOSITE", "SYNTACTIC", "SEMANTIC", "GRAPH", "URI" }
            : new[] { "RANK", "SCORE", "URI", "SNIPPET" };

        var rows = new List<IReadOnlyList<string>>(value.Results.Count);
        int rank = 1;
        foreach (FusedScoredResult r in value.Results)
        {
            if (withExplain)
            {
                rows.Add(new[]
                {
                    rank.ToString(CultureInfo.InvariantCulture),
                    r.CompositeScore.ToString("F3", CultureInfo.InvariantCulture),
                    FormatScore(r.SyntacticScore),
                    FormatScore(r.SemanticScore),
                    FormatScore(r.GraphScore),
                    r.SourceUri,
                });
            }
            else
            {
                rows.Add(new[]
                {
                    rank.ToString(CultureInfo.InvariantCulture),
                    r.CompositeScore.ToString("F3", CultureInfo.InvariantCulture),
                    r.SourceUri,
                    SnippetTruncator.Truncate(r.ContentSnippet),
                });
            }

            rank++;
        }

        TableWriter.Write(writer, headers, rows);

        if (value.Explanation is { } explanation)
        {
            writer.WriteLine(explanation.Caveat);
        }
    }

    private static string FormatScore(double? score)
        => score is double s
            ? s.ToString("F3", CultureInfo.InvariantCulture)
            : "-";
}
