namespace Hexalith.Memories.IntegrationTests.Ingestion;

using Hexalith.Memories.IntegrationTests.Fixtures;
using Shouldly;

/// <summary>Private coordination refuses foreign/replayed requests and finishes owned cleanup on failures.</summary>
[Trait("Category", "Integration")]
public sealed class MemoryErasureProcessProtocolTests
{
    /// <summary>A foreign secret and a replay cannot execute an owner operation; the next valid request still succeeds.</summary>
    [Fact]
    public async Task ForeignAndReplayedRequestsRefuseWithoutPoisoningValidSequence()
    {
        var token = TestContext.Current.CancellationToken;
        await using var worker = await MemoryErasureProcessProtocol.StartAsync(token);
        (await worker.RejectedAsync(false, token)).ShouldBe("rejected");
        (await worker.RejectedAsync(true, token)).ShouldBe("rejected");
        (await worker.CallAsync("identity", new { }, token)).GetRawText().ShouldBe(worker.Identity.GetRawText());
        await worker.DisposeAsync();
        worker.Exited.ShouldBeTrue();
    }

    /// <summary>A worker exiting before the first acknowledgement is reaped by the failed-start path.</summary>
    [Fact]
    public async Task FailedStartReturnsOnlyAfterOwnedWorkerCleanup()
    {
        int pid = 0;
        await Should.ThrowAsync<IOException>(() => MemoryErasureProcessProtocol.StartAsync(TestContext.Current.CancellationToken,
            failStart: true, observedStart: (value, _) => pid = value));
        pid.ShouldBeGreaterThan(0);
        Should.Throw<ArgumentException>(() => System.Diagnostics.Process.GetProcessById(pid));
    }

    /// <summary>Cancellation between OS launch and its first acknowledgement still reaps the exact owned process.</summary>
    [Fact]
    public async Task CancellationBeforeStartupAcknowledgementReapsObservedWorker()
    {
        using var caller = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        int pid = 0;
        await Should.ThrowAsync<OperationCanceledException>(() => MemoryErasureProcessProtocol.StartAsync(caller.Token,
            observedStart: (value, _) => { pid = value; caller.Cancel(); }));
        pid.ShouldBeGreaterThan(0);
        Should.Throw<ArgumentException>(() => System.Diagnostics.Process.GetProcessById(pid));
    }

    /// <summary>Malformed declared lengths refuse before any body allocation or operation dispatch.</summary>
    /// <param name="length">The invalid declared body length.</param>
    [Theory]
    [InlineData(-1)]
    [InlineData(1048577)]
    public async Task MalformedFrameLengthRefusesBeforeBodyRead(int length)
    {
        using var stream = new MemoryStream(BitConverter.GetBytes(length));
        await Should.ThrowAsync<IOException>(() => MemoryErasureProcessProtocol.ReadAsync(stream, TestContext.Current.CancellationToken));
        stream.Position.ShouldBe(sizeof(int));
    }

    /// <summary>Cancellation during coordination never abandons an already observed worker.</summary>
    [Fact]
    public async Task CanceledCoordinationStillCompletesOwnedWorkerCleanup()
    {
        await using var worker = await MemoryErasureProcessProtocol.StartAsync(TestContext.Current.CancellationToken);
        using var caller = new CancellationTokenSource(); caller.Cancel();
        await Should.ThrowAsync<OperationCanceledException>(() => worker.CallAsync("identity", new { }, caller.Token));
        await worker.DisposeAsync();
        worker.Exited.ShouldBeTrue();
    }
}
