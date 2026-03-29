// <copyright file="EmbeddingClient.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Ingestion;

using System.Net;
using System.Text;
using System.Text.Json;

using Dapr.Client;

/// <summary>Typed HTTP client for generating embeddings via Google text-embedding-004 API.</summary>
public class EmbeddingClient
{
    private const int ExpectedDimensions = 768;
    private const string GoogleEmbeddingEndpoint = "https://generativelanguage.googleapis.com/v1/models/text-embedding-004:embedContent";
    private const string SecretKeyName = "google-embedding-api-key";
    private const string SecretStoreName = "secretstore";

    private readonly DaprClient _daprClient;
    private readonly HttpClient _httpClient;

    private string? _apiKey;

    /// <summary>Initializes a new instance of the <see cref="EmbeddingClient"/> class.</summary>
    /// <param name="httpClient">The HTTP client injected by IHttpClientFactory.</param>
    /// <param name="daprClient">The DAPR client for secret retrieval.</param>
    public EmbeddingClient(HttpClient httpClient, DaprClient daprClient)
    {
        _httpClient = httpClient;
        _daprClient = daprClient;
    }

    /// <summary>Loads and caches the embedding API key so configuration failures happen before rate-limit budget is consumed.</summary>
    /// <param name="tenantId">The tenant identifier for error context.</param>
    /// <param name="ct">A cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public virtual async Task PrimeApiKeyAsync(string tenantId, CancellationToken ct)
        => _ = await GetApiKeyAsync(tenantId, ct).ConfigureAwait(false);

    /// <summary>Generates a 768-dimension embedding vector for the given text.</summary>
    /// <param name="text">The text content to embed.</param>
    /// <param name="tenantId">The tenant identifier for error context.</param>
    /// <param name="ct">A cancellation token.</param>
    /// <returns>A 768-dimension float array.</returns>
    public virtual async Task<float[]> GenerateAsync(string text, string tenantId, CancellationToken ct)
    {
        ArgumentException.ThrowIfNullOrEmpty(text);

        string apiKey = await GetApiKeyAsync(tenantId, ct).ConfigureAwait(false);

        string requestJson = JsonSerializer.Serialize(new
        {
            content = new
            {
                parts = new[]
                {
                    new { text },
                },
            },
        });

        using HttpRequestMessage request = new(HttpMethod.Post, GoogleEmbeddingEndpoint);
        request.Headers.Add("x-goog-api-key", apiKey);
        request.Content = new StringContent(requestJson, Encoding.UTF8, "application/json");

        using HttpResponseMessage response = await _httpClient.SendAsync(request, ct).ConfigureAwait(false);

        if (response.StatusCode == HttpStatusCode.TooManyRequests)
        {
            throw new EmbeddingRateLimitException(tenantId);
        }

        string responseBody = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);

        if (!response.IsSuccessStatusCode)
        {
            throw new EmbeddingApiException((int)response.StatusCode, responseBody, tenantId);
        }

        return ParseEmbeddingResponse(responseBody, tenantId);
    }

    private async Task<string> GetApiKeyAsync(string tenantId, CancellationToken ct)
    {
        if (_apiKey is not null)
        {
            return _apiKey;
        }

        try
        {
            Dictionary<string, string> secret = await _daprClient
                .GetSecretAsync(SecretStoreName, SecretKeyName, cancellationToken: ct)
                .ConfigureAwait(false);
            _apiKey = secret[SecretKeyName];
            return _apiKey;
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

    private static float[] ParseEmbeddingResponse(string responseBody, string tenantId)
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

            if (values.Length != ExpectedDimensions)
            {
                throw new EmbeddingApiException(
                    $"Expected {ExpectedDimensions} dimensions but received {values.Length}. " +
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
}
