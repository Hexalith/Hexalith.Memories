// <copyright file="CaseCreatedEvent.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.EventStore.Domain.Events;


/// <summary>Case aggregate creation event.</summary>
public sealed record CaseCreatedEvent(
    string TenantId,
    string CaseId,
    string Name,
    string? Description,
    DateTimeOffset CreatedAt) : IMemoriesEventPayload;
