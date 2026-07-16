// <copyright file="PerTenantConcurrencyGateTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Tests.Ingestion;

using Hexalith.Memories.Server.Ingestion;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

using Shouldly;

public class PerTenantConcurrencyGateTests
{
    [Fact]
    public async Task AcquireAsync_UpToMax_CompletesImmediately_BeyondMaxBlocks()
    {
        await using PerTenantConcurrencyGate gate = CreateGate(max: 3);

        IAsyncDisposable lease1 = await gate.AcquireAsync("t1", CancellationToken.None);
        IAsyncDisposable lease2 = await gate.AcquireAsync("t1", CancellationToken.None);
        IAsyncDisposable lease3 = await gate.AcquireAsync("t1", CancellationToken.None);

        // 4th acquisition must block until a prior lease is released.
        Task<IAsyncDisposable> pending = gate.AcquireAsync("t1", CancellationToken.None);
        (await Task.WhenAny(pending, Task.Delay(200))).ShouldNotBe(pending);

        await lease1.DisposeAsync();
        IAsyncDisposable lease4 = await pending;

        await lease2.DisposeAsync();
        await lease3.DisposeAsync();
        await lease4.DisposeAsync();
    }

    [Fact]
    public async Task AcquireAsync_IsolatesTenantsIndependently()
    {
        await using PerTenantConcurrencyGate gate = CreateGate(max: 2);

        IAsyncDisposable t1a = await gate.AcquireAsync("t1", CancellationToken.None);
        IAsyncDisposable t1b = await gate.AcquireAsync("t1", CancellationToken.None);

        // t1 is saturated; t2's acquisitions must still succeed without blocking.
        IAsyncDisposable t2a = await gate.AcquireAsync("t2", CancellationToken.None);
        IAsyncDisposable t2b = await gate.AcquireAsync("t2", CancellationToken.None);

        Task<IAsyncDisposable> t1Pending = gate.AcquireAsync("t1", CancellationToken.None);
        Task<IAsyncDisposable> t2Pending = gate.AcquireAsync("t2", CancellationToken.None);

        (await Task.WhenAny(t1Pending, Task.Delay(100))).ShouldNotBe(t1Pending);
        (await Task.WhenAny(t2Pending, Task.Delay(100))).ShouldNotBe(t2Pending);

        await t1a.DisposeAsync();
        await t2a.DisposeAsync();

        IAsyncDisposable t1c = await t1Pending;
        IAsyncDisposable t2c = await t2Pending;

        await t1b.DisposeAsync();
        await t2b.DisposeAsync();
        await t1c.DisposeAsync();
        await t2c.DisposeAsync();
    }

    [Fact]
    public async Task AcquireAsync_AfterDispose_SlotReleased()
    {
        await using PerTenantConcurrencyGate gate = CreateGate(max: 1);

        IAsyncDisposable lease = await gate.AcquireAsync("t1", CancellationToken.None);
        await lease.DisposeAsync();

        // Reacquire must succeed immediately (no block).
        Task<IAsyncDisposable> reacquire = gate.AcquireAsync("t1", CancellationToken.None);
        (await Task.WhenAny(reacquire, Task.Delay(500))).ShouldBe(reacquire);

        await (await reacquire).DisposeAsync();
    }

    [Fact]
    public async Task AcquireAsync_InvalidConcurrency_ClampsToOne()
    {
        await using PerTenantConcurrencyGate gate = CreateGate(max: 0);

        IAsyncDisposable first = await gate.AcquireAsync("t1", CancellationToken.None);
        Task<IAsyncDisposable> pending = gate.AcquireAsync("t1", CancellationToken.None);

        (await Task.WhenAny(pending, Task.Delay(200))).ShouldNotBe(pending);

        await first.DisposeAsync();
        await (await pending).DisposeAsync();
    }

    [Fact]
    public async Task AcquireAsync_SlotReleasedWhenBodyThrows()
    {
        await using PerTenantConcurrencyGate gate = CreateGate(max: 1);

        await Should.ThrowAsync<InvalidOperationException>(async () =>
        {
            await using IAsyncDisposable lease = await gate.AcquireAsync("t1", CancellationToken.None);
            throw new InvalidOperationException("simulated body failure");
        });

        // Reacquire succeeds → lease.DisposeAsync ran in the await-using teardown path.
        Task<IAsyncDisposable> reacquire = gate.AcquireAsync("t1", CancellationToken.None);
        (await Task.WhenAny(reacquire, Task.Delay(500))).ShouldBe(reacquire);
        await (await reacquire).DisposeAsync();
    }

