// <copyright file="EmbeddingProviderRegistry.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Ingestion;

/// <summary>Deterministic, case-insensitive resolver from a configured provider name to its <see cref="IEmbeddingProvider"/>
/// strategy. Supported runtime providers are exactly Google and Ollama as defined by <see cref="EmbeddingProviderDefaults"/>;
/// resolution never silently adds a new runtime provider and preserves the existing structured unsupported-provider error
/// (Story 23.9, AC6).</summary>
internal sealed class EmbeddingProviderRegistry
{
    private readonly GoogleEmbeddingProvider _google;
    private readonly OllamaEmbeddingProvider _ollama;

    /// <summary>Initializes a new instance of the <see cref="EmbeddingProviderRegistry"/> class.</summary>
    /// <param name="google">The Google provider strategy.</param>
    /// <param name="ollama">The Ollama provider strategy.</param>
    public EmbeddingProviderRegistry(GoogleEmbeddingProvider google, OllamaEmbeddingProvider ollama)
    {
        ArgumentNullException.ThrowIfNull(google);
        ArgumentNullException.ThrowIfNull(ollama);
        _google = google;
        _ollama = ollama;
    }

    /// <summary>Resolves the provider strategy for the supplied provider name.</summary>
    /// <param name="provider">The configured provider name (case-insensitive).</param>
    /// <returns>The matching provider strategy.</returns>
    /// <exception cref="ArgumentException">Thrown when the provider is not a supported runtime provider.</exception>
    public IEmbeddingProvider Resolve(string provider)
    {
        if (string.Equals(provider, EmbeddingProviderDefaults.GoogleProviderName, StringComparison.OrdinalIgnoreCase))
        {
            return _google;
        }

        if (string.Equals(provider, EmbeddingProviderDefaults.OllamaProviderName, StringComparison.OrdinalIgnoreCase))
        {
            return _ollama;
        }

        throw new ArgumentException(
            $"Provider '{provider}' is not supported. Supported providers: '{EmbeddingProviderDefaults.GoogleProviderName}', '{EmbeddingProviderDefaults.OllamaProviderName}'.",
            nameof(provider));
    }
}
