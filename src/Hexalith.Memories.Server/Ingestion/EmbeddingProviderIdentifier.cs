// <copyright file="EmbeddingProviderIdentifier.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Ingestion;

/// <summary>Parsed persisted embedding provider identifier.</summary>
/// <param name="Provider">The embedding provider name.</param>
/// <param name="Model">The provider-specific embedding model identifier.</param>
internal sealed record EmbeddingProviderIdentifier(string Provider, string Model)
{
    /// <summary>Formats a persisted identifier with a canonical provider name and the original model spelling.</summary>
    /// <param name="provider">The configured embedding provider name.</param>
    /// <param name="model">The provider-specific model identifier.</param>
    /// <returns>The identifier to persist with generated embeddings.</returns>
    internal static string Format(string provider, string model) => $"{provider.ToLowerInvariant()}:{model}";
}
