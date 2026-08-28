// <copyright file="DiagnosticStoreClass.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Contracts.V1.DerivedStores;

using System.Text.Json.Serialization;

using Hexalith.Memories.Contracts.V1;

/// <summary>Identifies a metadata-only diagnostic category. Values do not represent canonical Memories index families.</summary>
[JsonConverter(typeof(CamelCaseStringEnumConverter<DiagnosticStoreClass>))]
public enum DiagnosticStoreClass
{
    /// <summary>The vector-index governance probe category.</summary>
    VectorIndex,

    /// <summary>The embedding-store governance probe category.</summary>
    EmbeddingStore,

    /// <summary>The prompt-context-cache governance probe category.</summary>
    PromptContextCache,

    /// <summary>The candidate-ranking-cache governance probe category.</summary>
    CandidateRankingCache,
}