    [Fact]
    public async Task AcquireAsync_TimeoutExpires_ThrowsTimeoutException()
    {
        await using PerTenantConcurrencyGate gate = CreateGate(max: 1, acquireTimeoutSeconds: 1);

        IAsyncDisposable held = await gate.AcquireAsync("t1", CancellationToken.None);

        TimeoutException ex = await Should.ThrowAsync<TimeoutException>(
            async () => await gate.AcquireAsync("t1", CancellationToken.None));

        ex.Message.ShouldContain("t1");
        ex.Message.ShouldContain("1s");

        await held.DisposeAsync();
    }

    [Fact]
    public async Task AcquireAsync_InvalidTimeout_ClampsToOneSecond()
    {
        await using PerTenantConcurrencyGate gate = CreateGate(max: 1, acquireTimeoutSeconds: 0);

        IAsyncDisposable held = await gate.AcquireAsync("t1", CancellationToken.None);

        TimeoutException ex = await Should.ThrowAsync<TimeoutException>(
            async () => await gate.AcquireAsync("t1", CancellationToken.None));

        ex.Message.ShouldContain("1s");

        await held.DisposeAsync();
    }

    [Fact]
    public async Task AcquireAsync_CancellationBeforeSlotAvailable_ThrowsOperationCanceled()
    {
        await using PerTenantConcurrencyGate gate = CreateGate(max: 1, acquireTimeoutSeconds: 60);

        IAsyncDisposable held = await gate.AcquireAsync("t1", CancellationToken.None);

        using CancellationTokenSource cts = new();
        Task<IAsyncDisposable> pending = gate.AcquireAsync("t1", cts.Token);
        cts.Cancel();

        await Should.ThrowAsync<OperationCanceledException>(async () => await pending);

        // The semaphore budget was not consumed by the cancelled waiter; releasing the first
        // lease must leave the gate fully available.
        await held.DisposeAsync();
        gate.GetAvailableCount("t1").ShouldBe(1);
    }

    [Fact]
    public async Task AcquireAsync_ManyTenantsInParallel_AllComplete()
    {
        await using PerTenantConcurrencyGate gate = CreateGate(max: 1);

        Task<IAsyncDisposable>[] acquisitions = Enumerable.Range(0, 100)
            .Select(i => gate.AcquireAsync($"tenant-{i}", CancellationToken.None))
            .ToArray();

        IAsyncDisposable[] leases = await Task.WhenAll(acquisitions);

        foreach (IAsyncDisposable lease in leases)
        {
            await lease.DisposeAsync();
        }
    }

    [Fact]
    public async Task AcquireAsync_Contended_LogsActualQueueDepth()
    {
        CapturingLogger logger = new();
        await using PerTenantConcurrencyGate gate = CreateGate(max: 1, logger: logger);

        IAsyncDisposable first = await gate.AcquireAsync("t1", CancellationToken.None);
        Task<IAsyncDisposable> second = gate.AcquireAsync("t1", CancellationToken.None);
        Task<IAsyncDisposable> third = gate.AcquireAsync("t1", CancellationToken.None);

        await Task.Delay(100);

        IEnumerable<string> contentionMessages = logger.Entries
            .Where(entry => entry.EventId.Id == 6205)
            .Select(entry => entry.Message);
        contentionMessages.ShouldContain(message => message.Contains("queueDepth=1", StringComparison.Ordinal));
        contentionMessages.ShouldContain(message => message.Contains("queueDepth=2", StringComparison.Ordinal));

        await first.DisposeAsync();
        await (await second).DisposeAsync();
        await (await third).DisposeAsync();
    }

    private static PerTenantConcurrencyGate CreateGate(
        int max,
        int acquireTimeoutSeconds = 300,
        ILogger<PerTenantConcurrencyGate>? logger = null)
    {
        IngestionSettings settings = new()
        {
            PerTenantExtractionConcurrency = max,
            ExtractionGateAcquireTimeoutSeconds = acquireTimeoutSeconds,
        };
        IOptions<IngestionSettings> options = Options.Create(settings);
        return new PerTenantConcurrencyGate(options, logger ?? NullLogger<PerTenantConcurrencyGate>.Instance);
    }

    private sealed class CapturingLogger : ILogger<PerTenantConcurrencyGate>
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
