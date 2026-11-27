// <copyright file="RedisReadinessGateTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.IntegrationTests.Fixtures;

using Hexalith.Memories.AppHost;

using Shouldly;

/// <summary>Proves the Redis allocation probe is shared, cancellable and fenced across starts.</summary>
[Trait("Category", "Integration")]
public sealed class RedisReadinessGateTests
{
    private static readonly TimeSpan _testTimeout = TimeSpan.FromSeconds(5);

    [Fact]
    public async Task WaitAsync_ConsumersShareOneProbeAndCancelIndependently()
    {
        using var testBudget = new CancellationTokenSource(_testTimeout);
        using var callerCancellation = new CancellationTokenSource();
        var gate = new RedisReadinessGate(_testTimeout);
        var running = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var probing = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var pong = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        int probeCalls = 0;
        Task earlyConsumer = gate.WaitAsync(testBudget.Token);
        Task probe = gate.StartGenerationAsync(_ => running.Task, ProbeAsync, testBudget.Token);
        Task canceledConsumer = gate.WaitAsync(callerCancellation.Token);
        Task otherConsumer = gate.WaitAsync(testBudget.Token);

        probing.Task.IsCompleted.ShouldBeFalse();
        running.SetResult();
        await probing.Task.WaitAsync(testBudget.Token).ConfigureAwait(true);

        callerCancellation.Cancel();
        _ = await Should.ThrowAsync<OperationCanceledException>(() => canceledConsumer).ConfigureAwait(true);
        earlyConsumer.IsCompleted.ShouldBeFalse();
        otherConsumer.IsCompleted.ShouldBeFalse();

        pong.SetResult();
        await Task.WhenAll(probe, earlyConsumer, otherConsumer)
            .WaitAsync(testBudget.Token).ConfigureAwait(true);

        probeCalls.ShouldBe(1);
        _ = await Should.ThrowAsync<OperationCanceledException>(
            () => gate.WaitAsync(callerCancellation.Token)).ConfigureAwait(true);

        Task ProbeAsync(CancellationToken cancellationToken)
        {
            probeCalls++;
            probing.TrySetResult();
            return pong.Task.WaitAsync(cancellationToken);
        }
    }

    [Fact]
    public async Task StartGenerationAsync_LiveWaitersFollowRestartAndIgnoreLateSuccess()
    {
        using var testBudget = new CancellationTokenSource(_testTimeout);
        var gate = new RedisReadinessGate(_testTimeout);
        var latePong = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var oldProbeCanceled = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        Task staleProbe = gate.StartGenerationAsync(_ => Task.CompletedTask, ProbeAsync, testBudget.Token);
        Task staleConsumer = gate.WaitAsync(testBudget.Token);

        var running = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        Task currentProbe = gate.StartGenerationAsync(_ => running.Task, _ => Task.CompletedTask, testBudget.Token);
        Task currentConsumer = gate.WaitAsync(testBudget.Token);
        await oldProbeCanceled.Task.WaitAsync(testBudget.Token).ConfigureAwait(true);

        latePong.SetResult();
        await staleProbe.WaitAsync(testBudget.Token).ConfigureAwait(true);
        staleConsumer.IsCompleted.ShouldBeFalse();
        currentConsumer.IsCompleted.ShouldBeFalse();

        running.SetResult();
        await Task.WhenAll(currentProbe, staleConsumer, currentConsumer).WaitAsync(testBudget.Token).ConfigureAwait(true);

        async Task ProbeAsync(CancellationToken cancellationToken)
        {
            using CancellationTokenRegistration registration = cancellationToken.Register(() => oldProbeCanceled.SetResult());
            await latePong.Task.ConfigureAwait(true);
        }
    }

    [Fact]
    public async Task WaitAsync_LaterConsumersShareRetryAfterAProbeFailure()
    {
        using var testBudget = new CancellationTokenSource(_testTimeout);
        var gate = new RedisReadinessGate(_testTimeout);
        var firstAttempt = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var retry = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        int probeCalls = 0;
        Task initialProbe = gate.StartGenerationAsync(
            _ => Task.CompletedTask,
            _ => ++probeCalls == 1 ? firstAttempt.Task : retry.Task,
            testBudget.Token);
        Task firstConsumer = gate.WaitAsync(testBudget.Token);
        Task secondConsumer = gate.WaitAsync(testBudget.Token);

        var failure = new TimeoutException("Redis did not respond to its initial probe.");
        firstAttempt.SetException(failure);
        (await Should.ThrowAsync<TimeoutException>(() => firstConsumer).ConfigureAwait(true)).ShouldBeSameAs(failure);
        (await Should.ThrowAsync<TimeoutException>(() => secondConsumer).ConfigureAwait(true)).ShouldBeSameAs(failure);
        await initialProbe.WaitAsync(testBudget.Token).ConfigureAwait(true);
        probeCalls.ShouldBe(1);

        Task laterConsumer = gate.WaitAsync(testBudget.Token);
        Task anotherConsumer = gate.WaitAsync(testBudget.Token);
        probeCalls.ShouldBe(2);
        laterConsumer.IsCompleted.ShouldBeFalse();
        anotherConsumer.IsCompleted.ShouldBeFalse();

        retry.SetResult();
        await Task.WhenAll(laterConsumer, anotherConsumer).WaitAsync(testBudget.Token).ConfigureAwait(true);
        probeCalls.ShouldBe(2);
    }

    [Fact]
    public async Task StartGenerationAsync_StaleRunningNotificationCannotRewriteComponents()
    {
        using var testBudget = new CancellationTokenSource(_testTimeout);
        var gate = new RedisReadinessGate(_testTimeout);
        var staleRunning = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        bool stalePrepareRan = false;
        Task staleProbe = gate.StartGenerationAsync(
            _ => staleRunning.Task,
            _ =>
            {
                stalePrepareRan = true;
                return Task.CompletedTask;
            },
            testBudget.Token);

        var currentRunning = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        Task currentProbe = gate.StartGenerationAsync(_ => currentRunning.Task, _ => Task.CompletedTask, testBudget.Token);
        Task currentConsumer = gate.WaitAsync(testBudget.Token);

        staleRunning.SetResult();
        await staleProbe.WaitAsync(testBudget.Token).ConfigureAwait(true);
        stalePrepareRan.ShouldBeFalse();
        currentConsumer.IsCompleted.ShouldBeFalse();

        currentRunning.SetResult();
        await Task.WhenAll(currentProbe, currentConsumer).WaitAsync(testBudget.Token).ConfigureAwait(true);
    }

    [Fact]
    public async Task StartGenerationAsync_RequiresFreshProbeAfterEarlierSuccess()
    {
        using var testBudget = new CancellationTokenSource(_testTimeout);
        var gate = new RedisReadinessGate(_testTimeout);
        await gate.StartGenerationAsync(_ => Task.CompletedTask, _ => Task.CompletedTask, testBudget.Token).ConfigureAwait(true);
        await gate.WaitAsync(testBudget.Token).ConfigureAwait(true);

        var currentRunning = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        Task currentProbe = gate.StartGenerationAsync(_ => currentRunning.Task, _ => Task.CompletedTask, testBudget.Token);
        Task currentConsumer = gate.WaitAsync(testBudget.Token);

        currentConsumer.IsCompleted.ShouldBeFalse();
        currentRunning.SetResult();
        await Task.WhenAll(currentProbe, currentConsumer).WaitAsync(testBudget.Token).ConfigureAwait(true);
    }
}
