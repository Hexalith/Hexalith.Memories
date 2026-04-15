// <copyright file="ActivityRetryPolicy.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Ingestion;

/// <summary>Per-activity retry policy bound from <c>Ingestion:RetryPolicies:&lt;activityName&gt;</c>.</summary>
public sealed record ActivityRetryPolicy
{
    /// <summary>Gets the maximum number of attempts (must be &gt; 0).</summary>
    public int MaxAttempts { get; init; } = 5;

    /// <summary>Gets the first retry interval in seconds.</summary>
    public double FirstRetryIntervalSeconds { get; init; } = 2.0;

    /// <summary>Gets the backoff coefficient.</summary>
    public double BackoffCoefficient { get; init; } = 1.5;

    /// <summary>Gets the maximum retry interval in seconds.</summary>
    public double MaxRetryIntervalSeconds { get; init; } = 300.0;
}
