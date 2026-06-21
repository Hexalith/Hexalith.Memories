// <copyright file="CuratedSearchIndexEventTypes.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.EventStore;

using Hexalith.Memories.Contracts.V1;

/// <summary>The CloudEvent <c>type</c> values that drive curated search-index maintenance instead of the
/// generic raw-event ingestion workflow. The type is the contract name, matching what producers set on the
/// CloudEvent envelope (see the Tenants <c>MemoriesSearchIndexEventPublisher</c>).</summary>
internal static class CuratedSearchIndexEventTypes
{
    /// <summary>The <c>type</c> of an upsert (create or revise) curated entry event.</summary>
    internal const string Changed = nameof(SearchIndexEntryChanged);

    /// <summary>The <c>type</c> of a delete curated entry event.</summary>
    internal const string Removed = nameof(SearchIndexEntryRemoved);

    /// <summary>Determines whether a CloudEvent <c>type</c> denotes curated search-index maintenance.</summary>
    /// <param name="cloudEventType">The CloudEvent <c>type</c> value.</param>
    /// <returns><see langword="true"/> when the event must be routed to <see cref="ISearchIndexMaintenance"/>.</returns>
    internal static bool IsCuratedType(string? cloudEventType)
        => string.Equals(cloudEventType, Changed, StringComparison.Ordinal)
        || string.Equals(cloudEventType, Removed, StringComparison.Ordinal);
}
