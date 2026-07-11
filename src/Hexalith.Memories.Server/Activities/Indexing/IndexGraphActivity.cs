namespace Hexalith.Memories.Server.Activities.Indexing;

using Hexalith.Memories.Server.Activities;

using System.Text.Json;

using Dapr.Workflow;

using Hexalith.Memories.Contracts.V1;
using Hexalith.Memories.Server.Graph;
using Hexalith.Memories.Server.Ingestion;
using Hexalith.Memories.Server.Serialization;

using Microsoft.Extensions.Logging;

using StackExchange.Redis;

/// <summary>DAPR Workflow activity that indexes a memory unit in FalkorDB (graph database).</summary>
public sealed partial class IndexGraphActivity : WorkflowTraceLinkedActivity<IndexInput, IndexResult>
{
    private static readonly TimeSpan GraphOperationTimeout = TimeSpan.FromSeconds(10);

    private readonly IConnectionMultiplexer _falkorDb;
    private readonly IGraphQueryBuilder _graphQueryBuilder;
    private readonly ILogger<IndexGraphActivity> _logger;
    private readonly IWorkflowPayloadStore? _payloadStore;

    public IndexGraphActivity(
        [FromKeyedServices("falkordb")] IConnectionMultiplexer falkorDb,
        IGraphQueryBuilder graphQueryBuilder,
        ILogger<IndexGraphActivity> logger,
        IWorkflowPayloadStore? payloadStore = null)
    {
        _falkorDb = falkorDb;
        _graphQueryBuilder = graphQueryBuilder;
        _logger = logger;
        _payloadStore = payloadStore;
    }

    /// <inheritdoc/>
    protected override async Task<IndexResult> RunActivityAsync(
        WorkflowActivityContext context,
        IndexInput input)
    {
        ArgumentNullException.ThrowIfNull(input);
        TenantIdGuard.Validate(input.TenantId);
        string content = await ResolveContentAsync(input).ConfigureAwait(false);

        NFalkorDB.FalkorDB falkor = new(_falkorDb.GetDatabase());
        string graphId = input.TenantId;

        // 1. Merge case node
        (string query, IDictionary<string, object> parameters) = _graphQueryBuilder.BuildMergeCaseNode(input.CaseId);
        await falkor.SelectGraph(graphId).QueryAsync(query, parameters).WaitAsync(GraphOperationTimeout).ConfigureAwait(false);

        // Activities are non-deterministic code and may read wall clock directly (unlike workflows).
        // The MERGE query's ON CREATE SET is idempotent so replays re-apply the same timestamp to the
        // durable state without drift.
        DateTimeOffset nowUtc = DateTimeOffset.UtcNow;

        // 2. Merge memory unit node (idempotent — creates or updates). Story 9.2 Task 7.3 reads
        //    previousIsStub + stubCreatedAt from the MERGE result to emit 9154 on retroactive
        //    resolution (causal-chain gap filled by a subsequently-arriving event).
        (query, parameters) = _graphQueryBuilder.BuildMergeMemoryUnitNode(
            input.MemoryUnitId,
            input.CaseId,
            content,
            input.ContentHash,
            input.SourceUri,
            input.SourceType,
            input.EmbeddingProvider,
            input.EmbeddingModel,
            input.EmbeddingDimensions,
            input.IngestedBy,
            input.IngestedAt,
            JsonSerializer.Serialize(
                PersistenceModelMapper.ToStored(input.Metadata),
                MemoriesPersistenceJsonContext.Options));
        NFalkorDB.ResultSet mergeResult = await falkor.SelectGraph(graphId).QueryAsync(query, parameters).WaitAsync(GraphOperationTimeout).ConfigureAwait(false);
        TryEmitStubResolvedTelemetry(mergeResult, input, nowUtc);

        // 3. Contains edge: case → memory unit
        (query, parameters) = _graphQueryBuilder.BuildMergeEdge(
            input.CaseId,
            input.MemoryUnitId,
            EdgeType.Contains,
            EdgeTypeDefaults.Contains,
            EdgeOrigin.Explicit);
        await falkor.SelectGraph(graphId).QueryAsync(query, parameters).WaitAsync(GraphOperationTimeout).ConfigureAwait(false);

        // 4. Optional CausedBy edge. Symmetric to the CorrelationId guard below: a self-edge is
        //    skipped when CausationId == MemoryUnitId (malformed publisher where `causationid ==
        //    cloudevent.id` — produces a spurious cycle). Emits 9156 at Debug (high-frequency path).
        if (!string.IsNullOrWhiteSpace(input.CausationId)
            && !string.Equals(input.CausationId, input.MemoryUnitId, StringComparison.Ordinal))
        {
            (query, parameters) = _graphQueryBuilder.BuildMergeStubNode(input.CausationId, nowUtc);
            await falkor.SelectGraph(graphId).QueryAsync(query, parameters).WaitAsync(GraphOperationTimeout).ConfigureAwait(false);

            (query, parameters) = _graphQueryBuilder.BuildMergeEdge(
                input.CausationId,
                input.MemoryUnitId,
                EdgeType.CausedBy,
                EdgeTypeDefaults.CausedBy,
                EdgeOrigin.Explicit);
            await falkor.SelectGraph(graphId).QueryAsync(query, parameters).WaitAsync(GraphOperationTimeout).ConfigureAwait(false);
        }
        else if (string.Equals(input.CausationId, input.MemoryUnitId, StringComparison.Ordinal))
        {
            LogCausationIdSelfEdgeSkipped(_logger, input.MemoryUnitId);
        }

        // 5. Optional CorrelatedWith edge (Story 9.2 Task 6 — Risk #3 + Risk #15). The existing edge
        //    direction root-to-current is preserved (ADR 9.2-C). A self-edge is skipped when the
        //    current event IS the correlation root (CorrelationId == MemoryUnitId), because a root
        //    correlating with itself produces a spurious cycle and operators treat root-outbound
        //    traversals as "the group of correlated events" — including the root in that set would
        //    be confusing.
        if (!string.IsNullOrWhiteSpace(input.CorrelationId)
            && !string.Equals(input.CorrelationId, input.MemoryUnitId, StringComparison.Ordinal))
        {
            (query, parameters) = _graphQueryBuilder.BuildMergeStubNode(input.CorrelationId, nowUtc);
            await falkor.SelectGraph(graphId).QueryAsync(query, parameters).WaitAsync(GraphOperationTimeout).ConfigureAwait(false);

            (query, parameters) = _graphQueryBuilder.BuildMergeEdge(
                input.CorrelationId,
                input.MemoryUnitId,
                EdgeType.CorrelatedWith,
                EdgeTypeDefaults.CorrelatedWith,
                EdgeOrigin.Explicit);
            await falkor.SelectGraph(graphId).QueryAsync(query, parameters).WaitAsync(GraphOperationTimeout).ConfigureAwait(false);
        }
        else if (string.Equals(input.CorrelationId, input.MemoryUnitId, StringComparison.Ordinal))
        {
            // Debug level — high-frequency; structured event id 9155 via NaturalLanguageIntegrationLog.
            LogCorrelationIdSelfEdgeSkipped(_logger, input.MemoryUnitId);
        }

        _logger.LogInformation(
            "Indexed memory unit {MemoryUnitId} in FalkorDB graph for tenant {TenantId}",
            input.MemoryUnitId,
            input.TenantId);

        return new IndexResult("graph", input.MemoryUnitId, input.TenantId);
    }

