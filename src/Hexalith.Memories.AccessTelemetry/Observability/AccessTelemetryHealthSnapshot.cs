// <copyright file="AccessTelemetryHealthSnapshot.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.AccessTelemetry.Observability;

using Hexalith.Memories.AccessTelemetry.Contracts;

/// <summary>Privacy-safe operational health detail.</summary>
internal sealed record AccessTelemetryHealthSnapshot
{
    /// <summary>Gets health with the accepted precedence.</summary>
    public required AccessTelemetryHealthState State { get; init; }

    /// <summary>Gets a bounded reason.</summary>
    public required AccessTelemetryReason Reason { get; init; }

    /// <summary>Gets the bounded cause.</summary>
    public required string Cause { get; init; }

    /// <summary>Gets the bounded impact.</summary>
    public required string Impact { get; init; }

    /// <summary>Gets the accountable owner role.</summary>
    public required string Owner { get; init; }

    /// <summary>Gets the bounded next action.</summary>
    public required string NextAction { get; init; }
}
