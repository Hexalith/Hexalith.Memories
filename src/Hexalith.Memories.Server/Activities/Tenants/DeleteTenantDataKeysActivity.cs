// <copyright file="DeleteTenantDataKeysActivity.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Activities.Tenants;

using Dapr.Workflow;

using Hexalith.Memories.Contracts.V1;
using Hexalith.Memories.EventStore;
using Hexalith.Memories.Server.Infrastructure;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

using StackExchange.Redis;

/// <summary>Tenant deletion activity that cleans up Redis data keys not covered by FT.DROPINDEX DD
/// and purges Dapr-state EventStore mappings/observations (review D3 / ADR-IDA-001).</summary>
public sealed partial class DeleteTenantDataKeysActivity : WorkflowActivity<TenantDeletionInput, bool>
{
    private readonly IConnectionMultiplexer _redis;
    private readonly IAggregateCaseMappingStore _aggregateCaseMappingStore;
    private readonly IObservedEventTypeStore _observedEventTypeStore;
    private readonly IHostApplicationLifetime _hostApplicationLifetime;
    private readonly ILogger<DeleteTenantDataKeysActivity> _logger;

    /// <summary>Initializes a new instance of the <see cref="DeleteTenantDataKeysActivity"/> class.</summary>
    /// <param name="redis">The Redis connection multiplexer.</param>
    /// <param name="aggregateCaseMappingStore">Dapr-state aggregate→case mapping store.</param>
    /// <param name="observedEventTypeStore">Dapr-state observed-event-type store.</param>
    /// <param name="hostApplicationLifetime">Host lifetime used as the activity cancellation source
    /// (<see cref="WorkflowActivityContext"/> does not surface a <see cref="CancellationToken"/>).</param>
    /// <param name="logger">The logger instance.</param>
    public DeleteTenantDataKeysActivity(
        [FromKeyedServices("redis")] IConnectionMultiplexer redis,
        IAggregateCaseMappingStore aggregateCaseMappingStore,
        IObservedEventTypeStore observedEventTypeStore,
        IHostApplicationLifetime hostApplicationLifetime,
        ILogger<DeleteTenantDataKeysActivity> logger)
    {
        _redis = redis;
        _aggregateCaseMappingStore = aggregateCaseMappingStore;
        _observedEventTypeStore = observedEventTypeStore;
        _hostApplicationLifetime = hostApplicationLifetime;
        _logger = logger;
    }

    /// <inheritdoc/>
    public override async Task<bool> RunAsync(WorkflowActivityContext context, TenantDeletionInput input)
    {
        ArgumentNullException.ThrowIfNull(input);
        CancellationToken cancellationToken = _hostApplicationLifetime.ApplicationStopping;
        IDatabase db = _redis.GetDatabase();
        IServer server = _redis.GetServers().FirstOrDefault(static s => s.IsConnected)
            ?? throw new InvalidOperationException("No Redis server is available for tenant data cleanup.");

        long totalDeleted = 0;
        totalDeleted += await ScanAndDeleteAsync(server, db, $"{input.TenantId}:case:*").ConfigureAwait(false);
        totalDeleted += await ScanAndDeleteAsync(server, db, $"dedup:{input.TenantId}:*").ConfigureAwait(false);
        // Raw Redis eventstore:* keys remain for the direct-Redis dedup store and any pre-cutover orphans
        // (ADR-IDA-001 greenfield cutover). Dapr-state keys carry the app-id prefix and are purged below.
        totalDeleted += await ScanAndDeleteAsync(server, db, $"{input.TenantId}:eventstore:*").ConfigureAwait(false);
        totalDeleted += await ScanAndDeleteAsync(server, db, $"{input.TenantId}:embedding-migration:*").ConfigureAwait(false);
        totalDeleted += await ScanAndDeleteAsync(server, db, IndexSchemaDefinitions.GetSyntacticKeyPrefix(input.TenantId) + "*").ConfigureAwait(false);
        totalDeleted += await ScanAndDeleteAsync(server, db, IndexSchemaDefinitions.GetSemanticKeyPrefix(input.TenantId) + "*").ConfigureAwait(false);
        totalDeleted += await ScanAndDeleteAsync(server, db, IndexSchemaDefinitions.GetNaturalLanguageSemanticKeyPrefix(input.TenantId) + "*").ConfigureAwait(false);
        totalDeleted += await ScanAndDeleteAsync(server, db, IndexSchemaDefinitions.GetLegacyNaturalLanguageSemanticKeyPrefix(input.TenantId) + "*").ConfigureAwait(false);

        await _aggregateCaseMappingStore
            .DeleteAllTenantDataAsync(input.TenantId, cancellationToken)
            .ConfigureAwait(false);
        await _observedEventTypeStore
            .DeleteAllTenantDataAsync(input.TenantId, cancellationToken)
            .ConfigureAwait(false);

        LogKeysDeleted(_logger, input.TenantId, totalDeleted);
        return true;
    }

    private static async Task<long> ScanAndDeleteAsync(IServer server, IDatabase db, string pattern)
    {
        long deleted = 0;
        List<RedisKey> batch = new(1000);

        await foreach (RedisKey key in server.KeysAsync(database: db.Database, pattern: pattern, pageSize: 1000).ConfigureAwait(false))
        {
            batch.Add(key);

            if (batch.Count >= 1000)
            {
                deleted += await db.KeyDeleteAsync([.. batch]).ConfigureAwait(false);
                batch.Clear();
            }
        }

        if (batch.Count > 0)
        {
            deleted += await db.KeyDeleteAsync([.. batch]).ConfigureAwait(false);
        }

        return deleted;
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Deleted {Count} data keys for tenant '{TenantId}'")]
    private static partial void LogKeysDeleted(ILogger logger, string tenantId, long count);
}
