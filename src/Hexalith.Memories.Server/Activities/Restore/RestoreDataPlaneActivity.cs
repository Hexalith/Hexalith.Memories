// <copyright file="RestoreDataPlaneActivity.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Activities.Restore;

using System.Text.Json;

using Dapr.Workflow;

using Hexalith.Memories.Server.Activities.Indexing;
using Hexalith.Memories.Server.Graph;
using Hexalith.Memories.Server.Import;
using Hexalith.Memories.Server.Infrastructure;
using Hexalith.Memories.Server.Serialization;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

using StackExchange.Redis;

/// <summary>
/// Story 26.2 — restores every <b>byte-exact</b> data-plane artifact from a staged export: the syntactic
/// memory-unit hashes, case + members hashes, the FalkorDB nodes and edges (including the confidence-promotion
/// audit trail), and CONTAINS edges rebuilt from each unit's <c>caseId</c>. Vectors are re-derived separately by
/// <see cref="RestoreReindexUnitActivity"/> because embeddings are absent from the export. Idempotent: every
/// write is a Redis <c>HSET</c> overwrite or a graph <c>MERGE</c>, so a re-run converges to the same state.
/// </summary>
internal sealed class RestoreDataPlaneActivity : WorkflowActivity<RestoreDataPlaneInput, RestoreDataPlaneResult>
{
    private static readonly TimeSpan GraphOperationTimeout = TimeSpan.FromSeconds(30);

    private readonly IImportStagingStore _stagingStore;
    private readonly IConnectionMultiplexer _redis;
    private readonly IConnectionMultiplexer _falkorDb;
    private readonly IGraphQueryBuilder _graphQueryBuilder;
    private readonly ITenantIndexReadinessVerifier _readinessVerifier;
    private readonly ILogger<RestoreDataPlaneActivity> _logger;

    /// <summary>Initializes a new instance of the <see cref="RestoreDataPlaneActivity"/> class.</summary>
    /// <param name="stagingStore">The import staging store (source of the export payload).</param>
    /// <param name="redis">The Redis connection multiplexer (data plane).</param>
    /// <param name="falkorDb">The FalkorDB connection multiplexer (graph plane).</param>
    /// <param name="graphQueryBuilder">The parameterized graph query builder.</param>
    /// <param name="logger">The logger.</param>
    /// <param name="readinessVerifier">The tenant index readiness verifier (defaults to a self-constructed instance).</param>
    public RestoreDataPlaneActivity(
        IImportStagingStore stagingStore,
        [FromKeyedServices("redis")] IConnectionMultiplexer redis,
        [FromKeyedServices("falkordb")] IConnectionMultiplexer falkorDb,
        IGraphQueryBuilder graphQueryBuilder,
        ILogger<RestoreDataPlaneActivity> logger,
        ITenantIndexReadinessVerifier? readinessVerifier = null)
    {
        ArgumentNullException.ThrowIfNull(stagingStore);
        ArgumentNullException.ThrowIfNull(redis);
        ArgumentNullException.ThrowIfNull(falkorDb);
        ArgumentNullException.ThrowIfNull(graphQueryBuilder);
        ArgumentNullException.ThrowIfNull(logger);
        _stagingStore = stagingStore;
        _redis = redis;
        _falkorDb = falkorDb;
        _graphQueryBuilder = graphQueryBuilder;
        _logger = logger;
        _readinessVerifier = readinessVerifier
            ?? new TenantIndexReadinessVerifier(NullLogger<TenantIndexReadinessVerifier>.Instance);
    }

