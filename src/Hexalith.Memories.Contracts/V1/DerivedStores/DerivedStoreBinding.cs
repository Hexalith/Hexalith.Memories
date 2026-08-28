// <copyright file="DerivedStoreBinding.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Contracts.V1.DerivedStores;

/// <summary>Describes an atomically finalized tenant-scoped ingestion binding.</summary>
/// <param name="TenantId">The owning tenant.</param>
/// <param name="AssociationId">The governed association identifier.</param>
/// <param name="IntakeId">The governed intake identifier.</param>
/// <param name="SourceVersion">The finalized source version.</param>
/// <param name="PriorCaseId">The case that owned every unit when the binding was finalized.</param>
/// <param name="ExpectedAttachmentCount">The expected attachment count.</param>
/// <param name="Entries">The complete ordered manifest.</param>
/// <param name="FinalizedAtUtc">When the binding was atomically published.</param>
public sealed record DerivedStoreBinding(
    string TenantId,
    string AssociationId,
    string IntakeId,
    long SourceVersion,
    string PriorCaseId,
    int ExpectedAttachmentCount,
    IReadOnlyList<DerivedStoreBindingEntry> Entries,
    DateTimeOffset FinalizedAtUtc);
