// <copyright file="CaseDeletionRequestedEvent.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.EventStore.Domain.Events;


/// <summary>Case deletion intent event.</summary>
public sealed record CaseDeletionRequestedEvent(
    string TenantId,
    string CaseId,
    IReadOnlyList<string> MemoryUnitIds,
    DateTimeOffset DeletedAt) : IMemoriesEventPayload;
