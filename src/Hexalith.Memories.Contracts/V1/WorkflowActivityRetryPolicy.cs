// <copyright file="WorkflowActivityRetryPolicy.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Contracts.V1;

/// <summary>Durable retry policy values captured for a workflow activity.</summary>
public sealed record WorkflowActivityRetryPolicy
{
    /// <summary>Gets the maximum number of attempts.</summary>
    public int MaxAttempts { get; init; } = 5;

    /// <summary>Gets the first retry interval in seconds.</summary>
    public double FirstRetryIntervalSeconds { get; init; } = 2;

    /// <summary>Gets the exponential backoff coefficient.</summary>
    public double BackoffCoefficient { get; init; } = 1.5;

    /// <summary>Gets the maximum retry interval in seconds.</summary>
    public double MaxRetryIntervalSeconds { get; init; } = 300;
}
