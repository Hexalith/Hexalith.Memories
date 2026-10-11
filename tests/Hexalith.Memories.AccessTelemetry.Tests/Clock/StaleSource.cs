namespace Hexalith.Memories.AccessTelemetry.Tests.Clock;

using Hexalith.Memories.AccessTelemetry.Clock;

/// <summary>Supplies authenticated UTC evidence outside the verifier freshness window.</summary>
/// <param name="id">The source identifier.</param>
internal sealed class StaleSource(string id) : IAuthenticatedUtcSource
{
    /// <inheritdoc/>
    public string SourceId => id;

    /// <inheritdoc/>
    public Task<AuthenticatedUtcSample> GetUtcSampleAsync(CancellationToken cancellationToken)
    {
        DateTimeOffset stale = DateTimeOffset.UtcNow.AddMinutes(-2);
        return Task.FromResult(new AuthenticatedUtcSample(id, stale.AddMilliseconds(-25), stale.AddMilliseconds(25), true));
    }
}
