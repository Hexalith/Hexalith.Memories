// <copyright file="EvidencePacketMapper.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Contracts.V1;

using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

/// <summary>Pure mapper from lower-level retrieval and diagnostic contracts to the canonical evidence packet.</summary>
public static partial class EvidencePacketMapper
{
    private const string DefaultCaveat = "Scores measure query-result relevance, not factual accuracy or data completeness.";
    private const int MaxDisplayQueryLength = 1024;

    /// <summary>Creates the canonical unavailable packet used while evidence is loading or cannot be retrieved.</summary>
    /// <param name="tenantId">The requested tenant identifier.</param>
    /// <param name="caseId">The requested case identifier, or <see langword="null"/> for tenant scope.</param>
    /// <param name="isError">Whether retrieval failed rather than still being pending.</param>
    /// <returns>An empty, fail-closed packet with unknown isolation and no recovery actions.</returns>
    public static EvidencePacket Unavailable(string tenantId, string? caseId = null, bool isError = false)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        if (caseId is not null)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(caseId);
        }

        return new EvidencePacket(
            new EvidencePacketScope(
                tenantId,
                caseId,
                EvidencePacketIsolationStatus.Unknown,
                EvidencePacketPermissionsContext.ForScope(caseId)),
            new EvidencePacketResultSummary(string.Empty, 0, 0, null, null),
            [],
            new EvidencePacketEvidence(
                EvidencePacketEvidenceStrength.None,
                "Evidence packet is not available.",
                [],
                [],
                isError,
                null,
                []),
            new EvidencePacketGraphSummary(false, [], [], []),
            EvidencePacketState.Empty,
            new EvidencePacketOmittedDetails(
                0,
                0,
                EvidencePacketOmissionReason.None,
                [],
                [],
                []),
            []);
    }

    /// <summary>Maps a single-axis search result into an evidence packet.</summary>
    /// <param name="result">The lower-level search result.</param>
    /// <param name="scope">The explicit tenant and case scope for the request.</param>
    /// <returns>The mapped evidence packet.</returns>
    public static EvidencePacket FromSearchResult(SearchResult result, EvidencePacketScope scope)
    {
        ArgumentNullException.ThrowIfNull(result);
        ArgumentNullException.ThrowIfNull(scope);

        scope = NormalizeScope(scope);
        if (scope.IsolationStatus == EvidencePacketIsolationStatus.Unauthorized)
        {
            return BuildUnauthorizedPacket(scope, result.Query);
        }

        IReadOnlyList<ScoredResult> results = result.Results
            ?? throw new InvalidOperationException("The search response did not include a results collection.");
        if (scope.CaseId is { } caseId && results.Any(source => IsDifferentCase(caseId, source.CaseId)))
        {
            throw new InvalidOperationException("The search response contains evidence outside the requested case scope.");
        }

        IReadOnlyList<EvidencePacketSource> sources = results
            .Select((source, index) => new EvidencePacketSource(
                index + 1,
                source.MemoryUnitId,
                source.SourceUri,
                source.SourceType,
                source.ContentSnippet,
                source.Score,
                source.CaseId,
                source.CaseName,
                source.AnnotationsCount))
            .ToArray();
        IReadOnlyList<string> axesUsed = NormalizeAxes(result.AxesUsed is { Count: > 0 }
            ? result.AxesUsed
            : results.Select(static source => source.Axis));
        IReadOnlyList<string> unavailableAxes = NormalizeAxes(result.UnavailableAxes ?? []);
        EvidencePacketEvidenceStrength strength = sources.Count > 0 && !AxesProduceNormalizedScores(axesUsed)
            ? EvidencePacketEvidenceStrength.Unknown
            : DetermineEvidenceStrength(sources.Select(static source => source.Score));
        bool degraded = result.Degraded || ReportsBackendOmission(result.OmittedReason);
        EvidencePacketOmissionReason omissionReason = MapOmittedReason(result.OmittedReason, degraded);
        EvidencePacketState state = DetermineState(
            scope,
            result.TotalCount,
            sources.Count,
            degraded,
            result.OmittedCount,
            strength);

        EvidencePacketOmittedDetails omitted = BuildOmittedDetails(
            scope,
            result.Query,
            result.OmittedCount,
            result.EstimatedTokensTotal,
            omissionReason,
            unavailableAxes,
            state);

        return new EvidencePacket(
            scope,
            new EvidencePacketResultSummary(DisplayQuery(result.Query), result.TotalCount, sources.Count, result.HasIndexedMemoryUnits, null),
            sources,
            new EvidencePacketEvidence(
                strength,
                result.Explanation?.Caveat ?? DefaultCaveat,
                axesUsed,
                unavailableAxes,
                degraded,
                null,
                BuildAxisEvidence(result.Explanation, axesUsed, results)),
            new EvidencePacketGraphSummary(false, [], [], []),
            state,
            omitted,
            BuildRecovery(state, omitted));
    }

    /// <summary>Maps a hybrid search result into an evidence packet.</summary>
    /// <param name="result">The lower-level hybrid search result.</param>
    /// <param name="scope">The explicit tenant and case scope for the request.</param>
    /// <returns>The mapped evidence packet.</returns>
    public static EvidencePacket FromHybridSearchResult(HybridSearchResult result, EvidencePacketScope scope)
    {
        ArgumentNullException.ThrowIfNull(result);
        ArgumentNullException.ThrowIfNull(scope);

        scope = NormalizeScope(scope);
        if (scope.IsolationStatus == EvidencePacketIsolationStatus.Unauthorized)
        {
            return BuildUnauthorizedPacket(scope, result.Query);
        }

        IReadOnlyList<FusedScoredResult> results = result.Results
            ?? throw new InvalidOperationException("The search response did not include a results collection.");
        if (scope.CaseId is { } caseId && results.Any(source => IsDifferentCase(caseId, source.CaseId)))
        {
            throw new InvalidOperationException("The search response contains evidence outside the requested case scope.");
        }

        IReadOnlyList<EvidencePacketSource> sources = results
            .Select((source, index) => new EvidencePacketSource(
                index + 1,
                source.MemoryUnitId,
                source.SourceUri,
                source.SourceType,
                source.ContentSnippet,
                source.CompositeScore,
                source.CaseId,
                source.CaseName,
                source.AnnotationsCount))
            .ToArray();
        IReadOnlyList<string> axesUsed = NormalizeAxes(result.AxesUsed is { Count: > 0 }
            ? result.AxesUsed
            : InferHybridAxes(results));
        IReadOnlyList<string> unavailableAxes = NormalizeAxes(result.UnavailableAxes ?? []);
        EvidencePacketEvidenceStrength strength = DetermineEvidenceStrength(sources.Select(static source => source.Score));
        bool degraded = result.Degraded || result.AllEnabledAxesUnavailable == true || ReportsBackendOmission(result.OmittedReason);
        EvidencePacketOmissionReason omissionReason = MapOmittedReason(result.OmittedReason, degraded);
        EvidencePacketState state = DetermineState(
            scope,
            result.TotalCount,
            sources.Count,
            degraded,
            result.OmittedCount,
            strength);

        EvidencePacketOmittedDetails omitted = BuildOmittedDetails(
            scope,
            result.Query,
            result.OmittedCount,
            result.EstimatedTokensTotal,
            omissionReason,
            unavailableAxes,
            state);

        return new EvidencePacket(
            scope,
            new EvidencePacketResultSummary(DisplayQuery(result.Query), result.TotalCount, sources.Count, null, null),
            sources,
            new EvidencePacketEvidence(
                strength,
                result.Explanation?.Caveat ?? DefaultCaveat,
                axesUsed,
                unavailableAxes,
                degraded,
                result.AllEnabledAxesUnavailable,
                BuildHybridAxisEvidence(result.Explanation, axesUsed, results)),
            new EvidencePacketGraphSummary(false, [], [], []),
            state,
            omitted,
            BuildRecovery(state, omitted));
    }

    /// <summary>Maps a sanitized diagnostic error into an evidence packet.</summary>
    /// <param name="error">The structured error response.</param>
    /// <param name="scope">The explicit tenant and case scope for the request.</param>
    /// <param name="query">The original query, or an empty string when no query applies.</param>
    /// <returns>The mapped evidence packet.</returns>
    public static EvidencePacket FromError(ErrorResponse error, EvidencePacketScope scope, string query = "")
    {
        ArgumentNullException.ThrowIfNull(error);
        ArgumentNullException.ThrowIfNull(scope);

        scope = NormalizeScope(scope);
        bool unauthorized = IsUnauthorized(error.Code)
            || scope.IsolationStatus == EvidencePacketIsolationStatus.Unauthorized;
        EvidencePacketScope effectiveScope = unauthorized
            ? scope with { IsolationStatus = EvidencePacketIsolationStatus.Unauthorized }
            : scope;
        EvidencePacketState state = unauthorized ? EvidencePacketState.Unauthorized : EvidencePacketState.Degraded;
        EvidencePacketOmissionReason reason = unauthorized
            ? EvidencePacketOmissionReason.Authorization
            : EvidencePacketOmissionReason.BackendUnavailable;
        var omitted = new EvidencePacketOmittedDetails(
            0,
            0,
            reason,
            unauthorized ? ["sources", "evidence"] : ["evidence"],
            unauthorized ? ["authorization"] : ["diagnostics"],
            []);

        IReadOnlyList<EvidencePacketRecoveryAction> recovery = unauthorized
            ?
            [
                new EvidencePacketRecoveryAction(
                    EvidencePacketRecoveryKind.CheckAuthorization,
                    "checkAuthorization",
                    "Use an authorized tenant and case scope.",
                    "auth"),
            ]
            :
            [
                new EvidencePacketRecoveryAction(
                    EvidencePacketRecoveryKind.Retry,
                    "retry",
                    SanitizeGuidance(error.Suggestion, "Retry the authorized request or inspect service health."),
                    "diagnostics"),
            ];

        return new EvidencePacket(
            effectiveScope,
            new EvidencePacketResultSummary(DisplayQuery(query), 0, 0, null, null),
            [],
            new EvidencePacketEvidence(
                EvidencePacketEvidenceStrength.None,
                DefaultCaveat,
                [],
                [],
                !unauthorized,
                null,
                []),
            new EvidencePacketGraphSummary(false, [], [], []),
            state,
            omitted,
            recovery);
    }

    private static EvidencePacket BuildUnauthorizedPacket(EvidencePacketScope scope, string query)
    {
        var omitted = new EvidencePacketOmittedDetails(
            0,
            0,
            EvidencePacketOmissionReason.Authorization,
            ["sources", "evidence"],
            ["authorization"],
            []);
        return new EvidencePacket(
            scope with { IsolationStatus = EvidencePacketIsolationStatus.Unauthorized },
            new EvidencePacketResultSummary(DisplayQuery(query), 0, 0, null, null),
            [],
            new EvidencePacketEvidence(
                EvidencePacketEvidenceStrength.None,
                DefaultCaveat,
                [],
                [],
                false,
                null,
                []),
            new EvidencePacketGraphSummary(false, [], [], []),
            EvidencePacketState.Unauthorized,
            omitted,
            BuildRecovery(EvidencePacketState.Unauthorized, omitted));
    }

    private static EvidencePacketState DetermineState(
        EvidencePacketScope scope,
        long totalCount,
        int returnedCount,
        bool degraded,
        int omittedCount,
        EvidencePacketEvidenceStrength strength)
    {
        if (scope.IsolationStatus == EvidencePacketIsolationStatus.Unauthorized)
        {
            return EvidencePacketState.Unauthorized;
        }

        if (totalCount < 0)
        {
            throw new InvalidOperationException("The search response reported a negative match count.");
        }

        if (degraded)
        {
            return EvidencePacketState.Degraded;
        }

        if (omittedCount > 0)
        {
            return EvidencePacketState.PendingExpansion;
        }

        if (totalCount == 0)
        {
            return EvidencePacketState.Empty;
        }

        if (returnedCount == 0)
        {
            return EvidencePacketState.Partial;
        }

        return strength == EvidencePacketEvidenceStrength.Weak
            ? EvidencePacketState.Weak
            : EvidencePacketState.Complete;
    }

    private static EvidencePacketEvidenceStrength DetermineEvidenceStrength(IEnumerable<double?> scores)
    {
        double? best = scores
            .Where(static score => score.HasValue)
            .Select(static score => score!.Value)
            .DefaultIfEmpty(double.NaN)
            .Max();

        if (!best.HasValue || double.IsNaN(best.Value))
        {
            return EvidencePacketEvidenceStrength.None;
        }

        if (best.Value <= 0d)
        {
            return EvidencePacketEvidenceStrength.None;
        }

        if (best.Value < 0.4d)
        {
            return EvidencePacketEvidenceStrength.Weak;
        }

        if (best.Value < 0.75d)
        {
            return EvidencePacketEvidenceStrength.Moderate;
        }

        return EvidencePacketEvidenceStrength.Strong;
    }

    private static EvidencePacketOmissionReason MapOmittedReason(OmittedReason reason, bool degraded)
        => (reason, degraded) switch
        {
            (OmittedReason.Combined, _) => EvidencePacketOmissionReason.Combined,
            (OmittedReason.TokenBudget, true) => EvidencePacketOmissionReason.Combined,
            (OmittedReason.TokenBudget, false) => EvidencePacketOmissionReason.TokenBudget,
            (OmittedReason.BackendDegraded, _) => EvidencePacketOmissionReason.BackendUnavailable,
            (OmittedReason.None, true) => EvidencePacketOmissionReason.BackendUnavailable,
            (OmittedReason.None, false) => EvidencePacketOmissionReason.None,
            _ => throw new InvalidOperationException("The search response reported an unrecognized omission reason."),
        };

    private static bool ReportsBackendOmission(OmittedReason reason)
        => reason is OmittedReason.BackendDegraded or OmittedReason.Combined;

    private static EvidencePacketOmittedDetails BuildOmittedDetails(
        EvidencePacketScope scope,
        string query,
        int omittedCount,
        long estimatedTokensTotal,
        EvidencePacketOmissionReason reason,
        IReadOnlyList<string> unavailableAxes,
        EvidencePacketState state)
    {
        if (state == EvidencePacketState.Unauthorized)
        {
            return new EvidencePacketOmittedDetails(
                0,
                0,
                EvidencePacketOmissionReason.Authorization,
                ["sources", "evidence"],
                ["authorization"],
                []);
        }

        List<string> fieldNames = [];
        List<string> detailGroups = [];
        List<EvidencePacketExpansionHandle> handles = [];

        if (omittedCount > 0)
        {
            fieldNames.Add("sources");
            detailGroups.Add("rankedResults");
            handles.Add(new EvidencePacketExpansionHandle(
                BuildHandle(scope, query, "rankedResults"),
                EvidencePacketRecoveryKind.IncreaseTokenBudget,
                "rankedResults",
                scope.TenantId,
                scope.CaseId,
                "Re-run the authorized search with a larger tokenBudget or maxResults."));
        }

        if (unavailableAxes.Count > 0 || reason is EvidencePacketOmissionReason.BackendUnavailable or EvidencePacketOmissionReason.Combined)
        {
            fieldNames.Add("evidence.unavailableAxes");
            detailGroups.Add("backendDiagnostics");
        }

        return new EvidencePacketOmittedDetails(
            omittedCount,
            estimatedTokensTotal,
            reason,
            DistinctOrdinal(fieldNames),
            DistinctOrdinal(detailGroups),
            handles);
    }

    private static IReadOnlyList<EvidencePacketRecoveryAction> BuildRecovery(
        EvidencePacketState state,
        EvidencePacketOmittedDetails omitted)
    {
        return state switch
        {
            EvidencePacketState.Unauthorized =>
            [
                new EvidencePacketRecoveryAction(
                    EvidencePacketRecoveryKind.CheckAuthorization,
                    "checkAuthorization",
                    "Use an authorized tenant and case scope.",
                    "auth"),
            ],
            EvidencePacketState.Degraded => BuildDegradedRecovery(omitted),
            EvidencePacketState.PendingExpansion when omitted.Reason is EvidencePacketOmissionReason.TokenBudget or EvidencePacketOmissionReason.Combined =>
                [IncreaseTokenBudget()],
            EvidencePacketState.PendingExpansion =>
            [
                new EvidencePacketRecoveryAction(
                    EvidencePacketRecoveryKind.IncreaseMaxResults,
                    "increaseMaxResults",
                    "Re-run the authorized request with a larger maxResults to retrieve omitted detail groups.",
                    "rankedResults"),
            ],
            EvidencePacketState.Empty =>
            [
                new EvidencePacketRecoveryAction(
                    EvidencePacketRecoveryKind.BroadenScope,
                    "broadenScope",
                    "Retry with broader query terms or a broader authorized case scope.",
                    "search"),
            ],
            EvidencePacketState.Weak =>
            [
                new EvidencePacketRecoveryAction(
                    EvidencePacketRecoveryKind.BroadenScope,
                    "broadenScope",
                    "Retry with broader query terms or inspect the top memory units.",
                    "search"),
            ],
            EvidencePacketState.Partial =>
            [
                new EvidencePacketRecoveryAction(
                    EvidencePacketRecoveryKind.Retry,
                    "retry",
                    "Try a different result window or refine the authorized query to retrieve matching sources.",
                    "search"),
            ],
            _ => [],
        };
    }

    private static IReadOnlyList<EvidencePacketRecoveryAction> BuildDegradedRecovery(EvidencePacketOmittedDetails omitted)
    {
        List<EvidencePacketRecoveryAction> recovery =
        [
            new EvidencePacketRecoveryAction(
                EvidencePacketRecoveryKind.Retry,
                "retry",
                "Retry after the unavailable axis recovers.",
                "search"),
            new EvidencePacketRecoveryAction(
                EvidencePacketRecoveryKind.InspectBackendHealth,
                "inspectBackendHealth",
                "Inspect backend health before relying on unavailable axes.",
                "backendDiagnostics"),
        ];
        if (omitted.Reason == EvidencePacketOmissionReason.Combined && omitted.OmittedCount > 0)
        {
            recovery.Add(IncreaseTokenBudget());
        }

        return recovery;
    }

    private static EvidencePacketRecoveryAction IncreaseTokenBudget()
        => new(
            EvidencePacketRecoveryKind.IncreaseTokenBudget,
            "increaseTokenBudget",
            "Re-run the authorized search with a larger tokenBudget.",
            "rankedResults");

    private static IReadOnlyList<EvidencePacketAxisEvidence> BuildAxisEvidence(
        SearchExplanation? explanation,
        IReadOnlyList<string> axesUsed,
        IReadOnlyList<ScoredResult> results)
    {
        Dictionary<string, double?> bestScores = new(StringComparer.OrdinalIgnoreCase);
        foreach (ScoredResult result in results)
        {
            if (string.IsNullOrWhiteSpace(result.Axis))
            {
                continue;
            }

            if (!bestScores.TryGetValue(result.Axis, out double? current) || result.Score > current.GetValueOrDefault(double.MinValue))
            {
                bestScores[result.Axis] = result.Score;
            }
        }

        return BuildAxisEvidence(explanation, axesUsed, bestScores);
    }

    private static IReadOnlyList<EvidencePacketAxisEvidence> BuildHybridAxisEvidence(
        SearchExplanation? explanation,
        IReadOnlyList<string> axesUsed,
        IReadOnlyList<FusedScoredResult> results)
    {
        Dictionary<string, double?> bestScores = new(StringComparer.OrdinalIgnoreCase);
        foreach (FusedScoredResult result in results)
        {
            SetBest(bestScores, "syntactic", result.SyntacticScore);
            SetBest(bestScores, "semantic", result.SemanticScore);
            SetBest(bestScores, "graph", result.GraphScore);
        }

        return BuildAxisEvidence(explanation, axesUsed, bestScores);
    }

    private static IReadOnlyList<EvidencePacketAxisEvidence> BuildAxisEvidence(
        SearchExplanation? explanation,
        IReadOnlyList<string> axesUsed,
        IReadOnlyDictionary<string, double?> bestScores)
    {
        SortedSet<string> axisNames = new(StringComparer.Ordinal);
        foreach (string axis in axesUsed)
        {
            axisNames.Add(axis);
        }

        Dictionary<string, AxisExplanation> normalizedAxisDetails = new(StringComparer.OrdinalIgnoreCase);
        if (explanation?.AxisDetails is not null)
        {
            foreach (KeyValuePair<string, AxisExplanation> entry in explanation.AxisDetails)
            {
                if (string.IsNullOrWhiteSpace(entry.Key))
                {
                    continue;
                }

                string normalized = entry.Key.Trim().ToLowerInvariant();
                normalizedAxisDetails[normalized] = entry.Value;
                axisNames.Add(normalized);
            }
        }

        List<EvidencePacketAxisEvidence> evidence = [];
        foreach (string axis in axisNames)
        {
            _ = normalizedAxisDetails.TryGetValue(axis, out AxisExplanation? details);
            _ = bestScores.TryGetValue(axis, out double? score);
            evidence.Add(new EvidencePacketAxisEvidence(
                axis,
                score,
                details?.NormalizationMethod,
                details?.Description));
        }

        return evidence;
    }

    private static IReadOnlyList<string> InferHybridAxes(IReadOnlyList<FusedScoredResult> results)
    {
        SortedSet<string> axes = new(StringComparer.Ordinal);
        foreach (FusedScoredResult result in results)
        {
            if (result.GraphScore.HasValue)
            {
                axes.Add("graph");
            }

            if (result.SemanticScore.HasValue)
            {
                axes.Add("semantic");
            }

            if (result.SyntacticScore.HasValue)
            {
                axes.Add("syntactic");
            }
        }

        return axes.ToArray();
    }

    private static IReadOnlyList<string> NormalizeAxes(IEnumerable<string?> axes)
        => axes
            .Where(static axis => !string.IsNullOrWhiteSpace(axis))
            .Select(static axis => axis!.Trim().ToLowerInvariant())
            .Distinct(StringComparer.Ordinal)
            .Order(StringComparer.Ordinal)
            .ToArray();

    // Only the semantic axis produces scores already normalized to the [0,1] range that the strength
    // thresholds assume. Syntactic (raw BM25) and graph scores are unbounded, so single-axis strength
    // for those axes is reported as Unknown rather than fabricated. Hybrid grades the normalized
    // composite score and is unaffected.
    private static bool AxesProduceNormalizedScores(IReadOnlyList<string> axes)
        => axes.Count > 0 && axes.All(static axis => string.Equals(axis, "semantic", StringComparison.Ordinal));

    private static IReadOnlyList<string> DistinctOrdinal(IEnumerable<string> values)
        => values.Distinct(StringComparer.Ordinal).ToArray();

    private static void SetBest(IDictionary<string, double?> scores, string axis, double? score)
    {
        if (!score.HasValue)
        {
            return;
        }

        if (!scores.TryGetValue(axis, out double? current) || score.Value > current.GetValueOrDefault(double.MinValue))
        {
            scores[axis] = score.Value;
        }
    }

    private static string BuildHandle(EvidencePacketScope scope, string? query, string detailGroup)
    {
        using var material = new MemoryStream();
        using var writer = new BinaryWriter(material, Encoding.UTF8, leaveOpen: true);
        writer.Write(scope.TenantId);
        writer.Write(scope.CaseId ?? string.Empty);
        writer.Write(query ?? string.Empty);
        writer.Write(detailGroup);
        writer.Flush();
        material.Position = 0;
        byte[] hash = SHA256.HashData(material);
        return $"ep:v2:{Convert.ToHexStringLower(hash.AsSpan(0, 16))}:{detailGroup}";
    }

    private static EvidencePacketScope NormalizeScope(EvidencePacketScope scope)
        => scope.CaseId is not null && string.IsNullOrWhiteSpace(scope.CaseId)
            ? scope with
            {
                CaseId = null,
                PermissionsContext = scope.PermissionsContext == EvidencePacketPermissionsContext.TenantCase
                    ? EvidencePacketPermissionsContext.Tenant
                    : scope.PermissionsContext,
            }
            : scope;

    private static bool IsDifferentCase(string requestedCaseId, string? sourceCaseId)
        => !string.IsNullOrWhiteSpace(sourceCaseId) && !string.Equals(requestedCaseId, sourceCaseId, StringComparison.Ordinal);

    private static string DisplayQuery(string? query)
    {
        string safe = SanitizeFreeText(query, string.Empty);
        if (safe.Length <= MaxDisplayQueryLength)
        {
            return safe;
        }

        int length = char.IsHighSurrogate(safe[MaxDisplayQueryLength - 1]) ? MaxDisplayQueryLength - 1 : MaxDisplayQueryLength;
        return safe[..length];
    }

    private static bool IsUnauthorized(string code)
        => code.Contains("UNAUTHORIZED", StringComparison.OrdinalIgnoreCase)
            || code.Contains("FORBIDDEN", StringComparison.OrdinalIgnoreCase)
            || code.Contains("ACCESS_DENIED", StringComparison.OrdinalIgnoreCase)
            || code.Contains("PERMISSION_DENIED", StringComparison.OrdinalIgnoreCase)
            || string.Equals(code, "HTTP_401", StringComparison.OrdinalIgnoreCase)
            || string.Equals(code, "HTTP_403", StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Returns <paramref name="value"/> when it contains no sensitive text (connection strings, file
    /// paths, bearer tokens, JWTs, long opaque hex, or stack-trace markers); otherwise returns
    /// <paramref name="fallback"/>. Shared so CLI/MCP surfaces sanitize free text consistently with the
    /// packet recovery guidance.
    /// </summary>
    /// <param name="value">The candidate free text (for example a server error message).</param>
    /// <param name="fallback">The safe replacement used when sensitive text is detected.</param>
    /// <returns>The original value when safe, otherwise the fallback.</returns>
    public static string SanitizeFreeText(string? value, string fallback) => SanitizeGuidance(value, fallback);

    private static string SanitizeGuidance(string? value, string fallback)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return fallback;
        }

        return SensitiveTextRegex().IsMatch(value) ? fallback : value;
    }

    [GeneratedRegex("(bearer\\s+\\S+|(?:redis|rediss|postgres|postgresql|mongodb|mysql|amqp|amqps|bolt|neo4j)://\\S+|falkor\\S*|[A-Za-z]:\\\\|\\\\\\\\[^\\s\\\\]+|/(?:home|users|var|etc|opt|tmp|root|usr)/|\\b(?:api[_-]?key|password|pwd)\\s*=\\s*\\S|\\bauthorization:\\s*\\S|stack\\s*trace|\\bat\\s+\\w+(?:\\.\\w+)+|eyJ[A-Za-z0-9_/+=-]+\\.|\\b[a-f0-9]{32,}\\b)", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex SensitiveTextRegex();
}
