// <copyright file="FilterAxisTrace.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Web.Components.Filters;

/// <summary>
/// A traceability row binding a <see cref="MemoriesFilterAxis"/> to its label key and the named Evidence
/// Packet contract fields it constrains.
/// </summary>
/// <remarks>
/// Story 17.3 (Task 0, AC2) — the filter half of the interaction traceability table. Axes with no
/// side-channel-safe contract source record <see cref="FilterAxisTraceability.NoContractSource"/> instead
/// of inventing a backing field.
/// </remarks>
/// <param name="Axis">The filter axis.</param>
/// <param name="AxisLabelKey">Localization key for the axis label.</param>
/// <param name="ContractSources">The named contract fields the axis constrains.</param>
public sealed record FilterAxisTrace(
    MemoriesFilterAxis Axis,
    string AxisLabelKey,
    IReadOnlyList<string> ContractSources);
