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
    /// <summary>The only supported embedding provider name in the MVP.</summary>
    public const string GoogleProviderName = "google";

    /// <summary>The default Google embedding model used in the MVP.</summary>
    public const string GoogleModelName = "gemini-embedding-001";

    private const int GoogleMaxRateLimitPerMinute = 3000;

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

        if (!string.Equals(config.Provider, GoogleProviderName, StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException(
                $"Provider '{config.Provider}' is not supported in the MVP implementation. Only '{GoogleProviderName}' is currently supported.",
                nameof(config.Provider));
        }

        if (!ModelNamePattern().IsMatch(config.Model))
        {
            throw new ArgumentException(
                "Model must contain only letters, numbers, dots, underscores, and hyphens.",
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

        if (config.RateLimitPerMinute <= 0)
        {
            throw new ArgumentException("RateLimitPerMinute must be greater than 0.", nameof(config.RateLimitPerMinute));
        }

        if (config.RateLimitPerMinute > GoogleMaxRateLimitPerMinute)
        {
            throw new ArgumentException(
                $"RateLimitPerMinute must be {GoogleMaxRateLimitPerMinute} or less to prevent monopolizing shared API keys.",
                nameof(config.RateLimitPerMinute));
        }

        if (!ApiSecretKeyNamePattern().IsMatch(config.ApiSecretKeyName))
        {
            throw new ArgumentException(
                "ApiSecretKeyName must match ^[a-z0-9-]+$ (lowercase alphanumeric and hyphens only).",
                nameof(config.ApiSecretKeyName));
        }
    }

    [GeneratedRegex("^[a-z0-9-]+$")]
    private static partial Regex ApiSecretKeyNamePattern();

    [GeneratedRegex("^[A-Za-z0-9._-]+$")]
    private static partial Regex ModelNamePattern();
}
