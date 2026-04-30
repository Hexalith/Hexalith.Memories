// <copyright file="EmbeddingProviderDefaults.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Ingestion;

using System.Text.RegularExpressions;

using Hexalith.Memories.Contracts.V1;

/// <summary>Default embedding provider configurations and validation.</summary>
public static partial class EmbeddingProviderDefaults
{
    /// <summary>The Google embedding provider name.</summary>
    public const string GoogleProviderName = "google";

    /// <summary>The Ollama embedding provider name.</summary>
    public const string OllamaProviderName = "ollama";

    /// <summary>The default Google embedding model used in the MVP.</summary>
    public const string GoogleModelName = "gemini-embedding-001";

    /// <summary>The default Ollama embedding model (qwen3-embedding:4b — note the colon between model and tag is required by the Ollama identifier convention).</summary>
    public const string OllamaModelName = "qwen3-embedding:4b";

    private const int GoogleMaxRateLimitPerMinute = 3000;

    /// <summary>Default dimension count emitted by qwen3-embedding:4b on a self-hosted Ollama deployment.</summary>
    private const int OllamaDimensions = 2560;

    /// <summary>Self-hosted Ollama has no provider-side quota; this ceiling protects operator backend throughput from accidental misconfiguration.</summary>
    private const int OllamaMaxRateLimitPerMinute = 60_000;

    /// <summary>Returns the default Google embedding configuration using gemini-embedding-001.</summary>
    /// <returns>A <see cref="TenantEmbeddingConfig"/> with Google defaults.</returns>
    public static TenantEmbeddingConfig Google() => new()
    {
        Provider = GoogleProviderName,
        Model = GoogleModelName,
        Dimensions = 768,
        RateLimitPerMinute = 1500,
        ApiSecretKeyName = "google-embedding-api-key",
        ReindexRequired = false,
    };

    /// <summary>Returns the default Ollama embedding configuration using qwen3-embedding:4b (2560 dimensions, self-hosted).</summary>
    /// <returns>A <see cref="TenantEmbeddingConfig"/> with Ollama defaults.</returns>
    public static TenantEmbeddingConfig Ollama() => new()
    {
        Provider = OllamaProviderName,
        Model = OllamaModelName,
        Dimensions = OllamaDimensions,
        RateLimitPerMinute = 6000,
        ApiSecretKeyName = "memories-embedding-client-secret",
        ReindexRequired = false,
    };

    /// <summary>Gets the configuration fields that require a full reindex when changed.</summary>
    /// <param name="currentConfig">The currently stored configuration.</param>
    /// <param name="proposedConfig">The proposed replacement configuration.</param>
    /// <returns>The list of breaking field names.</returns>
    public static string[] GetBreakingChangeFields(
        TenantEmbeddingConfig currentConfig,
        TenantEmbeddingConfig proposedConfig)
    {
        ArgumentNullException.ThrowIfNull(currentConfig);
        ArgumentNullException.ThrowIfNull(proposedConfig);

        List<string> affectedFields = [];

        if (!string.Equals(currentConfig.Provider, proposedConfig.Provider, StringComparison.OrdinalIgnoreCase))
        {
            affectedFields.Add("provider");
        }

        if (!string.Equals(currentConfig.Model, proposedConfig.Model, StringComparison.OrdinalIgnoreCase))
        {
            affectedFields.Add("model");
        }

        if (currentConfig.Dimensions != proposedConfig.Dimensions)
        {
            affectedFields.Add("dimensions");
        }

        return [.. affectedFields];
    }

    /// <summary>Validates a tenant embedding configuration.</summary>
    /// <param name="config">The configuration to validate.</param>
    /// <exception cref="ArgumentNullException">Thrown when config is null.</exception>
    /// <exception cref="ArgumentException">Thrown when any field is invalid.</exception>
    public static void Validate(TenantEmbeddingConfig config)
    {
        ArgumentNullException.ThrowIfNull(config);
        ArgumentException.ThrowIfNullOrWhiteSpace(config.Provider, nameof(config.Provider));
        ArgumentException.ThrowIfNullOrWhiteSpace(config.Model, nameof(config.Model));

        if (!IsSupportedProvider(config.Provider))
        {
            throw new ArgumentException(
                $"Provider '{config.Provider}' is not supported. Supported providers: '{GoogleProviderName}', '{OllamaProviderName}'.",
                nameof(config.Provider));
        }

        if (!ModelNamePattern().IsMatch(config.Model))
        {
            throw new ArgumentException(
                "Model must contain only letters, numbers, dots, colons, underscores, and hyphens.",
                nameof(config.Model));
        }

        if (config.Dimensions <= 0)
        {
            throw new ArgumentException("Dimensions must be greater than 0.", nameof(config.Dimensions));
        }

        if (string.Equals(config.Model, GoogleModelName, StringComparison.OrdinalIgnoreCase) &&
            config.Dimensions is not (768 or 1536 or 3072))
        {
            throw new ArgumentException(
                $"Model '{GoogleModelName}' only supports dimensions 768, 1536, or 3072.",
                nameof(config.Dimensions));
        }

        if (string.Equals(config.Model, OllamaModelName, StringComparison.OrdinalIgnoreCase) &&
            config.Dimensions != OllamaDimensions)
        {
            throw new ArgumentException(
                $"Model '{OllamaModelName}' only supports {OllamaDimensions} dimensions.",
                nameof(config.Dimensions));
        }

        if (config.RateLimitPerMinute <= 0)
        {
            throw new ArgumentException("RateLimitPerMinute must be greater than 0.", nameof(config.RateLimitPerMinute));
        }

        int maxRateLimit = string.Equals(config.Provider, OllamaProviderName, StringComparison.OrdinalIgnoreCase)
            ? OllamaMaxRateLimitPerMinute
            : GoogleMaxRateLimitPerMinute;

        if (config.RateLimitPerMinute > maxRateLimit)
        {
            throw new ArgumentException(
                $"RateLimitPerMinute must be {maxRateLimit} or less for provider '{config.Provider}'.",
                nameof(config.RateLimitPerMinute));
        }

        if (!ApiSecretKeyNamePattern().IsMatch(config.ApiSecretKeyName))
        {
            throw new ArgumentException(
                "ApiSecretKeyName must match ^[a-z0-9-]+$ (lowercase alphanumeric and hyphens only).",
                nameof(config.ApiSecretKeyName));
        }
    }

    private static bool IsSupportedProvider(string provider) =>
        string.Equals(provider, GoogleProviderName, StringComparison.OrdinalIgnoreCase) ||
        string.Equals(provider, OllamaProviderName, StringComparison.OrdinalIgnoreCase);

    [GeneratedRegex("^[a-z0-9-]+$")]
    private static partial Regex ApiSecretKeyNamePattern();

    [GeneratedRegex("^[A-Za-z0-9.:_-]+$")]
    private static partial Regex ModelNamePattern();
}
