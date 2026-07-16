// <copyright file="ProjectionBindingMatcher.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Handlers;

using System;
using System.Collections.Generic;
using System.Linq;
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
            string aggregateKey = NormalizeToken(ExtractAggregateFromSourcePrefix(route.Key));
            List<string> eventKeys = observedTypes
                .Where(o => string.Equals(NormalizeToken(o.AggregateType), aggregateKey, StringComparison.Ordinal))
                .Select(o => NormalizeEventPattern(o.EventType))
                .DefaultIfEmpty("*")
                .Distinct(StringComparer.Ordinal)
                .OrderBy(static s => s, StringComparer.Ordinal)
                .ToList();

            foreach (string eventKey in eventKeys)
            {
                expectations.Add(new ProjectionBindingExpectation(
                    route.Key,
                    sourceKey,
                    aggregateKey,
                    eventKey,
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

        return patterns.Any(pattern => EventPatternCovers(pattern, expectation.EventKey));
    }

    private static bool EventPatternCovers(string pattern, string eventKey)
    {
        string normalized = NormalizeEventPattern(pattern);
        if (normalized == "*" || eventKey == "*")
        {
            return true;
        }

        if (normalized.EndsWith('*'))
        {
            return eventKey.StartsWith(normalized[..^1], StringComparison.Ordinal);
        }

        return string.Equals(normalized, eventKey, StringComparison.Ordinal);
    }

    private static string BuildComparisonKey(string tenantId, string sourceKey, string eventKey)
        => string.Concat(NormalizeToken(tenantId), "/", sourceKey, "/", eventKey);

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

        return normalized.ToLowerInvariant();
    }

    private static string NormalizeEventPattern(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return "*";
        }

        string trimmed = value.Trim();
        if (trimmed == "*")
        {
            return "*";
        }

        bool wildcard = trimmed.EndsWith('*');
        if (wildcard)
        {
            trimmed = trimmed[..^1];
        }

        ReadOnlySpan<char> span = trimmed.AsSpan();
        int lastDot = span.LastIndexOf('.');
        if (lastDot >= 0 && lastDot < span.Length - 1)
        {
            span = span[(lastDot + 1)..];
        }

        string terminal = VersionSuffixRegex.Replace(span.ToString(), string.Empty);
        string normalized = NormalizeToken(terminal);
        return wildcard ? string.Concat(normalized, "*") : normalized;
    }

    private static string NormalizeToken(string? value)
        => string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim().ToLowerInvariant();

    private static string ExtractAggregateFromSourcePrefix(string sourcePrefix)
    {
        if (string.IsNullOrEmpty(sourcePrefix))
        {
            return string.Empty;
        }

        ReadOnlySpan<char> span = sourcePrefix.AsSpan().Trim('/');
        int lastSlash = span.LastIndexOf('/');
        if (lastSlash >= 0 && lastSlash < span.Length - 1)
        {
            span = span[(lastSlash + 1)..];
        }

        int lastDot = span.LastIndexOf('.');
        if (lastDot >= 0 && lastDot < span.Length - 1)
        {
            span = span[(lastDot + 1)..];
        }

        return span.ToString();
    }
}

internal sealed record ProjectionBindingExpectation(
    string SourcePrefix,
    string SourceKey,
    string AggregateKey,
    string EventKey,
    string ComparisonKey);