    /// <inheritdoc/>
    public override async Task<RestoreDataPlaneResult> RunAsync(WorkflowActivityContext context, RestoreDataPlaneInput input)
    {
        ArgumentNullException.ThrowIfNull(input);

        byte[]? payload = await _stagingStore.RetrieveAsync(input.StagingKey, CancellationToken.None).ConfigureAwait(false);
        if (payload is null)
        {
            throw new ImportEnvelopeException(
                "IMPORT_STAGING_EXPIRED",
                $"Staged import payload '{input.StagingKey}' is missing or expired; resubmit the import.");
        }

        ImportEnvelope envelope = ImportEnvelopeReader.Parse(payload);

        IDatabase db = _redis.GetDatabase();

        // AC5: the tenant's syntactic index must already be provisioned (TenantProvisioningWorkflow owns index
        // creation). Verify before writing so a restore into an unprovisioned tenant fails loudly rather than
        // writing hashes that are never indexed and silently unsearchable.
        await _readinessVerifier
            .EnsureReadyAsync(db, input.TenantId, TenantIndexFamily.Syntactic, null, CancellationToken.None)
            .ConfigureAwait(false);

        NFalkorDB.FalkorDB falkor = new(_falkorDb.GetDatabase());

        foreach (ImportedCase importedCase in envelope.Cases)
        {
            await RestoreCaseAsync(db, falkor, input.TenantId, importedCase).ConfigureAwait(false);
        }

        List<string> memoryUnitIds = new(envelope.MemoryUnits.Count);
        foreach (ExportedMemoryUnit exported in envelope.MemoryUnits)
        {
            await RestoreMemoryUnitAsync(db, falkor, input.TenantId, exported.Unit).ConfigureAwait(false);
            memoryUnitIds.Add(exported.Unit.Id);
        }

        int restoredEdges = 0;
        foreach (ExportedEdge edge in envelope.Edges)
        {
            if (await RestoreEdgeAsync(falkor, input.TenantId, edge).ConfigureAwait(false))
            {
                restoredEdges++;
            }
        }

        _logger.LogInformation(
            "Restore data plane complete for tenant {TenantId}: {CaseCount} cases, {UnitCount} memory units, {EdgeCount} edges.",
            input.TenantId,
            envelope.Cases.Count,
            memoryUnitIds.Count,
            restoredEdges);

        return new RestoreDataPlaneResult(memoryUnitIds, envelope.Cases.Count, restoredEdges);
    }

    private async Task RestoreCaseAsync(IDatabase db, NFalkorDB.FalkorDB falkor, string tenantId, ImportedCase importedCase)
    {
        Case caseRecord = importedCase.Case;

        // Case hash — mirrors ProjectCaseHashActivity's field contract (round-trips CaseService.ParseCaseFromHash),
        // but restores the exported status/timestamps instead of hardcoding "active"/now.
        string caseKey = $"{tenantId}:case:{caseRecord.Id}";
        HashEntry[] caseEntries =
        [
            new HashEntry("id", caseRecord.Id),
            new HashEntry("tenantId", tenantId),
            new HashEntry("name", caseRecord.Name),
            new HashEntry("description", caseRecord.Description ?? string.Empty),
            new HashEntry("status", caseRecord.Status.ToString().ToLowerInvariant()),
            new HashEntry("createdAt", caseRecord.CreatedAt.ToString("o")),
            new HashEntry("lastUpdated", caseRecord.LastUpdated.ToString("o")),
        ];
        await db.HashSetAsync(caseKey, caseEntries).ConfigureAwait(false);

        // Members hash — one field per member, value = stored CaseMember JSON (mirrors CaseService.AddMemberAsync).
        string membersKey = $"{tenantId}:case:{caseRecord.Id}:members";
        foreach (CaseMember member in importedCase.Members)
        {
            string memberJson = JsonSerializer.Serialize(
                PersistenceModelMapper.ToStored(member),
                MemoriesPersistenceJsonContext.Options);
            await db.HashSetAsync(membersKey, member.MemberId, memberJson).ConfigureAwait(false);
        }

        (string query, IDictionary<string, object> parameters) = _graphQueryBuilder.BuildMergeCaseNode(
            caseRecord.Id,
            caseRecord.Name,
            tenantId,
            caseRecord.CreatedAt);
        await falkor.SelectGraph(tenantId).QueryAsync(query, parameters).WaitAsync(GraphOperationTimeout).ConfigureAwait(false);
    }

