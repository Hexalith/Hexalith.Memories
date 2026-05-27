// <copyright file="DeleteFalkorDbGraphFinalizerActivity.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Activities.Tenants;

using Dapr.Workflow;

using Hexalith.Memories.Contracts.V1;

using Microsoft.Extensions.Logging;

using StackExchange.Redis;

/// <summary>Tenant deletion activity that deletes the empty FalkorDB graph after all nodes have been batched out.</summary>
public sealed partial class DeleteFalkorDbGraphFinalizerActivity : WorkflowActivity<TenantDeletionInput, bool>
{
    private readonly IConnectionMultiplexer _falkorDb;
    private readonly ILogger<DeleteFalkorDbGraphFinalizerActivity> _logger;

    /// <summary>Initializes a new instance of the <see cref="DeleteFalkorDbGraphFinalizerActivity"/> class.</summary>
    /// <param name="falkorDb">The FalkorDB connection multiplexer.</param>
    /// <param name="logger">The logger instance.</param>
    public DeleteFalkorDbGraphFinalizerActivity(
        [FromKeyedServices("falkordb")] IConnectionMultiplexer falkorDb,
        ILogger<DeleteFalkorDbGraphFinalizerActivity> logger)
    {
        _falkorDb = falkorDb;
        _logger = logger;
    }

    /// <inheritdoc/>
    public override async Task<bool> RunAsync(WorkflowActivityContext context, TenantDeletionInput input)
    {
        ArgumentNullException.ThrowIfNull(input);

        try
        {
            IDatabase db = _falkorDb.GetDatabase();
            await db.ExecuteAsync("GRAPH.DELETE", input.TenantId).ConfigureAwait(false);
            LogGraphDeleted(_logger, input.TenantId);
        }
        catch (RedisServerException ex) when (IsGraphNotFoundError(ex))
        {
            LogGraphNotFound(_logger, input.TenantId);
        }

        return true;
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "FalkorDB graph deleted for tenant '{TenantId}'")]
    private static partial void LogGraphDeleted(ILogger logger, string tenantId);

    [LoggerMessage(Level = LogLevel.Warning, Message = "FalkorDB graph not found for tenant '{TenantId}' — nothing to delete")]
    private static partial void LogGraphNotFound(ILogger logger, string tenantId);

    private static bool IsGraphNotFoundError(RedisServerException ex)
        => ex.Message.Contains("Graph not found", StringComparison.OrdinalIgnoreCase)
        || ex.Message.Contains("ERR Invalid graph operation", StringComparison.OrdinalIgnoreCase);
}
