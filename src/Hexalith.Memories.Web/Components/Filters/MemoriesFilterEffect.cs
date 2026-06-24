// <copyright file="MemoriesFilterEffect.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Web.Components.Filters;

/// <summary>
/// How an active filter changes the trust meaning of the result set.
/// </summary>
/// <remarks>
/// Story 17.3 (AC2) — filters are trust modifiers. A filter that hides graph evidence, excludes an axis,
/// narrows sources, broadens case scope, or hides stale/conflicting evidence must say so near the affected
/// filter summary instead of silently changing the answer.
/// </remarks>
public enum MemoriesFilterEffect
{
    /// <summary>No trust-affecting effect.</summary>
    None = 0,

    /// <summary>Narrows the scope of the result set.</summary>
    NarrowsScope,

    /// <summary>Broadens the scope of the result set.</summary>
    BroadensScope,

    /// <summary>Excludes a retrieval axis from contributing evidence.</summary>
    ExcludesAxis,

    /// <summary>Changes graph traversal depth.</summary>
    ChangesGraphDepth,

    /// <summary>Hides stale or conflicting evidence.</summary>
    HidesStaleOrConflicting,

    /// <summary>Affects how confidence should be interpreted.</summary>
    AffectsConfidence,
}
