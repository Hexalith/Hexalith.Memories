// <copyright file="CaseIngestionCounterActorTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Tests.Actors;

using System.Reflection;

using Dapr.Actors;
using Dapr.Actors.Runtime;

using Hexalith.Memories.Contracts.V1;
using Hexalith.Memories.Server.Actors;

using Microsoft.Extensions.Logging;

using NSubstitute;

using Shouldly;

public class CaseIngestionCounterActorTests
{
    private const string StateName = "counterState";

    [Fact]
    public async Task TransitionAsync_NoExistingState_CreatesAndPersists()
    {
        (CaseIngestionCounterActor actor, IActorStateManager state, CapturingLogger logger) = CreateActor();
        SetupEmptyState(state);

        await actor.TransitionAsync("none", "queued", "t1");

        await state.Received().SetStateAsync(
            StateName,
            Arg.Is<CaseIngestionCounterState>(s => s.Queued == 1 && s.LastTransitionId == "t1"),
            Arg.Any<CancellationToken>());
        logger.Entries.ShouldContain(entry => entry.EventId.Id == 6307 && entry.Message.Contains("t1", StringComparison.Ordinal));
    }

    [Fact]
    public async Task TransitionAsync_DuplicateTransitionId_DoesNotPersist()
    {
        (CaseIngestionCounterActor actor, IActorStateManager state, _) = CreateActor();
        CaseIngestionCounterState existing = new(1, 0, 0, 0, "applied");
        SetupExistingState(state, existing);

        await actor.TransitionAsync("queued", "extracting", "applied");

        await state.DidNotReceive().SetStateAsync(
            StateName,
            Arg.Any<CaseIngestionCounterState>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task TransitionAsync_DuplicateTransitionId_EmitsIdempotentLogEvent()
    {
        (CaseIngestionCounterActor actor, IActorStateManager state, CapturingLogger logger) = CreateActor();
        CaseIngestionCounterState existing = new(1, 0, 0, 0, "applied");
        SetupExistingState(state, existing);

        await actor.TransitionAsync("queued", "extracting", "applied");

        logger.Entries.ShouldContain(entry => entry.EventId.Id == 6308 && entry.Message.Contains("applied", StringComparison.Ordinal));
    }

    [Fact]
    public async Task GetCountsAsync_NoExistingState_ReturnsZeros()
    {
        (CaseIngestionCounterActor actor, IActorStateManager state, _) = CreateActor();
        SetupEmptyState(state);

        CaseIngestionCounts counts = await actor.GetCountsAsync();

        counts.ShouldBe(new CaseIngestionCounts(0, 0, 0, 0));
    }

    [Fact]
    public async Task GetCountsAsync_ExistingState_ProjectsCorrectly()
    {
        (CaseIngestionCounterActor actor, IActorStateManager state, _) = CreateActor();
        SetupExistingState(state, new(2, 1, 3, 0, "tx"));

        CaseIngestionCounts counts = await actor.GetCountsAsync();

        counts.ShouldBe(new CaseIngestionCounts(2, 1, 3, 0));
    }

    [Fact]
    public async Task ResetAsync_PersistsZeroState()
    {
        (CaseIngestionCounterActor actor, IActorStateManager state, _) = CreateActor();

        await actor.ResetAsync();

        await state.Received().SetStateAsync(
            StateName,
            Arg.Is<CaseIngestionCounterState>(s =>
                s.Queued == 0 && s.Extracting == 0 && s.Embedding == 0 && s.Indexing == 0 && s.LastTransitionId == null),
            Arg.Any<CancellationToken>());
    }

    private static (CaseIngestionCounterActor Actor, IActorStateManager State, CapturingLogger Logger) CreateActor()
    {
        IActorStateManager stateManager = Substitute.For<IActorStateManager>();
        CapturingLogger logger = new();

        ActorHost host = ActorHost.CreateForTest<CaseIngestionCounterActor>(
            new ActorTestOptions { ActorId = new ActorId("tenant1:case1") });

        CaseIngestionCounterActor actor = new(host, new CaseIngestionCounterLogic(), logger);

        PropertyInfo? prop = typeof(Actor).GetProperty("StateManager", BindingFlags.Public | BindingFlags.Instance);
        prop?.SetValue(actor, stateManager);

        return (actor, stateManager, logger);
    }

    private static void SetupEmptyState(IActorStateManager stateManager)
        => stateManager.TryGetStateAsync<CaseIngestionCounterState>(StateName, Arg.Any<CancellationToken>())
            .Returns(new ConditionalValue<CaseIngestionCounterState>(false, default!));

    private static void SetupExistingState(IActorStateManager stateManager, CaseIngestionCounterState state)
        => stateManager.TryGetStateAsync<CaseIngestionCounterState>(StateName, Arg.Any<CancellationToken>())
            .Returns(new ConditionalValue<CaseIngestionCounterState>(true, state));

    private sealed class CapturingLogger : ILogger<CaseIngestionCounterActor>
    {
        public List<(LogLevel Level, EventId EventId, string Message)> Entries { get; } = [];

        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
            => Entries.Add((logLevel, eventId, formatter(state, exception)));
    }
}
