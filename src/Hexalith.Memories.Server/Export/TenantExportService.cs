// <copyright file="TenantExportService.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Export;

using System.Diagnostics;
using System.Globalization;
using System.IO.Pipelines;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;

using Dapr.Actors;
using Dapr.Actors.Client;

using Hexalith.Memories.Contracts.V1;
using Hexalith.Memories.Server.Activities.Indexing;
using Hexalith.Memories.Server.Actors;
using Hexalith.Memories.Server.Cases;
using Hexalith.Memories.Server.Graph;
using Hexalith.Memories.Server.Tenants;

using Microsoft.Extensions.Logging;

using StackExchange.Redis;

/// <summary>
/// Streaming exporter for case-scope and tenant-scope data (Story 8.3). Hydrates tenant + case
/// metadata upfront, then walks Redis hashes + FalkorDB edges and writes each record through
/// <see cref="ExportWriter"/> so response bytes reach the client as they are produced — the full
/// export is never materialized server-side.
/// </summary>
/// <remarks>
/// Snapshot isolation is advisory: <see cref="CaptureSnapshotAsync"/> captures
/// <see cref="ExportSnapshot.SnapshotAt"/> as the FIRST act of an export, and the streaming pass
/// filters memory units (<c>IngestedAt &lt;= SnapshotAt</c>) and edges (<c>CreatedAt &lt;= SnapshotAt</c>)
/// accordingly. See Risk #2 in the story spec and <c>docs/dev/export.md</c> for the exact semantics.
/// </remarks>
internal partial class TenantExportService
{
    private const int EdgeBatchSize = 100;
    private const int MemoryUnitScanPageSize = 250;
    private const int EnumerationBatchSize = 100;
    private static readonly TimeSpan SnapshotToleranceWindow = TimeSpan.FromMilliseconds(500);

    private static readonly Regex MemoryUnitIdRegex = new(
        @"^[0-9A-HJKMNP-TV-Z]{26}$|^[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}$|^[0-9a-fA-F]{32}$",
        RegexOptions.Compiled,
        TimeSpan.FromSeconds(1));

    private static readonly Regex CaseIdRegex = new(
        @"^[0-9A-HJKMNP-TV-Z]{26}$",
        RegexOptions.Compiled,
        TimeSpan.FromSeconds(1));

    private readonly IConnectionMultiplexer _redis;
    private readonly IConnectionMultiplexer _falkorDb;
    private readonly IGraphQueryBuilder _graphQueryBuilder;
    private readonly CaseService _caseService;
    private readonly TenantRegistryService _tenantRegistry;
    private readonly TenantMetricsService _tenantMetrics;
    private readonly IActorProxyFactory _actorProxyFactory;
    private readonly ILogger<TenantExportService> _logger;

    /// <summary>Initializes a new instance of the <see cref="TenantExportService"/> class.</summary>
    public TenantExportService(
        [FromKeyedServices("redis")] IConnectionMultiplexer redis,
        [FromKeyedServices("falkordb")] IConnectionMultiplexer falkorDb,
        IGraphQueryBuilder graphQueryBuilder,
        CaseService caseService,
        TenantRegistryService tenantRegistry,
        TenantMetricsService tenantMetrics,
        IActorProxyFactory actorProxyFactory,
        ILogger<TenantExportService> logger)
    {
        _redis = redis;
        _falkorDb = falkorDb;
        _graphQueryBuilder = graphQueryBuilder;
        _caseService = caseService;
        _tenantRegistry = tenantRegistry;
        _tenantMetrics = tenantMetrics;
        _actorProxyFactory = actorProxyFactory;
        _logger = logger;
    }

