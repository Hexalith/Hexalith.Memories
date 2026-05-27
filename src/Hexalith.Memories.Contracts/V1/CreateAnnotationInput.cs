// <copyright file="CreateAnnotationInput.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Contracts.V1;

/// <summary>Input payload for creating an annotation on an existing memory unit.</summary>
public sealed record CreateAnnotationInput(
    string TenantId,
    string CaseId,
    string TargetMemoryUnitId,
    string Content,
    string IngestedBy,
    string? AnnotationType = null);
