// <copyright file="IIngestionWorkflowInFlightRegistry.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Ingestion;

/// <summary>Tracks app-scheduled ingestion workflow instances for replay-safety startup checks.</summary>
internal interface IIngestionWorkflowInFlightRegistry
{
    /// <summary>Adds or refreshes an in-flight workflow entry.</summary>
    /// <param name="entry">The in-flight workflow entry.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task TrackAsync(IngestionWorkflowInFlightEntry entry, CancellationToken cancellationToken);

    /// <summary>Lists tracked in-flight workflow candidates for Dapr status checks.</summary>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The tracked entries ordered from oldest to newest.</returns>
    Task<IReadOnlyList<IngestionWorkflowInFlightEntry>> ListAsync(CancellationToken cancellationToken);

    /// <summary>Gets a value indicating whether the registry has been initialized by this application version.</summary>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns><see langword="true"/> when the registry can be trusted as the replay-safety source.</returns>
    Task<bool> IsInitializedAsync(CancellationToken cancellationToken);

    /// <summary>Marks the registry as initialized for subsequent replay-safety checks.</summary>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task MarkInitializedAsync(CancellationToken cancellationToken);

    /// <summary>Removes a tracked workflow instance.</summary>
    /// <param name="instanceId">The workflow instance identifier.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task RemoveAsync(string instanceId, CancellationToken cancellationToken);
}
