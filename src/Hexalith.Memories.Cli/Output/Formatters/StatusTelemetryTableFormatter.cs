// <copyright file="StatusTelemetryTableFormatter.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Cli.Output.Formatters;

using System.Globalization;

using Hexalith.Memories.Contracts.V1;

/// <summary>Tabular rendering of <see cref="TelemetrySummary"/> for <c>status telemetry</c>.</summary>
public sealed class StatusTelemetryTableFormatter : IOutputFormatter<TelemetrySummary>
{
    /// <inheritdoc />
    public OutputFormat Format => OutputFormat.Table;

    /// <inheritdoc />
    public void Write(TelemetrySummary value, TextWriter writer)
    {
        ArgumentNullException.ThrowIfNull(value);
        ArgumentNullException.ThrowIfNull(writer);

        writer.WriteLine($"Tenant: {value.TenantId}");
        writer.WriteLine($"As of:  {value.AsOf}");
        writer.WriteLine();

        TableWriter.Write(
            writer,
            new[] { "AXIS", "SIZE", "HEALTH" },
            new IReadOnlyList<string>[]
            {
                new[] { "syntactic", FormatNullable(value.IndexSizes.Syntactic), value.IndexHealth.Syntactic.ToString() },
                new[] { "semantic", FormatNullable(value.IndexSizes.Semantic), value.IndexHealth.Semantic.ToString() },
                new[] { "graph", FormatNullable(value.IndexSizes.Graph), value.IndexHealth.Graph.ToString() },
            });

        writer.WriteLine();

        TableWriter.Write(
            writer,
            new[] { "AXIS", "REQUESTS (5M)", "ERRORS (5M)" },
            new IReadOnlyList<string>[]
            {
                CreateAxisRow("syntactic", value.SearchMetrics.Syntactic),
                CreateAxisRow("semantic", value.SearchMetrics.Semantic),
                CreateAxisRow("graph", value.SearchMetrics.Graph),
                CreateAxisRow("hybrid", value.SearchMetrics.Hybrid),
            });

        writer.WriteLine();

        TableWriter.Write(
            writer,
            new[] { "METRIC", "VALUE" },
            new IReadOnlyList<string>[]
            {
                new[] { "documentsLast5m", value.IngestionMetrics.DocumentsLast5m.ToString(CultureInfo.InvariantCulture) },
                new[] { "failuresLast5m", value.IngestionMetrics.FailuresLast5m.ToString(CultureInfo.InvariantCulture) },
                new[] { "queueDepth", value.IngestionMetrics.QueueDepth.ToString(CultureInfo.InvariantCulture) },
            });
    }

    private static IReadOnlyList<string> CreateAxisRow(string axis, TelemetryAxisCounters counters)
        => new[]
        {
            axis,
            counters.RequestsLast5m.ToString(CultureInfo.InvariantCulture),
            counters.ErrorsLast5m.ToString(CultureInfo.InvariantCulture),
        };

    private static string FormatNullable(long? value)
        => value is null ? "unknown" : value.Value.ToString(CultureInfo.InvariantCulture);
}