// <copyright file="FilterChipAvailability.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Web.Components.Filters;

/// <summary>
/// Whether an inspectable filter chip represents a usable filter or an unavailable contract-boundary state.
/// </summary>
/// <remarks>
/// Story 17.3 (AC2) — unknown or future filter operators, confidence values, evidence states, graph depths,
/// source types, and malformed filter metadata must render as unavailable contract-boundary states with a
/// visible explanation, not be coerced into a known label or shown as a successful empty result.
/// </remarks>
public enum FilterChipAvailability
{
    /// <summary>The filter is a known, contract-recognized constraint.</summary>
    Available = 0,

    /// <summary>The filter operator or value is unknown to the contract and is shown as a disabled boundary state.</summary>
    Unavailable,
}
