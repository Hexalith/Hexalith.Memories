// <copyright file="Case.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Contracts.V1;

using System.Text.Json.Serialization;

/// <summary>Represents a case that groups memory units within a tenant.</summary>
public sealed record Case(
    string Id,
    string TenantId,
    string Name,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    string? Description,
    CaseStatus Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset LastUpdated,
    int MemoryUnitCount);
