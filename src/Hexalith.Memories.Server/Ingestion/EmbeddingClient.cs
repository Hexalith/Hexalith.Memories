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
    private const string FakeEmbeddingConfigKey = "Memories:Testing:UseFakeEmbedding";
    private const string SecretStoreName = "secretstore";

    private readonly ConcurrentDictionary<string, string> _apiKeyCache = new();
    private readonly DaprClient _daprClient;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly bool _useFakeEmbedding;

    /// <summary>Initializes a new instance of the <see cref="EmbeddingClient"/> class.</summary>
    /// <param name="httpClientFactory">The HTTP client factory for creating HTTP clients.</param>
    /// <param name="daprClient">The DAPR client for secret retrieval.</param>
    /// <param name="configuration">The application configuration.</param>
    /// <param name="hostEnvironment">The current host environment.</param>
    public EmbeddingClient(IHttpClientFactory httpClientFactory, DaprClient daprClient, IConfiguration configuration, IHostEnvironment hostEnvironment)
    {
        _httpClientFactory = httpClientFactory;
        _daprClient = daprClient;
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

        string apiKey = await GetApiKeyAsync(tenantId, config.ApiSecretKeyName, ct).ConfigureAwait(false);

        string endpointUrl = BuildEndpointUrl(config);

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

        using HttpResponseMessage response = await SendEmbeddingRequestAsync(endpointUrl, requestJson, apiKey, ct).ConfigureAwait(false);
        if (response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
        {
            _apiKeyCache.TryRemove(config.ApiSecretKeyName, out _);
            string refreshedApiKey = await GetApiKeyAsync(tenantId, config.ApiSecretKeyName, ct).ConfigureAwait(false);
            using HttpResponseMessage retryResponse = await SendEmbeddingRequestAsync(endpointUrl, requestJson, refreshedApiKey, ct).ConfigureAwait(false);
            return await HandleEmbeddingResponseAsync(retryResponse, tenantId, config.Dimensions, ct).ConfigureAwait(false);
        }

        return await HandleEmbeddingResponseAsync(response, tenantId, config.Dimensions, ct).ConfigureAwait(false);
    }

    private static string BuildEndpointUrl(TenantEmbeddingConfig config)
        => config.Provider.ToLowerInvariant() switch
        {
            EmbeddingProviderDefaults.GoogleProviderName => $"https://generativelanguage.googleapis.com/v1beta/models/{config.Model}:embedContent",
            _ => throw new ArgumentException(
                $"Provider '{config.Provider}' is not supported in the MVP implementation.",
                nameof(config.Provider)),
        };

    private async Task<HttpResponseMessage> SendEmbeddingRequestAsync(
        string endpointUrl,
        string requestJson,
        string apiKey,
        CancellationToken ct)
    {
        HttpClient httpClient = _httpClientFactory.CreateClient("EmbeddingClient");
        using HttpRequestMessage request = new(HttpMethod.Post, endpointUrl);
        request.Headers.Add("x-goog-api-key", apiKey);
        request.Content = new StringContent(requestJson, Encoding.UTF8, "application/json");

        return await httpClient.SendAsync(request, ct).ConfigureAwait(false);
    }

    private static async Task<float[]> HandleEmbeddingResponseAsync(
        HttpResponseMessage response,
        string tenantId,
        int expectedDimensions,
        CancellationToken ct)
    {
        if (response.StatusCode == HttpStatusCode.TooManyRequests)
        {
            int retryAfter = ParseRetryAfterSeconds(response.Headers.RetryAfter);
            throw new EmbeddingRateLimitException(tenantId) { RetryAfterSeconds = retryAfter };
        }

        string responseBody = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);

        if (!response.IsSuccessStatusCode)
        {
            throw new EmbeddingApiException((int)response.StatusCode, responseBody, tenantId);
        }

        return ParseEmbeddingResponse(responseBody, tenantId, expectedDimensions);
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
                $"Failed to retrieve embedding API key from DAPR secret store '{SecretStoreName}'. " +
                "Ensure the DAPR sidecar is running and deploy/dapr/components/secretstore.yaml is configured.",
                tenantId,
                ex);
        }
    }

    private static float[] ParseEmbeddingResponse(string responseBody, string tenantId, int expectedDimensions)
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
                    "Google API may have returned truncated or malformed response.",
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
