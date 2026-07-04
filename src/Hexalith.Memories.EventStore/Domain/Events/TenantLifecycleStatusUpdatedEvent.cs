// <copyright file="TenantLifecycleStatusUpdatedEvent.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.EventStore.Domain.Events;

using Hexalith.Memories.Contracts.V1;

/// <summary>Tenant lifecycle status update event.</summary>
public sealed record TenantLifecycleStatusUpdatedEvent(
    string TenantId,
    TenantStatus Status,
    DateTimeOffset UpdatedAt) : IMemoriesEventPayload;