    /// <summary>
    /// Pre-flight validation + snapshot capture. Called synchronously by the export endpoint before
    /// any response headers are written so 400/404 errors can return a clean JSON body.
    /// </summary>
    /// <param name="tenantId">Tenant to export.</param>
    /// <param name="caseId">Case identifier for case-scope; <see langword="null"/> for tenant-scope.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The captured snapshot.</returns>
    /// <exception cref="ArgumentException">Malformed identifier (400 at the endpoint).</exception>
    /// <exception cref="KeyNotFoundException">Tenant or case missing (404 at the endpoint).</exception>
    public virtual async Task<ExportSnapshot> CaptureSnapshotAsync(
        string tenantId,
        string? caseId,
        CancellationToken ct)
    {
        TenantIdGuard.Validate(tenantId);
        if (caseId is not null)
        {
            ValidateCaseIdFormat(caseId);
        }

        DateTimeOffset snapshotAt = DateTimeOffset.UtcNow;

        TenantRegistryEntry? entry = await _tenantRegistry.GetTenantEntryAsync(tenantId, ct).ConfigureAwait(false);
        if (entry is null)
        {
            throw new KeyNotFoundException($"Tenant '{tenantId}' not found.");
        }

        TenantInfo tenant = entry.Tenant;

        if (caseId is not null)
        {
            Case? caseRecord = await _caseService.GetCaseAsync(tenantId, caseId, ct).ConfigureAwait(false);
            if (caseRecord is null)
            {
                throw new KeyNotFoundException($"Case '{caseId}' not found in tenant '{tenantId}'.");
            }

            List<CaseMember> members = await _caseService.ListMembersAsync(tenantId, caseId, ct).ConfigureAwait(false);
            return new ExportSnapshot(snapshotAt, tenant, caseRecord, members, TenantConfig: null);
        }

        if (GetAnyServer(_redis) is null)
        {
            throw new RedisConnectionException(
                ConnectionFailureType.UnableToConnect,
                $"Redis server unavailable while capturing tenant export snapshot for tenant '{tenantId}'.");
        }

        ExportedTenantConfig tenantConfig = await BuildTenantConfigAsync(entry, ct).ConfigureAwait(false);
        return new ExportSnapshot(snapshotAt, tenant, CaseRecord: null, Members: null, tenantConfig);
    }

    /// <summary>Streams a case-scope export envelope through <paramref name="pipeWriter"/>.</summary>
    /// <param name="tenantId">Tenant identifier (already validated via <see cref="CaptureSnapshotAsync"/>).</param>
    /// <param name="caseId">Case identifier.</param>
    /// <param name="snapshot">Previously captured snapshot.</param>
    /// <param name="pipeWriter">Response body pipe writer.</param>
    /// <param name="ct">Cancellation token (flow from <c>HttpContext.RequestAborted</c>).</param>
    public virtual async Task WriteCaseExportAsync(
        string tenantId,
        string caseId,
        ExportSnapshot snapshot,
        PipeWriter pipeWriter,
        CancellationToken ct)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        ArgumentException.ThrowIfNullOrWhiteSpace(caseId);
        ArgumentNullException.ThrowIfNull(snapshot);
        ArgumentNullException.ThrowIfNull(pipeWriter);
        if (snapshot.CaseRecord is null || snapshot.Members is null)
        {
            throw new ArgumentException("Snapshot is not a case-scope snapshot.", nameof(snapshot));
        }

        long startTs = Stopwatch.GetTimestamp();
        LogExportStarted(_logger, tenantId, "case", snapshot.SnapshotAt);

        ExportManifest manifest = new(
            SchemaVersion: 1,
            Scope: ExportScope.Case,
            TenantId: tenantId,
            CaseId: caseId,
            ExportedAt: DateTimeOffset.UtcNow,
            SnapshotAt: snapshot.SnapshotAt);

