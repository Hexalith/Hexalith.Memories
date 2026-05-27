// <copyright file="NaturalLanguageEmbeddingRetryResult.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Contracts.V1;

/// <summary>Story 9.2 Task 8.3 — outcome of <c>NaturalLanguageEmbeddingRetryWorkflow</c>.</summary>
/// <param name="Indexed"><see langword="true"/> when the NL hash was successfully written on this retry.</param>
/// <param name="Reason">Optional reason code for the failure path (<c>"llm-still-unavailable"</c>,
/// <c>"memory-unit-deleted-during-retry"</c>).</param>
public sealed record NaturalLanguageEmbeddingRetryResult(
    bool Indexed,
    string? Reason = null);
