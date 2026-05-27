// <copyright file="EmbeddingClient.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Ingestion;

using System.Collections.Concurrent;
using System.Net;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

using Dapr.Client;

using Hexalith.Memories.Contracts.V1;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

/// <summary>Singleton HTTP client for generating embeddings via configurable provider APIs.</summary>
public class EmbeddingClient
{
    /// <summary>Minimum length below which a value is not redacted, to avoid masking benign short
    /// substrings (e.g., common words) that happen to overlap with a sensitive value.</summary>
    internal const int RedactionMinLength = 8;

    private const string FakeEmbeddingConfigKey = "Memories:Testing:UseFakeEmbedding";
    private const string SecretStoreName = "secretstore";

    private readonly ConcurrentDictionary<string, string> _apiKeyCache = new();
    private readonly DaprClient _daprClient;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IOidcTokenProvider? _oidcTokenProvider;
    private readonly bool _useFakeEmbedding;

    /// <summary>Initializes a new instance of the <see cref="EmbeddingClient"/> class.</summary>
    /// <param name="httpClientFactory">The HTTP client factory for creating HTTP clients.</param>
    /// <param name="daprClient">The DAPR client for secret retrieval.</param>
    /// <param name="configuration">The application configuration.</param>
    /// <param name="hostEnvironment">The current host environment.</param>
    public EmbeddingClient(
        IHttpClientFactory httpClientFactory,
        DaprClient daprClient,
        IConfiguration configuration,
        IHostEnvironment hostEnvironment)
        : this(httpClientFactory, daprClient, configuration, hostEnvironment, oidcTokenProvider: null)
    {
    }

    /// <summary>Initializes a new instance of the <see cref="EmbeddingClient"/> class.</summary>
    /// <param name="httpClientFactory">The HTTP client factory for creating HTTP clients.</param>
    /// <param name="daprClient">The DAPR client for secret retrieval.</param>
    /// <param name="configuration">The application configuration.</param>
    /// <param name="hostEnvironment">The current host environment.</param>
    /// <param name="oidcTokenProvider">The OIDC token provider used by Ollama client-credentials authentication, or <c>null</c> when no Ollama tenants are configured.</param>
    public EmbeddingClient(
        IHttpClientFactory httpClientFactory,
        DaprClient daprClient,
        IConfiguration configuration,
        IHostEnvironment hostEnvironment,
        IOidcTokenProvider? oidcTokenProvider)
    {
        ArgumentNullException.ThrowIfNull(httpClientFactory);
        ArgumentNullException.ThrowIfNull(daprClient);
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(hostEnvironment);

        _httpClientFactory = httpClientFactory;
        _daprClient = daprClient;
        _oidcTokenProvider = oidcTokenProvider;
        _useFakeEmbedding = configuration.GetValue<bool>(FakeEmbeddingConfigKey);

        if (_useFakeEmbedding && !hostEnvironment.IsDevelopment())
        {
            throw new InvalidOperationException("Fake embeddings are only supported in development and test environments.");
        }
    }

    /// <summary>Loads and caches the embedding API key so configuration failures happen before rate-limit budget is consumed.</summary>
    /// <param name="tenantId">The tenant identifier for error context.</param>
    /// <param name="config">The tenant embedding configuration containing the API secret key name.</param>
    /// <param name="ct">A cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public virtual async Task PrimeApiKeyAsync(string tenantId, TenantEmbeddingConfig config, CancellationToken ct)
    {
        EmbeddingProviderDefaults.Validate(config);

        if (_useFakeEmbedding)
        {
            return;
        }

        EnsureSupportedOllamaAuthMode(config, tenantId);
        _ = await GetApiKeyAsync(tenantId, config.ApiSecretKeyName, ct).ConfigureAwait(false);
    }

