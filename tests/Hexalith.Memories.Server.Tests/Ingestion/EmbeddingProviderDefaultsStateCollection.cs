// <copyright file="EmbeddingProviderDefaultsStateCollection.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Tests.Ingestion;

/// <summary>
/// Serializes tests that mutate or rely on the process-wide static options
/// in EmbeddingProviderDefaults so they cannot race each other in parallel test execution.
/// </summary>
[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class EmbeddingProviderDefaultsStateCollection
{
    /// <summary>The collection name.</summary>
    public const string Name = "EmbeddingProviderDefaultsState";
}
