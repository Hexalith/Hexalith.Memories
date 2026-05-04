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

    /// <summary>The API-key authentication mode.</summary>
    public const string ApiKeyAuthMode = "api-key";

    /// <summary>The OIDC client credentials authentication mode.</summary>
    public const string OidcClientCredentialsAuthMode = "oidc-client-credentials";

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
        BaseUrl = "https://llm.tache.ai",
        AuthMode = OidcClientCredentialsAuthMode,
        OidcTokenEndpoint = "https://auth.tache.ai/realms/tache/protocol/openid-connect/token",
        OidcClientId = "memories-embedding",
        OidcScope = "openid",
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

        if (string.Equals(currentConfig.Provider, OllamaProviderName, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(proposedConfig.Provider, OllamaProviderName, StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(
                NormalizeBaseUrl(currentConfig.BaseUrl),
                NormalizeBaseUrl(proposedConfig.BaseUrl),
                StringComparison.OrdinalIgnoreCase))
        {
            affectedFields.Add("baseUrl");
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
                "Model must start with a letter or number and contain only letters, numbers, dots, colons, underscores, and hyphens.",
                nameof(config.Model));
        }

        if (!IsSupportedAuthMode(config.AuthMode))
        {
            throw new ArgumentException(
                $"AuthMode {DescribeAuthMode(config.AuthMode)} is not supported. Supported auth modes: '{ApiKeyAuthMode}', '{OidcClientCredentialsAuthMode}'.",
                nameof(config.AuthMode));
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

        bool isOllama = string.Equals(config.Provider, OllamaProviderName, StringComparison.OrdinalIgnoreCase);
        bool isOidcClientCredentials = string.Equals(config.AuthMode, OidcClientCredentialsAuthMode, StringComparison.OrdinalIgnoreCase);

        // AC9: any non-empty BaseUrl / OidcTokenEndpoint must parse as an absolute HTTP(S) URL,
        // independent of provider or auth mode. Mode-specific required-presence checks follow.
        ValidateOptionalHttpUrl(config.BaseUrl, nameof(config.BaseUrl));
        ValidateOptionalHttpUrl(config.OidcTokenEndpoint, nameof(config.OidcTokenEndpoint));

        // AC4: OIDC client-credentials auth is only meaningful for the Ollama provider. Reject the
        // mode itself on non-Ollama providers so the configuration cannot enable token acquisition
        // behavior on a provider that does not support it.
        if (isOidcClientCredentials && !isOllama)
        {
            throw new ArgumentException(
                $"AuthMode '{OidcClientCredentialsAuthMode}' is only supported for the '{OllamaProviderName}' provider.",
                nameof(config.AuthMode));
        }

        if (isOllama && string.IsNullOrWhiteSpace(config.BaseUrl))
        {
            throw new ArgumentException(
                $"{nameof(config.BaseUrl)} is required for this embedding provider configuration.",
                nameof(config.BaseUrl));
        }

        if (isOidcClientCredentials)
        {
            if (string.IsNullOrWhiteSpace(config.OidcTokenEndpoint))
            {
                throw new ArgumentException(
                    $"{nameof(config.OidcTokenEndpoint)} is required for this embedding provider configuration.",
                    nameof(config.OidcTokenEndpoint));
            }

            if (string.IsNullOrWhiteSpace(config.OidcClientId))
            {
                throw new ArgumentException(
                    "OidcClientId is required for OIDC client credentials authentication.",
                    nameof(config.OidcClientId));
            }
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

    private static bool IsSupportedAuthMode(string? authMode) =>
        string.Equals(authMode, ApiKeyAuthMode, StringComparison.OrdinalIgnoreCase) ||
        string.Equals(authMode, OidcClientCredentialsAuthMode, StringComparison.OrdinalIgnoreCase);

    private static void ValidateOptionalHttpUrl(string? value, string propertyName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return;
        }

        if (!Uri.TryCreate(value, UriKind.Absolute, out Uri? uri) ||
            (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
        {
            throw new ArgumentException($"{propertyName} must be an absolute HTTP or HTTPS URL.", propertyName);
        }

        // AC3 (Story 14.3): reject credential-bearing URL shapes for both BaseUrl and
        // OidcTokenEndpoint. Error text deliberately does not echo the offending URL component
        // so embedded user-info, query secrets, or fragment values cannot leak through logs.
        if (!string.IsNullOrEmpty(uri.UserInfo))
        {
            throw new ArgumentException(
                $"{propertyName} must not contain embedded credentials (user-info component).",
                propertyName);
        }

        // RFC 6749 §3.2 permits token endpoints with query components in the abstract; this
        // repository rejects them uniformly so credential-bearing query strings (e.g.,
        // `?client_secret=...`) cannot slip past validation. Provider base URLs likewise reject
        // query strings.
        if (!string.IsNullOrEmpty(uri.Query))
        {
            throw new ArgumentException(
                $"{propertyName} must not contain a query string.",
                propertyName);
        }

        if (!string.IsNullOrEmpty(uri.Fragment))
        {
            throw new ArgumentException(
                $"{propertyName} must not contain a fragment.",
                propertyName);
        }
    }

    private static string DescribeAuthMode(string? authMode) => authMode switch
    {
        null => "<null>",
        "" => "<empty>",
        _ when string.IsNullOrWhiteSpace(authMode) => "<whitespace>",
        _ => $"'{authMode}'",
    };

    private static string NormalizeBaseUrl(string? baseUrl)
        => (baseUrl ?? string.Empty).Trim().TrimEnd('/');

    [GeneratedRegex("^[a-z0-9-]+$")]
    private static partial Regex ApiSecretKeyNamePattern();

    [GeneratedRegex("^[A-Za-z0-9][A-Za-z0-9.:_-]*$")]
    private static partial Regex ModelNamePattern();
}
