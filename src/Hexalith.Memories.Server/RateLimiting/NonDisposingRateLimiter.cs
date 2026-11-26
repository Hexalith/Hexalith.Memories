// <copyright file="NonDisposingRateLimiter.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.RateLimiting;

using System.Threading.RateLimiting;

/// <summary>
/// Exposes a shared rate limiter to an owning framework without transferring disposal ownership.
/// </summary>
internal sealed class NonDisposingRateLimiter(InboundRequestRateLimiter owner, string partitionKey) : RateLimiter
{
    /// <inheritdoc />
    public override TimeSpan? IdleDuration => owner.GetIdleDuration(partitionKey);

    /// <inheritdoc />
    public override RateLimiterStatistics? GetStatistics() => owner.GetStatistics(partitionKey);

    /// <inheritdoc />
    protected override ValueTask<RateLimitLease> AcquireAsyncCore(
        int permitCount,
        CancellationToken cancellationToken)
        => owner.AcquireAsync(partitionKey, permitCount, cancellationToken);

    /// <inheritdoc />
    protected override RateLimitLease AttemptAcquireCore(int permitCount)
        => owner.AttemptAcquire(partitionKey, permitCount);
}
