// <copyright file="IdempotencyResult.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Activities.Ingestion;

/// <summary>Result of the idempotency check indicating whether the source was already ingested.</summary>
/// <param name="IsDuplicate">Whether a duplicate was detected.</param>
/// <param name="ExistingMemoryUnitId">The existing memory unit ID if duplicate; null otherwise.</param>
/// <param name="IsSuperseded">Whether the same revision token names an older non-current output.</param>
public sealed record IdempotencyResult(bool IsDuplicate, string? ExistingMemoryUnitId, bool IsSuperseded = false);
