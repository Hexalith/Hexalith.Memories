// <copyright file="CaseGroupSummary.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Contracts.V1;

/// <summary>
/// Summary of search results grouped by case, providing case-level distribution metadata.
/// </summary>
/// <param name="CaseId">The case identifier.</param>
/// <param name="CaseName">The human-readable case name.</param>
/// <param name="ResultCount">The number of search results from this case.</param>
public sealed record CaseGroupSummary(string CaseId, string CaseName, int ResultCount);
