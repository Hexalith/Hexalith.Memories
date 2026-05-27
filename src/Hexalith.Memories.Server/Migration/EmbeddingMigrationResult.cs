// <copyright file="EmbeddingMigrationResult.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Migration;

/// <summary>Full migration command result.</summary>
/// <param name="Mode">The selected migration mode.</param>
/// <param name="ExitCode">The process exit code.</param>
/// <param name="Message">The operator-facing summary message.</param>
/// <param name="Elapsed">The elapsed command time.</param>
/// <param name="Tenants">The tenant reports.</param>
/// <param name="Failures">The sanitized per-unit failures.</param>
/// <param name="Progress">The emitted progress records.</param>
public sealed record EmbeddingMigrationResult(
    EmbeddingMigrationMode Mode,
    int ExitCode,
    string Message,
    TimeSpan Elapsed,
    IReadOnlyList<EmbeddingMigrationTenantReport> Tenants,
    IReadOnlyList<EmbeddingMigrationUnitFailure> Failures,
    IReadOnlyList<EmbeddingMigrationProgress> Progress);
