// <copyright file="EmbeddingProviderFaultPlan.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.IntegrationTests.Fixtures;

using System.Net;

/// <summary>Thread-safe, bounded HTTP failure sequence owned by one fake embedding-provider server.</summary>
public sealed class EmbeddingProviderFaultPlan
{
    private int _remainingFailureCount;

    /// <summary>Initializes a new instance of the <see cref="EmbeddingProviderFaultPlan"/> class.</summary>
    /// <param name="statusCode">HTTP 429 or 5xx response emitted while failures remain.</param>
    /// <param name="failureCount">Number of failures to emit before normal success resumes.</param>
    /// <param name="retryAfter">Optional Retry-After delay included with HTTP 429.</param>
    public EmbeddingProviderFaultPlan(
        HttpStatusCode statusCode,
        int failureCount,
        TimeSpan? retryAfter = null)
    {
        if (statusCode != HttpStatusCode.TooManyRequests && (int)statusCode < 500)
        {
            throw new ArgumentOutOfRangeException(
                nameof(statusCode),
                statusCode,
                "Embedding fault plans support only HTTP 429 or 5xx responses.");
        }

        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(failureCount);
        if (retryAfter is { } delay && delay <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(retryAfter), retryAfter, "Retry-After must be positive.");
        }

        StatusCode = statusCode;
        InitialFailureCount = failureCount;
        RetryAfter = retryAfter;
        _remainingFailureCount = failureCount;
    }

    /// <summary>Gets the failure status code.</summary>
    public HttpStatusCode StatusCode { get; }

    /// <summary>Gets the original bounded failure count.</summary>
    public int InitialFailureCount { get; }

    /// <summary>Gets the optional Retry-After delay.</summary>
    public TimeSpan? RetryAfter { get; }

    /// <summary>Gets the number of failures that have not yet been consumed.</summary>
    public int RemainingFailureCount => Math.Max(0, Volatile.Read(ref _remainingFailureCount));

    /// <summary>Atomically consumes one planned failure when any remain.</summary>
    /// <returns><see langword="true"/> when the caller should emit a failure.</returns>
    internal bool TryConsumeFailure()
    {
        while (true)
        {
            int remaining = Volatile.Read(ref _remainingFailureCount);
            if (remaining <= 0)
            {
                return false;
            }

            if (Interlocked.CompareExchange(ref _remainingFailureCount, remaining - 1, remaining) == remaining)
            {
                return true;
            }
        }
    }
}
