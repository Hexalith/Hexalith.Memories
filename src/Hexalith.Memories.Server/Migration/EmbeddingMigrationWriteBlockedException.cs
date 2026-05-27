// <copyright file="EmbeddingMigrationWriteBlockedException.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Migration;

/// <summary>Exception thrown when an active embedding migration marker blocks a stale semantic vector write.</summary>
public sealed class EmbeddingMigrationWriteBlockedException : InvalidOperationException
{
    /// <summary>Initializes a new instance of the <see cref="EmbeddingMigrationWriteBlockedException"/> class.</summary>
    /// <param name="tenantId">The tenant protected by the active marker.</param>
    /// <param name="expectedProvider">The marker target provider.</param>
    /// <param name="expectedModel">The marker target model.</param>
    /// <param name="expectedDimensions">The marker target dimensions.</param>
    /// <param name="actualProvider">The attempted write provider.</param>
    /// <param name="actualModel">The attempted write model.</param>
    /// <param name="actualDimensions">The attempted write dimensions.</param>
    public EmbeddingMigrationWriteBlockedException(
        string tenantId,
        string expectedProvider,
        string expectedModel,
        int expectedDimensions,
        string actualProvider,
        string actualModel,
        int actualDimensions)
        : base(
            $"Embedding vector write blocked by active tenant migration marker for tenant '{tenantId}'. "
            + $"Expected target '{expectedProvider}/{expectedModel}/{expectedDimensions}', "
            + $"but write used '{actualProvider}/{actualModel}/{actualDimensions}'.")
    {
    }
}
