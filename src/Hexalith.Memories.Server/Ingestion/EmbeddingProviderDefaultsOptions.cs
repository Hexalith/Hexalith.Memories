// <copyright file="EmbeddingProviderDefaultsOptions.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Ingestion;

/// <summary>Configuration-sourced default endpoints for the built-in embedding providers
/// (spec-infrastructure-dependency-abstraction — F1/F2, Decision D30). Bound from the
/// <c>EmbeddingProviders</c> configuration section so the shared Ollama/Google provider defaults are no
/// longer embedded as endpoint literals inside the <see cref="EmbeddingProviderDefaults"/> registry/
/// validation logic; Aspire/appsettings can override them per environment.</summary>
/// <remarks>The property-initializer values are the pre-change built-in defaults, kept here so the static
/// no-config path (<see cref="EmbeddingProviderDefaults.Ollama"/> / <see cref="EmbeddingProviderDefaults.Google"/>
/// and pre-startup or unit-test callers that never invoke <see cref="EmbeddingProviderDefaults.Configure"/>)
/// keeps producing byte-for-byte identical configs. Under the frozen "preserve produced default values"
/// boundary the value must survive somewhere; localizing it to this dedicated options type + appsettings
/// (overridable) is the config-sourcing the invariant asks for.</remarks>
public sealed record EmbeddingProviderDefaultsOptions
{
    /// <summary>Configuration section name bound to this options type.</summary>
    public const string SectionName = "EmbeddingProviders";

    /// <summary>Ollama built-in provider default endpoint + OIDC metadata.</summary>
    public OllamaProviderDefaults Ollama { get; init; } = new();

    /// <summary>Google built-in provider default endpoint.</summary>
    public GoogleProviderDefaults Google { get; init; } = new();
}

/// <summary>Config-sourced default endpoint and OIDC metadata for the built-in Ollama embedding provider.</summary>
public sealed record OllamaProviderDefaults
{
    /// <summary>Base URL of the self-hosted Ollama-compatible embedding endpoint.</summary>
    public string? BaseUrl { get; init; } = "https://llm.tache.ai";

    /// <summary>OIDC client-credentials token endpoint.</summary>
    public string? OidcTokenEndpoint { get; init; } = "https://auth.tache.ai/realms/tache/protocol/openid-connect/token";

    /// <summary>OIDC client id used for client-credentials token acquisition.</summary>
    public string? OidcClientId { get; init; } = "memories-embedding";

    /// <summary>OIDC scope requested during token acquisition.</summary>
    public string? OidcScope { get; init; } = "openid";
}

/// <summary>Config-sourced default endpoint for the built-in Google embedding provider.</summary>
public sealed record GoogleProviderDefaults
{
    /// <summary>Base URL of the Google Generative Language embedding models endpoint.</summary>
    public string ApiBaseUrl { get; init; } = "https://generativelanguage.googleapis.com/v1beta/models";
}