    /// <summary>Generates an embedding vector for the given text using the tenant's configured provider.</summary>
    /// <param name="text">The text content to embed.</param>
    /// <param name="tenantId">The tenant identifier for error context.</param>
    /// <param name="config">The tenant embedding configuration.</param>
    /// <param name="ct">A cancellation token.</param>
    /// <returns>A float array with the configured number of dimensions.</returns>
    public virtual async Task<float[]> GenerateAsync(string text, string tenantId, TenantEmbeddingConfig config, CancellationToken ct)
    {
        ArgumentException.ThrowIfNullOrEmpty(text);
        ArgumentNullException.ThrowIfNull(config);
        EmbeddingProviderDefaults.Validate(config);

        if (_useFakeEmbedding)
        {
            return CreateDeterministicVector(text, config.Dimensions);
        }

        if (IsGoogle(config.Provider))
        {
            return await GenerateGoogleAsync(text, tenantId, config, ct).ConfigureAwait(false);
        }

        if (IsOllama(config.Provider))
        {
            return await GenerateOllamaAsync(text, tenantId, config, ct).ConfigureAwait(false);
        }

        throw new ArgumentException(
            $"Provider '{config.Provider}' is not supported. Supported providers: '{EmbeddingProviderDefaults.GoogleProviderName}', '{EmbeddingProviderDefaults.OllamaProviderName}'.",
            nameof(config.Provider));
    }

    /// <summary>Parses a persisted embedding provider identifier using the first colon as separator.</summary>
    /// <param name="embeddingProvider">The persisted embedding provider identifier.</param>
    /// <returns>The parsed provider and model components.</returns>
    /// <exception cref="ArgumentException">Thrown when the identifier is malformed or uses an unsupported provider.</exception>
    internal static EmbeddingProviderIdentifier ParseEmbeddingProviderIdentifier(string embeddingProvider)
    {
        if (string.IsNullOrWhiteSpace(embeddingProvider))
        {
            throw CreateEmbeddingProviderIdentifierException(embeddingProvider);
        }

        int separatorIndex = embeddingProvider.IndexOf(':', StringComparison.Ordinal);
        if (separatorIndex <= 0 || separatorIndex == embeddingProvider.Length - 1)
        {
            throw CreateEmbeddingProviderIdentifierException(embeddingProvider);
        }

        string provider = embeddingProvider[..separatorIndex].Trim();
        string model = embeddingProvider[(separatorIndex + 1)..].Trim();
        if (string.IsNullOrWhiteSpace(provider) || string.IsNullOrWhiteSpace(model) || (!IsGoogle(provider) && !IsOllama(provider)))
        {
            throw CreateEmbeddingProviderIdentifierException(embeddingProvider);
        }

        return new EmbeddingProviderIdentifier(provider.ToLowerInvariant(), model);
    }

    /// <summary>Redacts the supplied sensitive values from <paramref name="text"/> using a length-aware,
    /// longest-first substring replacement so overlapping secrets are fully masked and short benign
    /// substrings are not accidentally masked.</summary>
    /// <param name="text">The upstream payload that may contain leaked secrets.</param>
    /// <param name="sensitiveValues">The values that must not appear in the redacted output.</param>
    /// <returns>The redacted string.</returns>
    internal static string RedactSensitiveValues(string text, IReadOnlyCollection<string?> sensitiveValues)
        => RedactSensitiveValues(text, sensitiveValues, fullInputText: null);

    /// <summary>Redacts the supplied sensitive values plus the full input text when present.</summary>
    /// <param name="text">The upstream payload that may contain leaked secrets.</param>
    /// <param name="sensitiveValues">The secret-like values that must not appear in the redacted output.</param>
    /// <param name="fullInputText">The full embedding input text, redacted without the secret-length floor.</param>
    /// <returns>The redacted string.</returns>
    internal static string RedactSensitiveValues(
        string text,
        IReadOnlyCollection<string?> sensitiveValues,
        string? fullInputText)
    {
        if (string.IsNullOrEmpty(text))
        {
            return text;
        }

        // Filter to non-blank, sufficiently-long, distinct values; longest first so a longer
        // secret is replaced before any shorter secret it contains as a substring.
        IEnumerable<string> ordered = (sensitiveValues ?? [])
            .Where(static v => !string.IsNullOrWhiteSpace(v) && v!.Length >= RedactionMinLength)
            .Select(static v => v!)
            .Distinct(StringComparer.Ordinal)
            .OrderByDescending(static v => v.Length);

        string sanitized = text;
        foreach (string value in ordered)
        {
            sanitized = sanitized.Replace(value, "[redacted]", StringComparison.Ordinal);
        }

        if (!string.IsNullOrEmpty(fullInputText))
        {
            sanitized = sanitized.Replace(fullInputText, "[redacted]", StringComparison.Ordinal);
        }

        return sanitized;
    }

