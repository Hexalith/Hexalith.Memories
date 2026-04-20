// <copyright file="ConsistencyInspectionService.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Consistency;

using System.Globalization;
using System.Text.RegularExpressions;

using Hexalith.Memories.Contracts.V1;
using Hexalith.Memories.Server.Activities.Indexing;
using Hexalith.Memories.Server.Graph;

using Microsoft.Extensions.Logging;

using NFalkorDB;

using StackExchange.Redis;

/// <summary>
/// Synchronous per-memory-unit consistency probe. Shared by the inspection endpoint
/// (<c>GET /api/tenants/{tenantId}/consistency/inspect/{id}</c>) AND by
/// <c>RepairUnitActivity</c> (for the fresh re-verify before repair — Risk #1).
/// </summary>
/// <remarks>
/// <para>
/// Activities cannot be invoked directly from a minimal-API handler (they require DAPR
/// runtime plumbing). Factoring the probe into a service lets both paths use identical
/// logic — including the ULID regex guard (Risk #4) and the <c>RepairPlanCalculator</c>
/// mapping.
/// </para>
/// <para>
/// The service throws <see cref="ArgumentException"/> for malformed memory-unit IDs (400
/// at the HTTP boundary) and <see cref="KeyNotFoundException"/> when all three backends
/// report absent (404 at the HTTP boundary). Both exceptions are documented intentional
/// control flow — not bugs.
/// </para>
/// </remarks>
public partial class ConsistencyInspectionService : IConsistencyInspectionService
{
    private static readonly TimeSpan GraphOperationTimeout = TimeSpan.FromSeconds(10);

    private readonly IConnectionMultiplexer _redis;
    private readonly IConnectionMultiplexer _falkorDb;
    private readonly IGraphQueryBuilder _graphQueryBuilder;
    private readonly ILogger<ConsistencyInspectionService> _logger;

    /// <summary>Initializes a new instance of the <see cref="ConsistencyInspectionService"/> class.</summary>
    public ConsistencyInspectionService(
        [FromKeyedServices("redis")] IConnectionMultiplexer redis,
        [FromKeyedServices("falkordb")] IConnectionMultiplexer falkorDb,
        IGraphQueryBuilder graphQueryBuilder,
        ILogger<ConsistencyInspectionService> logger)
    {
        _redis = redis;
        _falkorDb = falkorDb;
        _graphQueryBuilder = graphQueryBuilder;
        _logger = logger;
    }

    /// <summary>
    /// Probes the three backends for a single memory unit and returns a full inspection result.
    /// </summary>
    /// <param name="tenantId">The tenant identifier.</param>
    /// <param name="memoryUnitId">The memory unit identifier (must match the ULID regex).</param>
    /// <param name="ct">Cancellation token. Observed by all three probes.</param>
    /// <returns>The inspection result when at least one backend reports the unit.</returns>
    /// <exception cref="ArgumentException">Thrown when either identifier is malformed.</exception>
    /// <exception cref="KeyNotFoundException">Thrown when all three backends report absent.</exception>
    /// <exception cref="OperationCanceledException">Thrown when <paramref name="ct"/> is cancelled.</exception>
    public virtual async Task<ConsistencyInspectionResult> InspectAsync(
        string tenantId,
        string memoryUnitId,
        CancellationToken ct)
    {
        TenantIdGuard.Validate(tenantId);
        ValidateMemoryUnitIdFormat(memoryUnitId);

        ct.ThrowIfCancellationRequested();

        IDatabase redisDb = _redis.GetDatabase();

        string syntacticKey = $"{tenantId}:mu:{memoryUnitId}";
        string vectorKey = $"{tenantId}:vec:{memoryUnitId}";

        Task<HashEntry[]> syntacticTask = redisDb.HashGetAllAsync(syntacticKey);
        Task<HashEntry[]> semanticTask = redisDb.HashGetAllAsync(vectorKey);
        Task<(bool Exists, ConsistencyGraphDetail? Detail)> graphTask = ProbeGraphAsync(tenantId, memoryUnitId, ct);

        HashEntry[] syntacticEntries = await syntacticTask.WaitAsync(ct).ConfigureAwait(false);
        HashEntry[] semanticEntries = await semanticTask.WaitAsync(ct).ConfigureAwait(false);
        (bool graphExists, ConsistencyGraphDetail? graphDetail) = await graphTask.ConfigureAwait(false);

        bool syntacticPresent = syntacticEntries.Length > 0;
        bool semanticPresent = semanticEntries.Length > 0;

        if (!syntacticPresent && !semanticPresent && !graphExists)
        {
            throw new KeyNotFoundException(
                $"Memory unit '{memoryUnitId}' not found in any backend for tenant '{tenantId}'.");
        }

        ConsistencySyntacticDetail? syntacticDetail = syntacticPresent
            ? ExtractSyntacticDetail(syntacticEntries)
            : null;

        ConsistencySemanticDetail? semanticDetail = semanticPresent
            ? ExtractSemanticDetail(semanticEntries, vectorKey)
            : null;

        ConsistencyRepairRecommendation recommendation =
            RepairPlanCalculator.Calculate(syntacticPresent, semanticPresent, graphExists);

        LogInspection(
            _logger,
            tenantId,
            memoryUnitId,
            syntacticPresent,
            semanticPresent,
            graphExists,
            recommendation.ToString());

        return new ConsistencyInspectionResult(
            tenantId,
            memoryUnitId,
            syntacticPresent,
            semanticPresent,
            graphExists,
            syntacticDetail,
            semanticDetail,
            graphExists ? graphDetail : null,
            recommendation,
            DateTimeOffset.UtcNow);
    }

