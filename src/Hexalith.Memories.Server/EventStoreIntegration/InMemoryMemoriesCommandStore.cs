// <copyright file="InMemoryMemoriesCommandStore.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.EventStoreIntegration;

using Hexalith.Memories.EventStore.Domain.Commands;

/// <summary>Test fallback command store used only when a caller manually constructs services.</summary>
internal sealed class InMemoryMemoriesCommandStore : IMemoriesCommandStore
{
    /// <inheritdoc/>
    public Task<string> AcceptAsync<TCommand>(
        string tenantId,
        TCommand command,
        string actorId,
        CancellationToken cancellationToken)
        where TCommand : IMemoriesCommandContract
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        ArgumentNullException.ThrowIfNull(command);
        ArgumentException.ThrowIfNullOrWhiteSpace(actorId);
        return Task.FromResult($"{TCommand.CommandType}:{command.AggregateId}");
    }
}
