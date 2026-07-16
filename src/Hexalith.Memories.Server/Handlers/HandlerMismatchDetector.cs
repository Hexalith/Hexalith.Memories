// <copyright file="HandlerMismatchDetector.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Handlers;

using System;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

using Hexalith.Memories.Contracts.V1;
using Hexalith.Memories.EventStore;
using Hexalith.Memories.Telemetry;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

/// <summary>Story 9.3 — pure read-side detector that analyses the observation store for three mismatch
/// categories: <see cref="HandlerMismatchCategory.UnhandledEventType"/>,
/// <see cref="HandlerMismatchCategory.StaleHandler"/>, and
/// <see cref="HandlerMismatchCategory.VersionMismatch"/>. Story 16.1 adds an authoritative
/// projection-binding cross-check without changing the existing categories' semantics.</summary>
public sealed class HandlerMismatchDetector
{
    /// <summary>Hard cap on event-type string length before the regex engages — Risk #5 defense-in-depth
    /// (CloudEvents spec bounds <c>type</c> at 253 chars; anything over 256 is already suspect).</summary>
    internal const int MaxEventTypeLength = 256;

    /// <summary>Regex timeout — linear-worst-case pattern but the timeout is defense-in-depth.</summary>
    internal static readonly TimeSpan RegexMatchTimeout = TimeSpan.FromMilliseconds(100);

    private static readonly Regex VersionStemRegex = new(
        pattern: @"^(.+?)(V\d+)$",
        options: RegexOptions.Compiled | RegexOptions.CultureInvariant,
        matchTimeout: RegexMatchTimeout);

    private readonly IOptionsMonitor<TenantEventRoutingOptions> _routingOptions;
    private readonly IObservedEventTypeStore _observedEventTypeStore;
    private readonly IProjectionBindingProvider _projectionBindingProvider;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<HandlerMismatchDetector> _logger;

    public HandlerMismatchDetector(
        IOptionsMonitor<TenantEventRoutingOptions> routingOptions,
        IObservedEventTypeStore observedEventTypeStore,
        IProjectionBindingProvider projectionBindingProvider,
        TimeProvider timeProvider,
        ILogger<HandlerMismatchDetector> logger)
    {
        ArgumentNullException.ThrowIfNull(routingOptions);
        ArgumentNullException.ThrowIfNull(observedEventTypeStore);
        ArgumentNullException.ThrowIfNull(projectionBindingProvider);
        ArgumentNullException.ThrowIfNull(timeProvider);
        ArgumentNullException.ThrowIfNull(logger);

        _routingOptions = routingOptions;
        _observedEventTypeStore = observedEventTypeStore;
        _projectionBindingProvider = projectionBindingProvider;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    /// <summary>Detects mismatches within the given tenant's observation window.</summary>
    /// <param name="tenantId">The tenant to analyse.</param>
    /// <param name="window">The observation window width (24h for 9.3).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task<HandlerMismatchReport> DetectAsync(
        string tenantId,
        TimeSpan window,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        cancellationToken.ThrowIfCancellationRequested();

        TenantEventRoutingOptions options = _routingOptions.CurrentValue;
        int windowHours = (int)Math.Round(window.TotalHours, MidpointRounding.AwayFromZero);

        IReadOnlyList<ObservedEventType> observedTypes = await _observedEventTypeStore
            .GetAllObservedTypesAsync(tenantId, window, cancellationToken)
            .ConfigureAwait(false);

        List<KeyValuePair<string, string>> routedEntries = options.SourceToTenantMap
            .Where(kvp => string.Equals(kvp.Value, tenantId, StringComparison.Ordinal))
            .ToList();

        List<HandlerMismatch> mismatches = new();

        // StaleHandler — canonical phrasing: observedTypes.Count == 0.
        if (observedTypes.Count == 0)
        {
            foreach (KeyValuePair<string, string> entry in routedEntries)
            {
                mismatches.Add(BuildStaleHandlerMismatch(entry.Key, options.Topic, windowHours));
            }
        }

        // UnhandledEventType — observed aggregates that don't map to any routed source-prefix's naming.
        HashSet<string> routedPrefixTokens = new(
            routedEntries.Select(e => ExtractAggregateFromSourcePrefix(e.Key)),
            StringComparer.OrdinalIgnoreCase);

        foreach (ObservedEventType o in observedTypes)
        {
            if (!routedPrefixTokens.Contains(o.AggregateType))
            {
                mismatches.Add(BuildUnhandledMismatch(o, tenantId, windowHours, options.Topic));
            }
        }

        // VersionMismatch — group by stem on terminal segment.
        mismatches.AddRange(DetectVersionMismatches(observedTypes, windowHours));

        mismatches.AddRange(await DetectProjectionBindingMismatchesAsync(
            tenantId,
            routedEntries,
            observedTypes,
            cancellationToken).ConfigureAwait(false));

        // Telemetry + logs per mismatch.
        foreach (HandlerMismatch m in mismatches)
        {
            MemoriesMeter.HandlerMismatches.Add(
                1,
                new KeyValuePair<string, object?>("tenant_id", tenantId),
                new KeyValuePair<string, object?>("severity", m.Severity.ToString().ToLowerInvariant()));
            EventStoreIntegrationLog.HandlerMismatchDetected(
                _logger, tenantId, m.Category.ToString(), m.Severity.ToString(), m.Subject);
        }

        return new HandlerMismatchReport
        {
            TenantId = tenantId,
            AsOf = _timeProvider.GetUtcNow().ToString("O"),
            WindowHours = windowHours,
            Mismatches = mismatches,
            Summary = new HandlerMismatchReportSummary
            {
                RoutesConfigured = routedEntries.Count,
                ObservationsChecked = observedTypes.Count,
            },
        };
    }

