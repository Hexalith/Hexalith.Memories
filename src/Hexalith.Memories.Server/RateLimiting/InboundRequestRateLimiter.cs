// <copyright file="InboundRequestRateLimiter.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.RateLimiting;

using System.Collections.Concurrent;
using System.Threading.RateLimiting;

using Microsoft.Extensions.Options;

/// <summary>Shared keyed limiter used by body-bound endpoint filters.</summary>
internal sealed class InboundRequestRateLimiter : IAsyncDisposable
{
    private readonly ConcurrentDictionary<string, RateLimiter> _limiters = new(StringComparer.Ordinal);
    private readonly FixedWindowRateLimiterOptions _options;

    /// <summary>Initializes a new instance of the <see cref="InboundRequestRateLimiter"/> class.</summary>
    public InboundRequestRateLimiter(IOptions<InboundRateLimitOptions> options)
        : this(options?.Value ?? throw new ArgumentNullException(nameof(options)))
    {
    }

    /// <summary>Initializes a new instance of the <see cref="InboundRequestRateLimiter"/> class.</summary>
    public InboundRequestRateLimiter(InboundRateLimitOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        _options = new FixedWindowRateLimiterOptions
        {
            AutoReplenishment = true,
            PermitLimit = Math.Max(1, options.PermitLimit),
            QueueLimit = Math.Max(0, options.QueueLimit),
            QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
            Window = TimeSpan.FromSeconds(Math.Max(1, options.WindowSeconds)),
        };
    }

    /// <summary>Acquires a single inbound request permit for a partition key.</summary>
    public ValueTask<RateLimitLease> AcquireAsync(string partitionKey, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(partitionKey);
        return GetLimiter(partitionKey).AcquireAsync(permitCount: 1, cancellationToken);
    }

    /// <summary>Creates an ASP.NET Core rate-limiter partition backed by this shared limiter.</summary>
    public RateLimitPartition<string> CreatePartition(string partitionKey)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(partitionKey);
        return RateLimitPartition.Get(partitionKey, GetLimiter);
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        foreach (RateLimiter limiter in _limiters.Values)
        {
            await limiter.DisposeAsync().ConfigureAwait(false);
        }
    }

    private RateLimiter GetLimiter(string partitionKey)
        => _limiters.GetOrAdd(
            partitionKey,
            _ => new FixedWindowRateLimiter(_options));
}
