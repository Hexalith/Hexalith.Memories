namespace Hexalith.Memories.AccessTelemetry.Tests.Clock;

using System.Collections.Concurrent;
using System.Net;
using System.Net.Http.Json;

using Hexalith.Memories.AccessTelemetry.Clock;

using Shouldly;

/// <summary>Returns authenticated UTC responses for the three configured production sources.</summary>
internal sealed class ConfiguredUtcSourceHandler : HttpMessageHandler
{
    private readonly ConcurrentBag<string> _requestedSourceIds = [];

    /// <summary>Gets the source IDs reached by the configured HTTP clients.</summary>
    public IReadOnlyCollection<string> RequestedSourceIds => _requestedSourceIds;

    /// <inheritdoc/>
    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        Uri uri = request.RequestUri ?? throw new InvalidOperationException("The UTC request has no endpoint.");
        uri.Scheme.ShouldBe(Uri.UriSchemeHttps);
        uri.AbsolutePath.ShouldBe("/utc");
        string sourceId = uri.Host.Split('.')[0];
        new[] { "clock-a", "clock-b", "clock-c" }.ShouldContain(sourceId);
        request.Headers.Authorization?.Scheme.ShouldBe("Bearer");
        request.Headers.Authorization?.Parameter.ShouldBe($"token-{sourceId}");
        _requestedSourceIds.Add(sourceId);
        return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(new AuthenticatedUtcResponse
            {
                UnixMilliseconds = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                UncertaintyMilliseconds = 50,
            }),
        });
    }
}
