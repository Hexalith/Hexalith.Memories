// <copyright file="EmbeddingMigrationMarkerCorruptException.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Migration;

/// <summary>Exception thrown when an active embedding migration marker hash exists but is malformed.</summary>
/// <remarks>
/// Concurrency safety requires the runtime marker read to fail closed rather than silently disable
/// the guard when the durable marker hash is partially written, missing fields, or carries a foreign
/// tenant id. This exception surfaces those cases so operators and workflow retry/compensation paths
/// can react to durable-state corruption instead of treating it as "no marker present".
/// </remarks>
public sealed class EmbeddingMigrationMarkerCorruptException : InvalidOperationException
{
    /// <summary>Initializes a new instance of the <see cref="EmbeddingMigrationMarkerCorruptException"/> class.</summary>
    /// <param name="tenantId">The tenant whose marker hash is malformed.</param>
    /// <param name="reason">The specific malformation detected by the reader.</param>
    public EmbeddingMigrationMarkerCorruptException(string tenantId, string reason)
        : base($"Active embedding migration marker for tenant '{tenantId}' is malformed: {reason}. Refusing to fail open.")
    {
    }
}
