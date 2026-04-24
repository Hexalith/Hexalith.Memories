namespace Hexalith.Memories.Server.Activities.Indexing;

using System.Text.Json;

using Dapr.Workflow;

using Hexalith.Memories.Contracts.V1;
using Hexalith.Memories.Server.Graph;

using Microsoft.Extensions.Logging;

using StackExchange.Redis;

/// <summary>DAPR Workflow activity that indexes a memory unit in FalkorDB (graph database).</summary>
public sealed partial class IndexGraphActivity : WorkflowActivity<IndexInput, IndexResult>
{
    private static readonly TimeSpan GraphOperationTimeout = TimeSpan.FromSeconds(10);

    private readonly IConnectionMultiplexer _falkorDb;
    private readonly IGraphQueryBuilder _graphQueryBuilder;
    private readonly ILogger<IndexGraphActivity> _logger;

    public IndexGraphActivity(
        [FromKeyedServices("falkordb")] IConnectionMultiplexer falkorDb,
        IGraphQueryBuilder graphQueryBuilder,
        ILogger<IndexGraphActivity> logger)
    {
        _falkorDb = falkorDb;
        _graphQueryBuilder = graphQueryBuilder;
        _logger = logger;
    }

    /// <inheritdoc/>
    public override async Task<IndexResult> RunAsync(
        WorkflowActivityContext context,
        IndexInput input)
    {
        ArgumentNullException.ThrowIfNull(input);
        TenantIdGuard.Validate(input.TenantId);

        NFalkorDB.FalkorDB falkor = new(_falkorDb.GetDatabase());
        string graphId = input.TenantId;

        // 1. Merge case node
        (string query, IDictionary<string, object> parameters) = _graphQueryBuilder.BuildMergeCaseNode(input.CaseId);
        await falkor.QueryAsync(graphId, query, parameters).WaitAsync(GraphOperationTimeout).ConfigureAwait(false);

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
            input.Content,
            input.ContentHash,
            input.SourceUri,
            input.SourceType,
            input.EmbeddingProvider,
            input.EmbeddingModel,
            input.EmbeddingDimensions,
            input.IngestedBy,
            input.IngestedAt,
            JsonSerializer.Serialize(input.Metadata, MemoriesJsonContext.Options));
        NFalkorDB.ResultSet mergeResult = await falkor.QueryAsync(graphId, query, parameters).WaitAsync(GraphOperationTimeout).ConfigureAwait(false);
        TryEmitStubResolvedTelemetry(mergeResult, input, nowUtc);

        // 3. Contains edge: case → memory unit
        (query, parameters) = _graphQueryBuilder.BuildMergeEdge(
            input.CaseId,
            input.MemoryUnitId,
            EdgeType.Contains,
            EdgeTypeDefaults.Contains,
            EdgeOrigin.Explicit);
        await falkor.QueryAsync(graphId, query, parameters).WaitAsync(GraphOperationTimeout).ConfigureAwait(false);

        // 4. Optional CausedBy edge
        if (!string.IsNullOrWhiteSpace(input.CausationId))
        {
            (query, parameters) = _graphQueryBuilder.BuildMergeStubNode(input.CausationId, nowUtc);
            await falkor.QueryAsync(graphId, query, parameters).WaitAsync(GraphOperationTimeout).ConfigureAwait(false);

            (query, parameters) = _graphQueryBuilder.BuildMergeEdge(
                input.CausationId,
                input.MemoryUnitId,
                EdgeType.CausedBy,
                EdgeTypeDefaults.CausedBy,
                EdgeOrigin.Explicit);
            await falkor.QueryAsync(graphId, query, parameters).WaitAsync(GraphOperationTimeout).ConfigureAwait(false);
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
            await falkor.QueryAsync(graphId, query, parameters).WaitAsync(GraphOperationTimeout).ConfigureAwait(false);

            (query, parameters) = _graphQueryBuilder.BuildMergeEdge(
                input.CorrelationId,
                input.MemoryUnitId,
                EdgeType.CorrelatedWith,
                EdgeTypeDefaults.CorrelatedWith,
                EdgeOrigin.Explicit);
            await falkor.QueryAsync(graphId, query, parameters).WaitAsync(GraphOperationTimeout).ConfigureAwait(false);
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
}