    [LoggerMessage(EventId = 9155, Level = LogLevel.Debug, Message = "CorrelationId self-edge skipped for memory unit {MemoryUnitId} (CorrelationId == MemoryUnitId).")]
    private static partial void LogCorrelationIdSelfEdgeSkipped(ILogger logger, string memoryUnitId);

    [LoggerMessage(EventId = 9156, Level = LogLevel.Debug, Message = "CausationId self-edge skipped for memory unit {MemoryUnitId} (CausationId == MemoryUnitId).")]
    private static partial void LogCausationIdSelfEdgeSkipped(ILogger logger, string memoryUnitId);

    [LoggerMessage(EventId = 9154, Level = LogLevel.Information, Message = "Stub node resolved: tenant={TenantId}, memoryUnitId={MemoryUnitId}, causingEventId={CausingEventId}, stubCreatedAt={StubCreatedAt}, resolvedAt={ResolvedAt}.")]
    private static partial void LogStubResolved(
        ILogger logger,
        string tenantId,
        string memoryUnitId,
        string causingEventId,
        string stubCreatedAt,
        string resolvedAt);

    private void TryEmitStubResolvedTelemetry(NFalkorDB.ResultSet mergeResult, IndexInput input, DateTimeOffset resolvedAt)
    {
        // mergeResult has rows with columns [previousIsStub, stubCreatedAt]. When previousIsStub is
        // true, the MERGE transitioned a pre-existing stub to a full node (out-of-order resolution).
        if (mergeResult is null || mergeResult.Count == 0)
        {
            return;
        }

        try
        {
            foreach (NFalkorDB.Record record in mergeResult)
            {
                bool previousIsStub = TryReadBool(record, "previousIsStub");
                if (!previousIsStub)
                {
                    continue;
                }

                string stubCreatedAt = TryReadString(record, "stubCreatedAt") ?? "unknown";
                LogStubResolved(
                    _logger,
                    input.TenantId,
                    input.MemoryUnitId,
                    input.CausationId ?? string.Empty,
                    stubCreatedAt,
                    resolvedAt.ToString("o"));
                return;
            }
        }
        catch (Exception ex)
        {
            // Telemetry must never mask a successful index operation — swallow and log at Debug.
            _logger.LogDebug(ex, "Failed to extract previousIsStub from MERGE result for {MemoryUnitId}.", input.MemoryUnitId);
        }
    }

    private static bool TryReadBool(NFalkorDB.Record record, string column)
    {
        try
        {
            return record.GetValue<bool>(column);
        }
        catch
        {
            return false;
        }
    }

    private static string? TryReadString(NFalkorDB.Record record, string column)
    {
        try
        {
            return record.GetValue<string>(column);
        }
        catch
        {
            return null;
        }
    }

    private async Task<string> ResolveContentAsync(IndexInput input)
    {
        if (input.ContentReference is null)
        {
            return input.Content;
        }

        byte[] contentBytes = await RequirePayloadStore()
            .ReadAsync(
                input.ContentReference,
                input.TenantId,
                input.MemoryUnitId,
                WorkflowPayloadKind.ExtractedText,
                CancellationToken.None)
            .ConfigureAwait(false);
        return System.Text.Encoding.UTF8.GetString(contentBytes);
    }

    private IWorkflowPayloadStore RequirePayloadStore()
        => _payloadStore ?? throw new WorkflowPayloadException("PAYLOAD_STORE_UNAVAILABLE", "graph-content");
}