    private async Task RestoreMemoryUnitAsync(IDatabase db, NFalkorDB.FalkorDB falkor, string tenantId, MemoryUnit unit)
    {
        // Syntactic hash — byte-identical to ingest via the shared SyntacticHashProjection (AC2/AC7).
        string hashKey = IndexSchemaDefinitions.BuildSyntacticKey(tenantId, unit.Id);
        List<HashEntry> hashEntries = SyntacticHashProjection.BuildEntries(
            unit.Id,
            tenantId,
            unit.Content,
            unit.SourceUri,
            unit.SourceType,
            unit.Metadata,
            unit.ContentHash,
            unit.CaseId,
            unit.EmbeddingProvider,
            unit.EmbeddingModel,
            unit.IngestedBy,
            unit.IngestedAt,
            unit.LastUpdated);
        await db.HashSetAsync(hashKey, [.. hashEntries]).ConfigureAwait(false);

        // MemoryUnit graph node. BuildMergeMemoryUnitNode rejects blank provider/model; legacy units without
        // embedding attribution (pre-FR70) fall back to a sentinel so the node still merges.
        string metadataJson = JsonSerializer.Serialize(
            PersistenceModelMapper.ToStored(unit.Metadata),
            MemoriesPersistenceJsonContext.Options);
        (string query, IDictionary<string, object> parameters) = _graphQueryBuilder.BuildMergeMemoryUnitNode(
            unit.Id,
            unit.CaseId,
            unit.Content,
            unit.ContentHash,
            unit.SourceUri,
            unit.SourceType,
            string.IsNullOrWhiteSpace(unit.EmbeddingProvider) ? "unknown" : unit.EmbeddingProvider,
            string.IsNullOrWhiteSpace(unit.EmbeddingModel) ? "unknown" : unit.EmbeddingModel,
            unit.EmbeddingDimensions ?? 0,
            unit.IngestedBy,
            unit.IngestedAt,
            metadataJson);
        await falkor.SelectGraph(tenantId).QueryAsync(query, parameters).WaitAsync(GraphOperationTimeout).ConfigureAwait(false);

        // CONTAINS edge case -> memory unit. Rebuilt from caseId (never present in the exported edges[]).
        (query, parameters) = _graphQueryBuilder.BuildMergeEdge(
            unit.CaseId,
            unit.Id,
            EdgeType.Contains,
            EdgeTypeDefaults.Contains,
            EdgeOrigin.Explicit);
        await falkor.SelectGraph(tenantId).QueryAsync(query, parameters).WaitAsync(GraphOperationTimeout).ConfigureAwait(false);
    }

    private async Task<bool> RestoreEdgeAsync(NFalkorDB.FalkorDB falkor, string tenantId, ExportedEdge edge)
    {
        if (!Enum.TryParse(edge.EdgeType, ignoreCase: true, out EdgeType edgeType))
        {
            _logger.LogWarning(
                "Skipping restore of edge with unrecognized type '{EdgeType}' ({SourceId} -> {TargetId}).",
                edge.EdgeType,
                edge.SourceId,
                edge.TargetId);
            return false;
        }

        // CONTAINS edges are rebuilt from caseId; they must never come from edges[]. Skip defensively.
        if (edgeType == EdgeType.Contains)
        {
            return false;
        }

        if (!Enum.TryParse(edge.Origin, ignoreCase: true, out EdgeOrigin origin))
        {
            origin = EdgeOrigin.Inferred;
        }

        // Ensure both endpoints exist. For a case-scope export, an edge's far endpoint may live outside the
        // exported case (dangling target) — MERGE a stub so the edge MATCH resolves. ON CREATE SET means a
        // real node already merged above is never regressed to a stub.
        await MergeStubIfMissingAsync(falkor, tenantId, edge.SourceId, edge.CreatedAt).ConfigureAwait(false);
        await MergeStubIfMissingAsync(falkor, tenantId, edge.TargetId, edge.CreatedAt).ConfigureAwait(false);

        (string query, IDictionary<string, object> parameters) = _graphQueryBuilder.BuildRestoreEdge(
            edge.SourceId,
            edge.TargetId,
            edgeType,
            edge.Confidence,
            origin,
            edge.CreatedAt,
            edge.VerifiedBy,
            edge.PreviousConfidence);
        await falkor.SelectGraph(tenantId).QueryAsync(query, parameters).WaitAsync(GraphOperationTimeout).ConfigureAwait(false);
        return true;
    }

    private async Task MergeStubIfMissingAsync(NFalkorDB.FalkorDB falkor, string tenantId, string nodeId, DateTimeOffset stubCreatedAt)
    {
        (string query, IDictionary<string, object> parameters) = _graphQueryBuilder.BuildMergeStubNode(nodeId, stubCreatedAt);
        await falkor.SelectGraph(tenantId).QueryAsync(query, parameters).WaitAsync(GraphOperationTimeout).ConfigureAwait(false);
    }
}
