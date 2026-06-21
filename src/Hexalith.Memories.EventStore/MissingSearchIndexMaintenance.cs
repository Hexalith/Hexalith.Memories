// <copyright file="MissingSearchIndexMaintenance.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.EventStore;

using Hexalith.Memories.Contracts.V1;

/// <summary>Placeholder search-index maintenance used until the host supplies a concrete adapter. Curated
/// search-index events only arrive when a producer is publishing them against a configured index, so a
/// missing adapter is a host mis-configuration and must fail loudly rather than silently drop updates.</summary>
internal sealed class MissingSearchIndexMaintenance : ISearchIndexMaintenance
{
    private const string Guidance =
        "EventStore integration received a curated search-index event but no concrete ISearchIndexMaintenance "
        + "is registered. Register one by calling AddMemoriesEventStoreIntegration(..., builder => "
        + "builder.AddSearchIndexMaintenance<TImplementation>()).";

    public Task ApplyEntryChangedAsync(
        string indexTenantId,
        string sourceUri,
        SearchIndexEntryChanged entry,
        string? caseId,
        CancellationToken cancellationToken)
        => Task.FromException(new InvalidOperationException(Guidance));

    public Task ApplyEntryRemovedAsync(
        string indexTenantId,
        SearchIndexEntryRemoved entry,
        CancellationToken cancellationToken)
        => Task.FromException(new InvalidOperationException(Guidance));
}
