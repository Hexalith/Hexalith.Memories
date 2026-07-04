// <copyright file="MemoryUnitDeletionProjectionInput.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Activities.Cases;

/// <summary>Input for a workflow-owned memory-unit cleanup projection.</summary>
internal sealed record MemoryUnitDeletionProjectionInput(
    string TenantId,
    string CaseId,
    string MemoryUnitId,
    IReadOnlyList<string> AnnotationMemoryUnitIds);
