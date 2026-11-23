// <copyright file="ProjectionBindingMatcher.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Handlers;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

using Hexalith.Memories.EventStore;

/// <summary>Centralized comparison-key normalizer for route-to-projection binding coverage.</summary>
internal static class ProjectionBindingMatcher
{
    private static readonly Regex VersionSuffixRegex = new(
        pattern: @"V\d+$",
        options: RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase,
        matchTimeout: HandlerMismatchDetector.RegexMatchTimeout);

    public static IReadOnlyList<ProjectionBindingExpectation> BuildExpectations(
        string tenantId,
        IReadOnlyList<KeyValuePair<string, string>> routedEntries,
        IReadOnlyList<ObservedEventType> observedTypes)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        ArgumentNullException.ThrowIfNull(routedEntries);
        ArgumentNullException.ThrowIfNull(observedTypes);

        List<ProjectionBindingExpectation> expectations = [];
        foreach (KeyValuePair<string, string> route in routedEntries.OrderBy(static e => e.Key, StringComparer.OrdinalIgnoreCase))
        {
            string sourceKey = NormalizeSource(route.Key);
            string aggregateKey = GetRouteAggregateKey(route.Key);
            List<string> eventKeys = observedTypes
                .Where(o => string.Equals(NormalizeAggregateKey(o.AggregateType), aggregateKey, StringComparison.Ordinal))
                .Select(o => NormalizeEventName(o.EventType))
                .Where(static key => key.Length != 0)
                .Distinct(StringComparer.Ordinal)
                .OrderBy(static s => s, StringComparer.Ordinal)
                .ToList();

            foreach (string eventKey in eventKeys.DefaultIfEmpty("*"))
            {
                expectations.Add(new ProjectionBindingExpectation(
                    route.Key,
                    sourceKey,
                    aggregateKey,
                    eventKey,
                    IsUnobserved: eventKeys.Count == 0,
                    BuildComparisonKey(tenantId, sourceKey, eventKey)));
            }
        }

        return expectations
            .GroupBy(static e => e.ComparisonKey, StringComparer.Ordinal)
            .Select(static g => g.First())
            .ToList();
    }

    public static bool IsCovered(
        string tenantId,
        ProjectionBindingExpectation expectation,
        IReadOnlyList<ProjectionBinding> bindings)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        ArgumentNullException.ThrowIfNull(expectation);
        ArgumentNullException.ThrowIfNull(bindings);

        // Defensive: an adopter implementation may pass null entries inside the list even though
        // the contract declares non-nullable; a null entry must never silently fail the cross-check.
        return bindings.Any(binding => binding is not null && Covers(tenantId, expectation, binding));
    }

    private static bool Covers(string tenantId, ProjectionBindingExpectation expectation, ProjectionBinding binding)
    {
        if (!string.Equals(binding.TenantId?.Trim(), tenantId, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        string bindingSource = NormalizeSource(binding.SourcePrefix);
        string bindingAggregate = NormalizeToken(binding.AggregateType);
        bool routeMatches = !string.IsNullOrEmpty(bindingSource)
            && string.Equals(bindingSource, expectation.SourceKey, StringComparison.Ordinal);
        bool aggregateMatches = !string.IsNullOrEmpty(bindingAggregate)
            && string.Equals(bindingAggregate, expectation.AggregateKey, StringComparison.Ordinal);

        if (!routeMatches && !aggregateMatches)
        {
            return false;
        }

        // F8: an adopter may pass a null SupportedEventTypePatterns even though the contract declares
        // IReadOnlyList<string> as non-nullable; a null collection must not NRE the matcher and bubble
        // up into the detector's catch-all (which would silently swallow the cross-check).
        IReadOnlyList<string> patterns = binding.SupportedEventTypePatterns is null || binding.SupportedEventTypePatterns.Count == 0
            ? ["*"]
            : binding.SupportedEventTypePatterns;

        return patterns.Any(pattern => EventPatternCovers(pattern, expectation));
    }

    private static bool EventPatternCovers(string? pattern, ProjectionBindingExpectation expectation)
    {
        if (pattern is null)
        {
            return false;
        }

        string trimmed = pattern.Trim();
        if (pattern.Length == 0 || trimmed == "*")
        {
            return true;
        }

        bool wildcard = trimmed.EndsWith('*');
        string normalized = NormalizeEventName(wildcard ? trimmed[..^1] : trimmed);
        if (normalized.Length == 0)
        {
            return false;
        }

        return expectation.IsUnobserved || (wildcard
            ? expectation.EventKey.StartsWith(normalized, StringComparison.Ordinal)
            : string.Equals(normalized, expectation.EventKey, StringComparison.Ordinal));
    }

    private static string BuildComparisonKey(string tenantId, string sourceKey, string eventKey)
        => string.Concat(tenantId.Trim().ToLowerInvariant(), "/", sourceKey, "/", eventKey);

    /// <summary>Normalizes an aggregate name for route and observation comparisons.</summary>
    internal static string NormalizeAggregateKey(string? value) => NormalizeToken(value.AsSpan());

    /// <summary>Extracts the normalized terminal aggregate name from a route prefix.</summary>
    internal static string GetRouteAggregateKey(string sourcePrefix)
        => NormalizeToken(GetTerminalSegment(sourcePrefix));

    private static string NormalizeSource(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        // F5: treat '.' as equivalent to '/' so dot-style routes (`enterprise.claims`) and slash-style
        // bindings (`enterprise/claims`) canonicalize to the same source key. Without this, the matcher
        // depended on aggregate-fallback to OR-cover mismatched-notation routes, which silently passed
        // bindings registered against unrelated sources that happened to share an aggregate token.
        string normalized = value.Trim().Replace('\\', '/').Replace('.', '/').Trim('/');
        while (normalized.Contains("//", StringComparison.Ordinal))
        {
            normalized = normalized.Replace("//", "/", StringComparison.Ordinal);
        }

        return NormalizeToken(normalized);
    }

    private static string NormalizeEventName(string? value)
    {
        string terminal = NormalizeToken(GetTerminalSegment(value));
        Match suffix = VersionSuffixRegex.Match(terminal);
        return suffix.Success && suffix.Index > 0 ? terminal[..suffix.Index] : terminal;
    }

    private static string NormalizeToken(ReadOnlySpan<char> value)
    {
        string token = value.Trim().ToString();
        try
        {
            return token.Normalize(NormalizationForm.FormC).ToLowerInvariant();
        }
        catch (ArgumentException)
        {
            // A programmatic provider can supply invalid UTF-16. Match it literally.
            return token.ToLowerInvariant();
        }
    }

    private static ReadOnlySpan<char> GetTerminalSegment(string? value)
    {
        ReadOnlySpan<char> span = value.AsSpan().Trim().TrimEnd("./\\");
        return span[(span.LastIndexOfAny('.', '/', '\\') + 1)..];
    }
}
