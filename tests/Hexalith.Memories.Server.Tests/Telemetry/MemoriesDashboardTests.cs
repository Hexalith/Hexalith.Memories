// <copyright file="MemoriesDashboardTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Tests.Telemetry;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;

using Hexalith.Memories.AccessTelemetry.Contracts;
using Hexalith.Memories.Telemetry;

using Shouldly;

/// <summary>Story 24.4 — contract tests for the committed Grafana dashboard.</summary>
public sealed class MemoriesDashboardTests
{
    private static readonly Regex MetricRegex = new(
        @"(?<![A-Za-z0-9_:])(?<metric>memories_[A-Za-z0-9_:]+)",
        RegexOptions.CultureInvariant | RegexOptions.Compiled);

    private static readonly Regex LabelMatcherRegex = new(
        @"(?<tag>[a-z_][a-z0-9_]*)\s*(?:=|!=|=~|!~)",
        RegexOptions.CultureInvariant | RegexOptions.Compiled);

    private static readonly Regex AggregateGroupRegex = new(
        @"\bby\s*\((?<tags>[^)]*)\)",
        RegexOptions.CultureInvariant | RegexOptions.Compiled);

    private static readonly Regex LabelValuesRegex = new(
        @"label_values\([^,]+,\s*(?<tag>[a-z_][a-z0-9_]*)\s*\)",
        RegexOptions.CultureInvariant | RegexOptions.Compiled);

    [Fact]
    public void DashboardJson_IsCommittedAndValid()
    {
        using JsonDocument document = LoadDashboard();

        document.RootElement.GetProperty("title").GetString().ShouldBe("Hexalith Memories Operability");
        document.RootElement.GetProperty("panels").GetArrayLength().ShouldBeGreaterThan(0);
    }

    [Fact]
    public void DashboardQueries_UseOnlyPolicyMetricsAndApprovedTags()
    {
        using JsonDocument document = LoadDashboard();
        IReadOnlyDictionary<string, string> metricLookup = BuildPrometheusMetricLookup();
        IReadOnlyDictionary<string, IReadOnlyList<string>> metricTagPolicy = BuildMetricTagPolicy();
        HashSet<string> fallbackAllowedTagKeys = BuildAllowedTagKeys();

        foreach (string query in ExtractDashboardQueries(document.RootElement))
        {
            HashSet<string> canonicalMetricNames = [];
            foreach (Match match in MetricRegex.Matches(query))
            {
                string prometheusMetricName = match.Groups["metric"].Value;
                metricLookup.ShouldContainKey(prometheusMetricName);
                canonicalMetricNames.Add(metricLookup[prometheusMetricName]);
            }

            foreach (string tagKey in ExtractTagKeys(query))
            {
                if (tagKey == "__name__")
                {
                    continue;
                }

                if (canonicalMetricNames.Count == 0)
                {
                    fallbackAllowedTagKeys.ShouldContain(tagKey);
                    continue;
                }

                foreach (string canonicalMetricName in canonicalMetricNames)
                {
                    HashSet<string> allowedTags = [.. metricTagPolicy[canonicalMetricName]];
                    if (IsHistogram(canonicalMetricName))
                    {
                        allowedTags.Add("le");
                    }

                    allowedTags.ShouldContain(
                        tagKey,
                        $"Query '{query}' uses tag '{tagKey}' with metric '{canonicalMetricName}', but that tag is not in MetricTagKeyPolicy.");
                }
            }
        }
    }

    [Fact]
    public void DashboardQueries_ReferenceEveryMetricInPolicy()
    {
        using JsonDocument document = LoadDashboard();
        HashSet<string> referencedMetrics = [.. ExtractDashboardQueries(document.RootElement)
            .SelectMany(q => MetricRegex.Matches(q).Select(m => m.Groups["metric"].Value))];

        foreach (string metricName in BuildMetricTagPolicy().Keys)
        {
            BuildPrometheusMetricNameVariants(metricName).Any(referencedMetrics.Contains).ShouldBeTrue(metricName);
        }
    }

    [Fact]
    public void DashboardQueries_DoNotUseForbiddenHighCardinalityTags()
    {
        using JsonDocument document = LoadDashboard();
        HashSet<string> forbidden = ["case_id", "user", "memory_unit_id"];

        foreach (string query in ExtractDashboardQueries(document.RootElement))
        {
            foreach (string tagKey in ExtractTagKeys(query))
            {
                forbidden.ShouldNotContain(tagKey);
            }
        }
    }

    [Fact]
    public void LifecyclePanels_ExposeNoDataAndUseTrueEvidenceTimestamp()
    {
        using JsonDocument document = LoadDashboard();
        JsonElement[] panels = document.RootElement.GetProperty("panels")
            .EnumerateArray()
            .Where(panel => panel.GetProperty("title").GetString()?.StartsWith("Access Telemetry", StringComparison.Ordinal) == true)
            .ToArray();

        panels.Length.ShouldBeGreaterThanOrEqualTo(8);
        panels.ShouldAllBe(panel =>
            panel.GetProperty("fieldConfig").GetProperty("defaults").GetProperty("noValue").GetString() == "Unhealthy / NoData");

        string[] queries = panels.SelectMany(ExtractDashboardQueries).ToArray();
        queries.ShouldContain("max(memories_access_telemetry_lifecycle_queue_records)");
        queries.ShouldContain("max(memories_access_telemetry_lifecycle_capacity_records)");
        queries.ShouldContain("time() - max(memories_access_telemetry_lifecycle_physical_evidence_last_timestamp_seconds)");
        queries.ShouldContain(
            "max by (state) (memories_access_telemetry_lifecycle_profile) or on() label_replace(vector(1), \"state\", \"no_data\", \"\", \"\")");
        queries.ShouldNotContain(query => query.Contains("state!=\"matched\"", StringComparison.Ordinal));
        queries.ShouldNotContain(query => query.Contains("time() - timestamp(", StringComparison.Ordinal));
    }

