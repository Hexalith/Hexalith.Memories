// <copyright file="OllamaEmbeddingProvider.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Ingestion;

using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

using Hexalith.Memories.Contracts.V1;

/// <summary>Ollama embedding provider strategy. Owns the Ollama <c>/api/embed</c> request payload, OIDC client-credentials
/// bearer authentication, and response parsing for both single (<c>input</c> string) and batch (<c>input</c> array)
/// generation (Story 23.9, AC3). Transport, auth-retry, and redaction are shared
/// (<see cref="EmbeddingProviderTransport"/>).</summary>
internal sealed class OllamaEmbeddingProvider : IEmbeddingProvider
{
    private readonly EmbeddingSecretStore _secretStore;
    private readonly IOidcTokenProvider? _oidcTokenProvider;

    /// <summary>Initializes a new instance of the <see cref="OllamaEmbeddingProvider"/> class.</summary>
    /// <param name="secretStore">The shared DAPR secret store.</param>
    /// <param name="oidcTokenProvider">The OIDC token provider used by client-credentials authentication, or <c>null</c> when no Ollama tenants are configured.</param>
    public OllamaEmbeddingProvider(EmbeddingSecretStore secretStore, IOidcTokenProvider? oidcTokenProvider)
    {
        ArgumentNullException.ThrowIfNull(secretStore);
        _secretStore = secretStore;
        _oidcTokenProvider = oidcTokenProvider;
    }

    /// <inheritdoc/>
    public string DisplayName => "Ollama";

