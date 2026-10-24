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

/// <summary>SDK discovery and replay adapter for the existing pure case decisions and folds.</summary>
[EventStoreDomain(MemoriesDomain.Cases)]
public sealed class MemoriesCaseAggregateAdapter : ScopedMemoriesAggregate<CaseAggregateState>
{
    /// <inheritdoc />
    protected override string OwnedDomain => MemoriesDomain.Cases;

    /// <summary>Delegates case creation.</summary>
    public static DomainResult Handle(CreateCaseCommand command, CaseAggregateState? state, CommandEnvelope envelope)
    {
        MemoriesCommandScope.Validate(command, command.TenantId, envelope, state is null || state.TenantId == envelope.TenantId && state.CaseId == envelope.AggregateId);
        return new(CaseAggregate.Handle(command, state).Events);
    }
    /// <summary>Delegates case deletion.</summary>
    public static DomainResult Handle(DeleteCaseCommand command, CaseAggregateState? state, CommandEnvelope envelope)
    {
        MemoriesCommandScope.Validate(command, command.TenantId, envelope, state is null || state.TenantId == envelope.TenantId && state.CaseId == envelope.AggregateId);
        return new(CaseAggregate.Handle(command, state).Events);
    }
}
