// <copyright file="RestoreReindexBatchActivity.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Activities.Restore;

using Dapr.Workflow;

using Hexalith.Memories.Server.Import;

/// <summary>Re-indexes one bounded page while keeping its identifier list outside workflow history.</summary>
internal sealed class RestoreReindexBatchActivity
    : WorkflowActivity<RestoreReindexBatchInput, RestoreReindexBatchResult>
{
    private readonly IRestoreReindexUnitProcessor _reindexUnit;
    private readonly IImportStagingStore _stagingStore;

    /// <summary>Initializes a new instance of the <see cref="RestoreReindexBatchActivity"/> class.</summary>
    public RestoreReindexBatchActivity(
        IImportStagingStore stagingStore,
        IRestoreReindexUnitProcessor reindexUnit)
    {
        _stagingStore = stagingStore;
        _reindexUnit = reindexUnit;
    }

    /// <inheritdoc/>
    public override async Task<RestoreReindexBatchResult> RunAsync(
        WorkflowActivityContext context,
        RestoreReindexBatchInput input)
    {
        ArgumentNullException.ThrowIfNull(input);
        if (input.BatchSize is <= 0 or > 100)
        {
            throw new ArgumentOutOfRangeException(nameof(input), "Restore re-index batch size must be between 1 and 100.");
        }

        await _stagingStore.RenewAsync(input.StagingKey, CancellationToken.None).ConfigureAwait(false);
        if (!await _stagingStore.OwnsRestoreLeaseAsync(input.StagingKey, CancellationToken.None).ConfigureAwait(false))
        {
            throw new ImportEnvelopeException(
                "RESTORE_LEASE_LOST",
                "The restore operation lost its target lease while re-indexing.");
        }

        IReadOnlyList<string> ids = await _stagingStore
            .ReadReindexIdsAsync(input.StagingKey, input.Offset, input.BatchSize, CancellationToken.None)
            .ConfigureAwait(false);
        if (ids.Count != input.BatchSize)
        {
            throw new InvalidOperationException(
                $"Restore re-index page at offset {input.Offset} expected {input.BatchSize} ids but found {ids.Count} in staging '{input.StagingKey}'.");
        }

        int chunks = 0;
        foreach (string memoryUnitId in ids)
        {
            RestoreReindexResult result = await _reindexUnit
                .ReindexOneAsync(new RestoreReindexInput(input.TenantId, memoryUnitId))
                .ConfigureAwait(false);
            chunks += result.ChunkCount;
        }

        return new RestoreReindexBatchResult(ids.Count, chunks);
    }
}
