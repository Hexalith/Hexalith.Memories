// <copyright file="SyntacticMigrationUnit.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Migration;

/// <summary>Authoritative syntactic memory-unit data used for raw re-embedding.</summary>
/// <param name="MemoryUnitId">The memory unit identifier.</param>
/// <param name="Content">The persisted raw content.</param>
/// <param name="CaseId">The case identifier.</param>
/// <param name="CloudEventSubject">The optional CloudEvent subject.</param>
public sealed record SyntacticMigrationUnit(string MemoryUnitId, string? Content, string? CaseId, string? CloudEventSubject);
