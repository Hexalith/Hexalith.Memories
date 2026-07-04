// <copyright file="AggregateCommandHandlerTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.EventStore.Tests.Domain;

using Hexalith.Memories.Contracts.V1;
using Hexalith.Memories.EventStore.Domain.Aggregates;
using Hexalith.Memories.EventStore.Domain.Commands;
using Hexalith.Memories.EventStore.Domain.Events;
using Hexalith.Memories.EventStore.Domain.Results;
using Hexalith.Memories.EventStore.Domain.States;

using Shouldly;

public sealed class AggregateCommandHandlerTests
{
    private static readonly DateTimeOffset FixedNow = new(2026, 7, 4, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void CreateCase_WhenStateIsEmpty_EmitsCaseCreatedEvent()
    {
        MemoriesDomainResult result = CaseAggregate.Handle(
            new CreateCaseCommand("tenant-1", "case-1", "Case 1", "Description", FixedNow),
            state: null);

        result.IsSuccess.ShouldBeTrue();
        CaseCreatedEvent @event = result.Events.ShouldHaveSingleItem().ShouldBeOfType<CaseCreatedEvent>();
        @event.TenantId.ShouldBe("tenant-1");
        @event.CaseId.ShouldBe("case-1");
        @event.Name.ShouldBe("Case 1");
    }

    [Fact]
    public void CreateCase_WhenStateAlreadyExists_IsNoOp()
    {
        CaseAggregateState state = new();
        state.Apply(new CaseCreatedEvent("tenant-1", "case-1", "Case 1", null, FixedNow));

        MemoriesDomainResult result = CaseAggregate.Handle(
            new CreateCaseCommand("tenant-1", "case-1", "Case 1", null, FixedNow),
            state);

        result.IsNoOp.ShouldBeTrue();
        result.Events.ShouldBeEmpty();
    }

    [Fact]
    public void CreateCase_WhenRequiredInputIsMissing_RejectsCommand()
    {
        Should.Throw<ArgumentException>(() =>
            CaseAggregate.Handle(new CreateCaseCommand("tenant-1", "case-1", string.Empty, null, FixedNow), null));
    }

    [Fact]
    public void RequestAnnotation_WhenStateIsEmpty_EmitsAnnotationRequestedEvent()
    {
        MemoriesDomainResult result = MemoryUnitAggregate.Handle(
            new RequestAnnotationCommand(
                "tenant-1",
                "case-1",
                "annotation-1",
                "target-1",
                "annotation://target-1/annotation-1",
                "Looks relevant",
                "note",
                "user-1",
                FixedNow),
            state: null);

        result.IsSuccess.ShouldBeTrue();
        AnnotationRequestedEvent @event = result.Events.ShouldHaveSingleItem().ShouldBeOfType<AnnotationRequestedEvent>();
        @event.AnnotationMemoryUnitId.ShouldBe("annotation-1");
        @event.TargetMemoryUnitId.ShouldBe("target-1");
        @event.CaseId.ShouldBe("case-1");
    }

    [Fact]
    public void RequestAnnotation_WhenDuplicateAnnotationStateExists_IsNoOp()
    {
        MemoryUnitAggregateState state = new();
        state.Apply(new AnnotationRequestedEvent(
            "tenant-1",
            "case-1",
            "annotation-1",
            "target-1",
            "annotation://target-1/annotation-1",
            "Looks relevant",
            "note",
            "user-1",
            FixedNow));

        MemoriesDomainResult result = MemoryUnitAggregate.Handle(
            new RequestAnnotationCommand(
                "tenant-1",
                "case-1",
                "annotation-1",
                "target-1",
                "annotation://target-1/annotation-1",
                "Looks relevant",
                "note",
                "user-1",
                FixedNow),
            state);

        result.IsNoOp.ShouldBeTrue();
    }

    [Fact]
    public void DeleteMemoryUnit_WhenStateIsNotDeleted_EmitsDeletionRequestedEvent()
    {
        MemoriesDomainResult result = MemoryUnitAggregate.Handle(
            new DeleteMemoryUnitCommand("tenant-1", "case-1", "mu-1", ["annotation-1"], FixedNow),
            state: null);

        MemoryUnitDeletionRequestedEvent @event =
            result.Events.ShouldHaveSingleItem().ShouldBeOfType<MemoryUnitDeletionRequestedEvent>();
        @event.TenantId.ShouldBe("tenant-1");
        @event.CaseId.ShouldBe("case-1");
        @event.MemoryUnitId.ShouldBe("mu-1");
        @event.AnnotationMemoryUnitIds.ShouldBe(["annotation-1"]);
    }

    [Fact]
    public void DeleteCase_WhenAlreadyDeletionRequested_IsNoOp()
    {
        CaseAggregateState state = new();
        state.Apply(new CaseCreatedEvent("tenant-1", "case-1", "Case 1", null, FixedNow));
        state.Apply(new CaseDeletionRequestedEvent("tenant-1", "case-1", ["mu-1"], FixedNow));

        MemoriesDomainResult result = CaseAggregate.Handle(
            new DeleteCaseCommand("tenant-1", "case-1", ["mu-1"], FixedNow),
            state);

        result.IsNoOp.ShouldBeTrue();
    }

    [Fact]
    public void TenantLifecycleStatus_WhenStatusIsChanged_EmitsStatusEvent()
    {
        MemoriesTenantAggregateState state = new();
        state.Apply(new TenantRegisteredEvent("tenant-1", "Tenant 1", FixedNow));

        MemoriesDomainResult result = MemoriesTenantAggregate.Handle(
            new UpdateTenantLifecycleStatusCommand("tenant-1", TenantStatus.Active, FixedNow),
            state);

        TenantLifecycleStatusUpdatedEvent @event =
            result.Events.ShouldHaveSingleItem().ShouldBeOfType<TenantLifecycleStatusUpdatedEvent>();
        @event.Status.ShouldBe(TenantStatus.Active);
    }
}
