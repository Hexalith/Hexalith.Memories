// <copyright file="VerifyConsistencyActivity.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Activities.Indexing;

using Dapr.Workflow;

using Hexalith.Memories.Contracts.V1;
using Hexalith.Memories.Server.Consistency;
using Hexalith.Memories.Server.Graph;

using Microsoft.Extensions.Logging;

using NFalkorDB;

using StackExchange.Redis;

/// <summary>DAPR Workflow activity that verifies a memory unit exists in all three backends.</summary>
public sealed class VerifyConsistencyActivity : WorkflowActivity<ConsistencyInput, ConsistencyResult>
{
    private static readonly TimeSpan GraphOperationTimeout = TimeSpan.FromSeconds(10);

    private readonly IConnectionMultiplexer _redis;
    private readonly IConnectionMultiplexer _falkorDb;
    private readonly IGraphQueryBuilder _graphQueryBuilder;
    private readonly ILogger<VerifyConsistencyActivity> _logger;

    public VerifyConsistencyActivity(
        [FromKeyedServices("redis")] IConnectionMultiplexer redis,
        [FromKeyedServices("falkordb")] IConnectionMultiplexer falkorDb,
        IGraphQueryBuilder graphQueryBuilder,
        ILogger<VerifyConsistencyActivity> logger)
    {
        _redis = redis;
        _falkorDb = falkorDb;
        _graphQueryBuilder = graphQueryBuilder;
        _logger = logger;
    }

    /// <inheritdoc/>
    public override async Task<ConsistencyResult> RunAsync(
        WorkflowActivityContext context,
        ConsistencyInput input)
    {
        ArgumentNullException.ThrowIfNull(input);
        ArgumentException.ThrowIfNullOrWhiteSpace(input.MemoryUnitId);
        ArgumentException.ThrowIfNullOrWhiteSpace(input.TenantId);

        IDatabase redisDb = _redis.GetDatabase();

        // Check syntactic (RediSearch hash key)
        string syntacticKey = $"{input.TenantId}:mu:{input.MemoryUnitId}";
        bool syntacticExists = await redisDb.KeyExistsAsync(syntacticKey).ConfigureAwait(false);

        // Check semantic (Redis Vector hash key)
        string semanticKey = $"{input.TenantId}:vec:{input.MemoryUnitId}";
        bool semanticExists = await redisDb.KeyExistsAsync(semanticKey).ConfigureAwait(false);

        // Check natural-language semantic sibling (Redis Vector hash key)
        string naturalLanguageSemanticKey = $"{input.TenantId}:vec:nl:{input.MemoryUnitId}";
        bool naturalLanguageSemanticExists = await redisDb.KeyExistsAsync(naturalLanguageSemanticKey).ConfigureAwait(false);

        NaturalLanguageEmbeddingStatus naturalLanguageEmbeddingStatus = NaturalLanguageEmbeddingStatus.NotApplicable;
        if (syntacticExists)
        {
            RedisValue metadataJson = await redisDb.HashGetAsync(syntacticKey, "metadataJson").ConfigureAwait(false);
            naturalLanguageEmbeddingStatus = NaturalLanguageConsistencyState.ReadStatus(metadataJson.ToString());
        }

        string? consistencyNote = NaturalLanguageConsistencyState.BuildConsistencyNote(
            naturalLanguageEmbeddingStatus,
            naturalLanguageSemanticExists);
        ConsistencyNoteKind consistencyNoteKind = NaturalLanguageConsistencyState.BuildConsistencyNoteKind(
            naturalLanguageEmbeddingStatus,
            naturalLanguageSemanticExists);

        // Check graph (FalkorDB node)
        bool graphExists = await CheckGraphNodeExistsAsync(input.TenantId, input.MemoryUnitId).ConfigureAwait(false);

        _logger.LogInformation(
            "Consistency check for {MemoryUnitId} in tenant {TenantId}: syntactic={Syntactic}, semantic={Semantic}, semanticNl={SemanticNl}, graph={Graph}, nlStatus={NaturalLanguageStatus}",
            input.MemoryUnitId,
            input.TenantId,
            syntacticExists,
            semanticExists,
            naturalLanguageSemanticExists,
            graphExists,
            naturalLanguageEmbeddingStatus);

        return new ConsistencyResult(syntacticExists, semanticExists, graphExists)
        {
            NaturalLanguageSemanticExists = naturalLanguageSemanticExists,
            NaturalLanguageEmbeddingStatus = naturalLanguageEmbeddingStatus,
            ConsistencyNote = consistencyNote,
            ConsistencyNoteKind = consistencyNoteKind,
        };
    }

    private async Task<bool> CheckGraphNodeExistsAsync(string tenantId, string memoryUnitId)
    {
        FalkorDB falkor = new(_falkorDb.GetDatabase());
        (string query, IDictionary<string, object> parameters) = _graphQueryBuilder.BuildCheckMemoryUnitExists(memoryUnitId);

        ResultSet result = await falkor
            .QueryAsync(tenantId, query, parameters)
            .WaitAsync(GraphOperationTimeout)
            .ConfigureAwait(false);

        return result.Count > 0;
    }
}
