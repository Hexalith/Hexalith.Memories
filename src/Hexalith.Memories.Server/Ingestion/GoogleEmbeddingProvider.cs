// <copyright file="GoogleEmbeddingProvider.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Ingestion;

using System.Text;
using System.Text.Json;

using Hexalith.Memories.Contracts.V1;

/// <summary>Google Generative Language embedding provider strategy. Owns the Google request payload, endpoint path, the
/// <c>x-goog-api-key</c> header, and response parsing for both the single <c>:embedContent</c> call and the batch
/// <c>:batchEmbedContents</c> call (Story 23.9, AC3). Transport, auth-retry, and redaction are shared
/// (<see cref="EmbeddingProviderTransport"/>).</summary>
internal sealed class GoogleEmbeddingProvider : IEmbeddingProvider
{
    // spec-infrastructure-dependency-abstraction (F2, Decision D30): the Google endpoint base URL is
    // injected from config (EmbeddingProviders:Google:ApiBaseUrl) rather than a compiled const.
    private readonly string _apiBaseUrl;

    private readonly EmbeddingSecretStore _secretStore;

    /// <summary>Initializes a new instance of the <see cref="GoogleEmbeddingProvider"/> class.</summary>
    /// <param name="secretStore">The shared DAPR secret store.</param>
    /// <param name="apiBaseUrl">The config-sourced Google Generative Language models base URL.</param>
    public GoogleEmbeddingProvider(EmbeddingSecretStore secretStore, string apiBaseUrl)
    {
        ArgumentNullException.ThrowIfNull(secretStore);
        ArgumentException.ThrowIfNullOrWhiteSpace(apiBaseUrl);
        _secretStore = secretStore;
        // Trailing slash would produce a double-slash path segment when model names are appended (review P10).
        _apiBaseUrl = apiBaseUrl.TrimEnd('/');
    }

    /// <inheritdoc/>
    public string DisplayName => "Google";

    /// <inheritdoc/>
    public async Task PrimeAsync(string tenantId, TenantEmbeddingConfig config, CancellationToken ct)
        => _ = await _secretStore.GetSecretAsync(tenantId, config.ApiSecretKeyName, ct).ConfigureAwait(false);

    /// <inheritdoc/>
    public async Task<EmbeddingProviderCredentials> AuthenticateAsync(string tenantId, TenantEmbeddingConfig config, CancellationToken ct)
    {
        string apiKey = await _secretStore.GetSecretAsync(tenantId, config.ApiSecretKeyName, ct).ConfigureAwait(false);
        return new EmbeddingProviderCredentials(apiKey, [apiKey]);
    }

    /// <inheritdoc/>
    public async Task<EmbeddingProviderCredentials> RefreshCredentialsAsync(string tenantId, TenantEmbeddingConfig config, CancellationToken ct)
    {
        _secretStore.Evict(config.ApiSecretKeyName);
        string apiKey = await _secretStore.GetSecretAsync(tenantId, config.ApiSecretKeyName, ct).ConfigureAwait(false);
        return new EmbeddingProviderCredentials(apiKey, [apiKey]);
    }

    /// <inheritdoc/>
    public HttpRequestMessage BuildRequest(IReadOnlyList<string> texts, TenantEmbeddingConfig config, EmbeddingProviderCredentials credentials, bool batch)
    {
        string endpointUrl = batch
            ? $"{_apiBaseUrl}/{config.Model}:batchEmbedContents"
            : $"{_apiBaseUrl}/{config.Model}:embedContent";

        string requestJson = batch
            ? BuildBatchRequestJson(texts, config)
            : BuildSingleRequestJson(texts[0], config);

        HttpRequestMessage request = new(HttpMethod.Post, endpointUrl);
        request.Headers.Add("x-goog-api-key", credentials.PrimaryValue);
        request.Content = new StringContent(requestJson, Encoding.UTF8, "application/json");
        return request;
    }

    /// <inheritdoc/>
    public IReadOnlyList<float[]> ParseResponse(string responseBody, int expectedCount, int expectedDimensions, string tenantId, bool batch)
        => batch
            ? ParseBatchResponse(responseBody, expectedCount, expectedDimensions, tenantId)
            : [ParseSingleResponse(responseBody, expectedDimensions, tenantId)];

    private static string BuildSingleRequestJson(string text, TenantEmbeddingConfig config)
        => JsonSerializer.Serialize(new
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

    private static string BuildBatchRequestJson(IReadOnlyList<string> texts, TenantEmbeddingConfig config)
    {
        // Each batch request's model must match the path model (documented `models/{model}` resource form).
        string modelResource = $"models/{config.Model}";
        var requests = texts
            .Select(text => new
            {
                model = modelResource,
                content = new
                {
                    parts = new[]
                    {
                        new { text },
                    },
                },
                output_dimensionality = config.Dimensions,
            })
            .ToArray();

        return JsonSerializer.Serialize(new { requests });
    }

    private static float[] ParseSingleResponse(string responseBody, int expectedDimensions, string tenantId)
    {
        try
        {
            using JsonDocument doc = JsonDocument.Parse(responseBody);
            if (!doc.RootElement.TryGetProperty("embedding", out JsonElement embeddingElement) ||
                !embeddingElement.TryGetProperty("values", out JsonElement valuesElement))
            {
                throw new EmbeddingApiException(
                    "Malformed embedding API response: missing 'embedding.values' path.",
                    tenantId);
            }

            float[] values = valuesElement.Deserialize<float[]>()
                ?? throw new EmbeddingApiException(
                    "Malformed embedding API response: 'embedding.values' deserialized to null.",
                    tenantId);

            EnsureExpectedDimensions(values.Length, expectedDimensions, tenantId);
            return values;
        }
        catch (JsonException ex)
        {
            throw new EmbeddingApiException(
                "Malformed embedding API response: invalid JSON.",
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
                JsonElement item = embeddingsElement[i];
                if (item.ValueKind != JsonValueKind.Object ||
                    !item.TryGetProperty("values", out JsonElement valuesElement))
                {
                    throw new EmbeddingApiException(
                        $"Malformed embedding API response: missing 'embeddings[{i}].values' path.",
                        tenantId);
                }

                float[] values = valuesElement.Deserialize<float[]>()
                    ?? throw new EmbeddingApiException(
                        $"Malformed embedding API response: 'embeddings[{i}].values' deserialized to null.",
                        tenantId);

                EnsureExpectedDimensions(values.Length, expectedDimensions, tenantId);
                vectors[i] = values;
            }

            return vectors;
        }
        catch (JsonException ex)
        {
            throw new EmbeddingApiException(
                "Malformed embedding API response: invalid JSON.",
                tenantId,
                ex);
        }
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
