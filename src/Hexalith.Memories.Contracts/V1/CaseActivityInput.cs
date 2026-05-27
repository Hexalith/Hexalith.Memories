// <copyright file="CaseActivityInput.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Contracts.V1;

/// <summary>Input for the DAPR workflow activity that records a case activity event.</summary>
public sealed record CaseActivityInput(
    string TenantId,
    string CaseId,
    CaseActivityEventType EventType,
    string Actor,
    string Description,
    string? MemoryUnitId);
