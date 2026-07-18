// <copyright file="LifecycleConfigurationEpoch.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.AccessTelemetry.Contracts;

/// <summary>Immutable lifecycle configuration epoch stored by the fixed actor.</summary>
public sealed record LifecycleConfigurationEpoch
{
    /// <summary>Gets the epoch ULID.</summary>
    public required string Epoch { get; init; }

    /// <summary>Gets the exact schema version.</summary>
    public required int SchemaVersion { get; init; }

    /// <summary>Gets the component-profile hash.</summary>
    public required string ComponentProfileHash { get; init; }

    /// <summary>Gets the bounded retention seconds.</summary>
    public required int RetentionSeconds { get; init; }

    /// <summary>Gets the active marker-key generation.</summary>
    public required string MarkerKeyGeneration { get; init; }
}
