// <copyright file="TenantRegisteredEvent.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.EventStore.Domain.Events;


/// <summary>Tenant registration event.</summary>
public sealed record TenantRegisteredEvent(
    string TenantId,
    string DisplayName,
    DateTimeOffset RegisteredAt) : IMemoriesEventPayload;
