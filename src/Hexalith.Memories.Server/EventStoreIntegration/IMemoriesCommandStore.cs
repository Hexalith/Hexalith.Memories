// <copyright file="IMemoriesCommandStore.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.EventStoreIntegration;

using Hexalith.Memories.EventStore.Domain.Commands;

/// <summary>Submits authoritative Memories domain commands to Hexalith.EventStore.</summary>
public interface IMemoriesCommandStore
{
    /// <summary>Accepts a command in EventStore before projection fan-out starts.</summary>
    Task<string> AcceptAsync<TCommand>(
        string tenantId,
        TCommand command,
        string actorId,
        CancellationToken cancellationToken)
        where TCommand : IMemoriesCommandContract;
}
