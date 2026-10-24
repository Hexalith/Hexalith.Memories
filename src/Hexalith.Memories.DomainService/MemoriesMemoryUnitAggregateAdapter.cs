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

/// <summary>SDK discovery and replay adapter for the existing pure memory-unit decisions and folds.</summary>
[EventStoreDomain(MemoriesDomain.MemoryUnits)]
public sealed class MemoriesMemoryUnitAggregateAdapter : ScopedMemoriesAggregate<MemoryUnitAggregateState>
{
    /// <inheritdoc />
    protected override string OwnedDomain => MemoriesDomain.MemoryUnits;

    /// <summary>Delegates annotation intent.</summary>
    public static DomainResult Handle(RequestAnnotationCommand command, MemoryUnitAggregateState? state, CommandEnvelope envelope)
    {
        MemoriesCommandScope.Validate(command, command.TenantId, envelope, state is null || state.TenantId == envelope.TenantId && state.MemoryUnitId == envelope.AggregateId && state.CaseId == command.CaseId);
        return new(MemoryUnitAggregate.Handle(command, state).Events);
    }
    /// <summary>Delegates memory-unit deletion.</summary>
    public static DomainResult Handle(DeleteMemoryUnitCommand command, MemoryUnitAggregateState? state, CommandEnvelope envelope)
    {
        MemoriesCommandScope.Validate(command, command.TenantId, envelope, state is null || state.TenantId == envelope.TenantId && state.MemoryUnitId == envelope.AggregateId && state.CaseId == command.CaseId);
        return new(MemoryUnitAggregate.Handle(command, state).Events);
    }
}
