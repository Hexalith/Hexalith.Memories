// <copyright file="EmbeddingMigrationUnitFailure.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Migration;

/// <summary>Durable per-unit migration failure detail.</summary>
/// <param name="TenantId">The tenant identifier.</param>
/// <param name="MemoryUnitId">The memory unit identifier.</param>
/// <param name="ContentKind">The failed content kind.</param>
/// <param name="ErrorCategory">The error category.</param>
/// <param name="Message">The sanitized and truncated error message.</param>
public sealed record EmbeddingMigrationUnitFailure(
    string TenantId,
    string MemoryUnitId,
    string ContentKind,
    string ErrorCategory,
    string Message);
