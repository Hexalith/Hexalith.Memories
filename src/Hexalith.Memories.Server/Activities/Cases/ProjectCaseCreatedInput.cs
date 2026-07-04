// <copyright file="ProjectCaseCreatedInput.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Activities.Cases;

/// <summary>Projection input for a created case.</summary>
internal sealed record ProjectCaseCreatedInput(
    string TenantId,
    string CaseId,
    string Name,
    string? Description,
    DateTimeOffset CreatedAt);
