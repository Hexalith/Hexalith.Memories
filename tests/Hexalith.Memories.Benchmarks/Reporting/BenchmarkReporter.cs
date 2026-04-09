// <copyright file="BenchmarkReporter.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Benchmarks.Reporting;

using System.Globalization;
using System.Text;
using System.Text.Json;

using Hexalith.Memories.Benchmarks.Models;

/// <summary>
/// Produces benchmark output in JSON (machine-readable) and console (human-readable) formats.
/// </summary>
internal static class BenchmarkReporter
{
    private static readonly JsonSerializerOptions s_jsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    /// <summary>Serializes benchmark results to JSON and writes to the specified path.</summary>
    internal static void WriteResults(BenchmarkSuiteResult result, string outputPath)
    {
        string json = JsonSerializer.Serialize(result, s_jsonOptions);
        File.WriteAllText(outputPath, json);
    }

    /// <summary>Formats a human-readable console report of benchmark results.</summary>
    internal static string FormatConsoleReport(BenchmarkSuiteResult result)
    {
        StringBuilder sb = new();
        sb.AppendLine("╔══════════════════════════════════════════════════════════════════════════════════════════════════════╗");
        sb.AppendLine("║                           BENCHMARK SUITE — Three-Axis Thesis Validation                           ║");
        sb.AppendLine("╠══════════════════════════════════════════════════════════════════════════════════════════════════════╣");
        sb.AppendLine();

        // Header
        sb.AppendLine(string.Format(
            CultureInfo.InvariantCulture,
            "  {0,-8} {1,-45} {2,8} {3,8} {4,8} {5,8} {6,8} {7,10} {8,7}",
            "Query", "Description", "Hybrid", "Syntac", "Semant", "Graph", "H-P@3", "Best-P@3", "Winner"));
        sb.AppendLine(new string('-', 125));

        foreach (BenchmarkQueryResult qr in result.QueryResults)
        {
            string description = qr.QueryDescription.Length > 42
                ? string.Concat(qr.QueryDescription.AsSpan(0, 42), "...")
                : qr.QueryDescription;

            string graphStr = qr.GraphAxisActive
                ? qr.GraphNdcg10.ToString("F3", CultureInfo.InvariantCulture)
                : "  N/A";

            string winner = qr.HybridOutperforms ? "HYBRID" : "SINGLE";

            sb.AppendLine(string.Format(
                CultureInfo.InvariantCulture,
                "  {0,-8} {1,-45} {2,8:F3} {3,8:F3} {4,8:F3} {5,8} {6,8:F3} {7,10:F3} {8,7}",
                qr.QueryId,
                description,
                qr.HybridNdcg10,
                qr.SyntacticNdcg10,
                qr.SemanticNdcg10,
                graphStr,
                qr.HybridPrecisionAt3,
                qr.BestSingleAxisPrecisionAt3,
                winner));
        }

        sb.AppendLine();
        sb.AppendLine(new string('=', 125));

        string validationResult = result.ThesisValidated ? "PASSED" : "FAILED";
        sb.AppendLine(string.Format(
            CultureInfo.InvariantCulture,
            "  Thesis Validation: {0} ({1}/{2} queries, {3:P0} hybrid win rate)",
            validationResult,
            result.HybridWins,
            result.TotalQueries,
            result.HybridWinRate));

        sb.AppendLine();
        sb.AppendLine(string.Format(
            CultureInfo.InvariantCulture,
            "  Run timestamp: {0:O}",
            result.RunTimestamp));
        sb.AppendLine();
        sb.AppendLine($"  NOTE: {result.Caveat}");
        sb.AppendLine();

        return sb.ToString();
    }
}
