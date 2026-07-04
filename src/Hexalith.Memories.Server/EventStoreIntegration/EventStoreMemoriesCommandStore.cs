// <copyright file="EventStoreMemoriesCommandStore.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.EventStoreIntegration;

using System.Text.Json;

using BaUlid = ByteAether.Ulid.Ulid;

using Hexalith.EventStore.Client.Gateway;
using Hexalith.EventStore.Contracts.Commands;
using Hexalith.Memories.EventStore.Domain.Commands;

/// <summary>EventStore gateway implementation of <see cref="IMemoriesCommandStore"/>. Story 21.2:
/// command submission goes through the Hexalith.EventStore.Client SDK gateway contract so the
/// authoritative write path uses the platform API instead of hand-rolled service invocation.</summary>
internal sealed class EventStoreMemoriesCommandStore(IEventStoreGatewayClient gatewayClient) : IMemoriesCommandStore
{
    private static readonly JsonSerializerOptions SerializerOptions = JsonSerializerOptions.Web;

    private static readonly BaUlid.GenerationOptions UlidOptions = new()
    {
        Monotonicity = BaUlid.GenerationOptions.MonotonicityOptions.MonotonicIncrement,
    };

    /// <inheritdoc/>
    public async Task<string> AcceptAsync<TCommand>(
        string tenantId,
        TCommand command,
        string actorId,
        CancellationToken cancellationToken)
        where TCommand : IMemoriesCommandContract
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        ArgumentNullException.ThrowIfNull(command);
        ArgumentException.ThrowIfNullOrWhiteSpace(actorId);

        string messageId = BaUlid.New(UlidOptions).ToString();
        JsonElement payload = JsonSerializer.SerializeToElement(command, SerializerOptions);
        SubmitCommandResponse response = await gatewayClient.SubmitCommandAsync(
            new SubmitCommandRequest(
                messageId,
                tenantId,
                TCommand.Domain,
                command.AggregateId,
                TCommand.CommandType,
                payload,
                CorrelationId: messageId,
                Extensions: new Dictionary<string, string>(StringComparer.Ordinal)
                {
                    ["actorId"] = actorId,
                    ["source"] = "Hexalith.Memories.Server",
                }),
            cancellationToken).ConfigureAwait(false);

        return response.CorrelationId;
    }
}
