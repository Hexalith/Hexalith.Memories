// <copyright file="ConsistencyInspectionService.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Consistency;

using System.Globalization;
using System.Net;
using System.Text.Json;
using System.Text.RegularExpressions;

using Hexalith.Memories.Contracts.V1;
using Hexalith.Memories.Server.Activities.Indexing;
using Hexalith.Memories.Server.Graph;
using Hexalith.Memories.Server.Infrastructure;

using Microsoft.Extensions.Logging;

using NFalkorDB;

using StackExchange.Redis;

/// <summary>
/// Synchronous per-memory-unit consistency probe. Shared by the inspection endpoint
/// (<c>GET /api/v1/tenants/{tenantId}/consistency/inspect/{id}</c>) AND by
/// <c>RepairUnitActivity</c> (for the fresh re-verify before repair — Risk #1).
/// </summary>
/// <remarks>
/// <para>
/// Activities cannot be invoked directly from a minimal-API handler (they require DAPR
/// runtime plumbing). Factoring the probe into a service lets both paths use identical
/// backend-presence logic and the <c>RepairPlanCalculator</c> mapping.
/// </para>
/// <para>
/// The service throws <see cref="ArgumentException"/> for blank memory-unit IDs (400
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
    /// <param name="memoryUnitId">The opaque memory unit identifier, passed exactly as returned by Memories.</param>
    /// <param name="ct">Cancellation token. Observed by all three probes.</param>
    /// <returns>The inspection result when at least one backend reports the unit.</returns>
    /// <exception cref="ArgumentException">Thrown when either identifier is blank or the tenant identifier is malformed.</exception>
    /// <exception cref="KeyNotFoundException">Thrown when all three backends report absent.</exception>
    /// <exception cref="OperationCanceledException">Thrown when <paramref name="ct"/> is cancelled.</exception>
    public virtual async Task<ConsistencyInspectionResult> InspectAsync(
        string tenantId,
        string memoryUnitId,
        CancellationToken ct)
    {
        TenantIdGuard.Validate(tenantId);
        ArgumentException.ThrowIfNullOrWhiteSpace(memoryUnitId);

        ct.ThrowIfCancellationRequested();

        ConsistencyInspectionResult? result = await ProbeCandidateAsync(tenantId, memoryUnitId, ct).ConfigureAwait(false);
        if (result is not null)
        {
            return result;
        }

        if (TryGetGuidDAlias(memoryUnitId, out string guidDAlias))
        {
            result = await ProbeCandidateAsync(tenantId, guidDAlias, ct).ConfigureAwait(false);
            if (result is not null)
            {
                return result;
            }
        }

        throw new KeyNotFoundException(
            $"Memory unit '{memoryUnitId}' not found in any backend for tenant '{tenantId}'.");
    }

    private async Task<ConsistencyInspectionResult?> ProbeCandidateAsync(
        string tenantId,
        string memoryUnitId,
        CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        IDatabase redisDb = _redis.GetDatabase();

        string syntacticKey = IndexSchemaDefinitions.BuildSyntacticKey(tenantId, memoryUnitId);
        string vectorKey = IndexSchemaDefinitions.BuildSemanticKey(tenantId, memoryUnitId);
        string? chunkVectorKey = await FindFirstSemanticChunkKeyAsync(tenantId, memoryUnitId, ct).ConfigureAwait(false);
        string naturalLanguageVectorKey = IndexSchemaDefinitions.BuildNaturalLanguageSemanticKey(tenantId, memoryUnitId);

        Task<HashEntry[]> syntacticTask = redisDb.HashGetAllAsync(syntacticKey);
        Task<HashEntry[]> semanticTask = redisDb.HashGetAllAsync(chunkVectorKey ?? vectorKey);
        Task<HashEntry[]> naturalLanguageSemanticTask = redisDb.HashGetAllAsync(naturalLanguageVectorKey);
        Task<(bool Exists, ConsistencyGraphDetail? Detail)> graphTask = ProbeGraphAsync(tenantId, memoryUnitId, ct);

        HashEntry[] syntacticEntries = await syntacticTask.WaitAsync(ct).ConfigureAwait(false);
        HashEntry[] semanticEntries = await semanticTask.WaitAsync(ct).ConfigureAwait(false);
        HashEntry[] naturalLanguageSemanticEntries = await naturalLanguageSemanticTask.WaitAsync(ct).ConfigureAwait(false);
        (bool graphExists, ConsistencyGraphDetail? graphDetail) = await graphTask.ConfigureAwait(false);

        bool syntacticPresent = syntacticEntries.Length > 0;
        bool semanticPresent = EntriesBelongToMemoryUnit(semanticEntries, memoryUnitId);
        bool naturalLanguageSemanticPresent = naturalLanguageSemanticEntries.Length > 0;

        if (!syntacticPresent && !semanticPresent && !graphExists && !naturalLanguageSemanticPresent)
        {
            return null;
        }

        ConsistencySyntacticDetail? syntacticDetail = syntacticPresent
            ? ExtractSyntacticDetail(syntacticEntries)
            : null;

        ConsistencySemanticDetail? semanticDetail = semanticPresent
            ? ExtractSemanticDetail(semanticEntries, chunkVectorKey ?? vectorKey)
            : null;

        ConsistencySemanticDetail? naturalLanguageSemanticDetail = naturalLanguageSemanticPresent
            ? ExtractSemanticDetail(naturalLanguageSemanticEntries, naturalLanguageVectorKey)
            : null;

        NaturalLanguageEmbeddingStatus naturalLanguageEmbeddingStatus = syntacticPresent
            ? ReadNaturalLanguageEmbeddingStatus(syntacticEntries)
            : NaturalLanguageEmbeddingStatus.NotApplicable;

        string? consistencyNote = NaturalLanguageConsistencyState.BuildConsistencyNote(
            naturalLanguageEmbeddingStatus,
            naturalLanguageSemanticPresent);
        ConsistencyNoteKind consistencyNoteKind = NaturalLanguageConsistencyState.BuildConsistencyNoteKind(
            naturalLanguageEmbeddingStatus,
            naturalLanguageSemanticPresent);

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
            DateTimeOffset.UtcNow)
        {
            NaturalLanguageSemanticPresent = naturalLanguageSemanticPresent,
            NaturalLanguageSemanticDetail = naturalLanguageSemanticDetail,
            NaturalLanguageEmbeddingStatus = naturalLanguageEmbeddingStatus,
            ConsistencyNote = consistencyNote,
            ConsistencyNoteKind = consistencyNoteKind,
        };
    }

    private static bool TryGetGuidDAlias(string memoryUnitId, out string guidDAlias)
    {
        if (memoryUnitId.Length == 32 && LegacyGuidRegex().IsMatch(memoryUnitId))
        {
            guidDAlias = Guid.ParseExact(memoryUnitId, "N").ToString("D");
            return true;
        }

        guidDAlias = string.Empty;
        return false;
    }

    /// <summary>
    /// Preserves the legacy repair-seam validation for Crockford-base32 ULID and GUID identifiers.
    /// </summary>
    /// <param name="memoryUnitId">Candidate identifier.</param>
    /// <exception cref="ArgumentException">
    /// Thrown when the identifier matches neither the ULID nor a GUID pattern.
    /// </exception>
    /// <remarks>
    /// <c>InspectAsync</c> accepts opaque non-blank identifiers and does not call this method.
    /// <c>RepairUnitActivity</c> retains the existing normalization behavior through
    /// <see cref="NormalizeMemoryUnitId"/>.
    /// </remarks>
    public static void ValidateMemoryUnitIdFormat(string memoryUnitId)
        => _ = NormalizeMemoryUnitId(memoryUnitId);

    /// <summary>
    /// Validates and canonicalizes a memory-unit identifier so backend lookups use the stored key shape.
    /// ULIDs are returned unchanged; accepted GUIDs normalize to lowercase hyphenated <c>D</c> format.
    /// </summary>
    /// <param name="memoryUnitId">Candidate identifier.</param>
    /// <returns>The canonical lookup key to use against Redis and FalkorDB.</returns>
    /// <exception cref="ArgumentException">
    /// Thrown when the identifier matches neither the ULID nor a GUID pattern.
    /// </exception>
    public static string NormalizeMemoryUnitId(string memoryUnitId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(memoryUnitId);
        if (UlidRegex().IsMatch(memoryUnitId))
        {
            return memoryUnitId;
        }

        if (LegacyGuidRegex().IsMatch(memoryUnitId))
        {
            string format = memoryUnitId.Contains('-', StringComparison.Ordinal) ? "D" : "N";
            return Guid.ParseExact(memoryUnitId, format).ToString("D");
        }

        throw new ArgumentException(
            $"Memory unit ID '{memoryUnitId}' must be a 26-character Crockford-base32 ULID or a GUID (D or N format).",
            nameof(memoryUnitId));
    }

    [GeneratedRegex(@"^[0-9A-HJKMNP-TV-Z]{26}$")]
    private static partial Regex UlidRegex();

    [GeneratedRegex(@"^(?:[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}|[0-9a-fA-F]{32})$")]
    private static partial Regex LegacyGuidRegex();

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
            .SelectGraph(tenantId).QueryAsync(existsQuery, existsParams)
            .WaitAsync(GraphOperationTimeout, ct)
            .ConfigureAwait(false);

        if (existsResult.Count == 0)
        {
            return (false, null);
        }

        (string countQuery, IDictionary<string, object> countParams) =
            _graphQueryBuilder.BuildCountMemoryUnitEdges(memoryUnitId);

        ResultSet countResult = await falkor
            .SelectGraph(tenantId).QueryAsync(countQuery, countParams)
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

    private async Task<string?> FindFirstSemanticChunkKeyAsync(string tenantId, string memoryUnitId, CancellationToken ct)
    {
        IServer? server = GetAnyServer(_redis);
        if (server is null)
        {
            return null;
        }

        await foreach (RedisKey key in server.KeysAsync(pattern: IndexSchemaDefinitions.BuildSemanticChunkKeyPattern(tenantId, memoryUnitId), pageSize: 100).WithCancellation(ct))
        {
            if (IndexSchemaDefinitions.TryParseSemanticChunkKey(tenantId, key, out string parsedId, out _)
                && string.Equals(parsedId, memoryUnitId, StringComparison.Ordinal))
            {
                return key.ToString();
            }
        }

        return null;
    }

    private static IServer? GetAnyServer(IConnectionMultiplexer redis)
    {
        foreach (EndPoint endpoint in redis.GetEndPoints())
        {
            IServer server = redis.GetServer(endpoint);
            if (server.IsConnected)
            {
                return server;
            }
        }

        return null;
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

    private static NaturalLanguageEmbeddingStatus ReadNaturalLanguageEmbeddingStatus(HashEntry[] syntacticEntries)
    {
        Dictionary<string, string> map = HashEntriesToMap(syntacticEntries);
        return map.TryGetValue("metadataJson", out string? metadataJson)
            ? NaturalLanguageConsistencyState.ReadStatus(metadataJson)
            : NaturalLanguageEmbeddingStatus.NotApplicable;
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

    private static bool EntriesBelongToMemoryUnit(HashEntry[] entries, string memoryUnitId)
        => entries.Length > 0
            && string.Equals(
                GetValue(HashEntriesToMap(entries), "memoryUnitId"),
                memoryUnitId,
                StringComparison.Ordinal);

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
