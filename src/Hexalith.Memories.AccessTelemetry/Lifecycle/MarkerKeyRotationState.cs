// <copyright file="MarkerKeyRotationState.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.AccessTelemetry.Lifecycle;

/// <summary>Durable fixed-actor marker-key rotation state.</summary>
internal sealed record MarkerKeyRotationState
{
    /// <summary>Gets the current rotation phase.</summary>
    public MarkerKeyRotationPhase Phase { get; init; } = MarkerKeyRotationPhase.Stable;

    /// <summary>Gets the active marker-key generation.</summary>
    public required string ActiveGeneration { get; init; }

    /// <summary>Gets the staged generation.</summary>
    public string? StagedGeneration { get; init; }

    /// <summary>Gets the frozen old generation.</summary>
    public string? FrozenOldGeneration { get; init; }

    /// <summary>Gets the writer keys in the live staging snapshot.</summary>
    public IReadOnlySet<string> RequiredWriterKeys { get; init; } = new HashSet<string>(StringComparer.Ordinal);

    /// <summary>Gets the live writers that acknowledged the staged generation.</summary>
    public IReadOnlySet<string> AcknowledgedWriterKeys { get; init; } = new HashSet<string>(StringComparer.Ordinal);

    /// <summary>Gets the final successful old-generation write time.</summary>
    public long? FinalOldKeyWriteUnixMilliseconds { get; init; }

    /// <summary>Gets the earliest safe old-generation retirement time.</summary>
    public long? OldGenerationRetireAfterUnixMilliseconds { get; init; }
}
