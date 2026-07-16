// <copyright file="VerifyTenantActivity.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Activities.Tenants;

using System.Diagnostics;

using Dapr.Workflow;

using Hexalith.Memories.Contracts.V1;
using Hexalith.Memories.Server.Infrastructure;

using Microsoft.Extensions.Logging;

using StackExchange.Redis;

/// <summary>DAPR Workflow activity that verifies all three backends are provisioned for a tenant.</summary>
public sealed partial class VerifyTenantActivity : WorkflowActivity<TenantProvisioningInput, bool>
{
    private readonly IConnectionMultiplexer _redis;
    private readonly IConnectionMultiplexer _falkorDb;
    private readonly ILogger<VerifyTenantActivity> _logger;

    /// <summary>Initializes a new instance of the <see cref="VerifyTenantActivity"/> class.</summary>
    /// <param name="redis">The Redis connection multiplexer.</param>
    /// <param name="falkorDb">The FalkorDB connection multiplexer.</param>
    /// <param name="logger">The logger instance.</param>
    public VerifyTenantActivity(
        [FromKeyedServices("redis")] IConnectionMultiplexer redis,
        [FromKeyedServices("falkordb")] IConnectionMultiplexer falkorDb,
        ILogger<VerifyTenantActivity> logger)
    {
        _redis = redis;
        _falkorDb = falkorDb;
        _logger = logger;
    }

    /// <inheritdoc/>
    public override async Task<bool> RunAsync(WorkflowActivityContext context, TenantProvisioningInput input)
    {
        ArgumentNullException.ThrowIfNull(input);
        long startTimestamp = Stopwatch.GetTimestamp();
        IDatabase db = _redis.GetDatabase();
        List<string> failures = [];

        // Verify RediSearch index
        string syntacticIndex = IndexSchemaDefinitions.GetSyntacticIndexName(input.TenantId);
        try
        {
            RedisResult syntacticInfo = await db.ExecuteAsync("FT.INFO", syntacticIndex).ConfigureAwait(false);
            if (!IndexSchemaDefinitions.TryGetDocumentCount(syntacticInfo, out int syntacticDocumentCount))
            {
                failures.Add($"RediSearch index '{syntacticIndex}' has an unreadable document count");
            }
            else if (syntacticDocumentCount != 0)
            {
                failures.Add($"RediSearch index '{syntacticIndex}' is not empty (contains {syntacticDocumentCount} documents)");
            }
        }
        catch (RedisServerException)
        {
            failures.Add($"RediSearch index '{syntacticIndex}' not found");
        }

        // Verify Redis Vector index
        string semanticIndex = IndexSchemaDefinitions.GetSemanticIndexName(input.TenantId);
        try
        {
            RedisResult semanticInfo = await db.ExecuteAsync("FT.INFO", semanticIndex).ConfigureAwait(false);
            if (!IndexSchemaDefinitions.TryGetDocumentCount(semanticInfo, out int semanticDocumentCount))
            {
                failures.Add($"Redis Vector index '{semanticIndex}' has an unreadable document count");
            }
            else if (semanticDocumentCount != 0)
            {
                failures.Add($"Redis Vector index '{semanticIndex}' is not empty (contains {semanticDocumentCount} documents)");
            }
        }
        catch (RedisServerException)
        {
            failures.Add($"Redis Vector index '{semanticIndex}' not found");
        }

        // Verify FalkorDB graph
        try
        {
            NFalkorDB.FalkorDB falkor = new(_falkorDb.GetDatabase());
            NFalkorDB.ResultSet result = await falkor.SelectGraph(input.TenantId).QueryAsync("MATCH (n) RETURN count(n)")
                .ConfigureAwait(false);

            // Graph exists and is accessible -- verify it's empty
            NFalkorDB.Record? firstRecord = result.FirstOrDefault();
            if (firstRecord is null || firstRecord.Values.Count == 0)
            {
                failures.Add($"FalkorDB graph '{input.TenantId}' returned no count result");
            }
            else if (!long.TryParse(firstRecord.Values[0]?.ToString(), out long nodeCount))
            {
                failures.Add($"FalkorDB graph '{input.TenantId}' returned an unreadable node count");
            }
            else if (nodeCount != 0)
            {
                failures.Add($"FalkorDB graph '{input.TenantId}' is not empty (contains {nodeCount} nodes)");
            }
        }
        catch (RedisServerException ex)
        {
            failures.Add($"FalkorDB graph '{input.TenantId}' not accessible: {ex.Message}");
        }

        if (failures.Count > 0)
        {
            string message = $"Tenant '{input.TenantId}' verification failed: {string.Join("; ", failures)}";
            LogVerificationFailed(_logger, input.TenantId, message);
            LogActivityAudit(
                _logger,
                input.TenantId,
                nameof(VerifyTenantActivity),
                "failure",
                Stopwatch.GetElapsedTime(startTimestamp).TotalMilliseconds,
                DateTimeOffset.UtcNow);
            throw new InvalidOperationException(message);
        }

        LogVerificationPassed(_logger, input.TenantId);
        LogActivityAudit(
            _logger,
            input.TenantId,
            nameof(VerifyTenantActivity),
            "success",
            Stopwatch.GetElapsedTime(startTimestamp).TotalMilliseconds,
            DateTimeOffset.UtcNow);
        return true;
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Tenant '{TenantId}' verification passed — all backends provisioned")]
    private static partial void LogVerificationPassed(ILogger logger, string tenantId);

    [LoggerMessage(Level = LogLevel.Error, Message = "Tenant '{TenantId}' verification failed: {Details}")]
    private static partial void LogVerificationFailed(ILogger logger, string tenantId, string details);

    [LoggerMessage(Level = LogLevel.Information, Message = "Tenant provisioning activity {ActivityName} completed for tenant '{TenantId}' with result {Result} in {DurationMs} ms at {Timestamp:O}")]
    private static partial void LogActivityAudit(
        ILogger logger,
        string tenantId,
        string activityName,
        string result,
        double durationMs,
        DateTimeOffset timestamp);
}
