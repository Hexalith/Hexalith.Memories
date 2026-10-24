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

/// <summary>Fences transported stream scope before the canonical SDK reconstructs state and invokes pure decisions.</summary>
/// <typeparam name="TState">The existing pure state and Apply contract.</typeparam>
public abstract class ScopedMemoriesAggregate<TState> : EventStoreAggregate<TState>, IDomainProcessor, IAsyncDomainProcessor, IAggregateReplay, IAsyncAggregateReplay
    where TState : class, new()
{
    /// <summary>Gets the exact domain whose canonical gateway fallback type this adapter owns.</summary>
    protected abstract string OwnedDomain { get; }

    /// <summary>Accepts the exact persisted domain fallback as well as canonical SDK CLR type hints.</summary>
    public new bool CanReplayAggregateType(string aggregateType)
        => string.Equals(aggregateType, OwnedDomain, StringComparison.Ordinal) || base.CanReplayAggregateType(aggregateType);

    Task<DomainResult> IDomainProcessor.ProcessAsync(CommandEnvelope command, object? currentState)
        => ProcessScopedAsync(command, currentState, CancellationToken.None);

    Task<DomainResult> IAsyncDomainProcessor.ProcessAsync(CommandEnvelope command, object? currentState, CancellationToken cancellationToken)
        => ProcessScopedAsync(command, currentState, cancellationToken);

    private Task<DomainResult> ProcessScopedAsync(CommandEnvelope command, object? currentState, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        cancellationToken.ThrowIfCancellationRequested();
        DomainServiceCurrentState? source = DomainStateReplay.SnapshotAware(currentState);
        if (source is not null)
        {
            if (source.LastSnapshotSequence < 0 || source.CurrentSequence < source.LastSnapshotSequence
                || source.Events.Count != source.CurrentSequence - source.LastSnapshotSequence)
            {
                throw new InvalidOperationException("The Memories command state is not a complete snapshot/tail window.");
            }
            long sequence = source.LastSnapshotSequence;
            foreach (Hexalith.EventStore.Contracts.Events.EventEnvelope entry in source.Events)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (entry.Metadata.TenantId != command.TenantId || entry.Metadata.Domain != command.Domain
                    || entry.Metadata.AggregateId != command.AggregateId || entry.Metadata.SequenceNumber != ++sequence)
                {
                    throw new InvalidOperationException("The Memories event state crosses its authenticated stream scope.");
                }
            }
        }
        return base.ProcessAsync(command, currentState, cancellationToken);
    }
}
