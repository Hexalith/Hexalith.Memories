// <copyright file="RestoreTargetGuard.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Import;

using Hexalith.Memories.Server.Graph;
using Hexalith.Memories.Server.Infrastructure;
using Hexalith.Memories.Server.Search;

using Microsoft.Extensions.DependencyInjection;

using NRedisStack.RedisStackCommands;
using NRedisStack.Search;

using StackExchange.Redis;

using RedisSearchResult = NRedisStack.Search.SearchResult;

/// <summary>Redis/RediSearch/FalkorDB implementation of the clean-target restore invariant.</summary>
internal sealed class RestoreTargetGuard : IRestoreTargetGuard
{
    private readonly IConnectionMultiplexer _falkorDb;
    private readonly IGraphQueryBuilder _queryBuilder;
    private readonly IConnectionMultiplexer _redis;

    /// <summary>Initializes a new instance of the <see cref="RestoreTargetGuard"/> class.</summary>
    public RestoreTargetGuard(
        [FromKeyedServices("redis")] IConnectionMultiplexer redis,
        [FromKeyedServices("falkordb")] IConnectionMultiplexer falkorDb,
        IGraphQueryBuilder queryBuilder)
    {
        _redis = redis;
        _falkorDb = falkorDb;
        _queryBuilder = queryBuilder;
    }

    /// <inheritdoc/>
    public async Task EnsureCleanAsync(string tenantId, string? caseId, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        cancellationToken.ThrowIfCancellationRequested();

        IDatabase db = _redis.GetDatabase();
        string queryText = caseId is null
            ? "*"
            : $"@caseId:{{{RediSearchQueryEscaper.EscapeTag(caseId)}}}";
        RedisSearchResult search = await db.FT()
            .SearchAsync(
                IndexSchemaDefinitions.GetSyntacticIndexName(tenantId),
                new Query(queryText).Limit(0, 0).Dialect(2))
            .ConfigureAwait(false);

        bool caseHashExists = caseId is not null
            && (await db.KeyExistsAsync($"{tenantId}:case:{caseId}").ConfigureAwait(false)
                || await db.KeyExistsAsync($"{tenantId}:case:{caseId}:members").ConfigureAwait(false));

        long graphArtifacts = await CountGraphArtifactsAsync(tenantId, caseId).ConfigureAwait(false);
        if (search.TotalResults > 0 || caseHashExists || graphArtifacts > 0)
        {
            string scope = caseId is null ? $"tenant '{tenantId}'" : $"case '{caseId}' in tenant '{tenantId}'";
            throw new ImportEnvelopeException(
                "RESTORE_TARGET_NOT_CLEAN",
                $"Restore target {scope} is not clean ({search.TotalResults} indexed units, {graphArtifacts} graph artifacts). Remove or relocate existing data before restoring.");
        }
    }

    private async Task<long> CountGraphArtifactsAsync(string tenantId, string? caseId)
    {
        try
        {
            NFalkorDB.FalkorDB falkor = new(_falkorDb.GetDatabase());
            (string query, IDictionary<string, object> parameters) = caseId is null
                ? _queryBuilder.BuildCountAllNodes()
                : _queryBuilder.BuildCountCaseRestoreArtifacts(caseId);
            NFalkorDB.ResultSet result = await falkor.SelectGraph(tenantId).QueryAsync(query, parameters).ConfigureAwait(false);
            NFalkorDB.Record? record = result.FirstOrDefault();
            if (record is null
                || record.Values.Count == 0
                || !long.TryParse(record.Values[0]?.ToString(), out long count))
            {
                throw new InvalidOperationException("FalkorDB returned an unreadable restore target count.");
            }

            return count;
        }
        catch (RedisServerException ex) when (ex.Message.Contains("Graph not found", StringComparison.OrdinalIgnoreCase))
        {
            return 0;
        }
    }
}
