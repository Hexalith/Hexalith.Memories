// <copyright file="MemoriesFilter.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Web.Components.Filters;

/// <summary>
/// A single active filter as supplied by the host, before inspection mapping.
/// </summary>
/// <remarks>
/// Story 17.3 (AC2) — the host supplies the axis, the raw value/operator token, the trust effect it knows
/// from the contract, and whether the token is contract-recognized. The inspection mapper sanitizes the
/// value and, for unrecognized tokens, downgrades the chip to an unavailable contract-boundary state.
/// </remarks>
/// <param name="Axis">The filter axis.</param>
/// <param name="ValueToken">The raw filter value or operator token (sanitized for display by the mapper).</param>
/// <param name="Effect">The trust effect the host derived from the contract for this filter.</param>
/// <param name="IsContractKnown">Whether the value/operator is a contract-recognized token.</param>
public sealed record MemoriesFilter(
    MemoriesFilterAxis Axis,
    string ValueToken,
    MemoriesFilterEffect Effect,
    bool IsContractKnown);
