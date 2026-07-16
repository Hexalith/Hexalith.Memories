// <copyright file="RestoreDataPlaneInput.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Workflows.Contracts;

/// <summary>Input to <c>RestoreDataPlaneActivity</c> — restores every byte-exact data-plane artifact from the staged export.</summary>
/// <param name="TenantId">The target tenant.</param>
/// <param name="CaseId">The target case for a case-scoped restore; <see langword="null"/> for tenant scope.</param>
/// <param name="StagingKey">The staged export payload key.</param>
/// <param name="RequestedBy">The requesting principal (audit).</param>
public sealed record RestoreDataPlaneInput(
    string TenantId,
    string? CaseId,
    string StagingKey,
    string RequestedBy);
