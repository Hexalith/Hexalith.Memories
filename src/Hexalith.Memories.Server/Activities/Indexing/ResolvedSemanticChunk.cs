// <copyright file="ResolvedSemanticChunk.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Activities.Indexing;

/// <summary>Semantic chunk payload resolved inside the indexing activity boundary.</summary>
internal sealed record ResolvedSemanticChunk(
    string Text,
    float[] Vector,
    int Sequence,
    int StartOffset,
    int EndOffset);
