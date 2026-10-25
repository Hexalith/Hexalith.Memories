// <copyright file="AccessTelemetryLifecycleBootstrapSnapshot.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Telemetry.AccessTelemetryLifecycle;

using Hexalith.Memories.AccessTelemetry.Contracts;

/// <summary>Bounded, secret-free progress of the lifecycle sanitizer bootstrap.</summary>
/// <param name="Attempts">The number of completed bootstrap attempts, successful or failed.</param>
/// <param name="Stage">
/// The in-flight or last <see cref="AccessTelemetryLifecycleBootstrapStage"/> name, or <see cref="Unobserved"/> when no
/// bootstrap progress is wired.
/// </param>
/// <param name="FailureStage">The stage of the last failed attempt, or <see langword="null"/> before any failure.</param>
/// <param name="FailureKind">
/// The <see cref="AccessTelemetryLifecycleBootstrapFailureKind"/> name of the last failure, or <see langword="null"/> before any failure.
/// </param>
/// <param name="FirstPublishedUtcMs">The UTC Unix milliseconds of the first sanitizer publication, or <see langword="null"/> until then.</param>
/// <param name="CapabilityDiagnostic">The latest optional remote capability observation; absent on older responses.</param>
internal sealed record AccessTelemetryLifecycleBootstrapSnapshot(
    long Attempts,
    string Stage,
    string? FailureStage,
    string? FailureKind,
    long? FirstPublishedUtcMs,
    [property: System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
    AccessTelemetryCapabilityDiagnosticSnapshot? CapabilityDiagnostic = null)
{
    /// <summary>The bounded marker used when no bootstrap progress was observable.</summary>
    public const string Unobserved = "Unobserved";

    /// <summary>Gets the progress before the first bootstrap attempt.</summary>
    public static AccessTelemetryLifecycleBootstrapSnapshot NotStarted { get; } = new(
        0,
        nameof(AccessTelemetryLifecycleBootstrapStage.NotStarted),
        null,
        null,
        null);

    /// <summary>Gets the snapshot reported when no bootstrap progress is wired.</summary>
    public static AccessTelemetryLifecycleBootstrapSnapshot NotObserved { get; } = new(0, Unobserved, null, null, null);
}
