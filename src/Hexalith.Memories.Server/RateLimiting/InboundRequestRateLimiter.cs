// <copyright file="InboundRequestRateLimiter.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.RateLimiting;

using System.Threading.RateLimiting;

using Microsoft.Extensions.Options;

/// <summary>Shared keyed limiter used by body-bound endpoint filters.</summary>
internal sealed class InboundRequestRateLimiter : IAsyncDisposable
{
    private readonly Lock _gate = new();
    private readonly Dictionary<string, FixedWindowRateLimiter> _limiters = new(StringComparer.Ordinal);
    private readonly FixedWindowRateLimiterOptions _options;
    private bool _stopping;

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
        return AcquireAsync(partitionKey, permitCount: 1, cancellationToken);
    }

    /// <summary>Creates an ASP.NET Core rate-limiter partition backed by this shared limiter.</summary>
    public RateLimitPartition<string> CreatePartition(string partitionKey)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(partitionKey);
        return RateLimitPartition.Get(partitionKey, CreateFrameworkLimiter);
    }

    /// <inheritdoc />
    public ValueTask DisposeAsync()
    {
        lock (_gate)
        {
            _stopping = true;
            foreach (FixedWindowRateLimiter limiter in _limiters.Values)
            {
                limiter.Dispose();
            }

            _limiters.Clear();
        }

        return ValueTask.CompletedTask;
    }

    /// <summary>Starts an asynchronous acquisition while the shared limiter is available.</summary>
    internal ValueTask<RateLimitLease> AcquireAsync(string partitionKey, int permitCount, CancellationToken cancellationToken)
    {
        lock (_gate)
        {
            return _stopping
                ? ValueTask.FromResult<RateLimitLease>(ShutdownRateLimitLease.Instance)
                : GetOrCreateLimiter(partitionKey).AcquireAsync(permitCount, cancellationToken);
        }
    }

    /// <summary>Attempts an immediate acquisition while the shared limiter is available.</summary>
    internal RateLimitLease AttemptAcquire(string partitionKey, int permitCount)
    {
        lock (_gate)
        {
            return _stopping ? ShutdownRateLimitLease.Instance : GetOrCreateLimiter(partitionKey).AttemptAcquire(permitCount);
        }
    }

    /// <summary>Gets the current idle duration of an existing shared limiter.</summary>
    internal TimeSpan? GetIdleDuration(string partitionKey)
    {
        lock (_gate)
        {
            return _limiters.GetValueOrDefault(partitionKey)?.IdleDuration;
        }
    }

    /// <summary>Gets statistics for an existing shared limiter.</summary>
    internal RateLimiterStatistics? GetStatistics(string partitionKey)
    {
        lock (_gate)
        {
            return _limiters.GetValueOrDefault(partitionKey)?.GetStatistics();
        }
    }

    private NonDisposingRateLimiter CreateFrameworkLimiter(string partitionKey)
    {
        lock (_gate)
        {
            if (!_stopping)
            {
                _ = GetOrCreateLimiter(partitionKey);
            }

            return new NonDisposingRateLimiter(this, partitionKey);
        }
    }

    private FixedWindowRateLimiter GetOrCreateLimiter(string partitionKey)
    {
        if (!_limiters.TryGetValue(partitionKey, out FixedWindowRateLimiter? limiter))
        {
            limiter = new FixedWindowRateLimiter(_options);
            _limiters.Add(partitionKey, limiter);
        }

        return limiter;
    }
}
