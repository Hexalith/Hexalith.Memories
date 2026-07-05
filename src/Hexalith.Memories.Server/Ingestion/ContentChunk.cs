// <copyright file="ContentChunk.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Ingestion;

/// <summary>Deterministic raw payload chunk produced before embedding.</summary>
/// <param name="Sequence">Zero-based chunk sequence.</param>
/// <param name="Text">The non-empty chunk text.</param>
/// <param name="StartOffset">Inclusive UTF-16 character offset in the source text.</param>
/// <param name="EndOffset">Exclusive UTF-16 character offset in the source text.</param>
/// <param name="EstimatedTokens">The conservative token estimate for <paramref name="Text"/>.</param>
public sealed record ContentChunk(
    int Sequence,
    string Text,
    int StartOffset,
    int EndOffset,
    int EstimatedTokens);
