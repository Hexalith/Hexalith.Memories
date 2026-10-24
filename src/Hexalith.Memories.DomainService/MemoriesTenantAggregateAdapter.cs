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

/// <summary>SDK discovery and replay adapter for the existing pure tenant decisions and folds.</summary>
[EventStoreDomain(MemoriesDomain.Tenants)]
public sealed class MemoriesTenantAggregateAdapter : ScopedMemoriesAggregate<MemoriesTenantAggregateState>
{
    /// <inheritdoc />
    protected override string OwnedDomain => MemoriesDomain.Tenants;

    /// <summary>Delegates tenant registration without changing its event payload.</summary>
    public static DomainResult Handle(RegisterTenantCommand command, MemoriesTenantAggregateState? state, CommandEnvelope envelope)
    {
        MemoriesCommandScope.Validate(command, command.TenantId, envelope, state is null || state.TenantId == envelope.TenantId);
        return new(MemoriesTenantAggregate.Handle(command, state).Events);
    }
    /// <summary>Delegates tenant lifecycle changes.</summary>
    public static DomainResult Handle(UpdateTenantLifecycleStatusCommand command, MemoriesTenantAggregateState? state, CommandEnvelope envelope)
    {
        MemoriesCommandScope.Validate(command, command.TenantId, envelope, state is null || state.TenantId == envelope.TenantId);
        return new(MemoriesTenantAggregate.Handle(command, state).Events);
    }
}