        ExportWriter writer = new(pipeWriter);
        int memoryUnitCount = 0;
        int edgeCount = 0;
        try
        {
            writer.StartDocument();
            writer.WriteManifest(manifest);
            writer.WriteCaseSection(snapshot.CaseRecord, snapshot.Members);
            await writer.FlushAsync(ct).ConfigureAwait(false);

            writer.StartMemoryUnitsArray();
            List<string> discoveredMemoryUnitIds = await ListCaseMemoryUnitIdsAsync(tenantId, caseId, ct).ConfigureAwait(false);
            List<string> exportedMemoryUnitIds = [];
            foreach (string memoryUnitId in discoveredMemoryUnitIds)
            {
                ct.ThrowIfCancellationRequested();
                ExportedMemoryUnit? entry = await LoadExportedMemoryUnitAsync(tenantId, memoryUnitId, snapshot.SnapshotAt, ct).ConfigureAwait(false);
                if (entry is null)
                {
                    continue;
                }

                exportedMemoryUnitIds.Add(memoryUnitId);
                writer.WriteMemoryUnit(entry);
                memoryUnitCount++;
                if (memoryUnitCount % UnitEnumerationLogInterval == 0)
                {
                    LogMemoryUnitsEnumerated(_logger, tenantId, memoryUnitCount);
                }

                await writer.MaybeFlushAsync(ct).ConfigureAwait(false);
            }

            writer.EndMemoryUnitsArray();
            await writer.FlushAsync(ct).ConfigureAwait(false);

            writer.StartEdgesArray();
            edgeCount = await StreamEdgesAsync(tenantId, exportedMemoryUnitIds, snapshot.SnapshotAt, writer, ct).ConfigureAwait(false);
            writer.EndEdgesArray();

            writer.WriteStatistics(new ExportStatistics(memoryUnitCount, edgeCount, CaseCount: 1));
            writer.EndDocument();
            await writer.FlushAsync(ct).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            LogExportCancelled(_logger, tenantId, "case", memoryUnitCount);
            throw;
        }
        catch (Exception ex)
        {
            LogExportFailed(_logger, tenantId, "case", ex.Message);
            throw;
        }
        finally
        {
            await writer.DisposeAsync().ConfigureAwait(false);
        }

