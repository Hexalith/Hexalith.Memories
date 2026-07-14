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
using Hexalith.Memories.Server.Ingestion;
using Hexalith.Memories.Server.Migration;
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
    private readonly ITenantEmbeddingConfigProvider _tenantEmbeddingConfigProvider;
    private readonly ITenantIndexReadinessVerifier _readinessVerifier;
    private readonly IRestoreTargetGuard _targetGuard;
    private readonly ILogger<RestoreDataPlaneActivity> _logger;

    /// <summary>Initializes a new instance of the <see cref="RestoreDataPlaneActivity"/> class.</summary>
    /// <param name="stagingStore">The import staging store (source of the export payload).</param>
    /// <param name="redis">The Redis connection multiplexer (data plane).</param>
    /// <param name="falkorDb">The FalkorDB connection multiplexer (graph plane).</param>
    /// <param name="graphQueryBuilder">The parameterized graph query builder.</param>
    /// <param name="tenantEmbeddingConfigProvider">The target tenant embedding configuration provider.</param>
    /// <param name="targetGuard">The clean-target restore guard.</param>
    /// <param name="logger">The logger.</param>
    /// <param name="readinessVerifier">The tenant index readiness verifier (defaults to a self-constructed instance).</param>
    public RestoreDataPlaneActivity(
        IImportStagingStore stagingStore,
        [FromKeyedServices("redis")] IConnectionMultiplexer redis,
        [FromKeyedServices("falkordb")] IConnectionMultiplexer falkorDb,
        IGraphQueryBuilder graphQueryBuilder,
        ITenantEmbeddingConfigProvider tenantEmbeddingConfigProvider,
        IRestoreTargetGuard targetGuard,
        ILogger<RestoreDataPlaneActivity> logger,
        ITenantIndexReadinessVerifier? readinessVerifier = null)
    {
        ArgumentNullException.ThrowIfNull(stagingStore);
        ArgumentNullException.ThrowIfNull(redis);
        ArgumentNullException.ThrowIfNull(falkorDb);
        ArgumentNullException.ThrowIfNull(graphQueryBuilder);
        ArgumentNullException.ThrowIfNull(tenantEmbeddingConfigProvider);
        ArgumentNullException.ThrowIfNull(targetGuard);
        ArgumentNullException.ThrowIfNull(logger);
        _stagingStore = stagingStore;
        _redis = redis;
        _falkorDb = falkorDb;
        _graphQueryBuilder = graphQueryBuilder;
        _tenantEmbeddingConfigProvider = tenantEmbeddingConfigProvider;
        _targetGuard = targetGuard;
        _logger = logger;
        _readinessVerifier = readinessVerifier
            ?? new TenantIndexReadinessVerifier(NullLogger<TenantIndexReadinessVerifier>.Instance);
    }

    /// <inheritdoc/>
    public override async Task<RestoreDataPlaneResult> RunAsync(WorkflowActivityContext context, RestoreDataPlaneInput input)
    {
        ArgumentNullException.ThrowIfNull(input);

        TenantEmbeddingConfig config = await _tenantEmbeddingConfigProvider
            .GetAsync(input.TenantId, CancellationToken.None)
            .ConfigureAwait(false);

        await using Stream? preflightStream = await _stagingStore
            .OpenReadAsync(input.StagingKey, CancellationToken.None)
            .ConfigureAwait(false);
        if (preflightStream is null)
        {
            throw new ImportEnvelopeException(
                "IMPORT_STAGING_EXPIRED",
                $"Staged import payload '{input.StagingKey}' is missing or expired; resubmit the import.");
        }

        ImportEnvelopeScanResult scan = await ImportEnvelopeStreamProcessor.ProcessAsync(
            preflightStream,
            (importedCase, _) =>
            {
                ImportEnvelopeValidator.EnsureCaseTarget(importedCase, input.TenantId, input.CaseId);
                return Task.CompletedTask;
            },
            (exported, _) =>
            {
                ImportEnvelopeValidator.EnsureMemoryUnitTarget(exported, input.TenantId, input.CaseId);
                EnsureEmbeddingCompatibility(exported.Unit, input.TenantId, config);
                return Task.CompletedTask;
            },
            edgeHandler: null,
            CancellationToken.None).ConfigureAwait(false);
        ImportEnvelopeValidator.EnsureManifestTarget(scan.Manifest, input.TenantId, input.CaseId);

        IDatabase db = _redis.GetDatabase();

        // AC5: preflight both required index families before the first data-plane write. Running semantic
        // readiness only after cases/hashes were written could leave a target partially restored.
        await _readinessVerifier
            .EnsureReadyAsync(db, input.TenantId, TenantIndexFamily.Syntactic, null, CancellationToken.None)
            .ConfigureAwait(false);
        await _readinessVerifier
            .EnsureReadyAsync(db, input.TenantId, TenantIndexFamily.Semantic, config.Dimensions, CancellationToken.None)
            .ConfigureAwait(false);

        EmbeddingMigrationMarker? marker = await EmbeddingMigrationMarkerReader
            .ReadActiveMarkerAsync(db, input.TenantId, CancellationToken.None)
            .ConfigureAwait(false);
        EmbeddingMigrationMarkerReader.EnsureWriteMatchesMarker(
            marker,
            config.Provider,
            config.Model,
            config.Dimensions);

        await _stagingStore.RenewAsync(input.StagingKey, CancellationToken.None).ConfigureAwait(false);
        if (!await _stagingStore.OwnsRestoreLeaseAsync(input.StagingKey, CancellationToken.None).ConfigureAwait(false))
        {
            throw new ImportEnvelopeException(
                "RESTORE_LEASE_LOST",
                "The restore operation no longer owns the target lease; resubmit after the active restore finishes.");
        }

        if (!await _stagingStore.HasRestoreStartedAsync(input.StagingKey, CancellationToken.None).ConfigureAwait(false))
        {
            await _targetGuard.EnsureCleanAsync(input.TenantId, input.CaseId, CancellationToken.None).ConfigureAwait(false);
            await _stagingStore.MarkRestoreStartedAsync(input.StagingKey, CancellationToken.None).ConfigureAwait(false);
        }

        NFalkorDB.FalkorDB falkor = new(_falkorDb.GetDatabase());
        await _stagingStore.ResetReindexIdsAsync(input.StagingKey, CancellationToken.None).ConfigureAwait(false);
        List<string> reindexPage = new(1000);
        int restoredMemoryUnits = 0;
        int skippedRecords = 0;
        int restoredEdges = 0;

        await using Stream? restoreStream = await _stagingStore
            .OpenReadAsync(input.StagingKey, CancellationToken.None)
            .ConfigureAwait(false);
        if (restoreStream is null)
        {
            throw new ImportEnvelopeException(
                "IMPORT_STAGING_EXPIRED",
                $"Staged import payload '{input.StagingKey}' expired after preflight; resubmit the import.");
        }

        _ = await ImportEnvelopeStreamProcessor.ProcessAsync(
            restoreStream,
            (importedCase, _) => RestoreCaseAsync(db, falkor, input.TenantId, importedCase),
            async (exported, _) =>
            {
                if (await RestoreMemoryUnitAsync(db, falkor, input.TenantId, exported.Unit, config).ConfigureAwait(false))
                {
                    restoredMemoryUnits++;
                    reindexPage.Add(exported.Unit.Id);
                    if (reindexPage.Count == reindexPage.Capacity)
                    {
                        await _stagingStore
                            .AppendReindexIdsAsync(input.StagingKey, reindexPage, CancellationToken.None)
                            .ConfigureAwait(false);
                        reindexPage.Clear();
                    }
                }
                else
                {
                    skippedRecords++;
                }
            },
            async (edge, _) =>
            {
                switch (await RestoreEdgeAsync(falkor, input.TenantId, edge).ConfigureAwait(false))
                {
                    case RestoreEdgeOutcome.Restored:
                        restoredEdges++;
                        break;
                    case RestoreEdgeOutcome.SkippedInvalid:
                        skippedRecords++;
                        break;
                    default:
                        break;
                }
            },
            CancellationToken.None).ConfigureAwait(false);

        if (reindexPage.Count > 0)
        {
            await _stagingStore
                .AppendReindexIdsAsync(input.StagingKey, reindexPage, CancellationToken.None)
                .ConfigureAwait(false);
        }

        await _stagingStore.RenewAsync(input.StagingKey, CancellationToken.None).ConfigureAwait(false);

        _logger.LogInformation(
            "Restore data plane requested by {RequestedBy} complete for tenant {TenantId}: {CaseCount} cases, {UnitCount} memory units, {EdgeCount} edges, {SkippedCount} records skipped (invalid).",
            input.RequestedBy,
            input.TenantId,
            scan.Statistics.CaseCount,
            restoredMemoryUnits,
            restoredEdges,
            skippedRecords);

        return new RestoreDataPlaneResult(
            restoredMemoryUnits,
            scan.Statistics.CaseCount,
            restoredEdges,
            skippedRecords);
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

    private async Task<bool> RestoreMemoryUnitAsync(
        IDatabase db,
        NFalkorDB.FalkorDB falkor,
        string tenantId,
        MemoryUnit unit,
        TenantEmbeddingConfig config)
    {
        // Story 26.2 review (D4): a memory unit with a blank caseId is corrupt (ingestion always sets one) and
        // cannot be merged into the graph (BuildMergeMemoryUnitNode / BuildMergeEdge require a caseId). Skip it
        // best-effort — log + report via the skipped count — rather than aborting the whole restore mid-write.
        if (string.IsNullOrWhiteSpace(unit.CaseId))
        {
            _logger.LogWarning(
                "Skipping restore of memory unit {MemoryUnitId} for tenant {TenantId}: blank caseId in the export.",
                unit.Id,
                tenantId);
            return false;
        }

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
            unit.EmbeddingDimensions ?? config.Dimensions,
            unit.IngestedBy,
            unit.IngestedAt,
            unit.LastUpdated);
        await db.HashSetAsync(hashKey, [.. hashEntries]).ConfigureAwait(false);

        // MemoryUnit graph node. BuildMergeMemoryUnitNode rejects blank provider/model; legacy units without
        // embedding attribution (pre-FR70) fall back to a sentinel so the node still merges.
        string metadataJson = JsonSerializer.Serialize(
            PersistenceModelMapper.ToStored(unit.Metadata),
            MemoriesPersistenceJsonContext.Options);
        (string query, IDictionary<string, object> parameters) = _graphQueryBuilder.BuildRestoreMemoryUnitNode(
            unit.Id,
            unit.CaseId,
            unit.Content,
            unit.ContentHash,
            unit.SourceUri,
            unit.SourceType,
            string.IsNullOrWhiteSpace(unit.EmbeddingProvider) ? "unknown" : unit.EmbeddingProvider,
            string.IsNullOrWhiteSpace(unit.EmbeddingModel) ? "unknown" : unit.EmbeddingModel,
            unit.EmbeddingDimensions ?? config.Dimensions,
            unit.IngestedBy,
            unit.IngestedAt,
            unit.LastUpdated,
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
        return true;
    }

    private async Task<RestoreEdgeOutcome> RestoreEdgeAsync(NFalkorDB.FalkorDB falkor, string tenantId, ExportedEdge edge)
    {
        if (!Enum.TryParse(edge.EdgeType, ignoreCase: true, out EdgeType edgeType))
        {
            _logger.LogWarning(
                "Skipping restore of edge with unrecognized type '{EdgeType}' ({SourceId} -> {TargetId}).",
                edge.EdgeType,
                edge.SourceId,
                edge.TargetId);
            return RestoreEdgeOutcome.SkippedByDesign;
        }

        // CONTAINS edges are rebuilt from caseId; they must never come from edges[]. Skip defensively.
        if (edgeType == EdgeType.Contains)
        {
            return RestoreEdgeOutcome.SkippedByDesign;
        }

        if (!Enum.TryParse(edge.Origin, ignoreCase: true, out EdgeOrigin origin))
        {
            origin = EdgeOrigin.Inferred;
        }

        string query;
        IDictionary<string, object> parameters;
        try
        {
            // Validate/build before creating endpoint stubs. A corrupt edge must not leave orphan graph nodes.
            (query, parameters) = _graphQueryBuilder.BuildRestoreEdge(
                edge.SourceId,
                edge.TargetId,
                edgeType,
                edge.Confidence,
                origin,
                edge.CreatedAt,
                edge.VerifiedBy,
                edge.PreviousConfidence);
        }
        catch (ArgumentException ex)
        {
            // Story 26.2 review (D4): a corrupt edge (out-of-range / non-finite confidence, blank endpoint id)
            // is skipped best-effort and reported via the skipped count, rather than aborting the whole restore.
            _logger.LogWarning(
                ex,
                "Skipping restore of invalid edge {EdgeType} ({SourceId} -> {TargetId}) for tenant {TenantId}: {Reason}.",
                edge.EdgeType,
                edge.SourceId,
                edge.TargetId,
                tenantId,
                ex.Message);
            return RestoreEdgeOutcome.SkippedInvalid;
        }

        // Ensure both endpoints exist. For a case-scope export, an edge's far endpoint may live outside the
        // exported case (dangling target) — MERGE a stub so the edge MATCH resolves.
        await MergeStubIfMissingAsync(falkor, tenantId, edge.SourceId, edge.CreatedAt).ConfigureAwait(false);
        await MergeStubIfMissingAsync(falkor, tenantId, edge.TargetId, edge.CreatedAt).ConfigureAwait(false);
        await falkor.SelectGraph(tenantId).QueryAsync(query, parameters).WaitAsync(GraphOperationTimeout).ConfigureAwait(false);
        return RestoreEdgeOutcome.Restored;
    }

    private static void EnsureEmbeddingCompatibility(
        MemoryUnit unit,
        string tenantId,
        TenantEmbeddingConfig config)
    {
        if (!string.IsNullOrWhiteSpace(unit.EmbeddingProvider)
            && !RestoreReindexUnitActivity.MatchesProviderAttribution(
                unit.EmbeddingProvider,
                config.Provider,
                config.Model))
        {
            throw new ImportEnvelopeException(
                "IMPORT_EMBEDDING_PROVIDER_MISMATCH",
                $"Memory unit '{unit.Id}' uses embedding provider '{unit.EmbeddingProvider}', but target tenant '{tenantId}' uses '{config.Provider}:{config.Model}'.");
        }

        if (!string.IsNullOrWhiteSpace(unit.EmbeddingModel)
            && !string.Equals(unit.EmbeddingModel, config.Model, StringComparison.OrdinalIgnoreCase))
        {
            throw new ImportEnvelopeException(
                "IMPORT_EMBEDDING_MODEL_MISMATCH",
                $"Memory unit '{unit.Id}' uses embedding model '{unit.EmbeddingModel}', but target tenant '{tenantId}' uses '{config.Model}'.");
        }

        if (unit.EmbeddingDimensions != config.Dimensions)
        {
            throw new ImportEnvelopeException(
                "IMPORT_EMBEDDING_DIMENSIONS_MISMATCH",
                $"Memory unit '{unit.Id}' uses {unit.EmbeddingDimensions} embedding dimensions, but target tenant '{tenantId}' uses {config.Dimensions}.");
        }
    }

    private async Task MergeStubIfMissingAsync(NFalkorDB.FalkorDB falkor, string tenantId, string nodeId, DateTimeOffset stubCreatedAt)
    {
        (string query, IDictionary<string, object> parameters) = _graphQueryBuilder.BuildMergeStubNode(nodeId, stubCreatedAt);
        await falkor.SelectGraph(tenantId).QueryAsync(query, parameters).WaitAsync(GraphOperationTimeout).ConfigureAwait(false);
    }
}
