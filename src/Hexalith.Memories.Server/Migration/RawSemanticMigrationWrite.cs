// <copyright file="RawSemanticMigrationWrite.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Migration;

/// <summary>Raw semantic hash write payload.</summary>
/// <param name="MemoryUnitId">The memory unit identifier.</param>
/// <param name="CaseId">The case identifier.</param>
/// <param name="CloudEventSubject">The optional CloudEvent subject.</param>
/// <param name="Embedding">The generated embedding vector.</param>
public sealed record RawSemanticMigrationWrite(
    string MemoryUnitId,
    string CaseId,
    string? CloudEventSubject,
    float[] Embedding);
