// <copyright file="MemoryUnitDeletionRequestedEvent.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.EventStore.Domain.Events;


/// <summary>Memory-unit deletion intent event.</summary>
public sealed record MemoryUnitDeletionRequestedEvent(
    string TenantId,
    string CaseId,
    string MemoryUnitId,
    IReadOnlyList<string> AnnotationMemoryUnitIds,
    DateTimeOffset DeletedAt) : IMemoriesEventPayload;
