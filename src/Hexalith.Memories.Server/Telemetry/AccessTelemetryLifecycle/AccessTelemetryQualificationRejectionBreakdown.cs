// <copyright file="AccessTelemetryQualificationRejectionBreakdown.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Telemetry.AccessTelemetryLifecycle;

using System.Collections.Immutable;

using Hexalith.Memories.AccessTelemetry.Contracts;

/// <summary>Separates bounded, secret-free pre-queue rejections for one fixed-workload segment.</summary>
/// <param name="SanitizerUnavailable">The records rejected because no validated sanitizer was published.</param>
/// <param name="SanitationFailed">The records rejected because the published sanitizer refused the typed record.</param>
/// <param name="SanitizerUnavailableLifecycleReasons">
/// The sanitizer-unavailable rejections counted by the lifecycle status reason observed at rejection. Keys are
/// <see cref="AccessTelemetryReason"/> names or <see cref="Unobserved"/>; only nonzero counts are listed.
/// </param>
/// <param name="SanitizerUnavailableBootstrapStages">
/// The sanitizer-unavailable rejections counted by the bootstrap stage observed at rejection. Keys are
/// <see cref="AccessTelemetryLifecycleBootstrapStage"/> names or <see cref="Unobserved"/>; only nonzero counts are listed.
/// </param>
/// <param name="LifecycleHealth">The lifecycle health name observed when the observation was created.</param>
/// <param name="LifecycleReason">The lifecycle status reason name observed when the observation was created.</param>
/// <param name="Bootstrap">The bootstrap progress observed when the observation was created.</param>
internal sealed record AccessTelemetryQualificationRejectionBreakdown(
    long SanitizerUnavailable,
    long SanitationFailed,
    IReadOnlyDictionary<string, long> SanitizerUnavailableLifecycleReasons,
    IReadOnlyDictionary<string, long> SanitizerUnavailableBootstrapStages,
    string LifecycleHealth,
    string LifecycleReason,
    AccessTelemetryLifecycleBootstrapSnapshot Bootstrap)
{
    /// <summary>The bounded marker used when no defined lifecycle state was observable.</summary>
    public const string Unobserved = "Unobserved";

    private readonly IReadOnlyDictionary<string, long> _reasons = Freeze(SanitizerUnavailableLifecycleReasons);
    private readonly IReadOnlyDictionary<string, long> _stages = Freeze(SanitizerUnavailableBootstrapStages);

    /// <summary>Gets an empty breakdown whose lifecycle state and bootstrap progress were not observed.</summary>
    public static AccessTelemetryQualificationRejectionBreakdown Empty { get; } = new(
        0,
        0,
        ImmutableSortedDictionary<string, long>.Empty.WithComparers(StringComparer.Ordinal),
        ImmutableSortedDictionary<string, long>.Empty.WithComparers(StringComparer.Ordinal),
        Unobserved,
        Unobserved,
        AccessTelemetryLifecycleBootstrapSnapshot.NotObserved);

    /// <summary>Gets the sanitizer-unavailable rejections by lifecycle reason, as an owned read-only copy.</summary>
    public IReadOnlyDictionary<string, long> SanitizerUnavailableLifecycleReasons
    {
        get => _reasons;
        init => _reasons = Freeze(value);
    }

    /// <summary>Gets the sanitizer-unavailable rejections by bootstrap stage, as an owned read-only copy.</summary>
    public IReadOnlyDictionary<string, long> SanitizerUnavailableBootstrapStages
    {
        get => _stages;
        init => _stages = Freeze(value);
    }

    /// <summary>Gets the bounded name for one lifecycle reason.</summary>
    /// <param name="reason">The observed reason, if any.</param>
    /// <returns>The defined enum name, or <see cref="Unobserved"/>.</returns>
    public static string ReasonName(AccessTelemetryReason? reason)
        => reason is { } value && Enum.IsDefined(value) ? value.ToString() : Unobserved;

    /// <summary>Gets the bounded name for one observed bootstrap stage.</summary>
    /// <param name="stage">The observed stage name, if any.</param>
    /// <returns>The defined stage name, or <see cref="Unobserved"/>.</returns>
    public static string StageName(string? stage)
        => stage is not null && Enum.TryParse(stage, ignoreCase: false, out AccessTelemetryLifecycleBootstrapStage value) &&
            Enum.IsDefined(value) && string.Equals(value.ToString(), stage, StringComparison.Ordinal)
                ? stage
                : Unobserved;

    /// <summary>Subtracts an earlier monotonic breakdown, keeping only positive reason and stage deltas.</summary>
    /// <param name="earlier">The earlier breakdown for the same segment correlation.</param>
    /// <returns>The non-negative breakdown delta.</returns>
    public AccessTelemetryQualificationRejectionBreakdown Since(AccessTelemetryQualificationRejectionBreakdown earlier)
    {
        ArgumentNullException.ThrowIfNull(earlier);
        return this with
        {
            SanitizerUnavailable = Math.Max(0, SanitizerUnavailable - earlier.SanitizerUnavailable),
            SanitationFailed = Math.Max(0, SanitationFailed - earlier.SanitationFailed),
            SanitizerUnavailableLifecycleReasons = PositiveDeltas(SanitizerUnavailableLifecycleReasons, earlier.SanitizerUnavailableLifecycleReasons),
            SanitizerUnavailableBootstrapStages = PositiveDeltas(SanitizerUnavailableBootstrapStages, earlier.SanitizerUnavailableBootstrapStages),
        };
    }

    /// <summary>Returns this breakdown with the lifecycle state observed now.</summary>
    /// <param name="status">The current lifecycle status, or <see langword="null"/> when not wired.</param>
    /// <returns>The breakdown carrying bounded lifecycle health and reason names.</returns>
    public AccessTelemetryQualificationRejectionBreakdown WithLifecycle(AccessTelemetryLifecycleStatusSnapshot? status)
        => this with
        {
            LifecycleHealth = status is not null && Enum.IsDefined(status.Health) ? status.Health.ToString() : Unobserved,
            LifecycleReason = ReasonName(status?.Reason),
        };

    /// <summary>Returns this breakdown with the bootstrap progress observed now.</summary>
    /// <param name="bootstrap">The current bootstrap progress, or <see langword="null"/> when not wired.</param>
    /// <returns>The breakdown carrying the bounded bootstrap snapshot.</returns>
    public AccessTelemetryQualificationRejectionBreakdown WithBootstrap(AccessTelemetryLifecycleBootstrapSnapshot? bootstrap)
        => this with { Bootstrap = bootstrap ?? AccessTelemetryLifecycleBootstrapSnapshot.NotObserved };

    private static ImmutableSortedDictionary<string, long> PositiveDeltas(
        IReadOnlyDictionary<string, long> current,
        IReadOnlyDictionary<string, long> earlier)
    {
        ImmutableSortedDictionary<string, long>.Builder deltas = ImmutableSortedDictionary.CreateBuilder<string, long>(StringComparer.Ordinal);
        foreach (KeyValuePair<string, long> entry in current)
        {
            long before = earlier.TryGetValue(entry.Key, out long value) ? value : 0;
            if (entry.Value - before > 0)
            {
                deltas[entry.Key] = entry.Value - before;
            }
        }

        return deltas.ToImmutable();
    }

    private static IReadOnlyDictionary<string, long> Freeze(IReadOnlyDictionary<string, long>? source)
        => source switch
        {
            null => ImmutableSortedDictionary<string, long>.Empty.WithComparers(StringComparer.Ordinal),
            ImmutableSortedDictionary<string, long> frozen when ReferenceEquals(frozen.KeyComparer, StringComparer.Ordinal) => frozen,
            _ => ImmutableSortedDictionary.CreateRange(StringComparer.Ordinal, source),
        };
}