    private async Task<IReadOnlyList<HandlerMismatch>> DetectProjectionBindingMismatchesAsync(
        string tenantId,
        IReadOnlyList<KeyValuePair<string, string>> routedEntries,
        IReadOnlyList<ObservedEventType> observedTypes,
        CancellationToken cancellationToken)
    {
        if (routedEntries.Count == 0)
        {
            return Array.Empty<HandlerMismatch>();
        }

        ProjectionBindingSnapshot snapshot;
        try
        {
            snapshot = await _projectionBindingProvider.GetBindingsAsync(tenantId, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (Exception) when (cancellationToken.IsCancellationRequested)
        {
            // Any exception type during cancellation re-throws so callers see cancellation, never a silent empty result.
            throw;
        }
        catch (Exception ex)
        {
            EventStoreIntegrationLog.ProjectionBindingProviderFailed(_logger, tenantId, ex.GetType().FullName ?? ex.GetType().Name);
            return Array.Empty<HandlerMismatch>();
        }

        if (snapshot.Authority != ProjectionBindingRegistryAuthority.Authoritative)
        {
            return Array.Empty<HandlerMismatch>();
        }

        // Tenant mismatch on an Authoritative snapshot suggests an adopter bug — never silent.
        if (!string.Equals(snapshot.TenantId?.Trim(), tenantId, StringComparison.OrdinalIgnoreCase))
        {
            EventStoreIntegrationLog.ProjectionBindingSnapshotTenantMismatched(
                _logger,
                tenantId,
                snapshot.TenantId ?? "<null>");
            return Array.Empty<HandlerMismatch>();
        }

        // An Authoritative snapshot that returns null Bindings is a contract violation; downgrade to Unavailable behavior and log.
        if (snapshot.Bindings is null)
        {
            EventStoreIntegrationLog.ProjectionBindingSnapshotNullBindings(_logger, tenantId);
            return Array.Empty<HandlerMismatch>();
        }

        return ProjectionBindingMatcher
            .BuildExpectations(tenantId, routedEntries, observedTypes)
            .Where(expectation => !ProjectionBindingMatcher.IsCovered(tenantId, expectation, snapshot.Bindings))
            .Select(expectation => BuildProjectionBindingMissingMismatch(tenantId, expectation))
            .ToList();
    }

    private List<HandlerMismatch> DetectVersionMismatches(
        IReadOnlyList<ObservedEventType> observedTypes,
        int windowHours)
    {
        Dictionary<(string AggregateType, string Stem), List<(string Version, string FullType, long Count)>> stems = new();

        foreach (ObservedEventType o in observedTypes)
        {
            // Risk #5 — length cap BEFORE regex.
            if (string.IsNullOrEmpty(o.EventType) || o.EventType.Length > MaxEventTypeLength)
            {
                if (o.EventType?.Length > MaxEventTypeLength)
                {
                    EventStoreIntegrationLog.RegexSkippedForPathologicalEventType(
                        _logger,
                        reason: "length_exceeded",
                        truncatedEventType: Truncate(o.EventType, 128));
                }

                continue;
            }

            string terminalSegment = o.EventType.Split('.').Last();
            if (!TryMatchVersionStem(o.EventType, terminalSegment, out string? stem, out string? version))
            {
                continue;
            }

            (string AggregateType, string Stem) key = (o.AggregateType, stem!);
            if (!stems.TryGetValue(key, out List<(string, string, long)>? bucket))
            {
                bucket = new List<(string, string, long)>();
                stems[key] = bucket;
            }

            bucket.Add((version!, o.EventType, o.Count));
        }

        List<HandlerMismatch> results = new();
        foreach (KeyValuePair<(string AggregateType, string Stem), List<(string Version, string FullType, long Count)>> kvp in stems)
        {
            IEnumerable<IGrouping<string, (string Version, string FullType, long Count)>> byVersion =
                kvp.Value.GroupBy(t => t.Version, StringComparer.Ordinal);
            List<(string Version, long Count)> versionTotals = byVersion
                .Select(g => (Version: g.Key, Count: g.Sum(x => x.Count)))
                .Where(v => v.Count > 0)
                .OrderBy(v => v.Version, StringComparer.Ordinal)
                .ToList();

            if (versionTotals.Count < 2)
            {
                continue;
            }

            string versionsWithCounts = string.Join(
                ", ",
                versionTotals.Select(v => string.Create(CultureInfo.InvariantCulture, $"{v.Version} ({v.Count} events)")));
            long total = versionTotals.Sum(v => v.Count);
            string versionsPlain = string.Join(", ", versionTotals.Select(v => v.Version));

            results.Add(new HandlerMismatch
            {
                Category = HandlerMismatchCategory.VersionMismatch,
                Severity = HandlerMismatchSeverity.Warning,
                Subject = kvp.Key.Stem,
                Context = string.Create(
                    CultureInfo.InvariantCulture,
                    $"Aggregate '{kvp.Key.AggregateType}' stem '{kvp.Key.Stem}' observed with {versionTotals.Count} concurrent versions in the last {windowHours}h: {versionsWithCounts}. Total events across versions: {total}."),
                Suggestion = string.Create(
                    CultureInfo.InvariantCulture,
                    $"Multiple versions of '{kvp.Key.Stem}' observed for aggregate '{kvp.Key.AggregateType}' ({versionsPlain}) — review whether all versions are intentional, or whether a publisher is emitting an old version. See: https://docs.hexalith.dev/memories/runbooks/handler-version-mismatch."),
            });
        }

        return results;
    }

    private bool TryMatchVersionStem(
        string fullEventType,
        string terminalSegment,
        out string? stem,
        out string? version)
    {
        stem = null;
        version = null;

        if (string.IsNullOrWhiteSpace(terminalSegment) || terminalSegment.Length > MaxEventTypeLength)
        {
            return false;
        }

        try
        {
            Match match = VersionStemRegex.Match(terminalSegment);
            if (!match.Success)
            {
                return false;
            }

            stem = match.Groups[1].Value;
            version = match.Groups[2].Value;

            if (string.IsNullOrEmpty(stem))
            {
                return false;
            }

            return true;
        }
        catch (RegexMatchTimeoutException)
        {
            EventStoreIntegrationLog.RegexSkippedForPathologicalEventType(
                _logger,
                reason: "timeout",
                truncatedEventType: Truncate(fullEventType, 128));
            return false;
        }
        catch (ArgumentException)
        {
            EventStoreIntegrationLog.RegexSkippedForPathologicalEventType(
                _logger,
                reason: "invalid_input",
                truncatedEventType: Truncate(fullEventType, 128));
            return false;
        }
    }

    private static HandlerMismatch BuildStaleHandlerMismatch(
        string sourcePrefix,
        string topic,
        int windowHours) => new()
        {
            Category = HandlerMismatchCategory.StaleHandler,
            Severity = HandlerMismatchSeverity.Info,
            Subject = sourcePrefix,
            Context = string.Create(
            CultureInfo.InvariantCulture,
            $"SourceToTenantMap entry for '{sourcePrefix}' has received zero events in the last {windowHours}h."),
            Suggestion = string.Create(
            CultureInfo.InvariantCulture,
            $"Handler registered for source '{sourcePrefix}' but no events received in the last {windowHours}h — verify the publisher is online and targeting topic '{topic}'. Low-volume publishers may legitimately go silent; set up a publisher-side heartbeat event if certainty matters. See: https://docs.hexalith.dev/memories/runbooks/handler-stale-handler."),
        };

    private static HandlerMismatch BuildUnhandledMismatch(
        ObservedEventType observed,
        string tenantId,
        int windowHours,
        string topic) => new()
        {
            Category = HandlerMismatchCategory.UnhandledEventType,
            Severity = HandlerMismatchSeverity.Warning,
            Subject = string.Create(CultureInfo.InvariantCulture, $"{observed.AggregateType}/{observed.EventType}"),
            Context = string.Create(
            CultureInfo.InvariantCulture,
            $"Observed {observed.Count} event(s) of type '{observed.EventType}' on aggregate '{observed.AggregateType}' in the last {windowHours}h. No SourceToTenantMap entry routes this aggregate to tenant '{tenantId}'. Most recent observation: {observed.LastSeenAt:O}."),
            Suggestion = string.Create(
            CultureInfo.InvariantCulture,
            $"Add an EventStoreIntegration:Routing:SourceToTenantMap entry for source starting with '{observed.AggregateType}' OR verify publisher is targeting the configured topic '{topic}'. See: https://docs.hexalith.dev/memories/runbooks/handler-unhandled-event-type."),
        };

    private static HandlerMismatch BuildProjectionBindingMissingMismatch(
        string tenantId,
        ProjectionBindingExpectation expectation) => new()
        {
            Category = HandlerMismatchCategory.ProjectionBindingMissing,
            Severity = HandlerMismatchSeverity.Warning,
            Subject = expectation.ComparisonKey,
            Context = string.Create(
                CultureInfo.InvariantCulture,
                $"SourceToTenantMap configured source '{expectation.SourcePrefix}' for tenant '{tenantId}', but the authoritative projection registry returned no binding for expected projection binding key '{expectation.ComparisonKey}'."),
            Suggestion = string.Create(
                CultureInfo.InvariantCulture,
                $"Register an authoritative projection binding for source '{expectation.SourcePrefix}' and event key '{expectation.EventKey}', or update EventStoreIntegration:Routing:SourceToTenantMap so it matches a runtime-bound projection. This check proves declared binding coverage only, not projection liveness or lag. See: https://docs.hexalith.dev/memories/runbooks/handler-projection-binding-missing."),
        };

    private static string ExtractAggregateFromSourcePrefix(string sourcePrefix)
    {
        // Conservative heuristic: take the last '.'-separated token of the prefix, OR the last '/'-separated
        // token if no dot. Documented in docs/dev/eventstore-integration.md §11.2.
        if (string.IsNullOrEmpty(sourcePrefix))
        {
            return string.Empty;
        }

        ReadOnlySpan<char> span = sourcePrefix.AsSpan();
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

    private static string Truncate(string value, int max)
        => value.Length <= max ? value : value[..max];
}
