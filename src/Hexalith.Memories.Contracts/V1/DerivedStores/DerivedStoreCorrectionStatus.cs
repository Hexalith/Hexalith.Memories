// <copyright file="DerivedStoreCorrectionStatus.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Contracts.V1.DerivedStores;

/// <summary>Provides metadata-only durable correction status and convergence evidence.</summary>
/// <param name="OperationId">The deterministic tenant-scoped operation identifier.</param>
/// <param name="State">The durable lifecycle state.</param>
/// <param name="AssociationId">The governed association identifier.</param>
/// <param name="IntakeId">The governed intake identifier.</param>
/// <param name="CorrectionId">The governed correction identifier.</param>
/// <param name="SourceVersion">The correction source version.</param>
/// <param name="PriorCaseId">The prior case resolved exclusively from the finalized binding.</param>
/// <param name="CorrectedCaseId">The caller-supplied pre-existing corrected case.</param>
/// <param name="EntriesInvalidated">The number of prior-case unit derivative sets structurally invalidated.</param>
/// <param name="EntriesRebuilt">The number of unit derivative sets rebuilt or migrated.</param>
/// <param name="VersionGuardSkipped">Whether equal or newer convergence made this operation an idempotent no-op.</param>
/// <param name="DeadlineUtc">The durable sixty-minute terminal deadline.</param>
/// <param name="CompletedAtUtc">The terminal completion time, or <see langword="null"/> while nonterminal.</param>
/// <param name="FailureReasonCode">A safe metadata-only failure code, or <see langword="null"/>.</param>
public sealed record DerivedStoreCorrectionStatus(
    string OperationId,
    DerivedStoreCorrectionState State,
    string AssociationId,
    string IntakeId,
    string CorrectionId,
    long SourceVersion,
    string PriorCaseId,
    string CorrectedCaseId,
    int EntriesInvalidated,
    int EntriesRebuilt,
    bool VersionGuardSkipped,
    DateTimeOffset DeadlineUtc,
    DateTimeOffset? CompletedAtUtc,
    string? FailureReasonCode);
