namespace Hexalith.Memories.Cli.Tests.ClientRest;

using System.Net;
using System.Text;
using System.Text.Json;
using Hexalith.Memories.Client.Rest;
using Hexalith.Memories.Contracts.V1;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Shouldly;

/// <summary>Actual SDK reader controls for bounded streaming, scope, cancellation and public compatibility.</summary>
public sealed class SourceInventoryClientTests
{
    [Theory]
    [InlineData(null, 200)]
    [InlineData(1L, 200)]
    [InlineData(1048577L, 200)]
    [InlineData(null, 503)]
    public async Task OversizedInventoryIsRejectedBeforeDeserializationEvenWhenLengthIsMissingOrFalse(long? reportedLength, int status)
    {
        using var content = new InventoryStreamingContent(Encoding.UTF8.GetBytes(new string(' ', 1048577)), reportedLength);
        using var http = Http(content, status);
        var exception = await Should.ThrowAsync<MemoriesRemoteException>(() => Client(http).GetSourceInventoryAsync("tenant", "urn:source", ct: TestContext.Current.CancellationToken));
        exception.Error.Code.ShouldBe("INVALID_RESPONSE");
        content.StreamOpened.ShouldBe(reportedLength is null or <= 1048576);
    }

    [Theory]
    [InlineData(101, 0)]
    [InlineData(0, 8193)]
    public async Task EntryAndCursorBoundsAreEnforcedByThePublicReader(int count, int cursorLength)
    {
        var page = new SourceInventoryPage("tenant", "urn:source", "version", false, false, false,
            Enumerable.Range(0, count).Select(index => new SourceInventoryEntry("case", "workflow-" + index)).ToArray(),
            cursorLength == 0 ? null : new string('x', cursorLength));
        using var content = new InventoryStreamingContent(JsonSerializer.SerializeToUtf8Bytes(page, MemoriesJsonContext.Options));
        using var http = Http(content);
        (await Should.ThrowAsync<MemoriesRemoteException>(() => Client(http).GetSourceInventoryAsync("tenant", "urn:source", ct: TestContext.Current.CancellationToken)))
            .Error.Code.ShouldBe("INVALID_RESPONSE");
    }

    [Fact]
    public async Task CancellationDuringStreamAcquisitionIsPreservedAndValidPublicDefaultsStillRead()
    {
        byte[] page = JsonSerializer.SerializeToUtf8Bytes(new SourceInventoryPage("tenant", "urn:source", "v", false, false, false, [], null), MemoriesJsonContext.Options);
        using var cancelled = new CancellationTokenSource();
        using var content = new InventoryStreamingContent(page, beforeRead: token => { token.CanBeCanceled.ShouldBeTrue(); cancelled.Cancel(); });
        using var http = Http(content);
        await Should.ThrowAsync<OperationCanceledException>(() => Client(http).GetSourceInventoryAsync("tenant", "urn:source", ct: cancelled.Token));
        using var validContent = new InventoryStreamingContent(page);
        using var valid = Http(validContent);
        (await Client(valid).GetSourceInventoryAsync("tenant", "urn:source")).Version.ShouldBe("v");
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task StalledBodyHonorsTheTotalHttpTimeoutAndCallerCancellation(bool callerCancels)
    {
        using var content = new InventoryStreamingContent([], createStream: () => new InventoryStalledStream());
        using var http = Http(content);
        http.Timeout = callerCancels ? Timeout.InfiniteTimeSpan : TimeSpan.FromMilliseconds(100);
        using var caller = new CancellationTokenSource();
        if (callerCancels) { caller.CancelAfter(TimeSpan.FromMilliseconds(100)); }
        await Should.ThrowAsync<OperationCanceledException>(() => Client(http).GetSourceInventoryAsync("tenant", "urn:source", ct: caller.Token)
            .WaitAsync(TimeSpan.FromSeconds(2)));
        caller.IsCancellationRequested.ShouldBe(callerCancels);
    }

    private static HttpClient Http(HttpContent content, int status = 200)
        => new(new TestDelegatingHandler((_, _) => Task.FromResult(new HttpResponseMessage((HttpStatusCode)status) { Content = content })))
        { BaseAddress = new Uri("http://127.0.0.1/") };
    private static MemoriesClient Client(HttpClient http) => new(http, Options.Create(new MemoriesClientOptions()), NullLogger<MemoriesClient>.Instance);
}
