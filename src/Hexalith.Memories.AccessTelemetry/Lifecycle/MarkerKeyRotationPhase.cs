// <copyright file="MarkerKeyRotationPhase.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.AccessTelemetry.Lifecycle;

/// <summary>Durable staged marker-key rotation phases.</summary>
internal enum MarkerKeyRotationPhase
{
    /// <summary>No rotation is in progress.</summary>
    Stable,

    /// <summary>A new generation is staged and awaiting live acknowledgements.</summary>
    Staged,

    /// <summary>All live writers acknowledged and old-generation work is draining.</summary>
    Draining,
}
