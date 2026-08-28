// <copyright file="FinalizeDerivedStoreBindingRequest.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Contracts.V1.DerivedStores;

/// <summary>Requests atomic publication of a complete association/intake ingestion binding.</summary>
/// <param name="AssociationId">The governed association identifier.</param>
/// <param name="IntakeId">The governed intake identifier.</param>
/// <param name="SourceVersion">The positive source version guarded by this binding.</param>
/// <param name="PriorCaseId">The existing case that currently owns every bound MemoryUnit.</param>
/// <param name="ExpectedAttachmentCount">The exact number of attachment entries expected after the message.</param>
/// <param name="Entries">The complete ordered message-plus-attachment manifest.</param>
public sealed record FinalizeDerivedStoreBindingRequest(
    string AssociationId,
    string IntakeId,
    long SourceVersion,
    string PriorCaseId,
    int ExpectedAttachmentCount,
    IReadOnlyList<DerivedStoreBindingEntry> Entries);
