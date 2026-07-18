// <copyright file="AccessTelemetryOptionsValidationResult.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.AccessTelemetry.Contracts;

/// <summary>Fail-closed lifecycle validation result that never gates business readiness.</summary>
public sealed record AccessTelemetryOptionsValidationResult
{
    /// <summary>Gets whether all lifecycle configuration gates passed.</summary>
    public required bool IsValid { get; init; }

    /// <summary>Gets whether lifecycle writes may proceed.</summary>
    public required bool AllowsLifecycleWrites { get; init; }

    /// <summary>Gets whether business readiness should stop.</summary>
    public bool StopsBusinessReadiness { get; init; }

    /// <summary>Gets the bounded failure reason.</summary>
    public required AccessTelemetryReason Reason { get; init; }

    /// <summary>Gets the effective bounded retention when valid.</summary>
    public TimeSpan? EffectiveRetention { get; init; }

    /// <summary>Gets bounded configuration errors.</summary>
    public IReadOnlyList<string> Errors { get; init; } = [];
}
