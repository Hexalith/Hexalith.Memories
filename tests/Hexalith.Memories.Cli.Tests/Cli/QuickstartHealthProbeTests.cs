// <copyright file="QuickstartHealthProbeTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Cli.Tests.Cli;

using Hexalith.Memories.Cli.Quickstart;
using Hexalith.Memories.Client.Rest;

using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;

using Shouldly;

public sealed class QuickstartHealthProbeTests
{
    [Fact]
    public async Task WaitForReady_ImmediateReady_Returns()
    {
        var client = new StubHealthClient(alwaysHealthy: true);
        var clock = new FakeTimeProvider();
        var probe = new HealthProbe(client, clock);

        HealthProbeResult result = await probe.WaitForReadyAsync(TimeSpan.FromSeconds(60), TimeSpan.FromSeconds(1), CancellationToken.None);

        result.Ready.ShouldBeTrue();
        result.LastError.ShouldBeNull();
    }

    [Fact]
    public async Task WaitForReady_ReadyAfterRetries_Returns()
    {
        var client = new StubHealthClient(callsBeforeHealthy: 3);
        var clock = new FakeTimeProvider();
        var probe = new HealthProbe(client, clock);

        Task<HealthProbeResult> probeTask = probe.WaitForReadyAsync(TimeSpan.FromSeconds(60), TimeSpan.FromSeconds(1), CancellationToken.None);

        for (int i = 0; i < 5; i++)
        {
            clock.Advance(TimeSpan.FromSeconds(1));
            await Task.Yield();
        }

        HealthProbeResult result = await probeTask;
        result.Ready.ShouldBeTrue();
        client.CallCount.ShouldBeGreaterThanOrEqualTo(3);
    }

    [Fact]
    public async Task WaitForReady_TimesOut_WhenNeverHealthy()
    {
        var client = new StubHealthClient(alwaysHealthy: false);
        var clock = new FakeTimeProvider();
        var probe = new HealthProbe(client, clock);

        Task<HealthProbeResult> probeTask = probe.WaitForReadyAsync(TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(1), CancellationToken.None);

        for (int i = 0; i < 10; i++)
        {
            clock.Advance(TimeSpan.FromSeconds(1));
            await Task.Yield();
        }

        HealthProbeResult result = await probeTask;
        result.Ready.ShouldBeFalse();
    }

    [Fact]
    public async Task WaitForReady_Cancellation_BreaksOut()
    {
        var client = new StubHealthClient(alwaysHealthy: false);
        var clock = new FakeTimeProvider();
        var probe = new HealthProbe(client, clock);
        using var cts = new CancellationTokenSource();

        cts.Cancel();

        await Should.ThrowAsync<OperationCanceledException>(() => probe.WaitForReadyAsync(TimeSpan.FromSeconds(60), TimeSpan.FromSeconds(1), cts.Token));
    }

    private sealed class StubHealthClient : MemoriesClient
    {
        private readonly bool _alwaysHealthy;
        private readonly int _callsBeforeHealthy;

        public int CallCount { get; private set; }

        public StubHealthClient(bool alwaysHealthy)
            : base(
                new HttpClient { BaseAddress = new Uri("http://127.0.0.1:65001/") },
                Options.Create(new MemoriesClientOptions { Endpoint = new Uri("http://127.0.0.1:65001/") }),
                NullLogger<MemoriesClient>.Instance)
        {
            _alwaysHealthy = alwaysHealthy;
            _callsBeforeHealthy = alwaysHealthy ? 0 : int.MaxValue;
        }

        public StubHealthClient(int callsBeforeHealthy)
            : base(
                new HttpClient { BaseAddress = new Uri("http://127.0.0.1:65001/") },
                Options.Create(new MemoriesClientOptions { Endpoint = new Uri("http://127.0.0.1:65001/") }),
                NullLogger<MemoriesClient>.Instance)
        {
            _alwaysHealthy = false;
            _callsBeforeHealthy = callsBeforeHealthy;
        }

        public override Task<bool> ProbeHealthAsync(CancellationToken ct)
        {
            CallCount++;
            if (_alwaysHealthy)
            {
                return Task.FromResult(true);
            }

            return Task.FromResult(CallCount >= _callsBeforeHealthy);
        }
    }
}
