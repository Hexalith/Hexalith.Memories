// <copyright file="ExportedMemoryUnit.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Contracts.V1;

/// <summary>
/// Sealed wrapper that composes a canonical <see cref="MemoryUnit"/> with the export-scoped
/// inbound annotation projection (Story 8.3). The domain <see cref="MemoryUnit"/> record stays
/// pure — export bookkeeping lives on this wrapper, not on the domain type.
/// </summary>
/// <param name="Unit">The memory unit record with full metadata.</param>
/// <param name="AnnotationTargets">
/// Identifiers of annotation memory units that target <paramref name="Unit"/> via the
/// <c>ANNOTATES</c> edge. Empty list (never <see langword="null"/>) when the unit has no inbound
/// annotations; deserializers must treat the list as non-nullable.
/// </param>
public sealed record ExportedMemoryUnit(
    MemoryUnit Unit,
    IReadOnlyList<string> AnnotationTargets);
