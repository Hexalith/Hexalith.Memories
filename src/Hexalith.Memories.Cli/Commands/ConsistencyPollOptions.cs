// <copyright file="ConsistencyPollOptions.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Cli.Commands;

/// <summary>
/// Options controlling <c>memories consistency verify|repair --wait</c> polling cadence.
/// Exposed so tests can collapse the interval to zero instead of waiting real seconds.
/// </summary>
public sealed class ConsistencyPollOptions
{
    /// <summary>Interval between status polls while waiting for a consistency workflow.</summary>
    public TimeSpan PollInterval { get; set; } = TimeSpan.FromSeconds(5);

    /// <summary>Hard cap on the total <c>--wait</c> duration.</summary>
    public TimeSpan PollTimeout { get; set; } = TimeSpan.FromMinutes(30);
}
