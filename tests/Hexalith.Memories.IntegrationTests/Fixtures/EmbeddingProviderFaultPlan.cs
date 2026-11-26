// <copyright file="EmbeddingProviderFaultPlan.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.IntegrationTests.Fixtures;

using System.Net;

/// <summary>Thread-safe, bounded HTTP failure sequence owned by one fake embedding-provider server.</summary>
public sealed class EmbeddingProviderFaultPlan
{
    private readonly string? _requiredInput;
    private int _remainingFailureCount;

    /// <summary>Initializes a new instance of the <see cref="EmbeddingProviderFaultPlan"/> class.</summary>
    /// <param name="statusCode">HTTP 429 or 5xx response emitted while failures remain.</param>
    /// <param name="failureCount">Number of failures to emit before normal success resumes.</param>
    /// <param name="retryAfter">Optional Retry-After delay included with HTTP 429.</param>
    /// <param name="requiredInput">Optional exact input for a single-input request.</param>
    public EmbeddingProviderFaultPlan(
        HttpStatusCode statusCode,
        int failureCount,
        TimeSpan? retryAfter = null,
        string? requiredInput = null)
    {
        if (statusCode != HttpStatusCode.TooManyRequests && (int)statusCode is < 500 or > 599)
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

        if (requiredInput is not null)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(requiredInput);
        }

        StatusCode = statusCode;
        InitialFailureCount = failureCount;
        RetryAfter = retryAfter;
        _requiredInput = requiredInput;
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

    /// <summary>Atomically consumes one planned failure for a matching request when any remain.</summary>
    /// <param name="inputs">The validated embedding inputs for the request.</param>
    /// <returns><see langword="true"/> when the caller should emit a failure.</returns>
    internal bool TryConsumeFailure(IReadOnlyList<string> inputs)
    {
        if (_requiredInput is not null &&
            (inputs.Count != 1 || !string.Equals(inputs[0], _requiredInput, StringComparison.Ordinal)))
        {
            return false;
        }

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
