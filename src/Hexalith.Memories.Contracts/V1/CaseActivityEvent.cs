// <copyright file="CaseActivityEvent.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Contracts.V1;

using System.Text.Json.Serialization;

/// <summary>Represents a single activity event recorded in a case's activity stream.</summary>
public sealed record CaseActivityEvent(
    string Id,
    DateTimeOffset Timestamp,
    CaseActivityEventType EventType,
    string Actor,
    string Description,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    string? MemoryUnitId);
