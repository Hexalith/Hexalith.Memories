// <copyright file="DevelopmentAuthenticatedUtcSource.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.AccessTelemetry.Clock;

/// <summary>Explicit Development-only UTC source used by the local portable topology.</summary>
internal sealed class DevelopmentAuthenticatedUtcSource(string sourceId, TimeProvider timeProvider) : IAuthenticatedUtcSource
{
    /// <inheritdoc/>
    public string SourceId { get; } = sourceId;

    /// <inheritdoc/>
    public Task<AuthenticatedUtcSample> GetUtcSampleAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        DateTimeOffset now = timeProvider.GetUtcNow();
        return Task.FromResult(new AuthenticatedUtcSample(
            SourceId,
            now.AddMilliseconds(-25),
            now.AddMilliseconds(25),
            Authenticated: true));
    }
}
