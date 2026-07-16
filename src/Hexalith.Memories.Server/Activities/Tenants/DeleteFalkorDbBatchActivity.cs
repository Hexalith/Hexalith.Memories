// <copyright file="DeleteFalkorDbBatchActivity.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Activities.Tenants;

using Dapr.Workflow;

using Hexalith.Memories.Contracts.V1;
using Hexalith.Memories.Server.Graph;

using Microsoft.Extensions.Logging;

using StackExchange.Redis;

/// <summary>Tenant deletion activity that deletes a batch of nodes from the FalkorDB graph.</summary>
public sealed partial class DeleteFalkorDbBatchActivity : WorkflowActivity<BatchedGraphDeletionInput, BatchedGraphDeletionResult>
{
    private readonly IConnectionMultiplexer _falkorDb;
    private readonly IGraphQueryBuilder _queryBuilder;
    private readonly ILogger<DeleteFalkorDbBatchActivity> _logger;

    /// <summary>Initializes a new instance of the <see cref="DeleteFalkorDbBatchActivity"/> class.</summary>
    /// <param name="falkorDb">The FalkorDB connection multiplexer.</param>
    /// <param name="queryBuilder">The graph query builder.</param>
    /// <param name="logger">The logger instance.</param>
    public DeleteFalkorDbBatchActivity(
        [FromKeyedServices("falkordb")] IConnectionMultiplexer falkorDb,
        IGraphQueryBuilder queryBuilder,
        ILogger<DeleteFalkorDbBatchActivity> logger)
    {
        _falkorDb = falkorDb;
        _queryBuilder = queryBuilder;
        _logger = logger;
    }

    /// <inheritdoc/>
    public override async Task<BatchedGraphDeletionResult> RunAsync(WorkflowActivityContext context, BatchedGraphDeletionInput input)
    {
        ArgumentNullException.ThrowIfNull(input);

        try
        {
            NFalkorDB.FalkorDB falkor = new(_falkorDb.GetDatabase());

            // Count remaining nodes
            (string countQuery, IDictionary<string, object> countParams) = _queryBuilder.BuildCountAllNodes();
            NFalkorDB.ResultSet countResult = await falkor.SelectGraph(input.TenantId).QueryAsync(countQuery, countParams).ConfigureAwait(false);

            long remainingNodes = ReadRequiredLongResult(countResult, "count");

            if (remainingNodes == 0)
            {
                LogBatchComplete(_logger, input.TenantId, input.BatchNumber, 0);
                return new BatchedGraphDeletionResult(0, 0, true);
            }

            // Delete a batch
            (string deleteQuery, IDictionary<string, object> deleteParams) = _queryBuilder.BuildBatchDeleteNodes(input.BatchSize);
            NFalkorDB.ResultSet deleteResult = await falkor.SelectGraph(input.TenantId).QueryAsync(deleteQuery, deleteParams).ConfigureAwait(false);

            int deletedInBatch = checked((int)ReadRequiredLongResult(deleteResult, "delete"));

            long remaining = remainingNodes - deletedInBatch;
            bool isComplete = remaining <= 0;

            LogBatchComplete(_logger, input.TenantId, input.BatchNumber, deletedInBatch);
            return new BatchedGraphDeletionResult(Math.Max(0, remaining), deletedInBatch, isComplete);
        }
        catch (RedisServerException ex) when (IsGraphNotFoundError(ex))
        {
            LogGraphNotFound(_logger, input.TenantId);
            return new BatchedGraphDeletionResult(0, 0, true);
        }
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "FalkorDB batch {BatchNumber} complete for tenant '{TenantId}': deleted {DeletedCount} nodes")]
    private static partial void LogBatchComplete(ILogger logger, string tenantId, int batchNumber, int deletedCount);

    [LoggerMessage(Level = LogLevel.Warning, Message = "FalkorDB graph not found for tenant '{TenantId}' — batch deletion complete")]
    private static partial void LogGraphNotFound(ILogger logger, string tenantId);

    private static long ReadRequiredLongResult(NFalkorDB.ResultSet result, string operationName)
    {
        NFalkorDB.Record? record = result.FirstOrDefault();
        if (record is null || record.Values.Count == 0 || !long.TryParse(record.Values[0]?.ToString(), out long parsedValue))
        {
            throw new InvalidOperationException($"Unexpected FalkorDB {operationName} result during tenant deletion.");
        }

        return parsedValue;
    }

    private static bool IsGraphNotFoundError(RedisServerException ex)
        => ex.Message.Contains("Graph not found", StringComparison.OrdinalIgnoreCase)
        || ex.Message.Contains("ERR Invalid graph operation", StringComparison.OrdinalIgnoreCase);
}