    private static ArgumentException CreateEmbeddingProviderIdentifierException(string? embeddingProvider)
        => new(
            "EmbeddingProvider must use '{provider}:{model}' with a non-empty model and one of the supported providers: " +
            $"'{EmbeddingProviderDefaults.GoogleProviderName}', '{EmbeddingProviderDefaults.OllamaProviderName}'.",
            nameof(embeddingProvider));

    private async Task<float[]> GenerateGoogleAsync(string text, string tenantId, TenantEmbeddingConfig config, CancellationToken ct)
    {
        string apiKey = await GetApiKeyAsync(tenantId, config.ApiSecretKeyName, ct).ConfigureAwait(false);

        string endpointUrl = BuildGoogleEndpointUrl(config);

        string requestJson = JsonSerializer.Serialize(new
        {
            content = new
            {
                parts = new[]
                {
                    new { text },
                },
            },
            output_dimensionality = config.Dimensions,
        });

        using HttpResponseMessage response = await SendGoogleEmbeddingRequestAsync(endpointUrl, requestJson, apiKey, tenantId, ct).ConfigureAwait(false);
        if (response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
        {
            _apiKeyCache.TryRemove(config.ApiSecretKeyName, out _);
            string refreshedApiKey = await GetApiKeyAsync(tenantId, config.ApiSecretKeyName, ct).ConfigureAwait(false);
            using HttpResponseMessage retryResponse = await SendGoogleEmbeddingRequestAsync(endpointUrl, requestJson, refreshedApiKey, tenantId, ct).ConfigureAwait(false);
            return await HandleEmbeddingResponseAsync(
                retryResponse,
                tenantId,
                config.Dimensions,
                EmbeddingProviderDefaults.GoogleProviderName,
                [apiKey, refreshedApiKey],
                text,
                ct).ConfigureAwait(false);
        }

        return await HandleEmbeddingResponseAsync(
            response,
            tenantId,
            config.Dimensions,
            EmbeddingProviderDefaults.GoogleProviderName,
            [apiKey],
            text,
            ct).ConfigureAwait(false);
    }

    private async Task<float[]> GenerateOllamaAsync(string text, string tenantId, TenantEmbeddingConfig config, CancellationToken ct)
    {
        EnsureSupportedOllamaAuthMode(config, tenantId);
        IOidcTokenProvider tokenProvider = _oidcTokenProvider
            ?? throw new EmbeddingApiException("IOidcTokenProvider is required for Ollama OIDC client credentials authentication.", tenantId);

        string clientSecret = await GetApiKeyAsync(tenantId, config.ApiSecretKeyName, ct).ConfigureAwait(false);
        string accessToken = await GetAccessTokenWrappedAsync(tokenProvider, config, clientSecret, tenantId, ct).ConfigureAwait(false);
        EnsureNonBlankBearerToken(accessToken, tenantId);

        string endpointUrl = BuildOllamaEndpointUrl(config);
        string requestJson = JsonSerializer.Serialize(new
        {
            model = config.Model,
            input = text,
        });

        using HttpResponseMessage response = await SendOllamaEmbeddingRequestAsync(endpointUrl, requestJson, accessToken, tenantId, ct).ConfigureAwait(false);
        if (response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
        {
            // AC6: evict the cached DAPR client_secret before refreshing the bearer so a rotated
            // secret is re-read symmetrically with the Google API-key path. The refreshed secret
            // becomes the input to the OIDC token request.
            _apiKeyCache.TryRemove(config.ApiSecretKeyName, out _);
            string refreshedClientSecret = await GetApiKeyAsync(tenantId, config.ApiSecretKeyName, ct).ConfigureAwait(false);
            string refreshedAccessToken = await InvalidateAndRefreshWrappedAsync(
                tokenProvider,
                config,
                refreshedClientSecret,
                tenantId,
                ct).ConfigureAwait(false);
            EnsureNonBlankBearerToken(refreshedAccessToken, tenantId);
            using HttpResponseMessage retryResponse = await SendOllamaEmbeddingRequestAsync(
                endpointUrl,
                requestJson,
                refreshedAccessToken,
                tenantId,
                ct).ConfigureAwait(false);
            return await HandleEmbeddingResponseAsync(
                retryResponse,
                tenantId,
                config.Dimensions,
                EmbeddingProviderDefaults.OllamaProviderName,
                [clientSecret, refreshedClientSecret, accessToken, refreshedAccessToken],
                text,
                ct).ConfigureAwait(false);
        }

        return await HandleEmbeddingResponseAsync(
            response,
            tenantId,
            config.Dimensions,
            EmbeddingProviderDefaults.OllamaProviderName,
            [clientSecret, accessToken],
            text,
            ct).ConfigureAwait(false);
    }

    private static string BuildGoogleEndpointUrl(TenantEmbeddingConfig config)
        => $"https://generativelanguage.googleapis.com/v1beta/models/{config.Model}:embedContent";

    private static string BuildOllamaEndpointUrl(TenantEmbeddingConfig config)
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

    private async Task<HttpResponseMessage> SendGoogleEmbeddingRequestAsync(
        string endpointUrl,
        string requestJson,
        string apiKey,
        string tenantId,
        CancellationToken ct)
    {
        HttpClient httpClient = _httpClientFactory.CreateClient("EmbeddingClient");
        using HttpRequestMessage request = new(HttpMethod.Post, endpointUrl);
        request.Headers.Add("x-goog-api-key", apiKey);
        request.Content = new StringContent(requestJson, Encoding.UTF8, "application/json");

        return await SendEmbeddingRequestAsync(httpClient, request, "Google", tenantId, ct).ConfigureAwait(false);
    }

    private async Task<HttpResponseMessage> SendOllamaEmbeddingRequestAsync(
        string endpointUrl,
        string requestJson,
        string accessToken,
        string tenantId,
        CancellationToken ct)
    {
        HttpClient httpClient = _httpClientFactory.CreateClient("EmbeddingClient");
        using HttpRequestMessage request = new(HttpMethod.Post, endpointUrl);
        request.Headers.Authorization = CreateBearerAuthorizationHeader(accessToken, tenantId);
        request.Content = new StringContent(requestJson, Encoding.UTF8, "application/json");

        return await SendEmbeddingRequestAsync(httpClient, request, "Ollama", tenantId, ct).ConfigureAwait(false);
    }

    private static async Task<HttpResponseMessage> SendEmbeddingRequestAsync(
        HttpClient httpClient,
        HttpRequestMessage request,
        string providerName,
        string tenantId,
        CancellationToken ct)
    {
        try
        {
            return await httpClient.SendAsync(request, ct).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            // Caller cancellation must surface as OperationCanceledException, not a wrapped
            // transport failure (Story 14.3 Task 5 Implementation Guardrails).
            throw;
        }
        catch (HttpRequestException ex)
        {
            throw new EmbeddingApiException(
                $"{providerName} embedding provider transport error while sending request.",
                tenantId,
                ex);
        }
        catch (TaskCanceledException ex)
        {
            // Not caused by caller cancellation (handled above) — therefore HttpClient.Timeout.
            throw new EmbeddingApiException(
                $"{providerName} embedding provider request timed out.",
                tenantId,
                ex);
        }
        catch (IOException ex)
        {
            throw new EmbeddingApiException(
                $"{providerName} embedding provider IO error while sending request.",
                tenantId,
                ex);
        }
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
        => _ = CreateBearerAuthorizationHeader(accessToken, tenantId);

    private static AuthenticationHeaderValue CreateBearerAuthorizationHeader(string accessToken, string tenantId)
    {
        // AuthenticationHeaderValue throws FormatException on whitespace-only parameters; reject
        // before construction with a sanitized typed exception so callers above ingestion can
        // distinguish a bad-token contract from a bad-network outcome.
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
            return new AuthenticationHeaderValue("Bearer", accessToken);
        }
        catch (FormatException ex)
        {
            throw new EmbeddingApiException(
                "OIDC token provider returned an invalid access token; refusing to construct a bearer header.",
                tenantId,
                ex);
        }
    }

    private static async Task<float[]> HandleEmbeddingResponseAsync(
        HttpResponseMessage response,
        string tenantId,
        int expectedDimensions,
        string provider,
        IReadOnlyCollection<string?> sensitiveValues,
        string fullInputText,
        CancellationToken ct)
    {
        if (response.StatusCode == HttpStatusCode.TooManyRequests)
        {
            int retryAfter = ParseRetryAfterSeconds(response.Headers.RetryAfter);
            throw new EmbeddingRateLimitException(tenantId) { RetryAfterSeconds = retryAfter };
        }

        string responseBody = await ReadEmbeddingResponseBodyAsync(response, provider, tenantId, ct).ConfigureAwait(false);

        if (!response.IsSuccessStatusCode)
        {
            throw new EmbeddingApiException((int)response.StatusCode, RedactSensitiveValues(responseBody, sensitiveValues, fullInputText), tenantId);
        }

        return IsOllama(provider)
            ? ParseOllamaEmbeddingResponse(responseBody, tenantId, expectedDimensions)
            : ParseGoogleEmbeddingResponse(responseBody, tenantId, expectedDimensions);
    }

    private static async Task<string> ReadEmbeddingResponseBodyAsync(
        HttpResponseMessage response,
        string provider,
        string tenantId,
        CancellationToken ct)
    {
        try
        {
            return await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (HttpRequestException ex)
        {
            throw new EmbeddingApiException(
                $"{provider} embedding provider transport error while reading response.",
                tenantId,
                ex);
        }
        catch (TaskCanceledException ex)
        {
            throw new EmbeddingApiException(
                $"{provider} embedding provider response read timed out.",
                tenantId,
                ex);
        }
        catch (IOException ex)
        {
            throw new EmbeddingApiException(
                $"{provider} embedding provider IO error while reading response.",
                tenantId,
                ex);
        }
    }

    /// <summary>Parses the <c>Retry-After</c> header per RFC 9110 §10.2.3 — either a delta-seconds value
    /// or an HTTP-date. Returns <c>0</c> when the header is absent, malformed, or points at a past date,
    /// so the caller can fall back to its own default. Positive results are clamped to <c>[1, 3600]</c>.</summary>
    internal static int ParseRetryAfterSeconds(RetryConditionHeaderValue? header)
    {
        if (header is null)
        {
            return 0;
        }

        if (header.Delta.HasValue)
        {
            double seconds = header.Delta.Value.TotalSeconds;
            return seconds > 0
                ? (int)Math.Clamp(seconds, 1, 3600)
                : 1;
        }

        if (header.Date.HasValue)
        {
            double seconds = (header.Date.Value - DateTimeOffset.UtcNow).TotalSeconds;
            return seconds > 0 ? (int)Math.Clamp(seconds, 1, 3600) : 0;
        }

        return 0;
    }

    private async Task<string> GetApiKeyAsync(string tenantId, string apiSecretKeyName, CancellationToken ct)
    {
        if (_apiKeyCache.TryGetValue(apiSecretKeyName, out string? cachedKey))
        {
            return cachedKey;
        }

        try
        {
            Dictionary<string, string> secret = await _daprClient
                .GetSecretAsync(SecretStoreName, apiSecretKeyName, cancellationToken: ct)
                .ConfigureAwait(false);
            string apiKey = secret[apiSecretKeyName];
            _apiKeyCache.TryAdd(apiSecretKeyName, apiKey);
            return apiKey;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            throw new EmbeddingApiException(
                $"Failed to retrieve embedding credential secret from DAPR secret store '{SecretStoreName}'. " +
                "Ensure the DAPR sidecar is running and deploy/dapr/components/secretstore.yaml is configured.",
                tenantId,
                ex);
        }
    }

    private static float[] ParseGoogleEmbeddingResponse(string responseBody, string tenantId, int expectedDimensions)
    {
        try
        {
            using JsonDocument doc = JsonDocument.Parse(responseBody);
            if (!doc.RootElement.TryGetProperty("embedding", out JsonElement embeddingElement) ||
                !embeddingElement.TryGetProperty("values", out JsonElement valuesElement))
            {
                throw new EmbeddingApiException(
                    $"Malformed embedding API response: missing 'embedding.values' path. Response: {responseBody}",
                    tenantId);
            }

            float[] values = valuesElement.Deserialize<float[]>()
                ?? throw new EmbeddingApiException(
                    $"Malformed embedding API response: 'embedding.values' deserialized to null. Response: {responseBody}",
                    tenantId);

            if (values.Length != expectedDimensions)
            {
                throw new EmbeddingApiException(
                    $"Expected {expectedDimensions} dimensions but received {values.Length}. " +
                    "Embedding API may have returned a truncated or malformed response.",
                    tenantId);
            }

            return values;
        }
        catch (JsonException ex)
        {
            throw new EmbeddingApiException(
                $"Malformed embedding API response: invalid JSON. Response: {responseBody}",
                tenantId,
                ex);
        }
    }

    private static float[] ParseOllamaEmbeddingResponse(string responseBody, string tenantId, int expectedDimensions)
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

            JsonElement firstEmbedding = embeddingsElement[0];
            if (firstEmbedding.ValueKind != JsonValueKind.Array)
            {
                throw new EmbeddingApiException(
                    "Malformed embedding API response: first 'embeddings' item must be an array.",
                    tenantId);
            }

            float[] values = firstEmbedding.Deserialize<float[]>()
                ?? throw new EmbeddingApiException(
                    "Malformed embedding API response: first 'embeddings' item deserialized to null.",
                    tenantId);

            if (values.Length != expectedDimensions)
            {
                throw new EmbeddingApiException(
                    $"Expected {expectedDimensions} dimensions but received {values.Length}. " +
                    "Embedding API may have returned a truncated or malformed response.",
                    tenantId);
            }

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

    private static bool IsGoogle(string provider)
        => string.Equals(provider, EmbeddingProviderDefaults.GoogleProviderName, StringComparison.OrdinalIgnoreCase);

    private static bool IsOllama(string provider)
        => string.Equals(provider, EmbeddingProviderDefaults.OllamaProviderName, StringComparison.OrdinalIgnoreCase);

    private static void EnsureSupportedOllamaAuthMode(TenantEmbeddingConfig config, string tenantId)
    {
        if (!IsOllama(config.Provider))
        {
            return;
        }

        if (string.Equals(config.AuthMode, EmbeddingProviderDefaults.OidcClientCredentialsAuthMode, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        throw new EmbeddingApiException(
            $"Ollama auth mode '{config.AuthMode}' is not supported by EmbeddingClient. " +
            $"Use '{EmbeddingProviderDefaults.OidcClientCredentialsAuthMode}' until a provider-specific api-key contract is defined.",
            tenantId);
    }

    private static float[] CreateDeterministicVector(string text, int dimensions)
    {
        byte[] hash = SHA256.HashData(Encoding.UTF8.GetBytes(text));
        float[] vector = new float[dimensions];

        for (int i = 0; i < vector.Length; i++)
        {
            vector[i] = ((hash[i % hash.Length] / 255f) * 2f) - 1f;
        }

        return vector;
    }
}

/// <summary>Parsed persisted embedding provider identifier.</summary>
/// <param name="Provider">The embedding provider name.</param>
/// <param name="Model">The provider-specific embedding model identifier.</param>
internal sealed record EmbeddingProviderIdentifier(string Provider, string Model);
