// <copyright file="IImportStagingStore.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Import;

/// <summary>
/// Stages a raw import/restore payload (Story 26.2) so the durable restore workflow can read it back without
/// the multi-hundred-megabyte body ever becoming a Dapr workflow input (workflow inputs are persisted in the
/// state store). Keyed per tenant + workflow instance so cleanup and tenant isolation are trivial.
/// </summary>
internal interface IImportStagingStore
{
    /// <summary>Stages the payload and returns the staging key to hand to the restore workflow.</summary>
    /// <param name="tenantId">The target tenant (used to build a tenant-prefixed, isolated key).</param>
    /// <param name="instanceId">The restore workflow instance id.</param>
    /// <param name="payload">The complete UTF-8 import payload.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The staging key.</returns>
    Task<string> StageAsync(string tenantId, string instanceId, byte[] payload, CancellationToken cancellationToken);

    /// <summary>Retrieves a staged payload, or <see langword="null"/> when the key is absent/expired.</summary>
    /// <param name="stagingKey">The staging key returned by <see cref="StageAsync"/>.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The staged payload bytes, or <see langword="null"/>.</returns>
    Task<byte[]?> RetrieveAsync(string stagingKey, CancellationToken cancellationToken);

    /// <summary>Deletes a staged payload (best-effort cleanup after the restore completes).</summary>
    /// <param name="stagingKey">The staging key.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A task that completes when deletion is attempted.</returns>
    Task DeleteAsync(string stagingKey, CancellationToken cancellationToken);
}
