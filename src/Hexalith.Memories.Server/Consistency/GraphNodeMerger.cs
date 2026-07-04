// <copyright file="GraphNodeMerger.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Consistency;

using System.Globalization;

using Hexalith.Memories.Contracts.V1;
using Hexalith.Memories.Server.Graph;
using Hexalith.Memories.Server.Infrastructure;

using Microsoft.Extensions.Logging;

using NFalkorDB;

using StackExchange.Redis;

/// <summary>
/// Story 8.2: graph (FalkorDB) re-merge helper. Reads the authoritative syntactic hash and
/// re-creates the <c>MemoryUnit</c> node + <c>CONTAINS</c> edge. Used by
/// <c>RepairUnitActivity</c> when a unit's graph node has to be re-created.
/// </summary>
/// <remarks>
/// Graph re-merge is fully supported in Phase B because the syntactic hash carries every
/// field needed to reconstruct the node (content, contentHash, sourceUri, sourceType,
/// caseId, embeddingProvider, embeddingModel, ingestedBy, ingestedAt, metadataJson).
/// No embedding regeneration required — <c>embeddingDimensions</c> comes from the hash.
/// </remarks>
public partial class GraphNodeMerger : IGraphNodeMerger
{
    private static readonly TimeSpan GraphOperationTimeout = TimeSpan.FromSeconds(10);

    private readonly IConnectionMultiplexer _redis;
    private readonly IConnectionMultiplexer _falkorDb;
    private readonly IGraphQueryBuilder _graphQueryBuilder;
    private readonly ILogger<GraphNodeMerger> _logger;

    /// <summary>Initializes a new instance of the <see cref="GraphNodeMerger"/> class.</summary>
    public GraphNodeMerger(
        [FromKeyedServices("redis")] IConnectionMultiplexer redis,
        [FromKeyedServices("falkordb")] IConnectionMultiplexer falkorDb,
        IGraphQueryBuilder graphQueryBuilder,
        ILogger<GraphNodeMerger> logger)
    {
        _redis = redis;
        _falkorDb = falkorDb;
        _graphQueryBuilder = graphQueryBuilder;
        _logger = logger;
    }

    /// <summary>
    /// Re-merges the graph node for a memory unit, reading all required fields from the
    /// authoritative syntactic hash.
    /// </summary>
    /// <param name="tenantId">The tenant identifier.</param>
    /// <param name="memoryUnitId">The memory unit identifier.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Task representing the operation.</returns>
    /// <exception cref="KeyNotFoundException">Thrown when the syntactic hash is absent.</exception>
    public virtual async Task ReMergeFromSyntacticAsync(string tenantId, string memoryUnitId, CancellationToken ct)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        ArgumentException.ThrowIfNullOrWhiteSpace(memoryUnitId);
        ct.ThrowIfCancellationRequested();

        IDatabase db = _redis.GetDatabase();
        string syntacticKey = IndexSchemaDefinitions.BuildSyntacticKey(tenantId, memoryUnitId);

        HashEntry[] entries = await db.HashGetAllAsync(syntacticKey).WaitAsync(ct).ConfigureAwait(false);
        if (entries.Length == 0)
        {
            throw new KeyNotFoundException(
                $"Cannot re-merge graph: syntactic hash '{syntacticKey}' is absent. "
                + "Unit is classified Unrepairable by the repair workflow.");
        }

        Dictionary<string, string> map = HashEntriesToMap(entries);

        string content = GetRequired(map, "content");
        string contentHash = GetRequired(map, "contentHash");
        string sourceUri = GetRequired(map, "sourceUri");
        string sourceTypeRaw = GetRequired(map, "sourceType");
        string caseId = GetRequired(map, "caseId");
        string embeddingProvider = GetRequired(map, "embeddingProvider");
        string embeddingModel = GetRequired(map, "embeddingModel");
        string ingestedBy = GetRequired(map, "ingestedBy");
        string metadataJson = map.TryGetValue("metadataJson", out string? metadata) ? metadata : "{}";

        DateTimeOffset ingestedAt = DateTimeOffset.TryParse(
            GetRequired(map, "ingestedAt"),
            CultureInfo.InvariantCulture,
            DateTimeStyles.RoundtripKind,
            out DateTimeOffset parsed)
            ? parsed
            : DateTimeOffset.UtcNow;

        SourceType sourceType = Enum.TryParse(sourceTypeRaw, ignoreCase: true, out SourceType st)
            ? st
            : SourceType.File;

        FalkorDB falkor = new(_falkorDb.GetDatabase());

        // 1. Merge case node
        (string query, IDictionary<string, object> parameters) = _graphQueryBuilder.BuildMergeCaseNode(caseId);
        await falkor.QueryAsync(tenantId, query, parameters).WaitAsync(GraphOperationTimeout, ct).ConfigureAwait(false);

        // 2. Merge memory unit node
        // embeddingDimensions is not stored on the syntactic hash; use 0 as a placeholder so the
        // repaired node matches the non-dimensions schema. The authoritative dims live on the
        // semantic hash; dims on the graph node are informational only.
        (query, parameters) = _graphQueryBuilder.BuildMergeMemoryUnitNode(
            memoryUnitId,
            caseId,
            content,
            contentHash,
            sourceUri,
            sourceType,
            embeddingProvider,
            embeddingModel,
            embeddingDimensions: 0,
            ingestedBy,
            ingestedAt,
            metadataJson);
        await falkor.QueryAsync(tenantId, query, parameters).WaitAsync(GraphOperationTimeout, ct).ConfigureAwait(false);

        // 3. Contains edge case → memory unit
        (query, parameters) = _graphQueryBuilder.BuildMergeEdge(
            caseId,
            memoryUnitId,
            EdgeType.Contains,
            EdgeTypeDefaults.Contains,
            EdgeOrigin.Explicit);
        await falkor.QueryAsync(tenantId, query, parameters).WaitAsync(GraphOperationTimeout, ct).ConfigureAwait(false);

        LogReMergeCompleted(_logger, tenantId, memoryUnitId);
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

    private static string GetRequired(Dictionary<string, string> map, string key)
    {
        if (!map.TryGetValue(key, out string? value) || string.IsNullOrEmpty(value))
        {
            throw new InvalidOperationException(
                $"Syntactic hash is missing required field '{key}'; cannot re-merge graph node.");
        }

        return value;
    }

    [LoggerMessage(
        EventId = 8211,
        Level = LogLevel.Information,
        Message = "GraphReMergeCompleted tenant '{TenantId}' unit '{MemoryUnitId}'")]
    private static partial void LogReMergeCompleted(ILogger logger, string tenantId, string memoryUnitId);
}
