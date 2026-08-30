// <copyright file="InstrumentationInventoryTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Tests.Telemetry;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

using Hexalith.Memories.ServiceDefaults;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using NSubstitute;

using OpenTelemetry;
using OpenTelemetry.Trace;

using Shouldly;

using StackExchange.Redis;

/// <summary>
/// Story 8.5 Task 4.3 — code ↔ doc parity check for the Instrumentation Inventory table in
/// <c>docs/dev/telemetry.md</c>. Parses the table on every build and asserts each listed
/// <see cref="ActivitySource"/> name is subscribed by the <see cref="TracerProvider"/> built by
/// <see cref="Extensions.AddServiceDefaults"/>. The next instrumentation gap fails this Tier-2
/// test instead of surfacing months later as a Tier-3 assertion.
/// </summary>
[Trait("Category", "Unit")]
[Collection(Infrastructure.TelemetryTestCollection.Name)]
public sealed class InstrumentationInventoryTests
{
    private const string TelemetryDocRelativePath = "docs/dev/telemetry.md";
    private const string InventorySectionHeader = "## Instrumentation Inventory";

    [Fact]
    public void InventoryTable_MatchesRegisteredActivitySources()
    {
        // Parse the inventory table from telemetry.md.
        string telemetryDocPath = LocateTelemetryDoc();
        string markdown = File.ReadAllText(telemetryDocPath);
        IReadOnlyList<string> documentedSourceNames = ParseInventorySourceNames(markdown);

        documentedSourceNames.Count.ShouldBeGreaterThan(
            0,
            $"Instrumentation Inventory table not found or empty in {telemetryDocPath}. " +
            "Section header expected: \"{InventorySectionHeader}\".");

        // Build the TracerProvider used in production.
        HostApplicationBuilder builder = Host.CreateEmptyApplicationBuilder(new HostApplicationBuilderSettings
        {
            Args = [],
            EnvironmentName = "Development",
        });
        builder.Services.AddKeyedSingleton<IConnectionMultiplexer>(
            Extensions.RedisConnectionKey,
            (_, _) => Substitute.For<IConnectionMultiplexer>());
        builder.Services.AddKeyedSingleton<IConnectionMultiplexer>(
            Extensions.FalkorDbConnectionKey,
            (_, _) => Substitute.For<IConnectionMultiplexer>());
        builder.AddServiceDefaults();

        using ServiceProvider provider = builder.Services.BuildServiceProvider();
        _ = provider.GetRequiredService<TracerProvider>();

        // For each documented ActivitySource, assert it is subscribed by the built TracerProvider.
        // The application-name-derived source is documented as "<Environment.ApplicationName>" —
        // substitute the actual value when probing.
        string applicationName = builder.Environment.ApplicationName;
        List<string> unresolved = [];
        foreach (string documented in documentedSourceNames)
        {
            string resolved = documented == "<Environment.ApplicationName>"
                ? applicationName
                : documented;

            using var src = new ActivitySource(resolved);
            using Activity? activity = src.StartActivity($"inventory-parity-probe:{resolved}");
            if (activity is null || !activity.IsAllDataRequested)
            {
                unresolved.Add(resolved);
            }
        }

        unresolved.ShouldBeEmpty(
            "Instrumentation Inventory table in docs/dev/telemetry.md lists ActivitySource names " +
            "that are NOT subscribed by the TracerProvider built via AddServiceDefaults. " +
            "Either register them in ConfigureOpenTelemetry or remove them from the inventory table. " +
            $"Unresolved: {string.Join(", ", unresolved)}");
    }

    [Fact]
    public void LocateTelemetryDoc_UsesSolutionRootInsteadOfNearerDecoy()
    {
        DirectoryInfo root = Directory.CreateTempSubdirectory("memories-telemetry-root-");
        try
        {
            File.WriteAllText(Path.Combine(root.FullName, "Hexalith.Memories.slnx"), string.Empty);
            string expected = Path.Combine(root.FullName, TelemetryDocRelativePath);
            Directory.CreateDirectory(Path.GetDirectoryName(expected)!);
            File.WriteAllText(expected, "authoritative");

            string startDirectory = Path.Combine(root.FullName, "tests", "bin");
            Directory.CreateDirectory(startDirectory);
            string decoy = Path.Combine(root.FullName, "tests", TelemetryDocRelativePath);
            Directory.CreateDirectory(Path.GetDirectoryName(decoy)!);
            File.WriteAllText(decoy, "decoy");

            LocateTelemetryDoc(startDirectory).ShouldBe(expected);
        }
        finally
        {
            root.Delete(recursive: true);
        }
    }

