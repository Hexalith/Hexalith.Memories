// <copyright file="DerivedStoreBindingEntry.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Contracts.V1.DerivedStores;

/// <summary>Identifies one canonical MemoryUnit in an ordered finalized ingestion binding.</summary>
/// <param name="RecordKind">The governed record kind.</param>
/// <param name="Ordinal">The zero-based governed ordinal.</param>
/// <param name="MemoryUnitId">The existing canonical MemoryUnit identifier.</param>
public sealed record DerivedStoreBindingEntry(
    DerivedStoreRecordKind RecordKind,
    int Ordinal,
    string MemoryUnitId);