    private static JsonDocument LoadDashboard() =>
        JsonDocument.Parse(File.ReadAllText(FindDashboardPath()));

    private static string FindDashboardPath()
    {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);
        while (directory is not null)
        {
            string candidate = Path.Combine(directory.FullName, "deploy", "grafana", "dashboards", "memories-operability.json");
            if (File.Exists(candidate))
            {
                return candidate;
            }

            directory = directory.Parent;
        }

        throw new FileNotFoundException("Could not locate deploy/grafana/dashboards/memories-operability.json.");
    }

    private static IEnumerable<string> ExtractDashboardQueries(JsonElement element)
    {
        if (element.ValueKind == JsonValueKind.Object)
        {
            foreach (JsonProperty property in element.EnumerateObject())
            {
                if ((property.NameEquals("expr") || property.NameEquals("definition"))
                    && property.Value.ValueKind == JsonValueKind.String)
                {
                    yield return property.Value.GetString() ?? string.Empty;
                }

                foreach (string query in ExtractDashboardQueries(property.Value))
                {
                    yield return query;
                }
            }
        }
        else if (element.ValueKind == JsonValueKind.Array)
        {
            foreach (JsonElement child in element.EnumerateArray())
            {
                foreach (string query in ExtractDashboardQueries(child))
                {
                    yield return query;
                }
            }
        }
    }

    private static HashSet<string> BuildAllowedPrometheusMetricNames() =>
        [.. BuildMetricTagPolicy().Keys.SelectMany(BuildPrometheusMetricNameVariants)];

    private static IEnumerable<string> BuildPrometheusMetricNameVariants(string metricName)
    {
        string normalizedName = metricName.Replace('.', '_');
        string exportedName = AccessTelemetryMetricContract.MetricUnitSuffixes.TryGetValue(metricName, out string? unitSuffix)
            ? $"{normalizedName}_{unitSuffix}"
            : normalizedName;
        yield return exportedName;

        if (IsCounter(metricName))
        {
            yield return exportedName + "_total";
        }

        string? histogramUnit = GetHistogramUnit(metricName);
        if (histogramUnit is not null)
        {
            yield return $"{normalizedName}_{histogramUnit}_bucket";
            yield return $"{normalizedName}_{histogramUnit}_count";
            yield return $"{normalizedName}_{histogramUnit}_sum";
        }
    }

    private static IReadOnlyDictionary<string, string> BuildPrometheusMetricLookup()
    {
        Dictionary<string, string> lookup = new(StringComparer.Ordinal);
        foreach (string metricName in BuildMetricTagPolicy().Keys)
        {
            foreach (string prometheusMetricName in BuildPrometheusMetricNameVariants(metricName))
            {
                lookup[prometheusMetricName] = metricName;
            }
        }

        return lookup;
    }

    private static HashSet<string> BuildAllowedTagKeys()
    {
        HashSet<string> tagKeys = [.. BuildMetricTagPolicy().Values.SelectMany(v => v)];
        tagKeys.Add("le");
        return tagKeys;
    }

    private static IReadOnlyDictionary<string, IReadOnlyList<string>> BuildMetricTagPolicy()
    {
        var policy = new Dictionary<string, IReadOnlyList<string>>(MemoriesMeter.MetricTagKeyPolicy, StringComparer.Ordinal);
        foreach ((string metricName, IReadOnlyList<string> tags) in AccessTelemetryMetricContract.MetricTagKeyPolicy)
        {
            policy.Add(metricName, tags);
        }

        return policy;
    }

    private static string? GetHistogramUnit(string metricName)
    {
        if (HistogramMetricNames.Contains(metricName))
        {
            return "milliseconds";
        }

        return AccessTelemetryMetricContract.HistogramUnitSuffixes.TryGetValue(metricName, out string? unit)
            ? unit
            : null;
    }

    private static bool IsCounter(string metricName)
        => CounterMetricNames.Contains(metricName) || AccessTelemetryMetricContract.CounterMetricNames.Contains(metricName);

    private static bool IsHistogram(string metricName) => GetHistogramUnit(metricName) is not null;

    private static IEnumerable<string> ExtractTagKeys(string query)
    {
        foreach (Match match in LabelMatcherRegex.Matches(query))
        {
            yield return match.Groups["tag"].Value;
        }

        foreach (Match match in AggregateGroupRegex.Matches(query))
        {
            foreach (string tagKey in match.Groups["tags"].Value.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
            {
                yield return tagKey;
            }
        }

        foreach (Match match in LabelValuesRegex.Matches(query))
        {
            yield return match.Groups["tag"].Value;
        }
    }

    private static HashSet<string> CounterMetricNames { get; } =
    [
        MemoriesMeter.IngestionDocumentsName,
        MemoriesMeter.IngestionFailuresName,
        MemoriesMeter.SearchRequestsName,
        MemoriesMeter.RateLimitRejectionsName,
        MemoriesMeter.EmbeddingApiCallsName,
        MemoriesMeter.ConversationCacheHitName,
        MemoriesMeter.HandlerMismatchesName,
        MemoriesMeter.ObservationsDroppedName,
    ];

    private static HashSet<string> HistogramMetricNames { get; } =
    [
        MemoriesMeter.SearchDurationName,
        MemoriesMeter.NaturalLanguageDescriptionDurationName,
    ];
}
