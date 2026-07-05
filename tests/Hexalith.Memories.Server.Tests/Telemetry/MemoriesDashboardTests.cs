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
                    HashSet<string> allowedTags = [.. MemoriesMeter.MetricTagKeyPolicy[canonicalMetricName]];
                    if (HistogramMetricNames.Contains(canonicalMetricName))
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

        foreach (string metricName in MemoriesMeter.MetricTagKeyPolicy.Keys)
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
        [.. MemoriesMeter.MetricTagKeyPolicy.Keys.SelectMany(BuildPrometheusMetricNameVariants)];

    private static IEnumerable<string> BuildPrometheusMetricNameVariants(string metricName)
    {
        string normalizedName = metricName.Replace('.', '_');
        yield return normalizedName;

        if (CounterMetricNames.Contains(metricName))
        {
            yield return normalizedName + "_total";
        }

        if (HistogramMetricNames.Contains(metricName))
        {
            yield return normalizedName + "_milliseconds_bucket";
            yield return normalizedName + "_milliseconds_count";
            yield return normalizedName + "_milliseconds_sum";
        }
    }

    private static IReadOnlyDictionary<string, string> BuildPrometheusMetricLookup()
    {
        Dictionary<string, string> lookup = new(StringComparer.Ordinal);
        foreach (string metricName in MemoriesMeter.MetricTagKeyPolicy.Keys)
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
        HashSet<string> tagKeys = [.. MemoriesMeter.MetricTagKeyPolicy.Values.SelectMany(v => v)];
        tagKeys.Add("le");
        return tagKeys;
    }

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
