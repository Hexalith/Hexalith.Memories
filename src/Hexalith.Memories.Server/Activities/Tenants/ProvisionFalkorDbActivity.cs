// <copyright file="ProvisionFalkorDbActivity.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Activities.Tenants;

using System.Diagnostics;

using Dapr.Workflow;

using Hexalith.Memories.Contracts.V1;

using Microsoft.Extensions.Logging;

using StackExchange.Redis;

/// <summary>DAPR Workflow activity that creates a FalkorDB graph for a tenant.</summary>
public sealed partial class ProvisionFalkorDbActivity : WorkflowActivity<TenantProvisioningInput, bool>
{
    private readonly IConnectionMultiplexer _falkorDb;
    private readonly ILogger<ProvisionFalkorDbActivity> _logger;

    /// <summary>Initializes a new instance of the <see cref="ProvisionFalkorDbActivity"/> class.</summary>
    /// <param name="falkorDb">The FalkorDB connection multiplexer.</param>
    /// <param name="logger">The logger instance.</param>
    public ProvisionFalkorDbActivity(
        [FromKeyedServices("falkordb")] IConnectionMultiplexer falkorDb,
        ILogger<ProvisionFalkorDbActivity> logger)
    {
        _falkorDb = falkorDb;
        _logger = logger;
    }

    /// <inheritdoc/>
    public override async Task<bool> RunAsync(WorkflowActivityContext context, TenantProvisioningInput input)
    {
        ArgumentNullException.ThrowIfNull(input);
        long startTimestamp = Stopwatch.GetTimestamp();

        NFalkorDB.FalkorDB falkor = new(_falkorDb.GetDatabase());
        string graphId = input.TenantId;

        // Write+delete forces graph persistence while leaving it empty.
        // A read-only query like RETURN 1 may not persist the graph in FalkorDB.
        await falkor.SelectGraph(graphId).QueryAsync("CREATE (n:_SystemInit {ts: timestamp()}) WITH n DELETE n RETURN 1")
            .ConfigureAwait(false);

        LogGraphCreated(_logger, graphId, input.TenantId);
        LogActivityAudit(
            _logger,
            input.TenantId,
            nameof(ProvisionFalkorDbActivity),
            "success",
            Stopwatch.GetElapsedTime(startTimestamp).TotalMilliseconds,
            DateTimeOffset.UtcNow);
        return true;
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "FalkorDB graph '{GraphId}' created for tenant '{TenantId}'")]
    private static partial void LogGraphCreated(ILogger logger, string graphId, string tenantId);

    [LoggerMessage(Level = LogLevel.Information, Message = "Tenant provisioning activity {ActivityName} completed for tenant '{TenantId}' with result {Result} in {DurationMs} ms at {Timestamp:O}")]
    private static partial void LogActivityAudit(
        ILogger logger,
        string tenantId,
        string activityName,
        string result,
        double durationMs,
        DateTimeOffset timestamp);
}
