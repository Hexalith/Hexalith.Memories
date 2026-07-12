// <copyright file="NonDisposingRateLimiter.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.RateLimiting;

using System.Threading.RateLimiting;

/// <summary>
/// Exposes a shared rate limiter to an owning framework without transferring disposal ownership.
/// </summary>
internal sealed class NonDisposingRateLimiter(RateLimiter inner) : RateLimiter
{
    private readonly RateLimiter _inner = inner ?? throw new ArgumentNullException(nameof(inner));

    /// <inheritdoc />
    public override TimeSpan? IdleDuration => _inner.IdleDuration;

    /// <inheritdoc />
    public override RateLimiterStatistics? GetStatistics() => _inner.GetStatistics();

    /// <inheritdoc />
    protected override ValueTask<RateLimitLease> AcquireAsyncCore(
        int permitCount,
        CancellationToken cancellationToken)
        => _inner.AcquireAsync(permitCount, cancellationToken);

    /// <inheritdoc />
    protected override RateLimitLease AttemptAcquireCore(int permitCount)
        => _inner.AttemptAcquire(permitCount);
}
