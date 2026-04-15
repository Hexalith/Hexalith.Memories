namespace Hexalith.Memories.Server.Tests.Actors;

using System.Reflection;

using Dapr.Actors;
using Dapr.Actors.Runtime;

using Hexalith.Memories.Server.Actors;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

using NSubstitute;

using Shouldly;

/// <summary>
/// Unit tests for EmbeddingRateLimiterActor — the thin DAPR actor host
/// that wraps RateLimiterLogic and manages state persistence.
/// RateLimiterLogic is independently tested; these tests verify the actor
/// correctly loads, delegates, and persists state.
/// </summary>
public class EmbeddingRateLimiterActorTests
{
    [Fact]
    public async Task TryConsumeAsync_WhenNoStateExists_ShouldCreateDefaultAndAllow()
    {
        // Arrange
        (EmbeddingRateLimiterActor actor, IActorStateManager stateManager) = CreateActorWithMockState();
        SetupEmptyState(stateManager);

        // Act
        bool allowed = await actor.TryConsumeAsync();

        // Assert
        allowed.ShouldBeTrue("First request with default state (1500 budget) should be allowed");

        await stateManager.Received().SetStateAsync(
            "rateState",
            Arg.Is<RateLimitState>(s => s.Remaining == 1499 && s.CeilingPerMinute == 1500),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task TryConsumeAsync_WhenBudgetExhausted_ShouldReturnFalse()
    {
        // Arrange
        (EmbeddingRateLimiterActor actor, IActorStateManager stateManager) = CreateActorWithMockState();

        RateLimitState exhaustedState = new(Remaining: 0, WindowStart: DateTime.UtcNow, CeilingPerMinute: 100);
        SetupExistingState(stateManager, exhaustedState);

        // Act
        bool allowed = await actor.TryConsumeAsync();

        // Assert
        allowed.ShouldBeFalse("Should be denied when budget is exhausted");
    }

    [Fact]
    public async Task GetStateAsync_WhenNoStateExists_ShouldReturnDefaultState()
    {
        // Arrange
        (EmbeddingRateLimiterActor actor, IActorStateManager stateManager) = CreateActorWithMockState();
        SetupEmptyState(stateManager);

        // Act
        RateLimitState state = await actor.GetStateAsync();

        // Assert
        state.CeilingPerMinute.ShouldBe(1500);
        state.Remaining.ShouldBe(1500);
    }

    [Fact]
    public async Task GetStateAsync_WhenStateExists_ShouldReturnPersistedState()
    {
        // Arrange
        (EmbeddingRateLimiterActor actor, IActorStateManager stateManager) = CreateActorWithMockState();

        RateLimitState existingState = new(Remaining: 42, WindowStart: DateTime.UtcNow, CeilingPerMinute: 100);
        SetupExistingState(stateManager, existingState);

        // Act
        RateLimitState state = await actor.GetStateAsync();

        // Assert
        state.Remaining.ShouldBe(42);
        state.CeilingPerMinute.ShouldBe(100);
    }

    [Fact]
    public async Task ResetAsync_ShouldRestoreFullBudget()
    {
        // Arrange
        (EmbeddingRateLimiterActor actor, IActorStateManager stateManager) = CreateActorWithMockState();

        RateLimitState partialState = new(Remaining: 10, WindowStart: DateTime.UtcNow.AddMinutes(-2), CeilingPerMinute: 200);
        SetupExistingState(stateManager, partialState);

        // Act
        await actor.ResetAsync();

        // Assert
        await stateManager.Received().SetStateAsync(
            "rateState",
            Arg.Is<RateLimitState>(s => s.Remaining == 200 && s.CeilingPerMinute == 200),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SetCeilingAsync_ShouldUpdateCeilingAndClampRemaining()
    {
        // Arrange
        (EmbeddingRateLimiterActor actor, IActorStateManager stateManager) = CreateActorWithMockState();

        RateLimitState currentState = new(Remaining: 500, WindowStart: DateTime.UtcNow, CeilingPerMinute: 1500);
        SetupExistingState(stateManager, currentState);

        // Act
        await actor.SetCeilingAsync(100);

        // Assert — remaining clamped to new ceiling
        await stateManager.Received().SetStateAsync(
            "rateState",
            Arg.Is<RateLimitState>(s => s.CeilingPerMinute == 100 && s.Remaining == 100),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SetCeilingAsync_WhenRemainingBelowNewCeiling_ShouldPreserveRemaining()
    {
        // Arrange
        (EmbeddingRateLimiterActor actor, IActorStateManager stateManager) = CreateActorWithMockState();

        RateLimitState currentState = new(Remaining: 50, WindowStart: DateTime.UtcNow, CeilingPerMinute: 1500);
        SetupExistingState(stateManager, currentState);

        // Act
        await actor.SetCeilingAsync(200);

        // Assert — remaining unchanged (50 < 200)
        await stateManager.Received().SetStateAsync(
            "rateState",
            Arg.Is<RateLimitState>(s => s.CeilingPerMinute == 200 && s.Remaining == 50),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ReportRateLimitedAsync_ShouldZeroFloorRemainingAndAdvanceWindow()
    {
        // Arrange
        (EmbeddingRateLimiterActor actor, IActorStateManager stateManager) = CreateActorWithMockState();

        RateLimitState currentState = new(Remaining: 500, WindowStart: DateTime.UtcNow, CeilingPerMinute: 1500);
        SetupExistingState(stateManager, currentState);

        DateTime before = DateTime.UtcNow;

        // Act
        await actor.ReportRateLimitedAsync(30);

        // Assert — Remaining zeroed, WindowStart in the future (~30 s from now), ceiling preserved.
        await stateManager.Received().SetStateAsync(
            "rateState",
            Arg.Is<RateLimitState>(s =>
                s.Remaining == 0 &&
                s.CeilingPerMinute == 1500 &&
                s.WindowStart >= before.AddSeconds(29) &&
                s.WindowStart <= DateTime.UtcNow.AddSeconds(31)),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ReportRateLimitedAsync_ClampsNegativeRetryAfterToOneSecond()
    {
        // Arrange
        (EmbeddingRateLimiterActor actor, IActorStateManager stateManager) = CreateActorWithMockState();

        RateLimitState currentState = new(Remaining: 100, WindowStart: DateTime.UtcNow, CeilingPerMinute: 500);
        SetupExistingState(stateManager, currentState);

        DateTime before = DateTime.UtcNow;

        // Act
        await actor.ReportRateLimitedAsync(-5);

        // Assert — clamped to 1 s.
        await stateManager.Received().SetStateAsync(
            "rateState",
            Arg.Is<RateLimitState>(s =>
                s.Remaining == 0 &&
                s.WindowStart >= before &&
                s.WindowStart <= DateTime.UtcNow.AddSeconds(2)),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ReportRateLimitedAsync_ShouldLogRateLimitActorUpdated()
    {
        CapturingLogger logger = new();
        (EmbeddingRateLimiterActor actor, IActorStateManager stateManager) = CreateActorWithMockState(logger);

        RateLimitState currentState = new(Remaining: 25, WindowStart: DateTime.UtcNow, CeilingPerMinute: 200);
        SetupExistingState(stateManager, currentState);

        await actor.ReportRateLimitedAsync(30);

        logger.Entries.ShouldContain(entry =>
            entry.EventId.Id == 6203 &&
            entry.Level == LogLevel.Information &&
            entry.Message.Contains("test-tenant-001", StringComparison.Ordinal));
    }

    [Fact]
    public async Task SetCeilingAsync_ZeroCeiling_ShouldThrow()
    {
        // Arrange
        (EmbeddingRateLimiterActor actor, IActorStateManager stateManager) = CreateActorWithMockState();

        RateLimitState currentState = new(Remaining: 100, WindowStart: DateTime.UtcNow, CeilingPerMinute: 100);
        SetupExistingState(stateManager, currentState);

        // Act & Assert
        await Should.ThrowAsync<ArgumentOutOfRangeException>(
            () => actor.SetCeilingAsync(0));
    }

    private static (EmbeddingRateLimiterActor Actor, IActorStateManager StateManager) CreateActorWithMockState(
        ILogger<EmbeddingRateLimiterActor>? logger = null)
    {
        IActorStateManager stateManager = Substitute.For<IActorStateManager>();

        ActorHost host = ActorHost.CreateForTest<EmbeddingRateLimiterActor>(
            new ActorTestOptions { ActorId = new ActorId("test-tenant-001") });

        EmbeddingRateLimiterActor actor = new(host, logger ?? NullLogger<EmbeddingRateLimiterActor>.Instance);

        // Set the mock state manager via reflection (DAPR runtime normally sets this)
        PropertyInfo? prop = typeof(Actor).GetProperty("StateManager", BindingFlags.Public | BindingFlags.Instance);
        prop?.SetValue(actor, stateManager);

        return (actor, stateManager);
    }

    private static void SetupEmptyState(IActorStateManager stateManager)
    {
        stateManager.TryGetStateAsync<RateLimitState>("rateState", Arg.Any<CancellationToken>())
            .Returns(new ConditionalValue<RateLimitState>(false, default!));
    }

    private static void SetupExistingState(IActorStateManager stateManager, RateLimitState state)
    {
        stateManager.TryGetStateAsync<RateLimitState>("rateState", Arg.Any<CancellationToken>())
            .Returns(new ConditionalValue<RateLimitState>(true, state));
    }

    private sealed class CapturingLogger : ILogger<EmbeddingRateLimiterActor>
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
