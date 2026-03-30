// <copyright file="CleanupInput.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Activities.Indexing;

/// <summary>Input for cleanup/compensation activities.</summary>
/// <param name="MemoryUnitId">The memory unit to clean up.</param>
/// <param name="TenantId">The tenant identifier for namespacing.</param>
public sealed record CleanupInput(string MemoryUnitId, string TenantId);
