// <copyright file="StatusTelemetryHumanFormatter.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Cli.Output.Formatters;

using System.Globalization;

using Hexalith.Memories.Contracts.V1;

/// <summary>Plain-text rendering of <see cref="TelemetrySummary"/> for <c>memories status telemetry</c>.</summary>
public sealed class StatusTelemetryHumanFormatter : IOutputFormatter<TelemetrySummary>
{
    /// <inheritdoc />
    public OutputFormat Format => OutputFormat.Human;

    /// <inheritdoc />
    public void Write(TelemetrySummary value, TextWriter writer)
    {
        ArgumentNullException.ThrowIfNull(value);
        ArgumentNullException.ThrowIfNull(writer);

        writer.WriteLine($"Tenant: {value.TenantId}");
        writer.WriteLine($"As of:  {value.AsOf}");
        writer.WriteLine("Index sizes:");
        writer.WriteLine($"  syntactic: {FormatNullable(value.IndexSizes.Syntactic)} ({value.IndexHealth.Syntactic})");
        writer.WriteLine($"  semantic:  {FormatNullable(value.IndexSizes.Semantic)} ({value.IndexHealth.Semantic})");
        writer.WriteLine($"  graph:     {FormatNullable(value.IndexSizes.Graph)} ({value.IndexHealth.Graph})");
        writer.WriteLine("Search (last 5m):");
        WriteAxis(writer, "  syntactic", value.SearchMetrics.Syntactic);
        WriteAxis(writer, "  semantic ", value.SearchMetrics.Semantic);
        WriteAxis(writer, "  graph    ", value.SearchMetrics.Graph);
        WriteAxis(writer, "  hybrid   ", value.SearchMetrics.Hybrid);
        writer.WriteLine("Ingestion (last 5m):");
        writer.WriteLine($"  documents: {value.IngestionMetrics.DocumentsLast5m}");
        writer.WriteLine($"  failures:  {value.IngestionMetrics.FailuresLast5m}");
        writer.WriteLine($"  queue:     {value.IngestionMetrics.QueueDepth}");
    }

    private static void WriteAxis(TextWriter writer, string label, TelemetryAxisCounters axis)
        => writer.WriteLine($"{label} — requests: {axis.RequestsLast5m}, errors: {axis.ErrorsLast5m}");

    private static string FormatNullable(long? value)
        => value is null ? "unknown" : value.Value.ToString(CultureInfo.InvariantCulture);
}