    /// <inheritdoc/>
    public async Task PrimeAsync(string tenantId, TenantEmbeddingConfig config, CancellationToken ct)
    {
        EnsureSupportedAuthMode(config, tenantId);
        _ = await _secretStore.GetSecretAsync(tenantId, config.ApiSecretKeyName, ct).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task<EmbeddingProviderCredentials> AuthenticateAsync(string tenantId, TenantEmbeddingConfig config, CancellationToken ct)
    {
        EnsureSupportedAuthMode(config, tenantId);
        IOidcTokenProvider tokenProvider = _oidcTokenProvider
            ?? throw new EmbeddingApiException("IOidcTokenProvider is required for Ollama OIDC client credentials authentication.", tenantId);

        string clientSecret = await _secretStore.GetSecretAsync(tenantId, config.ApiSecretKeyName, ct).ConfigureAwait(false);
        string accessToken = await GetAccessTokenWrappedAsync(tokenProvider, config, clientSecret, tenantId, ct).ConfigureAwait(false);
        EnsureNonBlankBearerToken(accessToken, tenantId);
        return new EmbeddingProviderCredentials(accessToken, [clientSecret, accessToken]);
    }

    /// <inheritdoc/>
    public async Task<EmbeddingProviderCredentials> RefreshCredentialsAsync(string tenantId, TenantEmbeddingConfig config, CancellationToken ct)
    {
        IOidcTokenProvider tokenProvider = _oidcTokenProvider
            ?? throw new EmbeddingApiException("IOidcTokenProvider is required for Ollama OIDC client credentials authentication.", tenantId);

        // AC4: evict the cached DAPR client_secret before refreshing the bearer so a rotated secret is re-read
        // symmetrically with the Google API-key path. The refreshed secret becomes the input to the OIDC token request.
        _secretStore.Evict(config.ApiSecretKeyName);
        string refreshedClientSecret = await _secretStore.GetSecretAsync(tenantId, config.ApiSecretKeyName, ct).ConfigureAwait(false);
        string refreshedAccessToken = await InvalidateAndRefreshWrappedAsync(tokenProvider, config, refreshedClientSecret, tenantId, ct).ConfigureAwait(false);
        EnsureNonBlankBearerToken(refreshedAccessToken, tenantId);
        return new EmbeddingProviderCredentials(refreshedAccessToken, [refreshedClientSecret, refreshedAccessToken]);
    }

    /// <inheritdoc/>
    public HttpRequestMessage BuildRequest(IReadOnlyList<string> texts, TenantEmbeddingConfig config, EmbeddingProviderCredentials credentials, bool batch)
    {
        string endpointUrl = BuildEndpointUrl(config);
        string requestJson = batch
            ? JsonSerializer.Serialize(new { model = config.Model, input = texts })
            : JsonSerializer.Serialize(new { model = config.Model, input = texts[0] });

        HttpRequestMessage request = new(HttpMethod.Post, endpointUrl);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", credentials.PrimaryValue);
        request.Content = new StringContent(requestJson, Encoding.UTF8, "application/json");
        return request;
    }

    /// <inheritdoc/>
    public IReadOnlyList<float[]> ParseResponse(string responseBody, int expectedCount, int expectedDimensions, string tenantId, bool batch)
        => batch
            ? ParseBatchResponse(responseBody, expectedCount, expectedDimensions, tenantId)
            : [ParseSingleResponse(responseBody, expectedDimensions, tenantId)];

    private static string BuildEndpointUrl(TenantEmbeddingConfig config)
    {
        if (string.IsNullOrWhiteSpace(config.BaseUrl) ||
            !Uri.TryCreate(config.BaseUrl.Trim(), UriKind.Absolute, out Uri? baseUri) ||
            (baseUri.Scheme != Uri.UriSchemeHttp && baseUri.Scheme != Uri.UriSchemeHttps))
        {
            throw new ArgumentException($"{nameof(config.BaseUrl)} must be an absolute HTTP or HTTPS URL.", nameof(config.BaseUrl));
        }

        string normalized = baseUri.ToString().TrimEnd('/') + "/";
        return new Uri(new Uri(normalized, UriKind.Absolute), "api/embed").ToString();
    }

    private static async Task<string> GetAccessTokenWrappedAsync(
        IOidcTokenProvider tokenProvider,
        TenantEmbeddingConfig config,
        string clientSecret,
        string tenantId,
        CancellationToken ct)
    {
        try
        {
            return await tokenProvider
                .GetAccessTokenAsync(config.OidcTokenEndpoint!, config.OidcClientId!, clientSecret, config.OidcScope, ct)
                .ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex) when (ex is OidcTokenAcquisitionException or HttpRequestException or IOException or TaskCanceledException)
        {
            throw new EmbeddingApiException(
                "Failed to acquire OIDC access token for Ollama embedding provider.",
                tenantId,
                ex);
        }
    }

    private static async Task<string> InvalidateAndRefreshWrappedAsync(
        IOidcTokenProvider tokenProvider,
        TenantEmbeddingConfig config,
        string clientSecret,
        string tenantId,
        CancellationToken ct)
    {
        try
        {
            return await tokenProvider
                .InvalidateAndRefreshAsync(config.OidcTokenEndpoint!, config.OidcClientId!, clientSecret, config.OidcScope, ct)
                .ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex) when (ex is OidcTokenAcquisitionException or HttpRequestException or IOException or TaskCanceledException)
        {
            throw new EmbeddingApiException(
                "Failed to refresh OIDC access token for Ollama embedding provider after a 401/403 response.",
                tenantId,
                ex);
        }
    }

    private static void EnsureNonBlankBearerToken(string accessToken, string tenantId)
    {
        // AuthenticationHeaderValue throws FormatException on whitespace-only parameters; reject before header
        // construction with a sanitized typed exception so callers above ingestion can distinguish a bad-token contract
        // from a bad-network outcome.
        if (string.IsNullOrWhiteSpace(accessToken))
        {
            throw new EmbeddingApiException(
                "OIDC token provider returned a blank access token; refusing to construct a bearer header.",
                tenantId);
        }

        if (!string.Equals(accessToken, accessToken.Trim(), StringComparison.Ordinal))
        {
            throw new EmbeddingApiException(
                "OIDC token provider returned an invalid access token; refusing to construct a bearer header.",
                tenantId);
        }

        try
        {
            _ = new AuthenticationHeaderValue("Bearer", accessToken);
        }
        catch (FormatException ex)
        {
            throw new EmbeddingApiException(
                "OIDC token provider returned an invalid access token; refusing to construct a bearer header.",
                tenantId,
                ex);
        }
    }

    private static void EnsureSupportedAuthMode(TenantEmbeddingConfig config, string tenantId)
    {
        if (string.Equals(config.AuthMode, EmbeddingProviderDefaults.OidcClientCredentialsAuthMode, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        throw new EmbeddingApiException(
            $"Ollama auth mode '{config.AuthMode}' is not supported by EmbeddingClient. " +
            $"Use '{EmbeddingProviderDefaults.OidcClientCredentialsAuthMode}' until a provider-specific api-key contract is defined.",
            tenantId);
    }

    private static float[] ParseSingleResponse(string responseBody, int expectedDimensions, string tenantId)
    {
        try
        {
            using JsonDocument doc = JsonDocument.Parse(responseBody);
            if (!doc.RootElement.TryGetProperty("embeddings", out JsonElement embeddingsElement) ||
                embeddingsElement.ValueKind != JsonValueKind.Array)
            {
                throw new EmbeddingApiException(
                    "Malformed embedding API response: missing 'embeddings' array.",
                    tenantId);
            }

            if (embeddingsElement.GetArrayLength() == 0)
            {
                throw new EmbeddingApiException(
                    "Malformed embedding API response: 'embeddings' array is empty.",
                    tenantId);
            }

            if (embeddingsElement.GetArrayLength() != 1)
            {
                throw new EmbeddingApiException(
                    $"Expected 1 embeddings but received {embeddingsElement.GetArrayLength()}. " +
                    "Embedding API may have returned a truncated or malformed response.",
                    tenantId);
            }

            float[] values = ParseVectorElement(embeddingsElement[0], "first 'embeddings' item", tenantId);
            EnsureExpectedDimensions(values.Length, expectedDimensions, tenantId);
            return values;
        }
        catch (JsonException ex)
        {
            throw new EmbeddingApiException(
                "Malformed embedding API response: invalid JSON or non-numeric vector values.",
                tenantId,
                ex);
        }
    }

    private static IReadOnlyList<float[]> ParseBatchResponse(string responseBody, int expectedCount, int expectedDimensions, string tenantId)
    {
        try
        {
            using JsonDocument doc = JsonDocument.Parse(responseBody);
            if (!doc.RootElement.TryGetProperty("embeddings", out JsonElement embeddingsElement) ||
                embeddingsElement.ValueKind != JsonValueKind.Array)
            {
                throw new EmbeddingApiException(
                    "Malformed embedding API response: missing 'embeddings' array.",
                    tenantId);
            }

            int actualCount = embeddingsElement.GetArrayLength();
            if (actualCount != expectedCount)
            {
                throw new EmbeddingApiException(
                    $"Expected {expectedCount} embeddings but received {actualCount}. " +
                    "Embedding API may have returned a truncated or malformed response.",
                    tenantId);
            }

            float[][] vectors = new float[actualCount][];
            for (int i = 0; i < actualCount; i++)
            {
                float[] values = ParseVectorElement(embeddingsElement[i], $"'embeddings[{i}]' item", tenantId);
                EnsureExpectedDimensions(values.Length, expectedDimensions, tenantId);
                vectors[i] = values;
            }

            return vectors;
        }
        catch (JsonException ex)
        {
            throw new EmbeddingApiException(
                "Malformed embedding API response: invalid JSON or non-numeric vector values.",
                tenantId,
                ex);
        }
    }

    private static float[] ParseVectorElement(JsonElement element, string label, string tenantId)
    {
        if (element.ValueKind != JsonValueKind.Array)
        {
            throw new EmbeddingApiException(
                $"Malformed embedding API response: {label} must be an array.",
                tenantId);
        }

        return element.Deserialize<float[]>()
            ?? throw new EmbeddingApiException(
                $"Malformed embedding API response: {label} deserialized to null.",
                tenantId);
    }

    private static void EnsureExpectedDimensions(int actualDimensions, int expectedDimensions, string tenantId)
    {
        if (actualDimensions != expectedDimensions)
        {
            throw new EmbeddingApiException(
                $"Expected {expectedDimensions} dimensions but received {actualDimensions}. " +
                "Embedding API may have returned a truncated or malformed response.",
                tenantId);
        }
    }
}
