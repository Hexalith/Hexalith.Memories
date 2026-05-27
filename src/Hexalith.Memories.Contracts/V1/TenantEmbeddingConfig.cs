// <copyright file="TenantEmbeddingConfig.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Contracts.V1;

/// <summary>Per-tenant embedding provider configuration.</summary>
public sealed record TenantEmbeddingConfig
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TenantEmbeddingConfig"/> class for use with
    /// object-initializer syntax (e.g. <c>new() { Provider = "google", ... }</c>). The
    /// <c>required</c> modifier on each mandatory property prevents construction at compile time
    /// without a complete initializer, so this ctor cannot be called directly without setting them.
    /// </summary>
    public TenantEmbeddingConfig()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="TenantEmbeddingConfig"/> class. This is the
    /// JSON deserialization entry point for <see cref="MemoriesJsonContext"/>; the parameter
    /// default for <paramref name="authMode"/> is the only mechanism that distinguishes
    /// "<c>authMode</c> missing from legacy JSON" (defaults to <c>"api-key"</c>) from
    /// "<c>authMode</c> explicitly null" (kept as null, fails validation with a clear message).
    /// </summary>
    /// <param name="provider">The embedding provider name.</param>
    /// <param name="model">The embedding model name.</param>
    /// <param name="dimensions">The embedding vector dimension count.</param>
    /// <param name="rateLimitPerMinute">The maximum configured embedding requests per minute.</param>
    /// <param name="apiSecretKeyName">The provider API key or OIDC client secret name.</param>
    /// <param name="reindexRequired">Whether existing embeddings must be reindexed for this configuration.</param>
    /// <param name="baseUrl">The base URL of the embedding provider endpoint or gateway.</param>
    /// <param name="authMode">The authentication mode used by the embedding provider configuration.</param>
    /// <param name="oidcTokenEndpoint">The OIDC token endpoint used for client credentials authentication.</param>
    /// <param name="oidcClientId">The OIDC client identifier used for client credentials authentication.</param>
    /// <param name="oidcScope">The optional OIDC scope requested during client credentials authentication.</param>
    [System.Diagnostics.CodeAnalysis.SetsRequiredMembers]
    [System.Text.Json.Serialization.JsonConstructor]
    public TenantEmbeddingConfig(
        string provider,
        string model,
        int dimensions,
        int rateLimitPerMinute,
        string apiSecretKeyName,
        bool reindexRequired = false,
        string? baseUrl = null,
        string authMode = "api-key",
        string? oidcTokenEndpoint = null,
        string? oidcClientId = null,
        string? oidcScope = null)
    {
        Provider = provider;
        Model = model;
        Dimensions = dimensions;
        RateLimitPerMinute = rateLimitPerMinute;
        ApiSecretKeyName = apiSecretKeyName;
        ReindexRequired = reindexRequired;
        BaseUrl = baseUrl;
        AuthMode = authMode;
        OidcTokenEndpoint = oidcTokenEndpoint;
        OidcClientId = oidcClientId;
        OidcScope = oidcScope;
    }

    /// <summary>The embedding provider name.</summary>
    public required string Provider { get; init; }

    /// <summary>The embedding model name.</summary>
    public required string Model { get; init; }

    /// <summary>The embedding vector dimension count.</summary>
    public required int Dimensions { get; init; }

    /// <summary>The maximum configured embedding requests per minute.</summary>
    public required int RateLimitPerMinute { get; init; }

    /// <summary>
    /// The <em>name / identifier</em> of the secret that stores the provider API key
    /// or, in OIDC mode, the OIDC <c>client_secret</c> (e.g.
    /// <c>"google-embedding-api-key"</c> or <c>"memories-embedding-client-secret"</c>) — NOT the secret value itself.
    /// The server resolves the secret via the configured DAPR secret store at embedding time;
    /// the name is safe to return in public-facing configuration responses (see Story 5.5 AC2).
    /// </summary>
    public required string ApiSecretKeyName { get; init; }

    /// <summary>Whether existing embeddings must be reindexed for this configuration.</summary>
    public bool ReindexRequired { get; init; }

    /// <summary>The base URL of the embedding provider endpoint or gateway.</summary>
    public string? BaseUrl { get; init; }

    /// <summary>
    /// The authentication mode used by the embedding provider configuration. Defaults to <c>"api-key"</c>
    /// so that legacy JSON payloads omitting this property remain wire-compatible. Supported values are
    /// <c>"api-key"</c> and <c>"oidc-client-credentials"</c>; explicit <c>null</c>, empty, or whitespace
    /// values fail validation rather than fall back to the default.
    /// </summary>
    public string AuthMode { get; init; } = "api-key";

    /// <summary>The OIDC token endpoint used for client credentials authentication.</summary>
    public string? OidcTokenEndpoint { get; init; }

    /// <summary>The OIDC client identifier used for client credentials authentication.</summary>
    public string? OidcClientId { get; init; }

    /// <summary>The optional OIDC scope requested during client credentials authentication.</summary>
    public string? OidcScope { get; init; }
}
