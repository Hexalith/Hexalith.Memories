// <copyright file="IFailedNaturalLanguageEmbeddingRegistry.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.NaturalLanguage;

using Hexalith.Memories.Contracts.V1;

/// <summary>Story 9.2 Task 8.2 — Redis-backed queue surface for NL embedding retries. Parallels
/// <c>FailedUnitsRegistry</c> (Story 6.3) in shape; exists as an interface because it crosses the
/// Redis boundary (testability).</summary>
public interface IFailedNaturalLanguageEmbeddingRegistry
{
    /// <summary>Adds or replaces a live retry entry keyed by <c>MemoryUnitId</c> in
    /// <c>nl-embedding-retry:{tenantId}</c> and stores the payload in a companion Redis hash.</summary>
    /// <param name="record">The record to enqueue.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    Task EnqueueAsync(FailedNaturalLanguageEmbeddingRecord record, CancellationToken cancellationToken);

    /// <summary>Dequeues up to <paramref name="batchSize"/> records (oldest first) for the given tenant.</summary>
    /// <param name="tenantId">The tenant identifier.</param>
    /// <param name="batchSize">Maximum records to fetch.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The batched records.</returns>
    Task<IReadOnlyList<FailedNaturalLanguageEmbeddingRecord>> DequeueBatchAsync(
        string tenantId,
        int batchSize,
        CancellationToken cancellationToken);

    /// <summary>Removes a record from the retry queue (retry succeeded).</summary>
    /// <param name="record">The record to remove.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    Task CompleteAsync(FailedNaturalLanguageEmbeddingRecord record, CancellationToken cancellationToken);

    /// <summary>Increments attempts; moves to <c>nl-embedding-retry-dead:{tenantId}</c> when the attempt
    /// count reaches <paramref name="maxAttempts"/>.</summary>
    /// <param name="record">The record (with its current attempts).</param>
    /// <param name="maxAttempts">The dead-letter threshold from options.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns><see langword="true"/> when the record moved to the dead-letter set.</returns>
    Task<bool> IncrementAttemptsAsync(
        FailedNaturalLanguageEmbeddingRecord record,
        int maxAttempts,
        CancellationToken cancellationToken);

    /// <summary>Returns the current backlog count for a tenant (<c>ZCARD</c>).</summary>
    /// <param name="tenantId">The tenant identifier.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The backlog count.</returns>
    Task<long> GetBacklogCountAsync(string tenantId, CancellationToken cancellationToken);

    /// <summary>Reports the backing sorted-set and payload-hash memory footprint (bytes) via
    /// <c>MEMORY USAGE</c>.</summary>
    /// <param name="tenantId">The tenant identifier.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The memory usage in bytes (0 when unavailable).</returns>
    Task<long> GetBacklogBytesAsync(string tenantId, CancellationToken cancellationToken);

    /// <summary>Enumerates tenants that currently have at least one backlog entry via the tenant backlog set.</summary>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The tenant identifiers.</returns>
    IAsyncEnumerable<string> ListTenantsWithBacklogAsync(CancellationToken cancellationToken);
}