    /// <summary>
    /// Validates that a memory unit identifier matches the Crockford base32 ULID pattern.
    /// Guard that runs BEFORE any Cypher query is built (Risk #4 — Cypher-injection surface).
    /// </summary>
    /// <param name="memoryUnitId">Candidate identifier.</param>
    /// <exception cref="ArgumentException">Thrown when the identifier does not match the ULID pattern.</exception>
    public static void ValidateMemoryUnitIdFormat(string memoryUnitId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(memoryUnitId);
        if (!UlidRegex().IsMatch(memoryUnitId))
        {
            throw new ArgumentException(
                $"Memory unit ID '{memoryUnitId}' is not a valid 26-character Crockford-base32 ULID.",
                nameof(memoryUnitId));
        }
    }

    [GeneratedRegex(@"^[0-9A-HJKMNP-TV-Z]{26}$")]
    private static partial Regex UlidRegex();

    private async Task<(bool Exists, ConsistencyGraphDetail? Detail)> ProbeGraphAsync(
        string tenantId,
        string memoryUnitId,
        CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        FalkorDB falkor = new(_falkorDb.GetDatabase());
        (string existsQuery, IDictionary<string, object> existsParams) =
            _graphQueryBuilder.BuildCheckMemoryUnitExists(memoryUnitId);

        ResultSet existsResult = await falkor
            .QueryAsync(tenantId, existsQuery, existsParams)
            .WaitAsync(GraphOperationTimeout, ct)
            .ConfigureAwait(false);

        if (existsResult.Count == 0)
        {
            return (false, null);
        }

        (string countQuery, IDictionary<string, object> countParams) =
            _graphQueryBuilder.BuildCountMemoryUnitEdges(memoryUnitId);

        ResultSet countResult = await falkor
            .QueryAsync(tenantId, countQuery, countParams)
            .WaitAsync(GraphOperationTimeout, ct)
            .ConfigureAwait(false);

        Record? record = countResult.FirstOrDefault();
        if (record is null || record.Values.Count < 3)
        {
            return (true, new ConsistencyGraphDetail(0, 0, 0));
        }

        int outgoing = ParseEdgeCount(record.Values[0]);
        int incoming = ParseEdgeCount(record.Values[1]);
        int caseEdges = ParseEdgeCount(record.Values[2]);

        return (true, new ConsistencyGraphDetail(outgoing, incoming, caseEdges));
    }

    private static int ParseEdgeCount(object? value)
    {
        if (value is null)
        {
            return 0;
        }

        string? text = value.ToString();
        return int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out int parsed) ? parsed : 0;
    }

    private static ConsistencySyntacticDetail ExtractSyntacticDetail(HashEntry[] entries)
    {
        Dictionary<string, string> map = HashEntriesToMap(entries);

        DateTimeOffset ingestedAt = DateTimeOffset.TryParse(
            GetValue(map, "ingestedAt"),
            CultureInfo.InvariantCulture,
            DateTimeStyles.RoundtripKind,
            out DateTimeOffset parsed)
            ? parsed
            : DateTimeOffset.MinValue;

        return new ConsistencySyntacticDetail(
            ContentHash: GetValue(map, "contentHash"),
            IngestedAt: ingestedAt,
            SourceUri: GetValue(map, "sourceUri"),
            SourceType: GetValue(map, "sourceType"),
            CaseId: GetValue(map, "caseId"),
            EmbeddingProvider: GetValue(map, "embeddingProvider"),
            EmbeddingModel: GetValue(map, "embeddingModel"));
    }

    private static ConsistencySemanticDetail ExtractSemanticDetail(HashEntry[] entries, string vectorHashKey)
    {
        Dictionary<string, string> map = HashEntriesToMap(entries);

        // Vector bytes are stored under the "embedding" field (binary); count dims as bytes / 4.
        int dims = 0;
        foreach (HashEntry entry in entries)
        {
            if (entry.Name == "embedding")
            {
                byte[]? raw = (byte[]?)entry.Value;
                if (raw is not null)
                {
                    dims = raw.Length / sizeof(float);
                }

                break;
            }
        }

        // If the hash carries a dims field directly, prefer it.
        if (map.TryGetValue("embeddingDimensions", out string? dimsValue) &&
            int.TryParse(dimsValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out int parsedDims))
        {
            dims = parsedDims;
        }

        return new ConsistencySemanticDetail(dims, vectorHashKey);
    }

    private static Dictionary<string, string> HashEntriesToMap(HashEntry[] entries)
    {
        Dictionary<string, string> map = new(StringComparer.OrdinalIgnoreCase);
        foreach (HashEntry entry in entries)
        {
            string? name = entry.Name.ToString();
            if (!string.IsNullOrEmpty(name))
            {
                map[name] = entry.Value.ToString() ?? string.Empty;
            }
        }

        return map;
    }

    private static string GetValue(Dictionary<string, string> map, string key)
        => map.TryGetValue(key, out string? value) ? value : string.Empty;

    [LoggerMessage(
        EventId = 8200,
        Level = LogLevel.Information,
        Message = "ConsistencyInspection tenant '{TenantId}' unit '{MemoryUnitId}': syntactic={Syntactic}, semantic={Semantic}, graph={Graph}, recommendation={Recommendation}")]
    private static partial void LogInspection(
        ILogger logger,
        string tenantId,
        string memoryUnitId,
        bool syntactic,
        bool semantic,
        bool graph,
        string recommendation);
}
