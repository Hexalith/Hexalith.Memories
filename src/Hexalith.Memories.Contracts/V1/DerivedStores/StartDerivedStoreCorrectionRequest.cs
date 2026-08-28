// <copyright file="StartDerivedStoreCorrectionRequest.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Contracts.V1.DerivedStores;

/// <summary>Requests start-or-rejoin of a deterministic durable derived-store correction.</summary>
/// <param name="AssociationId">The governed association identifier.</param>
/// <param name="IntakeId">The governed intake identifier whose finalized binding owns the unit set.</param>
/// <param name="CorrectionId">The governed correction identifier.</param>
/// <param name="SourceVersion">The positive correction source version.</param>
/// <param name="CorrectedCaseId">The pre-existing same-tenant corrected case.</param>
public sealed record StartDerivedStoreCorrectionRequest(
    string AssociationId,
    string IntakeId,
    string CorrectionId,
    long SourceVersion,
    string CorrectedCaseId);
