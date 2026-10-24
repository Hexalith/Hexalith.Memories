namespace Hexalith.Memories.DomainService;

using Hexalith.EventStore.Client.Aggregates;
using Hexalith.EventStore.Client.Attributes;
using Hexalith.EventStore.Client.Handlers;
using Hexalith.EventStore.Contracts.Results;
using Hexalith.EventStore.Contracts.Commands;
using Hexalith.Memories.EventStore.Domain;
using Hexalith.Memories.EventStore.Domain.Aggregates;
using Hexalith.Memories.EventStore.Domain.Commands;
using Hexalith.Memories.EventStore.Domain.States;

internal static class MemoriesCommandScope
{
    internal static void Validate<TCommand>(TCommand command, string tenantId, CommandEnvelope envelope, bool stateScopeMatches)
        where TCommand : IMemoriesCommandContract
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentNullException.ThrowIfNull(envelope);
        if (envelope.Domain != TCommand.Domain || envelope.TenantId != tenantId || envelope.AggregateId != command.AggregateId || !stateScopeMatches)
        {
            throw new InvalidOperationException("The Memories command or replayed state does not match its authenticated aggregate scope.");
        }
    }
}
