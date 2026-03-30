namespace Hexalith.Memories.Server.Activities.Indexing;

using System.Text.Json;

using Dapr.Workflow;

using Hexalith.Memories.Contracts.V1;
using Hexalith.Memories.Server.Graph;

using Microsoft.Extensions.Logging;

using StackExchange.Redis;

/// <summary>DAPR Workflow activity that indexes a memory unit in FalkorDB (graph database).</summary>
public sealed class IndexGraphActivity : WorkflowActivity<IndexInput, IndexResult>
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

        // 2. Merge memory unit node (idempotent — creates or updates)
        (query, parameters) = _graphQueryBuilder.BuildMergeMemoryUnitNode(
            input.MemoryUnitId,
            input.CaseId,
            input.Content,
            input.ContentHash,
            input.SourceUri,
            input.SourceType,
            input.EmbeddingProvider,
            input.EmbeddingDimensions,
            input.IngestedBy,
            input.IngestedAt,
            JsonSerializer.Serialize(input.Metadata, MemoriesJsonContext.Options));
        await falkor.QueryAsync(graphId, query, parameters).WaitAsync(GraphOperationTimeout).ConfigureAwait(false);

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
            (query, parameters) = _graphQueryBuilder.BuildMergeStubNode(input.CausationId);
            await falkor.QueryAsync(graphId, query, parameters).WaitAsync(GraphOperationTimeout).ConfigureAwait(false);

            (query, parameters) = _graphQueryBuilder.BuildMergeEdge(
                input.CausationId,
                input.MemoryUnitId,
                EdgeType.CausedBy,
                EdgeTypeDefaults.CausedBy,
                EdgeOrigin.Explicit);
            await falkor.QueryAsync(graphId, query, parameters).WaitAsync(GraphOperationTimeout).ConfigureAwait(false);
        }

        // 5. Optional CorrelatedWith edge
        if (!string.IsNullOrWhiteSpace(input.CorrelationId))
        {
            (query, parameters) = _graphQueryBuilder.BuildMergeStubNode(input.CorrelationId);
            await falkor.QueryAsync(graphId, query, parameters).WaitAsync(GraphOperationTimeout).ConfigureAwait(false);

            (query, parameters) = _graphQueryBuilder.BuildMergeEdge(
                input.CorrelationId,
                input.MemoryUnitId,
                EdgeType.CorrelatedWith,
                EdgeTypeDefaults.CorrelatedWith,
                EdgeOrigin.Explicit);
            await falkor.QueryAsync(graphId, query, parameters).WaitAsync(GraphOperationTimeout).ConfigureAwait(false);
        }

        _logger.LogInformation(
            "Indexed memory unit {MemoryUnitId} in FalkorDB graph for tenant {TenantId}",
            input.MemoryUnitId,
            input.TenantId);

        return new IndexResult("graph", input.MemoryUnitId, input.TenantId);
    }
}