        double durationMs = Stopwatch.GetElapsedTime(startTs).TotalMilliseconds;
        LogExportCompleted(_logger, tenantId, "case", memoryUnitCount, edgeCount, durationMs);
    }

    /// <summary>Streams a tenant-scope export envelope through <paramref name="pipeWriter"/>.</summary>
    public virtual async Task WriteTenantExportAsync(
        string tenantId,
        ExportSnapshot snapshot,
        PipeWriter pipeWriter,
        CancellationToken ct)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        ArgumentNullException.ThrowIfNull(snapshot);
        ArgumentNullException.ThrowIfNull(pipeWriter);
        if (snapshot.TenantConfig is null)
        {
            throw new ArgumentException("Snapshot is not a tenant-scope snapshot.", nameof(snapshot));
        }

        long startTs = Stopwatch.GetTimestamp();
        LogExportStarted(_logger, tenantId, "tenant", snapshot.SnapshotAt);

        ExportManifest manifest = new(
            SchemaVersion: 1,
            Scope: ExportScope.Tenant,
            TenantId: tenantId,
            CaseId: null,
            ExportedAt: DateTimeOffset.UtcNow,
            SnapshotAt: snapshot.SnapshotAt);

        ExportWriter writer = new(pipeWriter);
        int memoryUnitCount = 0;
        int edgeCount = 0;
        int caseCount = 0;
        try
        {
            writer.StartDocument();
            writer.WriteManifest(manifest);
            writer.WriteTenantSection(snapshot.TenantConfig);
            await writer.FlushAsync(ct).ConfigureAwait(false);

            // Cases
            writer.StartCasesArray();
            List<Case> cases = await _caseService.ListCasesAsync(tenantId, int.MaxValue, ct).ConfigureAwait(false);
            foreach (Case caseRecord in cases)
            {
                ct.ThrowIfCancellationRequested();
                List<CaseMember> members = await _caseService.ListMembersAsync(tenantId, caseRecord.Id, ct).ConfigureAwait(false);
                writer.WriteCase(caseRecord, members);
                caseCount++;
                await writer.MaybeFlushAsync(ct).ConfigureAwait(false);
            }

            writer.EndCasesArray();
            await writer.FlushAsync(ct).ConfigureAwait(false);

            // Memory units: SCAN {tenantId}:mu:*
            writer.StartMemoryUnitsArray();
            List<string> memoryUnitIds = [];
            await foreach (string memoryUnitId in EnumerateMemoryUnitIdsAsync(tenantId, ct).ConfigureAwait(false))
            {
                ct.ThrowIfCancellationRequested();
                ExportedMemoryUnit? entry = await LoadExportedMemoryUnitAsync(tenantId, memoryUnitId, snapshot.SnapshotAt, ct).ConfigureAwait(false);
                if (entry is null)
                {
                    continue;
                }

                memoryUnitIds.Add(memoryUnitId);
                writer.WriteMemoryUnit(entry);
                memoryUnitCount++;
                if (memoryUnitCount % UnitEnumerationLogInterval == 0)
                {
                    LogMemoryUnitsEnumerated(_logger, tenantId, memoryUnitCount);
                }

                await writer.MaybeFlushAsync(ct).ConfigureAwait(false);
            }

            writer.EndMemoryUnitsArray();
            await writer.FlushAsync(ct).ConfigureAwait(false);

            // Edges
            writer.StartEdgesArray();
            edgeCount = await StreamEdgesAsync(tenantId, memoryUnitIds, snapshot.SnapshotAt, writer, ct).ConfigureAwait(false);
            writer.EndEdgesArray();

            writer.WriteStatistics(new ExportStatistics(memoryUnitCount, edgeCount, caseCount));
            writer.EndDocument();
            await writer.FlushAsync(ct).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            LogExportCancelled(_logger, tenantId, "tenant", memoryUnitCount);
            throw;
        }
        catch (Exception ex)
        {
            LogExportFailed(_logger, tenantId, "tenant", ex.Message);
            throw;
        }
        finally
        {
            await writer.DisposeAsync().ConfigureAwait(false);
        }

        double durationMs = Stopwatch.GetElapsedTime(startTs).TotalMilliseconds;
        LogExportCompleted(_logger, tenantId, "tenant", memoryUnitCount, edgeCount, durationMs);
    }

    private const int UnitEnumerationLogInterval = 1000;

    private static void ValidateCaseIdFormat(string caseId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(caseId);
        if (!CaseIdRegex.IsMatch(caseId))
        {
            throw new ArgumentException($"Invalid case id format: '{caseId}'. Expected a 26-character Crockford ULID.", nameof(caseId));
        }
    }

    private async Task<ExportedTenantConfig> BuildTenantConfigAsync(TenantRegistryEntry entry, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(entry);

        ITenantConfigurationActor configActor = _actorProxyFactory
            .CreateActorProxy<ITenantConfigurationActor>(new ActorId(entry.Tenant.Id), nameof(TenantConfigurationActor));
        TenantEmbeddingConfig embeddingConfig = await configActor.GetEmbeddingConfigAsync().ConfigureAwait(false);

        Task<(TenantIndexSizes Sizes, TenantIndexStatus Status)> sizesTask = _tenantMetrics.GetIndexSizesAsync(entry.Tenant.Id, ct);
        Task<long?> countTask = _tenantMetrics.GetMemoryUnitCountAsync(entry.Tenant.Id, ct);
        Task<DateTimeOffset?> activityTask = _tenantMetrics.GetLastActivityAtAsync(entry.Tenant.Id, ct);
        await Task.WhenAll(sizesTask, countTask, activityTask).ConfigureAwait(false);

        DateTimeOffset lastUpdated = entry.LastUpdated == default ? entry.Tenant.CreatedAt : entry.LastUpdated;

        TenantConfigurationView view = new()
        {
            Id = entry.Tenant.Id,
            DisplayName = entry.Tenant.DisplayName,
            Status = entry.Tenant.Status,
            CreatedAt = entry.Tenant.CreatedAt,
            LastActivityAt = activityTask.Result,
            MemoryUnitCount = countTask.Result,
            EmbeddingConfig = embeddingConfig,
            IndexStatus = sizesTask.Result.Status,
        };

        return new ExportedTenantConfig(
            Configuration: view,
            Status: entry.Tenant.Status,
            CreatedAt: entry.Tenant.CreatedAt,
            LastUpdated: lastUpdated);
    }

    private async Task<List<string>> ListCaseMemoryUnitIdsAsync(string tenantId, string caseId, CancellationToken ct)
    {
        NFalkorDB.FalkorDB falkor = new(_falkorDb.GetDatabase());
        (string query, IDictionary<string, object> parameters) = _graphQueryBuilder.BuildListCaseMemoryUnitIds(caseId);
        NFalkorDB.ResultSet result = await falkor.QueryAsync(tenantId, query, parameters).ConfigureAwait(false);

        List<string> ids = new(result.Count);
        foreach (NFalkorDB.Record record in result)
        {
            ct.ThrowIfCancellationRequested();
            string? id = record.Values[0]?.ToString();
            if (!string.IsNullOrWhiteSpace(id))
            {
                ids.Add(id);
            }
        }

        return ids;
    }

    private async IAsyncEnumerable<string> EnumerateMemoryUnitIdsAsync(
        string tenantId,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct)
    {
        IServer? server = GetAnyServer(_redis);
        if (server is null)
        {
            throw new RedisConnectionException(
                ConnectionFailureType.UnableToConnect,
                $"Redis server unavailable while enumerating memory units for tenant '{tenantId}'.");
        }

        string pattern = $"{tenantId}:mu:*";
        await foreach (RedisKey key in server.KeysAsync(pattern: pattern, pageSize: MemoryUnitScanPageSize).WithCancellation(ct))
        {
            ct.ThrowIfCancellationRequested();
            string keyStr = key.ToString();
            int separator = keyStr.IndexOf(":mu:", StringComparison.Ordinal);
            if (separator < 0)
            {
                continue;
            }

            string memoryUnitId = keyStr[(separator + ":mu:".Length)..];
            if (!string.IsNullOrWhiteSpace(memoryUnitId))
            {
                yield return memoryUnitId;
            }
        }
    }

    private async Task<ExportedMemoryUnit?> LoadExportedMemoryUnitAsync(
        string tenantId,
        string memoryUnitId,
        DateTimeOffset snapshotAt,
        CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        IDatabase db = _redis.GetDatabase();
        string muKey = $"{tenantId}:mu:{memoryUnitId}";
        HashEntry[] entries = await db.HashGetAllAsync(muKey).ConfigureAwait(false);
        if (entries.Length == 0)
        {
            return null;
        }

        MemoryUnit? mu = CaseService.ParseMemoryUnitFromHash(entries, tenantId, memoryUnitId);
        if (mu is null)
        {
            return null;
        }

        DateTimeOffset effectiveSnapshot = snapshotAt - SnapshotToleranceWindow;
        if (mu.IngestedAt > effectiveSnapshot)
        {
            return null;
        }

        List<MemoryUnit> annotations = await _caseService.ListAnnotationsAsync(tenantId, memoryUnitId, ct).ConfigureAwait(false);
        IReadOnlyList<string> annotationTargets = annotations.Count == 0
            ? []
            : annotations.Select(a => a.Id).ToList();

        return new ExportedMemoryUnit(mu, annotationTargets);
    }

    private async Task<int> StreamEdgesAsync(
        string tenantId,
        List<string> memoryUnitIds,
        DateTimeOffset snapshotAt,
        ExportWriter writer,
        CancellationToken ct)
    {
        if (memoryUnitIds.Count == 0)
        {
            return 0;
        }

        HashSet<string> exportedMemoryUnitIds = [.. memoryUnitIds];
        HashSet<string> processedMemoryUnitIds = [];
        int edgeCount = 0;
        DateTimeOffset effectiveSnapshot = snapshotAt - SnapshotToleranceWindow;

        NFalkorDB.FalkorDB falkor = new(_falkorDb.GetDatabase());
        for (int offset = 0; offset < memoryUnitIds.Count; offset += EdgeBatchSize)
        {
            ct.ThrowIfCancellationRequested();
            int count = Math.Min(EdgeBatchSize, memoryUnitIds.Count - offset);
            List<string> batch = memoryUnitIds.GetRange(offset, count);
            HashSet<string> currentBatchIds = [.. batch];
            HashSet<string> seenEdgeIds = [];

            (string query, IDictionary<string, object> parameters) = _graphQueryBuilder.BuildListEdgesForMemoryUnits(batch);
            NFalkorDB.ResultSet result = await falkor.QueryAsync(tenantId, query, parameters).ConfigureAwait(false);

            foreach (NFalkorDB.Record record in result)
            {
                ct.ThrowIfCancellationRequested();
                ExportedEdge? edge = TryParseEdge(record);
                if (edge is null)
                {
                    continue;
                }

                if (edge.CreatedAt > effectiveSnapshot)
                {
                    continue;
                }

                if (HasProcessedExportedEndpoint(edge, exportedMemoryUnitIds, currentBatchIds, processedMemoryUnitIds))
                {
                    continue;
                }

                if (!seenEdgeIds.Add(edge.Id))
                {
                    continue;
                }

                writer.WriteEdge(edge);
                edgeCount++;
                await writer.MaybeFlushAsync(ct).ConfigureAwait(false);
            }

            processedMemoryUnitIds.UnionWith(currentBatchIds);
        }

        return edgeCount;
    }

    private static ExportedEdge? TryParseEdge(NFalkorDB.Record record)
    {
        if (record.Values.Count < 9)
        {
            return null;
        }

        string? edgeId = FormatInvariant(record.Values[0]);
        string? sourceId = record.Values[1]?.ToString();
        string? targetId = record.Values[2]?.ToString();
        string? edgeType = NormalizeEdgeType(record.Values[3]?.ToString());
        if (string.IsNullOrWhiteSpace(edgeId) ||
            string.IsNullOrWhiteSpace(sourceId) ||
            string.IsNullOrWhiteSpace(targetId) ||
            string.IsNullOrWhiteSpace(edgeType))
        {
            return null;
        }

        float confidence = ParseFloat(record.Values[4]) ?? 0f;
        string origin = record.Values[5]?.ToString() ?? "unknown";
        if (!TryParseEdgeTimestamp(record.Values[6], out DateTimeOffset createdAt))
        {
            return null;
        }

        string? verifiedBy = record.Values[7]?.ToString();
        float? previousConfidence = ParseFloat(record.Values[8]);

        return new ExportedEdge(
            Id: edgeId,
            SourceId: sourceId,
            TargetId: targetId,
            EdgeType: edgeType,
            Confidence: confidence,
            Origin: origin,
            CreatedAt: createdAt,
            VerifiedBy: string.IsNullOrWhiteSpace(verifiedBy) ? null : verifiedBy,
            PreviousConfidence: previousConfidence);
    }

    private static bool HasProcessedExportedEndpoint(
        ExportedEdge edge,
        HashSet<string> exportedMemoryUnitIds,
        HashSet<string> currentBatchIds,
        HashSet<string> processedMemoryUnitIds)
    {
        bool sourceProcessedEarlier = exportedMemoryUnitIds.Contains(edge.SourceId)
            && processedMemoryUnitIds.Contains(edge.SourceId)
            && !currentBatchIds.Contains(edge.SourceId);
        bool targetProcessedEarlier = exportedMemoryUnitIds.Contains(edge.TargetId)
            && processedMemoryUnitIds.Contains(edge.TargetId)
            && !currentBatchIds.Contains(edge.TargetId);

        return sourceProcessedEarlier || targetProcessedEarlier;
    }

    private static string? FormatInvariant(object? value)
    {
        if (value is null)
        {
            return null;
        }

        return value switch
        {
            long l => l.ToString(CultureInfo.InvariantCulture),
            int i => i.ToString(CultureInfo.InvariantCulture),
            _ => value.ToString(),
        };
    }

    private static float? ParseFloat(object? value)
    {
        if (value is null)
        {
            return null;
        }

        return value switch
        {
            float f => f,
            double d => (float)d,
            int i => i,
            long l => l,
            string s => float.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out float parsed) ? parsed : null,
            _ => float.TryParse(value.ToString(), NumberStyles.Float, CultureInfo.InvariantCulture, out float parsed) ? parsed : null,
        };
    }

    private static bool TryParseEdgeTimestamp(object? value, out DateTimeOffset createdAt)
    {
        if (value is null)
        {
            createdAt = DateTimeOffset.MinValue;
            return true;
        }

        if (value is DateTimeOffset offset)
        {
            createdAt = offset;
            return true;
        }

        if (value is DateTime dateTime)
        {
            createdAt = new DateTimeOffset(dateTime);
            return true;
        }

        string? text = value.ToString();
        if (string.IsNullOrWhiteSpace(text))
        {
            createdAt = DateTimeOffset.MinValue;
            return true;
        }

        if (DateTimeOffset.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out createdAt))
        {
            return true;
        }

        createdAt = default;
        return false;
    }

    private static string? NormalizeEdgeType(string? edgeType)
    {
        if (string.IsNullOrWhiteSpace(edgeType))
        {
            return null;
        }

        string[] parts = edgeType.Split('_', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length == 0)
        {
            return null;
        }

        StringBuilder builder = new(parts[0].Length + edgeType.Length);
        builder.Append(parts[0].ToLowerInvariant());
        for (int i = 1; i < parts.Length; i++)
        {
            string part = parts[i].ToLowerInvariant();
            if (part.Length == 0)
            {
                continue;
            }

            builder.Append(char.ToUpperInvariant(part[0]));
            if (part.Length > 1)
            {
                builder.Append(part[1..]);
            }
        }

        return builder.ToString();
    }

    private static IServer? GetAnyServer(IConnectionMultiplexer redis)
    {
        // TODO: factor with TenantMetricsService.GetAnyServer + EnumerateMemoryUnitIdsActivity when a 4th caller lands.
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

    [LoggerMessage(EventId = 8301, Level = LogLevel.Information, Message = "Export started — tenant={TenantId} scope={Scope} snapshotAt={SnapshotAt:O}")]
    private static partial void LogExportStarted(ILogger logger, string tenantId, string scope, DateTimeOffset snapshotAt);

    [LoggerMessage(EventId = 8302, Level = LogLevel.Information, Message = "Export progress — tenant={TenantId} memoryUnitsEnumerated={Count}")]
    private static partial void LogMemoryUnitsEnumerated(ILogger logger, string tenantId, int count);

    [LoggerMessage(EventId = 8303, Level = LogLevel.Information, Message = "Export completed — tenant={TenantId} scope={Scope} units={UnitCount} edges={EdgeCount} durationMs={DurationMs}")]
    private static partial void LogExportCompleted(ILogger logger, string tenantId, string scope, int unitCount, int edgeCount, double durationMs);

    [LoggerMessage(EventId = 8310, Level = LogLevel.Warning, Message = "Export cancelled — tenant={TenantId} scope={Scope} unitsEmittedBeforeCancel={UnitsSoFar}")]
    private static partial void LogExportCancelled(ILogger logger, string tenantId, string scope, int unitsSoFar);

    [LoggerMessage(EventId = 8311, Level = LogLevel.Error, Message = "Export failed — tenant={TenantId} scope={Scope} exception={ExceptionMessage}")]
    private static partial void LogExportFailed(ILogger logger, string tenantId, string scope, string exceptionMessage);
}
