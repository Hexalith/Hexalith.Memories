namespace Hexalith.Memories.DomainService.Tests;

using System.Text.Json;
using Hexalith.EventStore.Client.Handlers;
using Hexalith.EventStore.Client.Aggregates;
using Hexalith.EventStore.Contracts.Commands;
using Hexalith.EventStore.Contracts.Events;
using Hexalith.EventStore.Contracts.Results;
using Hexalith.EventStore.Contracts.Replay;
using Hexalith.Memories.Contracts.V1;
using Hexalith.Memories.EventStore.Domain;
using Hexalith.Memories.EventStore.Domain.Commands;
using Hexalith.Memories.EventStore.Domain.Events;
using Hexalith.Memories.EventStore.Domain.States;
using Shouldly;

/// <summary>Uses the canonical SDK dispatcher/replay with existing pure commands, events, state and private setters.</summary>
public sealed class MemoriesAggregateAdapterTests
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);
    private static readonly DateTimeOffset Now = new(2026, 10, 24, 0, 0, 0, TimeSpan.Zero);

    [Theory]
    [InlineData(MemoriesDomain.Tenants)]
    [InlineData(MemoriesDomain.Cases)]
    [InlineData(MemoriesDomain.MemoryUnits)]
    public void ReplayRouterOwnsOnlyItsExactPersistedDomainFallbackAndSdkType(string domain)
    {
        object adapter = domain switch
        {
            MemoriesDomain.Tenants => new MemoriesTenantAggregateAdapter(),
            MemoriesDomain.Cases => new MemoriesCaseAggregateAdapter(),
            _ => new MemoriesMemoryUnitAggregateAdapter(),
        };
        var synchronous = (IAggregateReplay)adapter;
        var asynchronous = (IAsyncAggregateReplay)adapter;
        foreach (string candidate in new[] { MemoriesDomain.Tenants, MemoriesDomain.Cases, MemoriesDomain.MemoryUnits, "foreign",
            nameof(MemoriesTenantAggregateAdapter), nameof(MemoriesCaseAggregateAdapter), nameof(MemoriesMemoryUnitAggregateAdapter) })
        {
            synchronous.CanReplayAggregateType(candidate).ShouldBe(candidate == domain || candidate == adapter.GetType().Name);
            asynchronous.CanReplayAggregateType(candidate).ShouldBe(candidate == domain || candidate == adapter.GetType().Name);
        }
        synchronous.CanReplayAggregateType(adapter.GetType().Name).ShouldBeTrue();
        asynchronous.CanReplayAggregateType(adapter.GetType().Name).ShouldBeTrue();
    }

    [Fact]
    public async Task TenantDecisionsPreserveOriginalEventsAndRehydrateSnapshotTailAndAdminReplay()
    {
        var aggregate = new MemoriesTenantAggregateAdapter();
        var register = new RegisterTenantCommand("tenant-a", "Synthetic", Now);
        DomainResult first = await Process(aggregate, Envelope(register), null);
        first.Events.ShouldHaveSingleItem().ShouldBeOfType<TenantRegisteredEvent>();
        var snapshot = new MemoriesTenantAggregateState(); snapshot.Apply((TenantRegisteredEvent)first.Events[0]);
        DomainResult active = await Process(aggregate, Envelope(new UpdateTenantLifecycleStatusCommand("tenant-a", TenantStatus.Active, Now)),
            JsonSerializer.SerializeToElement(new DomainServiceCurrentState(JsonSerializer.SerializeToElement(snapshot, Json), [], 1, 1), Json));
        active.Events.ShouldHaveSingleItem().ShouldBeOfType<TenantLifecycleStatusUpdatedEvent>().Status.ShouldBe(TenantStatus.Active);
        EventEnvelope tail = Stored(active.Events[0], MemoriesDomain.Tenants, "tenant-a", 2);
        DomainResult duplicate = await Process(aggregate, Envelope(register), new DomainServiceCurrentState(JsonSerializer.SerializeToElement(snapshot, Json), [tail], 1, 2));
        duplicate.IsNoOp.ShouldBeTrue();
        AggregateReconstructionResult replay = aggregate.Replay(new("tenant-a", MemoriesDomain.Tenants, nameof(MemoriesTenantAggregateAdapter), "tenant-a", 2,
            [Replay(first.Events[0], 1), Replay(active.Events[0], 2)], true, "replay"));
        replay.Status.ShouldBe(AggregateReconstructionStatus.Succeeded);
        using JsonDocument state = JsonDocument.Parse(replay.StateJson!);
        state.RootElement.GetProperty("tenantId").GetString().ShouldBe("tenant-a");
        Enum.Parse<TenantStatus>(state.RootElement.GetProperty("status").GetString()!, ignoreCase: true).ShouldBe(TenantStatus.Active);
    }

    [Fact]
    public async Task CaseDecisionsUseTheSamePureSnapshotAndDeletionFold()
    {
        var aggregate = new MemoriesCaseAggregateAdapter();
        var create = new CreateCaseCommand("tenant-a", "case-a", "Synthetic", null, Now);
        DomainResult first = await Process(aggregate, Envelope(create), null);
        var snapshot = new CaseAggregateState(); snapshot.Apply(first.Events.Single().ShouldBeOfType<CaseCreatedEvent>());
        DomainResult deletion = await Process(aggregate, Envelope(new DeleteCaseCommand("tenant-a", "case-a", [], Now)),
            new DomainServiceCurrentState(JsonSerializer.SerializeToElement(snapshot, Json), [], 1, 1));
        deletion.Events.ShouldHaveSingleItem().ShouldBeOfType<CaseDeletionRequestedEvent>();
        (await Process(aggregate, Envelope(create), new DomainServiceCurrentState(null,
            [Stored(first.Events[0], MemoriesDomain.Cases, "case-a", 1), Stored(deletion.Events[0], MemoriesDomain.Cases, "case-a", 2)], 0, 2))).IsNoOp.ShouldBeTrue();
    }

    [Fact]
    public async Task MemoryUnitDecisionsPreserveAnnotationAndDeleteIntentAndRejectAnotherCase()
    {
        var aggregate = new MemoriesMemoryUnitAggregateAdapter();
        var annotate = new RequestAnnotationCommand("tenant-a", "case-a", "unit-a", "target", "urn:synthetic", "Synthetic annotation", "note", "actor", Now);
        DomainResult first = await Process(aggregate, Envelope(annotate), null);
        var snapshot = new MemoryUnitAggregateState(); snapshot.Apply(first.Events.Single().ShouldBeOfType<AnnotationRequestedEvent>());
        object state = new DomainServiceCurrentState(JsonSerializer.SerializeToElement(snapshot, Json), [], 1, 1);
        DomainResult deletion = await Process(aggregate, Envelope(new DeleteMemoryUnitCommand("tenant-a", "case-a", "unit-a", [], Now)), state);
        deletion.Events.ShouldHaveSingleItem().ShouldBeOfType<MemoryUnitDeletionRequestedEvent>();
        await AssertScopeRefusal(() => Process(aggregate, Envelope(new DeleteMemoryUnitCommand("tenant-a", "other-case", "unit-a", [], Now)), state));
        (await Process(aggregate, Envelope(annotate), state)).IsNoOp.ShouldBeTrue();
    }

    [Theory]
    [InlineData("tenant")]
    [InlineData("domain")]
    [InlineData("aggregate")]
    [InlineData("snapshot")]
    [InlineData("tail")]
    [InlineData("window")]
    public async Task ForeignEnvelopeSnapshotOrTailFailsBeforeAnyDecision(string changed)
    {
        var aggregate = new MemoriesCaseAggregateAdapter();
        CommandEnvelope command = Envelope(new CreateCaseCommand("tenant-a", "case-a", "Synthetic", null, Now));
        object? state = null;
        if (changed == "tenant") { command = command with { TenantId = "other-tenant" }; }
        if (changed == "domain") { command = command with { Domain = MemoriesDomain.Tenants }; }
        if (changed == "aggregate") { command = command with { AggregateId = "other-case" }; }
        if (changed == "snapshot") { state = JsonSerializer.SerializeToElement(new { tenantId = "other-tenant", caseId = "case-a", name = "Synthetic" }, Json); }
        if (changed == "tail")
        {
            EventEnvelope own = Stored(new CaseCreatedEvent("tenant-a", "case-a", "Synthetic", null, Now), MemoriesDomain.Cases, "case-a", 1);
            state = new DomainServiceCurrentState(null, [new(own.Metadata with { TenantId = "other-tenant" }, own.Payload, own.Extensions)], 0, 1);
        }
        if (changed == "window") { state = new DomainServiceCurrentState(null, [], 0, 1); }
        await AssertScopeRefusal(() => Process(aggregate, command, state), changed == "tail"
            ? "The Memories event state crosses its authenticated stream scope."
            : changed == "window" ? "The Memories command state is not a complete snapshot/tail window."
            : "The Memories command or replayed state does not match its authenticated aggregate scope.");
    }

    private static async Task AssertScopeRefusal(Func<Task<DomainResult>> action, string reason = "The Memories command or replayed state does not match its authenticated aggregate scope.")
    {
        Exception failure = await Should.ThrowAsync<Exception>(action);
        Exception refusal = failure is System.Reflection.TargetInvocationException invocation ? invocation.InnerException! : failure;
        refusal.ShouldBeOfType<InvalidOperationException>().Message.ShouldBe(reason);
    }

    private static Task<DomainResult> Process(IDomainProcessor processor, CommandEnvelope command, object? state) => processor.ProcessAsync(command, state);
    private static CommandEnvelope Envelope<T>(T command) where T : IMemoriesCommandContract
        => new("message", "tenant-a", T.Domain, command.AggregateId, T.CommandType, JsonSerializer.SerializeToUtf8Bytes(command, Json), "correlation", null, "actor", null);
    private static EventEnvelope Stored(IEventPayload payload, string domain, string aggregate, long sequence)
        => new(new("message-" + sequence, aggregate, domain, "tenant-a", domain, sequence, sequence, Now, "correlation", "cause", "actor", "v1", payload.GetType().FullName!, 1, "json"), JsonSerializer.SerializeToUtf8Bytes(payload, payload.GetType(), Json), null);
    private static ReplayEventEnvelope Replay(IEventPayload payload, long sequence)
        => new(sequence, payload.GetType().FullName!, JsonSerializer.SerializeToUtf8Bytes(payload, payload.GetType(), Json), "json", 1, "message-" + sequence, null, null);
}
