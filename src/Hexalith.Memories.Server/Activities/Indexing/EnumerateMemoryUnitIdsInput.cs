// <copyright file="EnumerateMemoryUnitIdsInput.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Activities.Indexing;

/// <summary>
/// Input for <c>EnumerateMemoryUnitIdsActivity</c>.
/// </summary>
/// <param name="TenantId">The tenant whose memory units to enumerate.</param>
/// <param name="MaxUnits">
/// Soft cap on the returned IDs. Applied AFTER the three-backend union to prevent DAPR
/// workflow state bloat. Default 50,000 per Task 1.2a.
/// </param>
public sealed record EnumerateMemoryUnitIdsInput(string TenantId, int MaxUnits = 50_000);