    [Fact]
    public void LocateTelemetryDoc_ReportsWhetherSolutionOrDocumentIsMissing()
    {
        DirectoryInfo root = Directory.CreateTempSubdirectory("memories-telemetry-missing-");
        try
        {
            string startDirectory = Path.Combine(root.FullName, "tests", "bin");
            Directory.CreateDirectory(startDirectory);

            FileNotFoundException missingSolution = Should.Throw<FileNotFoundException>(
                () => LocateTelemetryDoc(startDirectory));
            missingSolution.Message.ShouldContain("Could not locate solution marker");
            missingSolution.Message.ShouldContain(startDirectory);

            string solutionPath = Path.Combine(root.FullName, "Hexalith.Memories.slnx");
            File.WriteAllText(solutionPath, string.Empty);

            FileNotFoundException missingDocument = Should.Throw<FileNotFoundException>(
                () => LocateTelemetryDoc(startDirectory));
            missingDocument.Message.ShouldContain($"Located solution marker '{solutionPath}'");
            missingDocument.Message.ShouldContain(startDirectory);
        }
        finally
        {
            root.Delete(recursive: true);
        }
    }

    private static string LocateTelemetryDoc()
        => LocateTelemetryDoc(AppContext.BaseDirectory);

    private static string LocateTelemetryDoc(string startDirectory)
    {
        // Walk up from the test assembly location until we find the repo root (marked by
        // Hexalith.Memories.slnx), then resolve docs/dev/telemetry.md.
        string? current = Path.GetFullPath(startDirectory);
        while (current is not null)
        {
            string marker = Path.Combine(current, "Hexalith.Memories.slnx");
            if (File.Exists(marker))
            {
                string candidate = Path.Combine(current, TelemetryDocRelativePath);
                if (File.Exists(candidate))
                {
                    return candidate;
                }

                throw new FileNotFoundException(
                    $"Located solution marker '{marker}' but could not locate {TelemetryDocRelativePath} " +
                    $"starting from {startDirectory}.",
                    candidate);
            }

            current = Directory.GetParent(current)?.FullName;
        }

        throw new FileNotFoundException(
            $"Could not locate solution marker 'Hexalith.Memories.slnx' starting from {startDirectory}. " +
            $"The InstrumentationInventoryTests require that marker before reading {TelemetryDocRelativePath}.");
    }

    /// <summary>
    /// Parses the "ActivitySource.Name" column from the Instrumentation Inventory markdown table.
    /// Each listed row yields the source name stripped of surrounding backticks / whitespace /
    /// parenthetical annotations (e.g. "<c>Hexalith.Memories</c> (...)" → "Hexalith.Memories").
    /// </summary>
    private static IReadOnlyList<string> ParseInventorySourceNames(string markdown)
    {
        int sectionIndex = markdown.IndexOf(InventorySectionHeader, StringComparison.Ordinal);
        if (sectionIndex < 0)
        {
            return [];
        }

        // Bound the scan to the end of the section (next `## ` header or end of file).
        int nextSectionIndex = markdown.IndexOf("\n## ", sectionIndex + InventorySectionHeader.Length, StringComparison.Ordinal);
        string sectionText = nextSectionIndex > 0
            ? markdown[sectionIndex..nextSectionIndex]
            : markdown[sectionIndex..];

        List<string> sourceNames = [];
        foreach (string rawLine in sectionText.Split('\n'))
        {
            string line = rawLine.TrimEnd('\r');
            if (!line.StartsWith('|'))
            {
                continue;
            }

            // Skip the header + separator rows.
            if (line.Contains("ActivitySource.Name", StringComparison.Ordinal)
                || line.Contains("| :---", StringComparison.Ordinal))
            {
                continue;
            }

            string[] cells = line.Split('|', StringSplitOptions.None);
            if (cells.Length < 2)
            {
                continue;
            }

            // cells[0] is the blank before the leading `|`; cells[1] is the first column.
            string firstCell = cells[1].Trim();
            if (string.IsNullOrWhiteSpace(firstCell))
            {
                continue;
            }

            string sourceName = ExtractBacktickedName(firstCell) ?? firstCell;
            if (!string.IsNullOrWhiteSpace(sourceName))
            {
                sourceNames.Add(sourceName);
            }
        }

        return sourceNames;
    }

    /// <summary>Returns the contents of the first backticked run in <paramref name="cell"/>.</summary>
    private static string? ExtractBacktickedName(string cell)
    {
        Match match = Regex.Match(cell, "`([^`]+)`");
        return match.Success ? match.Groups[1].Value : null;
    }
}
