// <copyright file="CreateCaseInput.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Contracts.V1;

/// <summary>Input payload for creating a new case within a tenant.</summary>
public sealed record CreateCaseInput(
    string TenantId,
    string Name,
    string? Description);
