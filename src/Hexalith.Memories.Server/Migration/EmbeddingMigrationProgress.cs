// <copyright file="EmbeddingMigrationProgress.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Migration;

/// <summary>Per-batch operator-visible migration progress.</summary>
/// <param name="TenantId">The tenant identifier.</param>
/// <param name="ContentKind">The migrated content kind.</param>
/// <param name="BatchNumber">The one-based batch number.</param>
/// <param name="ProcessedCount">The processed unit count.</param>
/// <param name="SkippedCount">The skipped unit count.</param>
/// <param name="MissingCount">The missing unit count.</param>
/// <param name="FailedCount">The failed unit count.</param>
/// <param name="TotalCount">The total unit count for the content kind.</param>
/// <param name="Percent">The completed percentage.</param>
/// <param name="Elapsed">The elapsed migration time.</param>
public sealed record EmbeddingMigrationProgress(
    string TenantId,
    string ContentKind,
    int BatchNumber,
    int ProcessedCount,
    int SkippedCount,
    int MissingCount,
    int FailedCount,
    int TotalCount,
    double Percent,
    TimeSpan Elapsed);
