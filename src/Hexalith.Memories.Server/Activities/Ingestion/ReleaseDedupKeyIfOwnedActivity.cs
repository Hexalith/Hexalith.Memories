// <copyright file="ReleaseDedupKeyIfOwnedActivity.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Activities.Ingestion;

using Dapr.Workflow;

using StackExchange.Redis;

/// <summary>DAPR Workflow activity that removes a permanent dedup key only when it is still owned by this workflow.</summary>
public sealed class ReleaseDedupKeyIfOwnedActivity : WorkflowActivity<DedupKeyInput, bool>
{
    private readonly IConnectionMultiplexer _redis;

    public ReleaseDedupKeyIfOwnedActivity([FromKeyedServices("redis")] IConnectionMultiplexer redis)
    {
        _redis = redis;
    }

    /// <inheritdoc/>
    public override async Task<bool> RunAsync(
        WorkflowActivityContext context,
        DedupKeyInput input)
    {
        ArgumentNullException.ThrowIfNull(input);
        ArgumentException.ThrowIfNullOrWhiteSpace(input.DedupKey);
        ArgumentException.ThrowIfNullOrWhiteSpace(input.MemoryUnitId);

        IDatabase db = _redis.GetDatabase();
        ITransaction transaction = db.CreateTransaction();
        transaction.AddCondition(Condition.StringEqual(input.DedupKey, input.MemoryUnitId));
        Task<bool> deleteTask = transaction.KeyDeleteAsync(input.DedupKey, CommandFlags.None);

        bool committed = await transaction.ExecuteAsync(CommandFlags.None).ConfigureAwait(false);
        return committed && await deleteTask.ConfigureAwait(false);
    }
}
